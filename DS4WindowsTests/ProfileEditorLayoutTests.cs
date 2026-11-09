using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
    public class ProfileEditorLayoutTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileEditorLayoutTests(ITestOutputHelper output)
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

        private const string ProfileEditorXaml = "DS4Windows/UI/ProfileEditor.xaml";

        private XDocument LoadXaml(string relative)
        {
            XDocument document = XamlTestHelper.Load(relative);
            if (document == null)
            {
                _output.WriteLine(relative + " が見つからないため検査を省略した。");
            }

            return document;
        }

        /// <summary>タブ（<c>TabItem</c>）の識別名。名前つきのタブは <c>x:Name</c>、ほかは <c>Header="{lex:Loc …}"</c> のキー。</summary>
        private static string[] TopLevelTabs(XElement pane)
        {
            return XamlTestHelper.ChildElements(pane, "TabItem").Select(XamlTestHelper.Describe).ToArray();
        }

        private static XElement LeftPane(XDocument xaml) => XamlTestHelper.FindByName(xaml, "sidebarTabControl");

        private static XElement RightPane(XDocument xaml) => XamlTestHelper.FindByName(xaml, "profileSettingsTabCon");

        [Fact]
        public void TheLeftPane_HasControls_SpecialActions_Touchpad_Gyro_ControllerReadings_InThatOrder()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            if (xaml == null) return;

            // Phase10-Step3b W1: Touchpad・ジャイロを、Special Actions の次へ移した（Controller Readings は、最後）
            Assert.Equal(new[] { "Controls", "specialActionsTab", "touchpadTab", "gyroTab", "contReadingsTab" }, TopLevelTabs(LeftPane(xaml)));
        }

        [Fact]
        public void TheRightPane_HasOnlyAxisConfig_Lightbar_Other_InThatOrder()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            if (xaml == null) return;

            Assert.Equal(new[] { "AxisConfig", "Lightbar", "Other" }, TopLevelTabs(RightPane(xaml)));
        }

        [Fact]
        public void ControlsIsTheFirstLeftTab_SoThePresetButtonsIndexZeroStillMeansControls()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            string code = Read("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (xaml == null || code == null) return;

            Assert.Equal("Controls", TopLevelTabs(LeftPane(xaml)).First());
            Assert.Contains("sidebarTabControl.SelectedIndex = 0;", code);
        }

        [Fact]
        public void NoCodeDependsOnTheRightPanesTabNumberOrSelection()
        {
            string code = Read("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (code == null) return;

            // 右ペインのタブの移動で壊れるコード（番号、選択中の項目、項目の一覧への依存）がない
            Assert.Empty(Regex.Matches(code, @"profileSettingsTabCon\.(SelectedIndex|SelectedItem|SelectedContent|Items|SelectionChanged)"));

            // 右ペインへの参照は、DataContext の設定・解除の 4 か所だけ
            Assert.Equal(4, Regex.Matches(code, @"profileSettingsTabCon\.").Count);
            Assert.Equal(2, Regex.Matches(code, @"profileSettingsTabCon\.DataContext = profileSettingsVM;").Count);
            Assert.Equal(2, Regex.Matches(code, @"profileSettingsTabCon\.DataContext = null;").Count);

            // Phase10-Step3b W1: 左ペインへ移した 2 つのタブにも、同じ場所で、同じ設定・解除を行う（古い参照を残さない）
            // ジャイロのタブの DataContext は、サブ ViewModel（profileSettingsVM.Gyro）
            foreach (var (tab, value) in new[] { ("touchpadTab", @"profileSettingsVM\.Touchpad"), ("gyroTab", @"profileSettingsVM\.Gyro") })
            {
                Assert.Equal(2, Regex.Matches(code, tab + @"\.DataContext = " + value + ";").Count);
                Assert.Equal(2, Regex.Matches(code, tab + @"\.DataContext = null;").Count);
            }
        }

        [Fact]
        public void TheMovedTabs_UseOnlyThreeStaticResources_AndEachDefinesItsOwnSpaceMargin()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            if (xaml == null) return;

            // タッチパッドのタブの中身は、TouchpadSettingsPanel（UserControl）にある。そのリソースを自分で持つ
            XDocument touchpadXaml = LoadXaml("DS4Windows/UI/TouchpadSettingsPanel.xaml");
            if (touchpadXaml == null) return;
            XElement touchpad = touchpadXaml.Root;
            // ジャイロのタブの中身は、GyroSettingsPanel（UserControl）にある。そのリソースを自分で持つ
            XDocument gyroXaml = LoadXaml("DS4Windows/UI/GyroSettingsPanel.xaml");
            if (gyroXaml == null) return;
            XElement gyro = gyroXaml.Root;

            foreach (XElement tab in new[] { touchpad, gyro })
            {
                // NumberInput* は、テーマが持つ数値入力の共有スタイル（最小幅）で、画面の外に定義がある
                Assert.Equal(new[] { "InvertBoolToVisibilityConverter", "controlIndexCheck", "spaceMargin" },
                    XamlTestHelper.StaticResourceKeys(tab).Where(k => !k.StartsWith("NumberInput", StringComparison.Ordinal)).ToArray());
            }

            // InvertBoolToVisibilityConverter・controlIndexCheck は、画面全体のリソース（左右のペインの外）にある
            XElement sidebar = LeftPane(xaml);
            XElement rightPane = RightPane(xaml);
            foreach (string key in new[] { "InvertBoolToVisibilityConverter", "controlIndexCheck" })
            {
                XElement definition = xaml.Descendants().FirstOrDefault(e => XamlTestHelper.KeyOf(e) == key);
                Assert.True(definition != null, "x:Key=\"" + key + "\" の定義がない");
                Assert.DoesNotContain(definition.Ancestors(), a => ReferenceEquals(a, sidebar) || ReferenceEquals(a, rightPane));
            }

            // spaceMargin: どちらのタブも、自分のタブに持つ（ジャイロは、以前は右ペインの TabControl.Resources から取っていたため、W1 で、自分のタブに持たせた）
            Assert.Contains("spaceMargin", XamlTestHelper.OwnResourceKeys(touchpad));
            Assert.Contains("spaceMargin", XamlTestHelper.OwnResourceKeys(gyro));

            // 右ペインの見出しの高さ調整（Touchpad の Height／VerticalAlignment）は、外した
            Assert.Null(XamlTestHelper.Attr(touchpad, "Height"));
            Assert.Null(XamlTestHelper.Attr(touchpad, "VerticalAlignment"));
        }

        [Fact]
        public void TheLeftTabSelectionHandler_IgnoresEventsBubblingUpFromTheTabsContents()
        {
            string code = Read("DS4Windows/UI/ProfileEditor.xaml.cs");
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
        public void TheAxisConfigTab_NoLongerHasTheFourEntryButtons_ButKeepsTheReadOnlyAssignmentNames()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            if (xaml == null) return;

            XElement axis = XamlTestHelper.ChildElements(RightPane(xaml), "TabItem").First();
            Assert.Equal("AxisConfig", XamlTestHelper.Describe(axis));

            // Phase10-Step3b W2: 入口のボタンは、Controls タブへ移した（割り当てエディターの入口を、すべて左ペインのタブのボタンにする）
            string[] values = XamlTestHelper.AllAttributeValues(axis).ToArray();
            Assert.DoesNotContain(values, v => v.Contains("StickOuterBindButton_Click"));
            Assert.DoesNotContain(values, v => v.Contains("TriggerFullPullBtn_Click"));

            // 割り当て名の表示ラベルは、読み取り専用の表示として残す
            foreach (string label in new[] { "lsOuterBindLb", "rsOuterBindLb" })
            {
                Assert.True(XamlTestHelper.HasName(axis, label), label + " が Axis Config のタブにない");
            }
            // L2・R2 の部分は TriggerSettingsPanel（Axis Config のタブの中身の一部）にある
            XDocument triggerXaml = LoadXaml("DS4Windows/UI/TriggerSettingsPanel.xaml");
            if (triggerXaml == null) return;
            foreach (string label in new[] { "l2FullPullLb", "r2FullPullLb" })
            {
                Assert.True(XamlTestHelper.HasName(triggerXaml.Root, label), label + " が TriggerSettingsPanel にない");
            }
        }

        [Fact]
        public void TheAssignmentNameLabels_GetTheirDataContextFromTheMappingList()
        {
            string code = Read("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (code == null) return;

            // Axis Config の読み取り専用のラベルと、Controls タブの入口の横のラベル（Phase10-Step3b W2）。どちらも、同じ割り当て名を結ぶ
            // L2・R2 のラベルは TriggerSettingsPanel が持ち、編集画面は割り当ての一覧を渡して結ばせる
            Assert.Contains("triggersPanel.AssignFullPullAssociation(mappingListVM);", code);
            string panelCode = Read("DS4Windows/UI/TriggerSettingsPanel.xaml.cs");
            if (panelCode == null) return;
            Assert.Contains("l2FullPullLb.DataContext = mappingList.ControlMap[DS4Controls.L2FullPull];", panelCode);
            Assert.Contains("r2FullPullLb.DataContext = mappingList.ControlMap[DS4Controls.R2FullPull];", panelCode);
            Assert.Contains("lsOuterBindLb.DataContext = mappingListVM.ControlMap[DS4Controls.LSOuter];", code);
            Assert.Contains("rsOuterBindLb.DataContext = mappingListVM.ControlMap[DS4Controls.RSOuter];", code);
        }

        [Fact]
        public void TheControlsTab_IsStillA470WideGrid_WithACanvasRowAndAListRow()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            if (xaml == null) return;

            XElement controls = XamlTestHelper.ChildElements(LeftPane(xaml), "TabItem").First();
            Assert.Equal("Controls", XamlTestHelper.Describe(controls));

            // Phase10-Step3b W2（ユーザーの指示）: 4 つの入口は、専用のボタンではなく、下の文字の一覧に、ほかのボタンと同じ形式で加えた。
            // 絵と一覧の構成（幅 470、行は Canvas［高さは下記］と一覧［*］の 2 つ）は、変えていない
            // 構造だけを確かめる。属性の並び・空白・改行・「/>」の前の空白は、XAML の整形で変わるため、固定しない
            XElement grid = XamlTestHelper.ChildElements(controls, "Grid").Single(g => XamlTestHelper.Attr(g, "Width") == "470");
            XElement rows = XamlTestHelper.PropertyElement(grid, "RowDefinitions");
            Assert.NotNull(rows);
            string[] heights = XamlTestHelper.ChildElements(rows, "RowDefinition").Select(r => XamlTestHelper.Attr(r, "Height")).ToArray();
            Assert.Equal(new[] { "252", "*" }, heights);   // Phase9 UI 変更: 割り当て表示の下に余白（240 → 252）

            // 絵の上のボタンは、コントローラーの図のコントロール（配置表から生成）。旧: Canvas
            XElement canvas = XamlTestHelper.ChildElements(grid, "ControllerVisualizerControl").Single();
            Assert.Equal("0", XamlTestHelper.Attr(canvas, "Grid.Row"));
            Assert.Equal("conCanvas", XamlTestHelper.NameOf(canvas));

            XElement list = XamlTestHelper.ChildElements(grid, "ListView").Single();   // Phase9 UI 変更: 割り当て一覧は、2 列の表（ListView）
            Assert.Equal("1", XamlTestHelper.Attr(list, "Grid.Row"));
            Assert.Equal("mappingListBox", XamlTestHelper.NameOf(list));

            Assert.False(XamlTestHelper.HasName(controls, "axisEntriesPanel"));
            Assert.DoesNotContain(XamlTestHelper.AllAttributeValues(controls), v => v.Contains("axisEntriesPanel"));
        }

        [Fact]
        public void TheFourEntries_AreTheLastItemsOfTheMappingList_InTheSameFormatAsTheOtherButtons()
        {
            string vm = Read("DS4Windows/UI/ViewModels/MappingListViewModel.cs");
            if (vm == null) return;

            // 一覧の末尾に、4 つを、ほかのボタンと同じ MappedControl（表示名: 「LS Outer: …」の形式）で加える。コントローラーの入力による選択の添字（0〜33）は、変わらない
            string[] order =
            {
                "mappings.Add(new MappedControl(devIndex, DS4Controls.BRP, InputControlCatalog.LabelOf(DS4Controls.BRP), devType, profileSettingsService));",
                "lsOuterBindControl = new MappedControl(devIndex, DS4Controls.LSOuter, InputControlCatalog.LabelOf(DS4Controls.LSOuter), devType, profileSettingsService);",
                "rsOuterBindControl = new MappedControl(devIndex, DS4Controls.RSOuter, InputControlCatalog.LabelOf(DS4Controls.RSOuter), devType, profileSettingsService);",
                "l2FullPullControl = new MappedControl(devIndex, DS4Controls.L2FullPull, InputControlCatalog.LabelOf(DS4Controls.L2FullPull), devType, profileSettingsService);",
                "r2FullPullControl = new MappedControl(devIndex, DS4Controls.R2FullPull, InputControlCatalog.LabelOf(DS4Controls.R2FullPull), devType, profileSettingsService);",
                "mappings.Add(lsOuterBindControl);",
                "mappings.Add(rsOuterBindControl);",
                "mappings.Add(l2FullPullControl);",
                "mappings.Add(r2FullPullControl);",
                "foreach (MappedControl mapped in mappings)",
            };

            int at = -1;
            foreach (string line in order)
            {
                int next = vm.IndexOf(line, at + 1, StringComparison.Ordinal);
                Assert.True(next > at, line + " が、期待の順序にない");
                at = next;
            }

            // 一覧とは別の「追加のコントロール」の扱い（extraControls）は、なくなった
            Assert.DoesNotContain("extraControls", vm);

            // ControlMap・ControlIndexMap は、一覧と同じループで作る（4 つも、含まれる）
            Assert.Single(Regex.Matches(vm, @"controlMap\.Add\("));
            Assert.Single(Regex.Matches(vm, @"controlIndexMap\.Add\("));
        }

        [Fact]
        public void NoEntryClickHandlerIsLeftInTheXaml_ExceptTheOnesOfTheLeftPane()
        {
            XDocument xaml = LoadXaml(ProfileEditorXaml);
            string code = Read("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (xaml == null || code == null) return;

            // 専用のボタンがなくなったため、Full Pull・Outer Bind のクリックのハンドラーも、なくなった（入口は、一覧のダブルクリック）
            string[] xamlValues = XamlTestHelper.AllAttributeValues(xaml.Root).ToArray();
            Assert.DoesNotContain(xamlValues, v => v.Contains("StickOuterBindButton_Click"));
            Assert.DoesNotContain(xamlValues, v => v.Contains("TriggerFullPullBtn_Click"));
            Assert.DoesNotContain("StickOuterBindButton_Click", code);
            Assert.DoesNotContain("TriggerFullPullBtn_Click", code);

            // 残る入口の Click ハンドラーは、すべて、左ペインのタブにある（右ペインに、入口がない）
            string[] right = XamlTestHelper.AllAttributeValues(RightPane(xaml)).ToArray();
            string[] left = XamlTestHelper.AllAttributeValues(LeftPane(xaml)).ToArray();
            // 傾き（ジャイロのタブ）の入口は GyroSettingsPanel にある（編集画面の左ペインのタブの中身）
            XDocument gyroXaml = LoadXaml("DS4Windows/UI/GyroSettingsPanel.xaml");
            string[] gyroValues = gyroXaml == null ? left : XamlTestHelper.AllAttributeValues(gyroXaml.Root).ToArray();
            Assert.DoesNotContain(right, v => v == "TiltControlsButton_Click");
            Assert.Contains(gyroValues, v => v == "TiltControlsButton_Click");
            // スワイプ（タッチパッドのタブ）の入口は TouchpadSettingsPanel にある（編集画面の左ペインのタブの中身）
            XDocument touchpadXaml = LoadXaml("DS4Windows/UI/TouchpadSettingsPanel.xaml");
            string[] touchpadValues = touchpadXaml == null ? left : XamlTestHelper.AllAttributeValues(touchpadXaml.Root).ToArray();
            Assert.DoesNotContain(right, v => v == "SwipeControlsButton_Click");
            Assert.Contains(touchpadValues, v => v == "SwipeControlsButton_Click");

            // 図のボタン（配置表から生成）のクリックは、XAML ではなくコードで結び付ける。入口は左ペインの図のコントロールだけ
            Assert.Matches(@"button\.Click \+= HoverConBtn_Click;", code);
            Assert.Contains(left, v => v.Contains("DualShock4Editor"));
        }
    }
}
