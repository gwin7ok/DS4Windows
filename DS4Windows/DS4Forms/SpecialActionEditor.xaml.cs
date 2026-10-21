/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Microsoft.Win32;
using DS4Windows;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for SpecialActionEditor.xaml
    /// </summary>
    public partial class SpecialActionEditor : UserControl
    {

        // Phase10-Step2 W3: SA のモードは SpecialActEditorViewModel、Controls のモードは ControlActionEditorViewModel（共通の土台は ActionEditorViewModel）
        private ActionEditorViewModel specialActVM;
        private readonly bool controlMode;
        private readonly ControlSettingsEditTarget controlTarget;
        private MacroViewModel macroActVM;
        private LaunchProgramViewModel launchProgVM;
        private LoadProfileViewModel loadProfileVM;
        private PressKeyViewModel pressKeyVM;
        private ActionTypeEditorSet typeEditors;
        private SpecialActionViewModel disconnectBtVM;
        private CheckBatteryViewModel checkBatteryVM;
        private MultiActButtonViewModel multiActButtonVM;
        private SpecialActionViewModel saSteeringWheelVM;
        private SpecialActionViewModel calibrateGyroVM;

        public event EventHandler Cancel;

        /// <summary>Controls のモードか。</summary>
        public bool IsControlMode => controlMode;

        /// <summary>Controls のモードで「保存」が押され、入力が有効なときに発生する（受け手は、ProfileEditor の ShowControlAssignmentEditor。Phase10-Step3 W4）。</summary>
        public event EventHandler ControlSaved;

        /// <summary>Controls のモードで、保存が押されたときに選ばれていた種別。</summary>
        public DS4Windows.SpecialAction.ActionTypeId ControlSaveTypeId { get; private set; }
        public delegate void SaveHandler(object sender, string actionName);
        public event SaveHandler Saved;

        // Phase6-Step7b: 設定の読み書き先（specialActVM.DeviceNum＝編集スロット）とは別に、バッテリー確認の
        // 色プレビューと、ここから開くボタン設定画面・マクロ記録で使う実機のスロット番号を持つ。-1 は「実機なし」。
        // 詳細は docs-forDIMG/MadeByAgent/Phase6-Step7b-Plan.md §2.1
        private readonly int targetDevice;

        /// <summary>実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。</summary>
        private bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;

        /// <param name="deviceNum">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="targetDevice">
        /// Phase6-Step7b: 実機のスロット番号。-1 は実機なし。
        /// 渡し忘れをコンパイルで検出するため必須引数とする（Phase6-Step7b-Plan.md 決定2）
        /// </param>
        // Phase6-Step10-2-4a: BindingWindow／RecordBoxWindow の生成を窓口へ依頼する（必須引数）。
        // Phase6-Step10-3-2: 種別ごとの ViewModel の生成と、名前変更時の旧名の削除に必要なサービスも必須引数で受け取る。
        private readonly DS4Windows.DI.IEditorWindowFactory editorWindowFactory;
        private readonly DS4Windows.DI.ISpecialActionRepository specialActionRepository;
        // Phase7-Step10e W2: スロット数の上限は、静的な ControlService.CURRENT_DS4_CONTROLLER_LIMIT ではなく、注入された環境サービスから読む
        private readonly DS4Windows.DI.IEnvironmentService environmentService;

        /// <summary>SA のモード（従来どおり。SA の新規作成・編集）。</summary>
        public SpecialActionEditor(int deviceNum, ProfileList profileList, int targetDevice,
            DS4Windows.DI.IEditorWindowFactory editorWindowFactory,
            DS4Windows.DI.IViewModelFactory viewModelFactory,
            DS4Windows.DI.ISpecialActionRepository specialActionRepository,
            DS4Windows.DI.IProfileSettingsService profileSettingsService,
            DS4Windows.DI.IOutputSlotService outputSlotService,
            DS4Windows.DI.IEnvironmentService environmentService,
            DS4Windows.SpecialAction specialAction = null)
            : this(deviceNum, (DS4Windows.DS4ControlSettings)null, profileList, targetDevice, editorWindowFactory, viewModelFactory,
                specialActionRepository, profileSettingsService, outputSlotService, environmentService, specialAction)
        {
        }

        /// <summary>
        /// Controls のモード（Phase10-Step2 W3・W4）: Controls のボタン 1 つの割り当て（<paramref name="controlSettings"/>）を編集する。名前は、ボタンの表示名で固定（編集不可）、
        /// トリガーは、そのボタンだけ（変更不可）、種別の一覧は、Controls で現在保存できる種別だけ有効。
        /// Phase10-Step3 W4: <c>ProfileEditor</c> の <c>ShowControlAssignmentEditor</c> が、これを左ペイン全体に重ねて表示する（Controls の絵のボタン・一覧、Tilt、Swipe、Full Pull、Outer Bind、
        /// コントローラーの入力。Phase10-Step3-Plan.md §3.4）。保存（<see cref="ControlSaved"/>）は、編集用のコピーを元の設定へ反映してから発生する。
        /// </summary>
        public SpecialActionEditor(int deviceNum, DS4Windows.DS4ControlSettings controlSettings, ProfileList profileList, int targetDevice,
            DS4Windows.DI.IEditorWindowFactory editorWindowFactory,
            DS4Windows.DI.IViewModelFactory viewModelFactory,
            DS4Windows.DI.ISpecialActionRepository specialActionRepository,
            DS4Windows.DI.IProfileSettingsService profileSettingsService,
            DS4Windows.DI.IOutputSlotService outputSlotService,
            DS4Windows.DI.IEnvironmentService environmentService)
            : this(deviceNum, controlSettings ?? throw new ArgumentNullException(nameof(controlSettings)), profileList, targetDevice,
                editorWindowFactory, viewModelFactory, specialActionRepository, profileSettingsService, outputSlotService, environmentService, null)
        {
        }

        private SpecialActionEditor(int deviceNum, DS4Windows.DS4ControlSettings controlSettings, ProfileList profileList, int targetDevice,
            DS4Windows.DI.IEditorWindowFactory editorWindowFactory,
            DS4Windows.DI.IViewModelFactory viewModelFactory,
            DS4Windows.DI.ISpecialActionRepository specialActionRepository,
            DS4Windows.DI.IProfileSettingsService profileSettingsService,
            DS4Windows.DI.IOutputSlotService outputSlotService,
            DS4Windows.DI.IEnvironmentService environmentService,
            DS4Windows.SpecialAction specialAction)
        {
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this.editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            if (profileSettingsService == null) throw new ArgumentNullException(nameof(profileSettingsService));
            if (outputSlotService == null) throw new ArgumentNullException(nameof(outputSlotService));
            InitializeComponent();
            this.targetDevice = targetDevice;

            controlMode = controlSettings != null;
            if (controlMode)
            {
                // Phase10-Step2 W4: 対象のボタンの設定は、編集用のコピーを編集し、Save で元へ反映する（Cancel では、元は変わらない）
                controlTarget = new ControlSettingsEditTarget(controlSettings, profileSettingsService);
                specialActVM = new ControlActionEditorViewModel(deviceNum, controlTarget);
            }
            else
            {
                specialActVM = viewModelFactory.CreateSpecialActEditorViewModel(deviceNum, specialAction);
            }
            macroActVM = new MacroViewModel(specialActionRepository);
            launchProgVM = new LaunchProgramViewModel(specialActionRepository);
            loadProfileVM = new LoadProfileViewModel(profileList, specialActionRepository);
            pressKeyVM = new PressKeyViewModel(specialActionRepository, profileSettingsService, outputSlotService);
            pressKeyVM.SetDeviceNum(deviceNum);
            disconnectBtVM = new SpecialActionViewModel(5, specialActionRepository);
            checkBatteryVM = new CheckBatteryViewModel(specialActionRepository, environmentService);
            multiActButtonVM = new MultiActButtonViewModel(specialActionRepository);
            saSteeringWheelVM = new SpecialActionViewModel(8, specialActionRepository);
            calibrateGyroVM = new SpecialActionViewModel(9, specialActionRepository);

            // Phase10-Step2 W1: 種別ごとの入力の対応は、ActionTypeEditorSet に 1 か所で持つ（旧: LoadAction／CheckActionValid／SaveBtn_Click の 3 つの switch）
            typeEditors = new ActionTypeEditorSet(new Dictionary<DS4Windows.SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [DS4Windows.SpecialAction.ActionTypeId.Macro] = macroActVM,
                [DS4Windows.SpecialAction.ActionTypeId.Program] = launchProgVM,
                [DS4Windows.SpecialAction.ActionTypeId.Profile] = loadProfileVM,
                [DS4Windows.SpecialAction.ActionTypeId.Key] = pressKeyVM,   // Button も、Press Key のタブで編集する
                [DS4Windows.SpecialAction.ActionTypeId.DisconnectBT] = disconnectBtVM,
                [DS4Windows.SpecialAction.ActionTypeId.BatteryCheck] = checkBatteryVM,
                [DS4Windows.SpecialAction.ActionTypeId.MultiAction] = multiActButtonVM,
                [DS4Windows.SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate] = saSteeringWheelVM,
                [DS4Windows.SpecialAction.ActionTypeId.GyroCalibrate] = calibrateGyroVM,
            });

            // Hide tab headers. Tab content will still be visible
            blankActTab.Visibility = Visibility.Collapsed;
            macroActTab.Visibility = Visibility.Collapsed;
            launchProgActTab.Visibility = Visibility.Collapsed;
            loadProfileTab.Visibility = Visibility.Collapsed;
            pressKetActTab.Visibility = Visibility.Collapsed;
            disconnectBTTab.Visibility = Visibility.Collapsed;
            checkBatteryTab.Visibility = Visibility.Collapsed;
            multiActTab.Visibility = Visibility.Collapsed;
            sixaxisWheelCalibrateTab.Visibility = Visibility.Collapsed;
            gyroCalibrateTab.Visibility = Visibility.Collapsed;
            defaultActTab.Visibility = Visibility.Collapsed;
            unboundActTab.Visibility = Visibility.Collapsed;

            // Phase10-Step3: 入力の制限は、種別の一覧の項目の表示・非表示だけで行う（検証・保存ボタンは、モードを区別しない）。
            // Controls のモード: 「種別を選んでください」（添字 0）を隠す（選べるのは、Default／Macro／Output Key/Mouse/Button／Unbound）。
            // SA のモード: Controls のモードだけの「Default」「Unbound」を隠す
            if (controlMode)
            {
                ((ComboBoxItem)actionTypeCombo.Items[0]).Visibility = Visibility.Collapsed;
            }
            else
            {
                defaultTypeItem.Visibility = Visibility.Collapsed;
                unboundTypeItem.Visibility = Visibility.Collapsed;
            }

            // 種別の一覧には、全種別を出し、このモードで現在保存できない種別は、選べなくする（Controls は、Step3 まで Key・Button・Macro だけ）
            for (int i = 0; i < actionTypeCombo.Items.Count; i++)
            {
                if (actionTypeCombo.Items[i] is ComboBoxItem typeItem && i < specialActVM.TypeCatalog.All.Count)
                {
                    typeItem.IsEnabled = specialActVM.IsTypeEnabled(i);
                }
            }

            if (specialAction != null)
            {
                LoadAction(specialAction);
            }

            if (controlMode)
            {
                // 保存済みの割り当てを、種別ごとの入力へ読み込む。種別の一覧の初期の選択も、割り当てに合わせる
                controlTarget.LoadInto(pressKeyVM, macroActVM);
                specialActVM.ActionTypeIndex = controlTarget.InitialTypeIndex;

                // Phase10-Step4 W3: Macro タブの SA 専用の項目（Run on trigger release／Synchronized Run／Keep key state）は、Controls にも保存できるようになったため、
                // SA のモードと同じく出す（Step3 W3 までは、保存する場所がないため隠していた）
            }

            actionTypeTabControl.DataContext = specialActVM;
            actionTypeCombo.DataContext = specialActVM;
            actionNameTxt.DataContext = specialActVM;
            triggersListView.DataContext = specialActVM;

            macroActTab.DataContext = macroActVM;
            launchProgActTab.DataContext = launchProgVM;
            loadProfileTab.DataContext = loadProfileVM;
            pressKetActTab.DataContext = pressKeyVM;
            disconnectBTTab.DataContext = disconnectBtVM;
            checkBatteryTab.DataContext = checkBatteryVM;
            multiActTab.DataContext = multiActButtonVM;
            sixaxisWheelCalibrateTab.DataContext = saSteeringWheelVM;
            gyroCalibrateTab.DataContext = calibrateGyroVM;

            SetupLateEvents();
        }

        private void SetupLateEvents()
        {
            actionTypeCombo.SelectionChanged += ActionTypeCombo_SelectionChanged;
        }

        private void UnregisterDataContext()
        {
            actionTypeTabControl.DataContext = null;
            actionTypeCombo.DataContext = null;
            actionNameTxt.DataContext = null;
            triggersListView.DataContext = null;

            macroActTab.DataContext = null;
            launchProgActTab.DataContext = null;
            loadProfileTab.DataContext = null;
            pressKetActTab.DataContext = null;
            disconnectBTTab.DataContext = null;
            checkBatteryTab.DataContext = null;
            multiActTab.DataContext = null;
            sixaxisWheelCalibrateTab.DataContext = null;
            gyroCalibrateTab.DataContext = null;
        }

        private void LoadAction(DS4Windows.SpecialAction specialAction)
        {
            // Phase10-Step2 W2: トリガーの読み込みと、一覧にない名前の除外は、ViewModel が行う（旧: チェックボックス 49＋49 個を、ここで 1 つずつ照合）
            specialActVM.LoadAction(specialAction);

            // Phase10-Step2 W1: 種別に対応する入力へ読み込む（Button も、Press Key のタブ）
            typeEditors.Load(specialAction);
        }

        private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (specialActVM.ActionTypeIndex <= 0)
            {
                saveBtn.IsEnabled = false;
            }
            else
            {
                saveBtn.IsEnabled = true;
            }

            triggersListView.Visibility = Visibility.Visible;
        }

        // Controls のモードの保存。入力が無効なときは、結びつきを外さず、エラーを表示したまま戻る（SA のモードと同じ）。
        // 有効なときは、種別ごとの入力から、編集用のコピーへ書き出し、元の設定へ反映してから ControlSaved を出す（Phase10-Step2 W4）。
        // Phase10-Step3 W4: ControlSaved の受け手は、ProfileEditor（ShowControlAssignmentEditor）
        private void SaveControlMode()
        {
            DS4Windows.SpecialAction tempAct = new DS4Windows.SpecialAction("null", "null", "null", "null");
            if (!specialActVM.IsValid(tempAct)) return;
            if (!specialActVM.IsTypeEnabled(specialActVM.ActionTypeIndex)) return;   // Controls に、まだ保存できない種別

            controlTarget.WriteFrom(specialActVM.ActionTypeIndex, pressKeyVM, macroActVM);
            controlTarget.Apply();

            ControlSaveTypeId = specialActVM.TypeAssoc[specialActVM.ActionTypeIndex];
            UnregisterDataContext();
            ControlSaved?.Invoke(this, EventArgs.Empty);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            UnregisterDataContext();

            Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (controlMode)
            {
                SaveControlMode();
                return;
            }

            DS4Windows.SpecialAction.ActionTypeId typeId = specialActVM.TypeAssoc[specialActVM.ActionTypeIndex];
            DS4Windows.SpecialAction tempAct = new DS4Windows.SpecialAction("null", "null", "null", "null");
            bool valid = specialActVM.IsValid(tempAct);
            if (valid)
            {
                specialActVM.SetAction(tempAct);
                // Duplicate-name validation is handled by the ViewModel (normalized
                // comparison). No additional re-check here to avoid duplication
                // and potential race conditions.
                valid = typeEditors.IsValid(typeId, tempAct);
            }
            else if (specialActVM.ExistingName)
            {
                // If the ViewModel already detected an existing name error,
                // show the message near the Save button and bail out *before*
                // we detach data contexts. This keeps the editor state intact
                // so the user can correct the name. Use a lightweight custom
                // window so we can position it precisely.
                try
                {
                    // Use the custom-positioned window but styled to look like
                    // a native MessageBox so we can control exact placement.
                    ShowMessageWindowAt(Properties.Resources.ActionExists, saveBtn);
                }
                catch
                {
                    // Fallback to MessageBox if positioning fails for any reason
                    MessageBox.Show(Properties.Resources.ActionExists);
                }
                return;
            }

            // Phase10-Step2 W2（実機確認で発見。以前からの不具合）: 入力が無効なとき（トリガーなし、名前が空、種別未選択）に、先に
            // データの結びつきを外すと、エラーが表示される前に、編集画面が空になっていた。無効なときは、結びつきを保ち、エラーを表示したまま、修正できるようにする
            // （名前の重複で、メッセージを出して戻る経路と同じ）。保存できるときだけ、結びつきを外す
            if (valid)
            {
                UnregisterDataContext();
                bool editMode = specialActVM.EditMode;
                if (editMode && specialActVM.SavedAction.name != specialActVM.ActionName)
                {
                    // Phase6-Step10-3-2: Global.RemoveAction の置換（Actions.xml への保存と読み直しを含む削除）
                    specialActionRepository.RemoveActionAndSave(specialActVM.SavedAction.name);
                    editMode = false;
                }

                // Phase10-Step2 W1: 種別に対応する入力を保存する
                typeEditors.Save(typeId, tempAct, editMode);

                Saved?.Invoke(this, tempAct.name);
            }
        }

        private void RecordMacroBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = macroActVM.PrepareSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(specialActVM.DeviceNum, settings, targetDevice,
                showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                macroActVM.Macro.Clear();
                macroActVM.Macro.AddRange((int[])settings.action.actionMacro);
                macroActVM.UpdateMacroString();
                // Phase10-Step1 W3: Execute Mode／Scan Code は、記録画面に出さず（記録専用）、Macro タブの PlaybackOptions だけが持つ。
                // 記録画面へは PrepareSettings で渡し、保存で同じ値が返るため、手の書き戻しは不要
            };

            recordWin.ShowDialog();
        }

        // Unload trigger toggle buttons and handlers removed: Key-type SpecialActions use single-trigger toggle mode.

        private void BatteryEmptyColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            Color tempcolor = checkBatteryVM.EmptyColor;
            dialog.colorPicker.SelectedColor = tempcolor;
            // Phase6-Step7b: 色のプレビューは実機（targetDevice）に対して行う。実機なしなら何もしない
            if (HasTargetDevice) checkBatteryVM.StartForcedColor(tempcolor, targetDevice);
            dialog.ColorChanged += (sender2, color) =>
            {
                if (HasTargetDevice) checkBatteryVM.UpdateForcedColor(color, targetDevice);
            };
            dialog.ShowDialog();
            if (HasTargetDevice) checkBatteryVM.EndForcedColor(targetDevice);
            checkBatteryVM.EmptyColor = dialog.colorPicker.SelectedColor.GetValueOrDefault();
        }

        private void BatteryFullColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            Color tempcolor = checkBatteryVM.FullColor;
            dialog.colorPicker.SelectedColor = tempcolor;
            // Phase6-Step7b: 色のプレビューは実機（targetDevice）に対して行う。実機なしなら何もしない
            if (HasTargetDevice) checkBatteryVM.StartForcedColor(tempcolor, targetDevice);
            dialog.ColorChanged += (sender2, color) =>
            {
                if (HasTargetDevice) checkBatteryVM.UpdateForcedColor(color, targetDevice);
            };
            dialog.ShowDialog();
            if (HasTargetDevice) checkBatteryVM.EndForcedColor(targetDevice);
            checkBatteryVM.FullColor = dialog.colorPicker.SelectedColor.GetValueOrDefault();
        }

        private void MultiTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareTapSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(specialActVM.DeviceNum, settings, targetDevice, false,
                showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.TapMacro.Clear();
                multiActButtonVM.TapMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void MultiHoldTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareHoldSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(specialActVM.DeviceNum, settings, targetDevice, false,
                showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.HoldMacro.Clear();
                multiActButtonVM.HoldMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateHoldDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void MultiDoubleTapTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            DS4Windows.DS4ControlSettings settings = multiActButtonVM.PrepareDoubleTapSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(specialActVM.DeviceNum, settings, targetDevice, false,
                showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                multiActButtonVM.DoubleTapMacro.Clear();
                multiActButtonVM.DoubleTapMacro.AddRange((int[])settings.action.actionMacro);
                multiActButtonVM.UpdateDoubleTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Exe (*.exe)|*.exe|Batch (*.bat,*.cmd)|*.bat;*.cmd|All Files (*.*)|*.*";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                launchProgVM.Filepath = dialog.FileName;
            }
        }

        private void PressKeySelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (controlMode)
            {
                // Phase10-Step3 W2: Controls のモードは、対象のボタンと Extras を載せた一時設定を、Control のモードの出力ピッカーへ渡す。
                // 結果は、Press Key の状態（Default を含む）と、編集用のコピーの Extras へ取り込む（元の設定は、Save まで変わらない）
                DS4Windows.DS4ControlSettings controlSettings = pressKeyVM.PrepareSettings(controlTarget.Control, controlTarget.Extras);
                BindingWindow controlWindow = editorWindowFactory.CreateBindingWindow(specialActVM.DeviceNum, controlSettings, targetDevice,
                    BindingWindow.ExposeMode.Control);
                controlWindow.Owner = App.Current.MainWindow;
                controlWindow.ShowDialog();
                pressKeyVM.ReadSettings(controlSettings, specialActVM.DeviceNum, allowDefault: true);
                controlTarget.Extras = controlSettings.extras;
                pressKeyVM.UpdateToggleControls();

                // 出力ピッカーで Default／Unbound を選んだときは、種別の一覧の「Default」「Unbound」の項目へ切り替える（一覧と、選んだ内容を一致させる）
                if (pressKeyVM.SelectedActionType == DS4Windows.DS4ControlSettings.ActionType.Default)
                {
                    actionTypeCombo.SelectedIndex = specialActVM.TypeCatalog.DefaultIndex;
                }
                else if (pressKeyVM.SelectedActionType == DS4Windows.DS4ControlSettings.ActionType.Button
                    && pressKeyVM.SelectedButton == (int)DS4Windows.X360Controls.Unbound)
                {
                    actionTypeCombo.SelectedIndex = specialActVM.TypeCatalog.UnboundIndex;
                }

                return;
            }

            DS4Windows.DS4ControlSettings settings = pressKeyVM.PrepareSettings();
            BindingWindow window = editorWindowFactory.CreateBindingWindow(specialActVM.DeviceNum, settings, targetDevice,
                BindingWindow.ExposeMode.SpecialAction);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
            pressKeyVM.ReadSettings(settings, specialActVM.DeviceNum);
            pressKeyVM.UpdateToggleControls();
        }

        // Show a transient message window positioned near the given anchor element.
        // This creates a small modal window with an OK button and positions it
        // slightly below the anchor (e.g., the Save button).
        private void ShowMessageWindowAt(string message, FrameworkElement anchor)
        {
            // Keep the existing custom window implementation as a fallback.
            Window owner = Application.Current?.MainWindow;

            Window msgWin = new Window()
            {
                Title = string.Empty,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.Manual,
                FontFamily = SystemFonts.MessageFontFamily,
                FontSize = SystemFonts.MessageFontSize
            };

            var panel = new StackPanel() { Margin = new Thickness(16) };
            var tb = new TextBlock()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                MaxWidth = 420
            };
            var okBtn = new Button() { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 80 };
            okBtn.Click += (s, e) => msgWin.Close();
            panel.Children.Add(tb);
            panel.Children.Add(okBtn);
            msgWin.Content = panel;

            // Compute screen position for anchor and horizontally center the dialog
            double centerX = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                        var transform = src.CompositionTarget.TransformFromDevice;
                        var dpiPt = transform.Transform(anchorPt);
                        AppLogger.LogDebug($"SpecialActionEditor: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                        centerX = dpiPt.X + anchor.ActualWidth / 2.0;
                        top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        centerX = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else if (owner != null)
                {
                    centerX = owner.Left + 200;
                    top = owner.Top + 100;
                }
            }
            catch { }

            // Measure content to determine width, then position so dialog is centered
            panel.Measure(new Size(600, 400));
            double contentW = panel.DesiredSize.Width;
            double windowW = contentW + 32; // include margins/chrome approximation
            msgWin.Left = centerX - windowW / 2.0;
            msgWin.Top = top;

            msgWin.ShowDialog();
        }

        // Show a native MessageBox positioned by creating an invisible tiny owner
        // window located at the desired anchor point. MessageBox centers itself
        // over its owner, so placing the owner at the anchor makes the MessageBox
        // appear at that spot while preserving native look-and-feel.
        private void ShowPositionedMessageBox(string message, FrameworkElement anchor)
        {
            // Compute target point (screen coordinates in WPF units)
            double left = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                        var transform = src.CompositionTarget.TransformFromDevice;
                        var dpiPt = transform.Transform(anchorPt);
                        AppLogger.LogDebug($"SpecialActionEditor.ShowPositionedMessageBox: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                        left = dpiPt.X + anchor.ActualWidth / 2.0; // center anchor horizontally
                        top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        left = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else
                {
                    var owner = Application.Current?.MainWindow;
                    if (owner != null)
                    {
                        left = owner.Left + 100;
                        top = owner.Top + 100;
                    }
                }
            }
            catch { }

            // Create tiny transparent owner window at the computed point
            var ownerWindow = new Window()
            {
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Place owner so its center is at (left, top). Adjust to account for 1x1 size.
            ownerWindow.Left = left - 0.5;
            ownerWindow.Top = top - 0.5;

            try
            {
                ownerWindow.Show();
                // Use ownerWindow as owner for native MessageBox
                MessageBox.Show(ownerWindow, message);
            }
            finally
            {
                try { ownerWindow.Close(); } catch { }
            }
        }
    }
}
