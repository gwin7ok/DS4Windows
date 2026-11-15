using System;

namespace DS4Windows.Cli
{
    /// <summary>
    /// コマンドラインの引数の解釈（状態なし）。<c>-command &lt;コマンド&gt;</c>（旧 <c>DS4Windows.exe</c> と同じ書き方）と、
    /// <c>command &lt;コマンド&gt;</c>、オプションを付けない <c>&lt;コマンド&gt;</c> を受ける。コマンドは 1〜256 文字。
    /// </summary>
    public sealed class CliArguments
    {
        public const int MaxCommandLength = 256;

        public string Command { get; private set; }
        public string Error { get; private set; }
        public bool ShowHelp { get; private set; }

        public static CliArguments Parse(string[] args)
        {
            var result = new CliArguments();
            if (args == null || args.Length == 0)
            {
                result.ShowHelp = true;
                return result;
            }

            string first = args[0];
            string lower = first.ToLowerInvariant();
            if (lower == "-h" || lower == "--help" || lower == "-?" || lower == "/?" || lower == "help")
            {
                result.ShowHelp = true;
                return result;
            }

            string command;
            if (lower == "-command" || lower == "command")
            {
                if (args.Length < 2)
                {
                    result.Error = "Command string not given";
                    return result;
                }
                command = args[1];
            }
            else
            {
                command = first;
            }

            if (command.Length == 0 || command.Length > MaxCommandLength)
            {
                result.Error = "Command length is invalid";
                return result;
            }

            result.Command = command;
            return result;
        }

        /// <summary>問い合わせ（結果を返すコマンド）か。</summary>
        public bool IsQuery => Command != null && Command.StartsWith("query.", StringComparison.OrdinalIgnoreCase);
    }
}
