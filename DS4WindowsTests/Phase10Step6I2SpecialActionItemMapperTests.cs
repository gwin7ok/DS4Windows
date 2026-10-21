using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I2（Phase10-Step6-Plan.md §4）: <see cref="SpecialActionItemMapper"/>（SA ⇄ 型のある項目）の検査。
    /// 保存形式（<c>SpecialAction</c> の文字列）は変えない。旧書式の文字列が、同じ項目になること、項目から作った SA が、同じ文字列に戻ることを確かめる。
    /// </summary>
    public class Phase10Step6I2SpecialActionItemMapperTests
    {
        private static readonly DS4Controls[] Cross = { DS4Controls.Cross };

        [Fact]
        public void Program_ReadsHiddenKeywordOnce_AndRoundTrips()
        {
            var sa = new SpecialAction("p", "Cross", "Program", "C:\\a.exe", 0.5, "$hidden -x $HIDDEN");

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Equal(SpecialAction.ActionTypeId.Program, item.Type);
            Assert.Equal("C:\\a.exe", item.ProgramPath);
            Assert.True(item.HideWindow);
            Assert.Equal(" -x $HIDDEN", item.ProgramArguments);   // 取り除くのは最初の 1 回だけ（起動処理と同じ）
            Assert.Equal(0.5, item.HoldSeconds);

            SpecialAction back = SpecialActionItemMapper.ToSpecialAction("p", Cross, item);
            Assert.Equal("C:\\a.exe", back.details);
            Assert.Equal("$hidden -x $HIDDEN", back.extra);
            Assert.Equal(0.5, back.delayTime);
            Assert.Equal(new[] { DS4Controls.Cross }, back.trigger);
        }

        [Fact]
        public void Program_WithoutHiddenKeyword_KeepsTheArgumentsAsTheyAre()
        {
            var sa = new SpecialAction("p", "Cross", "Program", "a.exe", 0, "-a -b");
            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.False(item.HideWindow);
            Assert.Equal("-a -b", item.ProgramArguments);
            Assert.Equal("-a -b", SpecialActionItemMapper.ToSpecialAction("p", Cross, item).extra);
        }

        [Fact]
        public void Profile_ReadsNameIdAndAutomaticUntrigger_AndRoundTrips()
        {
            var sa = new SpecialAction("pf", "Cross", "Profile", "Other", 0, "AutomaticUntrigger") { profileId = "abc" };

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Equal("Other", item.ProfileName);
            Assert.Equal("abc", item.ProfileId);
            Assert.True(item.AutomaticUntrigger);

            SpecialAction back = SpecialActionItemMapper.ToSpecialAction("pf", Cross, item);
            Assert.Equal("Other", back.details);
            Assert.Equal("abc", back.profileId);
            Assert.True(back.automaticUntrigger);
            Assert.True(back.IsTemporaryProfileAction);
        }

        [Fact]
        public void Profile_WithoutId_BecomesNullIdAgain()
        {
            var sa = new SpecialAction("pf", "Cross", "Profile", "Other");
            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Equal(string.Empty, item.ProfileId);
            Assert.Null(SpecialActionItemMapper.ToSpecialAction("pf", Cross, item).profileId);
        }

        [Fact]
        public void BatteryCheck_ReadsLegacyPipeFormat_AndWritesTheSameFormatAsTheEditor()
        {
            var sa = new SpecialAction("b", "Cross", "BatteryCheck", "1.5|False|True|10|20|30|40|50|60", 1.5);

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.False(item.BatteryNotification);
            Assert.True(item.BatteryLightbar);
            Assert.Equal(new DS4Color(10, 20, 30), item.EmptyColor);
            Assert.Equal(new DS4Color(40, 50, 60), item.FullColor);
            Assert.Equal(1.5, item.HoldSeconds);

            SpecialAction back = SpecialActionItemMapper.ToSpecialAction("b", Cross, item);
            Assert.Equal("1.5,False,True,10,20,30,40,50,60", back.details);   // SA のコンストラクターは「|」を「,」にする。同じ形
            Assert.Equal(sa.details, back.details);
        }

        [Fact]
        public void BatteryCheck_WithBrokenDetails_FallsBackToDefaults()
        {
            var sa = new SpecialAction("b", "Cross", "BatteryCheck", "1|x", 0);
            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.True(item.BatteryNotification);
            Assert.True(item.BatteryLightbar);
            Assert.Equal(default(DS4Color), item.EmptyColor);
        }

        [Fact]
        public void MultiAction_SplitsThreeMacros_AndRoundTrips()
        {
            var sa = new SpecialAction("m", "Cross", "MultiAction", "1/2,3,4/5/6", 0.3);

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Equal(new List<int> { 1, 2 }, item.TapMacro);
            Assert.Equal(new List<int> { 3 }, item.HoldMacro);
            Assert.Equal(new List<int> { 4, 5, 6 }, item.DoubleTapMacro);
            Assert.Equal(sa.details, SpecialActionItemMapper.ToSpecialAction("m", Cross, item).details);
        }

        [Fact]
        public void MultiAction_MissingOrBrokenMacros_AreEmpty()
        {
            var sa = new SpecialAction("m", "Cross", "MultiAction", "1/x", 0);
            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Empty(item.TapMacro);
            Assert.Empty(item.HoldMacro);
            Assert.Empty(item.DoubleTapMacro);
        }

        [Fact]
        public void Macro_ReadsTheThreeSaOnlyOptions_AndRoundTrips()
        {
            var sa = new SpecialAction("mc", "Cross", "Macro", "1/2", 0, "RunOnRelease Sync KeepKeyState");

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.True(item.RunOnRelease);
            Assert.True(item.Synchronized);
            Assert.True(item.KeepKeyState);

            SpecialAction back = SpecialActionItemMapper.ToSpecialAction("mc", Cross, item);
            Assert.True(back.pressRelease);
            Assert.True(back.synchronized);
            Assert.True(back.keepKeyState);
        }

        [Theory]
        [InlineData("DisconnectBT", SpecialAction.ActionTypeId.DisconnectBT)]
        [InlineData("GyroCalibration", SpecialAction.ActionTypeId.GyroCalibrate)]
        public void DelayOnlyTypes_CarryTheHoldSeconds(string type, SpecialAction.ActionTypeId id)
        {
            var sa = new SpecialAction("d", "Cross", type, "", 2.5);

            ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);

            Assert.Equal(id, item.Type);
            Assert.Equal(2.5, item.HoldSeconds);
            Assert.Equal(2.5, SpecialActionItemMapper.ToSpecialAction("d", Cross, item).delayTime);
        }

        [Theory]
        [InlineData("Key", "65")]
        [InlineData("Button", "3")]
        public void KeyAndButton_AreNotRepresentedAsItems(string type, string details)
        {
            Assert.Null(SpecialActionItemMapper.ToItem(new SpecialAction("k", "Cross", type, details)));
        }

        [Fact]
        public void NullAndUnknownType_GiveNull()
        {
            Assert.Null(SpecialActionItemMapper.ToItem(null));
            Assert.Null(SpecialActionItemMapper.ToItem(new SpecialAction("u", "Cross", "Nothing", "")));
        }

        [Fact]
        public void ToSpecialAction_DoesNotShareTheTriggerList_AndRejectsNullItem()
        {
            var triggers = new List<DS4Controls> { DS4Controls.Cross };
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT };

            SpecialAction sa = SpecialActionItemMapper.ToSpecialAction("d", triggers, item);
            triggers.Add(DS4Controls.Circle);

            Assert.Equal(new[] { DS4Controls.Cross }, sa.trigger);
            Assert.Throws<System.ArgumentNullException>(() => SpecialActionItemMapper.ToSpecialAction("d", triggers, null));
        }
    }
}
