using DS4Windows.Core.Abstractions;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのライトバーの設定の窓口。<see cref="IProfileSettingsService"/> のスロット番号の配列を、
    /// スロット番号を渡さずに読み書きできるようにする（値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class LightbarSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public LightbarSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        /// <summary>ライトバーの設定（モード、色、点滅、充電時の色など。中身を直接書き換える）。</summary>
        public LightbarSettingInfo Info => _settings.LightbarSettingsInfo[_slot];
    }
}
