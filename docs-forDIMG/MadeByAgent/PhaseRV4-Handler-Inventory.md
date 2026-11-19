# PhaseRV4-0: ハンドラーの棚卸しと分類（承認待ち）

作成日: 2026-11-20
対象: `DS4Windows/UI` の、PhaseRV3 で扱った 4 画面（MainWindow・About・AutoProfiles・AssignmentEditor）を除く全画面の XAML のイベント属性（実数 159 件）と、`BindingWindow.xaml.cs` の `Click +=` 147 件。
分類は RV3-0 と同じ。(a) 操作→コマンド、(b) 状態の更新→バインディング、(c) 見た目だけ→View に残す（許可リストに載せる）。
実数は、`ViewCommandBindingRatchetTests` の正規表現で、コメントを除いて数えた（同じハンドラーを複数の部品が使う場合は、部品ごとに 1 件）。

## 1. (c) の許可リスト案（承認をお願いするもの）

| 区分 | 内容 | 件数 | 理由 |
|---|---|---|---|
| C1 ウィンドウの閉じる・結果の確定 | `Close()`・`DialogResult`・`Closed` の後始末（`DataContext = null`）だけのハンドラー | 約 30 | ウィンドウそのものの操作で、ViewModel の状態ではない。RV3 の `MainDS4Window_Closed` と同じ扱い |
| C2 コンテキストメニューを開く | ボタンを押すとメニューを開く（`ContextMenu.IsOpen = true`） | 4（Gyro 3、Touchpad 1） | RV3 の `AddProgramsBtn_Click` と同じ |
| C3 マウス・ドラッグ・入力の捕捉 | `RecordBox` のドラッグ並べ替え（5＋DragOver・Drop）、記録中の生のキー・マウスの捕捉（4）、`WindowTargetEditor` の照準（2）、一覧の列幅の保存（1）、ホバーの説明（2）、`LostFocus`（1） | 約 17 | 座標・フォーカス・生の入力で、ViewModel へ移せない |
| C4 色選択の窓を出す | `ColorPickerWindow`（WPF の窓）を出し、色の変更を実機へ送る | 7（BindingWindow 1、ProfileEditor 1、Lightbar 4、ほか） | WPF の窓の生成が要る。`IColorPicker` の契約を作れば (a) にできるが、RV4 では見送る（D は §4） |

## 2. 画面ごとの分類

