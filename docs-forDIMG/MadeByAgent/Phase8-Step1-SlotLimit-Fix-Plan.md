# Phase8-Step1 W2b 計画書: 「デバイス上限が 4 前提」の箇所の洗い出しと、8 スロット対応の是正

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step1-Plan.md`（W2 の副産物）、`Phase8-Step1-Trigger-Spec-Ledger.md` §8 項目 10  
発端: Phase8-Step1 W2 の調査で、`Mapping.nowAction`／`oldnowAction` が 4 要素のため、`delayTime` のアクションがデバイス 4〜7 で例外になることが判明。ユーザー依頼（2026-10-19）: 「デバイス上限が 4 要素を前提とした実装が他にもないか調べて、まとめて 8 に修正する計画を立てて」  
状態: **完了（Q1〜Q4 を推奨どおり承認。W2b-1〜W2b-4 のビルド・テスト成功、2026-10-19）。W2b-5（実機。5 台以上でのdelayTime）は未**

---

## 1. 調査の方法と範囲

- 対象: `DS4Windows/` の全 `.cs`（`HidLibrary`、`Translations`、生成コードを除く）と `*.xaml`。
- 手法（読み取りのみ）:
  - 要素数 4（または 2〜7）の配列の生成・初期化子（`new T[4]`、`{ a, b, c, d }`、同一要素の列挙）。
  - デバイス添字を 3・4 と比較する式（`< 4`、`<= 3`、`> 3`、`>= 4`、`Length == 4` ほか）。
  - 定数 `OldXInputControllerCount`（4）、`NUMBER_SLOTS`（4）の参照。
  - コメント・文書の「4 台」「0〜3」。
  - 配列の宣言を持つ主要クラス（`Mapping`、`ControlService`、`DS4LightBar`、`DS4Devices`、`Mouse`、保持クラス）の、サイズ式の確認。
  - git 履歴での導入時期の確認。
- 前提: 現行の上限は `ControllerLimits.MaxControllerCount = 8`（2020-09-06 の上流コミット `5925be0a`「8 Controller prototype」で 4 → 8）。`TestProfileItemCount = 9`（8＋テスト用の 1）。OS が Windows 7 以前のときだけ 4（`EnvironmentService.CalculateControllerSlotLimit` ＝ `OldXInputControllerCount`）。
- 限界: 静的な走査であり、実行時に確かめた結果ではない。XAML の項目数や、デバイス添字を別名の変数で比較する式は、パターンに合わない場合は拾えない。

## 2. 調査結果

### A. 是正対象（確定）

**A-1. `Mapping.nowAction`／`oldnowAction`（`Mapping.cs:695-696`）が 4 要素。**
- 初期化子が `DateTime.MinValue` を 4 つ。8 スロットの環境で、デバイス 4〜7（コントローラー 5〜8 台目）に `delayTime > 0` の特殊アクションがあると、`IndexOutOfRangeException` になる。
- 影響:
  - `MapCustomAction`: `delayTime` の分岐（`Mapping.cs:4332-4336` 付近）が、毎フレーム `nowAction[device]` を読むため、毎フレーム例外 → 外側の `catch` で `MapCustomAction exception` が GUI ログに記録され、**そのデバイスの特殊アクション全体（復帰の判定を含む）が、毎フレーム打ち切られる**。GUI ログが毎フレーム増える。
  - Stage3（Button）: `IsSpecialActionTriggered` の `delayTime` 分岐（`Mapping.cs:6674` 付近）が同じ配列を読むため、`Synchronous Button SA handling error` となり、**そのデバイスの Button 種別がすべて**、そのフレームの出力を打ち切られる。
- 旧実装との比較: 4 要素の初期化子は **上流の 2015-02-12（Version 1.4.24）から**。上流が上限を 8 にした 2020-09-06 の変更で、更新が漏れた。**DI 移行で入ったものではなく、上流の元からの不具合**。同じ `Mapping` の `oldnowKeyAct`（8 要素）や `deviceRuntime` ほかは、`MaxControllerCount` に更新済み。
- 是正: `new DateTime[ControllerLimits.MaxControllerCount]`（`DateTime` の既定値は `MinValue` なので、初期化子は不要）。
- 見込み: 2 行。

### B. 要判断（意図の可能性がある）

**B-1. `OutputSlotPersistDTO.MapTo`（`DTOXml/OutputSlotPersistDTO.cs:71`）が、出力スロットの添字を `0〜3` に限る。**
- 保存（`MapFrom`）はすべての恒久（Permanent）のスロットを書くが、読込は添字 4〜7 のスロットを無視する。出力スロットは `slotLimit`（8）個あるため、5〜8 番目のスロットを恒久にした設定が、次回起動で復元されない。
- 旧実装との比較: 条件 `idx >= 0 && idx <= 3` は、**上流の `OutputSlotPersist` から**（2022-09-24 の `b7d44452`［DTO 化］で、旧 `idx <= 3` をそのまま移した）。上流の元の実装で、出力スロットは当初 4 つ固定だった（`OutputSlotManager.cs:36-39` に、4 つの `OutSlotDevice` を並べた旧初期化子がコメントで残る）。
- 判断が要る理由: 仮想パッドの上限が XInput の 4 台（X360 出力）に由来する意図かもしれない。DS4 出力は 8 台まで可能なため、スロット 5〜8 に恒久の設定を許すかは仕様の問題。
- 是正案（採用する場合）: 条件を `idx >= 0 && idx < destination.OutputSlots.Length` にする。
- 見込み: 1 行。ただし、実機（スロット 5〜8 の恒久の保存・復元）での確認が要る。

### C. 意図的な 4（対象外。理由つき）

- **`UdpServer.NUMBER_SLOTS = 4`**（Cemuhook／DSU プロトコルの仕様。クライアントが要求できるスロットは 4 つ）。`ControlService` の利用箇所（`udpEuroPairAccel`／`udpEuroPairGyro`／`udpOutBuffers`、`ChangeUDPStatus` のループ、`ChangeMotionEventStatus` の `i < NUMBER_SLOTS`、`PrepareConnectedInputControllerSettingEvents` の `index < NUMBER_SLOTS`）は、すべて範囲のガード付き。`UdpServer.ClientRequestTimes.padIds`（4 要素）も、プロトコルの要求の添字（`idToReg < padIds.Length` のガード付き）。**8 台目までを UDP で配信できないのは仕様**。
- **`EnvironmentService.CalculateControllerSlotLimit`**（OS が Windows 7 以前のとき 4。XInput の旧上限）。
- **`ProfileEditor.specialActionsHeaderTextBlocks`（4）**、`ProfileSettingsViewModel.touchpadInvertToValue`（4）、`PresetMenuHelper` の 4 要素、`JoyConDevice`／`SwitchProDevice` の `for (i < 4)`、`gyroModeBuffer`（4 バイト）、`SensitivitySettings._values`（6）、`Mapping.stickValueHistory` の内側の `[2]`: いずれも、デバイス数と無関係な固定の構造（列数、バイト数、軸数）。

### D. コメントのみ（動作に影響なし）

- `Actions/IMacroPlayer.cs:14,21,29`、`DI/IProfileApplicationService.cs:13,35` の `deviceIndex` の説明が「0〜3」のまま。
- `Mapping.cs:5438,5447` の `new bool[4]`（`PlayMacroDirect`／`EndMacroDirect`）は、デバイス数ではなく、上流（Version 1.4.4）以来の未使用に近い引数（`macrocontrol` を添字参照する箇所がない）。Phase6-Step9b F05 のコメント「4 スロット固定」の対象外。動作に影響しないため、是正しない。
- これらは訂正の対象（コメントのみ）。

### E. 問題なしを確認したもの

- `Mapping` の `deviceRuntime`、`suppressedTriggerButtons`、`customMapQueue`、`oldnowKeyAct`、`held`、`fadetimer`、`prevFadetimer`、`lastColor`、各保持クラスの配列（`MaxControllerCount` または `TestProfileItemCount` で宣言、初期化子の数も一致。要素数が宣言と合わないとコンパイルエラーになる形）。
- `DS4LightBar` の `counters`／`fadewatches`／`forcelight` ほか、`ControlService` の `DS4Controllers`／`touchPad`／各状態配列、`DefaultMacroPlayer._isPlaying`、`BackingStore` の `profileActions` 系、`AppSettingsDTO`／`AutoProfileEntrySerializer` の `Controller1`〜`Controller8`、`AutoProfileService` の `ControllerSlotLimit` ループ。

## 3. 是正の計画

### W2b-1: 回帰テスト（先に書く。是正前に赤になる形）
1. **配列の長さのガード**: `Mapping` の、デバイス添字の配列フィールド（`nowAction`、`oldnowAction`、`oldnowKeyAct`、`held`、`fadetimer`、`prevFadetimer`、`lastColor`、`deviceRuntime`、`suppressedTriggerButtons`、`customMapQueue` ほか）の長さが `ControllerLimits.MaxControllerCount` であること。リフレクションで、名前の許可リストを検査する（長さが 4 のフィールドが新たに増えたことも検出する）。
2. **実走行**: `delayTime > 0` の特殊アクション（Key／Button／Profile など）を、デバイス 5（`MappingCharacterizationScope.Device`）で走らせ、`MapCustomAction exception` と `Synchronous Button SA handling error` が記録されないこと。`SpecialActionTestScope` を使う。
3. **遅延の挙動の特性化**（4 要素が直ったあと、デバイス添字に依存しない挙動を固定）: 離している間に `oldnowAction` が更新され、押してから `delayTime` が経つまで不成立、経過後に成立する（実時間 `Thread.Sleep` を使うが、閾値の前後は余裕を持つ）。

### W2b-2: 是正 A-1
- `nowAction`／`oldnowAction` を `new DateTime[ControllerLimits.MaxControllerCount]` にする。テストが成功することを確認する。
- ラチェットの基準値には影響しない見込み。

### W2b-3: 是正 B-1（Q1 の決定による）
- 採用する場合: `OutputSlotPersistDTO.MapTo` の条件を `idx < destination.OutputSlots.Length` に。回帰テスト（スロット 5〜8 の恒久の保存・復元のゴールデン、または往復テスト）を足す。
- 採用しない場合: 意図を、コードのコメントと台帳に記録する（TODO ではなく、仕様として）。

### W2b-4: コメントの訂正（D）と、台帳・進捗文書の更新
- `IMacroPlayer`／`IProfileApplicationService` の「0〜3」を「0〜（スロット上限 − 1）」に訂正。
- `Phase8-Step1-Trigger-Spec-Ledger.md` §8 項目 10 を、是正済みとして更新。`Phase8-Status.md` に W2b を記録。

### W2b-5: 実機確認（任意）
- 5 台以上のコントローラー（またはスロット 5 以降への割り当て）で、`delayTime` のアクションが動くこと。機材がなければ、リスク受容として記録する（台帳 U-番号を追加）。

## 4. 決定してほしい点（質問と推奨）

- **Q1（B-1 の扱い）**: A: スロット 5〜8 の恒久の復元を許す（`< Length`）。B: 仕様として 4 に残す（コメントで意図を記録）。
  - 推奨: **A**。理由: 出力スロットは `slotLimit`（8）個あり、保存側は全スロットを書く。読込側だけが 4 に限るのは非対称で、情報が黙って失われる。ただし、X360 出力の XInput の上限（4）が意図なら B。仮想パッドの実装上の制約をご存じなら、教えてください。
- **Q2（実行のタイミング）**: A: Phase8 の Step1 の途中（W3 の前）に実施。B: Phase8 の後（Phase9）。
  - 推奨: **A**。理由: 2 行の修正で、特性化テスト（B-1〜B-4）と同じハーネスで検証でき、Phase8 の特殊アクションの統一ディスパッチが、8 スロットで動く前提を固められる。
- **Q3（`delayTime` の挙動の特性化）**: A: W2b-1 の 3 で固定する。B: 固定せず、例外が出ないことの確認（2）だけにする。
  - 推奨: **A**。理由: `delayTime` は統一ディスパッチの `ComboTriggerInputAction` が引き継ぐ仕様のため、先に固定しておく。ただし実時間を使うため、不安定になる場合は B に切り替える。
- **Q4（調査範囲の追加）**: XAML の項目数や、デバイス添字を別名で比較する式は、静的な走査で拾えない。A: 現状の調査で十分とする。B: 8 台を想定した実機の通し確認（W2b-5）で補う。
  - 推奨: **B**（機材があれば）。なければ A でリスク受容。

## 5. 完了条件

- [ ] W2b-1 のテストが、是正前は赤、是正後に成功する（ユーザーが実行）。
- [ ] 既存の全テスト、ラチェットの一致検査、複数型ファイル 0 のガードが成功する。
- [ ] 台帳・進捗文書・コメントの更新。
- [ ] B-1 の決定（Q1）が反映されている。
