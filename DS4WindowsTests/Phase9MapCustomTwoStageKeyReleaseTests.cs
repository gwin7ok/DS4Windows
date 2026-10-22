using System.Threading;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 MapCustom の分割 M3 の後の挙動修正（Phase9-TwoStageStuckKey-Fix-Plan.md）:
    /// 2 段階トリガーの状態機械が、トリガーを離したフレームで <c>previousActiveButtons</c> を Reset() してしまい、
    /// 「前のフレームで出力していた」ことが分からなくなるため、Key の割り当てに離した値（0）が渡らず、キーが押されたままになる不具合の再現と固定。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase9MapCustomTwoStageKeyReleaseTests
    {
        private static void Frame(ControlsKeyTestScope scope, bool isL2, byte value, byte raw)
        {
            DS4State input = MappingCharacterizationScope.NeutralState();
            if (isL2) { input.L2 = value; input.L2Raw = raw; } else { input.R2 = value; input.R2Raw = raw; }
            scope.Scope.RunMapCustom(input);
        }

        [Theory]
        [InlineData(true, TwoStageTriggerMode.HipFire)]
        [InlineData(false, TwoStageTriggerMode.HipFire)]
        [InlineData(true, TwoStageTriggerMode.HairTrigger)]
        [InlineData(false, TwoStageTriggerMode.HairTrigger)]
        [InlineData(true, TwoStageTriggerMode.ExclusiveButtons)]
        [InlineData(false, TwoStageTriggerMode.ExclusiveButtons)]
        public void SoftPullKey_IsReleased_WhenTheTriggerIsReleased(bool isL2, TwoStageTriggerMode mode)
        {
            using var scope = new ControlsKeyTestScope();
            TriggerOutputSettings settings = isL2
                ? scope.Scope.Settings.L2OutputSettings[scope.Device]
                : scope.Scope.Settings.R2OutputSettings[scope.Device];
            TwoStageTriggerMode savedMode = settings.twoStageMode;
            int savedHipFire = settings.hipFireMS;
            try
            {
                settings.twoStageMode = mode;
                settings.hipFireMS = 10;
                scope.MapKey(isL2 ? DS4Controls.L2 : DS4Controls.R2, 65);
                scope.MapKey(isL2 ? DS4Controls.L2FullPull : DS4Controls.R2FullPull, 66);
                scope.Publish();

                Frame(scope, isL2, 120, 120);       // 浅く引く（Hip Fire は、ここで待ちが始まる）
                Thread.Sleep(60);
                Frame(scope, isL2, 120, 120);       // 待ちが明けて、浅い引きのキーが押される
                Assert.Contains(scope.Native(65), scope.Recorder.KeyPresses);

                Frame(scope, isL2, 0, 0);           // 離す
                Frame(scope, isL2, 0, 0);

                Assert.Contains(scope.Native(65), scope.Recorder.KeyReleases);
            }
            finally
            {
                settings.twoStageMode = savedMode;
                settings.hipFireMS = savedHipFire;
            }
        }
    }
}
