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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;
namespace DS4Windows.Core.Settings
{

    public partial class BackingStore
    {
        // ProfileEditor layout fields (DTOと統合)
        public const int DEFAULT_PROFILE_EDITOR_LEFT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_LEFT_WIDTH;
        public const int DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_RIGHT_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_NAME_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_DETAIL_COL_WIDTH;
        // Active 列はプロファイルに永続化しない列幅の初期値
        public const int DEFAULT_SPECIAL_ACTION_ACTIVE_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_ACTIVE_COL_WIDTH;
        // SpecialActionDeleteColWidth 定数削除

        // Controller tab column widths
        public const int DEFAULT_CONTROLLER_INDEX_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_INDEX_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_STATUS_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_STATUS_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_BATTERY_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_BATTERY_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EDIT_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EDIT_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_CUSTOMCOLOR_COL_WIDTH;
        public const int DEFAULT_OUTPUT_SLOT_NUM_COL_WIDTH = WindowLayoutDefaults.OUTPUT_SLOT_NUM_COL_WIDTH;
        public const int DEFAULT_OUTPUT_SLOT_INPUT_NUM_COL_WIDTH = WindowLayoutDefaults.OUTPUT_SLOT_INPUT_NUM_COL_WIDTH;
        public const int DEFAULT_OUTPUT_SLOT_INPUT_ID_COL_WIDTH = WindowLayoutDefaults.OUTPUT_SLOT_INPUT_ID_COL_WIDTH;
        public const int DEFAULT_OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH = WindowLayoutDefaults.OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH;
        public const int DEFAULT_OUTPUT_SLOT_RESERVATION_COL_WIDTH = WindowLayoutDefaults.OUTPUT_SLOT_RESERVATION_COL_WIDTH;
        public const int DEFAULT_AUTO_PROFILE_NAME_COL_WIDTH = WindowLayoutDefaults.AUTO_PROFILE_NAME_COL_WIDTH;
        public const int DEFAULT_AUTO_PROFILE_PATH_COL_WIDTH = WindowLayoutDefaults.AUTO_PROFILE_PATH_COL_WIDTH;
        public const int DEFAULT_AUTO_PROFILE_TITLE_COL_WIDTH = WindowLayoutDefaults.AUTO_PROFILE_TITLE_COL_WIDTH;
        public const int DEFAULT_LOG_TIME_COL_WIDTH = WindowLayoutDefaults.LOG_TIME_COL_WIDTH;
        public const int DEFAULT_LOG_DATA_COL_WIDTH = WindowLayoutDefaults.LOG_DATA_COL_WIDTH;

        // Log settings
        public const int DEFAULT_LOG_MAX_ARCHIVE_FILES = 50;
        public const string DEFAULT_LOG_MIN_LEVEL = "Info";

        /// <summary>画面の配置（ウィンドウの位置・大きさ、エディターの幅、一覧の列の幅）。</summary>
        public LayoutSettings Layout { get; } = new LayoutSettings();
        /// <summary>ログの保存の設定。</summary>
        public LogSettings Log { get; } = new LogSettings();
        /// <summary>更新の確認の設定。</summary>
        public UpdateCheckSettings UpdateCheck { get; } = new UpdateCheckSettings();
        /// <summary>通知・起動・動作の設定。</summary>
        public BehaviorSettings Behavior { get; } = new BehaviorSettings();
        /// <summary>OSC・UDP・Steam フォルダーの設定。</summary>
        public NetworkSettings Network { get; } = new NetworkSettings();
        /// <summary>言語・アイコン・テーマの設定。</summary>
        public AppearanceState Appearance { get; } = new AppearanceState();
        // SpecialActionDeleteColWidth フィールド削除

