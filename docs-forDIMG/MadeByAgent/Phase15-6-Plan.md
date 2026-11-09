# Phase15-Step6 計画書: 設定サービスの分割（R-10）と ProfileSettingsViewModel／ProfileEditor の領域分割（G-1・F-4）

作成日: 2026-11-09
状態: **承認済み（2026-11-09）。15-6a〜15-6h 完了**
上位: `Phase15-Plan.md` §5 の 15-6、P8＝C（全領域を切り出す）、P9＝B（実機確認は Phase15 の最後に 1 回）

## 1. 目的
- `IProfileSettingsService` の「スロット番号の配列を返すプロパティ 90 個」を、領域ごとの小さな型へ分け、利用者が必要な領域だけに依存できるようにする（R-10）。
- `ProfileSettingsViewModel`（約 3,300 行）を領域ごとのサブ ViewModel に分け、`ProfileEditor`（xaml 4,130 行／cs 2,270 行）の該当部分を領域ごとの部品に分ける（G-1・F-4）。
- 見た目・保存内容・操作は変えない。

## 2. 設計（Strangler Fig。古い入口は最後まで残す）

### 2.1 設定サービス側（Core）
- `IProfileSettingsService` に `ProfileSlotSettings ForSlot(int slot)` を足す。
- `ProfileSlotSettings`（`Core/Settings`）は、領域ごとのプロパティ `Gyro`、`Touchpad`、`Triggers`、`Sticks`、`Lightbar`、`Output`、`Misc` を持つ。
- 領域の型（例: `GyroSlotSettings`）は、スロット番号を 1 つ持ち、**既存の配列・メソッドへ委譲する薄い窓口**にする。値は今までの場所（`BackingStore`）に残る。つまり保存・読込の経路は変わらない。
- 既存の配列プロパティは、全領域の移行が終わる最後の Step（15-6-Z）まで消さない。

### 2.2 ViewModel 側（UI）
- 領域ごとに `ObservableObject` 派生のサブ ViewModel（例: `GyroSettingsViewModel`）を作る。そのスロットの領域の窓口（2.1）だけを受け取る。
- `ProfileSettingsViewModel` は `Gyro` などの読み取り専用プロパティでサブ VM を公開する。古いプロパティは、画面の移行が終わるまで残し、最後に消す。
- 生成は `IViewModelFactory`（15-5 で集めたもの）を通す。

### 2.3 画面側（F-4）
- 領域ごとの `UserControl`（例: `GyroSettingsPanel`）へ、`ProfileEditor.xaml` の該当タブの中身とコードビハインドの該当ハンドラーを移す。`DataContext` はサブ VM。
- `ProfileEditor` は、タブの枠と、領域をまたぐ処理（保存・読込・Undo・試験表示）だけを持つ。

## 3. Step の分け方（1 領域＝1 サブ Step。それぞれ Debug ALL と起動確認、コミット）

