using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for ProfileEditor.xaml
    /// </summary>
    public partial class ProfileEditor : UserControl
    {
        // ...既存フィールド...

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        private class HoverImageInfo
        {
            public Point point;
            public Size size;
        }

        /// <summary>実機なし（プロファイル一覧から開いた場合）を表す targetDevice の値。</summary>
        public const int NoTargetDevice = -1;

        // 設定の読み書き先（編集スロット）。常に作業スロット Global.TEST_PROFILE_INDEX を使い、
        // 編集内容は保存・適用を押すまで、接続中のどのコントローラーにも影響しない
        private readonly int deviceNum = ControllerLimits.TestProfileIndex;
        // ランブルテスト・ライトバーのプレビュー・校正・マクロ記録などで使う実機のスロット番号。
        // NoTargetDevice（-1）は「実機なし」。設定の読み書き先 deviceNum とは別に持つ。
        // ViewModel をこの値で生成するため、コンストラクタでだけ設定する
        private readonly int targetDevice = NoTargetDevice;
        // レイアウト・言語設定（コンストラクタ注入。Global.ProfileEditor*Width 等の置換先）
        private readonly DS4Windows.UI.Abstractions.IEditorLayoutSettings appSettingsService;
        // 編集スロットの LS／RS 補正値・出力種別の読み書き先（Global.LSModInfo 等の置換先）
        private readonly IProfileEditViewModelFactory _viewModelFactory;
        // 補助ウィンドウ（ボタン割り当て、プリセット、スティック校正、スペシャルアクション編集）の生成の窓口
        private readonly DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory;
        // 業務のサービス（プロファイルの読み込み、スペシャルアクションの参照、実機の振動・入力・較正）への窓口（V2）
        private readonly DS4Windows.UI.Services.ProfileEditorBackend backend;

        /// <summary>実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。</summary>
        private bool HasTargetDevice => backend.HasTargetDevice(targetDevice);
        private ProfileSettingsViewModel profileSettingsVM;
        private MappingListViewModel mappingListVM;

        // 一覧（メイン画面のプロファイル一覧）に名前があるか。一覧が取れないときは、あるものとして扱う
        private static bool LoadProfileTargetExists(string name)
        {
            var holder = (Application.Current?.MainWindow as MainWindow)?.ProfileListHolder;
            if (holder?.ProfileListCol == null) return true;

            return holder.ProfileListCol.Any(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }
        private ProfileEntity? currentProfile;
        private SpecialActionsListViewModel specialActionsVM;
        // スペシャルアクション一覧を最初に表示したときに、一度だけ並べ替えを適用する
        private bool specialActionsSorted = false;

        public event EventHandler? Closed;

        public delegate void ProfileSavedHandler(ProfileEditor sender, string profile);
        public event ProfileSavedHandler? ProfileSaved;

        /// <summary>
        /// アクションの除去・名前変更により、編集中以外も含むプロファイルの XML を書き換えたときに、
        /// 書き換えたプロファイル名の一覧を通知する。MainWindow が、保存時と同じ再適用の経路（SyncProfileListAndControllers）を呼ぶ。
        /// </summary>
        public event Action<IReadOnlyList<string>>? ProfilesUpdatedExternally;

        private Dictionary<Button, ImageBrush> hoverImages =
            new Dictionary<Button, ImageBrush>();

        private Dictionary<Button, HoverImageInfo> hoverLocations = new Dictionary<Button, HoverImageInfo>();
        private Dictionary<Button, int> hoverIndexes = new Dictionary<Button, int>();
        private Dictionary<int, Button> reverseHoverIndexes = new Dictionary<int, Button>();

        private bool keepsize;
        private bool controllerReadingsTabActive = false;

        // 読み取り専用のままにする。書き込みの経路は KeepSizeCheckBox_Click（Click イベント）だけで、XAML のバインディングは使わない
        // （以前の IsChecked="{Binding Keepsize, Mode=TwoWay}" は、DataContext がメインウィンドウの MainWindowsViewModel のため解決せず、外した）。
        public bool Keepsize
        {
            get => keepsize;
        }

        private System.Timers.Timer? inputTimer;

        private TouchButtonUserControl touchButtonUC;

        // （重複していた空のコンストラクタ宣言を削除）
        // 並べ替えを適用して一覧を更新する。見出しの文字（矢印）は SpecialActionsListViewModel の
        // ヘッダー文字プロパティが変更通知で更新する
        // 振動テストボタンの動作（画面に依存しない判断）
        private readonly RumbleTestController rumbleTest;

        // NOTE: Local visual-tree helper methods were removed in favor of the shared
        // `UtilMethods.FindVisualChildren<T>` and `UtilMethods.FindVisualChild<childItem>` helpers.
        //
        // The previous helper implementations (FindVisualChildByName, FindVisualChildrenByTypeName,
        // FindAncestorByTypeName) were duplicates of functionality provided by the project's
        // `UtilMethods` class. They have been removed to reduce maintenance burden and avoid
        // divergence. If a call site needs specific behavior not covered by UtilMethods,
        // add a narrowly-scoped helper or adapt the call site accordingly.

        /// <param name="targetDevice">
        /// Edit／New Profile ボタンを押したコントローラーのスロット番号。
        /// プロファイル一覧から開いた場合は NoTargetDevice（-1）。編集スロットは常に Global.TEST_PROFILE_INDEX
        /// </param>
        /// <param name="appSettingsService">
        /// エディタ画面のレイアウト（左右幅・Special Actions の列幅・ウィンドウサイズ）と表示言語の設定。
        /// MainWindow から渡す（Global の直接参照を置換）
        /// </param>
        /// <param name="profileSettingsService">
        /// プロファイル設定（LSModInfo／RSModInfo／OutContType）。MainWindow から渡す
        /// </param>
        public ProfileEditor(int targetDevice, DS4Windows.UI.Abstractions.IEditorLayoutSettings appSettingsService,
            DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory, DS4Windows.UI.Services.ProfileEditorBackend backend,
            IControllerReadingsFeed readingsSource, IProfileEditViewModelFactory viewModelFactory,
            IActionViewModelFactory actionViewModelFactory)
        {
            if (actionViewModelFactory == null) throw new ArgumentNullException(nameof(actionViewModelFactory));
            ArgumentNullException.ThrowIfNull(backend);
            this.backend = backend;
            // 補助ウィンドウの生成を窓口へ依頼する（必須引数）
            ArgumentNullException.ThrowIfNull(editorWindowFactory);
            this.editorWindowFactory = editorWindowFactory;
            this.targetDevice = targetDevice;
            ArgumentNullException.ThrowIfNull(appSettingsService);
            this.appSettingsService = appSettingsService;
            AppLogger.LogDebug($"[ProfileEditor] Opened profile editor for device={deviceNum}, targetDevice={targetDevice}");

            InitCommands();
            InitializeComponent();
            InitVisualizerButtons();

            // 旧 AppHost.GetService の 4 件を、コンストラクター引数に替えた（IEditorWindowFactory.CreateProfileEditor が渡す）
            rumbleTest = backend.CreateRumbleTest();
            // ControllerReadingsControl は XAML から生成されるため、ControlService を Initialize で渡す
            conReadingsUserCon.Initialize(readingsSource ?? throw new ArgumentNullException(nameof(readingsSource)));

            // SpecialActionsリスト表示前にカルチャを明示的に再設定
            var lang = appSettingsService.UseLang;
            var ci = System.Globalization.CultureInfo.GetCultureInfo(lang);
            DS4Windows.Properties.Resources.Culture = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;

            // ファクトリが解決できない場合のレガシーフォールバック（new ProfileSettingsViewModel）は削除した
            // （ViewModel の依存を必須引数にしたため、ファクトリ以外に組み立てる経路が無い）
            var vmFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _viewModelFactory = vmFactory;
            profileSettingsVM = vmFactory.CreateProfileSettingsViewModel(deviceNum, targetDevice);
            picBoxHover.Visibility = Visibility.Hidden;
            picBoxHover2.Visibility = Visibility.Hidden;

            mappingListVM = vmFactory.CreateMappingListViewModel(deviceNum, profileSettingsVM.Output.ContType);
            specialActionsVM = actionViewModelFactory.CreateSpecialActionsListViewModel(deviceNum);

            // S9-10: 削除されたプロファイルを参照する Load Profile は、一覧でも「(none)」と表示する（割り当て編集画面と同じ）
            mappingListVM.ProfileExists = LoadProfileTargetExists;
            specialActionsVM.ProfileExists = LoadProfileTargetExists;

            touchButtonUC = new TouchButtonUserControl(vmFactory.CreateTouchButtonViewModel(deviceNum));
            TouchpadButtonControlDisplaySetup();

            RemoveHoverBtnText();
            PopulateHoverImages();
            PopulateHoverLocations();
            PopulateHoverIndexes();
            PopulateReverseHoverIndexes();
            PopulateGyroActionsTriggersMenu();

            AssignTiltAssociation();
            touchpadPanel.AssignSwipeAssociation(mappingListVM);
            AssignTriggerFullPullAssociation();
            AssignStickOuterBindAssociation();

            inputTimer = new System.Timers.Timer(100);
            inputTimer.Elapsed += InputDS4;
            SetupEvents();

            // --- 追加: 保存されたSplitter位置と列幅の復元 ---
            RestoreSplitterAndColumnWidths();
        }
        // Splitter位置とSpecial Actions列幅の保存・復元は IAppSettingsService 経由で統一

        private bool IsValidWidth(double w)
        {
            return !double.IsNaN(w) && !double.IsInfinity(w) && w > 0.0 && w < 10000.0;
        }

        // ...既存のコード...

        private void PopulateGyroActionsTriggersMenu()
        {
            gyroPanel.CreateTriggerMenus(profileSettingsVM.Gyro);
        }

        private void SticksPanel_CustomCurveEditorRequested(object? sender, string target)
        {
            if (target == "LS") LaunchCurveEditor(profileSettingsVM.Sticks.Left.CustomCurve);
            else if (target == "RS") LaunchCurveEditor(profileSettingsVM.Sticks.Right.CustomCurve);
        }

        private void TriggersPanel_CustomCurveEditorRequested(object? sender, string target)
        {
            if (target == "L2") LaunchCurveEditor(profileSettingsVM.Triggers.L2CustomCurve);
            else if (target == "R2") LaunchCurveEditor(profileSettingsVM.Triggers.R2CustomCurve);
        }

        private void TouchpadVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 出力モードを変えたら、「ボタンの有効化方式」の部品を、そのモードのタブへ移す
            if (e.PropertyName == nameof(TouchpadSettingsViewModel.TouchpadOutputIndex)) TouchpadButtonControlDisplaySetup();
        }

        private void TouchpadPanel_SwipeControlClicked(object? sender, DS4Controls control)
        {
            ShowControlAssignmentEditor(mappingListVM.ControlMap[control], updateHighlight: true);
        }

        private void GyroPanel_TiltControlClicked(object? sender, DS4Controls control)
        {
            ShowControlAssignmentEditor(mappingListVM.ControlMap[control], updateHighlight: true);
        }

        /// <summary>
        /// Place touchpad button mode options UserControl in active Touchpad TabItem.
        /// Applicable TabItem control needs to contain a ContentControl
        /// </summary>
        private void TouchpadButtonControlDisplaySetup()
        {
            touchpadPanel.PlaceTouchButtonControl(touchButtonUC, profileSettingsVM.Touchpad.TouchpadOutputIndex);
        }

        private void UpdateReadingsSZDeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.SixAxisZDead = profileSettingsVM.SixAxis.SZDeadZone;
        }

        private void UpdateReadingsSXDeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.SixAxisXDead = profileSettingsVM.SixAxis.SXDeadZone;
        }

        private void UpdateReadingsR2DeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.R2Dead = profileSettingsVM.Triggers.R2DeadZone;
        }

        private void UpdateReadingsL2DeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.L2Dead = profileSettingsVM.Triggers.L2DeadZone;
        }

        private void UpdateReadingsLsDeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadX = profileSettingsVM.Sticks.Left.DeadZone;
            conReadingsUserCon.LsDeadY = profileSettingsVM.Sticks.Left.DeadZone;
        }

        private void UpdateReadingsLsDeadZoneX(object? sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadX = sticksPanel.AxialLeft.AxialVM.DeadZoneX;
        }

        private void UpdateReadingsLsDeadZoneY(object? sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadY = sticksPanel.AxialLeft.AxialVM.DeadZoneY;
        }

        private void UpdateReadingsRsDeadZone(object? sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadX = profileSettingsVM.Sticks.Right.DeadZone;
            conReadingsUserCon.RsDeadY = profileSettingsVM.Sticks.Right.DeadZone;
        }

        private void UpdateReadingsRsDeadZoneX(object? sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadX = sticksPanel.AxialRight.AxialVM.DeadZoneX;
        }

        private void UpdateReadingsRsDeadZoneY(object? sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadY = sticksPanel.AxialRight.AxialVM.DeadZoneY;
        }

        private void UpdateReadingsLSDrift(object? sender, EventArgs e)
        {
            conReadingsUserCon.LsDriftX = profileSettingsVM.Sticks.Left.DriftXAxis;
            conReadingsUserCon.LsDriftY = profileSettingsVM.Sticks.Left.DriftYAxis;
        }

        private void UpdateReadingsRSDrift(object? sender, EventArgs e)
        {
            conReadingsUserCon.RsDriftX = profileSettingsVM.Sticks.Right.DriftXAxis;
            conReadingsUserCon.RsDriftY = profileSettingsVM.Sticks.Right.DriftYAxis;
        }

        private void AssignTiltAssociation()
        {
            gyroPanel.AssignTiltAssociation(mappingListVM);
        }

        private void AssignTriggerFullPullAssociation()
        {
            triggersPanel.AssignFullPullAssociation(mappingListVM);
        }

        private void AssignStickOuterBindAssociation()
        {
            sticksPanel.AssignOuterBindAssociation(mappingListVM);
        }

        // コントローラーの図のボタンは、ControllerVisualizerControl が配置表（DualShock4Editor）から生成する。元の名前のまま持つ
        private Button crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l1ConBtn, r1ConBtn, l2ConBtn, r2ConBtn, shareConBtn, optionsConBtn, guideConBtn, muteConBtn, fnlConBtn, fnrConBtn, blpConBtn, brpConBtn, leftTouchConBtn, multiTouchConBtn, rightTouchConBtn, topTouchConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn;

        private void StopEditorBindings()
        {
            ClearEditorDataContexts();

            touchButtonUC.UnregisterDataContext();
            sticksPanel.AxialLeft.UnregisterDataContext();
            sticksPanel.AxialRight.UnregisterDataContext();
        }

        // S9 の追加要望: コントローラー図のマウスオーバーでボタン一覧の選択を切り替える処理に、500ms の遅延を入れる
        // （通過しただけの意図しないマウスオーバーで、選択が切り替わらないようにする）。ホバー画像の表示は即時のまま
        private const int HoverSelectDelayMs = 500;
        private System.Timers.Timer? hoverSelectTimer;   // UI のタイマーは System.Timers.Timer に統一。Elapsed は別スレッドで来るため、UI へ戻して処理する
        private Button? hoverSelectPending;

        private void CancelHoverSelect()
        {
            hoverSelectTimer?.Stop();
            hoverSelectPending = null;
        }

        private void InputControlHighlight(Button control, bool selectNow = true)
        {
            HighlightHoverImage(control);
            if (selectNow) SelectHoveredControl(control);
        }

        private void UpdateHighlightLabel(MappedControl mapped)
        {
            string display = $"{mapped.ControlName}: {mapped.MappingName}";

            highlightControlDisplayLb.Content = display;
        }

        private void ContBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Button control) return;
            InputControlHighlight(control, selectNow: false);
            StartHoverSelect(control);
        }

        // GyroOutModeCombo_SelectionChanged（ジャイロ出力モードの変更時に、実機の
        // touchPad.ResetToggleGyroModes() を即時に呼ぶ処理）と FrictionUD_ValueChanged（トラックボール摩擦の変更時に
        // ResetTrackAccel を即時に呼ぶ処理）は削除した。編集スロットが常に作業スロット（8）になり到達しなくなったため。
        // 同じ処理は、適用・保存時の ApplyProfile → ControlService.CheckProfileOptions で行われる
        //

        #region Save / Apply Profile Logic (Unified)

        #endregion

        private void KeepSizeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            keepsize = checkBox != null && checkBox.IsChecked == true;
        }

        private void SixAxisPanel_CustomCurveEditorRequested(object? sender, string target)
        {
            if (target == "SX") LaunchCurveEditor(profileSettingsVM.SixAxis.SXCustomCurve);
            else if (target == "SZ") LaunchCurveEditor(profileSettingsVM.SixAxis.SZCustomCurve);
        }

        private void LaunchCurveEditor(string customDefinition)
        {
            profileSettingsVM.LaunchCurveEditor(customDefinition);
        }

        // 書き換えたプロファイルがあれば、MainWindow へ通知する（再適用のロジックは MainWindow の 1 か所だけ）
        private void NotifyProfilesUpdated(IReadOnlyList<string> changedProfiles)
        {
            if (changedProfiles != null && changedProfiles.Count > 0)
            {
                ProfilesUpdatedExternally?.Invoke(changedProfiles);
            }
        }

        private void Ds4LightbarColorBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            highlightControlDisplayLb.Content = "";
        }

        // 左ペイン全体（タブ）に重なって表示されている編集画面（Controls の割り当てエディター、SA の編集画面）。
        // 表示されているあいだは null でない。開いているあいだは、新しく開かない（コントローラーの入力［タイマー］と、右ペインに残る入口からの二重起動を防ぐ）。
        // TODO(Phase10-Step3b): 入口をすべて左ペインのタブのボタンにする（Axis Config の 4 つの入口は、Controls タブへボタンとして追加）。
        //   そのあとも、この判定は、コントローラーの入力（タイマー）経由の安全装置として残す
        private UserControl? leftPaneEditor;

        private void ShowControlBindingWindow()
        {
            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            ShowControlAssignmentEditor(mpControl, updateHighlight: true);
        }

        private void EditSelectedControl()
        {
            if (mappingListVM.SelectedIndex >= 0)
            {
                ShowControlBindingWindow();
            }
        }

        private void GyroCalibration_Click(object? sender, EventArgs e)
        {
            // 校正するのは実機（FuncDevNum＝targetDevice、実機なしならコントローラー0）。
            // 編集スロットのフィールド deviceNum と区別するため、ローカル変数名を分けた
            int calibrationDevice = profileSettingsVM.FuncDevNum;
            // 未接続のスロットでは何もしない（以前は null の機器を触って NullReferenceException でアプリが終了していた）
            backend.ResetGyroCalibration(calibrationDevice);
        }

        private void ExportActions()
        {
            specialActionsVM.PickAndExportActions();
        }

        // ソート状態（列名・方向）は SpecialActionsListViewModel.CurrentSortColumn /
        // CurrentSortAscending が唯一の実体。View 側では保持しない（二重状態の解消）。
    } // ← ProfileEditorクラスの閉じ括弧を追加
}