        public const double DEFAULT_UDP_SMOOTH_MINCUTOFF = 0.4;
        public const double DEFAULT_UDP_SMOOTH_BETA = 0.2;
        // Use 15 minutes for default Idle Disconnect when initially enabling the option
        public const int DEFAULT_ENABLE_IDLE_DISCONN_MINS = 15;
        public const double DEFAULT_SX_TILT_DEADZONE = 0.25;
        public const double DEFAULT_SX_TILT_MAXZONE = 1.0;
        public const string DEFAULT_SA_TRIGGERS = "-1";
        public const string DEFAULT_GYRO_MSTICK_TRIGGERS = "-1";
        public const OutContType DEFAULT_OUT_CONT_TYPE = OutContTypeInfo.DefaultType;
        public const bool DEFAULT_OUTPUT_TO_DS4 = true;
        public const bool DEFAULT_TOUCH_TOGGLE = true;
        public const bool DEFAULT_TOUCHPAD_JITTER_COMP = true;
        public const int DEFAULT_TOUCHPAD_SENS = 100;
        public const int DEFAULT_DS4_BT_POLL_RATE = 4;
        public const int DEFAULT_RUMBLE = 100;
        public const double DEFAULT_ANALOG_SENS = 1.0;
        public const bool DEFAULT_DINPUT_ONLY = false;
        public const TouchpadOutMode DEFAULT_TOUCH_OUT_MODE = TouchpadOutMode.Mouse;
        public const GyroOutMode DEFAULT_GYRO_OUT_MODE = GyroOutMode.Controls;
        public const bool DEFAULT_SA_TRIGGER_COND = true;
        public const bool DEFAULT_SA_MSTICK_TRIGGER_COND = true;
        public const bool DEFAULT_GYRO_TRIGGER_TURNS = true;
        public const bool DEFAULT_GYRO_MSTICK_TRIGGER_TURNS = true;
        public const int DEFAULT_GYRO_SENS = 100;
        public const int DEFAULT_GYRO_SENS_VERTICAL_SCALE = 100;
        public const int DEFAULT_TOUCH_DIS_INVERT_TRIGGER = -1;
        public const bool DEFAULT_TRACKBALL_MODE = false;
        public const double DEFAULT_TRACKBALL_FRICTION = 10.0;

        // Stick output curve consts in place more as a precaution
        // DEFAULT_STICK_OUTPUT_CURVE／_ID、DEFAULT_SA_OUTPUT_CURVE／_ID は削除した（廃止済みの ProfileDTO の既定値の残骸。参照 0 件）。

        public String m_Profile = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName + "\\Profiles.xml";   // 実行中のアセンブリの場所には、親のフォルダーがある
        public String m_Actions = PathService.AppDataPathState + "\\Actions.xml";
        public string m_linkedProfiles = PathService.AppDataPathState + "\\LinkedProfiles.xml";
        public string m_controllerConfigs = PathService.AppDataPathState + "\\ControllerConfigs.xml";

        // 作業用の XmlDocument（m_Xdoc）は、読み書きを担う各シリアライザーが自分のフィールドとして持つ。BackingStore からは削除した。
        // ninth (fifth in old builds) value used for options, not last controller
        public ButtonMouseInfo[] buttonMouseInfos = new ButtonMouseInfo[ControllerLimits.TestProfileItemCount]
        {
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
        };

        /// <summary>基本的な設定（タッチパッドの切り替え、無操作での切断、出力データ、BT のポーリング、起動プログラム、DInput のみ）の持ち主（スロットごとに 1 つ）。</summary>
        public readonly BasicSlotSettings[] basicSlots = new BasicSlotSettings[ControllerLimits.TestProfileItemCount];

        public ProfileSlotNames profilePath = new ProfileSlotNames(ControllerLimits.TestProfileItemCount);
        public ProfileSlotNames olderProfilePath = new ProfileSlotNames(ControllerLimits.TestProfileItemCount);
        public ProfileSlotNames selectedProfile = new ProfileSlotNames(ControllerLimits.TestProfileItemCount);
        // 上の 3 つ（現在・直前・選択のプロファイル）は、保管がプロファイルの固定 ID で、名前を読み書きする窓口（ProfileSlotNames）。
        // 登録簿は、DI の登録が結ぶ（登録簿が BackingStore に依存しない向き）。結ばなければ、名前の控えだけで、従来の配列と同じに動く
        public void AttachProfileIdentity(DS4Windows.Core.Abstractions.IProfileIdentityService identity)
        {
            profilePath.AttachIdentity(identity);
            olderProfilePath.AttachIdentity(identity);
            selectedProfile.AttachIdentity(identity);
            linkedProfileUI.AttachIdentity(identity);   // Phase10-Step4d W5d
            linkedProfiles.AttachIdentity(identity);
        }
        public ProfileSlotNames linkedProfileUI = new ProfileSlotNames(ControllerLimits.TestProfileItemCount);
        // プロファイル切り替え時の無効アクション名エラーログ抑制用
        public HashSet<string> loggedInvalidActions = new HashSet<string>();
        public bool[] firstConnectionAfterStartup = new bool[ControllerLimits.TestProfileItemCount] { true, true, true, true, true, true, true, true, true };
        public LinkedProfileMap linkedProfiles = new LinkedProfileMap();   // 値は、プロファイルの名前を読み書きする窓口（保管は ID）
        // Cache properties instead of performing a string comparison every frame
        public bool[] distanceProfiles = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };
        /// <summary>振動の設定（強さ、自動停止、モーターの入れ替え）の持ち主（スロットごとに 1 つ）。</summary>
        public readonly RumbleSlotSettings[] rumbleSlots = new RumbleSlotSettings[ControllerLimits.TestProfileItemCount];

