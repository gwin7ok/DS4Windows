# 修正計画書: 再接続時のプロファイルを、コントローラー単位で管理する

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
区分: 不具合の修正（Phase8-Step7 の本流とは別件。7.5b の実機確認 U-33 の最中にユーザーが発見）  
状態: **Q1〜Q6 を推奨（A）どおり承認（2026-10-20）。実装済み（W1〜W3 を 1 つにまとめた。ビルド・テストの結果待ち）**  
調査の方法: コードの読解のみ（実機・テストでの再現は未実施。原因は、コードの流れから特定した）

---

## 1. 症状と本来の仕様

**仕様**
- DS4Windows の起動後、**そのコントローラーを初めて**接続したとき: そのコントローラーの LinkedProfile を適用する。
- 切断のあと、DS4Windows を再起動せずに、**同じコントローラーを**再接続したとき: 切断の直前に適用されていたプロファイルを適用する。

**症状**
1. DS4Windows を起動する。
2. コントローラー 1（LinkedProfile = A）を接続し、切断する。
3. コントローラー 2（LinkedProfile = B）を接続する。
4. コントローラー 2 は、起動後の初めての接続なので B が適用されるはずが、**A が適用される**。

## 2. 原因（コードの特定）

**「初回接続か再接続か」と「再接続で使うプロファイル」が、どちらも、スロット番号（`index` ＝ 0〜）で管理されていて、コントローラー（MAC アドレス）で管理されていない。**

| 場所 | 内容 |
|---|---|
| `BackingStore.firstConnectionAfterStartup`（`bool[]`、`BackingStore.cs:169`） | 「起動後の初回接続」のフラグが、**スロットごと**。`App.xaml.cs:526` の `ResetConnectionFlags()` で、起動時に全スロットを true にする。 |
| `DeviceStateService.IsFirstConnection(slotIndex)`／`MarkConnected(slotIndex)`（`DeviceStateService.cs:19・21`） | 上の配列を、スロット番号で読み書きするだけ。MAC は見ない。 |
| `ControlService.PrepareConnectedInputController…`（`ControlService.cs:2218〜2243`） | `IsFirstConnection(index)` が true → LinkedProfile（なければ `OlderProfilePath[index]`）を適用し、`MarkConnected(index)`。false（再接続）→ **`SelectedProfile[index]`**（スロットの、直前のプロファイル）を適用。 |
| `ProfileApplicationService.CompleteProfileApplication`（`ProfileApplicationService.cs:215`） | 適用のたびに、`SelectedProfile[device]` を**スロットごとに**更新する。 |
| スロットの割り当て（`ControlService.cs:2110〜2114`） | 接続したコントローラーは、`DS4Controllers[Index] == null` の**最初の空きスロット**に入る。 |

**症状の流れ**
- コントローラー 1 はスロット 0。初回接続 → A を適用し、スロット 0 の「初回」フラグを false にする。`SelectedProfile[0]` ＝ A。
- 切断すると、スロット 0 は空く（フラグ・`SelectedProfile[0]` は残る）。
- コントローラー 2（初めての接続）は、最初の空きスロットである**スロット 0** に入る。スロット 0 のフラグは false なので「再接続」と判定され、**`SelectedProfile[0]` ＝ A を適用する**。LinkedProfile（B）は参照されない。

**逆の場合（コントローラー 1 が別のスロットに再接続）**: 今度は、そのスロットのフラグが true なら「初回接続」となり、Linked（または `OlderProfilePath`）を適用する。「切断前のプロファイル」ではなく Linked になる（手動で切り替えていたときに、仕様と違う）。

**補足**
- LinkedProfile の検索自体は、MAC アドレスで行っている（`ContainsLinkedProfile(device.getMacAddress())`）。つまり、直す対象は、**初回／再接続の判定と、再接続で使うプロファイルの記憶**だけ。
- `OlderProfilePath[index]`（Linked 未登録の初回接続で使う）は、設定ファイルに保存される、**スロットごとの前回のプロファイル**で、スロット単位が元の設計。今回は対象外（Q5）。

