using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>{Binding …, FallbackValue={lex:Loc …}}</c> は、表示言語を切り替えたとき、WPFLocalizeExtension が使用済みの Binding の
    /// <c>FallbackValue</c> を書き換えようとして、InvalidOperationException（バインドを、使用した後に変更することはできません）で落ちる。
    /// XAML に書かない（結びつけ先の ViewModel が文字を持つ）。
    /// </summary>
    public class XamlBindingPitfallGuardTests
    {
        [Fact]
        public void NoXaml_PutsALocalizeExtensionInsideAFallbackValue()
        {
            if (SourceText.Read("DS4Windows/UI/MainWindow.xaml") == null) return;

            string uiDir = Path.GetDirectoryName(SourceFileLocator.Find("DS4Windows/UI/MainWindow.xaml"))!;
            var offenders = Directory.EnumerateFiles(uiDir, "*.xaml", SearchOption.AllDirectories)
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"FallbackValue\s*=\s*\{lex:"))
                .Select(Path.GetFileName)
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void NoXaml_UsesMouseBindings()
        {
            // 一覧の項目のダブルクリックは、ListViewItem が MouseDown を処理済みにするため、MouseBinding には届かない（RelativeSource も解決できない）。MouseDoubleClick で結ぶ
            if (SourceText.Read("DS4Windows/UI/MainWindow.xaml") == null) return;
            string uiDir = Path.GetDirectoryName(SourceFileLocator.Find("DS4Windows/UI/MainWindow.xaml"))!;
            var offenders = Directory.EnumerateFiles(uiDir, "*.xaml", SearchOption.AllDirectories)
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"<(MouseBinding|KeyBinding)"))
                .Select(Path.GetFileName)
                .ToList();

            Assert.Empty(offenders);
        }
    }
}
