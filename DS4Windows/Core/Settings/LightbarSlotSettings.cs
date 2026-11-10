using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのLightbarの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class LightbarSlotSettings
    {
        public LightbarSlotSettings(int slot)
        {
            Slot = slot;
            Info = new LightbarSettingInfo();
        }

        public int Slot { get; }

        public LightbarSettingInfo Info { get; }
    }
}
