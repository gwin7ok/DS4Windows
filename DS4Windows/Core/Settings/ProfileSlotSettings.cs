using DS4Windows.Core.Model;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの設定の入口。領域（ジャイロなど）ごとの小さな窓口を返す。
    /// 全機能の契約（<see cref="IProfileSettingsService"/>）の代わりに、利用者が必要な領域だけを受け取れるようにする。
    /// </summary>
    public class ProfileSlotSettings
    {
        public ProfileSlotSettings(IProfileSettingsService settings, int slot, GyroSlotSettings gyro)
        {
            Slot = slot;
            Gyro = gyro ?? throw new System.ArgumentNullException(nameof(gyro));
            Touchpad = new TouchpadSlotSettings(settings, slot);
            Triggers = new TriggerSlotSettings(settings, slot);
            Sticks = new StickSlotSettings(settings, slot);
            Lightbar = new LightbarSlotSettings(settings, slot);
            Output = new OutputSlotSettings(settings, slot);
            General = new GeneralSlotSettings(settings, slot);
            SixAxis = new SixAxisSlotSettings(settings, slot);
        }

        public int Slot { get; }

        public GyroSlotSettings Gyro { get; }

        public TouchpadSlotSettings Touchpad { get; }

        public TriggerSlotSettings Triggers { get; }

        public StickSlotSettings Sticks { get; }

        public LightbarSlotSettings Lightbar { get; }

        public OutputSlotSettings Output { get; }

        public GeneralSlotSettings General { get; }

        public SixAxisSlotSettings SixAxis { get; }
    }
}
