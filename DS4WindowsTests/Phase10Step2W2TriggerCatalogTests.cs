using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2・§5）: トリガーの一覧（<see cref="TriggerCatalog"/>）。並び・保存する名前（<c>Tag</c>）・
    /// 表示名（<c>Content</c>）は、従来の <c>AssignmentEditor.xaml</c> の、名前つきのチェックボックス 49 個と同じ。既存の <c>Actions.xml</c> を
    /// 読めるように、保存する名前は、すべて <c>SpecialAction</c> が解釈できる名前であること。
    /// </summary>
    public class Phase10Step2W2TriggerCatalogTests
    {
        // 従来の XAML の (Tag, Content)。この並びで、チェックボックスが並んでいた
        private static readonly (string Name, string Label)[] LegacyCheckBoxes =
        {
            ("Cross", "Cross"), ("Circle", "Circle"), ("Square", "Square"), ("Triangle", "Triangle"),
            ("Options", "Options"), ("Share", "Share"), ("Up", "Up"), ("Down", "Down"), ("Left", "Left"), ("Right", "Right"),
            ("PS", "PS"), ("Mute", "Mute"), ("Capture", "Capture"), ("SideL", "Side L"), ("SideR", "Side R"),
            ("L1", "L1"), ("R1", "R1"), ("L2", "L2"), ("L2 Full Pull", "L2 Full Pull"), ("R2", "R2"), ("R2 Full Pull", "R2 Full Pull"),
            ("L3", "L3"), ("R3", "R3"),
            ("Function Left", "Function L"), ("Function Right", "Function R"),
            ("Bottom Left Paddle", "Paddle L"), ("Bottom Right Paddle", "Paddle R"),
            ("Left Touch", "Left Touch"), ("Upper Touch", "Upper Touch"), ("Multitouch", "Multitouch"), ("Right Touch", "Right Touch"),
            ("Left Stick Up", "Left Stick Up"), ("Left Stick Down", "Left Stick Down"),
            ("Left Stick Left", "Left Stick Left"), ("Left Stick Right", "Left Stick Right"),
            ("Right Stick Up", "Right Stick Up"), ("Right Stick Down", "Right Stick Down"),
            ("Right Stick Left", "Right Stick Left"), ("Right Stick Right", "Right Stick Right"),
            ("Swipe Up", "Swipe Up"), ("Swipe Down", "Swipe Down"), ("Swipe Left", "Swipe Left"), ("Swipe Right", "Swipe Right"),
            ("Tilt Up", "Tilt Up"), ("Tilt Down", "Tilt Down"), ("Tilt Left", "Tilt Left"), ("Tilt Right", "Tilt Right"),
            ("Touch Started", "Touch Started"), ("Touch Ended", "Touch Ended"),
        };

        [Fact]
        public void TheDefaultCatalog_StartsWithTheLegacy49Items_InTheSameOrder_WithTheSameNamesAndLabels()
        {
            var catalog = new TriggerCatalog();

            // Phase10-Step4c W5（ユーザー承認、2026-10-21）: 末尾に LS Outer／RS Outer を足した（51 項目）。従来の 49 項目の並び・名前・表示名は変わらない
            Assert.Equal(49, LegacyCheckBoxes.Length);
            Assert.Equal(51, catalog.Items.Count);
            Assert.Equal(LegacyCheckBoxes, catalog.Items.Take(49).Select(i => (i.Name, i.Label)).ToArray());
            Assert.Equal(new[] { ("LS Outer", "LS Outer"), ("RS Outer", "RS Outer") }, catalog.Items.Skip(49).Select(i => (i.Name, i.Label)).ToArray());
        }

        [Fact]
        public void TheNames_AreUnique()
        {
            var catalog = new TriggerCatalog();

            Assert.Equal(catalog.Items.Count, catalog.Items.Select(i => i.Name).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void EveryName_IsUnderstoodByTheSpecialAction_SoAnExistingActionsXmlLoads()
        {
            var catalog = new TriggerCatalog();

            foreach (TriggerItem item in catalog.Items)
            {
                var action = new SpecialAction("n", item.Name, "Macro", "65");

                Assert.Single(action.trigger);
                Assert.True((int)action.trigger[0] != 0, $"「{item.Name}」を SpecialAction が解釈できない（保存する名前の取り違え）");
            }
        }

        [Fact]
        public void Contains_MatchesTheNamesExactly()
        {
            var catalog = new TriggerCatalog();

            Assert.True(catalog.Contains("SideL"));
            Assert.True(catalog.Contains("Function Left"));
            Assert.False(catalog.Contains("Side L"));     // 表示名では引けない（保存する名前だけ）
            Assert.False(catalog.Contains("cross"));      // 大文字・小文字を区別する
            Assert.False(catalog.Contains("Bogus"));
            Assert.False(catalog.Contains(null));
        }

        [Fact]
        public void ACustomCatalog_HoldsOnlyTheGivenItems()
        {
            var catalog = new TriggerCatalog(new[] { new TriggerItem("Cross", "Cross") });

            Assert.Single(catalog.Items);
            Assert.True(catalog.Contains("Cross"));
            Assert.False(catalog.Contains("Circle"));
        }

        [Fact]
        public void Constructor_RejectsNullAndNullItems()
        {
            Assert.Throws<ArgumentNullException>(() => new TriggerCatalog((IReadOnlyList<TriggerItem>)null));
            Assert.Throws<ArgumentException>(() => new TriggerCatalog(new TriggerItem[] { null }));
        }
    }
}
