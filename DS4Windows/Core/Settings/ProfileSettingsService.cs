#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    public class ProfileSettingsService : IProfileSettingsService
    {
        private readonly object _syncLock = new object();
        public const int TEST_PROFILE_ITEM_COUNT = ControllerLimits.TestProfileItemCount;
        public const int MAX_DS4_CONTROLLER_COUNT = ControllerLimits.MaxControllerCount;

        // Step10-2-A: m_Config(BackingStore)委譲用。Global.storeと同一インスタンスを参照する
        // (データの二重管理を避けるため、専用バックアップ配列は持たない)
        private readonly BackingStore _config;
        private BackingStore SafeConfig => _config;

        // スロット上限（現在接続台数ではない）を IEnvironmentService から取得する。
        // 既存の生成箇所（テスト・Global のフォールバック）を変更しないよう省略可能とし、
        // 省略時は状態を持たない既定実装を用いる（AppHost.Services は使用しない）。
        private readonly IEnvironmentService _environmentService;

        // 全ての依存を必須化（Global.store／new EnvironmentService／Program.rootHub の参照を廃止）。
        // 旧 Func<ControlService>（ControlService → IProfileSettingsService の依存循環を避ける遅延。決定 D4-3）。使うのは GetController だけだったため、依存のない IDeviceStateAccessor を直接受ける
        private readonly IConfigurableDeviceLookup _deviceState;

        public ProfileSettingsService(BackingStore backingStore,
            IEnvironmentService environmentService,
            IConfigurableDeviceLookup deviceState)
        {
            ArgumentNullException.ThrowIfNull(backingStore);
            _config = backingStore;
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(deviceState);
            _deviceState = deviceState;
            // BackingStore が、一時プロファイル・DInput のみ・出力 KBM のマッピングを読むための登録（最初の 1 つが有効）
            _config.LinkProfileSettings(this);
        }

        public bool[] DistanceProfiles => _config.distanceProfiles;

        private bool[] _touchpadActive = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _useTempProfile = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        // 一時プロファイルの名前の保管は、プロファイルの固定 ID（名前を読み書きする窓口。ProfileSlotNames）。名前を変えても、読み出す名前は現在の名前になる
        private ProfileSlotNames _tempProfileName = new ProfileSlotNames(TEST_PROFILE_ITEM_COUNT);

        /// <summary>登録簿を結ぶ（DI の登録が、登録簿を作るときに呼ぶ。BackingStore のスロットの保管と同じ）。</summary>
        public void AttachProfileIdentity(DS4Windows.Core.Abstractions.IProfileIdentityService identity) => _tempProfileName.AttachIdentity(identity);
        private bool[] _tempProfileDistance = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        private bool[] _useDInputOnly = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        private bool[] _linkedProfileCheck = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };

        public bool[] TouchpadActiveArray
        {
            get => _touchpadActive;
            set
            {
                lock (_syncLock)
                {
                    _touchpadActive = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] UseTempProfileArray
        {
            get => _useTempProfile;
            set
            {
                lock (_syncLock)
                {
                    _useTempProfile = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] TempProfileDistanceArray
        {
            get => _tempProfileDistance;
            set
            {
                lock (_syncLock)
                {
                    _tempProfileDistance = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool[] UseDInputOnlyArray
        {
            get => _useDInputOnly;
            set
            {
                lock (_syncLock)
                {
                    _useDInputOnly = value ?? new bool[TEST_PROFILE_ITEM_COUNT];
                }
            }
        }

        public bool GetTouchpadActive(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _touchpadActive.Length)
                return _touchpadActive[deviceIndex];
            return true;
        }

        public bool GetUseTempProfile(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _useTempProfile.Length)
                return _useTempProfile[deviceIndex];
            return false;
        }

        public void SetUseTempProfile(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _useTempProfile.Length)
            {
                lock (_syncLock)
                {
                    var old = _useTempProfile[deviceIndex];
                    if (old != value)
                    {
                        _useTempProfile[deviceIndex] = value;
                    }
                }
            }
        }

        public string GetTempProfileName(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileName.Length)
                return _tempProfileName[deviceIndex];
            return string.Empty;
        }

        public void SetTempProfileName(int deviceIndex, string value)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileName.Length)
            {
                lock (_syncLock)
                {
                    var old = _tempProfileName[deviceIndex];
                    if (old != value)
                    {
                        _tempProfileName[deviceIndex] = value ?? string.Empty;
                    }
                }
            }
        }

        public void SetTempProfileDistance(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _tempProfileDistance.Length)
            {
                lock (_syncLock)
                {
                    var old = _tempProfileDistance[deviceIndex];
                    if (old != value)
                    {
                        _tempProfileDistance[deviceIndex] = value;
                    }
                }
            }
        }

        public bool GetLinkedProfileCheck(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _linkedProfileCheck.Length)
                return _linkedProfileCheck[deviceIndex];
            return false;
        }

        public void SetLinkedProfileCheck(int deviceIndex, bool value)
        {
            if (deviceIndex >= 0 && deviceIndex < _linkedProfileCheck.Length)
            {
                lock (_syncLock)
                {
                    var old = _linkedProfileCheck[deviceIndex];
                    if (old != value)
                    {
                        _linkedProfileCheck[deviceIndex] = value;
                    }
                }
            }
        }

        // ---- Step10-2-A-1: スティック関連 (m_Config委譲) ----



        // ---- Step10-2-A-2: トリガー(L2/R2)関連 (m_Config委譲) ----
        public OutputTypeSlotSettings OutputType(int slot) => SafeConfig.outputTypeSlots[slot];



        // ---- Step10-2-A-3: タッチパッド関連 (m_Config委譲) ----

        // ---- Step10-2-A-4: ジャイロ関連 (m_Config委譲) ----





        // ---- Step10-2-A-5: ライトバー・ランブル関連 (m_Config委譲) ----
        public RumbleSlotSettings RumbleSlot(int slot) => SafeConfig.rumbleSlots[slot];

        public DualSenseSlotSettings DualSenseSlot(int slot) => SafeConfig.dualSenseSlots[slot];


        public byte GetRumbleBoost(int deviceIndex)
        {
            if (_deviceState.GetConfigurableDevice(deviceIndex)?.UsesGenericRumbleRescaleSetting == true &&
                DualSenseSlot(deviceIndex) is { UseGenericRumbleRescale: false })
                return 100;

            return SafeConfig != null ? SafeConfig.rumbleSlots[deviceIndex].Boost : (byte)100;
        }

        public int GetRumbleAutostopTime(int deviceIndex)
        {
            return SafeConfig != null ? SafeConfig.rumbleSlots[deviceIndex].AutostopTime : 0;
        }

        public ref DS4Color GetMainColor(int deviceIndex) => ref SafeConfig.lightbarSlots[deviceIndex].Info.ds4winSettings.m_Led;

        public void SetRumbleAutostopTime(int index, int value)
        {
            if (SafeConfig != null && index >= 0 && index < ControllerLimits.TestProfileItemCount)
            {
                SafeConfig.rumbleSlots[index].AutostopTime = value;
                IConfigurableDevice? tempDev = _deviceState.GetConfigurableDevice(index);
                if (tempDev != null && tempDev.IsSynced())
                    tempDev.RumbleAutostopTime = value;
            }
        }

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        public ButtonMouseInfo[] ButtonMouseInfos => SafeConfig.buttonMouseInfos;
        public BasicSlotSettings BasicSlot(int slot) => SafeConfig.basicSlots[slot];
        public bool GetEnableTouchToggle(int deviceIndex) => SafeConfig != null && SafeConfig.basicSlots[deviceIndex].EnableTouchToggle;

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----

        // ---- Step10-2-A-8: 残余設定・デバイスオプション (m_Config委譲) ----
        public bool DS4Mapping
        {
            get => SafeConfig != null && SafeConfig.ds4Mapping;
            set
            {
                if (SafeConfig != null) SafeConfig.ds4Mapping = value;
            }
        }

        // 課題④: ProfileActions のインターフェース実装
        public List<string>?[] ProfileActions
        {
            get => SafeConfig.profileActions;
            set
            {
                if (SafeConfig != null)
                {
                    SafeConfig.profileActions = value;
                }
            }
        }

        public int GetBTPollRate(int deviceIndex) => SafeConfig != null ? SafeConfig.basicSlots[deviceIndex].BTPollRate : 0;
        public bool GetDInputOnly(int deviceIndex) => SafeConfig != null && SafeConfig.basicSlots[deviceIndex].DinputOnly;
        public int GetIdleDisconnectTimeout(int deviceIndex) => SafeConfig != null ? SafeConfig.basicSlots[deviceIndex].IdleDisconnectTimeout : 0;
        public bool GetEnableOutputDataToDS4(int deviceIndex) => SafeConfig != null && SafeConfig.basicSlots[deviceIndex].EnableOutputDataToDS4;
        public DS4ControlSettings? GetDS4CSetting(int deviceIndex, string control) => SafeConfig.GetDS4CSetting(deviceIndex, control);
        public DS4ControlSettings? GetDS4CSetting(int deviceIndex, DS4Controls control) => SafeConfig.GetDS4CSetting(deviceIndex, control);

        // ---- Step10-2-A-9: Mapping.cs専用 ----
        public bool ProfileChangedNotification
        {
            get => SafeConfig != null && SafeConfig.profileChangedNotification;
            set
            {
                if (SafeConfig != null) SafeConfig.profileChangedNotification = value;
            }
        }
        public VirtualKBMMapping OutputKBMMapping { get; set; } = null!;   // 出力 KBM のハンドラーの確立（OutputKBMHandlerLifecycle）が、マッピングを使う処理より前に必ず設定する

        public X360Controls[] GetDefaultButtonMapping()
        {
            return (X360Controls[])X360ControlTables.defaultButtonMapping.Clone();
        }

        /// <summary>Windows の仮想キーの値から、現在の出力 KB+M のシステム用の別名（actionAlias）を更新する。</summary>
        public void RefreshActionAlias(DS4ControlSettings setting)
        {
            setting.action.actionAlias = 0;
            if (setting.actionType == DS4ControlSettings.ActionType.Key)
            {
                setting.action.actionAlias = OutputKBMMapping.GetRealEventKey(Convert.ToUInt32(setting.action.actionKey));
            }
        }

        // コピーしない版。Global.reverseX360ButtonMapping は起動時に作られて以後変化しない定数表。
        // 元の Global.reverseX360ButtonMapping（素の public static 配列）と同じく、参照をそのまま返す。
        public DS4Controls[] ReverseX360ButtonMapping => X360ControlTables.reverseX360ButtonMapping;

        // ---- 接続時のエクストラボタン再構築 ----
        public void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons)
        {
            _config.ds4controlSettings[deviceIndex].ResetExtraButtons();
            if (buttons != null)
            {
                _config.ds4controlSettings[deviceIndex].EstablishExtraButtons(buttons);
            }
        }

        // ---- カスタムアクション／エクストラの有無 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.containsCustomAction／Extras と同一）。
        public bool ContainsCustomAction(int deviceIndex) => SafeConfig.containsCustomAction[deviceIndex];

        public bool ContainsCustomExtras(int deviceIndex) => SafeConfig.containsCustomExtras[deviceIndex];

        // ---- 入力→出力割り当てグループ ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の配列を直接参照する（従来の Global.GetControlSettingsGroup と同一）。
        public ControlSettingsGroup GetControlSettingsGroup(int deviceIndex) => SafeConfig.ds4controlSettings[deviceIndex];

        public RemovedShiftSettings GetRemovedShiftSettings(int deviceIndex) => SafeConfig.removedShiftSettings[deviceIndex];

        // 毎入力レポートの経路からも呼ばれるため、スロットごとに 1 度だけ作って使い回す（呼び出しごとに作らない）
        private readonly ProfileSlotSettings[] _slotSettings = new ProfileSlotSettings[ControllerLimits.TestProfileItemCount];

        public ProfileSlotSettings ForSlot(int slot)
        {
            ProfileSlotSettings window = _slotSettings[slot];
            if (window == null)
            {
                window = new ProfileSlotSettings(this, slot, SafeConfig.gyroSlots[slot], SafeConfig.touchpadSlots[slot], SafeConfig.triggerSlots[slot], SafeConfig.stickSlots[slot], SafeConfig.lightbarSlots[slot], SafeConfig.sixAxisSlots[slot]);
                _slotSettings[slot] = window;
            }
            return window;
        }
    }
}