using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ジャイロのトリガーの判定（<see cref="TouchpadMouse.IsGyroTriggerActive"/>）のテスト。
    /// 判定は動きのフレームごとに行われる。設定の文字列（「4,5」）の解釈は、判定の結果を変えずに、
    /// 割り当てなしで行うこと。旧実装（毎回 Split と TryParse）と、全パターンで結果が一致することを固定する。
    /// </summary>
    public class TouchpadMouseGyroTriggerTests
    {
        private const int Slot = 0;

        private static readonly string[] Patterns =
        {
            "-1", "4", "5", "4,5", "5,4", "0,4", "0,1", "8,x,4", "x,4", "4,x", "x", "", ",", "4,", ",4", " 4", "4,4", "99", "-1,0", "0,-1",
        };

        // ボタンの状態: Cross(0)、L1(4)、DpadUp(8) が押されている。ほかは押されていない
        private static bool Pressed(int key) => key == -1 || key == 0 || key == 4 || key == 8;

        // 旧実装（毎回 Split と TryParse）の判定の部分
        private static bool OldDecision(string triggers, bool andCond)
        {
            string[] ss = triggers.Split(',');
            bool result = andCond;
            if (!string.IsNullOrEmpty(ss[0]))
            {
                for (int index = 0; index < ss.Length; index++)
                {
                    if (andCond && !(int.TryParse(ss[index], out int i) && Pressed(i)))
                    {
                        result = false;
                        break;
                    }
                    else if (!andCond && int.TryParse(ss[index], out i) && Pressed(i))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private static (TouchpadMouse mouse, IProfileSettingsService settings) Create()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(
                new BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            InputDeviceCore device = (InputDeviceCore)RuntimeHelpers.GetUninitializedObject(typeof(TestDs4Device));
            var mouse = new TouchpadMouse(Slot, device, settings, new RecordingVirtualKbm(), new StickMotionState(),
                new SyntheticInputState(), TimeProvider.System);

            // 切り替え（トグル）の状態を判定に混ぜないため、トグルを切る（既定は有効）
            mouse.ToggleGyroControls = false;

            var state = new ControllerState { Cross = true, L1 = true, DpadUp = true };
            typeof(TouchpadMouse).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(mouse, state);
            return (mouse, settings);
        }

        private static void SetTriggers(IProfileSettingsService settings, GyroOutMode mode, string triggers, bool andCond)
        {
            var gyro = settings.ForSlot(Slot).Gyro;
            switch (mode)
            {
                case GyroOutMode.Controls: gyro.Controls.triggers = triggers; gyro.Controls.triggerCond = andCond; break;
                case GyroOutMode.Mouse: gyro.MouseTriggers = triggers; gyro.MouseTriggerCondAnd = andCond; break;
                case GyroOutMode.MouseJoystick: gyro.MouseStickTriggers = triggers; gyro.MouseStickTriggerCondAnd = andCond; break;
            }
        }

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Mouse)]
        [InlineData(GyroOutMode.MouseJoystick)]
        public void TheDecision_MatchesTheOldImplementation_ForEveryTriggerPattern(GyroOutMode mode)
        {
            var (mouse, settings) = Create();

            foreach (string pattern in Patterns)
            {
                foreach (bool andCond in new[] { false, true })
                {
                    SetTriggers(settings, mode, pattern, andCond);
                    Assert.True(OldDecision(pattern, andCond) == mouse.IsGyroTriggerActive(mode),
                        $"mode={mode} triggers=\"{pattern}\" and={andCond}");
                }
            }
        }

        [Fact]
        public void ChangingTheTriggerString_IsPickedUpOnTheNextCall()
        {
            var (mouse, settings) = Create();

            SetTriggers(settings, GyroOutMode.Mouse, "5", false);
            Assert.False(mouse.IsGyroTriggerActive(GyroOutMode.Mouse));
            SetTriggers(settings, GyroOutMode.Mouse, "4", false);
            Assert.True(mouse.IsGyroTriggerActive(GyroOutMode.Mouse));
        }

        [Fact]
        public void TheDecision_DoesNotAllocate_PerFrame()
        {
            var (mouse, settings) = Create();
            SetTriggers(settings, GyroOutMode.Mouse, "5,4,0", false);

            for (int i = 0; i < 100; i++) mouse.IsGyroTriggerActive(GyroOutMode.Mouse);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++) mouse.IsGyroTriggerActive(GyroOutMode.Mouse);
            long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / 5000;

            Assert.True(perCall == 0, $"1 回あたり {perCall} バイト");
        }
    }
}
