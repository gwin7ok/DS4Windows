# Phase8 不具合修正: マクロの「押している間繰り返す」フラグの統合 計画書

作成日: 2026-10-20
状態: **承認済み（Q2＝ログの項目を消す案）。W1・W2 は実装済みで、ビルド・テスト成功を確認済み。W3 も実装済みで、ビルド・テスト成功、実機確認済み（ログ確認でも異常なし）。W4（文書）も完了。本計画は完了（2026-10-21）**
- W3 の追加の整理: `ActionsLoadedRuntimeReset` は、`macrodone` の解除を削除したため `MacroRuntimeState` への依存（コンストラクターの引数）も削除した。`ds4ControlMapping`／`DS4ControltoInt`／`macroEndIndex`／`DS4_CONTROL_MACRO_ARRAY_LEN`／`MacroRuntimeState.ControlArrayLength` も、`macrodone` の添字専用だったため削除した。
- W3 の削除したテスト: `Phase7Step9W3MacroDispatchGuardTests`（W2 で前倒しした `MappingPlayMacroDispatchGuardTests` に加えて）。
- W1・W2 は、W1 だけだと編集画面が読み込み後の `RepeatMacro` を認識できず、再保存で繰り返しが落ちるため、まとめて実施した。
- W3 のうち `MappingPlayMacroDispatchGuardTests` の削除は、`HoldMacro` の削除でコンパイルできなくなるため、W2 で前倒しした。
対象ブランチ: `For-DI-migration-work`

## 1. 不具合と原因

- 症状: Controls タブの □ に割り当てた「Repeat While Held」のマクロが、押しっぱなしでも繰り返し実行されない。
- 原因: 「押している間繰り返す」を表すフラグが `HoldMacro`（Controls タブ）と `RepeatMacro`（SA タブ）に分かれており、経路ごとに片方しか保存・復元・実行されない。
  - Controls の XML は `HoldMacro` だけを書き、読み込みも `HoldMacro` だけを復元する（`ProfileButtonSaveSection.cs:66`、`ProfileButtonControlLoadSection.cs:174`）。
  - Phase8-Step7-8c で、Controls のマクロは SA と同じ「立ち上がり 1 回」の経路に移った。この経路の繰り返しは `PlayMacroTask` の内部ループ（`RepeatMacro` のみ対象）が担う。
  - 8c の計画書（§53, §81）は「編集画面が両方のフラグを保存するため影響しない」としたが、保存の段階で `RepeatMacro` が落ちるため、通常の使い方（保存→再読み込み）でも繰り返さなくなった。
- 暫定対応（実施済み）: `Mapping.cs` の内部ループの条件を `RepeatMacro || HoldMacro` にした。本計画で恒久対応に置き換える。

## 2. 決定事項（ユーザー承認済みの方針）

| ID | 決定 |
|---|---|
| D1 | `HoldMacro` と `RepeatMacro` を **`RepeatMacro` に統合**する（SA で使っている値に揃える） |
| D2 | 待ち時間は **SA 側の仕様を採用**する（`HoldMacro` の 50ms 待ちを廃止する） |
| D3 | Controls の設定ファイル（プロファイル XML）は、保存時に **`RepeatMacro`** と書く。読み込みでは、旧値 `HoldMacro` も受け付けて `RepeatMacro` に置き換える（読み込みのみ可能。保存時に置換される） |
| D4 | 設定ファイルの下方互換性（新しいファイルを旧バージョンで読むこと）は失ってよい（旧バージョンでは繰り返さない 1 回実行のマクロになる） |
| D5 | `Mapping.PlayMacro` の `control != DS4Controls.None` の死んだ経路（`macrodone`、`macroDispatchInFlight`）の整理を、**同じ Step で行う**（Phase9 へ回さない） |

## 3. 変更内容

