using System;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class DeviceStateService : IDeviceStateService
    {
        // Phase7-Step3 W5: Global の転送（IsFirstConnection／MarkConnected／ResetConnectionFlags）を削除し、
        // 注入された BackingStore のフラグ配列を直接使う。
        private readonly BackingStore _config;

        public DeviceStateService(BackingStore config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定 ----
        public bool IsFirstConnection(int slotIndex) => _config.firstConnectionAfterStartup[slotIndex];

        public void MarkConnected(int slotIndex) => _config.firstConnectionAfterStartup[slotIndex] = false;

        // ---- Phase6-Step7-1: 起動時の初回接続フラグの一括リセット（App.xaml.cs から使用）----
        public void ResetConnectionFlags()
        {
            for (int i = 0; i < ControllerLimits.TestProfileItemCount; i++)
            {
                _config.firstConnectionAfterStartup[i] = true;
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