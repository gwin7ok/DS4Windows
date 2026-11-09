using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W2: Controller Readings タブの数値が、最大桁の値でも見切れないことを確かめる（見た目のみの不具合の再発防止）。
    /// 1) 各数値ラベルの宣言（幅・余白）が、最大桁の値に足りること
    /// 2) 実物のコントロールを左ペインの幅で配置し、数値ラベルとその入れ物（親の要素）が、割り当てられた幅からはみ出して切られないこと
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ControllerReadingsNoClippingTests
    {
        private const string ReadingsXaml = "DS4Windows/UI/ControllerReadingsControl.xaml";

        // 左ペインの幅。Controls タブ（ProfileEditor.xaml の Grid Width=470）が決める。実際の幅はこれ以上
        private const double PaneWidth = 470;

        // 値の範囲（ControllerReadingsControl.xaml.cs の UpdateUiControls と、DS4Touchpad・SixAxis の定数による）
        //   スティック・トリガー: 0〜255。タッチパッド: X 0〜1920、Y 0〜942。加速度: -aX/64 なので ±512 ほど
        private static readonly Dictionary<string, string> WorstCase = new Dictionary<string, string>
        {
            ["lxInValLb"] = "255", ["lxOutValLb"] = "255", ["lyInValLb"] = "255", ["lyOutValLb"] = "255",
            ["rxInValLb"] = "255", ["rxOutValLb"] = "255", ["ryInValLb"] = "255", ["ryOutValLb"] = "255",
            ["l2InValLb"] = "255", ["l2OutValLb"] = "255", ["r2InValLb"] = "255", ["r2OutValLb"] = "255",
            ["touchXValLb"] = "1920", ["touchYValLb"] = "942",
            ["sixAxisXInValLb"] = "-512", ["sixAxisXOutValLb"] = "-512",
            ["sixAxisZInValLb"] = "-512", ["sixAxisZOutValLb"] = "-512",
        };

        // フォント（null は、この PC の WPF の既定。設定によってサイズが 12 を超えることがある）と、サイズ。
        // 通常の Windows の 12、大きめの設定の 16（このリポジトリの作者の環境は 16）で確かめる
        private static readonly (string Family, double Size)[] Fonts =
        {
            (null, 0),
            ("Segoe UI", 12), ("Yu Gothic UI", 12), ("Meiryo UI", 12),
            ("Segoe UI", 16),
        };

        private readonly ITestOutputHelper _output;

        public ControllerReadingsNoClippingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EveryReadoutLabel_IsWideEnoughForItsLargestValue()
        {
            XDocument xaml = XamlTestHelper.Load(ReadingsXaml);
            if (xaml == null)
            {
                _output.WriteLine(ReadingsXaml + " が見つからないため検査を省略した。");
                return;
            }

            var tooNarrow = new List<string>();
            RunOnSta(() =>
            {
                foreach (KeyValuePair<string, string> pair in WorstCase)
                {
                    XElement label = XamlTestHelper.FindByName(xaml, pair.Key);
                    double declared = double.Parse(DeclaredAttr(label, "Width"), CultureInfo.InvariantCulture);
                    Thickness padding = (Thickness)new ThicknessConverter().ConvertFromInvariantString(DeclaredAttr(label, "Padding"));

                    foreach ((string family, double size) in Fonts)
                    {
                        double needed = MeasureNeededWidth(pair.Value, padding, family, size);
                        _output.WriteLine($"{pair.Key,-18} 値 {pair.Value,-5} 宣言 {declared,5:0.0}  必要 {needed,6:0.0}  余裕 {declared - needed,6:0.0}  {FontName(family, size)}");
                        if (needed > declared)
                        {
                            tooNarrow.Add($"{pair.Key}（値 {pair.Value}、{FontName(family, size)}）: 幅 {declared} に対し、必要な幅 {needed:0.0}");
                        }
                    }
                }
            });

            Assert.True(tooNarrow.Count == 0, "最大桁の値が入りきらない数値ラベルがある:\n" + string.Join("\n", tooNarrow));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void NoReadoutLabel_NorItsContainers_AreClippedAtThePaneWidth()
        {
            var clipped = new List<string>();
            RunOnSta(() =>
            {
                foreach ((string family, double size) in Fonts)
                {
                    ControllerReadingsControl control = CreateLaidOutControl(family, size);
                    var seen = new HashSet<FrameworkElement>();

                    foreach (string name in WorstCase.Keys)
                    {
                        var label = (Label)control.FindName(name);
                        Assert.NotNull(label);

                        // ラベルの文字そのもの（宣言の幅に収まっているか）
                        foreach (TextBlock text in Descendants<TextBlock>(label))
                        {
                            Report(text, $"{FontName(family, size)} {name} の文字", seen, clipped);
                        }

                        // ラベルを入れている親の要素（列の幅に収まっているか）
                        for (DependencyObject node = VisualTreeHelper.GetParent(label); node != null; node = VisualTreeHelper.GetParent(node))
                        {
                            if (node is FrameworkElement parent)
                            {
                                Report(parent, $"{FontName(family, size)} {name} を入れる {parent.GetType().Name}", seen, clipped);
                            }
                        }
                    }
                }
            });

            Assert.True(clipped.Count == 0, "数値ラベルが見切れる（割り当てられた幅を超えて切られる）:\n" + string.Join("\n", clipped));
        }

        private static void Report(FrameworkElement element, string what, HashSet<FrameworkElement> seen, List<string> clipped)
        {
            if (!seen.Add(element)) return;
            double slot = LayoutInformation.GetLayoutSlot(element).Width;
            // 割り当てより大きく描画される要素は、WPF が割り当ての範囲で切り取る（見切れ）
            if (element.RenderSize.Width > slot + 0.5)
            {
                clipped.Add($"{what}: 描画幅 {element.RenderSize.Width:0.0} が、割り当て {slot:0.0} を超える");
            }
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (T nested in Descendants<T>(child)) yield return nested;
            }
        }

        /// <summary>ラベルに書かれた属性。無ければ、Style で指定した共有スタイル（DefaultTheme.xaml）の Setter から読む</summary>
        private static string DeclaredAttr(XElement label, string name)
        {
            string direct = XamlTestHelper.Attr(label, name);
            if (direct != null) return direct;

            string style = XamlTestHelper.Attr(label, "Style");
            Match key = style == null ? null : Regex.Match(style, @"\{(?:Static|Dynamic)Resource (\w+)\}");
            Assert.True(key != null && key.Success, name + " の宣言が見つからない: " + label.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Name")?.Value);

            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            XDocument theme = XDocument.Load(SourceFileLocator.Find("DS4Windows/UI/Themes/DefaultTheme.xaml"));
            XElement styleElement = theme.Root.Elements().First(e => (string)e.Attribute(x + "Key") == key.Groups[1].Value);
            return (string)styleElement.Elements().First(e => (string)e.Attribute("Property") == name).Attribute("Value");
        }

        /// <summary>実物のコントロールに最大桁の値を入れ、左ペインの幅で配置する</summary>
        private static ControllerReadingsControl CreateLaidOutControl(string family, double size)
        {
            var control = new ControllerReadingsControl();
            // 実物のテーマ（数値ラベルの共有スタイルを含む）を、アプリと同じく画面の外のリソースとして与える
            var resources = (ResourceDictionary)Application.LoadComponent(new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
            control.Resources.MergedDictionaries.Add(resources);
            if (family != null)
            {
                control.FontFamily = new FontFamily(family);
                control.FontSize = size;
            }

            foreach (KeyValuePair<string, string> pair in WorstCase)
            {
                ((Label)control.FindName(pair.Key)).Content = int.Parse(pair.Value, CultureInfo.InvariantCulture);
            }

            control.Measure(new Size(PaneWidth, double.PositiveInfinity));
            control.Arrange(new Rect(0, 0, PaneWidth, control.DesiredSize.Height));
            control.UpdateLayout();
            return control;
        }

        /// <summary>同じ余白の Label に値を入れて、幅を指定せずに測った、必要な幅（余白を含む）</summary>
        private static double MeasureNeededWidth(string text, Thickness padding, string family, double size)
        {
            var label = new Label
            {
                Content = text,
                Padding = padding,
                HorizontalContentAlignment = HorizontalAlignment.Right,
            };
            if (family != null)
            {
                label.FontFamily = new FontFamily(family);
                label.FontSize = size;
            }
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return label.DesiredSize.Width;
        }

        private static string FontName(string family, double size)
        {
            if (family != null) return $"{family} {size}";
            var probe = new Label();
            return $"(この PC の既定: {probe.FontFamily.Source} {probe.FontSize})";
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                // Assert の失敗は、そのままの型で伝える（メッセージを読みやすくする）
                if (failure is Xunit.Sdk.XunitException) throw failure;
                throw new InvalidOperationException("STA スレッドで失敗: " + failure.Message, failure);
            }
        }
    }
}
