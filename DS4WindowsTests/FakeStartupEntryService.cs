using DS4Windows.Core.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Windows のスタートアップ登録（スタートアップ フォルダーのショートカットと、タスク スケジューラー）に触れない偽物。
    /// 本物の <c>StartupEntryService.HasTaskEntry</c> は COM のタスク スケジューラーへ問い合わせるため、1 回約 0.35 秒かかり、利用者の環境の状態にも左右される
    /// （Phase14d-Step4 で、設定画面の ViewModel を作るテストの時間の大半がここだと分かった）。登録の有無は、プロパティで与える。
    /// </summary>
    internal sealed class FakeStartupEntryService : IStartupEntryService
    {
        public bool StartProgEntry { get; set; }
        public bool TaskEntry { get; set; }

        public bool HasStartProgEntry() => StartProgEntry;
        public bool HasTaskEntry() => TaskEntry;
        public void WriteStartProgEntry(string exeLocation, string programDirectory) => StartProgEntry = true;
        public void DeleteStartProgEntry() => StartProgEntry = false;
        public void DeleteOldTaskEntry(string programDirectory) { }
        public bool CanWriteStartEntry() => true;
        public void WriteTaskEntry(string programDirectory, string exeFileName) => TaskEntry = true;
        public void DeleteTaskEntry() => TaskEntry = false;
        public bool CheckStartupExeLocation(string exeLocation) => false;
        public void LaunchOldTask() { }
    }
}
