using System;
using System.IO;
using Xunit;
using DS4Windows.Core.Model;

namespace DS4WindowsTests
{
    /// <summary>
    /// 名前から値を引く 2 つの関数を <c>BackingStore</c> から <c>DS4ControlNames</c>／<c>X360ControlTables</c> へ移した。
    /// 本体は移動のみ（値の対応は変えない）。呼び出し元は付け替え済みで、<c>BackingStore</c> に残していない。
    /// </summary>
    public class NameFunctionsTests
    {
        [Theory]
        [InlineData("Cross", DS4Controls.Cross)]
        [InlineData("cross", DS4Controls.Cross)]
        [InlineData("L2", DS4Controls.L2)]
        [InlineData("bnShare", DS4Controls.Share)]
        [InlineData("bnUp", DS4Controls.DpadUp)]
        [InlineData("bnLSLeft", DS4Controls.LXNeg)]
        [InlineData("bnRSDown", DS4Controls.RYPos)]
        [InlineData("bnTouchEnded", DS4Controls.TouchEnded)]
        public void DS4Controls_ByName_KeepsTheMapping(string name, DS4Controls expected)
        {
            Assert.Equal(expected, DS4ControlNames.GetDS4ControlsByName(name));
        }

        [Fact]
        public void DS4Controls_ByName_UnknownBnName_IsZero_AndUnknownEnumNameThrowsLikeBefore()
        {
            Assert.Equal((DS4Controls)0, DS4ControlNames.GetDS4ControlsByName("bnNothing"));
            Assert.Throws<ArgumentException>(() => DS4ControlNames.GetDS4ControlsByName("NotAControl"));
            // "sbn" で始まる旧シフト名は "bn" で始まらないため、旧コードと同じく列挙体の解析に回って例外になる（switch 内の sbn 系の case は、到達しない）
            Assert.Throws<ArgumentException>(() => DS4ControlNames.GetDS4ControlsByName("sbnShare"));
        }

        [Theory]
        [InlineData("LB", X360Controls.LB)]
        [InlineData("lb", X360Controls.LB)]
        [InlineData("Left Bumper", X360Controls.LB)]
        [InlineData("Back", X360Controls.Back)]
        [InlineData("Up Button", X360Controls.DpadUp)]
        [InlineData("Left Y-Axis+", X360Controls.LYPos)]
        [InlineData("Touchpad Click", X360Controls.TouchpadClick)]
        [InlineData("5th Mouse Button", X360Controls.FifthMouse)]
        [InlineData("Unbound", X360Controls.Unbound)]
        [InlineData("Something Else", X360Controls.Unbound)]
        public void X360Controls_ByName_KeepsTheMapping(string name, X360Controls expected)
        {
            Assert.Equal(expected, X360ControlTables.GetX360ControlsByName(name));
        }

    }
}
