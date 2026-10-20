using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W1（Phase10-Step1-Plan.md §3.1・§5）: <see cref="MacroPlaybackOptions"/> の単体テスト。
    /// <c>DS4KeyType</c> との相互変換が、<c>RepeatMacro</c>／<c>ScanCode</c> のビットだけを操作し、ほかのビットを保つこと。
    /// </summary>
    public class Phase10Step1W1MacroPlaybackOptionsTests
    {
        [Theory]
        [InlineData(DS4KeyType.None, false, false)]
        [InlineData(DS4KeyType.Macro, false, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro, true, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.ScanCode, false, true)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, true, true)]
        public void FromKeyType_ReadsRepeatMacroAndScanCode(DS4KeyType keyType, bool repeat, bool scan)
        {
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(keyType);

            Assert.Equal(repeat, options.RepeatWhileHeld);
            Assert.Equal(scan, options.UseScanCode);
        }

        [Theory]
        [InlineData(DS4KeyType.None)]
        [InlineData(DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Toggle | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Unbound)]
        public void ApplyTo_AfterFromKeyType_ReturnsTheSameKeyType(DS4KeyType keyType)
        {
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(keyType);

            Assert.Equal(keyType, options.ApplyTo(keyType));
        }

        [Theory]
        [InlineData(DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.Toggle)]
        [InlineData(DS4KeyType.Unbound | DS4KeyType.Toggle | DS4KeyType.Macro)]
        public void ApplyTo_SetsOnlyTheTwoBits_AndKeepsTheOthers(DS4KeyType others)
        {
            var options = new MacroPlaybackOptions { RepeatWhileHeld = true, UseScanCode = true };

            DS4KeyType result = options.ApplyTo(others);

            Assert.Equal(others | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, result);
        }

        [Theory]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Toggle | DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        public void ApplyTo_ClearsOnlyTheTwoBits_AndKeepsTheOthers(DS4KeyType keyType)
        {
            var options = new MacroPlaybackOptions();   // どちらも偽

            DS4KeyType result = options.ApplyTo(keyType);

            Assert.Equal(keyType & ~(DS4KeyType.RepeatMacro | DS4KeyType.ScanCode), result);
            Assert.True(result.HasFlag(DS4KeyType.Macro));
        }

        [Fact]
        public void PlayModeIndex_MapsToRepeatWhileHeld()
        {
            var options = new MacroPlaybackOptions();

            Assert.Equal(MacroPlaybackOptions.PlayOnceIndex, options.PlayModeIndex);

            options.PlayModeIndex = MacroPlaybackOptions.RepeatWhileHeldIndex;
            Assert.True(options.RepeatWhileHeld);
            Assert.Equal(MacroPlaybackOptions.RepeatWhileHeldIndex, options.PlayModeIndex);

            options.PlayModeIndex = MacroPlaybackOptions.PlayOnceIndex;
            Assert.False(options.RepeatWhileHeld);

            options.RepeatWhileHeld = true;
            Assert.Equal(MacroPlaybackOptions.RepeatWhileHeldIndex, options.PlayModeIndex);
        }

        [Fact]
        public void PlayModeIndex_AnythingButRepeat_IsPlayOnce()
        {
            var options = new MacroPlaybackOptions { RepeatWhileHeld = true };

            options.PlayModeIndex = -1;   // ComboBox の選択解除

            Assert.False(options.RepeatWhileHeld);
        }

        [Fact]
        public void Repeatable_False_DoesNotClearTheLoadedRepeatValue()
        {
            // 従来の RecordBox は、ドロップダウンを無効にしても、読み込んだ RepeatMacro を保存時に書き戻していた。その挙動を保つ
            MacroPlaybackOptions options = MacroPlaybackOptions.FromKeyType(DS4KeyType.Macro | DS4KeyType.RepeatMacro, repeatable: false);

            Assert.False(options.Repeatable);
            Assert.True(options.RepeatWhileHeld);
            Assert.True(options.ApplyTo(DS4KeyType.Macro).HasFlag(DS4KeyType.RepeatMacro));
        }

        [Fact]
        public void PropertyChanged_IsRaisedOnlyOnRealChanges()
        {
            var options = new MacroPlaybackOptions();
            var raised = new List<string>();
            options.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            options.RepeatWhileHeld = true;
            options.RepeatWhileHeld = true;   // 変化なし
            options.UseScanCode = true;
            options.UseScanCode = true;       // 変化なし
            options.Repeatable = false;
            options.Repeatable = false;       // 変化なし

            Assert.Equal(new[] { "RepeatWhileHeld", "PlayModeIndex", "UseScanCode", "Repeatable" }, raised);
        }

        [Fact]
        public void PlayModeIndex_ChangeRaisesBothNotifications()
        {
            var options = new MacroPlaybackOptions();
            var raised = new List<string>();
            options.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            options.PlayModeIndex = MacroPlaybackOptions.RepeatWhileHeldIndex;

            Assert.Contains("RepeatWhileHeld", raised);
            Assert.Contains("PlayModeIndex", raised);
        }

        [Fact]
        public void LoadFrom_ReplacesBothValues()
        {
            var options = new MacroPlaybackOptions { RepeatWhileHeld = true, UseScanCode = true };

            options.LoadFrom(DS4KeyType.Macro);

            Assert.False(options.RepeatWhileHeld);
            Assert.False(options.UseScanCode);
        }
    }
}
