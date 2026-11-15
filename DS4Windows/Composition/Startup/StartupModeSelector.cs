using DS4Windows.Core.Utilities;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// 起動引数から起動の種類を決める（旧 <c>App.CheckOptions</c> の判断の部分）。副作用はない。
    /// 条件の優先順位は、エラー → ドライバーの導入 → デバイスの再有効化 → 旧タスクの起動 → コマンド の順。
    /// </summary>
    public static class StartupModeSelector
    {
        public static StartupMode Select(ArgumentParser parser)
        {
            if (parser.HasErrors) return StartupMode.ArgumentError;
            if (parser.Driverinstall) return StartupMode.DriverInstall;
            if (parser.ReenableDevice) return StartupMode.ReenableDevice;
            if (parser.Runtask) return StartupMode.RunTask;
            if (parser.Command) return StartupMode.LegacyCommand;
            return StartupMode.Normal;
        }
    }
}
