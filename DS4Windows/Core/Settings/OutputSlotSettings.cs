using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input.Devices;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの出力（仮想コントローラーの種類、振動）の設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class OutputSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public OutputSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        /// <summary>保存される出力の種類（Xbox 360／DS4）。</summary>
        public OutContType OutContType
        {
            get => _settings.OutContType[_slot];
            set => _settings.OutContType[_slot] = value;
        }

        /// <summary>仮想コントローラーを作らない（DInput のみ）。</summary>
        public bool DInputOnly
        {
            get => _settings.BasicSlot(_slot).DinputOnly;
            set => _settings.BasicSlot(_slot).DinputOnly = value;
        }

        public bool VirtualTriggerButtons
        {
            get => _settings.OutputVirtualTriggerButton[_slot];
            set => _settings.OutputVirtualTriggerButton[_slot] = value;
        }

        public DS4TriggerOutputMode OutputDS4TriggerMode
        {
            get => _settings.OutputDS4TriggerMode[_slot];
            set => _settings.OutputDS4TriggerMode[_slot] = value;
        }

        public byte RumbleBoost
        {
            get => _settings.RumbleSlot(_slot).Boost;
            set => _settings.RumbleSlot(_slot).Boost = value;
        }

        /// <summary>振動の自動停止までの時間（ミリ秒。XML と同じ単位）。</summary>
        public int RumbleAutostopTimeMs
        {
            get => _settings.GetRumbleAutostopTime(_slot);
            set => _settings.SetRumbleAutostopTime(_slot, value);
        }

        // TODO(技術的負債): RumbleEmulationMode が DualSenseDevice（第1層）の入れ子の型のため、Core がこの型を参照する
        // （IProfileSettingsService と同じ。記録のみの規則 L2）。列挙型を Core/Model へ移せば、この参照はなくなる
        public RumbleEmulationMode DualSenseRumbleEmulationMode
        {
            get => _settings.DualSenseSlot(_slot).RumbleEmulationMode;
            set => _settings.DualSenseSlot(_slot).RumbleEmulationMode = value;
        }

        public byte DualSenseHapticPowerLevel
        {
            get => _settings.DualSenseSlot(_slot).HapticPowerLevel;
            set => _settings.DualSenseSlot(_slot).HapticPowerLevel = value;
        }

        public bool UseGenericRumbleStrRescaleForDualSenses
        {
            get => _settings.DualSenseSlot(_slot).UseGenericRumbleRescale;
            set => _settings.DualSenseSlot(_slot).UseGenericRumbleRescale = value;
        }

        public bool InverseRumbleMotors
        {
            get => _settings.RumbleSlot(_slot).InverseMotors;
            set => _settings.RumbleSlot(_slot).InverseMotors = value;
        }
    }
}