### 3.1 保存と読み込み（Controls タブ）
- `ProfileButtonSaveSection.cs`: `RepeatMacro` があれば `RepeatMacro` を書く（`HoldMacro` は書かない）。なければ従来どおり `Macro`。
- `ProfileButtonControlLoadSection.cs`: `RepeatMacro` を読む。旧値 `HoldMacro` を読んだときも `RepeatMacro` にする。**注意**: 文字列の `Contains` 判定のため、`"RepeatMacro"` と `"HoldMacro"` はどちらも `"Macro"` を含み、`Macro` も同時に立つ（現状と同じ。保存時の `Macro` との重複は、読み込み時に吸収される）。

### 3.2 実行（Mapping.cs。ピンポイントで置換）
- 内部ループの条件（暫定対応）を `RepeatMacro` だけにする（`|| HoldMacro` を外す）。
- `HoldMacro` の 50ms 待ちと `macrodone` の解除（2 箇所）を削除する（D2）。
- 同期キューの条件（`RepeatMacro`）は変更しない。

### 3.2b 死んだ経路の整理（D5）
**根拠**: `PlayMacro`／`PlayMacroTask` の呼び出し元は 3 箇所（`PlayMacroDirect`、`PlayMacroString`）で、すべて `control` に `DS4Controls.None` を渡す。`control != None` の経路は、旧 Controls 経路（Phase8-Step7-8c で廃止）専用で、本番では到達しない。`EndMacro` の 3 つのオーバーロードも、`control != None` のときだけ働くため、本番では何もしない。`MacroRuntimeState` の `TODO(Phase8、K3)` も、この暫定対策を Phase8 で置き換える予定と記している。

**削除する**:
- `Mapping.PlayMacro` の START GUARD のうち `controlDispatchInFlight` の確保（`TryBeginDispatch`）。`IsMacroRunning` による二重起動防止は残す（ログ文言 `PlayMacro START GUARD: ... DECISION=RUN/NORUN` は維持。`control=`、`ControlDispatchInFlight=` の項目は、`control=None` と `ControlDispatchInFlight=False` の固定値を出さなくなるため、ログ書式の変更を 5 章で扱う）。
- `PlayMacroTask` の `macrodone` の判定・設定、`finally` の `EndDispatch`。
- `EndMacro` の 3 つのオーバーロードと、その呼び出し（本番では何もしない）。`EndMacroDirect`（`IMacroPlayer.Stop` の実体）は、現状も何もしないため、インターフェースを保つ空実装として残し、理由と将来の対応を TODO コメントに記す。
- `PlayMacro`／`PlayMacroTask` の引数 `control`（常に None）と `macrocontrol`（未使用。`macroControl` のフィールドを使う）。
- `MacroRuntimeState` の `MacroDone`、`ClearMacroDone`、`DispatchInFlight`、`TryBeginDispatch`、`EndDispatch`、`_dispatchLock`。
- `Mapping` の `macrodone` フィールドと、関連コメント（355〜382 行付近、434 行付近）。
- `ActionsLoadedRuntimeReset` の `ClearMacroDone` の呼び出し（ログの順序は、macrodone の解除の工程を除く。ログ文言は維持。確認は 5 章）。

**重複の統合**: `PlayMacroTask` の「最初の 1 回」と「繰り返しのループ内」で、手順の再生とキー解放のコードが、ほぼ同じものが 2 つある。1 つのローカル関数にまとめる（挙動は変えない。最初の 1 回は無条件、続けて `RepeatMacro` かつ `BeingTriggered` の間は再実行）。

**残す**: `MacroControl`／`MacroCount`（ゲームパッドのボタンのマクロ出力に使う）、`TaskQueue`（同期キュー）、`IsMacroRunning`、`ScanCode` の判定。

### 3.3 編集画面
- `OutBinding.cs`: `macroType` が `RepeatMacro` のとき `Macro | RepeatMacro` を設定する（旧: `HoldMacro` のとき `HoldMacro` のみ）。
- `RecordBoxViewModel.cs`: コンストラクターの `HoldMacro` 判定を `RepeatMacro` に変える。`ExportMacro` は `RepeatMacro` だけを立てる。
- `MacroViewModel.cs`: `PrepareSettings` は `RepeatMacro` だけを立てる。`LoadAction` は `RepeatMacro` だけを見る。
- `SpecialActionEditor.xaml.cs:451`: `RepeatMacro` だけを見る。
- **対象外**: `MultiActButtonViewModel` の `HoldMacro`（長押しジェスチャーのマクロ。別の概念で、同名なだけ）。

