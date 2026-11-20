using DS4Windows.Output.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    public class OutputSlotService : IOutputSlotService
    {
        // Phase6-Step10-4-3: 全ての依存を必須化（Program.rootHub／AppHost／new のフォールバックを廃止。Phase6-Step10 §8 の TODO を解消）。
        // スロット管理（OutputSlotManager）の実体と、仮想スロットの抜き差しは、ControlService ではなく VirtualPadService が持つ
        // （過渡期の逆依存 OutputSlotService → ControlService を解消した）。
        // IVirtualPadService を直接受ける（循環は出力種別の配列を別の持ち主へ移して避けている）。
        // VirtualPadService は出力種別の配列を IOutputDeviceTypeState から受けるため、本サービスへ依存せず、循環しない。
        private readonly IVirtualPadService _virtualPad;
        private readonly IOutputSlotStore _store;
        private readonly IOutputDeviceTypeState _types;   // 出力種別の 2 本の配列の持ち主
        private OutputSlotManager SlotManager => _virtualPad.OutputSlotManager;
        private IVirtualPadService VirtualPad => _virtualPad;
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs>? OutputSlotChanged;

        // 孤立配列 _deviceTypes（Global/BackingStore と非連動で常に None を返していた）と、
        // その読み書き API（GetOutputDeviceType／SetOutputDeviceType）は削除した。
        // 出力デバイス種別の三態は、永続設定＝IProfileSettingsService.OutContType、
        // 実行時接続状態＝ActiveOutDevType、UI一時状態＝OutDevTypeTemp を参照すること。

        public OutputSlotService(IVirtualPadService virtualPad, IOutputSlotStore store, IOutputDeviceTypeState types)
        {
            ArgumentNullException.ThrowIfNull(virtualPad);
            _virtualPad = virtualPad;
            ArgumentNullException.ThrowIfNull(store);
            _store = store;
            ArgumentNullException.ThrowIfNull(types);
            _types = types;
        }

        /// <summary>
        /// OutputSlotChanged イベントの発行。
        /// deviceType は、プラグイン／アンプラグ操作で指定された種別であり、永続設定（OutContType）には書き込まない。
        /// </summary>
        private void RaiseOutputSlotChanged(int slotIndex, OutContType deviceType, OutputDevice? outputDevice)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] OutputSlotService.OutputSlotChanged: Slot {slotIndex} = {deviceType}");
            OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, deviceType, outputDevice));
        }

        // 実体 OutputSlotManager 連動操作
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

        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<OutSlotDevice, OutputSlotView> _views =
            new System.Runtime.CompilerServices.ConditionalWeakTable<OutSlotDevice, OutputSlotView>();
        private EventHandler<OutputSlotAssignmentEventArgs>? _slotAssigned;
        private EventHandler<OutputSlotAssignmentEventArgs>? _slotUnassigned;
        private bool _managerHooked;

        public IReadOnlyList<IOutputSlotView> SlotViews
        {
            get
            {
                HookManager();
                OutSlotDevice[]? slots = SlotManager?.OutputSlots;
                if (slots == null) return Array.Empty<IOutputSlotView>();

                var result = new IOutputSlotView[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                    result[i] = ViewOf(slots[i])!; // 空きスロットも、従来どおり ViewOf の結果をそのまま入れる
                return result;
            }
        }

        // スロット管理は IVirtualPadService が持ち、このサービスより後に作られることがある。
        // 通知の購読が先に来ても取りこぼさないよう、スロット管理が見えた最初の機会に、一度だけ購読する。
        public event EventHandler<OutputSlotAssignmentEventArgs> SlotAssigned
        {
            add { _slotAssigned += value; HookManager(); }
            remove { _slotAssigned -= value; }
        }

        public event EventHandler<OutputSlotAssignmentEventArgs> SlotUnassigned
        {
            add { _slotUnassigned += value; HookManager(); }
            remove { _slotUnassigned -= value; }
        }

        private void HookManager()
        {
            lock (_syncLock)
            {
                if (_managerHooked) return;
                OutputSlotManager manager = SlotManager;
                if (manager == null) return;

                manager.SlotAssigned += (sender, slotNum, slot) => _slotAssigned?.Invoke(this, new OutputSlotAssignmentEventArgs(slotNum));
                manager.SlotUnassigned += (sender, slotNum, slot) => _slotUnassigned?.Invoke(this, new OutputSlotAssignmentEventArgs(slotNum));
                _managerHooked = true;
            }
        }

        private OutputSlotView? ViewOf(OutSlotDevice slot) => slot == null ? null : _views.GetValue(slot, s => new OutputSlotView(s));

        public OutSlotDevice? GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return null;
            return SlotManager?.GetOutSlotDevice(slotNumber);
        }

        // UI（出力スロット管理画面）・UDP コマンドから仮想スロットを抜き差しする正式な入口。
        // 画面の従来の挙動と同等にする: 事前条件（未接続かつ入力未割り当て）を確認し、イベントスレッド
        // （IVirtualPadService.QueueOnEventThread）へ切り替えて VirtualPadService の正規 API を呼ぶ。戻り値は「要求を受け付けたか」。
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

        // PluginSlot と同じ方針。事前条件は「接続中かつ入力未割り当て」。
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

        // 2 つの配列の実体は IOutputDeviceTypeState が持つ。
        // 本サービスは、IOutputSlotService の公開 API（UI・IPC の利用者がいる）として、同じ配列を返すだけにした。

        // ---- Profile Editor で選択中の未確定(Temp)出力デバイスタイプ ----
        // 三態の「UI一時状態」。永続設定（IProfileSettingsService.OutContType）とは別物。初期値は全スロット X360。
        public OutContType[] OutDevTypeTemp => _types.OutDevTypeTemp;

        // ---- スロット別・現在稼働中の出力デバイスタイプ ----
        // 三態の「実行時接続状態」。未接続時は None。永続設定（IProfileSettingsService.OutContType）とは別物。
        public OutContType[] ActiveOutDevType => _types.ActiveOutDevType;
    }
}