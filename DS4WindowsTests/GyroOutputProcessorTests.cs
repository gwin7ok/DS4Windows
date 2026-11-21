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
using DS4Windows.Processing.Services.Conditioning;
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
            var mouse = new TouchpadMouseFactory(settings, new RecordingVirtualKbm(), sticks, new SyntheticInputState(), TimeProvider.System).Create(Slot, device);
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
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));   // 押しっぱなし: トグルは保たれる
            f.Mouse.ResetToggleGyroModes();
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));   // 初期化後の最初の立ち上がり
        }

        // ---- RV13-0: 判定・トグル・逆ラチェット・マウススティックの初期化の特性化（RV13 の整理で挙動を変えないための固定） ----

        [Theory]
        [InlineData(false, true)]    // OR: L1 が押されていれば成立
        [InlineData(true, false)]    // AND: L2 が押されていないので成立しない
        public void Controls_TriggerList_IsCombinedByOrOrAnd(bool andCondition, bool expectedActive)
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.Controls;
            gyro.Controls.triggers = "4,5";
            gyro.Controls.triggerCond = andCondition;
            gyro.TriggerTurns = false;
            f.State.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);
            f.Mouse.SixaxisMoved(null, Motion(0, 0));   // 機器の現在の状態（L1 が押されている）を読み込む

            Assert.Equal(expectedActive, f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));
        }

        [Fact]
        public void EachMode_ReadsItsOwnTriggerList()
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.Controls;
            gyro.Controls.triggers = "5";                // 押されていない
            gyro.Controls.triggerCond = false;
            gyro.MouseTriggers = "4";                     // 押されている
            gyro.MouseTriggerCondAnd = false;
            gyro.MouseStickTriggers = "5";                // 押されていない
            gyro.MouseStickTriggerCondAnd = false;
            f.State.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);
            f.Mouse.SixaxisMoved(null, Motion(0, 0));

            Assert.False(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Mouse, f.State));
            Assert.False(f.Mouse.IsGyroTriggerActive(GyroOutMode.MouseJoystick, f.State));
        }

        [Fact]
        public void EmptyTriggerList_IsActiveOnlyForTheAndCondition()
        {
            Fixture f = Create();
            var gyro = f.Settings.ForSlot(Slot).Gyro;
            gyro.OutputMode = GyroOutMode.Controls;
            gyro.Controls.triggers = string.Empty;
            f.State.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.004);
            f.Mouse.SixaxisMoved(null, Motion(0, 0));

            gyro.Controls.triggerCond = false;
            Assert.False(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));
            gyro.Controls.triggerCond = true;
            Assert.True(f.Mouse.IsGyroTriggerActive(GyroOutMode.Controls, f.State));
        }

    }
}
