using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3b W0（Phase10-Step3b-Plan.md §2・§5）: <c>ProfileEditor.xaml</c> の画面構成の特性化（ソース走査）。製品コードは変えない。
    /// 左ペイン（<c>sidebarTabControl</c>）と右ペイン（<c>profileSettingsTabCon</c>）のタブの構成、右ペインのタブが依存するリソース、
    /// 右ペインのタブの番号・選択に依存するコードがないこと、Axis Config の 4 つの入口（Outer Bind／Full Pull）の構成、Controls タブの行の構成を固定する。
    /// W1（Touchpad・ジャイロのタブの移動。実施済み）で、左右のタブの構成、リソース、DataContext の検査を、移動後の構成へ更新した。
    /// W2（4 つの入口の Controls タブへの追加）で、Axis Config の入口と Controls タブの行の検査を、変更後の構成へ更新する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step3bW0ProfileEditorLayoutTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step3bW0ProfileEditorLayoutTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relative)
        {
            string path = SourceFileLocator.Find(relative);
            if (path == null)
            {
                _output.WriteLine(relative + " が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string Between(string text, string startMarker, string endMarker)
        {
            int start = text.IndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, startMarker + " が見つからない");
            int end = endMarker == null ? text.Length : text.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
            Assert.True(end >= 0, endMarker + " が見つからない");
            return text.Substring(start, end - start);
        }

        /// <summary>タブ（12 空白の字下げの <c>&lt;TabItem</c>）の見出し。名前つきのタブは、<c>x:Name</c> を返す。</summary>
        private static string[] TopLevelTabs(string pane)
        {
            return Regex.Matches(pane, @"\n {12}<TabItem (?<attrs>[^>]*)>")
                .Cast<Match>()
                .Select(m =>
                {
                    string attrs = m.Groups["attrs"].Value;
                    Match name = Regex.Match(attrs, @"x:Name=""(?<n>\w+)""");
                    if (name.Success) return name.Groups["n"].Value;
                    Match header = Regex.Match(attrs, @"Header=""\{lex:Loc (?<h>\w+)\}""");
                    return header.Success ? header.Groups["h"].Value : attrs;
                })
                .ToArray();
        }

        private string LeftPane(string xaml) => Between(xaml, "<TabControl x:Name=\"sidebarTabControl\"", "<TabControl x:Name=\"profileSettingsTabCon\"");

        private string RightPane(string xaml) => Between(xaml, "<TabControl x:Name=\"profileSettingsTabCon\"", null);

        [Fact]
        public void TheLeftPane_HasControls_SpecialActions_Touchpad_Gyro_ControllerReadings_InThatOrder()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null) return;

            // Phase10-Step3b W1: Touchpad・ジャイロを、Special Actions の次へ移した（Controller Readings は、最後）
            Assert.Equal(new[] { "Controls", "specialActionsTab", "touchpadTab", "gyroTab", "contReadingsTab" }, TopLevelTabs(LeftPane(xaml)));
        }

        [Fact]
        public void TheRightPane_HasOnlyAxisConfig_Lightbar_Other_InThatOrder()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null) return;

            Assert.Equal(new[] { "AxisConfig", "Lightbar", "Other" }, TopLevelTabs(RightPane(xaml)));
        }

        [Fact]
        public void ControlsIsTheFirstLeftTab_SoThePresetButtonsIndexZeroStillMeansControls()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            string code = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.Equal("Controls", TopLevelTabs(LeftPane(xaml)).First());
            Assert.Contains("sidebarTabControl.SelectedIndex = 0;", code);
        }

        [Fact]
        public void NoCodeDependsOnTheRightPanesTabNumberOrSelection()
        {
            string code = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (code == null) return;

            // 右ペインのタブの移動で壊れるコード（番号、選択中の項目、項目の一覧への依存）がない
            Assert.Empty(Regex.Matches(code, @"profileSettingsTabCon\.(SelectedIndex|SelectedItem|SelectedContent|Items|SelectionChanged)"));

            // 右ペインへの参照は、DataContext の設定・解除の 4 か所だけ
            Assert.Equal(4, Regex.Matches(code, @"profileSettingsTabCon\.").Count);
            Assert.Equal(2, Regex.Matches(code, @"profileSettingsTabCon\.DataContext = profileSettingsVM;").Count);
            Assert.Equal(2, Regex.Matches(code, @"profileSettingsTabCon\.DataContext = null;").Count);

            // Phase10-Step3b W1: 左ペインへ移した 2 つのタブにも、同じ場所で、同じ設定・解除を行う（古い参照を残さない）
            foreach (string tab in new[] { "touchpadTab", "gyroTab" })
            {
                Assert.Equal(2, Regex.Matches(code, tab + @"\.DataContext = profileSettingsVM;").Count);
                Assert.Equal(2, Regex.Matches(code, tab + @"\.DataContext = null;").Count);
            }
        }

        [Fact]
        public void TheMovedTabs_UseOnlyThreeStaticResources_AndEachDefinesItsOwnSpaceMargin()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null) return;

            string touchpad = Between(xaml, "<TabItem x:Name=\"touchpadTab\"", "<TabItem x:Name=\"gyroTab\"");
            string gyro = Between(xaml, "<TabItem x:Name=\"gyroTab\"", "<TabItem x:Name=\"contReadingsTab\"");

            foreach (string tab in new[] { touchpad, gyro })
            {
                string[] keys = Regex.Matches(tab, @"StaticResource (\w+)").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray();
                Assert.Equal(new[] { "InvertBoolToVisibilityConverter", "controlIndexCheck", "spaceMargin" }, keys);
            }

            // InvertBoolToVisibilityConverter・controlIndexCheck は、画面全体のリソース（右ペインの外）にある
            string head = xaml.Substring(0, xaml.IndexOf("<TabControl x:Name=\"sidebarTabControl\"", StringComparison.Ordinal));
            Assert.Contains("x:Key=\"InvertBoolToVisibilityConverter\"", head);
            Assert.Contains("x:Key=\"controlIndexCheck\"", head);

            // spaceMargin: どちらのタブも、自分のタブに持つ（ジャイロは、以前は右ペインの TabControl.Resources から取っていたため、W1 で、自分のタブへ定義した）
            Assert.Contains("<TabItem.Resources>", touchpad);
            Assert.Contains("x:Key=\"spaceMargin\"", touchpad);
            Assert.Contains("<TabItem.Resources>", gyro);
            Assert.Contains("x:Key=\"spaceMargin\"", gyro);

            // 右ペインの見出しの高さ調整（Touchpad の Height／VerticalAlignment）は、外した
            string touchpadHeader = Regex.Match(touchpad, @"<TabItem x:Name=""touchpadTab""[^>]*>").Value;
            Assert.DoesNotContain("Height=", touchpadHeader);
            Assert.DoesNotContain("VerticalAlignment=", touchpadHeader);
        }

        [Fact]
        public void TheLeftTabSelectionHandler_IgnoresEventsBubblingUpFromTheTabsContents()
        {
            string code = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (code == null) return;

            // Touchpad・ジャイロの Output Mode（ComboBox）や入れ子の TabControl の選択変更が、左ペインのタブの選択変更として扱われない
            int handler = code.IndexOf("private void SidebarTabControl_SelectionChanged(", StringComparison.Ordinal);
            Assert.True(handler >= 0);
            int guard = code.IndexOf("if (e.OriginalSource != sidebarTabControl) return;", handler, StringComparison.Ordinal);
            int firstLog = code.IndexOf("[SidebarTabControl_SelectionChanged] SelectedItem=", handler, StringComparison.Ordinal);
            Assert.True(guard > handler, "判定がない");
            Assert.True(guard < firstLog, "判定が、最初のログ・処理より前にない");
        }

        [Fact]
        public void TheFourAxisConfigEntries_AreInTheAxisConfigTab_WithTheirClickHandlersAndTags()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null) return;

            string axis = Between(xaml, "<TabItem Header=\"{lex:Loc AxisConfig}\"", "<TabItem Header=\"{lex:Loc Lightbar}\"");

            Assert.Equal(2, Regex.Matches(axis, @"Click=""StickOuterBindButton_Click""").Count);
            Assert.Equal(2, Regex.Matches(axis, @"Click=""TriggerFullPullBtn_Click""").Count);
            foreach (string tag in new[] { "LSOuterBind", "RSOuterBind", "L2FullPull", "R2FullPull" })
            {
                Assert.Contains("Tag=\"{Binding " + tag + ", Source={StaticResource controlIndexCheck}}\"", axis);
            }

            // 割り当て名の表示ラベル（読み取り専用。W2 で、ボタンを Controls タブへ移したあとも残す）
            foreach (string label in new[] { "lsOuterBindLb", "rsOuterBindLb", "l2FullPullLb", "r2FullPullLb" })
            {
                Assert.Contains("x:Name=\"" + label + "\"", axis);
            }
        }

        [Fact]
        public void TheAssignmentNameLabels_GetTheirDataContextFromTheMappingList()
        {
            string code = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (code == null) return;

            Assert.Contains("l2FullPullLb.DataContext = mappingListVM.ControlMap[DS4Controls.L2FullPull];", code);
            Assert.Contains("r2FullPullLb.DataContext = mappingListVM.ControlMap[DS4Controls.R2FullPull];", code);
            Assert.Contains("lsOuterBindLb.DataContext = mappingListVM.ControlMap[DS4Controls.LSOuter];", code);
            Assert.Contains("rsOuterBindLb.DataContext = mappingListVM.ControlMap[DS4Controls.RSOuter];", code);
        }

        [Fact]
        public void TheControlsTab_IsA470WideGrid_WithACanvasRowAndAListRow()
        {
            string xaml = Read("DS4Windows/DS4Forms/ProfileEditor.xaml");
            if (xaml == null) return;

            string controls = Between(xaml, "<TabItem Header=\"{lex:Loc Controls}\">", "<TabItem x:Name=\"specialActionsTab\"");

            Assert.Contains("<Grid Width=\"470\">", controls);
            Match rows = Regex.Match(controls, @"<Grid\.RowDefinitions>(?<body>.*?)</Grid\.RowDefinitions>", RegexOptions.Singleline);
            Assert.True(rows.Success);
            Assert.Equal(2, Regex.Matches(rows.Groups["body"].Value, @"<RowDefinition ").Count);
            Assert.Contains("<RowDefinition Height=\"240\" />", rows.Groups["body"].Value);
            Assert.Contains("<RowDefinition Height=\"*\" />", rows.Groups["body"].Value);
            Assert.Contains("x:Name=\"conCanvas\"", controls);
            Assert.Contains("x:Name=\"mappingListBox\"", controls);
        }
    }
}
