namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの振動の設定（強さ、自動停止までの時間、左右のモーターの入れ替え）の持ち主（値そのもの）。
    /// <see cref="BackingStore"/> がスロットごとに 1 つ持ち、保存・読み込み（プロファイルの XML）、振動の出力、設定画面が同じ 1 つのオブジェクトを読み書きする。
    /// 実機への即時反映（自動停止時間の変更）は、設定サービスが行う。
    /// </summary>
    public class RumbleSlotSettings
    {
        public RumbleSlotSettings(int slot)
        {
            Slot = slot;
            ResetForProfileLoad();
        }

        public int Slot { get; }

        /// <summary>振動の強さ（%）。</summary>
        public byte Boost { get; set; }

        /// <summary>自動停止までの時間（ミリ秒。0 なら無効）。</summary>
        public int AutostopTime { get; set; }

        /// <summary>左右のモーターを入れ替える。</summary>
        public bool InverseMotors { get; set; }

        /// <summary>プロファイルの読み込み前の既定値へ戻す（新しく作るときの値でもある）。</summary>
        public void ResetForProfileLoad()
        {
            Boost = (byte)BackingStore.DEFAULT_RUMBLE;
            AutostopTime = 0;
            InverseMotors = false;
        }
    }
}
