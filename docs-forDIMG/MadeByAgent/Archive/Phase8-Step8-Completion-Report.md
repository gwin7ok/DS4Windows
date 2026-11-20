# Phase8-Step8 完了報告書

作成日: 2026-10-20
計画書: `Phase8-Step8-Plan.md`
状態: **8-0・8a・8b・8c・8d すべて完了（ビルド・テスト成功。実機確認は Step9 で合格）。Phase8 全体は `Phase8-Completion-Report.md`。**

---

## 8-0 ジャイロスワイプの廃止（2026-10-20。W0〜W4 完了。ビルド・テスト成功。実機確認は Step9 の U-48）

計画書: `Phase8-Step8-Plan.md` §5.1・§5.6〜§5.8（Q2＝C［機能廃止。規約 2.2 のユーザーの明示的な承認］、Q9＝A［機能全体を廃止。通知で「残す／削除」を選べる］）。

- **範囲**: 割り当て（`DS4Controls.GyroSwipeLeft／Right／Up／Down`）、設定（`GyroDirectionalSwipeInfo`、プロファイルの `<GyroSwipeSettings>`）、ジャイロの出力モード `GyroOutMode.DirectionalSwipe`（W2 の調査で判明。編集画面の選択肢にもあった）、処理（`Mouse` の判定、`Mapping` のスワイプの処理）、編集画面（出力モードの選択肢、スワイプのタブ）。
- **利用者のデータの扱い（Q9＝A）**: 保存済みのプロファイルのスワイプの内容は、読み込んで解釈せず、そのまま保持し（`RemovedGyroSwipeSettings`）、保存で書き戻す。編集画面でそのプロファイルを開いたとき、警告を出し、利用者が「削除」（設定を捨てる）か「残す」（動作はしない。開くたびに警告）を選べる。保持するのは、(1) 既定値と異なる `<GyroSwipeSettings>`、(2) `<Control>` 内の `GyroSwipe*` の割り当て、(3) 出力モード `DirectionalSwipe`。出力モードを編集画面で選び直したら、保持した廃止されたモードは破棄する。
- **保存形式**: 設定のないときは、廃止前と同じ既定値の `<GyroSwipeSettings>` を書く（形式は変わらない）。保持した割り当ての項目は、元のグループ（`Button`／`Macro`／`Key`／`Extras`／`KeyType`）の末尾へ戻る（項目の順序だけが変わりうる）。
- **W1**: 保持・警告の部品（`RemovedGyroSwipeSettings`／`RemovedGyroSwipeChoice`／`RemovedGyroSwipeOutcome`／`IRemovedGyroSwipePrompt`／`RemovedGyroSwipeNotice`）と仕様のテスト。Shift の型と並べて新設した（共通化は Phase9 の整理で扱う）。
- **W2**: データモデル・実行・読込・保存・画面の項目の削除。`ProfileGyroSwipeLoadSection` を `ProfileGyroMouseLoadSection` に改名（同じ節のジャイロの感度・マウスの読込は変えない）。列挙の値が減ったため、添字で引く表の同じ行を削除（保存されている番号は `ds4ControlMapping` の値で、変更なし）。ゴールデンから該当の行を削除。
- **W3**: 警告ダイアログ（`MessageBoxRemovedGyroSwipePrompt`）、`ProfileEditor.Reload` の配線、文言（英語・日本語）。
- **承認済みの機能廃止（規約 2.2）**: ジャイロスワイプ（4 方向のスワイプの割り当て、スワイプの設定、出力モード）。
- **テスト**: `Phase8Step8W1RemovedGyroSwipeSpecTests`、`Phase8Step8W2RemovedGyroSwipeWiringTests`、`Phase8Step8W3RemovedGyroSwipeNoticeWiringTests`。更新: ゴールデン 7 ファイル、`Phase8Step4bW2LoadFoundationTests`、`Phase8Step78bW2ControlsKeyTests`、`ProfileEditorTargetDeviceGuardTests`（`CreateBindingWindow(` の呼び出し 7 → 6）。
- **残る文言**: `Strings` の `DirectionalSwipe`（翻訳 4 言語）は未使用。翻訳ファイルの整理は Phase9 で扱う。
- **実機確認（Step9。U-48）**: スワイプの設定を持つプロファイルを開いたときの警告（残す／削除）、「残す」で保存したファイルに内容が残ること、「削除」で消えること、出力モード「ディレクショナルスワイプ」のプロファイルの扱い、通常のジャイロ（Controls／Mouse／MouseJoystick／Passthru）が従来どおり動くこと、編集画面のジャイロの出力モードの選択が正しい項目を指すこと。

---

## 8a Controls タブの旧枝などの削除（2026-10-20。ビルド・テスト成功）

