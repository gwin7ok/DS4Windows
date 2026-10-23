using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-2 W1（R2-MacroToggleRepeat-Plan.md §3.1・§3.3）: <see cref="MacroPlaybackOptions"/> の「繰り返しをトグルする」モード（<c>RepeatToggle</c>）。
    /// 実行時の表現は <see cref="DS4KeyType.RepeatLatchMacro"/>。Play Once／Repeat While Held／Repeat (Toggle) は、同時には 1 つだけ。
    /// </summary>
    public class R2W1MacroPlaybackOptionsToggleTests
    {
        [Fact]
        public void RepeatLatchMacro_IsBit64_AndDoesNotOverlapOtherBits()
        {
            Assert.Equal((byte)64, (byte)DS4KeyType.RepeatLatchMacro);
            DS4KeyType others = DS4KeyType.ScanCode | DS4KeyType.Toggle | DS4KeyType.Unbound | DS4KeyType.Macro | DS4KeyType.RepeatMacro;
            Assert.Equal(DS4KeyType.None, others & DS4KeyType.RepeatLatchMacro);
        }

        [Theory]
        [InlineData(DS4KeyType.Macro, false, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro, true, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro, false, true)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro | DS4KeyType.ScanCode, false, true)]
        public void FromKeyType_ReadsTheMode(DS4KeyType keyType, bool held, bool toggle)
        {
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(keyType);

            Assert.Equal(held, options.RepeatWhileHeld);
            Assert.Equal(toggle, options.RepeatToggle);
        }

        [Fact]
        public void FromKeyType_BothFlags_PrefersToggle()
        {
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.RepeatLatchMacro);

            Assert.True(options.RepeatToggle);
            Assert.False(options.RepeatWhileHeld);
        }

        [Theory]
        [InlineData(DS4KeyType.None)]
        [InlineData(DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Toggle | DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro)]
        public void ApplyTo_AfterFromKeyType_ReturnsTheSameKeyType(DS4KeyType keyType)
        {
            Assert.Equal(keyType, MacroPlaybackOptions.FromKeyType(keyType).ApplyTo(keyType));
        }

        [Fact]
        public void ApplyTo_SwitchingMode_ClearsTheOtherModeBit_AndKeepsTheOthers()
        {
            DS4KeyType held = DS4KeyType.Toggle | DS4KeyType.Macro | DS4KeyType.RepeatMacro;
            var options = new MacroPlaybackOptions { RepeatToggle = true };

            DS4KeyType result = options.ApplyTo(held);

            Assert.Equal(DS4KeyType.Toggle | DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro, result);
        }

        [Fact]
        public void SettingOneMode_ClearsTheOther()
        {
            var options = new MacroPlaybackOptions { RepeatWhileHeld = true };

            options.RepeatToggle = true;
            Assert.True(options.RepeatToggle);
            Assert.False(options.RepeatWhileHeld);

            options.RepeatWhileHeld = true;
            Assert.True(options.RepeatWhileHeld);
            Assert.False(options.RepeatToggle);
        }

        [Fact]
        public void PlayModeIndex_MapsAllThreeModes()
        {
            var options = new MacroPlaybackOptions();
            Assert.Equal(MacroPlaybackOptions.PlayOnceIndex, options.PlayModeIndex);

            options.PlayModeIndex = MacroPlaybackOptions.RepeatToggleIndex;
            Assert.True(options.RepeatToggle);
            Assert.False(options.RepeatWhileHeld);
            Assert.Equal(MacroPlaybackOptions.RepeatToggleIndex, options.PlayModeIndex);

            options.PlayModeIndex = MacroPlaybackOptions.RepeatWhileHeldIndex;
            Assert.True(options.RepeatWhileHeld);
            Assert.False(options.RepeatToggle);

            options.PlayModeIndex = MacroPlaybackOptions.PlayOnceIndex;
            Assert.False(options.RepeatWhileHeld);
            Assert.False(options.RepeatToggle);
        }

        [Fact]
        public void PlayModeIndex_OutOfRange_IsPlayOnce()
        {
            var options = new MacroPlaybackOptions { RepeatToggle = true };

            options.PlayModeIndex = -1;

            Assert.False(options.RepeatToggle);
            Assert.False(options.RepeatWhileHeld);
        }

        [Fact]
        public void PropertyChanged_ForToggle_IsRaisedOnlyOnRealChanges()
        {
            var options = new MacroPlaybackOptions();
            var raised = new List<string>();
            options.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            options.RepeatToggle = true;
            options.RepeatToggle = true;   // 変化なし
            options.RepeatWhileHeld = true;   // Toggle が偽になる

            Assert.Equal(new[] { "RepeatToggle", "PlayModeIndex", "RepeatWhileHeld", "RepeatToggle", "PlayModeIndex" }, raised);
        }

        [Fact]
        public void Repeatable_False_DoesNotClearTheLoadedToggleValue()
        {
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro, repeatable: false);

            Assert.True(options.RepeatToggle);
            Assert.True(options.ApplyTo(DS4KeyType.Macro).HasFlag(DS4KeyType.RepeatLatchMacro));
        }
    }
}
