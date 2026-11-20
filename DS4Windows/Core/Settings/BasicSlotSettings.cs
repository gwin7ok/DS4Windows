namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの基本的な設定（タッチパッドの切り替え、無操作での切断、物理コントローラーへの出力データ、Bluetooth のポーリング間隔、
    /// 一緒に起動するプログラム、DInput のみ）の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、入力処理、設定画面が同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class BasicSlotSettings
    {
        public BasicSlotSettings(int slot)
        {
            Slot = slot;
            ResetForProfileLoad();
        }

        public int Slot { get; }

        public bool EnableTouchToggle { get; set; }

        /// <summary>無操作で切断するまでの時間（秒。0 なら無効）。</summary>
        public int IdleDisconnectTimeout { get; set; }

        public bool EnableOutputDataToDS4 { get; set; }

        public int BTPollRate { get; set; }

        /// <summary>プロファイルと一緒に起動するプログラムのパス（なければ空）。</summary>
        public string? LaunchProgram { get; set; }

        public bool DinputOnly { get; set; }

        /// <summary>プロファイルの読み込み前の既定値へ戻す（新しく作るときの値でもある）。</summary>
        public void ResetForProfileLoad()
        {
            EnableTouchToggle = BackingStore.DEFAULT_TOUCH_TOGGLE;
            IdleDisconnectTimeout = 0;
            EnableOutputDataToDS4 = BackingStore.DEFAULT_OUTPUT_TO_DS4;
            BTPollRate = BackingStore.DEFAULT_DS4_BT_POLL_RATE;
            LaunchProgram = string.Empty;
            DinputOnly = BackingStore.DEFAULT_DINPUT_ONLY;
        }
    }
}
