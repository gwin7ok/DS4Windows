using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2h（R-4）: ウインドウ名の一致方法の一覧（保存用の印との相互変換）と、「実行ファイル名のみで判定」の変換。
    /// </summary>
    public class Phase12Step2W2hWindowTitleMatchModeTests
    {
        [Theory]
        [InlineData("", "", WindowTitleMatchMode.Exact)]
        [InlineData(null, "", WindowTitleMatchMode.Exact)]
        [InlineData("Notepad", "Notepad", WindowTitleMatchMode.Exact)]
        [InlineData("^Untitled", "Untitled", WindowTitleMatchMode.StartsWith)]
        [InlineData("Notepad$", "Notepad", WindowTitleMatchMode.EndsWith)]
        [InlineData("*Note", "Note", WindowTitleMatchMode.Contains)]
        [InlineData("^", "^", WindowTitleMatchMode.Exact)]       // 1 文字は印とみなさない（照合の規則と同じ）
        [InlineData("$", "$", WindowTitleMatchMode.Exact)]
        [InlineData("^a$", "a$", WindowTitleMatchMode.StartsWith)]   // ^ が優先（照合の規則と同じ）
        public void ParseTitle_SplitsTheMarkerIntoTheMode(string stored, string text, WindowTitleMatchMode mode)
        {
            Assert.Equal(text, WindowTargetText.ParseTitle(stored, out WindowTitleMatchMode actual));
            Assert.Equal(mode, actual);
        }

        [Theory]
        [InlineData("", WindowTitleMatchMode.Contains, "")]
        [InlineData("Notepad", WindowTitleMatchMode.Exact, "Notepad")]
        [InlineData("Untitled", WindowTitleMatchMode.StartsWith, "^Untitled")]
        [InlineData("Notepad", WindowTitleMatchMode.EndsWith, "Notepad$")]
        [InlineData("Note", WindowTitleMatchMode.Contains, "*Note")]
        public void ComposeTitle_AddsTheMarker(string text, WindowTitleMatchMode mode, string expected)
        {
            Assert.Equal(expected, WindowTargetText.ComposeTitle(text, mode));
        }

        [Theory]
        [InlineData("Notepad")]
        [InlineData("^Untitled")]
        [InlineData("Notepad$")]
        [InlineData("*Note")]
        [InlineData("")]
        public void ParseThenCompose_KeepsTheStoredText(string stored)
        {
            string text = WindowTargetText.ParseTitle(stored, out WindowTitleMatchMode mode);

            Assert.Equal(stored, WindowTargetText.ComposeTitle(text, mode));
        }

        [Theory]
        [InlineData("^Untitled", WindowTitleMatchMode.StartsWith, "Untitled - Notepad", true)]
        [InlineData("Notepad$", WindowTitleMatchMode.EndsWith, "Untitled - Notepad", true)]
        [InlineData("*itled", WindowTitleMatchMode.Contains, "Untitled - Notepad", true)]
        [InlineData("Untitled - Notepad", WindowTitleMatchMode.Exact, "Untitled - Notepad", true)]
        [InlineData("Untitled", WindowTitleMatchMode.Exact, "Untitled - Notepad", false)]
        public void ComposedTitle_IsInterpretedByTheMatcherAsTheChosenMode(string stored, WindowTitleMatchMode mode, string windowTitle, bool expected)
        {
            string text = WindowTargetText.ParseTitle(stored, out WindowTitleMatchMode parsed);
            Assert.Equal(mode, parsed);

            var matcher = new WindowTargetMatcher(string.Empty, WindowTargetText.ComposeTitle(text, parsed));

            Assert.Equal(expected, matcher.IsMatch(string.Empty, windowTitle.ToLower()));
        }

        [Theory]
        [InlineData("C:\\Windows\\notepad.exe", "\\notepad.exe$")]
        [InlineData("c:/windows/notepad.exe", "\\notepad.exe$")]
        [InlineData("^C:\\Games\\game.exe", "\\game.exe$")]
        [InlineData("*game.exe", "\\game.exe$")]
        [InlineData("C:\\Games\\game.exe$", "\\game.exe$")]
        [InlineData("notepad.exe", "\\notepad.exe$")]
        [InlineData("\\notepad.exe$", "\\notepad.exe$")]
        [InlineData("  C:\\x\\a.exe  ", "\\a.exe$")]
        public void ToExecutableNameOnly_KeepsOnlyTheFileName_AsAnEndsWithMatch(string path, string expected)
        {
            Assert.Equal(expected, WindowTargetText.ToExecutableNameOnly(path));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("C:\\Games\\")]   // ファイル名が取れない
        public void ToExecutableNameOnly_LeavesAnUnusableValueAsItIs(string path)
        {
            Assert.Equal(path, WindowTargetText.ToExecutableNameOnly(path));
        }

        [Fact]
        public void ToExecutableNameOnly_MatchesTheSameExecutableInAnyFolder_ButNotALongerName()
        {
            var matcher = new WindowTargetMatcher(WindowTargetText.ToExecutableNameOnly("C:\\Windows\\notepad.exe"), string.Empty);

            Assert.True(matcher.IsMatch("d:\\other\\notepad.exe", string.Empty));
            Assert.False(matcher.IsMatch("d:\\other\\xnotepad.exe", string.Empty));
        }

        [Fact]
        public void ViewModel_ComposesTheStoredTitleFromTheTextAndTheMode_AndSavesIt()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = new FocusWindowViewModel(repo) { WindowPath = "x" };
            vm.TitleText = "Untitled";
            vm.TitleMatchMode = WindowTitleMatchMode.StartsWith;

            Assert.Equal("^Untitled", vm.WindowTitle);

            vm.SaveAction(new SpecialAction("N", "Cross", "FocusWindow", "x"), edit: false);
            Assert.Equal("^Untitled", repo.SavedExtras);
            Assert.Equal("^Untitled", vm.ToControlItem(SpecialAction.ActionTypeId.FocusWindow).WindowTitle);
        }

        [Fact]
        public void ViewModel_LoadsAnExistingMarkedTitleIntoTheTextAndTheMode()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository());

            vm.LoadAction(new SpecialAction("N", "Cross", "FocusWindow", "x", 0, "*Note"));

            Assert.Equal("Note", vm.TitleText);
            Assert.Equal(WindowTitleMatchMode.Contains, vm.TitleMatchMode);
            Assert.Equal("*Note", vm.WindowTitle);
        }

        [Fact]
        public void ViewModel_DefaultsToExactMatch()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository());

            Assert.Equal(WindowTitleMatchMode.Exact, vm.TitleMatchMode);
            vm.TitleText = "Window";
            Assert.Equal("Window", vm.WindowTitle);
        }

        [Fact]
        public void ViewModel_TypedMarkerIsStripped_AndTheChosenModeWins()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository()) { TitleMatchMode = WindowTitleMatchMode.EndsWith };

            vm.TitleText = "*Note";

            Assert.Equal("Note", vm.TitleText);
            Assert.Equal(WindowTitleMatchMode.EndsWith, vm.TitleMatchMode);
            Assert.Equal("Note$", vm.WindowTitle);
        }

        [Fact]
        public void ViewModel_ValidationUsesTheTitleText_NotTheMarker()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository()) { TitleMatchMode = WindowTitleMatchMode.Contains };

            // 一致方法だけを選んでも、本文が空なら、タイトルの指定は空（保存できない）
            Assert.False(vm.IsValid(new SpecialAction("N", "Cross", "FocusWindow", "x")));

            vm.TitleText = "Note";
            Assert.True(vm.IsValid(new SpecialAction("N", "Cross", "FocusWindow", "x")));
        }
    }
}