| Step | 内容 | 規模の目安 |
|---|---|---|
| 15-6a | **ジャイロ**（ForSlot の土台＋`GyroSlotSettings`＋`GyroSettingsViewModel`＋`GyroSettingsPanel`。Binding 約 76 行、コードビハインド約 33 行） | 大（土台を含む） | ← **完了**（2026-11-09。`IProfileSettingsService.ForSlot(int)`、`ProfileSlotSettings`、`GyroSlotSettings`［Core/Settings］、`GyroSettingsViewModel`［UI/ViewModels。Binding 名は元のまま］、`GyroSettingsPanel`［UI。ジャイロのタブの中身。編集画面とはイベントで連携］。`ProfileSettingsViewModel` は `Gyro` を公開し、DS3 の傾きの擬似の窓口だけを転送。テスト `GyroSettingsViewModelTests`。XAML の StaticResource は読み込み時に解決されるため、パネルを実際に読み込むテストを足した）
| 15-6b | タッチパッド | 中 | ← **完了**（2026-11-09。`TouchpadSlotSettings`［Core/Settings。`ProfileSlotSettings.Touchpad`］、`TouchpadSettingsViewModel`［UI/ViewModels。Binding 名は元のまま。`TouchDisInvertString` と反転トリガーのメニューの更新・反映を含む］、`TouchpadSettingsPanel`［UI。タッチパッドのタブの中身。「ボタンの有効化方式」の部品の置き場所の選択、スワイプの割り当ての表示、反転トリガーのメニューを持ち、スワイプの入口は `SwipeControlClicked` イベントで編集画面へ知らせる］。`ProfileSettingsViewModel` は `Touchpad` を公開。出力モードの変更は `Touchpad.PropertyChanged` で受ける。テスト `TouchpadSettingsViewModelTests`）
| 15-6c | トリガー | 中 | ← **完了**（2026-11-09。トリガーは独立したタブではなく、右ペインの Axis Config タブの「L2 & R2」の GroupBox。`TriggerSlotSettings`［Core/Settings。`ProfileSlotSettings.Triggers`］、`TriggerSettingsViewModel`［UI/ViewModels。Binding 名は元のまま］、`TriggerSettingsPanel`［UI。`DataContext="{Binding Triggers}"` で Axis Config の中に置く。カスタム曲線の編集は `CustomCurveEditorRequested` イベント、Full Pull の割り当て表示は `AssignFullPullAssociation`］。L2/R2 のデッドゾーン変更の試験表示への反映は `Triggers.PropertyChanged` で受ける。**パネルの XAML 根には lex:… の 3 属性を付けない**［付けると、WPFLocalizeExtension が既存の全リスナーへ通知し、別スレッドで作られた古い部品の Dispatcher を待って、テストの全体実行が止まる。実行時は編集画面の根から継承される］。パネルのテストは共有の STA スレッド `WpfPanelTestRunner` で実行）
| 15-6d | スティック（軸別デッドゾーン、AxialStick を含む） | 大 | ← **完了**（2026-11-09。Axis Config タブの LS・RS の 2 つの GroupBox を 1 つのパネルにした。`StickSlotSettings`［Core/Settings。`ProfileSlotSettings.Sticks`。ドリフト補正の 4 項目を含む］、`StickSettingsViewModel`［UI/ViewModels。LS*／RS*／ドリフト。Binding 名は元のまま］、`StickSettingsPanel`［UI。`DataContext="{Binding Sticks}"`。`AxialLeft`／`AxialRight` で軸別デッドゾーンの部品を編集画面へ公開。曲線の編集と校正はイベント、Outer Bind の割り当て表示は `AssignOuterBindAssociation`］。デッドゾーン・ドリフトの試験表示への反映と、出力モードの変更によるプロファイルの印の更新は `Sticks.PropertyChanged` で受ける。`StickCalibrationWindow` のドリフト書き込みは `Sticks` 経由。テスト `StickSettingsViewModelTests`）
| 15-6e | ライトバー | 中 | ← **完了**（2026-11-09。`LightbarSlotSettings`［Core/Settings。`ProfileSlotSettings.Lightbar`。`Info` で設定の中身を公開］、`LightbarSettingsViewModel`［UI/ViewModels。色・点滅・充電・虹色、ライトバーの表示ブラシ、色選択中の実機への強制表示（`StartForcedColor` ほか。実機＝targetDevice に対して行う）。虹色の画像の読み込みと、色の成分の変更に伴う再通知もここ］、`LightbarSettingsPanel`［UI。タブの中身。色の選択画面の開閉と強制表示、虹色の切り替え、「バッテリー残量で色を変える」の枠の表示を持つ］。左ペインの図のライトバーの四角（`lightbarRect`）と、図の上のライトバーのボタンは、`Lightbar` を使う。テスト `LightbarSettingsViewModelTests`）
| 15-6f | 出力（仮想コントローラー、振動） | 中 | ← **完了**（2026-11-09。「その他」タブの「仮想コントローラー」と「振動」の 2 つの GroupBox を 1 つのパネルにした。`OutputSlotSettings`［Core/Settings。`ProfileSlotSettings.Output`。出力の種類、DInput のみ、DS4 のトリガー出力、振動の強さ・自動停止、DualSense の振動、モーターの反転］、`OutputSettingsViewModel`［UI/ViewModels。Binding 名は元のまま。画面で選んだ種類 `TempControllerIndex` は、保存される値と出力ピッカー用の一時の種類 `OutDevTypeTemp` へ書く。`RefreshFromSettings`／`SyncOutDevTypeTempFromSettings`／`ResetOutDevTypeTemp`］、`OutputSettingsPanel`［UI。`DataContext="{Binding Output}"`。種類の選択 `OutConTypeCombo` と、振動のテストのボタン `HeavyRumbleTestBtn`／`LightRumbleTestBtn`、`RumbleTestClick` イベントを公開。実機を動かす振動のテストとマッピング一覧の更新は `ProfileEditor` が持つ］。`CorrectOutputTypeToDs4` は親に残し `Output` を使う。**マウス（ButtonMouse*、OutputMouseSpeed など）は 15-6g へ回した**［「その他」タブの一般項目と一緒に扱うため］。Core が `DualSenseDevice.RumbleEmulationMode`（第1層の入れ子の型）を参照する記録のみの規則 L2 が 1 件増えた［29→30。列挙型を Core/Model へ移せば消える。TODO コメントあり］。テスト `OutputSettingsViewModelTests`）
| 15-6g | 残り（一般・その他） | 中 | ← **完了**（2026-11-09。「その他」タブの残りの項目を 1 つのパネルにした。`GeneralSlotSettings`［Core/Settings。`ProfileSlotSettings.General`。ボタンによるマウス移動、入力の表示、タッチパッドの切り替え、DS4 への出力データ、同時起動するプログラム、無操作での切断、Bluetooth のポーリング間隔］、`GeneralSettingsViewModel`［UI/ViewModels。マウス速度の表示値の連動もここ。`UpdateLaunchProgram`／`ResetLauchProgram`／`RefreshFromSettings`］、`GeneralSettingsPanel`［UI。`DataContext="{Binding General}"`。`UseControllerUD` を公開し、`UseControllerReadoutClick`／`LaunchProgramBrowseClick` で知らせる］。親の `OnOwnPropertyChanged`（マウスの連動だけだった）は不要になり削除。**残り**: 6 軸（SX／SZ）の項目と、`ProfileEditor` の整理［`Reload`、SA 一覧の見出し・並べ替え］は 15-6Z の前に扱う。テスト `GeneralSettingsViewModelTests`）
| 15-6h | 6 軸（SX／SZ）と `ProfileEditor` の整理 | 小 | ← **完了**（2026-11-09。`SixAxisSlotSettings`［`ProfileSlotSettings.SixAxis`］、`SixAxisSettingsViewModel`、`SixAxisSettingsPanel`［`DataContext="{Binding SixAxis}"`。`CustomCurveEditorRequested` で "SX"／"SZ" を知らせる］。デッドゾーンの試験表示への反映は `SixAxis.PropertyChanged` で受ける。`ProfileEditor` は、各タブの DataContext の解除・付け直しを `ClearEditorDataContexts`／`AssignEditorDataContexts` に集約［`Reload`・`StopEditorBindings`・`RefreshEditorBindings` の重複を解消］。**特殊アクション一覧の見出し・並べ替え（`UpdateSpecialActionsHeaderTexts` ほか）は、表示の生成のタイミングに依存し、実機での見え方の確認が要るため、設定の分割とは別の整理として 15-7 以降へ回した**。テスト `SixAxisSettingsViewModelTests`）
| 15-6Z | 配列プロパティ・古いメソッドの削除、ラチェットの更新、設計図・規約の更新 | 中 |

