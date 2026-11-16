using System;
using System.Collections.Generic;
using System.Windows;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 割り当て編集画面（<see cref="AssignmentEditor"/>）の編集の状態と操作。種別ごとの ViewModel 一式、保存の順序、
    /// 記録画面・出力ピッカーへの設定の受け渡しを持ち、画面は業務のサービス・設定の型を直接参照しない（V2）。
    /// SA のモード（新規・編集）と、Controls のモード（ボタン 1 つの割り当て）の両方を扱う。
    /// </summary>
    public sealed class AssignmentEditorSession
    {
        /// <summary>保存の結果。</summary>
        public enum SaveOutcome
        {
            /// <summary>入力が無効（トリガーなし、名前が空、種別未選択など）。結びつきを保ち、エラーを表示したまま修正できる。</summary>
            Invalid,
            /// <summary>同名の SA がある。メッセージを出して戻る（結びつきは保つ）。</summary>
            ExistingName,
            Saved,
        }

        private readonly IEditorWindowFactory editorWindowFactory;
        private readonly ISpecialActionRepository specialActionRepository;
        private readonly IEnvironmentService environmentService;
        private readonly ControlSettingsEditTarget controlTarget;
        private readonly ActionTypeEditorSet typeEditors;

        // SA のモードで編集中の SA に付ける Extras（SpecialAction.outputExtras と同じ書式。付けないときは null）。
        // Press Key の出力ピッカーで編集し、SA の保存のあとに SaveActionExtras で書く
        private string saExtras;

        /// <param name="deviceNum">設定を読み書きするスロット（編集用の作業スロット）。</param>
        /// <param name="controlSettings">Controls のモードで編集するボタンの設定。SA のモードは null。</param>
        /// <param name="targetDevice">実機のスロット番号。-1 は実機なし。</param>
        public AssignmentEditorSession(int deviceNum, DS4ControlSettings controlSettings, SpecialAction specialAction, ProfileList profileList, int targetDevice,
            IEditorWindowFactory editorWindowFactory, IProfileEditViewModelFactory viewModelFactory, IActionViewModelFactory actionViewModelFactory,
            ISpecialActionRepository specialActionRepository, IProfileSettingsService profileSettingsService, IEnvironmentService environmentService,
            IWindowSystem windowSystem)
        {
            this.editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            if (actionViewModelFactory == null) throw new ArgumentNullException(nameof(actionViewModelFactory));
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            if (profileSettingsService == null) throw new ArgumentNullException(nameof(profileSettingsService));
            if (windowSystem == null) throw new ArgumentNullException(nameof(windowSystem));
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            TargetDevice = targetDevice;
            DeviceNum = deviceNum;

            // FocusWindow のターゲットマークで、ドロップしたウィンドウを取得する（DS4Windows 自身のウィンドウは対象にしない）
            WindowPicker = new WindowTargetPicker(windowSystem, Environment.ProcessId);

            ControlMode = controlSettings != null;
            if (ControlMode)
            {
                // 対象のボタンの設定は、編集用のコピーを編集し、Save で元へ反映する（Cancel では、元は変わらない）
                controlTarget = new ControlSettingsEditTarget(controlSettings, profileSettingsService);
                ActionVM = viewModelFactory.CreateControlActionEditorViewModel(deviceNum, controlTarget);
            }
            else
            {
                ActionVM = actionViewModelFactory.CreateSpecialActEditorViewModel(deviceNum, specialAction);
            }

            MacroVM = actionViewModelFactory.CreateMacroViewModel();
            LaunchProgVM = actionViewModelFactory.CreateLaunchProgramViewModel();
            LoadProfileVM = actionViewModelFactory.CreateLoadProfileViewModel(profileList);
            PressKeyVM = viewModelFactory.CreatePressKeyViewModel(deviceNum);
            DisconnectBtVM = actionViewModelFactory.CreateSpecialActionViewModel(5);
            CheckBatteryVM = actionViewModelFactory.CreateCheckBatteryViewModel();
            MultiActVM = actionViewModelFactory.CreateMultiActButtonViewModel();
            CalibrateGyroVM = actionViewModelFactory.CreateSpecialActionViewModel(9);
            FocusWindowVM = actionViewModelFactory.CreateFocusWindowViewModel();

            // アナログな出力の強さの入力は、SA のモードだけに出す（Controls は、入力のアナログ値で強さが決まる）
            PressKeyVM.StrengthApplies = !ControlMode;

            // 種別ごとの入力の対応は、ActionTypeEditorSet に 1 か所で持つ
            typeEditors = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [SpecialAction.ActionTypeId.Macro] = MacroVM,
                [SpecialAction.ActionTypeId.Program] = LaunchProgVM,
                [SpecialAction.ActionTypeId.Profile] = LoadProfileVM,
                [SpecialAction.ActionTypeId.Key] = PressKeyVM,   // Button も、Press Key のタブで編集する
                [SpecialAction.ActionTypeId.DisconnectBT] = DisconnectBtVM,
                [SpecialAction.ActionTypeId.BatteryCheck] = CheckBatteryVM,
                [SpecialAction.ActionTypeId.MultiAction] = MultiActVM,
                [SpecialAction.ActionTypeId.GyroCalibrate] = CalibrateGyroVM,
                [SpecialAction.ActionTypeId.FocusWindow] = FocusWindowVM,
            });

            if (specialAction != null)
            {
                // トリガーの読み込みと、一覧にない名前の除外は、ViewModel が行う
                ActionVM.LoadAction(specialAction);
                saExtras = specialAction.outputExtras;
                typeEditors.Load(specialAction);
            }

            if (ControlMode)
            {
                // 保存済みの割り当てを、種別ごとの入力へ読み込む。種別の一覧の初期の選択も、割り当てに合わせる
                controlTarget.LoadInto(PressKeyVM, MacroVM, typeEditors);
                ActionVM.ActionTypeIndex = controlTarget.InitialTypeIndex;
            }
        }

        public bool ControlMode { get; }
        public int DeviceNum { get; }
        public int TargetDevice { get; }
        public WindowTargetPicker WindowPicker { get; }

        public ActionEditorViewModel ActionVM { get; }
        public MacroViewModel MacroVM { get; }
        public LaunchProgramViewModel LaunchProgVM { get; }
        public LoadProfileViewModel LoadProfileVM { get; }
        public PressKeyViewModel PressKeyVM { get; }
        public SpecialActionViewModel DisconnectBtVM { get; }
        public CheckBatteryViewModel CheckBatteryVM { get; }
        public MultiActButtonViewModel MultiActVM { get; }
        public SpecialActionViewModel CalibrateGyroVM { get; }
        public FocusWindowViewModel FocusWindowVM { get; }

        /// <summary>実機（TargetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。</summary>
        public bool HasTargetDevice => TargetDevice >= 0 && TargetDevice < environmentService.ControllerSlotLimit;

        /// <summary>
        /// Controls のモードの保存。入力が無効なときは false（エラーを表示したまま戻る）。
        /// 有効なときは、種別ごとの入力から編集用のコピーへ書き出し、元の設定へ反映して true を返す。
        /// </summary>
        public bool TrySaveControl()
        {
            SpecialAction tempAct = new SpecialAction("null", "null", "null", "null");
            if (!ActionVM.IsValid(tempAct)) return false;
            if (!ActionVM.IsTypeEnabled(ActionVM.ActionTypeIndex)) return false;   // Controls に、まだ保存できない種別

            // 種別ごとの入力の検証を、SA のモードと同じく呼ぶ。Default（None）と Unbound（Button に写る）は、入力がないため検証しない
            int typeIndex = ActionVM.ActionTypeIndex;
            bool hasInput = typeIndex != ActionVM.TypeCatalog.DefaultIndex && typeIndex != ActionVM.TypeCatalog.UnboundIndex;
            if (hasInput && !typeEditors.IsValid(ActionVM.TypeAssoc[typeIndex], tempAct)) return false;

            controlTarget.WriteFrom(ActionVM.ActionTypeIndex, PressKeyVM, MacroVM, typeEditors);
            controlTarget.Apply();
            return true;
        }

        /// <summary>
        /// SA のモードの保存。無効なときは、結びつきを保ったまま戻る（修正できるように）。保存できるときだけ、
        /// <paramref name="detachBindings"/> でデータの結びつきを外してから、旧名の削除→種別ごとの保存→Extras の保存の順に行う。
        /// </summary>
        public SaveOutcome TrySaveSpecialAction(Action detachBindings, out string savedName)
        {
            savedName = null;
            SpecialAction.ActionTypeId typeId = ActionVM.TypeAssoc[ActionVM.ActionTypeIndex];
            SpecialAction tempAct = new SpecialAction("null", "null", "null", "null");
            bool valid = ActionVM.IsValid(tempAct);
            if (valid)
            {
                ActionVM.SetAction(tempAct);
                // 名前の重複の検証は ViewModel が行う（ここでは再確認しない）
                valid = typeEditors.IsValid(typeId, tempAct);
            }
            else if (ActionVM.ExistingName)
            {
                return SaveOutcome.ExistingName;
            }

            if (!valid) return SaveOutcome.Invalid;

            detachBindings?.Invoke();
            bool editMode = ActionVM.EditMode;
            if (editMode && ActionVM.SavedAction.name != ActionVM.ActionName)
            {
                // Actions.xml への保存と読み直しを含む削除
                specialActionRepository.RemoveActionAndSave(ActionVM.SavedAction.name);
                editMode = false;
            }

            typeEditors.Save(typeId, tempAct, editMode);

            // SA の保存は SA を作り直すため、Extras は保存のあとに書く（付けていないときは、書くものがない）
            if (!string.IsNullOrWhiteSpace(saExtras)) specialActionRepository.SaveActionExtras(tempAct.name, saExtras);

            savedName = tempAct.name;
            return SaveOutcome.Saved;
        }

        /// <summary>Macro タブの記録画面を開く。</summary>
        public void RecordMacro()
        {
            DS4ControlSettings settings = MacroVM.PrepareSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(DeviceNum, settings, TargetDevice, showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                MacroVM.Macro.Clear();
                MacroVM.Macro.AddRange((int[])settings.action.actionMacro);
                MacroVM.UpdateMacroString();
                // Execute Mode／Scan Code は、記録画面に出さず（記録専用）、Macro タブの PlaybackOptions だけが持つ
            };

            recordWin.ShowDialog();
        }

        public void RecordMultiTap()
        {
            DS4ControlSettings settings = MultiActVM.PrepareTapSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(DeviceNum, settings, TargetDevice, false, showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                MultiActVM.TapMacro.Clear();
                MultiActVM.TapMacro.AddRange((int[])settings.action.actionMacro);
                MultiActVM.UpdateTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        public void RecordMultiHold()
        {
            DS4ControlSettings settings = MultiActVM.PrepareHoldSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(DeviceNum, settings, TargetDevice, false, showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                MultiActVM.HoldMacro.Clear();
                MultiActVM.HoldMacro.AddRange((int[])settings.action.actionMacro);
                MultiActVM.UpdateHoldDisplayText();
            };

            recordWin.ShowDialog();
        }

        public void RecordMultiDoubleTap()
        {
            DS4ControlSettings settings = MultiActVM.PrepareDoubleTapSettings();
            RecordBoxWindow recordWin = editorWindowFactory.CreateRecordBoxWindow(DeviceNum, settings, TargetDevice, false, showPlaybackOptions: false);
            recordWin.Saved += (sender2, args) =>
            {
                MultiActVM.DoubleTapMacro.Clear();
                MultiActVM.DoubleTapMacro.AddRange((int[])settings.action.actionMacro);
                MultiActVM.UpdateDoubleTapDisplayText();
            };

            recordWin.ShowDialog();
        }

        /// <summary>
        /// Press Key の出力ピッカーを開き、結果を取り込む。出力ピッカーで Default／Unbound を選んだときは、
        /// 種別の一覧を切り替える添字を返す（切り替えないときは null）。
        /// </summary>
        public int? PickOutput()
        {
            if (ControlMode)
            {
                // 対象のボタンと Extras を載せた一時設定を、Control のモードの出力ピッカーへ渡す。
                // 結果は、Press Key の状態（Default を含む）と、編集用のコピーの Extras へ取り込む（元の設定は、Save まで変わらない）
                DS4ControlSettings controlSettings = PressKeyVM.PrepareSettings(controlTarget.Control, controlTarget.Extras);
                BindingWindow controlWindow = editorWindowFactory.CreateBindingWindow(DeviceNum, controlSettings, TargetDevice,
                    BindingWindow.ExposeMode.Control);
                controlWindow.Owner = Application.Current.MainWindow;
                bool wasDefault = PressKeyVM.SelectedActionType == DS4ControlSettings.ActionType.Default;
                controlWindow.ShowDialog();
                PressKeyVM.ReadSettings(controlSettings, DeviceNum, allowDefault: true);
                controlTarget.Extras = controlSettings.extras;
                PressKeyVM.UpdateToggleControls();

                // 何も選ばずに閉じた［× ほか］ときは切り替えない（Default だった状態で、種別だけを変えてから開き、× で閉じると、
                // 状態は Default のまま変わっていないのに、一覧が Default に戻って見えるため）
                if (PressKeyVM.SelectedActionType == DS4ControlSettings.ActionType.Default
                    && (!wasDefault || controlWindow.DefaultPicked))
                {
                    return ActionVM.TypeCatalog.DefaultIndex;
                }

                if (PressKeyVM.SelectedActionType == DS4ControlSettings.ActionType.Button
                    && PressKeyVM.SelectedButton == (int)DS4Windows.Core.Model.X360Controls.Unbound)
                {
                    return ActionVM.TypeCatalog.UnboundIndex;
                }

                return null;
            }

            DS4ControlSettings settings = PressKeyVM.PrepareSettings();
            // 編集中の SA の Extras を、出力ピッカーへ渡して、結果を受け取る（Controls のモードと同じ）。何も設定されていない値（全部 0）は、付けない
            if (!string.IsNullOrEmpty(saExtras)) settings.extras = saExtras;
            BindingWindow window = editorWindowFactory.CreateBindingWindow(DeviceNum, settings, TargetDevice,
                BindingWindow.ExposeMode.SpecialAction);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            saExtras = settings.IsExtrasEmpty(settings.extras) ? null : settings.extras;
            PressKeyVM.ReadSettings(settings, DeviceNum);
            PressKeyVM.UpdateToggleControls();
            return null;
        }
    }
}
