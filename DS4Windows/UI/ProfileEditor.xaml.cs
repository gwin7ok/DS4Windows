using DS4Windows.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using NonFormTimer = System.Timers.Timer;
using System.ComponentModel;
using System.Windows.Forms;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
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

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
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

        // Phase6-Step7b: 設定の読み書き先（編集スロット）。常に作業スロット Global.TEST_PROFILE_INDEX を使い、
        // 編集内容は保存・適用を押すまで、接続中のどのコントローラーにも影響しない
        private readonly int deviceNum = ControllerLimits.TestProfileIndex;
        // Phase6-Step7b: ランブルテスト・ライトバーのプレビュー・校正・マクロ記録などで使う実機のスロット番号。
        // NoTargetDevice（-1）は「実機なし」。設定の読み書き先 deviceNum とは別に持つ。
        // ViewModel をこの値で生成するため、コンストラクタでだけ設定する
        private readonly int targetDevice = NoTargetDevice;
        // Phase6-Step8-2: レイアウト・言語設定（コンストラクタ注入。Global.ProfileEditor*Width 等の置換先）
        private readonly IAppSettingsService appSettingsService;
        // Phase6-Step8-3: 編集スロットの LS／RS 補正値・出力種別の読み書き先（Global.LSModInfo 等の置換先）
        private readonly IProfileSettingsService profileSettingsService;
        private readonly IViewModelFactory _viewModelFactory;
        // Phase6-Step10-2-4a: 補助ウィンドウ（ボタン割り当て、プリセット、スティック校正、スペシャルアクション編集）の生成の窓口
        private readonly DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory;
        // Phase7-Step10e W2: スロット数の上限は、静的な environmentService.ControllerSlotLimit ではなく、注入された環境サービスから読む
        private readonly DS4Windows.Core.Abstractions.IEnvironmentService environmentService;

        /// <summary>実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。</summary>
        private bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;
        private ProfileSettingsViewModel profileSettingsVM;
        private readonly DS4Windows.Core.Abstractions.IProfileRepository profileRepository;
        private readonly DS4Windows.Processing.Actions.IProfileSwitcher profileSwitcher;
        private readonly IControllerSlotCommands slotCommands;
        private readonly DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch runtimeFeatures;
        private MappingListViewModel mappingListVM;

        // 一覧（メイン画面のプロファイル一覧）に名前があるか。一覧が取れないときは、あるものとして扱う
        private static bool LoadProfileTargetExists(string name)
        {
            var holder = (Application.Current?.MainWindow as MainWindow)?.ProfileListHolder;
            if (holder?.ProfileListCol == null) return true;

            return holder.ProfileListCol.Any(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }
        private ProfileEntity currentProfile;
        private SpecialActionsListViewModel specialActionsVM;
        // 特殊アクション一覧を最初に表示したときに、一度だけ並べ替えを適用する
        private bool specialActionsSorted = false;

        public event EventHandler Closed;

        public delegate void ProfileSavedHandler(ProfileEditor sender, string profile);
        public event ProfileSavedHandler ProfileSaved;

        /// <summary>
        /// Phase6-Step10-3b-3: アクションの除去・名前変更により、編集中以外も含むプロファイルの XML を書き換えたときに、
        /// 書き換えたプロファイル名の一覧を通知する。MainWindow が、保存時と同じ再適用の経路（SyncProfileListAndControllers）を呼ぶ。
        /// </summary>
        public event Action<IReadOnlyList<string>> ProfilesUpdatedExternally;

        private Dictionary<Button, ImageBrush> hoverImages =
            new Dictionary<Button, ImageBrush>();

        private Dictionary<Button, HoverImageInfo> hoverLocations = new Dictionary<Button, HoverImageInfo>();
        private Dictionary<Button, int> hoverIndexes = new Dictionary<Button, int>();
        private Dictionary<int, Button> reverseHoverIndexes = new Dictionary<int, Button>();

        private bool keepsize;
        private bool controllerReadingsTabActive = false;

        // Phase11 W3: 読み取り専用のままにする。書き込みの経路は KeepSizeCheckBox_Click（Click イベント）だけで、XAML のバインディングは使わない
        // （以前の IsChecked="{Binding Keepsize, Mode=TwoWay}" は、DataContext がメインウィンドウの MainWindowsViewModel のため解決せず、外した）。
        public bool Keepsize
        {
            get => keepsize;
        }

        private NonFormTimer inputTimer;

        private TouchButtonUserControl touchButtonUC;

        // （重複していた空のコンストラクタ宣言を削除）
        // 並べ替えを適用して一覧を更新する。見出しの文字（矢印）は SpecialActionsListViewModel の
        // ヘッダー文字プロパティが変更通知で更新する
        // 振動テストボタンの動作（画面に依存しない判断）
        private readonly RumbleTestController rumbleTest;

        private void SortSpecialActionsList(string columnName, bool asc)
        {
            ListSortDirection sortDirection = asc ? ListSortDirection.Ascending : ListSortDirection.Descending;
            specialActionsVM.SortActions(columnName, sortDirection);

            var view = (CollectionView)CollectionViewSource.GetDefaultView(specialActionsVM.ActionCol);
            view?.Refresh();
            AppLogger.LogDebug($"[SpecialActions] Sorted: column={columnName}, asc={asc}, count={specialActionsVM.ActionCol.Count}");
        }

        // NOTE: Local visual-tree helper methods were removed in favor of the shared
        // `UtilMethods.FindVisualChildren<T>` and `UtilMethods.FindVisualChild<childItem>` helpers.
        //
        // The previous helper implementations (FindVisualChildByName, FindVisualChildrenByTypeName,
        // FindAncestorByTypeName) were duplicates of functionality provided by the project's
        // `UtilMethods` class. They have been removed to reduce maintenance burden and avoid
        // divergence. If a call site needs specific behavior not covered by UtilMethods,
        // add a narrowly-scoped helper or adapt the call site accordingly.

        /// <param name="targetDevice">
        /// Phase6-Step7b: Edit／New Profile ボタンを押したコントローラーのスロット番号。
        /// プロファイル一覧から開いた場合は NoTargetDevice（-1）。編集スロットは常に Global.TEST_PROFILE_INDEX
        /// </param>
        /// <param name="appSettingsService">
        /// Phase6-Step8-2: エディタ画面のレイアウト（左右幅・Special Actions の列幅・ウィンドウサイズ）と表示言語の設定。
        /// MainWindow から渡す（Global の直接参照を置換）
        /// </param>
        /// <param name="profileSettingsService">
        /// Phase6-Step8-3: プロファイル設定（LSModInfo／RSModInfo／OutContType）。MainWindow から渡す
        /// </param>
        public ProfileEditor(int targetDevice, IAppSettingsService appSettingsService, IProfileSettingsService profileSettingsService,
            DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory, IProfileRepository profileRepository,
            IProfileSwitcher profileSwitcher, IControllerReadingsSource readingsSource, IViewModelFactory viewModelFactory,
            DS4Windows.Core.Abstractions.IEnvironmentService environmentService, DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch runtimeFeatures,
            IControllerSlotCommands slotCommands)
        {
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this.runtimeFeatures = runtimeFeatures ?? throw new ArgumentNullException(nameof(runtimeFeatures));   // Phase13-Step10 10-d: 旧 ControlService.setRumble／GetActiveInputControl の転送
            // Phase6-Step10-2-4a: 補助ウィンドウの生成を窓口へ依頼する（必須引数）
            this.editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            this.targetDevice = targetDevice;
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            AppLogger.LogDebug($"[ProfileEditor] Opened profile editor for device={deviceNum}, targetDevice={targetDevice}");

            InitializeComponent();
            InitVisualizerButtons();

            // Phase7-Step5 W5（Q5＝A）: 旧 AppHost.GetService の 4 件を、コンストラクター引数に替えた（IEditorWindowFactory.CreateProfileEditor が渡す）
            this.profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            this.profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
            this.slotCommands = slotCommands ?? throw new ArgumentNullException(nameof(slotCommands));
            rumbleTest = new RumbleTestController(slotCommands, environmentService);
            // Phase6-Step10-2-5: ControllerReadingsControl は XAML から生成されるため、ControlService を Initialize で渡す
            conReadingsUserCon.Initialize(readingsSource ?? throw new ArgumentNullException(nameof(readingsSource)));

            // SpecialActionsリスト表示前にカルチャを明示的に再設定
            var lang = appSettingsService.UseLang;
            var ci = System.Globalization.CultureInfo.GetCultureInfo(lang);
            DS4Windows.Properties.Resources.Culture = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;

            // Phase6-Step9-4: ファクトリが解決できない場合のレガシーフォールバック（new ProfileSettingsViewModel）は削除した
            // （ViewModel の依存を必須引数にしたため、ファクトリ以外に組み立てる経路が無い）
            var vmFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _viewModelFactory = vmFactory;
            profileSettingsVM = vmFactory.CreateProfileSettingsViewModel(deviceNum, targetDevice);
            picBoxHover.Visibility = Visibility.Hidden;
            picBoxHover2.Visibility = Visibility.Hidden;

            mappingListVM = vmFactory.CreateMappingListViewModel(deviceNum, profileSettingsVM.Output.ContType);
            specialActionsVM = vmFactory.CreateSpecialActionsListViewModel(deviceNum);

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

            inputTimer = new NonFormTimer(100);
            inputTimer.Elapsed += InputDS4;
            SetupEvents();

            // --- 追加: 保存されたSplitter位置と列幅の復元 ---
            RestoreSplitterAndColumnWidths();
        }
        // Splitter位置とSpecial Actions列幅の保存・復元は IAppSettingsService 経由で統一

        // ウィンドウサイズ初期化ボタンのクリックイベント
        // すべての初期値は WindowLayoutDefaults から取得される（BackingStore経由）
        private void WindowSizeResetBtn_Click(object sender, RoutedEventArgs e)
        {
            // 初期値にリセット（左右領域・列幅）
            // これらは WindowLayoutDefaults.PROFILE_EDITOR_* の値を参照している
            appSettingsService.ProfileEditorLeftWidth = BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
            appSettingsService.ProfileEditorRightWidth = BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
            appSettingsService.SpecialActionNameColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
            appSettingsService.SpecialActionTriggerColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
            appSettingsService.SpecialActionDetailColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

            // ウィンドウ自体のサイズも初期値にリセット（ポジションは変更しない）
            // これらは WindowLayoutDefaults.MAIN_WINDOW_* の値を参照している
            appSettingsService.FormWidth = BackingStore.DEFAULT_FORM_WIDTH;
            appSettingsService.FormHeight = BackingStore.DEFAULT_FORM_HEIGHT;

            var win = Window.GetWindow(this);
            if (win != null)
            {
                // WindowLayoutDefaultsの値は論理ピクセルの初期値
                // WPFのWidthプロパティも論理ピクセルなので、そのまま代入
                win.Width = BackingStore.DEFAULT_FORM_WIDTH;
                win.Height = BackingStore.DEFAULT_FORM_HEIGHT;
            }

            // UIにも即時反映
            RestoreSplitterAndColumnWidths();
        }

        private void SaveSplitterAndColumnWidths()
        {
            var grid = this.Content as Grid ?? this.FindName("baseGrid") as Grid;
            if (grid == null || grid.ColumnDefinitions.Count < 3) return;

            double leftWidth = grid.ColumnDefinitions[0].Width.Value;
            double rightWidth = grid.ColumnDefinitions[2].Width.Value;

            // Special Actions ListViewのGridViewColumn幅取得
            var specialActionsLV = this.FindName("specialActionsLV") as System.Windows.Controls.ListView;
            if (specialActionsLV?.View is GridView gridView && gridView.Columns.Count >= 4)
            {
                try
                {
                    // IAppSettingsService 経由で保存
                    appSettingsService.ProfileEditorLeftWidth = (int)leftWidth;
                    appSettingsService.ProfileEditorRightWidth = (int)rightWidth;
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    appSettingsService.SpecialActionNameColWidth = (int)gridView.Columns[1].Width;
                    appSettingsService.SpecialActionTriggerColWidth = (int)gridView.Columns[2].Width;
                    appSettingsService.SpecialActionDetailColWidth = (int)gridView.Columns[3].Width;
                    // 必要なら他の列も追加

                    AppLogger.Log(AppLogLevel.Debug, $"[SaveSplitterAndColumnWidths] Saved left={appSettingsService.ProfileEditorLeftWidth} right={appSettingsService.ProfileEditorRightWidth} nameCol={appSettingsService.SpecialActionNameColWidth} trigCol={appSettingsService.SpecialActionTriggerColWidth} detailCol={appSettingsService.SpecialActionDetailColWidth}");
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Debug, $"[SaveSplitterAndColumnWidths] failed: {ex.Message}");
                }
            }
        }

        private void RestoreSplitterAndColumnWidths()
        {
            try
            {
                var grid = this.Content as Grid ?? this.FindName("baseGrid") as Grid;
                if (grid != null && grid.ColumnDefinitions.Count >= 3)
                {
                    double left = appSettingsService.ProfileEditorLeftWidth;
                    double right = appSettingsService.ProfileEditorRightWidth;
                    if (IsValidWidth(left))
                        grid.ColumnDefinitions[0].Width = new GridLength(left);
                    if (IsValidWidth(right))
                        grid.ColumnDefinitions[2].Width = new GridLength(right);
                }
                var specialActionsLV = this.FindName("specialActionsLV") as System.Windows.Controls.ListView;
                if (specialActionsLV?.View is GridView gridView && gridView.Columns.Count >= 4)
                {
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    // Active 列はプロファイルに永続化しない初期幅を毎回適用する（BackingStore の定数を使用）
                    gridView.Columns[0].Width = BackingStore.DEFAULT_SPECIAL_ACTION_ACTIVE_COL_WIDTH;

                    double nameW = appSettingsService.SpecialActionNameColWidth;
                    double trigW = appSettingsService.SpecialActionTriggerColWidth;
                    double detailW = appSettingsService.SpecialActionDetailColWidth;
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    if (IsValidWidth(nameW))
                        gridView.Columns[1].Width = nameW;
                    if (IsValidWidth(trigW))
                        gridView.Columns[2].Width = trigW;
                    if (IsValidWidth(detailW))
                        gridView.Columns[3].Width = detailW;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogLevel.Debug, $"[RestoreSplitterAndColumnWidths] failed: {ex.Message}");
            }
        }

        private bool IsValidWidth(double w)
        {
            return !double.IsNaN(w) && !double.IsInfinity(w) && w > 0.0 && w < 10000.0;
        }

        // ...既存のコード...

        private void PopulateGyroActionsTriggersMenu()
        {
            gyroPanel.CreateTriggerMenus(profileSettingsVM.Gyro);
        }

        private void SetupEvents()
        {
            outputPanel.OutConTypeCombo.SelectionChanged += OutConTypeCombo_SelectionChanged;
            outputPanel.RumbleTestClick += RumbleTestBtn_Click;
            generalPanel.UseControllerReadoutClick += UseControllerReadoutCk_Click;
            generalPanel.LaunchProgramBrowseClick += LaunchProgBrowseBtn_Click;
            mappingListBox.SelectionChanged += MappingListBox_SelectionChanged;
            Closed += ProfileEditor_Closed;

            profileSettingsVM.SixAxis.PropertyChanged += ProfileSettingsVM_PropertyChanged;
            sixAxisPanel.CustomCurveEditorRequested += SixAxisPanel_CustomCurveEditorRequested;
            gyroPanel.TiltControlClicked += GyroPanel_TiltControlClicked;
            gyroPanel.CalibrationRequested += GyroCalibration_Click;
            profileSettingsVM.Touchpad.PropertyChanged += TouchpadVM_PropertyChanged;
            touchpadPanel.SwipeControlClicked += TouchpadPanel_SwipeControlClicked;
            profileSettingsVM.Triggers.PropertyChanged += TriggersVM_PropertyChanged;
            profileSettingsVM.Sticks.PropertyChanged += SticksVM_PropertyChanged;
            sticksPanel.CustomCurveEditorRequested += SticksPanel_CustomCurveEditorRequested;
            sticksPanel.CalibrationRequested += CalibrateStick_OnClick;
            triggersPanel.CustomCurveEditorRequested += TriggersPanel_CustomCurveEditorRequested;

        }

        /// <summary>
        /// 6 軸の設定の値が変わったとき、試験表示（デッドゾーン）へ反映する。
        /// </summary>
        private void ProfileSettingsVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SixAxisSettingsViewModel.SXDeadZone): UpdateReadingsSXDeadZone(sender, EventArgs.Empty); break;
                case nameof(SixAxisSettingsViewModel.SZDeadZone): UpdateReadingsSZDeadZone(sender, EventArgs.Empty); break;
            }
        }

        /// <summary>
        /// 左右のスティックの軸ごとのデッドゾーンが変わったとき、試験表示へ反映する。
        /// </summary>
        private void AxialStick_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            bool left = ReferenceEquals(sender, sticksPanel.AxialLeft.AxialVM);
            switch (e.PropertyName)
            {
                case nameof(AxialStickControlViewModel.DeadZoneX):
                    if (left) UpdateReadingsLsDeadZoneX(sender, EventArgs.Empty); else UpdateReadingsRsDeadZoneX(sender, EventArgs.Empty);
                    break;
                case nameof(AxialStickControlViewModel.DeadZoneY):
                    if (left) UpdateReadingsLsDeadZoneY(sender, EventArgs.Empty); else UpdateReadingsRsDeadZoneY(sender, EventArgs.Empty);
                    break;
            }
        }

        private void SticksVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // デッドゾーンを変えたら、コントローラーの読み取り値の表示（試験表示）へ反映する
            switch (e.PropertyName)
            {
                case nameof(StickSettingsViewModel.LSDeadZone): UpdateReadingsLsDeadZone(sender, EventArgs.Empty); break;
                case nameof(StickSettingsViewModel.RSDeadZone): UpdateReadingsRsDeadZone(sender, EventArgs.Empty); break;
                case nameof(StickSettingsViewModel.LeftStickDriftXAxis):
                case nameof(StickSettingsViewModel.LeftStickDriftYAxis):
                    UpdateReadingsLSDrift(sender, EventArgs.Empty); break;
                case nameof(StickSettingsViewModel.RightStickDriftXAxis):
                case nameof(StickSettingsViewModel.RightStickDriftYAxis):
                    UpdateReadingsRSDrift(sender, EventArgs.Empty); break;
            }
        }

        private void SticksPanel_CustomCurveEditorRequested(object sender, string target)
        {
            if (target == "LS") LaunchCurveEditor(profileSettingsVM.Sticks.LSCustomCurve);
            else if (target == "RS") LaunchCurveEditor(profileSettingsVM.Sticks.RSCustomCurve);
        }

        private void TriggersVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // デッドゾーンを変えたら、コントローラーの読み取り値の表示（試験表示）へ反映する
            switch (e.PropertyName)
            {
                case nameof(TriggerSettingsViewModel.L2DeadZone): UpdateReadingsL2DeadZone(sender, EventArgs.Empty); break;
                case nameof(TriggerSettingsViewModel.R2DeadZone): UpdateReadingsR2DeadZone(sender, EventArgs.Empty); break;
            }
        }

        private void TriggersPanel_CustomCurveEditorRequested(object sender, string target)
        {
            if (target == "L2") LaunchCurveEditor(profileSettingsVM.Triggers.L2CustomCurve);
            else if (target == "R2") LaunchCurveEditor(profileSettingsVM.Triggers.R2CustomCurve);
        }

        private void TouchpadVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 出力モードを変えたら、「ボタンの有効化方式」の部品を、そのモードのタブへ移す
            if (e.PropertyName == nameof(TouchpadSettingsViewModel.TouchpadOutputIndex)) TouchpadButtonControlDisplaySetup();
        }

        private void TouchpadPanel_SwipeControlClicked(object sender, DS4Controls control)
        {
            ShowControlAssignmentEditor(mappingListVM.ControlMap[control], updateHighlight: true);
        }

        private void GyroPanel_TiltControlClicked(object sender, DS4Controls control)
        {
            ShowControlAssignmentEditor(mappingListVM.ControlMap[control], updateHighlight: true);
        }

        private void UnregisterEvents()
        {
            outputPanel.OutConTypeCombo.SelectionChanged -= OutConTypeCombo_SelectionChanged;
            outputPanel.RumbleTestClick -= RumbleTestBtn_Click;
            generalPanel.UseControllerReadoutClick -= UseControllerReadoutCk_Click;
            generalPanel.LaunchProgramBrowseClick -= LaunchProgBrowseBtn_Click;
            mappingListBox.SelectionChanged -= MappingListBox_SelectionChanged;
            Closed -= ProfileEditor_Closed;

            profileSettingsVM.SixAxis.PropertyChanged -= ProfileSettingsVM_PropertyChanged;
            sixAxisPanel.CustomCurveEditorRequested -= SixAxisPanel_CustomCurveEditorRequested;
            gyroPanel.TiltControlClicked -= GyroPanel_TiltControlClicked;
            gyroPanel.CalibrationRequested -= GyroCalibration_Click;
            profileSettingsVM.Touchpad.PropertyChanged -= TouchpadVM_PropertyChanged;
            touchpadPanel.SwipeControlClicked -= TouchpadPanel_SwipeControlClicked;
            profileSettingsVM.Triggers.PropertyChanged -= TriggersVM_PropertyChanged;
            profileSettingsVM.Sticks.PropertyChanged -= SticksVM_PropertyChanged;
            sticksPanel.CustomCurveEditorRequested -= SticksPanel_CustomCurveEditorRequested;
            sticksPanel.CalibrationRequested -= CalibrateStick_OnClick;
            triggersPanel.CustomCurveEditorRequested -= TriggersPanel_CustomCurveEditorRequested;
            sticksPanel.AxialLeft.AxialVM.PropertyChanged -= AxialStick_PropertyChanged;
            sticksPanel.AxialRight.AxialVM.PropertyChanged -= AxialStick_PropertyChanged;


            inputTimer.Stop();
            inputTimer.Elapsed -= InputDS4;
            inputTimer = null;

            StopEditorBindings();
        }


        /// <summary>
        /// Place touchpad button mode options UserControl in active Touchpad TabItem.
        /// Applicable TabItem control needs to contain a ContentControl
        /// </summary>
        private void TouchpadButtonControlDisplaySetup()
        {
            touchpadPanel.PlaceTouchButtonControl(touchButtonUC, profileSettingsVM.Touchpad.TouchpadOutputIndex);
        }

        private void UpdateReadingsSZDeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.SixAxisZDead = profileSettingsVM.SixAxis.SZDeadZone;
        }

        private void UpdateReadingsSXDeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.SixAxisXDead = profileSettingsVM.SixAxis.SXDeadZone;
        }

        private void UpdateReadingsR2DeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.R2Dead = profileSettingsVM.Triggers.R2DeadZone;
        }

        private void UpdateReadingsL2DeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.L2Dead = profileSettingsVM.Triggers.L2DeadZone;
        }

        private void UpdateReadingsLsDeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadX = profileSettingsVM.Sticks.LSDeadZone;
            conReadingsUserCon.LsDeadY = profileSettingsVM.Sticks.LSDeadZone;
        }

        private void UpdateReadingsLsDeadZoneX(object sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadX = sticksPanel.AxialLeft.AxialVM.DeadZoneX;
        }

        private void UpdateReadingsLsDeadZoneY(object sender, EventArgs e)
        {
            conReadingsUserCon.LsDeadY = sticksPanel.AxialLeft.AxialVM.DeadZoneY;
        }

        private void UpdateReadingsRsDeadZone(object sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadX = profileSettingsVM.Sticks.RSDeadZone;
            conReadingsUserCon.RsDeadY = profileSettingsVM.Sticks.RSDeadZone;
        }

        private void UpdateReadingsRsDeadZoneX(object sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadX = sticksPanel.AxialRight.AxialVM.DeadZoneX;
        }

        private void UpdateReadingsRsDeadZoneY(object sender, EventArgs e)
        {
            conReadingsUserCon.RsDeadY = sticksPanel.AxialRight.AxialVM.DeadZoneY;
        }

        private void UpdateReadingsLSDrift(object sender, EventArgs e)
        {
            conReadingsUserCon.LsDriftX = profileSettingsVM.Sticks.LeftStickDriftXAxis;
            conReadingsUserCon.LsDriftY = profileSettingsVM.Sticks.LeftStickDriftYAxis;
        }

        private void UpdateReadingsRSDrift(object sender, EventArgs e)
        {
            conReadingsUserCon.RsDriftX = profileSettingsVM.Sticks.RightStickDriftXAxis;
            conReadingsUserCon.RsDriftY = profileSettingsVM.Sticks.RightStickDriftYAxis;
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

        private void MappingListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mappingListVM.SelectedIndex >= 0)
            {
                if (reverseHoverIndexes.TryGetValue(mappingListVM.SelectedIndex, out Button tempBtn))
                {
                    InputControlHighlight(tempBtn);
                }
            }

        }

        // コントローラーの図のボタンは、ControllerVisualizerControl が配置表（DualShock4Editor）から生成する。元の名前のまま持つ
        private Button crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l1ConBtn, r1ConBtn, l2ConBtn, r2ConBtn, shareConBtn, optionsConBtn, guideConBtn, muteConBtn, fnlConBtn, fnrConBtn, blpConBtn, brpConBtn, leftTouchConBtn, multiTouchConBtn, rightTouchConBtn, topTouchConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn;

        /// <summary>配置表から生成されたボタンを元の名前のフィールドに持ち、クリック・ホバー・右クリックの処理を結び付ける</summary>
        private void InitVisualizerButtons()
        {
            crossConBtn = conCanvas["crossConBtn"];
            circleConBtn = conCanvas["circleConBtn"];
            squareConBtn = conCanvas["squareConBtn"];
            triangleConBtn = conCanvas["triangleConBtn"];
            l1ConBtn = conCanvas["l1ConBtn"];
            r1ConBtn = conCanvas["r1ConBtn"];
            l2ConBtn = conCanvas["l2ConBtn"];
            r2ConBtn = conCanvas["r2ConBtn"];
            shareConBtn = conCanvas["shareConBtn"];
            optionsConBtn = conCanvas["optionsConBtn"];
            guideConBtn = conCanvas["guideConBtn"];
            muteConBtn = conCanvas["muteConBtn"];
            fnlConBtn = conCanvas["fnlConBtn"];
            fnrConBtn = conCanvas["fnrConBtn"];
            blpConBtn = conCanvas["blpConBtn"];
            brpConBtn = conCanvas["brpConBtn"];
            leftTouchConBtn = conCanvas["leftTouchConBtn"];
            multiTouchConBtn = conCanvas["multiTouchConBtn"];
            rightTouchConBtn = conCanvas["rightTouchConBtn"];
            topTouchConBtn = conCanvas["topTouchConBtn"];
            l3ConBtn = conCanvas["l3ConBtn"];
            lsuConBtn = conCanvas["lsuConBtn"];
            lsrConBtn = conCanvas["lsrConBtn"];
            lsdConBtn = conCanvas["lsdConBtn"];
            lslConBtn = conCanvas["lslConBtn"];
            r3ConBtn = conCanvas["r3ConBtn"];
            rsuConBtn = conCanvas["rsuConBtn"];
            rsrConBtn = conCanvas["rsrConBtn"];
            rsdConBtn = conCanvas["rsdConBtn"];
            rslConBtn = conCanvas["rslConBtn"];
            upConBtn = conCanvas["upConBtn"];
            rightConBtn = conCanvas["rightConBtn"];
            downConBtn = conCanvas["downConBtn"];
            leftConBtn = conCanvas["leftConBtn"];

            // 大きさ 0 で隠れているボタンには、処理を結び付けない
            foreach (Button button in new[] { crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l1ConBtn, r1ConBtn, l2ConBtn, r2ConBtn, shareConBtn, optionsConBtn, guideConBtn, leftTouchConBtn, multiTouchConBtn, rightTouchConBtn, topTouchConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn })
            {
                button.Click += HoverConBtn_Click;
                button.MouseEnter += ContBtn_MouseEnter;
                button.MouseLeave += ContBtn_MouseLeave;
            }

            // プリセットのメニューを出すボタン（ABXY、スティック、十字キー）
            foreach (Button button in new[] { crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn })
            {
                button.MouseRightButtonUp += ConBtn_MouseRightButtonUp;
            }
        }

        private void RemoveHoverBtnText()
        {
            crossConBtn.Content = "";
            circleConBtn.Content = "";
            squareConBtn.Content = "";
            triangleConBtn.Content = "";
            l1ConBtn.Content = "";
            r1ConBtn.Content = "";
            l2ConBtn.Content = "";
            r2ConBtn.Content = "";
            shareConBtn.Content = "";
            optionsConBtn.Content = "";
            guideConBtn.Content = "";
            muteConBtn.Content = "";
            leftTouchConBtn.Content = "";
            multiTouchConBtn.Content = "";
            rightTouchConBtn.Content = "";
            topTouchConBtn.Content = "";

            l3ConBtn.Content = "";
            lsuConBtn.Content = "";
            lsrConBtn.Content = "";
            lsdConBtn.Content = "";
            lslConBtn.Content = "";

            r3ConBtn.Content = "";
            rsuConBtn.Content = "";
            rsrConBtn.Content = "";
            rsdConBtn.Content = "";
            rslConBtn.Content = "";

            upConBtn.Content = "";
            rightConBtn.Content = "";
            downConBtn.Content = "";
            leftConBtn.Content = "";

            fnlConBtn.Content = "";
            fnrConBtn.Content = "";
            blpConBtn.Content = "";
            brpConBtn.Content = "";
        }


        /// <summary>
        /// 編集するプロファイルを作業スロット（Global.TEST_PROFILE_INDEX）へ読み込み、画面を初期化する。
        /// Phase6-Step7b: 読み込み先は常に作業スロットで、コントローラーのスロットには触れない。
        /// 実機（targetDevice）はコンストラクタで確定しているため、引数では受け取らない。
        /// </summary>
        /// <param name="profile">編集するプロファイル。null は新規作成（プリセット選択から始める）</param>
        // Phase8-Step7 7.8d: 廃止された「シフト」の設定の警告（画面の実装）
        private readonly IRemovedShiftPrompt removedShiftPrompt = new MessageBoxRemovedShiftPrompt();
        // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の設定の警告（画面の実装）
        private readonly IRemovedGyroSwipePrompt removedGyroSwipePrompt = new MessageBoxRemovedGyroSwipePrompt();

        public void Reload(ProfileEntity profile = null)
        {
            ClearEditorDataContexts();

            AppLogger.LogDebug($"[ProfileEditor] Reload: editSlot={deviceNum}, targetDevice={targetDevice}, profile={(profile != null ? profile.Name : "(new profile)")}");

            if (profile != null)
            {
                currentProfile = profile;
                profileRepository.ProfilePath[ControllerLimits.TestProfileIndex] = profile.Name;

                profileRepository.LoadProfile(deviceNum, profile.Name);
                // Phase8-Step7 7.8d: 廃止された「シフト」の設定が残っていれば、警告して、削除するか残すかを選ばせる（残せば、開くたびに警告する）
                RemovedShiftNotice.Apply(profileSettingsService.GetRemovedShiftSettings(deviceNum), profile.Name, removedShiftPrompt);
                // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の設定が残っていれば、同じく警告して、削除するか残すかを選ばせる（残せば、開くたびに警告する）
                RemovedGyroSwipeNotice.Apply(profileSettingsService.ForSlot(deviceNum).Gyro.RemovedSwipe, profile.Name, removedGyroSwipePrompt);
                profileNameTxt.Text = profile.Name;
                profileNameTxt.IsEnabled = false;
            }
            else
            {
                currentProfile = null;
                PresetOptionWindow presetWin = editorWindowFactory.CreatePresetOptionWindow();
                presetWin.SetupData(deviceNum);
                presetWin.ShowDialog();
                if (presetWin.Result == MessageBoxResult.Cancel)
                {
                    profileSettingsVM.LoadBlankProfile();
                }
            }

            // プロファイルロード完了後に、XMLから読み込まれた ProfileActions を使ってリストを初期化・復元
            List<string> currentActionsList = profileRepository.ProfileActions != null && profileRepository.ProfileActions.Length > deviceNum
                ? profileRepository.ProfileActions[deviceNum]
                : null;
            string currentProfileActions = currentActionsList != null ? string.Join("/", currentActionsList) : string.Empty;
            specialActionsVM.LoadActions(currentProfile == null, currentProfileActions);

            lightbarPanel.RefreshColorByBatteryLayout(profileSettingsVM.Lightbar);

            // Phase6-Step7b: Controller Readings は、実機（なければコントローラー0）の入力に対して、
            // 作業スロット（編集中の設定）で計算した結果を表示する
            int readingsDevice = profileSettingsVM.FuncDevNum;
            generalPanel.UseControllerUD.Value = readingsDevice + 1;
            conReadingsUserCon.UseDevice(readingsDevice, ControllerLimits.TestProfileIndex);
            contReadingsTab.IsEnabled = true;

            conReadingsUserCon.EnableControl(false);
            sticksPanel.AxialLeft.UseDevice(_viewModelFactory.CreateAxialStickControlViewModel(profileSettingsVM.Sticks.LSMod));
            sticksPanel.AxialRight.UseDevice(_viewModelFactory.CreateAxialStickControlViewModel(profileSettingsVM.Sticks.RSMod));

            mappingListVM.UpdateMappings();
            profileSettingsVM.UpdateLateProperties();
            // Phase6-Step6-1: mappingListVM はプロファイル読み込み前の出力種別で生成されるため、読み込み後の
            // 出力種別（Xbox 360／DS4）でボタン名表記を明示的に揃える（出力の種類の選択の SelectionChanged の発生有無に依存しない）
            mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType);
            touchpadPanel.PopulateDisInvertMenu(profileSettingsVM.Touchpad);
            gyroPanel.PopulateTriggerMenus(profileSettingsVM.Gyro);
            AssignEditorDataContexts();
            // 読み込んだプロファイルのタッチパッド出力モードに合わせて、Button Activation Mode の表示位置を揃える
            // （コンストラクタでの配置は読み込み前の値で行われ、TouchpadOutputIndex の setter を通らない変更では更新されない）
            TouchpadButtonControlDisplaySetup();

            StickDeadZoneInfo lsMod = profileSettingsVM.Sticks.LSMod;
            if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                conReadingsUserCon.LsDeadX = profileSettingsVM.Sticks.LSDeadZone;
                conReadingsUserCon.LsDeadY = profileSettingsVM.Sticks.LSDeadZone;
            }
            else if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                conReadingsUserCon.LsDeadX = sticksPanel.AxialLeft.AxialVM.DeadZoneX;
                conReadingsUserCon.LsDeadY = sticksPanel.AxialLeft.AxialVM.DeadZoneY;
            }

            StickDeadZoneInfo rsMod = profileSettingsVM.Sticks.RSMod;
            if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                conReadingsUserCon.RsDeadX = profileSettingsVM.Sticks.RSDeadZone;
                conReadingsUserCon.RsDeadY = profileSettingsVM.Sticks.RSDeadZone;
            }
            else if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                conReadingsUserCon.RsDeadX = sticksPanel.AxialRight.AxialVM.DeadZoneX;
                conReadingsUserCon.RsDeadY = sticksPanel.AxialRight.AxialVM.DeadZoneY;
            }

            conReadingsUserCon.L2Dead = profileSettingsVM.Triggers.L2DeadZone;
            conReadingsUserCon.R2Dead = profileSettingsVM.Triggers.R2DeadZone;
            conReadingsUserCon.SixAxisXDead = profileSettingsVM.SixAxis.SXDeadZone;
            conReadingsUserCon.SixAxisZDead = profileSettingsVM.SixAxis.SZDeadZone;

            conReadingsUserCon.LsDriftX = profileSettingsVM.Sticks.LeftStickDriftXAxis;
            conReadingsUserCon.LsDriftY = profileSettingsVM.Sticks.LeftStickDriftYAxis;
            conReadingsUserCon.RsDriftX = profileSettingsVM.Sticks.RightStickDriftXAxis;
            conReadingsUserCon.RsDriftY = profileSettingsVM.Sticks.RightStickDriftYAxis;

            sticksPanel.AxialLeft.AxialVM.PropertyChanged += AxialStick_PropertyChanged;
            sticksPanel.AxialRight.AxialVM.PropertyChanged += AxialStick_PropertyChanged;

            // 初期表示も統一ソートメソッドを使用
            // NOTE: 呼び出しを削除しました。レンダリング完了後に Dispatcher.BeginInvoke で
            //       `SortSpecialActionsList(currentSortColumn, currentSortAsc)` を実行して
            //       ヘッダー内の DataTemplate 部分が確実に生成されてから ▲/▼ を反映します。

            if (profileSettingsVM.General.UseControllerReadout)
            {
                inputTimer.Start();
            }
        }

        /// <summary>
        /// 設定を読み直す前に、各タブの DataContext を外す。ViewModel の読み直しは PropertyChanged を出さずに値を入れ替えるため、
        /// 外してから <see cref="AssignEditorDataContexts"/> で付け直すことで、画面へ反映する（<c>UpdateLateProperties</c> の規約）。
        /// </summary>
        private void ClearEditorDataContexts()
        {
            profileSettingsTabCon.DataContext = null;
            touchpadTab.DataContext = null;
            gyroTab.DataContext = null;
            mappingListBox.DataContext = null;
            specialActionsTab.DataContext = null;
            lightbarRect.DataContext = null;
        }

        /// <summary>読み直しのあと、各タブの DataContext を付け直す（<see cref="ClearEditorDataContexts"/> の対）。</summary>
        private void AssignEditorDataContexts()
        {
            profileSettingsTabCon.DataContext = profileSettingsVM;
            touchpadTab.DataContext = profileSettingsVM.Touchpad;
            gyroTab.DataContext = profileSettingsVM.Gyro;
            mappingListBox.DataContext = mappingListVM;
            specialActionsTab.DataContext = specialActionsVM;
            lightbarRect.DataContext = profileSettingsVM.Lightbar;
        }

        private void StopEditorBindings()
        {
            ClearEditorDataContexts();

            touchButtonUC.UnregisterDataContext();
            sticksPanel.AxialLeft.UnregisterDataContext();
            sticksPanel.AxialRight.UnregisterDataContext();
        }

        private void RefreshEditorBindings()
        {
            // 注意: profileSettingsVM は INotifyPropertyChanged 非実装のため、直後のDataContext代入
            // （同一インスタンスの再代入）だけではWPFのバインディングは更新されない。本メソッドの
            // 呼び出し元（PresetBtn_Click）が必ず先にStopEditorBindings()（DataContext=null）を
            // 呼んでいることが、UI反映を成立させる前提条件になっている。詳細はUpdateLateProperties()の
            // コメント、および Phase5-Step14-FormSettings-Unification-Status.md タスク(c)-1 を参照。
            specialActionsVM.LoadActions(currentProfile == null);
            mappingListVM.UpdateMappings();
            profileSettingsVM.UpdateLateProperties();
            // Phase6-Step6-1: プリセット適用後の出力種別でボタン名表記を明示的に揃える（Reload と同じ理由）
            mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType);
            touchpadPanel.PopulateDisInvertMenu(profileSettingsVM.Touchpad);
            gyroPanel.PopulateTriggerMenus(profileSettingsVM.Gyro);
            AssignEditorDataContexts();
            touchButtonUC.RegisterDataContext();
            // プリセット適用でタッチパッド出力モードが変わっても、Button Activation Mode の表示位置が追従するようにする
            TouchpadButtonControlDisplaySetup();

            conReadingsUserCon.LsDeadX = profileSettingsVM.Sticks.LSDeadZone;
            conReadingsUserCon.RsDeadX = profileSettingsVM.Sticks.RSDeadZone;
            conReadingsUserCon.L2Dead = profileSettingsVM.Triggers.L2DeadZone;
            conReadingsUserCon.R2Dead = profileSettingsVM.Triggers.R2DeadZone;
            conReadingsUserCon.SixAxisXDead = profileSettingsVM.SixAxis.SXDeadZone;
            conReadingsUserCon.SixAxisZDead = profileSettingsVM.SixAxis.SZDeadZone;
            conReadingsUserCon.LsDriftX = profileSettingsVM.Sticks.LeftStickDriftXAxis;
            conReadingsUserCon.LsDriftY = profileSettingsVM.Sticks.LeftStickDriftYAxis;
            conReadingsUserCon.RsDriftX = profileSettingsVM.Sticks.RightStickDriftXAxis;
            conReadingsUserCon.RsDriftY = profileSettingsVM.Sticks.RightStickDriftYAxis;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (profileSettingsVM.FuncDevNum < environmentService.ControllerSlotLimit)
            {
                runtimeFeatures.SetRumble(0, 0, profileSettingsVM.FuncDevNum);
            }

            profileSettingsVM.Output.ResetOutDevTypeTemp();
            // Phase6-Step7b: 編集は作業スロットに対して行っており、コントローラーのスロットには届いていないため、
            // 元のプロファイルを読み込み直して取り消す処理は不要（旧: HaltReportingRunAction 内で LoadProfile していた）。
            // 作業スロットは次に編集画面を開いたときに読み込み直される
            AppLogger.LogDebug($"[ProfileEditor] Cancel: edit slot {deviceNum} discarded; controller slots were not modified (targetDevice={targetDevice})");

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void HoverConBtn_Click(object sender, RoutedEventArgs e)
        {
            // 500ms の遅延の前にクリックされたときも、クリックしたボタンの割り当てを開く（選択を先に確定する）
            CancelHoverSelect();
            if (sender is Button clicked) SelectHoveredControl(clicked);

            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            ShowControlAssignmentEditor(mpControl, updateHighlight: true);
        }

        // S9 の追加要望: コントローラー図のマウスオーバーでボタン一覧の選択を切り替える処理に、500ms の遅延を入れる
        // （通過しただけの意図しないマウスオーバーで、選択が切り替わらないようにする）。ホバー画像の表示は即時のまま
        private const int HoverSelectDelayMs = 500;
        private System.Windows.Threading.DispatcherTimer hoverSelectTimer;
        private Button hoverSelectPending;

        private void CancelHoverSelect()
        {
            hoverSelectTimer?.Stop();
            hoverSelectPending = null;
        }

        private void StartHoverSelect(Button control)
        {
            if (hoverSelectTimer == null)
            {
                hoverSelectTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(HoverSelectDelayMs),
                };
                hoverSelectTimer.Tick += (s, e) =>
                {
                    hoverSelectTimer.Stop();
                    Button pending = hoverSelectPending;
                    hoverSelectPending = null;
                    if (pending != null) SelectHoveredControl(pending);
                };
            }

            hoverSelectTimer.Stop();
            hoverSelectPending = control;
            hoverSelectTimer.Start();
        }

        private void InputControlHighlight(Button control, bool selectNow = true)
        {
            HighlightHoverImage(control);
            if (selectNow) SelectHoveredControl(control);
        }

        private void HighlightHoverImage(Button control)
        {
            if (hoverImages.TryGetValue(control, out ImageBrush tempBrush))
            {
                picBoxHover.Source = tempBrush.ImageSource;
                //picBoxHover.Width = tempBrush.ImageSource.Width * .8;
                //picBoxHover.Height = tempBrush.ImageSource.Height * .8;
                //control.Background = tempBrush;
                //control.Background = new SolidColorBrush(Colors.Green);
                //control.Width = tempBrush.ImageSource.Width;
                //control.Height = tempBrush.ImageSource.Height;
            }

            if (hoverLocations.TryGetValue(control, out HoverImageInfo tempInfo))
            {
                Canvas.SetLeft(picBoxHover, tempInfo.point.X);
                Canvas.SetTop(picBoxHover, tempInfo.point.Y);
                picBoxHover.Width = tempInfo.size.Width;
                picBoxHover.Height = tempInfo.size.Height;
                picBoxHover.Stretch = Stretch.Fill;
                picBoxHover.Visibility = Visibility.Visible;
            }
        }

        private void SelectHoveredControl(Button control)
        {
            if (hoverIndexes.TryGetValue(control, out int tempIndex))
            {
                mappingListVM.SelectedIndex = tempIndex;
                mappingListBox.ScrollIntoView(mappingListBox.SelectedItem);
                MappedControl mapped = mappingListVM.Mappings[tempIndex];
                UpdateHighlightLabel(mapped);
            }
        }

        private void UpdateHighlightLabel(MappedControl mapped)
        {
            string display = $"{mapped.ControlName}: {mapped.MappingName}";

            highlightControlDisplayLb.Content = display;
        }

        private void ContBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            InputControlHighlight(control, selectNow: false);
            StartHoverSelect(control);
        }

        private void ContBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelHoverSelect();
            //Button control = sender as Button;
            //control.Background = new SolidColorBrush(Colors.Transparent);
            Canvas.SetLeft(picBoxHover, 0);
            Canvas.SetTop(picBoxHover, 0);
            picBoxHover.Visibility = Visibility.Hidden;
        }

        // Phase6-Step7b（決定7＝H1）: GyroOutModeCombo_SelectionChanged（ジャイロ出力モードの変更時に、実機の
        // touchPad.ResetToggleGyroModes() を即時に呼ぶ処理）と FrictionUD_ValueChanged（トラックボール摩擦の変更時に
        // ResetTrackAccel を即時に呼ぶ処理）は削除した。編集スロットが常に作業スロット（8）になり到達しなくなったため。
        // 同じ処理は、適用・保存時の ApplyProfile → ControlService.CheckProfileOptions で行われる
        // （Phase6-Step7b-Plan.md §1A.3）

        #region Save / Apply Profile Logic (Unified)

        /// <summary>
        /// プロファイル保存および適用の共通処理ルート。
        /// UI上の選択値をSSOTに反映してXMLに保存し、親画面へ保存完了通知（ProfileSaved）を送信します。
        /// </summary>
        /// <param name="isApply">保存（false）か適用（true）か</param>
        /// <returns>保存成否</returns>
        private bool ExecuteSaveOrApply(bool isApply = false)
        {
            string profileName = profileNameTxt.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show(Properties.Resources.ValidName, "DS4Windows",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            // Phase 5 Step 14: SpecialActionsListViewModel から有効なアクション名リストを取得し、Global.ProfileActions に確実に反映
            if (specialActionsVM != null)
            {
                specialActionsVM.CommitEnabledActionsToProfile();
            }

            SaveSplitterAndColumnWidths();

            // Phase6-Step8-3: profileRepository はコンストラクタで必ず非 null（Global.SaveProfile へのフォールバックを削除）
            bool saved = profileSettingsVM.SaveProfile(profileName);

            if (saved)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Editor_ProfileSaved", profileName);
                // Phase6-Step7b（決定8）: 保存（Save）と適用（Apply）の違いは、画面を閉じるかどうかだけ。
                // どちらも親画面へ ProfileSaved を通知し、MainWindow.SyncProfileListAndControllers が、
                // プロファイル一覧を読み込み直したうえで、このプロファイルを使っているスロットにだけ再適用する。
                // 編集画面を開いたコントローラー（targetDevice）へ直接適用はしない（新規作成したプロファイルで
                // コントローラーが切り替わらないようにするため）。targetDevice はプレビュー・校正・ランブルテスト専用
                AppLogger.LogDebug($"[ProfileEditor] {(isApply ? "Apply" : "Save")}: profile '{profileName}' saved; re-applying to controller slots that use it (targetDevice={targetDevice})");
                ProfileSaved?.Invoke(this, profileName);
                return true;
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Editor_ProfileSaveFailed", profileName);
                return false;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ExecuteSaveOrApply(isApply: false))
            {
                Close();
            }
        }
        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSaveOrApply(isApply: true);
        }

        #endregion

        private void KeepSizeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            keepsize = checkBox != null && checkBox.IsChecked == true;
        }

        public void Close()
        {
            if (profileSettingsVM.FuncDevNum < environmentService.ControllerSlotLimit)
            {
                runtimeFeatures.SetRumble(0, 0, profileSettingsVM.FuncDevNum);
            }

            // 画面サイズ保持チェックが有効な場合のみレイアウト保存
            if (keepsize)
            {
                SaveSplitterAndColumnWidths();
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private enum RumbleType
        {
            Light,
            Heavy
        }

        private void RumbleTestBtn_Click(object sender, RoutedEventArgs e)
        {
            // 振動させるのは実機（FuncDevNum＝targetDevice、実機なしならコントローラー0）。
            // モーターの左右反転は、編集中の設定（作業スロット deviceNum）の値を読む。判断は RumbleTestController が行い、
            // ここは押したボタンの文字だけを変える
            var btn = sender as Button;
            bool heavyButton = btn.Name == "heavyRumbleTestBtn";
            var outcome = rumbleTest.Toggle(profileSettingsVM.Output, profileSettingsVM.FuncDevNum, heavyButton);
            if (outcome == RumbleTestOutcome.None) return;

            bool started = outcome == RumbleTestOutcome.Started;
            if (heavyButton)
                outputPanel.HeavyRumbleTestBtn.Content = started ? Properties.Resources.StopHText : Properties.Resources.TestHText;
            else
                outputPanel.LightRumbleTestBtn.Content = started ? Properties.Resources.StopLText : Properties.Resources.TestLText;
        }

        private void SixAxisPanel_CustomCurveEditorRequested(object sender, string target)
        {
            if (target == "SX") LaunchCurveEditor(profileSettingsVM.SixAxis.SXCustomCurve);
            else if (target == "SZ") LaunchCurveEditor(profileSettingsVM.SixAxis.SZCustomCurve);
        }

        private void LaunchCurveEditor(string customDefinition)
        {
            profileSettingsVM.LaunchCurveEditor(customDefinition);
        }

        private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Program (*.exe)|*.exe";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                profileSettingsVM.General.UpdateLaunchProgram(dialog.FileName);
            }
        }

        private void OutConTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox outConTypeCombo = outputPanel.OutConTypeCombo;
            if (outConTypeCombo == null)
                return;

            OutContType selected = OutContTypeInfo.FromProfileChoiceIndex(outConTypeCombo.SelectedIndex);

            if (profileSettingsVM != null)
            {
                profileSettingsVM.Output.ControllerTypeIndex = outConTypeCombo.SelectedIndex;
                profileSettingsVM.Output.TempControllerIndex = outConTypeCombo.SelectedIndex;
                profileSettingsVM.Output.TempConType = selected;
            }

            if (mappingListVM != null)
            {
                mappingListVM.UpdateMappingDevType(selected);
            }
        }
        private void NewActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (leftPaneEditor != null) return;
            ProfileList profList = (Application.Current.MainWindow as MainWindow).ProfileListHolder;
            AssignmentEditor actEditor = editorWindowFactory.CreateAssignmentEditor(deviceNum, profList, targetDevice, null);
            OpenLeftPaneEditor(actEditor);
            actEditor.Cancel += (sender2, args) =>
            {
                CloseLeftPaneEditor();
            };
            actEditor.Saved += (sender2, actionName) =>
            {
                SpecialAction action = specialActionsVM.GetActionOrPlaceholder(actionName);
                SpecialActionItem newitem = specialActionsVM.CreateActionItem(action);
                newitem.Active = true;
                int lastIdx = specialActionsVM.ActionCol.Count;
                newitem.Index = lastIdx;
                specialActionsVM.ActionCol.Add(newitem);
                CloseLeftPaneEditor();
                // After adding a new special action, re-apply the current sort
                // so the list reflects the user's chosen sort column and order.
                SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
                // Persistence and cache update deferred to Apply/Save to avoid
                // emitting removed-invalid logs at UI-time.
            };
        }

        // Phase9 UI 変更: SA の行をダブルクリックすると、「アクションを編集」と同じ編集画面を開く（Controls タブの一覧のダブルクリックと同じ）。
        // ヘッダー、空きの領域、「有効」のチェックボックスのダブルクリックは対象外
        private void SpecialActionsLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is System.Windows.Controls.ListViewItem) && !(source is System.Windows.Controls.Primitives.ButtonBase))
            {
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            if (!(source is System.Windows.Controls.ListViewItem)) return;

            EditActionBtn_Click(sender, e);
        }

        private void EditActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (leftPaneEditor != null) return;
            if (specialActionsVM.SpecialActionIndex >= 0)
            {
                SpecialActionItem item = specialActionsVM.CurrentSpecialActionItem;
                int currentIndex = item.Index;
                //int viewIndex = specialActionsVM.SpecialActionIndex;
                //int currentIndex = specialActionsVM.ActionCol[viewIndex].Index;
                //SpecialActionItem item = specialActionsVM.ActionCol[currentIndex];
                ProfileList profList = (Application.Current.MainWindow as MainWindow).ProfileListHolder;
                AssignmentEditor actEditor = editorWindowFactory.CreateAssignmentEditor(deviceNum, profList, targetDevice, item.SpecialAction);
                OpenLeftPaneEditor(actEditor);
                actEditor.Cancel += (sender2, args) =>
                {
                    CloseLeftPaneEditor();
                };
                actEditor.Saved += (sender2, actionName) =>
                {
                    string oldName = item.SpecialAction?.name;
                    string newName = actionName;

                    // Update current profile's references from oldName -> newName
                    try
                    {
                        var pa = profileRepository.ProfileActions; // List<string>[]
                        if (pa != null && pa.Length > deviceNum && pa[deviceNum] != null)
                        {
                            var list = pa[deviceNum];
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] == oldName)
                                {
                                    list[i] = newName;
                                }
                            }
                        }
                        profileSettingsVM.RefreshExtraProfileInfo();
                    }
                    catch { }

                    // Find the item index at runtime (guard against stale captured index)
                    int idx = specialActionsVM.ActionCol.IndexOf(item);
                    if (idx < 0)
                    {
                        idx = currentIndex;
                    }

                    DS4Windows.Core.Settings.SpecialAction action = specialActionsVM.GetActionOrPlaceholder(newName);
                    SpecialActionItem newitem = specialActionsVM.CreateActionItem(action);
                    newitem.Active = item.Active;
                    newitem.Index = idx;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (idx >= 0 && idx < specialActionsVM.ActionCol.Count)
                        {
                            specialActionsVM.ActionCol.RemoveAt(idx);
                            specialActionsVM.ActionCol.Insert(idx, newitem);
                        }
                        else
                        {
                            specialActionsVM.ActionCol.Add(newitem);
                        }
                    });

                    CloseLeftPaneEditor();
                    // After editing an existing special action, re-apply the
                    // current sort so any name/type changes are reflected
                    // according to the current sort column and direction.
                    SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);

                    // Phase6-Step10-3b-4: 名前が変わったときは、編集中以外も含むすべてのプロファイルの XML の参照を
                    // 新名へ置換し、書き換えたプロファイルを使っているスロットへ再適用する（MainWindow の 1 か所の経路）。
                    // 編集中のプロファイルのメモリ上の参照は、上で置換済み。
                    if (!string.IsNullOrEmpty(oldName) && !string.Equals(oldName, newName, StringComparison.Ordinal))
                    {
                        NotifyProfilesUpdated(profileRepository.ReplaceActionReference(oldName, newName));
                    }
                    else
                    {
                        // 名前が同じで内容だけ変わった場合は、プロファイルの XML の書き換えは不要。ただし、接続中のコントローラーは、
                        // プロファイルの適用時に取り込んだ古い内容のアクションを保持している（Actions.xml の読み直しでは更新されない）。
                        // そのアクションを使っているプロファイルを、保存時と同じ再適用の経路で再適用して、新しい内容を反映する。
                        NotifyProfilesUpdated(profileRepository.GetAppliedProfilesUsingAction(newName));
                    }
                };
            }
        }

        // 書き換えたプロファイルがあれば、MainWindow へ通知する（再適用のロジックは MainWindow の 1 か所だけ）
        private void NotifyProfilesUpdated(IReadOnlyList<string> changedProfiles)
        {
            if (changedProfiles != null && changedProfiles.Count > 0)
            {
                ProfilesUpdatedExternally?.Invoke(changedProfiles);
            }
        }

        // R-1: 選んだ SA を複製する。複製は無効（チェックなし）で一覧に加わる。保存（Actions.xml）は、複製の時点で行われる
        private void DuplicateActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (leftPaneEditor != null) return;
            if (specialActionsVM.SpecialActionIndex < 0) return;

            SpecialActionItem item = specialActionsVM.CurrentSpecialActionItem;
            if (item == null) return;

            SpecialActionItem newItem = specialActionsVM.DuplicateAction(item);
            if (newItem == null)
            {
                MessageBox.Show($"Special action \"{item.ActionName}\" can't be duplicated because it isn't saved in Actions.xml.",
                    "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
        }

        private void RemoveActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (specialActionsVM.SpecialActionIndex >= 0)
            {
                SpecialActionItem item = specialActionsVM.CurrentSpecialActionItem;
                //int currentIndex = specialActionsVM.ActionCol[specialActionsVM.SpecialActionIndex].Index;
                //SpecialActionItem item = specialActionsVM.ActionCol[currentIndex];
                if (item == null) return;

                // Phase6-Step10-3b-3: 確認のうえ、Actions.xml と、すべてのプロファイルの XML から取り除く
                MessageBoxResult confirm = MessageBox.Show(
                    $"Remove special action \"{item.ActionName}\"?\n\n" +
                    "It will also be removed from every profile.",
                    "DS4Windows", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (confirm != MessageBoxResult.Yes) return;

                string removedName = item.ActionName;
                specialActionsVM.RemoveAction(item);
                NotifyProfilesUpdated(profileRepository.RemoveActionReference(removedName));
            }
        }

        private void SpecialActionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Phase 5 Step 14: ViewModel 側の ProfileActions 文字列を即時再同期
            if (specialActionsVM != null)
            {
                specialActionsVM.CommitEnabledActionsToProfile();
            }
        }
        private void Ds4LightbarColorBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            highlightControlDisplayLb.Content = "Click the lightbar for color picker";
        }

        private void Ds4LightbarColorBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            highlightControlDisplayLb.Content = "";
        }

        private void Ds4LightbarColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            Color tempcolor = profileSettingsVM.Lightbar.MainColor;
            dialog.colorPicker.SelectedColor = tempcolor;
            profileSettingsVM.Lightbar.StartForcedColor(tempcolor);
            dialog.ColorChanged += (sender2, color) =>
            {
                profileSettingsVM.Lightbar.UpdateForcedColor(color);
            };
            dialog.ShowDialog();
            profileSettingsVM.Lightbar.EndForcedColor();
            profileSettingsVM.Lightbar.UpdateMainColor(dialog.colorPicker.SelectedColor.GetValueOrDefault());
        }

        private void InputDS4(object sender, System.Timers.ElapsedEventArgs e)
        {
            inputTimer.Stop();

            bool activeWin = false;
            int tempDeviceNum = 0;
            Dispatcher.Invoke(() =>
            {
                activeWin = Application.Current.MainWindow.IsActive;
                tempDeviceNum = profileSettingsVM.FuncDevNum;
            });

            if (activeWin && profileSettingsVM.General.UseControllerReadout)
            {
                int index = -1;
                // Phase5-Step15-2-d: 281行目で解決済みのcontrolServiceフィールドを使わず、
                // ここだけProgram.rootHubを直接呼んでいた置換漏れを修正。
                switch (runtimeFeatures.GetActiveInputControl(tempDeviceNum))
                {
                    case DS4Controls.None: break;
                    case DS4Controls.Cross: index = 0; break;
                    case DS4Controls.Circle: index = 1; break;
                    case DS4Controls.Square: index = 2; break;
                    case DS4Controls.Triangle: index = 3; break;
                    case DS4Controls.Options: index = 4; break;
                    case DS4Controls.Share: index = 5; break;
                    case DS4Controls.DpadUp: index = 6; break;
                    case DS4Controls.DpadDown: index = 7; break;
                    case DS4Controls.DpadLeft: index = 8; break;
                    case DS4Controls.DpadRight: index = 9; break;
                    case DS4Controls.PS: index = 10; break;
                    case DS4Controls.Mute: index = 11; break;
                    case DS4Controls.L1: index = 12; break;
                    case DS4Controls.R1: index = 13; break;
                    case DS4Controls.L2: index = 14; break;
                    case DS4Controls.R2: index = 15; break;
                    case DS4Controls.L3: index = 16; break;
                    case DS4Controls.R3: index = 17; break;
                    case DS4Controls.TouchLeft: index = 18; break;
                    case DS4Controls.TouchRight: index = 19; break;
                    case DS4Controls.TouchMulti: index = 20; break;
                    case DS4Controls.TouchUpper: index = 21; break;
                    case DS4Controls.LYNeg: index = 22; break;
                    case DS4Controls.LYPos: index = 23; break;
                    case DS4Controls.LXNeg: index = 24; break;
                    case DS4Controls.LXPos: index = 25; break;
                    case DS4Controls.RYNeg: index = 26; break;
                    case DS4Controls.RYPos: index = 27; break;
                    case DS4Controls.RXNeg: index = 28; break;
                    case DS4Controls.RXPos: index = 29; break;
                    case DS4Controls.FnL: index = 30; break;
                    case DS4Controls.FnR: index = 31; break;
                    case DS4Controls.BLP: index = 32; break;
                    case DS4Controls.BRP: index = 33; break;
                    default: break;
                }

                if (index >= 0 && leftPaneEditor == null)
                {
                    Dispatcher.BeginInvoke((System.Action)(() =>
                    {
                        if (leftPaneEditor != null) return;
                        mappingListVM.SelectedIndex = index;
                        ShowControlBindingWindow();
                    }));
                }
            }

            if (profileSettingsVM.General.UseControllerReadout)
            {
                inputTimer.Start();
            }
        }
        private void ProfileEditor_Closed(object sender, EventArgs e)
        {
            // 画面サイズ保持チェックが有効な場合のみレイアウト保存
            if (keepsize)
            {
                SaveSplitterAndColumnWidths();
            }
            profileSettingsVM.General.UseControllerReadout = false;
            inputTimer.Stop();
            conReadingsUserCon.EnableControl(false);
            // Rebuild cached profile info for this device. CalculateProfileActionDicts
            // no longer emits missing-action logs directly; callers should invoke
            // EmitMissingActionLogsForDevice when they want messages emitted.
            try
            {
                profileSettingsVM.RefreshExtraProfileInfo();
            }
            catch { }
            UnregisterEvents();
        }

        private void UseControllerReadoutCk_Click(object sender, RoutedEventArgs e)
        {
            // Phase6-Step7b: 判定は編集スロット（常に 8）ではなく実機の有無で行う（一覧経由では従来どおり開始しない）
            if (profileSettingsVM.General.UseControllerReadout && HasTargetDevice)
            {
                inputTimer.Start();
            }
            else
            {
                inputTimer.Stop();
            }
        }

        // Phase10-Step3 W4: 左ペイン全体（タブ）に重なって表示されている編集画面（Controls の割り当てエディター、SA の編集画面）。
        // 表示されているあいだは null でない。開いているあいだは、新しく開かない（コントローラーの入力［タイマー］と、右ペインに残る入口からの二重起動を防ぐ）。
        // TODO(Phase10-Step3b): 入口をすべて左ペインのタブのボタンにする（Axis Config の 4 つの入口は、Controls タブへボタンとして追加）。
        //   そのあとも、この判定は、コントローラーの入力（タイマー）経由の安全装置として残す
        private UserControl leftPaneEditor;

        /// <summary>編集画面を、左ペイン全体に重ねて表示する（タブを隠すため、編集中は、左ペインのタブを切り替えられない）。</summary>
        private void OpenLeftPaneEditor(UserControl editor)
        {
            // 左の列の幅は Auto（中身で決まる）ため、タブを隠しても、列の幅が変わらないよう、いまの幅に固定する
            controlEditorHost.Width = sidebarTabControl.ActualWidth;
            leftPaneEditor = editor;
            sidebarTabControl.Visibility = Visibility.Collapsed;
            controlEditorHost.Children.Add(editor);
            editor.Visibility = Visibility.Visible;
            controlEditorHost.Visibility = Visibility.Visible;
        }

        private void CloseLeftPaneEditor()
        {
            if (leftPaneEditor == null) return;

            controlEditorHost.Children.Remove(leftPaneEditor);
            leftPaneEditor = null;
            controlEditorHost.Visibility = Visibility.Collapsed;
            controlEditorHost.ClearValue(WidthProperty);
            sidebarTabControl.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 割り当てエディターを、左ペイン全体に重ねて表示する。保存したら、従来と同じ更新（割り当て名、ハイライトのラベル、カスタムの印）を行う。
        /// キャンセルは、何もしない（編集用のコピーを捨てる）。従来の選択ダイアログ（BindingWindow の Full のモード）は、使わない。
        /// </summary>
        private void ShowControlAssignmentEditor(MappedControl mpControl, bool updateHighlight)
        {
            if (mpControl == null || leftPaneEditor != null) return;

            ProfileList profList = (Application.Current.MainWindow as MainWindow).ProfileListHolder;
            AssignmentEditor editor = editorWindowFactory.CreateControlActionEditor(deviceNum, mpControl.Setting, profList, targetDevice);
            editor.ControlSaved += (sender2, args) =>
            {
                CloseLeftPaneEditor();
                mpControl.UpdateMappingName();
                if (updateHighlight)
                {
                    UpdateHighlightLabel(mpControl);
                }

                profileSettingsVM.RefreshCustomsFlags();
            };
            editor.Cancel += (sender2, args) => CloseLeftPaneEditor();

            OpenLeftPaneEditor(editor);
        }

        private void ShowControlBindingWindow()
        {
            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            ShowControlAssignmentEditor(mpControl, updateHighlight: true);
        }

        // Phase9 UI 変更: Controls タブの全ての割り当て（L2／R2 Full Pull、スティックの Outer Bind を含む一覧の全項目）を Default に戻す。
        // Extras、マクロも含めて、そのボタンの設定を初期化する（DS4ControlSettings.Reset）。プロファイルは Save するまで保存されない
        private void ResetAllControlsBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = MessageBox.Show(
                DS4Windows.Translations.Strings.ResetAllControlsConfirm,
                "DS4Windows", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes) return;

            foreach (MappedControl mpControl in mappingListVM.Mappings)
            {
                mpControl.Setting.Reset();
                mpControl.UpdateMappingName();
            }

            profileSettingsVM.RefreshCustomsFlags();
            highlightControlDisplayLb.Content = "";
            AppLogger.LogUser(AppLogLevel.Info, "Log_Editor_ControlsReset");
        }

        private void MappingListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (mappingListVM.SelectedIndex >= 0)
            {
                ShowControlBindingWindow();
            }
        }

        private void SidebarTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Phase10-Step3b W1: 左ペインのタブの中の ComboBox や入れ子の TabControl（Touchpad・ジャイロの Output Mode ほか）の選択変更も、
            // このイベントとして届く。左ペインのタブ自身の選択変更だけを扱う
            if (e.OriginalSource != sidebarTabControl) return;

            AppLogger.Log(AppLogLevel.Debug, $"[SidebarTabControl_SelectionChanged] SelectedItem={sidebarTabControl.SelectedItem?.GetType().Name}, SelectedIndex={sidebarTabControl.SelectedIndex}, IsLoaded={sidebarTabControl.IsLoaded}, IsVisible={sidebarTabControl.IsVisible}, Added={e.AddedItems?.Count}, Removed={e.RemovedItems?.Count}");
            // 特殊アクションのタブを最初に開いたときに、一度だけ並べ替えを適用する
            if (!specialActionsSorted && sidebarTabControl.SelectedItem == specialActionsTab)
            {
                specialActionsSorted = true;
                SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
            }

            if (sidebarTabControl.SelectedItem == contReadingsTab)
            {
                controllerReadingsTabActive = true;
                conReadingsUserCon.EnableControl(true);
            }
            else if (controllerReadingsTabActive)
            {
                controllerReadingsTabActive = false;
                conReadingsUserCon.EnableControl(false);
            }
        }

        private void ConBtn_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Button btn = sender as Button;
            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            profileSettingsVM.PresetMenuUtil.SetHighlightControl(mpControl.Control);
            ContextMenu cm = conCanvas.FindResource("presetMenu") as ContextMenu;
            MenuItem temp = cm.Items[0] as MenuItem;
            temp.Header = profileSettingsVM.PresetMenuUtil.PresetInputLabel;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
        }

        private void PresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 項目ごとの Click ではなく、ContextMenu で受ける（MenuItem.Click は上へ伝わる）。
            // 項目を ContextMenu のリソースに置くと、コンパイル時の接続番号と読み込み時の順序がずれて、画面を開く時に落ちるため。
            MenuItem item = e.OriginalSource as MenuItem;
            if (item == null || item.HasItems)
                return;
            int baseTag = Convert.ToInt32(item.Tag);
            int subTag = Convert.ToInt32(item.CommandParameter);
            if (baseTag >= 0 && subTag >= 0)
            {
                List<DS4Controls> controls =
                    profileSettingsVM.PresetMenuUtil.ModifySettingWithPreset(baseTag, subTag);
                foreach (DS4Controls control in controls)
                {
                    MappedControl mpControl = mappingListVM.ControlMap[control];
                    mpControl.UpdateMappingName();
                }

                profileSettingsVM.RefreshCustomsFlags();
                highlightControlDisplayLb.Content = "";
            }
        }

        private void PresetBtn_Click(object sender, RoutedEventArgs e)
        {
            sidebarTabControl.SelectedIndex = 0;

            PresetOptionWindow presetWin = editorWindowFactory.CreatePresetOptionWindow();
            presetWin.SetupData(deviceNum);
            presetWin.ToPresetsScreen();
            presetWin.DelayPresetApply = true;
            presetWin.ShowDialog();

            if (presetWin.Result == MessageBoxResult.OK)
            {
                StopEditorBindings();
                presetWin.ApplyPreset();
                RefreshEditorBindings();
            }
        }

        private void GyroCalibration_Click(object sender, EventArgs e)
        {
            // Phase6-Step7b: 校正するのは実機（FuncDevNum＝targetDevice、実機なしならコントローラー0）。
            // 編集スロットのフィールド deviceNum と区別するため、ローカル変数名を分けた
            int calibrationDevice = profileSettingsVM.FuncDevNum;
            if (calibrationDevice < environmentService.ControllerSlotLimit)
            {
                // 未接続のスロットでは何もしない（以前は null の機器を触って NullReferenceException でアプリが終了していた）
                slotCommands.ResetGyroCalibration(calibrationDevice);
            }
        }

        private void CalibrateStick_OnClick(object sender, int tag)
        {
            // Phase6-Step7b: 校正には実機が必要。旧判定 deviceNum == 8（一覧経由）は、編集スロットが常に 8 になったため
            // 実機の有無（targetDevice）の判定に置き換えた。メッセージの文言は維持
            if (!HasTargetDevice)
            {
                MessageBox.Show("Stick recalibration is only available if the profile editor is opened " +
                                "with the Edit button next to the controller you want to recalibrate in the main " +
                                "window.",
                    "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var stick = tag switch
            {
                0 => Stick.Left,
                1 => Stick.Right,
                _ => throw new IndexOutOfRangeException("Wrong stick index. Must be 0 for left or 1 for right.")
            };

            // 実機（targetDevice）の現在の入力を読み、補正値は profileSettingsVM 経由で作業スロットへ書く
            StickCalibrationWindow window = editorWindowFactory.CreateStickCalibrationWindow(stick, targetDevice, profileSettingsVM);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void ExportSpecialActions_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".xml";
            dialog.FileName = "Actions";
            dialog.Filter = "DS4Windows Special Actions (*.xml)|*.xml";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var profileStream = new StreamReader(specialActionsVM.ActionsFilePath).BaseStream;
                var dialogStream = dialog.OpenFile();
                profileStream.CopyTo(dialogStream);
                profileStream.Close();
                dialogStream.Close();
            }
        }

        // ソート状態（列名・方向）は SpecialActionsListViewModel.CurrentSortColumn /
        // CurrentSortAscending が唯一の実体。View 側では保持しない（二重状態の解消）。

        // 列ヘッダークリックイベント
        void SpecialActionsHeader_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var col = btn?.Tag as string;
            if (col != null)
            {
                string currentCol = specialActionsVM?.CurrentSortColumn ?? "Name";
                bool currentAsc = specialActionsVM?.CurrentSortAscending ?? true;
                bool asc = col != currentCol ? true : !currentAsc;
                SortSpecialActionsList(col, asc);
            }
        }
    } // ← ProfileEditorクラスの閉じ括弧を追加
}