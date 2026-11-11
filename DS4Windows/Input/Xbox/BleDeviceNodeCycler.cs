using System;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// Bluetooth LE の機器を切断する。従来の Bluetooth 向けの切断の API（<c>IOCTL_BTH_DISCONNECT_DEVICE</c>）は BLE の HID では効かず、
    /// Windows も公式の切断を出していない（「削除」＝ペアリング解除だけ）ため、機器のノード（<c>BTHLE\DEV_…</c>）を無効にして、すぐ有効に戻す。
    /// ペアリングは残る。管理者権限が要る（なければ失敗）。有効に戻せなかったときは、機器が無効のままにならないよう、数回やり直す。
    /// </summary>
    public static class BleDeviceNodeCycler
    {
        private const int EnableAttempts = 5;

        /// <summary>切断を要求できたら true（無効にできた）。XUSB のインターフェースのパスから、機器のノードを探す。</summary>
        public static bool Disconnect(string xusbInterfacePath)
        {
            string owner = DevicePathProbe.GetInstanceIdFromDevicePath(xusbInterfacePath);
            string nodeId = string.IsNullOrEmpty(owner) ? string.Empty : DevicePathProbe.FindBluetoothLeDeviceNode(owner);
            if (string.IsNullOrEmpty(nodeId))
            {
                AppLogger.LogDebug($"[BLE] device node not found for {xusbInterfacePath}");
                return false;
            }

            if (NativeMethods.CM_Locate_DevNodeW(out uint node, nodeId, 0) != 0)
            {
                AppLogger.LogDebug($"[BLE] cannot locate {nodeId}");
                return false;
            }

            uint disabled = NativeMethods.CM_Disable_DevNode(node, 0);
            AppLogger.LogDebug($"[BLE] disable {nodeId}: result={disabled}");
            if (disabled != 0)
            {
                return false;   // 権限がない場合など。何も変わっていない
            }

            Thread.Sleep(500);   // 切断の通知が、行き渡るのを待つ
            for (int attempt = 1; attempt <= EnableAttempts; attempt++)
            {
                uint enabled = NativeMethods.CM_Enable_DevNode(node, 0);
                AppLogger.LogDebug($"[BLE] enable {nodeId}: attempt={attempt} result={enabled}");
                if (enabled == 0)
                {
                    return true;
                }

                Thread.Sleep(500);
            }

            AppLogger.LogWarn($"[BLE] could not enable {nodeId} again after disconnecting it; enable it in Device Manager");
            return true;
        }
    }
}