| 画面 | 件数 | (a)(b) の扱い | (c) |
|---|---|---|---|
| BindingWindow（出力ピッカー） | XAML 6＋`+=` 147 | `+=` 147（`associatedBindings` 辞書）→ XAML の `Command`＋`CommandParameter`（D1＝B）。Default・Unbound・TestRumble・RecordMacro → コマンド | Closing（`WriteBinds`。C1）、色（C4） |
| ProfileEditor | 約 21 | Save・Apply・Cancel・Preset・ResetAll・WindowSizeReset・KeepSize・SpecialAction の新規／編集／複製／削除／書き出し／チェック・ヘッダー・ダブルクリック 2 → コマンド。`SidebarTabControl_SelectionChanged` → バインディング | ホバー 2（C3）、色（C4） |
| RecordBox | 22 | Record・Save・Cancel・AddMouseMove・AddWaitTime・ClearSteps・LoadPreset・SavePreset・Four/FiveMouse・AddRumble・ChangeLight・Enter キー 3・ダブルクリック → コマンド | 記録中の捕捉 4、ドラッグ 5＋2、LostFocus 1、`PointerModeCb_SelectionChanged`（`Handled = true` だけ。削除できるか RV4-5 で確認） |
| TouchpadSettingsPanel | 32 | `TouchDisInvertMenuItem_Click` 28、`SwipeControlsButton_Click` 4 → `ItemsControl`＋コマンド | `TouchDisInvertBtn_Click` 1（C2） |
| GyroSettingsPanel | 11 | `TiltControlsButton_Click` 4、`GyroCalibration_Click` 4 → コマンド | 3（C2） |
| LightbarSettingsPanel | 5 | `ColorByBatteryPerCk_Click`、`RainbowBtn_Click`（16 行。ViewModel へ）→ コマンド | 色 3（C4） |
| General・Output・SixAxis・Trigger・StickSide・StickCalibration | 約 12 | 親へイベントを転送するだけのもの（`CustomEditorBtn_Click`、`RumbleTestBtn_Click` など）→ 親の ViewModel のコマンドへ直結 | `CloseButton_OnClick`（C1） |
| WelcomeDialog | 6 | ViGEm・HidHide・FakerInput の導入 3、Finished、Step2・Bluetooth の起動 → `ExternalComponentInstaller`＋コマンド（D2＝A） | なし |
| WindowTargetEditor | 3 | `BrowseBtn_Click`・`ExeNameOnlyBtn_Click`・`TitleMatchCombo_SelectionChanged` → コマンド・バインディング | 照準 2（C3） |
| OutputSlotManagerControl | 4 | Plugin・Unplug・SlotChangeAccept → コマンド | 列幅の保存 1（C3） |
| PluginOutDevWindow・RenameProfileWindow・DupBox・PresetOptionWindow・SaveWhere・LanguagePackControl・LanguageSelectDialog・UpdaterWindow（`YesBtn_Click` は 240 行）・FirstLaunchUtilWindow・Changelog・LogMessageDisplay・ControllerRegisterOptionsWindow | 約 45 | 確定系（Accept・Confirm・Apply・Yes・Save）→ コマンド。`UpdaterWindow.YesBtn_Click`（240 行）は、更新の取得・導入の流れで RV4-6 で扱う（導入は実行しない） | 閉じる・取り消し（C1） |
| ControllerReadingsControl | XAML 0 | タイマーを `GuardedTimerTick` の形へ（RV4-6） | — |

## 3. `BindingWindow` の `+=` 147（RV4-0 で決めること）

- 引数の型: Xbox のボタン 37 は `X360Controls` の名前（文字列。`CommandParameter="LXPos"` など。型変換は `Enum` の既定の変換）、キー 110 は仮想キーコード（整数）。2 つのコマンド（`SelectXboxOutputCommand`・`SelectKeyOutputCommand`）に分ける。
- 黄金値: 移行前の `associatedBindings`（147 件）を、テストの中の表へ写す（`BindingWindowButtonTableTests`）。移行後の XAML の `Command`＋`CommandParameter` を読み、147 件が一致し、重複がないことを検査する。
- ハンドラー `OutputButtonBtn_Click`・`OutputKeyBtn_Click` は、コマンドの本体（`BindingWindowViewModel` のメソッド）へ移し、辞書とともに削除する。

## 4. 承認をお願いする点

1. (c) の許可リスト案（§1。C1〜C4）。C4（色選択の窓）を (c) にするか、`IColorPicker` を作って (a) にするか。推奨: **(c) のまま許可**（RV3 の AssignmentEditor の電池の色と同じ。契約は RV6 の共有部品化で検討）。
2. `ProfileEditor.xaml.cs` の `partial` の区切り案（D4＝B）: `ProfileEditor.SpecialActions.cs`（特殊アクション一覧の操作）、`ProfileEditor.SaveApply.cs`（保存・適用・取り消し）、`ProfileEditor.Sidebar.cs`（サイドバー・プリセット・ウィンドウの大きさ）、既存の `HoverTables`。移動だけのコミットにする。
3. 進め方: 規模が大きい（約 300 件）ため、RV4-1（BindingWindow）から順に、各 Step ごとに Debug ALL（0 警告・0 エラー）→ コミットし、RV4-7 の凍結検査を最後に更新する。

## 5. 実数の根拠

- `ViewCommandBindingBaseline` の凍結値（RV3 完了時点）の合計から、RV3 の 4 画面（17 件）を除いた値。
- `UpdaterWindow` と `WelcomeDialog` の導入は、実機でもテストでも**実際の導入を行わない**（規約 §4）。