## 3. 修正の方針

「初回／再接続」と「直前のプロファイル」を、**MAC アドレスごと**にプロセスの中で記憶する。

- **記憶の置き場所**: `IDeviceStateService`（初回接続の判定を、すでに持つサービス。Q3）。
  - `IsFirstConnection(int slotIndex, string macAddress)`／`MarkConnected(int slotIndex, string macAddress)`（MAC 版を追加）。MAC を持たない（有効なシリアルがない）コントローラーは、従来のスロット単位に落とす（Q2）。
  - `void RememberProfile(string macAddress, string profileName)`／`string GetRememberedProfile(string macAddress)`（新設。大文字小文字を区別しない辞書）。
  - `ResetConnectionFlags()` は、MAC の記憶も消す（起動時）。従来のスロット単位の API は残す（有効なシリアルがない場合と、既存のテスト用）。
- **記憶の書き込み**: プロファイルを適用したとき（`CompleteProfileApplication`。非同期の切替、手動の切替、自動プロファイル、特殊アクション、再接続のすべてが通る）に、そのスロットのコントローラーの MAC で記憶する（Q4）。切断の経路（`Removal` イベント、`Stop` など）に依存せず、常に最新になる。
- **選択の変更（`ControlService.cs:2218〜2243`）**:
  - 初回接続（その MAC が未登録）→ 従来どおり、LinkedProfile（なければ `OlderProfilePath[index]`）。
  - 再接続（その MAC が登録済み）→ 記憶したプロファイル。記憶がなければ、従来どおり `SelectedProfile[index]`。
- **変えないもの**: 一時プロファイル／AutoProfile（`useAutoProfile`）の接続、LinkedProfile の UI 状態の更新、`Stop`／`Start`（サービスの再開）の扱い、記憶の永続化（なし。起動後だけ有効）。

## 4. 作業

- **W1（特性化。コードは変えない）**: `DeviceStateService` の現在の挙動をテストで固定する。スロット 0 で、コントローラー 1 → 切断 → コントローラー 2 の順に呼ぶと、2 つ目が「再接続」と判定される（今の不具合の再現）。`ControlService` の選択部分は、実機のデバイスが要るため、既存の `Phase8Step6W1DisconnectCharacterizationTests`（ソースの読解）の形式で、順序（初回は Linked／Older、再接続は Selected）を固定する。
- **W2（実装）**: `IDeviceStateService`／`DeviceStateService`（MAC 版と記憶）、`ProfileApplicationService.CompleteProfileApplication`（記憶の書き込み。`IDeviceStateService` を受け取る必要があるかは、実装時に確認）、`ControlService` の選択部分。W1 のテストを、新しい期待値（2 つ目のコントローラーは「初回」）に更新する。
- **W3（テスト）**: 仕様の 4 つの場合を、`DeviceStateService` の単体テストで確認する。(a) 別のコントローラーが同じスロットに入ったら初回、(b) 同じコントローラーが別のスロットに再接続したら記憶したプロファイル、(c) 記憶の更新（切替のたび）、(d) MAC なしはスロット単位のまま、(e) `ResetConnectionFlags` で記憶も消える。
- **W4（実機確認 U-34）**: コントローラー 2 台（それぞれ別の LinkedProfile）で、症状の手順を確認する。
- **文書**: 完了報告書（本ファイルに追記）、`Phase6-Step11-RealDevice-Verification-Checklist.md` に U-34、`Phase8-Status.md` に別件として 1 行。クラス図（`03-Class-Interface-Diagram.md`）の `IDeviceStateService` の記載は、更新するかをユーザーの指示で決める。

### 実装の記録（2026-10-20）

