using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    public class OutputSlotService : IOutputSlotService
    {
        // Phase6-Step10-4-3: 全ての依存を必須化（Program.rootHub／AppHost／new のフォールバックを廃止。Phase6-Step10 §8 の TODO を解消）。
        // Phase9 9-E E-4: スロット管理（OutputSlotManager）の実体と、仮想スロットの抜き差しは、ControlService ではなく VirtualPadService が持つ
        // （過渡期の逆依存 OutputSlotService → ControlService を解消した）。
        // Phase13-Step7 7-d: 旧: 遅延取得デリゲート 2 本をやめ、IVirtualPadService を直接受ける。
        // VirtualPadService は出力種別の配列を IOutputDeviceTypeState から受けるため、本サービスへ依存せず、循環しない。
        private readonly IVirtualPadService _virtualPad;
        private readonly IOutputSlotStore _store;
        private readonly IOutputDeviceTypeState _types;   // Phase13-Step7 7-b: 出力種別の 2 本の配列の持ち主（旧: 本サービスのフィールド）
        private OutputSlotManager SlotManager => _virtualPad.OutputSlotManager;
        private IVirtualPadService VirtualPad => _virtualPad;
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // Phase6-Step5-2: 孤立配列 _deviceTypes（Global/BackingStore と非連動で常に None を返していた）と、
        // その読み書き API（GetOutputDeviceType／SetOutputDeviceType）は削除した。
        // 出力デバイス種別の三態は、永続設定＝IProfileSettingsService.OutContType、
        // 実行時接続状態＝ActiveOutDevType、UI一時状態＝OutDevTypeTemp を参照すること。

        public OutputSlotService(IVirtualPadService virtualPad, IOutputSlotStore store, IOutputDeviceTypeState types)
        {
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _types = types ?? throw new ArgumentNullException(nameof(types));
        }

        /// <summary>
        /// OutputSlotChanged イベントの発行（Phase6-Step5-2 で SetOutputDeviceType から分離）。
        /// deviceType は、プラグイン／アンプラグ操作で指定された種別であり、永続設定（OutContType）には書き込まない。
        /// </summary>
        private void RaiseOutputSlotChanged(int slotIndex, OutContType deviceType, OutputDevice outputDevice)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] OutputSlotService.OutputSlotChanged: Slot {slotIndex} = {deviceType}");
            OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, deviceType, outputDevice));
        }

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
        public IReadOnlyList<OutSlotDevice> OutputSlots
        {
            get
            {
                if (SlotManager?.OutputSlots != null)
                {
                    return SlotManager.OutputSlots.ToList().AsReadOnly();
                }
                return Array.Empty<OutSlotDevice>();
            }
        }

        public OutSlotDevice GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return null;
            return SlotManager?.GetOutSlotDevice(slotNumber);
        }

        // Phase6-Step10-5: UI（出力スロット管理画面）・UDP コマンドから仮想スロットを抜き差しする正式な入口。
        // 画面の従来の挙動と同等にする: 事前条件（未接続かつ入力未割り当て）を確認し、イベントスレッド
        // （Phase9 E-4 以降は IVirtualPadService.QueueOnEventThread。旧: ControlService.EventDispatcher）へ切り替えて VirtualPadService の正規 API を呼ぶ。戻り値は「要求を受け付けたか」。
        // OutputSlotChanged は、抜き差しの要求を実行した後に発行する。
        // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護（OutputSlotManager の非同期キューと破棄順序には触れない）。
        public bool PluginSlot(int slotNumber, OutContType devType)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                var virtualPad = VirtualPad;
                if (slotDevice == null || virtualPad == null) return false;

                if (slotDevice.CurrentAttachedStatus != OutputSlotAttachedStatus.UnAttached ||
                    slotDevice.CurrentInputBound != OutputSlotInputBound.Unbound)
                {
                    return false;
                }

                virtualPad.QueueOnEventThread(() =>
                {
                    try
                    {
                        virtualPad.AttachUnboundOutDev(slotDevice, devType);
                        RaiseOutputSlotChanged(slotNumber, devType, null);
                    }
                    catch (Exception ex)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogWarn($"[DI] OutputSlotService.PluginSlot dispatch failed: {ex}");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogWarn($"[DI] OutputSlotService.PluginSlot failed: {ex}");
                return false;
            }
        }

        // Phase6-Step10-5: PluginSlot と同じ方針。事前条件は「接続中かつ入力未割り当て」。
        public bool UnplugSlot(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                var virtualPad = VirtualPad;
                if (slotDevice == null || virtualPad == null) return false;

                if (slotDevice.CurrentAttachedStatus != OutputSlotAttachedStatus.Attached ||
                    slotDevice.CurrentInputBound != OutputSlotInputBound.Unbound)
                {
                    return false;
                }

                virtualPad.QueueOnEventThread(() =>
                {
                    try
                    {
                        virtualPad.DetachUnboundOutDev(slotDevice);
                        RaiseOutputSlotChanged(slotNumber, OutContType.None, null);
                    }
                    catch (Exception ex)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogWarn($"[DI] OutputSlotService.UnplugSlot dispatch failed: {ex}");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogWarn($"[DI] OutputSlotService.UnplugSlot failed: {ex}");
                return false;
            }
        }

        public bool LoadOutputSlots()
        {
            if (SlotManager == null || _store == null) return false;
            return _store.Load(SlotManager);
        }

        public bool SaveOutputSlots()
        {
            if (SlotManager == null || _store == null) return false;
            return _store.Save(SlotManager);
        }

        // Phase13-Step7 7-b: 2 つの配列（旧 Global.outDevTypeTemp／Global.activeOutDevType → Phase7-Step6 W1 で本サービスが保持）の実体は IOutputDeviceTypeState が持つ。
        // 本サービスは、IOutputSlotService の公開 API（UI・IPC の利用者がいる）として、同じ配列を返すだけにした。

        // ---- Phase5-Step13-6: Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        // 三態の「UI一時状態」。永続設定（IProfileSettingsService.OutContType）とは別物。初期値は全スロット X360。
        public OutContType[] OutDevTypeTemp => _types.OutDevTypeTemp;

        // ---- Phase5-Step13-7: スロット別・現在稼働中の出力デバイスタイプ ----
        // 三態の「実行時接続状態」。未接続時は None。永続設定（IProfileSettingsService.OutContType）とは別物。
        public OutContType[] ActiveOutDevType => _types.ActiveOutDevType;
    }
}