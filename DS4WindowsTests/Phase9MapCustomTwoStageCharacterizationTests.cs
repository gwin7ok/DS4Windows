using System;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 MapCustom の分割 M3: <c>MapCustom</c> の L2／R2 の複製を 1 つにまとめる前に、2 段階トリガーの出力（浅い引き＝Y、深い引き＝B）を
    /// <c>MapCustom</c> の全体を通して固定する。L2 と R2 で同じ結果になること（折りたたみで取り違えがないこと）も確かめる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase9MapCustomTwoStageCharacterizationTests
    {
        private static DS4State Run(bool isL2, TwoStageTriggerMode mode, byte value, byte raw)
        {
            using var scope = new MappingCharacterizationScope();
            TriggerOutputSettings settings = isL2
                ? scope.Settings.L2OutputSettings[MappingCharacterizationScope.Device]
                : scope.Settings.R2OutputSettings[MappingCharacterizationScope.Device];
            TwoStageTriggerMode saved = settings.twoStageMode;
            try
            {
                settings.twoStageMode = mode;
                DS4Controls soft = isL2 ? DS4Controls.L2 : DS4Controls.R2;
                DS4Controls full = isL2 ? DS4Controls.L2FullPull : DS4Controls.R2FullPull;
                scope.RemapToButton(soft, X360Controls.Y);   // 浅い引き → Y（Triangle）
                scope.RemapToButton(full, X360Controls.B);   // 深い引き → B（Circle）

                DS4State input = MappingCharacterizationScope.NeutralState();
                if (isL2) { input.L2 = value; input.L2Raw = raw; }
                else { input.R2 = value; input.R2Raw = raw; }
                return scope.RunMapCustom(input);
            }
            finally
            {
                settings.twoStageMode = saved;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Disabled_OnlyTheNormalBindingApplies(bool isL2)
        {
            DS4State output = Run(isL2, TwoStageTriggerMode.Disabled, 220, 220);

            Assert.True(output.Triangle);
            Assert.False(output.Circle);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Normal_SoftPull_OutputsOnlyTheSoftBinding(bool isL2)
        {
            DS4State output = Run(isL2, TwoStageTriggerMode.Normal, 220, 220);

            Assert.True(output.Triangle);
            Assert.False(output.Circle);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Normal_FullPull_OutputsBothBindings(bool isL2)
        {
            DS4State output = Run(isL2, TwoStageTriggerMode.Normal, 255, 255);

            Assert.True(output.Triangle);
            Assert.True(output.Circle);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExclusiveButtons_FullPull_OutputsOnlyTheFullBinding(bool isL2)
        {
            DS4State output = Run(isL2, TwoStageTriggerMode.ExclusiveButtons, 255, 255);

            Assert.False(output.Triangle);
            Assert.True(output.Circle);
        }

        [Theory]
        [InlineData(true, TwoStageTriggerMode.Normal)]
        [InlineData(true, TwoStageTriggerMode.ExclusiveButtons)]
        [InlineData(false, TwoStageTriggerMode.Normal)]
        [InlineData(false, TwoStageTriggerMode.ExclusiveButtons)]
        public void Released_OutputsNothing(bool isL2, TwoStageTriggerMode mode)
        {
            DS4State output = Run(isL2, mode, 0, 0);

            Assert.False(output.Triangle);
            Assert.False(output.Circle);
        }
    }
}
