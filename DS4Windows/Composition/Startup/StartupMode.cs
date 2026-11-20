namespace DS4Windows.Composition.Startup
{
    /// <summary>起動引数から決まる、起動の種類（<see cref="StartupModeSelector"/> が決める）。</summary>
    public enum StartupMode
    {
        /// <summary>通常の起動（以降の手順へ進む）。</summary>
        Normal,

        /// <summary>引数にエラーがある。終了コード 1 で終了する。</summary>
        ArgumentError,

        /// <summary>ドライバーの導入画面だけを出して終了する。</summary>
        DriverInstall,

        /// <summary>昇格した補助プロセスとして、デバイスを再有効化して終了する。</summary>
        ReenableDevice,

        /// <summary>昇格した補助プロセスとして、旧タスクを起動して終了する。</summary>
        RunTask,

        /// <summary>古い書き方のコマンド指定。何もせず終了する（コマンドの送信は DS4WindowsCli.exe が行う）。</summary>
        LegacyCommand,
    }
}
