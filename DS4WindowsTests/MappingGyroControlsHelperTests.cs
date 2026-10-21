using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6b（決定 P2）: ジャイロを Controls に使うかの判定（`Global.IsUsingSAForControls` の置き換え）が、
    /// 置き換え前と同じ結果（出力モードが Controls のときだけ true）になることを検証する。
    /// Phase9 9-B: 判定は `Mapping.IsUsingGyroForControls`（private）から `ControlActivityReader` の中へ移ったため、
    /// リフレクションではなく、公開の窓口（`IsActive`）の結果（GyroXNeg が有効になるのは Controls のときだけ）で確かめる。
    /// 検証後は元の値へ戻す（Global の static 状態を残さない。Phase6-Status.md §6.4 の教訓1）。
    /// </summary>
    public class MappingGyroControlsHelperTests
    {
        private const int Slot = 0;

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.None)]
        [InlineData(GyroOutMode.Mouse)]
        public void GyroControlsJudgement_IsTrueOnlyWhenTheOutputModeIsControls(GyroOutMode mode)
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            GyroOutMode originalMode = settings.GyroOutputMode[Slot];
            double originalSx = settings.SXSens[Slot];
            try
            {
                settings.GyroOutputMode[Slot] = mode;
                settings.SXSens[Slot] = 1.0;

                var state = new DS4State();
                state.Motion.accelX = 1000;   // GyroXNeg: SXSens * -AccelX < -67 （Controls のときだけ判定される）

                bool actual = new ControlActivityReader().IsActive(settings, Slot, DS4Controls.GyroXNeg, state, new DS4StateExposed(state), null);

                Assert.Equal(mode == GyroOutMode.Controls, actual);   // Global.IsUsingSAForControls は Phase6-Step12 Wave 4 で削除（同じ判定）
            }
            finally
            {
                settings.GyroOutputMode[Slot] = originalMode;
                settings.SXSens[Slot] = originalSx;
            }
        }
    }
}
