# Phase8-Step8 計画書: 旧ディスパッチと系統 B の削除

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 の Step8、D3＝C1、D5）、`Phase8-Step7-Plan.md`（Q4＝A: 旧コードは Step8 まで残す）、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-Completion-Report.md`  
状態: **Q1〜Q8 を承認済み（2026-10-20。ユーザー: Q2＝C［ジャイロスワイプの機能廃止］、他は推奨）。Q9＝A（機能全体を廃止。通知で「残す／削除」を選べる）も承認済み（2026-10-20）。W0（監査）の結果は §5。**

---

## 1. 目的と位置づけ

- Step7（7.1〜7.8f）で、特殊アクション（SA）のすべての種別と Controls タブの割り当て（Key／Macro／Extras／Button のパッド・マウス・ホイール・移動・Toggle）を、新経路（`TriggerDispatchEngine`）へ切り替えた。旧経路のコードは、ロールバックを `ServiceRegistration` の 1 行で済ませるために、Step8 まで残してある。
- Step8 では、新経路が処理するようになった旧コードと、役目を終えた**系統 B**（`Actions.Action` 系の `IActionFactory`、各 Adapter など）、および切替のスイッチ（`BindingMigrationOptions`）を削除する。`Mapping.cs`（現在 7,401 行）から、約 2,000 行を減らす見込み（Phase8 全体計画 §26 の見積もり）。
- **Step8 の後は、ロールバックが「1 行を外す」ではなく「PR を git で戻す」になる。** そのため、削除を小さな PR に分け、各 PR でテスト（旧・新の比べで固定した期待値）を通す。実機確認（Step9。U-35〜U-47）を削除の前に行うかは Q1。
- やらないこと: `Mapping` の責務分割（Phase9）、挙動・ログ・保存形式の変更（規約 2.2／2.3。削除する診断ログは Q7 で承認を得る）、ジャイロスワイプ以外の新機能。

## 2. 削除の対象（調査の見立て。W0 で確定する）

行番号は 2026-10-20 時点の `Mapping.cs`。

### 2.1 `Mapping.cs` の旧経路

| 区分 | 場所（概算） | 内容 |
|---|---|---|
| A. SA の Stage3（Button の同期処理）と出力の組み合わせ | 2,650〜2,840 行付近（`lastButtonTriggerState` ほか） | 旧 Button の毎フレームの出力。新経路は `ApplyFrameOutputs`（`_engine.Owns` で素通しにしている） |
| B. `customMapQueue` とその処理 | 2,629〜2,650 行、3,183 行、`ControlToXInput` | 旧パッド出力のキュー（7.8f-1 で新経路は `ControlPadOutputAction`） |
| C. `ProcessControlSettingAction` の Controls タブの旧枝 | 3,738〜4,090 行 | Key／Macro／Button（パッド・マウス・ホイール・移動・Toggle）／Default の素通し（`_engine.OwnsControl` で素通し） |
| D. `MapCustomAction` の SA の種別ごとの旧枝 | 4,166〜5,250 行（約 1,000 行） | Macro、Button、Key、Program、Profile、DisconnectBT、BatteryCheck、ステアリング較正、GyroCalibrate、MultiAction と、押し続けの解除・uTrigger の処理 |
| E. 判定の旧関数 | `GetBoolMapping`、`getBoolSpecialActionMapping`、`GetBoolActionMapping`、`GetXYAxisMapping`、`GetByteMapping`、`GetMouseWheelMapping`、`getBool*Mapping`（静的）ほか | 新経路は `TriggerInput` に移植済み。**`getMouseMapping` は、新経路が借りている（`MouseSampler`）ため残す**（Q5） |
| F. 旧経路だけの状態 | `lastButtonTriggerState`、`stickWheelRemainder`／`stickWheelDownDir`／`stickWheel`、`pressagain`、`_syntheticActionCache`（`GetOrCreateSyntheticKeyAction`）、`FindSpecialActionForLogicalKey`、`DispatchOrSetBeingTriggered`、`GetBeingTriggered`／`SetBeingTriggered` ほか | `oldnow`は`Commit`のデジタルのホイールの繰り返しが使うため残す。他は呼び出し元が消えたら削除（W0 で確定） |
| G. 旧経路の診断ログ | `Mapping SUMMARY (5s)`、`SpecialAction BUTTON/KEY press delegated…` ほか | 旧機構の説明用。削除は Q7 で承認を得る |

### 2.2 系統 B と切替のスイッチ

| 区分 | 内容 |
|---|---|
| K. ジャイロスワイプ（機能廃止。Q2＝C） | `DS4Controls.GyroSwipeLeft／Right／Up／Down`、`GyroSwipeInfo`（設定）、`Mouse.cs` の処理、`ProfileGyroSwipeLoadSection`／`ProfileMotionSaveSection` の読み書き、`ProfileEditor` の画面、`MappingListViewModel`、`ControlSettingsGroup`、`X360ControlTables`／`DS4ControlNames`／`DS4StateFieldMapping` の項目、`Mapping` の処理（約 145 か所で参照）。範囲は W0 で確定し、Q9 で承認を得る |
| H. 系統 B | `IActionFactory`／`ActionFactory`／`DefaultActionFactory`、`*Adapter`（`KeyActionAdapter`、`MacroActionAdapter`、`ProfileSwitchActionAdapter`、`LaunchProcessActionAdapter`、`KeyButtonActionControllerAdapter`）、`IActionController`／`KeyButtonActionController` 系ほか。**`KeyAction` は `KeyOutputAction` が委譲しているため残る**（D1 の注記）。W0 で、各型の呼び出し元を確かめて確定する |
| I. 切替のスイッチ | `BindingMigrationOptions`（`MigratedTypes`／`MigratedControlKinds`）、`ControlBindingKind` の「有効な区分」の判定、エンジンの `_options.IsMigrated(...)`。全種別が移行済みなので、常に有効にする（Q3） |
| J. テスト | 旧・新の両経路の Theory（`InlineData(false)`: 15 ファイル、`migrated` を取るテスト: 36 ファイル）、旧関数を reflection で呼ぶ比べ（`TriggerInputTests`、`Phase8Step78bW2ControlsKeyTests`、`Phase8Step78fW2ControlsPadTests`）、`MigratedTypes` を触るテスト（28 ファイル） |

### 2.3 新経路に入っていない割り当て（旧経路が処理している残り）

Step8 で旧経路を消すと、処理する者がいなくなるもの。**W0 で全件を洗い出し、Q2 で扱いを決める。**

- ジャイロスワイプの割り当て（`GyroSwipe*`。7.8b から対象外。**Q2＝C により、機能を廃止する**。範囲とデータの扱いは Q9）
- Extras の値が読めない割り当て（9 個未満など。旧経路でも途中までしか動かない）
- SA の「詳細が読めない」もの（旧経路でも何も出力しないことを 7.1 などで固定済み）
- 本番では起きないが、テスト用の構築（ポートなし）で束縛が作られない場合

## 3. 方針

1. **削除の前に、旧関数の期待値を固定する（W1）。** 旧関数と新経路を reflection で比べているテスト（アナログ値、判定、`IsActive` など）は、旧関数を消すと成り立たない。旧関数が返す値を、全コントロール・乱数の入力で表にして**テストデータとして保存**し、削除後は表と比べる。
2. **削除は小さな PR に分ける（Q1）。** 依存の少ない方から:
   - **8-0（先行）**: K（ジャイロスワイプの廃止。Q2＝C）。Shift の廃止（7.8d）と同じ進め方（計画書の W 構成、保存済みのプロファイルの扱い、利用者への通知）。
   - **8a**: C（Controls タブの旧枝）、B（`customMapQueue`）、F のうち Controls 専用のもの、判定の旧関数のうち Controls 専用のもの。
   - **8b**: A（Stage3）と D（SA の種別ごとの旧枝）、F の残り、E の残り、G。
   - **8c**: 系統 B（H）。
   - **8d**: スイッチの削除（I）、テストの整理（J）、モデル図・文書の更新。
3. 各 PR で、(a) 旧・新の比べで固定した期待値のテスト、(b) 割り当て・ロールバックの代わりの「削除後も同じ出力」の検査、(c) ラチェット（`Global.*` ほか）の一致検査、(d) `Mapping.cs` の行数の減少の確認、を通す。ビルド・テストはユーザーが実行する。
4. 規約: 1 ファイル＝1 型、削除したクラスのテストも削除、コメント（設計意図・TODO）を誤って消さない。TODO（Step8 で削除と書いたもの）は、削除と同時に消す。

## 4. 作業（Wave）

- **W0（監査）**: 2.1〜2.3 の確定。① 各区分の呼び出し元の洗い出し（削除してよいもの／残すもの）。② 2.3 の全件。③ 旧経路が行う副作用で、新経路が引き継いでいないもの（ログ、静的な状態）。④ 旧関数を使うテストの一覧。⑤ 削除する診断ログの一覧（Q7）。結果は本書 §5 に追記する。
- **8-0 の Wave（ジャイロスワイプの廃止。7.8d の Shift と同じ構成）**: **W1**（保持・警告の部品と仕様のテスト。配線なし）→ **W2**（データモデル・実行・読込・保存・画面の項目の削除）→ **W3**（編集画面の警告ダイアログ、文言）→ **W4**（文書、チェックリスト）。詳細は §5.6 以降。
- **W1（期待値の固定。8a〜8d の前提）**: 旧関数の値の表（J の reflection の比べの置き換え）。W0 で挙がった「新経路に入っていない割り当て」の特性化テスト。
- **W2（8a）→ W3（8b）→ W4（8c）→ W5（8d）**: 実装。各 PR のあとにユーザーがビルド・テスト・コミット。
- **W6**: 文書（完了報告書、`Phase8-Status.md`、モデル図 01〜04、`copilot-instructions.md` の過渡期の記述）。

## 5. W0 の結果（2026-10-20。コードの調査のみ。変更なし）

### 5.1 ジャイロスワイプの範囲（K）

参照は 18 ファイル、約 145 か所。

- **処理**: `Mouse.cs`（`GyroSwipeData`、向きの判定。約 13 か所）、`Mapping.cs`（`MapCustom` の約 2,559〜2,620 行の「前の向きを先に離す」処理、`ds4ControlMapping` の 4 行 ほか約 18 か所）、`DS4StateFieldMapping`（種別の表 4 行、入力の取り込み 4 行）。
- **割り当て（dcs）**: `ControlSettingsGroup`（`GyroSwipeLeft／Right／Up／Down` の 4 つ。8 か所）、`DS4ControlNames`、`X360ControlTables`、`DS4Controls`（列挙の 4 値）、`MappingListViewModel`、`ControlIndexCheck`、`ControlBindingBuilder`（除外の記述）。
- **設定**: `GyroSwipeInfo`（`GyroDirectionalSwipeInfo`。`IProfileSettingsService`、`ProfileSettingsService`）。
- **画面**: `ProfileEditor.xaml`（11 か所）、`ProfileEditor.xaml.cs`（13 か所）、`ProfileSettingsViewModel`（27 か所）。
- **保存形式**: 読み込み `ProfileGyroSwipeLoadSection`（`GyroSwipeSettings` 要素: `DeadZoneX`／`DeadZoneY`／`Triggers`／`TriggerCond`／`TriggerTurns`／`XAxis`／`DelayTime`。**同じ節にジャイロの感度・マウス［`GyroSensitivity` ほか］の読み込みも同居しており、これは廃止しない**）、書き込み `ProfileMotionSaveSection`（`GyroSwipeSettings` を**毎回**書く＝既定値でも常に存在）。割り当て（dcs）は、Controls の各要素（`GyroSwipeLeft` などの名前）として保存される。
- **Shift の廃止（7.8d）との違い**: Shift は「内容があるときだけ要素がある」ため、有無で通知を判定できた。ジャイロスワイプの設定は**既定値でも常に書かれる**ので、通知の判定は「`GyroSwipeSettings` が既定値と異なる、または `GyroSwipe*` の割り当てが既定（Default）でない」ときにする（W1 の特性化テストで、判定の条件を固定する）。
- **同じ進め方で足す型（Shift と対応）**: 保持する内容の入れ物（`RemovedShiftSettings` 相当）、通知の判定（`RemovedShiftNotice` 相当）、プロンプトの契約と選択（`IRemovedShiftPrompt`／`RemovedShiftChoice`／`RemovedShiftOutcome` 相当）、画面のダイアログ（`MessageBoxRemovedShiftPrompt` 相当）、文言（`Resources.resx`／`ja.resx`）、編集画面での呼び出し。**型を共通化するかは W1 の設計で決める**（1 ファイル＝1 型。Shift の型を一般化して「廃止された機能の通知」にすると、型が減るが、7.8d のテストを書き換える）。
- **注意**: 「残す」の選択では、`GyroSwipeSettings` の内容と `GyroSwipe*` の割り当ての両方を保持して、そのまま保存し直す（解釈しない）。「削除」では両方を既定にする。選択は記憶しない（Shift と同じ）。

### 5.2 旧経路の呼び出し元の洗い出し（区分 A〜F）

呼び出し元が旧経路の中だけに閉じていることを確かめた（`DS4Windows` 内の参照ファイル数）。

| 記号 | 参照の状況 | 扱い |
|---|---|---|
| `GetBoolActionMapping` | `Mapping.cs` 11 か所、ほかは新経路の移植元の説明のみ（`TriggerInput`、`ControlJudgement` のコメント）、ビルダーのコメント | 8a で削除（`Mapping` 内の旧枝が消えれば呼び出し元なし） |
| `GetBoolMapping` | `Mapping.cs` 5 か所 | 8a で削除 |
| `getBoolSpecialActionMapping` | `Mapping.cs` 17 か所。`Actions/*` はコメントのみ | 8b で削除 |
| `GetXYAxisMapping`／`GetByteMapping`／`GetMouseWheelMapping` | `Mapping.cs` のみ（`customMapQueue` の処理とホイール枝）。`Actions/*` はコメントのみ | 8a で削除（W1 の表でテストを置き換え） |
| `getMouseMapping` | `Mapping.cs`（`SampleMouseMovement` 経由で新経路が使う） | **残す**（Q5＝A） |
| `FindSpecialActionForLogicalKey`／`GetOrCreateSyntheticKeyAction`／`ClearSyntheticActionCache` | `Mapping.cs` と、ビルダー（識別用の SA を作るため。コメント参照のみか、W0 後半で確認） | 8a で削除。ビルダーの参照が実コードなら、置き換える |
| `DispatchOrSetBeingTriggered`／`GetBeingTriggered`／`SetBeingTriggered` | `Mapping.cs` のみ（15・20・5 か所） | 8b で削除 |
| `GetIsToggledOn` | `Mapping.cs` 2 か所（旧 Toggle）。`ControlToggleOutputAction` はコメントのみ | 8a で削除 |
| `lastButtonTriggerState` | `Mapping.cs` 8 か所 | 8b で削除 |
| `customMapQueue`／`ControlToXInput` | `Mapping.cs` 8 か所 | 8a で削除 |
| `stickWheelRemainder`ほか（`stickWheel`、`stickWheelDownDir`、`pressagain`） | `Mapping.cs` のみ。`oldnow` は `Commit` の繰り返しが使う | 8a で削除（`oldnow` は残す。`pressagain` は `Commit` も読むので、定数 false として整理する） |
| `EnsureActionDoneInitialized`／`ReleaseActionKeys`／`IfAxisIsNotModified` | `Mapping.cs`。`ReleaseActionKeys` は `TriggerButtonKeyReleaser` も参照 | W0 後半で確認。`IfAxisIsNotModified` は呼び出し元なし（以前から未使用）で、8a で削除 |
| `PlayMacroDirect` | `IMapping`、`DefaultMacroPlayer`（新経路が使う） | **残す** |

### 5.3 系統 B（H）の呼び出し元（暫定。8c の着手時にコンパイルで確定）

- 削除の候補（新経路から使われていない）: `IActionFactory`／`ActionFactory`／`DefaultActionFactory`、`ActionEntry`、`KeyActionAdapter`／`MacroActionAdapter`／`ProfileSwitchActionAdapter`／`LaunchProcessActionAdapter`、`SpecialActionBase`、`IKeyActionCreator`／`DefaultKeyActionCreator`、`MouseOutputAction`（`Mapping` が参照しているため要確認）。
- 残る見込み: `KeyAction`、`KeyButtonActionController`、`KeyButtonActionControllerAdapter`、`IControllerRegistry`／`DefaultControllerRegistry`、`IKeyButtonControllerRegistry`／`KeyButtonControllerRegistry`、`PressActionController`、`RepeatHelperToIRepeaterAdapter`（新経路の `KeyOutputAction`、`SyntheticDispatcher` が使う）。
- 注意: `DefaultActionManager` が `IActionFactory` に依存している。状態の管理（`IManagedActionManager`）は新経路が使うため、ファクトリーへの依存を外す変更が要る。

### 5.4 旧経路の診断ログ（G）の一覧

8b の着手前に、`Mapping.cs` の `AppLogger` 呼び出しを走査して一覧にし、承認を得る（旧機構の説明だけのもの［`Mapping SUMMARY (5s)`、`setBeingTriggeredCalls`、`press delegated to KeyButtonActionController` など］と、新経路が同じ文言で出すもの［残す］に分ける）。この一覧は、8b の前に本書に追記する。

### 5.6 8-0 W1 の実装（2026-10-20。ビルド・テスト成功）

保持・警告の判定の部品（配線なし）と、その仕様のテスト。7.8d の Shift の W1 と対応する。**型は共通化せず、Shift の型と並べて新設した**（7.8d のテストを書き換えない。重複は Phase9 の整理で扱う）。新設（1 ファイル 1 型）:

- `RemovedGyroSwipeSettings`（保持）: (1) `<GyroSwipeSettings>` ノードを、**既定値と異なるときだけ**保持（`CaptureSettings`。欠けた項目は既定とみなし、数は数として、文字列は大文字小文字を区別せずに比べる。知らない項目があれば保持）。(2) `<Control>` 内の `GyroSwipeLeft／Right／Up／Down` の割り当ての項目（`CaptureControlEntry(親のグループ名, ノード)`）を、そのまま保持。`BeginCapture`／`Clear`、書き出し `AppendSettingsTo`（保持がなければ、廃止前に毎回書いていた既定値の要素。形式は変わらない）、`AppendControlsTo`（該当するグループ［`Button`・`Macro`・`Key`・`Extras`・`KeyType`］へ戻す。なければ作る）、`IsSwipeControlName`。
- `RemovedGyroSwipeChoice`（Delete／Keep）、`RemovedGyroSwipeOutcome`（NoNotice／Deleted／Kept）、`IRemovedGyroSwipePrompt`（警告の窓口。実装は W3 のダイアログ）、`RemovedGyroSwipeNotice`（警告の判定。選択は記憶しない。「削除」は設定と割り当ての項目の両方を捨てる）。
- テスト: `Phase8Step8W1RemovedGyroSwipeSpecTests`（既定・書き方の違いは保持しない、一つでも違えば保持、割り当ての項目、読込の始めに捨てる、書き出し［既定の要素は従来と同じ文字列］、グループへ戻す、警告［内容なしは尋ねない、削除、残すは毎回尋ねる、プロンプトなし］）。
- 実行への影響: なし（どこからも使っていない）。

### 5.7 8-0 W2 の実装（2026-10-20。ビルド・テスト成功。初回の指摘［ビルド 2 件、テスト期待値 3 件］は修正済み）

W1 の調査（§5.1）に **「ジャイロの出力モード `DirectionalSwipe`」** が加わった（`GyroOutMode` の値で、編集画面の「ジャイロの出力モード」の選択肢にもあった。W2 の調査で判明）。廃止の範囲は、割り当て・設定・モード・処理・画面・保存形式の全体。

- **削除（データモデル・実行）**: `DS4Controls` の `GyroSwipeLeft／Right／Up／Down`（列挙の値が 4 つ減り、後ろの値は繰り上がる。添字で引く表［`ds4ControlMapping`、`DS4StateFieldMapping`、`X360ControlTables`、`DS4ControlNames`、`ControlSettingsGroup`］は同じ 4 行を削除して整合。保存されている番号は `ds4ControlMapping` の値［変更なし］と名前だけ）、`GyroOutMode.DirectionalSwipe`、`GyroDirectionalSwipeInfo`（ファイルごと）、`BackingStore.gyroSwipeInfo`、`IProfileSettingsService.GyroSwipeInf`／`GetGyroSwipeInfo`、`Mouse.GyroSwipeData`・`gyroSwipe`・`SixDirectionalSwipe` と出力モードの枝、`Mapping` の `MapCustom` のスワイプの処理（約 75 行）。
- **保持（Q9＝A）**: `BackingStore.removedGyroSwipeSettings`（スロットごと。`ResetProfile` で空に）、`IProfileSettingsService.GetRemovedGyroSwipeSettings`。`RemovedGyroSwipeSettings` に、ジャイロの出力モードの保持（`CaptureGyroOutputMode`／`GyroOutputModeTextForSave`／`HadDirectionalSwipeMode`／`ForgetDirectionalSwipeMode`）を追加した（W1 のテストも追加）。
- **読込**: `ProfileGyroSwipeLoadSection` を `ProfileGyroMouseLoadSection` に改名（`GyroSwipeSettings` は保持だけ。同じ節のジャイロの感度・マウスの読込は変えない）。`ProfileGyroControlsLoadSection`（`GyroOutputMode` の値を保持用に取り込む。`DirectionalSwipe` は列挙に解けず、`None` になる）、`ProfileButtonControlLoadSection`（`Button`／`Macro`／`Key`／`Extras`／`KeyType` の各グループで、`GyroSwipe*` の項目を解釈せずに保持して飛ばす）。
- **保存**: `ProfileMotionSaveSection`（`GyroSwipeSettings` は保持した内容、なければ従来と同じ既定値の要素。`GyroOutputMode` は保持していれば `DirectionalSwipe` のまま）、`ProfileButtonSaveSection`（保持した項目を元のグループへ戻す。保存の順序は、元のグループの末尾になる）。
- **画面**: `ProfileEditor.xaml`（出力モードの選択肢と、スワイプのタブを削除。残りの選択肢は 0〜3）、`ProfileEditor.xaml.cs`（スワイプ用のメソッド・配線）、`ProfileSettingsViewModel`（スワイプの設定・メニュー。`GyroOutModeIndex` の添字の詰め直し。選び直したら保持した廃止されたモードを破棄）、`MappingListViewModel`、`ControlIndexCheck`。警告のダイアログは W3。
- **ゴールデン**: `profile-gap.xml`、`profile-varied.xml`、`profile-gap.resaved.xml`、`profile-legacy.resaved.xml` から `GyroSwipe*` の割り当て 8 行を削除し、`<GyroSwipeSettings>` を既定値に。状態のゴールデン 3 つから、スワイプの項目と `gyroSwipeInfo` を削除し、`ds4settings` の添字 48（Capture）を 44 に繰り上げ。`profile-default.xml`、`profile-minimal.rewritten.xml` は変更なし。
- **テスト**: 新規 `Phase8Step8W2RemovedGyroSwipeWiringTests`（設定・割り当て・モードの保持と書き戻し、削除、`ResetProfile`、メンバーがないこと、ソースのガード）。更新: `Phase8Step4bW2LoadFoundationTests`（節の名前）、`Phase8Step78bW2ControlsKeyTests`（スワイプの行を削除）。
- **残す文言**: `Strings` の `DirectionalSwipe`（翻訳 4 言語）は未使用になるが、W3 の文言と一緒に整理する。

### 5.8 8-0 W3 の実装（2026-10-20。ビルド・テスト成功。8-0 の W4［文書］も完了）

- 新設: `MessageBoxRemovedGyroSwipePrompt`（`DS4Forms/`。`IRemovedGyroSwipePrompt` の画面の実装。「はい」＝設定を削除、「いいえ」［またはダイアログを閉じる］＝残す。選択は記憶しない。Shift の `MessageBoxRemovedShiftPrompt` と同じ作り）。
- 配線: `ProfileEditor.Reload`（`profileRepository.LoadProfile` の直後、Shift の警告に続けて `RemovedGyroSwipeNotice.Apply(…)`。プロファイルがあるときだけ）。Shift とジャイロスワイプの両方が残っているプロファイルでは、警告が 2 回続けて出る。
- 文言: `Resources.resx`（英語）、`Resources.ja.resx`（日本語）に `RemovedGyroSwipeWarning`（`{0}`＝プロファイル名。スワイプの割り当て・設定・出力モード「ディレクショナルスワイプ」が含まれることを書く）、`Resources.Designer.cs` に参照用のプロパティ。ほかの言語は英語にフォールバック。
- テスト: `Phase8Step8W3RemovedGyroSwipeNoticeWiringTests`（サービスが保持の入れ物を返す、文言［英語・日本語］、`Reload` の読込の直後に警告を呼ぶ［1 か所］）。ダイアログそのものは、Step9 の実機（画面）の確認（U-48）。
- 残る未使用の文言: `Strings` の `DirectionalSwipe`（翻訳 4 言語）。W4 で削除するか判断する。

### 5.10 8a の実装（2026-10-20。ビルド・テスト成功）

- **削除（`Mapping.cs`。7,401 行 → 約 6,450 行。8a だけで約 950 行減）**: `ProcessControlSettingAction` の旧経路の本体（Controls タブの Key／Macro／Button／Extras／Default の素通し。新経路が所有しない割り当ては、何もしない）、`customMapQueue`／`ControlToXInput` とその処理・切断時の消去・初期化、`GetBoolMapping`／`GetBoolActionMapping`／`GetXYAxisMapping`／`GetByteMapping`／`GetMouseWheelMapping`／`GetIsToggledOn`、`FindSpecialActionForLogicalKey`、合成キーの SA のキャッシュ（`GetOrCreateSyntheticKeyAction`／`ClearSyntheticActionCache`）、`IfAxisIsNotModified`（未使用）、旧経路だけの状態（`stickWheel*`、`held`、`extrasRumbleActive`）。
- **残したもの**: 判定の `getBoolSpecialActionMapping`（SA の旧枝が使う。8b で削除）、`getMouseMapping`（Q5＝A。新経路が借りる）、`GetBoolMappingExternal`（`IMapping` のメンバー。`RecordBox` が使う）、`oldnow`／`pressagain`（`Commit` が使う）、`PlayMacro`／`EndMacro`／`DispatchOrSetBeingTriggered`（SA の旧枝。8b）。
- **テスト**: `ControlsKeyTestScope` は、引数にかかわらず新経路（`migrated` の引数は 8d で整理）。`MappingCharacterizationScope.RemapToButton` は、割り当てを変えたら束縛を組み立て直して登録する。識別用の SA は `SyntheticKeyActionFactory`（テスト用）で作る。削除: 旧関数を reflection で比べるテスト 3 つ（`TriggerInputTests`、`Phase8Step78bW2ControlsKeyTests`、`Phase8Step78fW2ControlsPadTests`。ゴールデンが置き換える）、旧経路だけの記録のテスト（Toggle の毎フレーム反転 2 つ、ホイールの共有状態）、合成 SA のキャッシュのテスト、`LegacyFunctionGolden`（旧関数の表の生成）。ロールバックのテスト 6 つは、「区分を外すと所有しない・束縛を作らない」だけを確かめる形に改めた（旧経路が処理する部分を削除）。ガード（`MappingHelperMethods…`、`MappingControlPath…`、`Phase8Step1SlotLimitTests`）の対象の一覧から、削除したメンバーを除いた。
- **ロールバック**: この PR 以降、Controls タブの旧経路はない。戻す手段は git のみ（Q1＝A）。
- **ビルド・テストでの修正**: (1) 削除スクリプトが `HandleDeviceDisconnect` の「マクロのタスクキューの消去」と「`deviceRuntime` の作り直し」まで消していたため復元（`customMapQueue` の消去だけを削除）。(2) `Mapping.cs` の閉じ括弧の重複。(3) 旧経路専用の `migrated: false` のケースを外した（`ADcsWithExtras_IsNotOwnedByTheNewPath`、`SharesTheStateWithAKeySpecialActionOfTheSameKey`、Macro の Play Once、Extras の同時押し・適用後の残り）。(4) 9 個未満の Extras は「何もしない」に期待値を変更（Q2 の仕様の固定）。(5) キー種別を外すと束縛を作らないテストは、オプションから `Key` を外す形に変更。(6) 較正ガードの終端マーカー。(7) テストの後始末（`MappingCharacterizationScope` は束縛を、`ControlsKeyTestScope` は Toggle の状態を、Dispose で消す）。

### 5.11 8b の実装（2026-10-20。ビルド・テスト待ち）

- **ユーザーの判断（2026-10-20）**: (1) Q7 の診断ログの削除一覧（`Phase8-Step8-Q7-Legacy-Log-List.md`。L1〜L19、M1〜M7。L18・L19 の GUI ログの削除を含む）を承認。4601（`MapCustomAction exception`）は、メソッドが残るため残した。(2) **解除トリガー（uTrigger）のボタンによる Profile の復帰は、機能廃止**（規約 2.2。UI から作れず、旧データにだけ残る。B 案）。切替は新経路が行い、復帰は自動（AutomaticUntrigger）のときだけ。Actions.xml の uTrigger の値は、読み込みも保存もそのまま（通知なし。UI で作れず、通知の対象が実質いないため）。
- **削除（`Mapping.cs`。6,463 行 → 4,632 行。8b だけで約 1,830 行減。8a と合わせて 7,322 → 4,632 行［約 2,690 行減］）**: `MapCustomAction` の SA の種別ごとの旧枝（トリガーの判定、Key／Macro／Button／Program／Profile／DisconnectBT／BatteryCheck／ステアリング較正／GyroCalibrate／MultiAction、押し続けの解除、uTrigger の復帰）。メソッドは、エンジンへの引き渡しだけの同期の `void MapCustomAction(int device, ControlService ctrl)` に縮めた（以前の `async void` は、初期化待ちが完了済みで、実際の `await` はタップ判定の遅延だけだったため、同期にしても動作は変わらない）。Stage3（Button SA の毎フレームの出力）、`LogSpecialActionTrace`、`GetBeingTriggered`／`SetBeingTriggered`／`SetBeingTriggeredIf`／`DispatchOrSetBeingTriggered`／`DispatchInputEdge`／`MakePrevKey`／`prevInputEstablished`、`TryDispatchSATriggerEstablished`／`TryDispatchSATriggerReleased`、`LogActionDoneCountOnTrigger`、`EnsureActionDoneInitialized`、`ReleaseActionKeys`、`IsSpecialActionTriggered`、`getBoolSpecialActionMapping`、`ShouldClearToggledOn`、`ToggleReleaseHoldMs`、`lastButtonTriggerState`、`mappingEvalSinceLast`／`setBeingTriggeredCallsSinceLast`／`mappingLastSummaryUtcTicks`（`Mapping SUMMARY (5s)` のログごと）、`oldnowKeyAct`。
- **残したもの**: `IsUsingGyroForControls`（`GetBoolMappingExternal` が使う）、`PlayMacro` 系（新経路の `PlayMacroDirect`／`PlayMacroString` が使う）、`CaptureProfileUntrigger`／`RestoreProfileUntrigger`（`IMapping` のメンバー。合図は常に空になる。8d で整理）、`DeviceRuntimeState.UntriggerAction`／`UntriggerIndex`（同上）。
- **ビルダー**: `ActionBindingBuilder` の Profile は、uTrigger を持っていても束縛を作る（以前は `null`）。`UntriggerPolicy.Buttons`／`BindingPolicy.UntriggerButtons` は使う者がなくなった（8d で削除。TODO を付けた）。
- **束縛を持たない SA**: 何もしない（Q2）。例: 種別を `MigratedTypes` から外した場合、ポートが作れず束縛を作れない場合。
- **テスト**: `SpecialActionTestScope` は、引数にかかわらず新経路（`ControlsKeyTestScope` と同じ。引数は 8d で整理）。削除: `MappingLogActionDoneCountOnTriggerTests`、`Phase8Step4W4WiringTests.NothingMigrated_TheLegacyLoopRuns_AsBefore`、`Phase8Step75bW1…` の `Legacy_*` 2 件、`Phase8Step76cBatteryCheckTests` の旧枝の検査、`Step10FourTwoGlobalReferenceGuardTests.Mapping_FreshPressGuard_CoversAllSpecialActionTypes`、`MappingControlPathGlobalReferenceGuardTests` の `ReleaseActionKeys`。書き換え: ロールバックのテスト（`…_NoBindingIsBuilt_AndNothingHappens`。束縛がなければ何もしない）、uTrigger の 3 件（切替はして、復帰しない）、`Phase8Step1ProfileSwitchCharacterizationTests`（合図はエンジンが持つ）、`ButtonSpecialActionOutputTests`（旧枝・Stage3 がないことのガード）、較正・SlotLimit などのガードのマーカー。旧経路専用の `migrated: false` のケースは外した（Phase7cW2、Phase8Step1Macro）。
- **ログ（規約 2.3）**: 画面に出るログが変わるのは L18・L19 の 2 件だけ（旧経路が消えれば起きない状況のログ）。新経路が同じ文言で出すものは、変更なし。
- **ロールバック**: git のみ（Q1＝A）。

### 5.5 テストへの影響

- 旧・新の両経路の Theory は `InlineData(false)` を持つファイルが 15、`migrated` 引数を取るテストが 36 ファイル、`MigratedTypes`／`MigratedControlKinds` を触るテストが 28 ファイル。
- 旧関数を reflection で呼んで比べているテスト: `TriggerInputTests`、`Phase8Step78bW2ControlsKeyTests`、`Phase8Step78fW2ControlsPadTests`。W1 で、旧関数の値の表（乱数の入力と期待値）をテストデータに固定してから、置き換える。
- ジャイロスワイプ関連のテストは、廃止（8-0）で削除・更新する（W1 で一覧にする）。

### 5.9 W1 の実装（2026-10-20。ゴールデンを生成済み［Controls・Passthru の 2 ファイル。各 60 行 × 52 項目、例外 0 件、2 つの表は異なる］。コミット後の再実行で、旧関数との照合と、新経路との一致の両方が通った。ビルド・テスト成功）

- `LegacyFunctionTable`（テスト用。旧関数 `getBoolSpecialActionMapping`／`GetBoolActionMapping`／`GetBoolMapping`／`GetByteMapping`／`GetXYAxisMapping` を reflection で呼び、決定的な入力［種 80801、60 回］で、全コントロールの戻り値を表にする。1 項目 ＝ 判定(SA)・判定(Controls)・`GetBoolMapping`・バイト値・軸(alt=false)・軸(alt=true)。例外は x）と、`Phase8Step8W1LegacyFunctionGoldenTests`。
- (1) `LegacyFunctionGolden`（Controls／Passthru）: 旧関数の表を、`Golden/Phase7Step10c/step8-legacy-functions-{mode}.txt` と照合。**初回はファイルを生成して失敗する。内容を確認してコミットし、再実行する**（この Step の W1 は、旧関数がまだある今のうちに生成すること）。
- (2) `NewFunctionsMatchTheGolden`: 新経路（`TriggerInput`）の表が、ゴールデンの該当の列と一致する（`GetBoolMapping` の列は新経路にないため除く。Q3＝A で SA の判定に揃えた）。旧関数を削除した後は、これが「旧と同じ値」の根拠になる。
- 8a・8b で、旧関数を reflection で呼ぶ既存のテスト（`TriggerInputTests`、`Phase8Step78bW2ControlsKeyTests`、`Phase8Step78fW2ControlsPadTests` の比べ）は、旧関数の削除と同時に削除する（このゴールデンが置き換える）。
- 新経路に入っていない割り当ての特性化（§2.3）は、Q2＝C により、ジャイロスワイプを 8-0 で廃止したため、残りは「値が読めない割り当て」のみ。これは 8a の W で、「束縛を作らない＝何もしない」を固定するテストを足す。

## 6. 質問（承認待ち）

- **Q1（実機確認との順序）**: A（推奨）: 削除を 8a〜8d の小さな PR に分けて先に行い、実機確認（Step9。U-35〜U-47 をまとめて）を最後に行う。不具合が見つかったら、PR 単位で git で戻す。Step7 の PR は、すべてビルド・テスト成功済みで、旧・新の比べで同じ出力を確かめている。B: 削除の前に、実機確認の主要項目（U-27〜U-47 のうち、押す・離す・切断・プロファイル切替を含む短い版）を行い、合格してから削除する。ロールバックの 1 行が使えるうちに実機で確かめられるため安全だが、実機確認を 2 回行うことになる。
- **Q2（新経路に入っていない割り当て。§2.3）【承認: C。ユーザー、2026-10-20】**: ジャイロスワイプの機能を廃止する（規約 2.2 の機能廃止。ユーザーの明示的な承認）。値が読めないなど、旧経路でも実質何もしないものは、「束縛を作らない＝何もしない」を仕様として固定する（テストで確かめる）。（却下した案: A 新経路に入れる［小さな 7.8g］、B 旧経路のまま残す）
- **Q3（スイッチの扱い）**: A（推奨）: 8d で `BindingMigrationOptions` を削除し、エンジンは束縛があれば常に処理する。ビルダーも常に全種別を組み立てる。ロールバックの手段は git のみ。B: スイッチを残す（常に全部有効の既定で。テスト用の差し込み口として）。コードが減らず、「旧経路が消えたのにスイッチがある」状態になる。
- **Q4（旧・新の両経路のテスト）**: A（推奨）: 両経路を走らせる Theory は、新経路だけにして（`migrated` の引数を消す）、期待値は旧経路で固定したものをそのまま使う。旧関数を reflection で比べるテストは、W1 の表との比べに置き換える。B: 旧・新の比べのテストは、削除と同時にすべて削除する（期待値の固定が失われる）。
- **Q5（`getMouseMapping`）**: A（推奨）: `Mapping` に残す（新経路が `MouseSampler` で借りている。マウス変換は Phase9 で `Mapping` から分ける）。B: 8d で `TriggerInput` 側（または新しいクラス）へ移す。感度・加速・デッドゾーンの計算を動かすため、特性化テストがまだ薄い現状ではリスクが高い。
- **Q6（系統 B の範囲）**: A（推奨）: W0 で呼び出し元がなくなった型だけを削除し、残るもの（`KeyAction`、`KeyButtonActionController` が新経路から使われていれば）は残して、残す理由を TODO ではなく通常のコメントにする。B: 系統 B を残す方針で、削除は `IActionFactory` 系のみに限る。
- **Q7（旧経路の診断ログの削除。規約 2.3）**: A（推奨）: 旧機構の動作を説明するだけの診断ログ（`Mapping SUMMARY (5s)`、`setBeingTriggeredCalls`、「press delegated to KeyButtonActionController」など）は、コードと一緒に削除する。新経路が同じ事象を出すログ（Key の押す・離す、SA の立ち上がり・立ち下がりなど）は 7.x で同じ文言に揃えてあり、変えない。削除する一覧は W0 で作り、承認を得てから消す。B: すべてのログを新経路に移す（旧機構にしか意味のないログも）。
- **Q9（ジャイロスワイプ廃止の範囲とデータの扱い）【承認: A。ユーザー、2026-10-20。条件: 通知のときに、Shift の廃止と同じく、利用者が「設定を残す」か「設定を削除する」かを選べること】**: 廃止の範囲は、dcs の割り当て（`GyroSwipe*`）だけか、機能全体（設定の `GyroSwipeInfo`、編集画面の項目、`Mouse.cs` の処理、プロファイルの読み書き）か。A（推奨）: 機能全体を廃止する（dcs だけ廃止して設定を残すと、効かない設定が残るため）。保存済みのプロファイルの該当部分は、Shift の廃止（7.8d）と同じく、読み込んで解釈せず、そのまま保存し直す（内容を失わない）。利用者がその内容を持つプロファイルを開いたときは、廃止の通知を出す（文言は W0 で作り、承認を得る）。B: 機能全体を廃止し、保存済みの該当部分は読み込み時に捨てる（通知なし）。保存し直すと内容が失われる。C: dcs の割り当て（`GyroSwipe*`）だけ廃止する。設定と編集画面は残す。
- **Q8（`Mapping.cs` の減少の確認）**: A（推奨）: 各 PR で行数の変化を記録し、全体で約 2,000 行の減少を目標にする（目標未満でも、残す理由を記録する）。B: 行数は目標にしない。

## 7. 完了条件

- [x] Q1〜Q9 の承認（2026-10-20）。[x] W0 の結果の追記（§5。2026-10-20）。[x] 8-0（ジャイロスワイプの廃止）の W1〜W4 完了（2026-10-20。`Phase8-Step8-Completion-Report.md`）。
- [ ] 8a〜8d のすべてでビルド・テストが成功し、固定した期待値のテストが通る。
- [ ] `Mapping.cs` に旧ディスパッチ（§2.1 の A〜D）が残っていない。系統 B の削除対象が残っていない（残すものは理由を記録）。
- [ ] スイッチの扱いが Q3 のとおり。ラチェットの基準値が実数と一致。
- [ ] 完了報告書、`Phase8-Status.md`、モデル図、規約の記述を更新。
- [ ] 実機確認は Step9（Q1 のとおり）。

## 8. 次

- W0 の結果（§5）をユーザーが確認 → 8-0（ジャイロスワイプ廃止）の W1 → 結果を §5 に追記して確認 → W1 → 8a〜8d。