W1（特性化）は、コードの変更が小さいため、W2（実装）・W3（テスト）と同じ波にまとめた（不具合の再現は、新しい期待値のテストで表す）。
- `IDeviceStateService`／`DeviceStateService`: `IsFirstConnection(slot, mac)`、`MarkConnected(slot, mac)`、`RememberProfile(mac, profile)`、`GetRememberedProfile(mac)` を追加（大文字小文字を区別しない辞書、ロックつき）。`ResetConnectionFlags()` が MAC の状態も消す。MAC がないときは、従来のスロット単位に落とす（Q2）。
- `ControlService`: 接続時の選択を MAC で判定（初回 → Linked／`OlderProfilePath`、再接続 → 記憶したプロファイル［なければ `SelectedProfile[index]`］）。選択したプロファイルを、その場で記憶（`RememberProfile`）。コンストラクターで `SelectedProfileChanged` を購読し、適用のたびに、そのスロットのコントローラーの MAC で記憶する（`RememberAppliedProfile`）。
- **記憶の書き込みの設計の変更**: 計画書の §3 は `ProfileApplicationService.CompleteProfileApplication` に書き込む案だったが、`Phase7Step10bW3ContractCleanupGuardTests`（`ProfileApplicationService` が `IDeviceStateService` を持たないことを確認）に反するため、すでに適用のたびに発火する `SelectedProfileChanged`（ControlService が購読）で記憶する形にした。`ProfileApplicationService` は変えていない。
- テスト: `ReconnectProfilePerControllerTests`（新設）、`Phase8Step6W1DisconnectCharacterizationTests` の期待を更新（`IsFirstConnection(index, connectedMac)`）。

## 5. 質問（推奨は A）

- **Q1（「直前のプロファイル」の意味）**: A（推奨）: 今のスロット単位と同じ意味（`SelectedProfile`、つまり最後に適用したプロファイル。一時プロファイルを含む）を、コントローラー単位にする。B: 一時プロファイルは除き、通常のプロファイルだけを記憶する。
- **Q2（シリアルが無効なコントローラー）**: A（推奨）: MAC が使えないコントローラーは、従来のスロット単位のまま（今の動きを変えない）。B: 「接続の順番」など別の手がかりを作る（過剰）。
- **Q3（記憶の置き場所）**: A（推奨）: `IDeviceStateService` に足す。B: 専用のサービスを新設する（インターフェース、実装、登録の追加が要る）。
- **Q4（書き込みの契機）**: A（推奨）: プロファイルの適用のたび（`CompleteProfileApplication`）に記憶する。B: 切断のとき（`Removal` イベント）に記憶する（`Stop` や異常な切断の経路で漏れうる）。
- **Q5（`OlderProfilePath` の扱い）**: A（推奨）: 今回は変えない。Linked 未登録のコントローラーの**初回接続**は、従来どおり、スロットの前回のプロファイル（`OlderProfilePath[index]`）を使う（設定ファイルに保存される、スロット単位の仕様）。B: これもコントローラー単位に直す（保存形式の変更を伴う。別の計画書にする）。
- **Q6（実機確認）**: A（推奨）: W4 で、コントローラー 2 台を使い、ユーザーが U-34 を確認する。B: 単体テストだけで済ませる（実際のスロットの割り当ては、単体テストが届かない）。

## 6. リスクと対応

- **MAC の表記**: `getMacAddress()` の戻り値は、`ContainsLinkedProfile` と同じ文字列。辞書は大文字小文字を区別しない。
- **適用の経路の漏れ**: `CompleteProfileApplication` を通らない適用がないかを、W2 で確認する（`SelectedProfile` を直接書くのは、`ControlService.cs:2246` と `CompositeDeviceModel.cs:175` のみ。後者は画面の選択で、適用に続く）。
- **Joy-Con の結合（2 台で 1 スロット）**: 主デバイスの MAC を使う。W2 で確認する。
- **ロールバック**: 変更は小さいので、コミットを戻せば元に戻る。

## 7. 完了条件

- [ ] W1〜W3 のビルド・テストが成功（ユーザーが実行）。全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。
- [ ] 症状の手順（コントローラー 1 → 切断 → コントローラー 2）で、コントローラー 2 に B が適用される（U-34）。
- [ ] 同じコントローラーの再接続で、切断前のプロファイルが適用される（U-34）。
