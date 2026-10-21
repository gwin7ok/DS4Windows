using System;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I1（Phase10-Step6-Plan.md §4）: 共通の割り当て定義（<see cref="AssignmentDefinition"/>）の型の検査。
    /// I1 は型の追加だけで、既存の経路は使っていない（I3 で接続する）。
    /// </summary>
    public class Phase10Step6I1AssignmentDefinitionTests
    {
        private static readonly DS4Controls[] One = { DS4Controls.Cross };

        [Fact]
        public void Definition_KeepsItsValues_AndDefaultsTheOptionalOnes()
        {
            var d = new AssignmentDefinition("id1", "n1", AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.Key,
                new[] { DS4Controls.L1, DS4Controls.Cross }, 0.5);

            Assert.Equal("id1", d.Id);
            Assert.Equal("n1", d.Name);
            Assert.Equal(AssignmentOrigin.SpecialAction, d.Origin);
            Assert.Equal(SpecialAction.ActionTypeId.Key, d.Kind);
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.Cross }, d.Triggers);
            Assert.Equal(0.5, d.HoldSeconds);
            Assert.Null(d.Item);
            Assert.Null(d.Key);
            Assert.Null(d.Button);
            Assert.Null(d.Extras);
        }

        [Fact]
        public void Controls_RequiresExactlyOneTrigger()
        {
            Assert.NotNull(new AssignmentDefinition("c", "c", AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Key, One));
            Assert.Throws<ArgumentException>(() => new AssignmentDefinition("c", "c", AssignmentOrigin.Controls,
                SpecialAction.ActionTypeId.Key, new[] { DS4Controls.L1, DS4Controls.Cross }));
        }

        [Fact]
        public void SpecialAction_AllowsSeveralTriggers_ButNotNoneOrNull()
        {
            Assert.Throws<ArgumentException>(() => new AssignmentDefinition("s", "s", AssignmentOrigin.SpecialAction,
                SpecialAction.ActionTypeId.Key, Array.Empty<DS4Controls>()));
            Assert.Throws<ArgumentException>(() => new AssignmentDefinition("s", "s", AssignmentOrigin.SpecialAction,
                SpecialAction.ActionTypeId.Key, null));
        }

        [Fact]
        public void NegativeHoldSeconds_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AssignmentDefinition("c", "c", AssignmentOrigin.Controls,
                SpecialAction.ActionTypeId.Key, One, -0.1));
        }

        [Fact]
        public void NullIdAndName_BecomeEmpty()
        {
            var d = new AssignmentDefinition(null, null, AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Key, One);
            Assert.Equal(string.Empty, d.Id);
            Assert.Equal(string.Empty, d.Name);
        }

        [Fact]
        public void Definition_IsImmutable_ForTriggersAndItem()
        {
            var triggers = new[] { DS4Controls.Cross };
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = "a.exe" };
            var d = new AssignmentDefinition("c", "c", AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Program, triggers, 0, item);

            triggers[0] = DS4Controls.Circle;
            item.ProgramPath = "b.exe";

            Assert.Equal(DS4Controls.Cross, d.Triggers[0]);
            Assert.Equal("a.exe", d.Item.ProgramPath);
        }

        [Fact]
        public void KeyAndButtonValues_AreCarried()
        {
            var d = new AssignmentDefinition("k", "k", AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Key, One,
                key: new KeyBindingValues(65, true, false));
            Assert.Equal((ushort)65, d.Key.Value.LogicalKey);
            Assert.True(d.Key.Value.ScanCode);

            var b = new AssignmentDefinition("b", "b", AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.Button, One,
                button: new ButtonBindingValues(true, X360Controls.A, true));
            Assert.True(b.Button.Value.Toggle);
        }
    }
}
