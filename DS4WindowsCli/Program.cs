#nullable enable

using System;
using DS4Windows.Core.Platform;

namespace DS4Windows.Cli
{
    /// <summary>
    /// <c>DS4WindowsCli.exe -command &lt;コマンド&gt;</c>。動いている DS4Windows へコマンドを送る（書き方と結果は、旧 <c>DS4Windows.exe -command</c> と同じ）。
    /// 終了コード: 0＝成功、1＝DS4Windows が動いていない、2＝引数が不正、3＝他のクライアントが使用中。
    /// </summary>
    public static class Program
    {
        private const string Usage =
            "Usage: DS4WindowsCli.exe -command <command>\n" +
            "  start | stop | cycle | shutdown | Disconnect[.<n>]\n" +
            "  LoadProfile.<n>.<name> | LoadTempProfile.<n>.<name>\n" +
            "  ChangeLedColor.<n>.<r>.<g>.<b>\n" +
            "  OutputSlot.<n>.Unplug | PlugDS4 | PlugX360\n" +
            "  Query.<n>.<ProfileName | OutContType | ActiveOutDevType | UsedInputOnly | DeviceVidPid | DevicePath |\n" +
            "           MacAddress | DisplayName | ConnType | ExclusiveStatus | Battery | Charging |\n" +
            "           OutputSlotType | OutputSlotPermanentType>\n" +
            "Exit code: 0=ok, 1=DS4Windows is not running, 2=invalid arguments, 3=busy";

        public static int Main(string[] args)
        {
            CliArguments parsed = CliArguments.Parse(args);
            if (parsed.ShowHelp)
            {
                Console.Out.WriteLine(Usage);
                return args == null || args.Length == 0 ? CommandLineClient.ExitBadArguments : CommandLineClient.ExitOk;
            }

            if (parsed.Error != null)
            {
                Console.Error.WriteLine(parsed.Error);
                return CommandLineClient.ExitBadArguments;
            }

            using (var ipc = new IpcMemoryMap())
            {
                int code = new CommandLineClient(ipc, Console.Out).Run(parsed.Command!, parsed.IsQuery); // 解析に成功した（Error がない）ときはコマンドがある
                if (code == CommandLineClient.ExitNotRunning)
                    Console.Error.WriteLine("DS4Windows is not running.");
                else if (code == CommandLineClient.ExitBusy)
                    Console.Error.WriteLine("Another DS4WindowsCli is using the result channel.");
                return code;
            }
        }
    }
}
