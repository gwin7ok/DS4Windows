using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-6: 割り当てエディターで、Default だったボタンの種別を Output Key などに変えて出力ピッカーを開き、何も選ばずに × で閉じると、
    /// 種別の一覧が Default に戻って見えた。ピッカーで「Default」を選んだとき（<c>DefaultPicked</c>）、または Default 以外から Default に変わったときだけ切り替える。
    /// </summary>
    public class Phase11W6PickerCloseKeepsTypeTests
    {
        private readonly ITestOutputHelper _output;

        public Phase11W6PickerCloseKeepsTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Normalized(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return Regex.Replace(text, @"\s+", " ");
        }

        [Fact]
        public void TheEditor_SwitchesTheTypeListToDefault_OnlyWhenDefaultWasPickedOrChanged()
        {
            string code = Normalized("DS4Windows/UI/AssignmentEditor.xaml.cs");
            if (code == null) return;

            Assert.Contains("bool wasDefault = pressKeyVM.SelectedActionType == DS4Windows.Core.Settings.DS4ControlSettings.ActionType.Default; controlWindow.ShowDialog();", code);
            Assert.Contains("ActionType.Default && (!wasDefault || controlWindow.DefaultPicked)) { actionTypeCombo.SelectedIndex = specialActVM.TypeCatalog.DefaultIndex;", code);
        }

        [Fact]
        public void ThePicker_SetsDefaultPicked_OnlyWhereDefaultIsChosen()
        {
            string code = Normalized("DS4Windows/UI/BindingWindow.xaml.cs");
            if (code == null) return;

            Assert.Contains("public bool DefaultPicked { get; private set; }", code);
            Assert.Equal(2, Regex.Matches(code, @"DefaultPicked = true; (binding|actBind)\.outputType = OutBinding\.OutType\.Default;").Count);
            Assert.DoesNotContain("DefaultPicked = true; Close", code);
        }
    }
}
