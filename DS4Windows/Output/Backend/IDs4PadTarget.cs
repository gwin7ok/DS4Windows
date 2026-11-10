namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 DS4 パッド 1 台の操作。ドライバーの型を使わない値だけで表す（ボタンのビットは <see cref="Ds4Buttons"/>、<see cref="Ds4SpecialButtons"/>）。</summary>
    public interface IDs4PadTarget
    {
        byte LeftTrigger { set; }
        byte RightTrigger { set; }
        byte LeftThumbX { set; }
        byte LeftThumbY { set; }
        byte RightThumbX { set; }
        byte RightThumbY { set; }

        void SetButtonsFull(ushort buttons);
        void SetSpecialButtonsFull(byte buttons);
        void SetDPadDirection(Ds4DPadDirection direction);
        void SubmitReport();
        void ResetReport();

        /// <summary>生のレポート（拡張 API）を送る。</summary>
        void SubmitRawReport(byte[] report);

        /// <summary>
        /// ゲームからの出力レポート（振動・ライトバー）を待つ。<paramref name="timedOut"/> が true なら時間切れ。
        /// ドライバーが閉じられると例外（<see cref="System.ComponentModel.Win32Exception"/> など）を投げる。
        /// </summary>
        byte[] AwaitRawOutputReport(int timeoutMs, out bool timedOut);

        void Connect();
        void Disconnect();
    }
}
