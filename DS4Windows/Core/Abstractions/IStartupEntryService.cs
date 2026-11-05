namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase14b-Step15（決定 D12）: Windows のスタートアップ登録の契約（実装は <c>Core/Platform/StartupEntryService</c>）。
    /// 設定画面（<c>SettingsViewModel</c>）が使う。旧 静的クラス <c>StartupMethods</c> のメソッドと同じ並び・意味。
    /// </summary>
    public interface IStartupEntryService
    {
        /// <summary>スタートアップフォルダーにショートカットがあるか。</summary>
        bool HasStartProgEntry();

        /// <summary>タスクスケジューラーにタスク「RunDS4Windows」があるか。</summary>
        bool HasTaskEntry();

        /// <summary>スタートアップフォルダーにショートカットを作る。</summary>
        /// <param name="exeLocation">実行ファイルのフルパス（IPathService.ExecutablePath）</param>
        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        void WriteStartProgEntry(string exeLocation, string programDirectory);

        /// <summary>スタートアップフォルダーのショートカットを消す（読み取り専用なら消さない）。</summary>
        void DeleteStartProgEntry();

        /// <summary>別の task.bat を指す古いタスクを消す。</summary>
        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        void DeleteOldTaskEntry(string programDirectory);

        /// <summary>ショートカットを書き換えられるか（読み取り専用でないか）。</summary>
        bool CanWriteStartEntry();

        /// <summary>task.bat を作り直し、ログオン時に実行するタスクを登録する。</summary>
        /// <param name="programDirectory">プログラムフォルダ（IPathService.ProgramDirectory）</param>
        /// <param name="exeFileName">実行ファイル名（Path.GetFileName(IPathService.ExecutablePath)）</param>
        void WriteTaskEntry(string programDirectory, string exeFileName);

        /// <summary>タスク「RunDS4Windows」を消す。</summary>
        void DeleteTaskEntry();

        /// <summary>ショートカットの指す先が、今の実行ファイルと違うか。</summary>
        /// <param name="exeLocation">実行ファイルのフルパス（IPathService.ExecutablePath）</param>
        bool CheckStartupExeLocation(string exeLocation);

        /// <summary>タスク「RunDS4Windows」があれば実行する（起動引数 -runtask）。</summary>
        void LaunchOldTask();
    }
}