各サブ Step の終わりに、そのタブの Binding 名の一覧を機械的に突き合わせる検査（XAML の Binding パスが、DataContext の型に存在することの検査）を足す。Binding の誤りはビルドで見つからないため、これを必須の守りにする。

## 4. 守り（テスト）
- 新規: `XamlBindingPathTests`（`ProfileEditor` とその部品の XAML の `{Binding Path}` が、DataContext の型の公開プロパティに存在する）。
- 既存の `ProfileSettingsViewModel*Tests`、`ProfileEditor*Tests`、ProfileEditor 系のソース検査は、サブ Step ごとに通す。
- 領域の窓口は、既存の配列と同じ値を読み書きすることを、領域ごとに単体テストで確かめる。

## 5. リスクと緩和
- Binding の抜け（値の表示・保存・Undo の取りこぼし）→ 上記の検査、領域ごとの小さなコミット。
- 実機確認は最後の 1 回（P9＝B）のため、戻しが難しくなる → 1 領域＝1 コミットにして、問題の領域だけ戻せるようにする。

## 6. 決めること（承認時に確認）
1. 領域の分け方（`Gyro`／`Touchpad`／`Triggers`／`Sticks`／`Lightbar`／`Output`／`Misc`）でよいか。
2. 画面の部品は `UserControl` に分ける形でよいか。
3. 最初の Step（15-6a）を、ジャイロ（土台を含む）としてよいか。