詳細: `Phase8-Step8-Plan.md` §5.10。`Mapping.cs` は 7,322 行 → 6,449 行。`ProcessControlSettingAction` の旧経路の本体、`customMapQueue`、判定の旧関数（`GetBoolMapping` ほか）、合成キーの SA のキャッシュなどを削除した。新経路が所有しない割り当て（9 個未満の Extras など）は、何もしない。

## 8b SA の Stage3 と MapCustomAction の旧枝などの削除（2026-10-20。ビルド・テスト成功）

詳細: `Phase8-Step8-Plan.md` §5.11、ログの一覧は `Phase8-Step8-Q7-Legacy-Log-List.md`。`Mapping.cs` は 6,449 → 4,632 行（8a と合わせて 7,322 → 4,632 行）。
- **承認済みの機能廃止（規約 2.2）**: Profile 切替の解除トリガー（uTrigger）のボタンによる復帰。UI から作れず（a7277da9 で削除）、旧データだけに残っていた。切替は行い、復帰は自動（AutomaticUntrigger）のときだけ。
- **ログ**: 診断ログの削除一覧（L1〜L19、M1〜M7）を承認のうえ削除。画面に出るログが変わるのは 2 件（`BeingTriggered initialization failed ...`、Stage3 の `Synchronous Button SA handling error`）だけ。
- **実機確認（Step9）**: 全種別の SA（Key／Macro／Button／Program／Profile［自動の復帰］／DisconnectBT／BatteryCheck／ステアリング較正／GyroCalibrate／MultiAction）が、従来どおり動くこと（U-35〜U-47 を含む）。

## 8c 系統 B の削除（2026-10-20。ビルド・テスト成功）

詳細: `Phase8-Step8-Plan.md` §5.12。`IActionFactory` 系（工場、アダプター 4 つ、`SpecialActionBase`、`Action`、`MappingContext`、`IKeyActionCreator` 系、`MouseOutputAction`、`TriggerContext`）の 14 ファイルを削除し、`DefaultActionManager`・`IManagedActionManager`・`ActionManager`・`ActionEntry` から Action 実体とディスパッチを外した。`KeyAction`／`KeyButtonActionController` ほか、新経路が使う型は残した。`ServiceProviderHolder` の参照は 17 → 10。実機確認への影響なし。

## 8d 移行スイッチなどの削除（2026-10-20。ビルド・テスト成功）

詳細: `Phase8-Step8-Plan.md` §5.13。`BindingMigrationOptions` と、そのコンストラクター引数、`UntriggerPolicy.Buttons`／`BindingPolicy.UntriggerButtons`、`Mapping` の未使用の定数 2 つを削除した。本番の既定は全種別・全区分が有効だったため、実行・ログへの影響はない。テストは、スイッチを触るロールバックのテストを削除し、スコープからスイッチの退避・復元を外した。`Capture`／`RestoreProfileUntrigger` と、テストの `migrated` 引数は、Phase9 の整理に残した。

## 持ち越し C-2: テストの `migrated` 引数の整理（2026-10-20。ビルド・テスト成功）

`Phase8-Completion-Report.md` §3 の C-2。小さな PR として、Phase9 の前に行った（コードの変更なし。テストのみ、43 ファイル）。

- **削除した引数**: `SpecialActionTestScope(bool migrated)`、`ControlsKeyTestScope(bool migrated)`、`AnalogMouseTestHarness(bool migrated, …)`、各ファイルの `Run`／`RunLogged`／`TwoKeys`／`Harness` などの `bool migrated`、`[Theory]` の `InlineData(false/true)`（83 メソッド。引数が 1 つだけのものは `[Fact]` にした）、`foreach (bool migrated in new[] { false, true })`（6 か所）、`Migrated` プロパティ。
- **旧・新の比べの扱い**: `Assert.Equal(legacy, migrated)` は、同じ経路どうしの比べで検査にならないため削除し、残りの検査（`Assert.Contains`／`Assert.Equal(期待値, …)`）は新経路の期待値として残した。比べだけが検査だったテストは、削除した（`Phase8Step73W1`／`74W1`／`75aW1` の `ADelay_BehavesTheSame`・`AProfileApplyWhileHeld_*`、`Phase8Step7W2ApplyAndOrderTests.ProfileApplyWhileReleased_*`）。旧・新の割り当ての比べだった `TheNewPath_DoesNotAddAllocationsToTheFrame_*`（72W3／73W3［2 件］／74W3／75aW3／75bW3／78bW2／78cW2／78eW2／78fW2）も削除した（`76a〜d`・`77b` の「ありとなし」の比べは、残した）。
- **変更なしの注意**: テスト名の「BothPaths」「OnEitherPath」は外した。実行・ログへの影響はない（テストのみ）。
- **ロールバック**: git のみ。
