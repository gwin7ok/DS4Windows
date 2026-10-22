namespace DS4WinWPF
{
    /// <summary>
    /// Phase9 9-F F-1a（Phase9-F-Plan.md §5）: 昇格した更新の完了モード（<c>--complete-install</c>、<c>--complete-install-target</c>）のコマンドライン引数の読み取り。
    /// 旧 <c>App.Application_Startup</c> の中の同じループを、そのまま移した（状態なし）。
    /// </summary>
    public static class CompleteInstallArguments
    {
        /// <summary>
        /// 2 つの引数が、どちらも値つきで指定されているときだけ真。<paramref name="source"/>／<paramref name="target"/> は、最後に指定された値。
        /// </summary>
        public static bool TryParse(string[] args, out string source, out string target)
        {
            source = null;
            target = null;
            if (args == null) return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--complete-install" && i + 1 < args.Length)
                {
                    source = args[++i];
                }
                else if (args[i] == "--complete-install-target" && i + 1 < args.Length)
                {
                    target = args[++i];
                }
            }

            return !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target);
        }
    }
}
