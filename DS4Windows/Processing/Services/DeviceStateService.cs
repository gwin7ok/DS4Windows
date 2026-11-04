using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class DeviceStateService : IDeviceStateService
    {
        // Phase7-Step3 W5: Global の転送（IsFirstConnection／MarkConnected／ResetConnectionFlags）を削除し、
        // 注入された BackingStore のフラグ配列を直接使う。
        private readonly BackingStore _config;

        // 再接続のプロファイルをコントローラー単位にする修正（Reconnect-Profile-PerController-Fix-Plan.md）。MAC アドレスごと（大文字小文字を区別しない）
        private readonly object _macLock = new object();
        private readonly HashSet<string> _connectedMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _rememberedProfiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public DeviceStateService(BackingStore config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定 ----
        public bool IsFirstConnection(int slotIndex) => _config.firstConnectionAfterStartup[slotIndex];

        public void MarkConnected(int slotIndex) => _config.firstConnectionAfterStartup[slotIndex] = false;

        public bool IsFirstConnection(int slotIndex, string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress))
                return IsFirstConnection(slotIndex);   // 有効なシリアルがない: 従来のスロット単位

            lock (_macLock)
            {
                return !_connectedMacs.Contains(macAddress);
            }
        }

        public void MarkConnected(int slotIndex, string macAddress)
        {
            MarkConnected(slotIndex);
            if (string.IsNullOrEmpty(macAddress)) return;

            lock (_macLock)
            {
                _connectedMacs.Add(macAddress);
            }
        }

        public void RememberProfile(string macAddress, string profileName)
        {
            if (string.IsNullOrEmpty(macAddress) || string.IsNullOrWhiteSpace(profileName)) return;

            lock (_macLock)
            {
                _rememberedProfiles[macAddress] = profileName;
            }
        }

        public string GetRememberedProfile(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress)) return null;

            lock (_macLock)
            {
                return _rememberedProfiles.TryGetValue(macAddress, out string profile) ? profile : null;
            }
        }

        // ---- Phase6-Step7-1: 起動時の初回接続フラグの一括リセット（App.xaml.cs から使用）----
        public void ResetConnectionFlags()
        {
            for (int i = 0; i < ControllerLimits.TestProfileItemCount; i++)
            {
                _config.firstConnectionAfterStartup[i] = true;
            }

            lock (_macLock)
            {
                _connectedMacs.Clear();
                _rememberedProfiles.Clear();
            }
        }

        // ---- Phase6-Step2-4 (PR-4): デバイスのシリアル変更通知 ----
        // Phase7-Step10d W4: 旧 Global.DeviceSerialChange（static イベント）の実体をこのサービスへ移した（通知の内容は同一）。
        // 本体に購読者はいない（旧 Global のイベントも同様）。
        public event EventHandler<SerialChangeArgs> DeviceSerialChange;

        public void OnDeviceSerialChange(object sender, int slotIndex, string serial)
        {
            if (DeviceSerialChange != null)
            {
                SerialChangeArgs args = new SerialChangeArgs(slotIndex, serial);
                DeviceSerialChange(sender, args);
            }
        }
    }
}