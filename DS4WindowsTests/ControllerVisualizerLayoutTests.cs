using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI.Controls;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// コントローラーの図のボタンの配置表（<see cref="ControllerLayouts"/>）と <see cref="ControllerVisualizerControl"/> の検査。
    /// 期待値は、配置表へ移す前の BindingWindow が持っていた座標（Xbox 360 は XAML、DualShock 4 は InitDS4Canvas()）を、そのまま写したもの。
    /// 形式は「ID|Tag|左|上|幅|高さ|表示（1／0）」。
    /// </summary>
    public class ControllerVisualizerLayoutTests
    {
        private static readonly string[] Xbox360Expected =
        {
            "guideBtn|Guide|303|87|26|20|1",
            "aBtn|A|433|114|24|18|1",
            "bBtn|B|470|85|24|18|1",
            "xBtn|X|400|89|24|18|1",
            "yBtn|Y|437|59|24|18|1",
            "lbBtn|LB|27|59|64|18|1",
            "rbBtn|RB|536|62|64|18|1",
            "ltBtn|LT|48|17|42|36|1",
            "rtBtn|RT|544|20|42|36|1",
            "backBtn|Back|257|86|20|20|1",
            "startBtn|Start|353|88|20|20|1",
            "lsbBtn|Left Stick|174|102|16|16|1",
            "lsuBtn|LS Y-Axis-|159|81|47|16|1",
            "lsrBtn|LS X-Axis+|197|96|16|28|1",
            "lsdBtn|LS Y-Axis+|167|122|28|16|1",
            "lslBtn|LS X-Axis-|148|95|16|28|1",
            "rsbBtn|Right Stick|370|169|16|16|1",
            "rsuBtn|RS Y-Axis-|365|149|26|16|1",
            "rsrBtn|RS X-Axis+|392|161|16|28|1",
            "rsdBtn|RS Y-Axis+|363|189|28|16|1",
            "rslBtn|RS X-Axis-|347|167|16|16|1",
            "dpadUBtn|Dpad Up|240|136|16|12|1",
            "dpadRBtn|Dpad Right|268|150|16|30|1",
            "dpadDBtn|Dpad Down|244|187|16|12|1",
            "dpadLBtn|Dpad Left|214|148|16|30|1",
            "touchpadClickBtn|Touchpad Click|256|57|126|72|0"
        };

        private static readonly string[] DualShock4Expected =
        {
            "guideBtn|Guide|303|162|26|20|1",
            "aBtn|A|442|148|24|18|1",
            "bBtn|B|474|120|24|18|1",
            "xBtn|X|408|116|24|18|1",
            "yBtn|Y|440|90|24|18|1",
            "lbBtn|LB|154|24|46|20|1",
            "rbBtn|RB|428|24|46|20|1",
            "ltBtn|LT|162|6|46|20|1",
            "rtBtn|RT|428|6|46|20|1",
            "backBtn|Back|218|76|20|20|1",
            "startBtn|Start|395|76|20|20|1",
            "lsbBtn|Left Stick|238|182|16|16|1",
            "lsuBtn|LS Y-Axis-|230|160|32|16|1",
            "lsrBtn|LS X-Axis+|264|176|16|28|1",
            "lsdBtn|LS Y-Axis+|232|202|32|16|1",
            "lslBtn|LS X-Axis-|216|176|16|28|1",
            "rsbBtn|Right Stick|377|184|16|16|1",
            "rsuBtn|RS Y-Axis-|370|160|32|16|1",
            "rsrBtn|RS X-Axis+|400|176|16|28|1",
            "rsdBtn|RS Y-Axis+|370|200|32|16|1",
            "rslBtn|RS X-Axis-|352|176|16|28|1",
            "dpadUBtn|Dpad Up|170|100|16|12|1",
            "dpadRBtn|Dpad Right|194|112|16|30|1",
            "dpadDBtn|Dpad Down|170|144|16|12|1",
            "dpadLBtn|Dpad Left|144|112|16|30|1",
            "touchpadClickBtn|Touchpad Click|256|57|126|72|1"
        };

        private static readonly string[] EditorExpected =
        {
            "crossConBtn||330|125|26|26|1",
            "circleConBtn||357|99|26|26|1",
            "squareConBtn||300|98|26|26|1",
            "triangleConBtn||330|74|26|26|1",
            "l1ConBtn||75|22|50|18|1",
            "r1ConBtn||317|22|50|18|1",
            "l2ConBtn||78|0|46|20|1",
            "r2ConBtn||316|0|46|20|1",
            "shareConBtn||134|63|16|24|1",
            "optionsConBtn||291|63|16|24|1",
            "guideConBtn||211|144|20|20|1",
            "muteConBtn||0|0|0|0|0",
            "fnlConBtn||0|0|0|0|0",
            "fnrConBtn||0|0|0|0|0",
            "blpConBtn||0|0|0|0|0",
            "brpConBtn||0|0|0|0|0",
            "leftTouchConBtn||158|69|37|51|1",
            "multiTouchConBtn||199|74|41|45|1",
            "rightTouchConBtn||251|70|32|53|1",
            "topTouchConBtn||157|46|125|23|1",
            "l3ConBtn||138|147|42|42|1",
            "lsuConBtn||150|140|17|15|1",
            "lsrConBtn||172|162|17|15|1",
            "lsdConBtn||150|180|17|15|1",
            "lslConBtn||126|162|17|15|1",
            "r3ConBtn||262|149|42|42|1",
            "rsuConBtn||275|142|17|15|1",
            "rsrConBtn||298|162|17|15|1",
            "rsdConBtn||275|180|17|15|1",
            "rslConBtn||248|162|17|15|1",
            "upConBtn||88|74|20|35|1",
            "rightConBtn||103|103|35|20|1",
            "downConBtn||88|114|20|35|1",
            "leftConBtn||56|103|35|20|1"
        };

        private static string Line(HitboxRegion r) =>
            string.Join("|", r.Id, r.Tag, r.Left.ToString(CultureInfo.InvariantCulture), r.Top.ToString(CultureInfo.InvariantCulture),
                r.Width.ToString(CultureInfo.InvariantCulture), r.Height.ToString(CultureInfo.InvariantCulture), r.IsVisible ? "1" : "0");

        [Fact]
        public void Xbox360Layout_KeepsEveryCoordinateOfTheOldWindow()
        {
            Assert.Equal(Xbox360Expected, ControllerLayouts.Xbox360.Regions.Select(Line).ToArray());
        }

        [Fact]
        public void DualShock4Layout_KeepsEveryCoordinateOfTheOldWindow()
        {
            Assert.Equal(DualShock4Expected, ControllerLayouts.DualShock4.Regions.Select(Line).ToArray());
        }

        [Fact]
        public void EditorLayout_KeepsEveryCoordinateOfTheOldProfileEditor()
        {
            Assert.Equal(EditorExpected, ControllerLayouts.DualShock4Editor.Regions.Select(r => Line(r).Replace("|" + r.Tag + "|", "||")).ToArray());
        }

        [Fact]
        public void BothLayouts_ListTheSameButtonIds()
        {
            Assert.Equal(ControllerLayouts.Xbox360.Regions.Select(r => r.Id).OrderBy(x => x),
                ControllerLayouts.DualShock4.Regions.Select(r => r.Id).OrderBy(x => x));
        }

        [Fact]
        public void ComputeFit_IsTheIdentityAtTheReferenceSize_AndCentersWhenTheAreaIsWider()
        {
            ControllerLayout layout = ControllerLayouts.Xbox360;
            Assert.Equal((1.0, 0.0, 0.0), ControllerVisualizerControl.ComputeFit(layout, layout.ReferenceWidth, layout.ReferenceHeight));
            Assert.Equal((1.0, 0.0, 0.0), ControllerVisualizerControl.ComputeFit(layout, 0, 0));

            (double scale, double offsetX, double offsetY) = ControllerVisualizerControl.ComputeFit(layout, layout.ReferenceWidth * 2, layout.ReferenceHeight);
            Assert.Equal(1.0, scale);
            Assert.Equal(layout.ReferenceWidth / 2, offsetX);
            Assert.Equal(0.0, offsetY);
        }

        [Fact]
        public void Control_PlacesTheButtonsAtTheLayoutCoordinates_AndKeepsTheSameInstancesWhenTheLayoutChanges()
        {
            RunOnSta(() =>
            {
                var control = new ControllerVisualizerControl { Width = 630, Height = 247, Layout = ControllerLayouts.Xbox360 };
                control.Measure(new Size(630, 247));
                control.Arrange(new Rect(0, 0, 630, 247));

                AssertPlaced(control, Xbox360Expected);
                Dictionary<string, Button> before = control.Buttons.ToDictionary(p => p.Key, p => p.Value);
                Assert.Equal(Visibility.Collapsed, control["touchpadClickBtn"].Visibility);

                control.Layout = ControllerLayouts.DualShock4;
                control.Measure(new Size(630, 247));
                control.Arrange(new Rect(0, 0, 630, 247));

                AssertPlaced(control, DualShock4Expected);
                foreach (KeyValuePair<string, Button> pair in before)
                {
                    Assert.Same(pair.Value, control[pair.Key]);
                }

                Assert.Equal(Visibility.Visible, control["touchpadClickBtn"].Visibility);
            });
        }

        [Fact]
        public void Control_ScalesTheCoordinatesWithTheDisplayArea()
        {
            RunOnSta(() =>
            {
                var control = new ControllerVisualizerControl { Width = 1260, Height = 494, ScaleToFit = true, Layout = ControllerLayouts.Xbox360 };
                control.Measure(new Size(1260, 494));
                control.Arrange(new Rect(0, 0, 1260, 494));

                HitboxRegion a = ControllerLayouts.Xbox360.Regions.First(r => r.Id == "aBtn");
                Button button = control["aBtn"];
                Assert.Equal(a.Left * 2, Canvas.GetLeft(button), 6);
                Assert.Equal(a.Top * 2, Canvas.GetTop(button), 6);
                Assert.Equal(a.Width * 2, button.Width, 6);
                Assert.Equal(a.Height * 2, button.Height, 6);
            });
        }

        /// <summary>
        /// BindingWindow が使う XAML の書き方（配置表を x:Static で指定、ボタンのスタイルを自分の Resources から StaticResource で指定、
        /// 図の上の他の要素）で、ボタンが生成されてスタイルが当たり、他の要素より前に置かれること。
        /// </summary>
        [Fact]
        public void Xaml_BuildsTheButtonsWithTheStyleFromItsOwnResources_InFrontOfOtherChildren()
        {
            RunOnSta(() =>
            {
                const string xaml = @"
<c:ControllerVisualizerControl xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                               xmlns:c=""clr-namespace:DS4Windows.UI.Controls;assembly=DS4Windows""
                               Height=""247"" Layout=""{x:Static c:ControllerLayouts.Xbox360}"">
    <c:ControllerVisualizerControl.Resources>
        <Style x:Key=""NoBGHoverBtn"" TargetType=""{x:Type Button}""><Setter Property=""Background"" Value=""Transparent""/></Style>
    </c:ControllerVisualizerControl.Resources>
    <c:ControllerVisualizerControl.ButtonStyle><StaticResource ResourceKey=""NoBGHoverBtn""/></c:ControllerVisualizerControl.ButtonStyle>
    <Image x:Name=""highlightImg"" Width=""20"" Height=""20""/>
</c:ControllerVisualizerControl>";
                var control = (ControllerVisualizerControl)System.Windows.Markup.XamlReader.Parse(xaml);

                Assert.Equal(ControllerLayouts.Xbox360.Regions.Count, control.Buttons.Count);
                Assert.All(control.Buttons.Values, b => Assert.Same(control.ButtonStyle, b.Style));
                Assert.NotNull(control.ButtonStyle);

                UIElement image = control.Children.OfType<Image>().Single();
                Assert.All(control.Buttons.Values, b => Assert.True(Panel.GetZIndex(b) > Panel.GetZIndex(image)));
            });
        }

        private static void AssertPlaced(ControllerVisualizerControl control, string[] expected)
        {
            foreach (string line in expected)
            {
                string[] f = line.Split('|');
                Button button = control[f[0]];
                Assert.Equal(f[1], button.Tag);
                Assert.Equal(double.Parse(f[2], CultureInfo.InvariantCulture), Canvas.GetLeft(button));
                Assert.Equal(double.Parse(f[3], CultureInfo.InvariantCulture), Canvas.GetTop(button));
                Assert.Equal(double.Parse(f[4], CultureInfo.InvariantCulture), button.Width);
                Assert.Equal(double.Parse(f[5], CultureInfo.InvariantCulture), button.Height);
                Assert.Equal(f[6] == "1" ? Visibility.Visible : Visibility.Collapsed, button.Visibility);
            }
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
                if (failure is Xunit.Sdk.XunitException) throw failure;
                throw new InvalidOperationException("STA スレッドで失敗: " + failure.Message, failure);
            }
        }
    }
}