### 3.4 enum
- `DS4KeyType.HoldMacro` を削除する。値 16 は欠番にする（保存はビット値ではなく名前のため影響なし）。

## 4. テスト

| 対象 | 対応 |
|---|---|
| `Phase8Step78cW1ControlsMacroCharacterizationTests`、`Phase8Step78cW2ControlsMacroTests` | `HoldMacro \| RepeatMacro` を `RepeatMacro` に変更 |
| `MappingPlayMacroDispatchGuardTests`、`Phase7Step9W3MacroDispatchGuardTests` | 死んだ経路（`DispatchInFlight`、`TryBeginDispatch`／`EndDispatch`）そのもののテストのため、**ファイルごと削除**する |
| `Phase7Step7MacroRuntimeStateGuardTests`、`Phase7Step8W3OutputKbmOwnershipGuardTests`（167〜173 行）、`Phase7Step7SyntheticInputStateGuardTests`（89 行） | `MacroDone`／`ClearMacroDone`／`DispatchInFlight` への参照と、ソース文字列の検査（`private readonly bool[] macrodone;` など）を削除・更新する |
| `Step10FourGlobalRatchetGuardTests` ほかのラチェット | `MacroRuntimeState`／`Mapping` の `Global`／`AppHost` の件数が変わる場合は、基準値も同時に更新する（一致検査 `Phase7Step10eW5RatchetEqualityGuardTests` を確認する） |
| 新規: `PlayMacroTask` の統合後 | 最初の 1 回の再生、`RepeatMacro` の間の再実行、離したときの停止、`keepKeyState` の有無のキー解放が、統合前と同じであること |
| 新規: 保存・読み込み | `RepeatMacro` が保存・復元される。旧 XML の `HoldMacro` が `RepeatMacro` として読まれ、再保存で `RepeatMacro` になる |
| 新規: 実行 | `Macro \| RepeatMacro` のみの keyType（`HoldMacro` なし）で、押している間繰り返し、離すと止まる |

## 5. 文書
- `Phase8-Step7-8c-Plan.md` §48, §53, §77, §81 と `Phase8-Step7-Completion-Report.md` の該当箇所に、誤りの訂正と本計画への参照を追記する。
- `Phase8-Status.md` 等に、この不具合修正と決定事項（D1〜D4）を記録する。

## 6. リスク
- 連打の間隔が変わる（Controls のマクロは、毎回の 50ms 待ちがなくなる）。マクロ自身の待ちステップで調整する。
- 旧バージョンでは、新しく保存した Controls のマクロが繰り返さない（D4 で許容）。
- ユーザーの実機確認: □の押しっぱなしで繰り返すこと、離すと止まること、保存→再起動後も繰り返すこと。

## 7. 作業の順序（マイクロステップ）

1. **W1**: 保存・読み込みの統合（3.1）と、その新規テスト。
2. **W2**: 実行側の統合（3.2、3.3、3.4）と、`RepeatMacro` のみの繰り返しのテスト。この時点で不具合は恒久的に直る。
3. **W3**: 死んだ経路の整理と重複の統合（3.2b）、テストの削除・更新、ラチェットの基準値の更新。
4. **W4**: 文書（5 章）。
各 W の後に、ユーザーがビルドと `dotnet test` を実行して確認する。W2 の後に、実機で □ の繰り返しを確認する（W3 は実機確認を W2 とまとめてもよい）。

## 8. 確認事項

- **Q2**: 上記 3.2b のログ書式の変更（`PlayMacro START GUARD` から `control=`、`ControlDispatchInFlight=` の項目を消す）は、既存ログの維持の原則（2.3）の例外として扱ってよいか。項目を残して固定値（`control=None`、`ControlDispatchInFlight=False`）を出し続ける案もある。推奨は、項目を消す案（死んだ値を出さないため）。
