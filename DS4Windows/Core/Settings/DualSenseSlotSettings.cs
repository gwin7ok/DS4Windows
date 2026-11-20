#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの DualSense 固有の設定（振動のエミュレーション方式、汎用の振動の再スケール、ハプティックの強さ）の持ち主（値そのもの）。
    /// <see cref="BackingStore"/> がスロットごとに 1 つ持ち、保存・読み込み（プロファイルの XML）、入力機種の反映、設定画面が同じ 1 つのオブジェクトを読み書きする。
    /// プロファイルの読み込み前の初期化では戻さない（従来の配列も戻していなかった）。
    /// </summary>
    public class DualSenseSlotSettings
    {
        public DualSenseSlotSettings(int slot) => Slot = slot;

        public int Slot { get; }

        // TODO(技術的負債): RumbleEmulationMode が DualSenseDevice（第1層）の入れ子の型のため、Core がこの型を参照する（規則 L2 は記録のみ）。列挙型を Core/Model へ移せば、この参照はなくなる
        public RumbleEmulationMode RumbleEmulationMode { get; set; }

        public bool UseGenericRumbleRescale { get; set; }

        public byte HapticPowerLevel { get; set; }
    }
}