        // Rotation angle expressed in radians

        // Rotation angle expressed in radians

        /// <summary>DualSense 固有の設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly DualSenseSlotSettings[] dualSenseSlots = new DualSenseSlotSettings[ControllerLimits.TestProfileItemCount];

        public const int DEFAULT_FORM_WIDTH = WindowLayoutDefaults.MAIN_WINDOW_WIDTH;

        public const int DEFAULT_FORM_HEIGHT = WindowLayoutDefaults.MAIN_WINDOW_HEIGHT;

        public const int DEFAULT_FORM_LOCATION_X = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_X;

        public const int DEFAULT_FORM_LOCATION_Y = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_Y;

        // Legacy CheckWhen removed. Store explicit defaults for the new fields.
        public const int DEFAULT_CHECK_EVERY_VALUE = 1;
        public const int DEFAULT_CHECK_EVERY_UNIT = 1;

        public const int DEFAULT_NOTIFICATIONS = 2;

        public const bool DEFAULT_SWIPE_PROFILES = false;
        public List<SpecialAction> actions = new List<SpecialAction>();
        /// <summary>
        /// 廃止された「シフト」機能の設定（プロファイルの &lt;ShiftControl&gt;）を、解釈せずに保持する。スロットごと。
        /// 読込のたびに取り込み直し（<see cref="RemovedShiftSettings.Capture"/>）、保存で、そのまま書き出す（<see cref="RemovedShiftSettings.AppendTo"/>）。
        /// </summary>
        public readonly RemovedShiftSettings[] removedShiftSettings = new RemovedShiftSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>
        /// 廃止された「ジャイロスワイプ」機能の設定（<c>&lt;GyroSwipeSettings&gt;</c>、<c>&lt;Control&gt;</c> 内の <c>GyroSwipe*</c> の割り当て、
        /// ジャイロの出力モード <c>DirectionalSwipe</c>）を、解釈せずに保持する。スロットごと。読込のたびに取り込み直し、保存で、そのまま書き出す。
        /// </summary>

        /// <summary>ジャイロの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly GyroSlotSettings[] gyroSlots = new GyroSlotSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>Touchpadの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly TouchpadSlotSettings[] touchpadSlots = new TouchpadSlotSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>Triggersの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly TriggerSlotSettings[] triggerSlots = new TriggerSlotSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>Sticksの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly StickSlotSettings[] stickSlots = new StickSlotSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>Lightbarの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly LightbarSlotSettings[] lightbarSlots = new LightbarSlotSettings[ControllerLimits.TestProfileItemCount];

        /// <summary>SixAxisの設定の持ち主（スロットごとに 1 つ）。</summary>
        public readonly SixAxisSlotSettings[] sixAxisSlots = new SixAxisSlotSettings[ControllerLimits.TestProfileItemCount];

        public List<DS4ControlSettings>[] ds4settings = new List<DS4ControlSettings>[ControllerLimits.TestProfileItemCount]
            { new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(),
              new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>() };
        public ControlSettingsGroup[] ds4controlSettings;

        public List<string>?[] profileActions = new List<string>?[ControllerLimits.TestProfileItemCount] { null, null, null, null, null, null, null, null, null };
        public int[] profileActionCount = new int[ControllerLimits.TestProfileItemCount] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Dictionary<string, SpecialAction>[] profileActionDict = new Dictionary<string, SpecialAction>[ControllerLimits.TestProfileItemCount]
            { new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(),
              new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>() };

        public Dictionary<string, int>[] profileActionIndexDict = new Dictionary<string, int>[ControllerLimits.TestProfileItemCount]
            { new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(),
              new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>() };

