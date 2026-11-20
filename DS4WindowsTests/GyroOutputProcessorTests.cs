using System;
using System.Reflection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Gyro;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ジャイロの出力の部品（<see cref="GyroOutputProcessor"/>）の振る舞い。TouchpadMouse から切り出したときの特性化:
    /// Controls の出力の許可がトリガーと反転で決まること、マウススティックが中立と最大で期待の値になること、トグルの状態の初期化。
    /// </summary>
    public class GyroOutputProcessorTests
    {
        private const int Slot = 0;

        private sealed class Fixture
        {
            public TouchpadMouse Mouse;
            public IProfileSettingsService Settings;
            public ControllerState State;
            public StickMotionState Sticks;
        }

        private static Fixture Create()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(
                new BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            InputDeviceCore device = TestDs4Device.Create();
            var state = new ControllerState { L1 = true };
            typeof(InputDeviceCore).GetField("cState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(device, state);
            var sticks = new StickMotionState();
            var mouse = new TouchpadMouse(Slot, device, settings, new RecordingVirtualKbm(), sticks, new SyntheticInputState(), TimeProvider.System);
            mouse.ToggleGyroControls = false;
            mouse.ToggleGyroMouse = false;
            mouse.ToggleGyroStick = false;
            return new Fixture { Mouse = mouse, Settings = settings, State = state, Sticks = sticks };
        }

        private static SixAxisEventArgs Motion(int yaw, int pitch)
        {
            var sa = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);
            sa.gyroYawFull = yaw;
            sa.gyroPitchFull = pitch;
            sa.gyroRollFull = 0;
            return new SixAxisEventArgs(DateTime.UtcNow, sa);
        }

        [Theory]
        [InlineData("4", false, false)]   // L1 が押されている＝トリガーが成立。反転なし → 出力を許可しない
        [InlineData("4", true, true)]     // 反転 → トリガーが成立している間だけ許可する
        [InlineData("5", false, true)]    // L2 は押されていない＝成立しない。反転なし → 許可する
        [InlineData("5", true, false)]
        public void Controls_OutputIsAllowed_AccordingToTheTriggerAndTheReverseFlag(string triggers, bool reverse, bool expected)
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.Controls;
            gyro.Controls.triggers = triggers;
            gyro.Controls.triggerCond = false;
            gyro.TriggerTurns = reverse;
            f.State.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);

            f.Mouse.SixaxisMoved(null, Motion(0, 0));

            Assert.Equal(expected, f.State.Motion.outputGyroControls);
        }

        [Fact]
        public void MouseStick_IsNeutralWithoutMotion_AndMovesWithALargeYaw()
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.MouseJoystick;
            gyro.MouseStickTriggers = string.Empty;       // トリガーなし＝反転なしなら常に出力する
            gyro.MouseStickTriggerCondAnd = false;
            gyro.MouseStickTriggerTurns = false;

            f.Mouse.SixaxisMoved(null, Motion(0, 0));
            Assert.Equal(128, f.Sticks.GyroStickX[Slot]);
            Assert.Equal(128, f.Sticks.GyroStickY[Slot]);

            f.Mouse.SixaxisMoved(null, Motion(20000, 0));
            Assert.True(f.Sticks.GyroStickX[Slot] > 128, $"X = {f.Sticks.GyroStickX[Slot]}");

            f.Mouse.SixaxisMoved(null, Motion(-20000, 0));
            Assert.True(f.Sticks.GyroStickX[Slot] < 128, $"X = {f.Sticks.GyroStickX[Slot]}");
        }

        [Fact]
        public void ResetToggleGyroModes_ClearsTheToggledState()
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.Controls;
            gyro.Controls.triggers = "4";
            gyro.Controls.triggerCond = false;
            gyro.TriggerTurns = false;
            f.State.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);

            f.Mouse.SixaxisMoved(null, Motion(0, 0));   // 部品が機器の現在の状態（L1 が押されている）を読み込む
            f.Mouse.ToggleGyroControls = true;
            // 立ち上がりでトグルが入る → 初期化で、押したままでもまた立ち上がりとして扱われる
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls));
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls));   // 押しっぱなし: トグルは保たれる
            f.Mouse.ResetToggleGyroModes();
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls));   // 初期化後の最初の立ち上がり
        }
    }
}
