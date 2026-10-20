using System;
using System.Windows;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W0（Phase10-Step3-Plan.md §3.1・§5）: Extras（振動・ライトバー・マウス感度）の読み書きの特性化。
    /// Extras は、出力ピッカー（<c>BindingWindow</c>）に残すため、<see cref="OutBinding"/> の <c>ParseExtras</c>／<c>CompileExtras</c>／<c>IsUsingExtras</c>
    /// の現在の挙動（書式、境界、不正な値）を固定する。製品コードは変えない。
    /// </summary>
    public class Phase10Step3W0OutBindingExtrasTests
    {
        private const string Full = "10,20,1,255,128,0,100,1,40";

        static Phase10Step3W0OutBindingExtrasTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }
        }

        [Fact]
        public void ParseThenCompile_RoundTripsAFullyUsedExtras()
        {
            var bind = new OutBinding();
            bind.ParseExtras(Full);

            Assert.Equal(10, bind.HeavyRumble);
            Assert.Equal(20, bind.LightRumble);
            Assert.True(bind.UseExtrasColor);
            Assert.Equal(255, bind.ExtrasColorR);
            Assert.Equal(128, bind.ExtrasColorG);
            Assert.Equal(0, bind.ExtrasColorB);
            Assert.Equal(100, bind.FlashRate);
            Assert.True(bind.UseMouseSens);
            Assert.Equal(40, bind.MouseSens);
            Assert.Equal(Full, bind.CompileExtras());
        }

        [Fact]
        public void Compile_OfAFreshBinding_WritesAllZeros()
        {
            Assert.Equal("0,0,0,0,0,0,0,0,0", new OutBinding().CompileExtras());
        }

        [Fact]
        public void Parse_ColorFlagOff_ResetsTheColorAndFlashRate()
        {
            var bind = new OutBinding();
            bind.ParseExtras("1,2,0,9,9,9,50,0,0");

            Assert.False(bind.UseExtrasColor);
            Assert.Equal(255, bind.ExtrasColorR);
            Assert.Equal(255, bind.ExtrasColorG);
            Assert.Equal(255, bind.ExtrasColorB);
            Assert.Equal(0, bind.FlashRate);
        }

        [Fact]
        public void Parse_MouseFlagOff_ResetsTheSensitivityTo25()
        {
            var bind = new OutBinding();
            bind.ParseExtras("0,0,0,0,0,0,0,0,77");

            Assert.False(bind.UseMouseSens);
            Assert.Equal(25, bind.MouseSens);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1,2,3")]
        [InlineData("1,2,1,255,0,0,1,1,40,9")]
        public void Parse_WithAWrongNumberOfValues_IsIgnored(string extras)
        {
            var bind = new OutBinding();
            bind.ParseExtras(extras);

            Assert.Equal("0,0,0,0,0,0,0,0,0", bind.CompileExtras());
            Assert.False(bind.IsUsingExtras());
        }

        [Fact]
        public void Parse_NonNumericValues_BecomeZero()
        {
            var bind = new OutBinding();
            bind.ParseExtras("x,y,0,0,0,0,0,0,0");

            Assert.Equal(0, bind.HeavyRumble);
            Assert.Equal(0, bind.LightRumble);
        }

        [Fact]
        public void IsUsingExtras_IsFalseForAFreshBinding()
        {
            Assert.False(new OutBinding().IsUsingExtras());
        }

        [Theory]
        [InlineData("5,0,0,0,0,0,0,0,0")]
        [InlineData("0,5,0,0,0,0,0,0,0")]
        [InlineData("0,0,1,255,255,255,0,0,0")]
        [InlineData("0,0,0,0,0,0,0,1,25")]
        [InlineData("0,0,0,0,0,0,0,1,40")]
        public void IsUsingExtras_IsTrue_WhenAnyPartIsSet(string extras)
        {
            var bind = new OutBinding();
            bind.ParseExtras(extras);

            Assert.True(bind.IsUsingExtras());
        }

        [Fact]
        public void IsUsingExtras_ChangingOnlyOneColorChannel_IsNotCounted()
        {
            // 既存の癖: 「3 つの色がすべて 255 以外」のときだけ、色の変更とみなす（色の旗が立っていない場合）
            var bind = new OutBinding { ExtrasColorR = 10 };

            Assert.False(bind.IsUsingExtras());
        }

        [Fact]
        public void WriteBind_WritesTheExtrasOnlyWhenSomethingIsUsed()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            var settings = new DS4ControlSettings(DS4Controls.Cross) { extras = "stale" };
            var bind = new OutBinding { outputType = OutBinding.OutType.Default };

            bind.WriteBind(settings, profile);
            Assert.Equal(string.Empty, settings.extras);

            bind.ParseExtras(Full);
            bind.WriteBind(settings, profile);
            Assert.Equal(Full, settings.extras);
        }
    }
}
