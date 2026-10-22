using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: 入力レポートの処理から切り出した 3 つの部品の値の特性化（旧 <c>ControlService</c> の挙動）。
    /// タッチパッドの切り替え（PS＋タッチで ON／OFF、離すまで再切り替えしない）、スライド、押されている入力の探索の順序。
    /// <c>Mouse</c> と <c>DS4Device</c> は作れないため、それらを読まない経路（tp が null、デバイスなし）を調べる。
    /// </summary>
    public class Phase9EInputReportPartsTests
    {
        private const int Slot = 0;

        private static TouchpadToggleService NewToggle(out IProfileSettingsService settings)
        {
            settings = TestRepositoryFactory.CreateProfileSettingsService();
            settings.TouchOutMode[Slot] = TouchpadOutMode.Mouse;
            settings.TouchpadActiveArray[Slot] = true;
            return new TouchpadToggleService(settings, TestHost.GetService<IEnvironmentService>());
        }

        [Fact]
        public void Toggle_TurnsTheTouchpadOff_ThenWaitsForRelease_ThenTurnsItOn()
        {
            var toggle = NewToggle(out var settings);
            var touching = new DS4State { Touch1 = true };
            var psWasDown = new DS4State { PS = true };

            toggle.CheckForToggle(Slot, touching, psWasDown);
            Assert.False(settings.TouchpadActiveArray[Slot]);

            // 離さないうちは、もう一度切り替えない
            toggle.CheckForToggle(Slot, touching, psWasDown);
            Assert.False(settings.TouchpadActiveArray[Slot]);

            // 離してから、もう一度 → ON に戻る
            toggle.CheckForToggle(Slot, new DS4State(), new DS4State());
            toggle.CheckForToggle(Slot, touching, psWasDown);
            Assert.True(settings.TouchpadActiveArray[Slot]);
        }

        [Fact]
        public void Toggle_DoesNothing_WhenTheTouchpadOutputIsControls()
        {
            var toggle = NewToggle(out var settings);
            settings.TouchOutMode[Slot] = TouchpadOutMode.Controls;

            toggle.CheckForToggle(Slot, new DS4State { Touch1 = true }, new DS4State { PS = true });

            Assert.True(settings.TouchpadActiveArray[Slot]);
        }

        [Fact]
        public void StartTouchpadOff_TurnsItOff_OnlyInsideTheSlotLimit()
        {
            var toggle = NewToggle(out var settings);
            int limit = TestHost.GetService<IEnvironmentService>().ControllerSlotLimit;

            toggle.StartTouchpadOff(Slot);
            Assert.False(settings.TouchpadActiveArray[Slot]);

            // 範囲外の添字は何もしない（例外も出さない）
            Assert.Null(Record.Exception(() => toggle.StartTouchpadOff(limit)));
        }

        [Fact]
        public void Slide_IsNone_WhenNotConnected_OrNotTouchingWithTwoFingers()
        {
            var toggle = NewToggle(out _);

            Assert.Equal("none", toggle.Slide(Slot, new DS4State { Touch2 = true }, null, false));
            Assert.Equal("none", toggle.Slide(Slot, new DS4State { Touch2 = false }, null, true));
        }

        [Fact]
        public void Probe_ReturnsNone_WhenNotConnected()
        {
            var probe = new ActiveInputProbe();
            Assert.Equal(DS4Controls.None, probe.GetActive(new DS4State { Cross = true }, null, false));
        }

        [Fact]
        public void Probe_ReturnsTheFirstActiveInput_InTheOriginalOrder()
        {
            var probe = new ActiveInputProbe();

            // Cross は Circle・L1 などより先に判定される
            Assert.Equal(DS4Controls.Cross, probe.GetActive(new DS4State { Cross = true, Circle = true, L1 = true }, null, true));
            Assert.Equal(DS4Controls.Circle, probe.GetActive(new DS4State { Circle = true, L1 = true }, null, true));
            Assert.Equal(DS4Controls.L1, probe.GetActive(new DS4State { L1 = true, R1 = true }, null, true));
            // L1 の次は L2（トリガー）、その次が L3
            Assert.Equal(DS4Controls.L2, probe.GetActive(new DS4State { L2 = 255, L3 = true }, null, true));
            Assert.Equal(DS4Controls.DpadUp, probe.GetActive(new DS4State { DpadUp = true, Share = true }, null, true));
        }

        [Fact]
        public void LatencyMonitor_Reset_DoesNotThrow_ForEverySlot()
        {
            var monitor = new LatencyMonitor(TestHost.GetService<IAppSettingsService>(), TestRepositoryFactory.CreateProfileSettingsService());
            for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
            {
                int slot = i;
                Assert.Null(Record.Exception(() => monitor.Reset(slot)));
            }
        }
    }
}
