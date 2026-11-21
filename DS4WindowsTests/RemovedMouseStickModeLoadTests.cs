using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 廃止した出力モード「マウススティック」（<c>MouseJoystick</c>）で保存された古いプロファイルは、
    /// 「操作（Controls）」モードの既定値として読み込む（そのモードの設定は、すべて既定値へ戻す）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class RemovedMouseStickModeLoadTests
    {
        private const int Slot = PersistenceHarness.Slot;

        [Fact]
        public void ATouchpadProfileSavedInTheRemovedMouseStickMode_LoadsAsControlsWithDefaults()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.touchpadSlots[Slot].OutputMode = TouchpadOutMode.MouseJoystick;
            store.touchpadSlots[Slot].TouchSensitivity = 77;
            store.touchpadSlots[Slot].Invert = 3;
            string xml = PersistenceHarness.SaveProfile(ctx, store, "RemovedTouchStick");
            Assert.Matches(@"<TouchpadOutputMode>\s*MouseJoystick\s*</TouchpadOutputMode>", xml);

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "RemovedTouchStickLoad");

            Assert.Equal(TouchpadOutMode.Controls, loaded.touchpadSlots[Slot].OutputMode);
            Assert.Equal(BackingStore.DEFAULT_TOUCHPAD_SENS, loaded.touchpadSlots[Slot].TouchSensitivity);
            Assert.Equal(0, loaded.touchpadSlots[Slot].Invert);
        }

        [Fact]
        public void AGyroProfileSavedInTheRemovedMouseStickMode_LoadsAsControlsWithDefaults()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.gyroSlots[Slot].OutputMode = GyroOutMode.MouseJoystick;
            store.gyroSlots[Slot].Sensitivity = 321;
            store.gyroSlots[Slot].MouseStick.deadZone = 55;
            string xml = PersistenceHarness.SaveProfile(ctx, store, "RemovedGyroStick");
            Assert.Matches(@"<GyroOutputMode>\s*MouseJoystick\s*</GyroOutputMode>", xml);

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "RemovedGyroStickLoad");

            Assert.Equal(GyroOutMode.Controls, loaded.gyroSlots[Slot].OutputMode);
            Assert.Equal(BackingStore.DEFAULT_GYRO_SENS, loaded.gyroSlots[Slot].Sensitivity);
            Assert.NotEqual(55, loaded.gyroSlots[Slot].MouseStick.deadZone);
        }

        [Fact]
        public void ATouchpadProfileInAnotherMode_KeepsItsSettings()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.touchpadSlots[Slot].OutputMode = TouchpadOutMode.Mouse;
            store.touchpadSlots[Slot].TouchSensitivity = 77;
            string xml = PersistenceHarness.SaveProfile(ctx, store, "KeepTouch");

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "KeepTouchLoad");

            Assert.Equal(TouchpadOutMode.Mouse, loaded.touchpadSlots[Slot].OutputMode);
            Assert.Equal(77, loaded.touchpadSlots[Slot].TouchSensitivity);
        }
    }
}