        public const bool DEFAULT_FLASH_WHEN_LATE = true;

        public const int DEFAULT_FLASH_WHEN_LATE_AT = 500;

        public const int DEFAULT_OSC_SERV_PORT = 9000;

        public const int DEFAULT_OSC_SEND_PORT = 9001;

        public const string DEFAULT_OSC_SEND_ADDRESS = "127.0.0.1";

        public const int DEFAULT_UDP_SERV_PORT = 26760;

        // 127.0.0.1=IPAddress.Loopback (default), 0.0.0.0=IPAddress.Any as all interfaces, x.x.x.x = Specific ipv4 interface address or hostname
        public const string DEFAULT_UDP_SERV_LISTEN_ADDR = "127.0.0.1";

        // debouncingMs（デバウンス時間）は機能廃止で削除した

        public ControlServiceDeviceOptions deviceOptions =
            new ControlServiceDeviceOptions();

        // Cache whether profile has custom action
        public bool[] containsCustomAction = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };

        // Cache whether profile has custom extras
        public bool[] containsCustomExtras = new bool[ControllerLimits.TestProfileItemCount] { false, false, false, false, false, false, false, false, false };

        //public bool[] touchStickTrackballMode = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        //public double[] touchStickTrackballFriction = new double[Global.TEST_PROFILE_ITEM_COUNT] { 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0 };

        /// <summary>出力の種類（保存される仮想コントローラーの種類、仮想のトリガーボタン、DS4 のトリガーの出力方式）の持ち主（スロットごとに 1 つ）。</summary>
        public readonly OutputTypeSlotSettings[] outputTypeSlots = new OutputTypeSlotSettings[ControllerLimits.TestProfileItemCount];

        public const bool DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS = true;

        public const DS4TriggerOutputMode DEFAULT_DS4_TRIGGER_OUTPUT = DS4TriggerOutputMode.Default;

        // TRUE=AutoProfile reverts to default profile if current foreground process is unknown, FALSE=Leave existing profile active when a foreground proces is unknown (ie. no matching auto-profile rule)
        public const bool DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE = true;
        public AutoProfileDisplayProfileSwitchChoices autoProfileSwitchNotifyChoice =
            AutoProfileDisplayProfileSwitchChoices.None;

        bool tempBool = false;

        // 旧 Global.activeOutDevType。スロットごとの、現在接続中の出力種別（実体は OutputSlotService が持つ）。
        // ServiceRegistration の IOutputSlotService の登録が、OutputSlotService の生成時に ActiveOutDevType の配列を設定する。
        // 設定前（コンテナ構築前の起動処理、コンテナを通らないテスト）は、全スロット None の既定の配列を読む（接続前は元々 None）。
        // 書き込むのは ControlService だけで、既定の配列は書き換わらない。配列の参照は再代入されない前提で使うこと。
        // コンストラクター注入にしない理由 → 配列を持つ OutputSlotService は BackingStore の後に作られ、BackingStore の登録側から解決できない（循環）。
        public OutContType[] ActiveOutDevType { get; set; } = new OutContType[ControllerLimits.TestProfileItemCount];

        // 旧 Global.ProfileSettingsServiceInstance（静的に最初のコンテナの実体を保持していた）の置き換え。
        // 一時プロファイル・DInput のみの配列と、出力 KBM のマッピング（RefreshActionAlias が使う）は ProfileSettingsService が持つ。
        // ProfileSettingsService が BackingStore を受け取る（逆向きの依存）ため、コンストラクターでは渡せない。ProfileSettingsService が
        // 自分の生成時に LinkProfileSettings で登録する（最初の 1 つが有効）。
        // 登録がないまま使われたときのフォールバック（コントローラーを持たない ProfileSettingsService を自分で作る）は撤去した。
        // 通常の起動では、プロファイルの読込より前に BuildHostAndServices の CreateControlService が DI の ProfileSettingsService を結ぶため通らない。
        // 登録がないまま使われたら、組み立ての誤りとして例外にする。テストは明示的に設定する。
        private readonly object _profileSettingsLock = new object();
        private DS4Windows.Core.Abstractions.IProfileSettingsService? _profileSettings;

        /// <summary>
        /// 設定ファイル（Profiles.xml）へ保存するスロットのプロファイル名。一時プロファイル（CLI の LoadTempProfile、自動プロファイル）を使っている間は、
        /// 一時の名前ではなく通常のプロファイル（profilePath）を保存する（次回の起動の既定が、一時の設定にならないように）。
        /// </summary>
        public string GetPersistedProfileName(int slot)
        {
            var linked = _profileSettings;
            string normal = profilePath[slot];
            bool temp = linked != null && linked.GetUseTempProfile(slot);
            return temp && !string.IsNullOrEmpty(normal) ? normal : selectedProfile[slot];
        }

        public DS4Windows.Core.Abstractions.IProfileSettingsService ProfileSettings
        {
            get
            {
                var linked = _profileSettings;
                if (linked != null)
                    return linked;

                lock (_profileSettingsLock)
                {
                    if (_profileSettings != null)
                        return _profileSettings;
                }

                const string message = "[DI] BackingStore.ProfileSettings: no IProfileSettingsService is linked (create ProfileSettingsService for this store first)";
                AppLogger.Log(AppLogLevel.Error, message);
                throw new InvalidOperationException(message);
            }
            set
            {
                lock (_profileSettingsLock) { _profileSettings = value; }
            }
        }

        /// <summary>ProfileSettingsService が、自分の生成時に呼ぶ。まだ登録がないときだけ登録する（最初の 1 つが有効）。</summary>
        internal void LinkProfileSettings(DS4Windows.Core.Abstractions.IProfileSettingsService service)
        {
            lock (_profileSettingsLock)
            {
                if (_profileSettings == null)
                    _profileSettings = service;
            }
        }

        // スロット数の上限は、静的な _environmentService.ControllerSlotLimit ではなく、注入された環境サービスから読む。
        private readonly DS4Windows.Core.Abstractions.IEnvironmentService _environmentService;

        public BackingStore(DS4Windows.Core.Abstractions.IEnvironmentService environmentService)
        {
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ds4controlSettings = new ControlSettingsGroup[ControllerLimits.TestProfileItemCount];
            for (int i = 0; i < gyroSlots.Length; i++) gyroSlots[i] = new GyroSlotSettings(i, environmentService);
            for (int i = 0; i < sixAxisSlots.Length; i++) sixAxisSlots[i] = new SixAxisSlotSettings(i);
            for (int i = 0; i < basicSlots.Length; i++) basicSlots[i] = new BasicSlotSettings(i);
            for (int i = 0; i < rumbleSlots.Length; i++) rumbleSlots[i] = new RumbleSlotSettings(i);
            for (int i = 0; i < outputTypeSlots.Length; i++) outputTypeSlots[i] = new OutputTypeSlotSettings(i);
            for (int i = 0; i < dualSenseSlots.Length; i++) dualSenseSlots[i] = new DualSenseSlotSettings(i);
            for (int i = 0; i < lightbarSlots.Length; i++) lightbarSlots[i] = new LightbarSlotSettings(i);
            for (int i = 0; i < stickSlots.Length; i++) stickSlots[i] = new StickSlotSettings(i);
            for (int i = 0; i < triggerSlots.Length; i++) triggerSlots[i] = new TriggerSlotSettings(i);
            for (int i = 0; i < touchpadSlots.Length; i++) touchpadSlots[i] = new TouchpadSlotSettings(i);

            for (int i = 0; i < ControllerLimits.TestProfileItemCount; i++)
            {
                foreach (DS4Controls dc in Enum.GetValues(typeof(DS4Controls)))
                {
                    if (dc != DS4Controls.None)
                        ds4settings[i].Add(new DS4ControlSettings(dc));
                }

                ds4controlSettings[i] = new ControlSettingsGroup(ds4settings[i]);
                removedShiftSettings[i] = new RemovedShiftSettings();

                EstablishDefaultSpecialActions(i);
                CacheExtraProfileInfo(i);
            }

            SetupDefaultColors();
        }
        public void EstablishDefaultSpecialActions(int idx)
        {
            // Do not overwrite an existing profileActions list. Only seed a default
            // when the list is missing (null). This ensures we only add the default
            // Disconnect action for truly new/unspecified profiles and won't force-enable
            // it for existing profiles on startup.
            if (profileActions[idx] != null)
            {
                profileActionCount[idx] = profileActions[idx]!.Count;   // 直前で null でないことを確かめている
                return;
            }

            // Initialize to an empty list for unspecified profiles but do NOT
            // automatically add any Disconnect action. That behavior caused
            // unwanted enabling of Disconnect for profiles that simply had no
            // explicit action configuration.
            profileActions[idx] = new List<string>();
        }

        public string StickOutputCurveString(int id)
        {
            string result = "linear";
            switch (id)
            {
                case 0: break;
                case 1: result = "enhanced-precision"; break;
                case 2: result = "quadratic"; break;
                case 3: result = "cubic"; break;
                case 4: result = "easeout-quad"; break;
                case 5: result = "easeout-cubic"; break;
                case 6: result = "custom"; break;
                default: break;
            }

            return result;
        }

        public int StickOutputCurveId(string name)
        {
            int id = 0;
            switch (name)
            {
                case "linear": id = 0; break;
                case "enhanced-precision": id = 1; break;
                case "quadratic": id = 2; break;
                case "cubic": id = 3; break;
                case "easeout-quad": id = 4; break;
                case "easeout-cubic": id = 5; break;
                case "custom": id = 6; break;
                default: break;
            }

            return id;
        }

        public void SetGyroMouseDZ(int index, int value, IProfileLoadTarget control) => gyroSlots[index].SetMouseDeadZone(value, control);

        public void SetGyroControlsToggle(int index, bool value, IProfileLoadTarget control) => gyroSlots[index].SetControlsToggle(value, control);

        public void SetGyroMouseToggle(int index, bool value, IProfileLoadTarget control) => gyroSlots[index].SetMouseToggle(value, control);

        public void SetGyroMouseStickToggle(int index, bool value, IProfileLoadTarget control) => gyroSlots[index].SetMouseStickToggle(value, control);

        // GetX360ControlString（XML 保存用の名前）は X360ControlTables へ移した。

        // Legacy Load implementation (formerly LoadOld) will be the canonical Load method below.

        // Load() の末尾にあった偽の exe 名の読み込みを切り出した

        public void PostProcessLoad()
        {
            // Check if any set profile names should be considered Distance profiles
            for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
            {
                if (profilePath[i] != null && profilePath[i].ToLower().Contains("distance"))
                {
                    distanceProfiles[i] = true;
                }
            }

            // Compile shortcut version number if lastVersionChecked is populated
            if (!string.IsNullOrEmpty(UpdateCheck.lastVersionChecked))
            {
                UpdateCheck.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(UpdateCheck.lastVersionChecked);
                if (UpdateCheck.lastVersionCheckedNum == 0) UpdateCheck.lastVersionChecked = string.Empty;
            }

            Network.oscServPort = Math.Clamp(Network.oscServPort, 1024, 65535);
            Network.oscSendPort = Math.Clamp(Network.oscSendPort, 1024, 65535);
            Network.udpServPort = Math.Clamp(Network.udpServPort, 1024, 65535);

            Network.udpSmoothingMincutoff = Math.Clamp(Network.udpSmoothingMincutoff, 0.00001, 100.0);
            Network.udpSmoothingBeta = Math.Clamp(Network.udpSmoothingBeta, 0.0, 1.0);
        }

        // Actions.xml の読み込み（または既定の作成）の後に発行する。BackingStore から Mapping への逆参照をなくすための通知。
        // コンテナ構築前の読み込みでは購読者がいないが、その時点では実行時状態は新品であり、結果は同じ。
        public event Action? ActionsLoaded;

        // ActionsLoaded の購読者の差し替え。ブートストラップの BackingStore はコンテナより長く生きるため、
        // コンテナが作り直されても購読が 1 つになるよう、前回の購読者を BackingStore 自身が覚えて外す
        private Action? _actionsLoadedHandler;

        /// <summary>実在するコントロール（<see cref="DS4Controls.None"/> 以外）の設定を返す。プリセットの読み込みなど、コントロールが固定のときに使う（なければ例外）。</summary>
        /// <summary>スロットの有効なスペシャルアクション名のリスト。リストは、プロファイルの読み込み前の既定値の設定（ResetProfile）で作られるため、読み込み中・保存中は必ずある。</summary>
        public List<string> GetRequiredProfileActions(int device)
        {
            return profileActions[device] ?? throw new InvalidOperationException($"スロット {device} のスペシャルアクション名のリストがない");
        }

        // 旧形式の保存・読込に欠けていた設定（ProfileDTO にあり、旧形式に無かった 10 要素）の読み書き。
    }
}

