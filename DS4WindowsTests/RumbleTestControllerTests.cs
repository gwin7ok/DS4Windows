using System.Collections.Generic;
using DS4Windows.Core.Platform;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 振動テストのボタンの動作（<see cref="RumbleTestController"/>）。
    /// 元は <c>ProfileEditor</c> のクリック処理にあった分岐で、反転設定でのモーターの入れ替わり・DualSense の強さの補正・開始と停止を固定する。
    /// </summary>
    [Xunit.Collection("SharedProfileStoreRumble")]
    public class RumbleTestControllerTests : System.IDisposable
    {
        // 共有のストアを書き換えるため、終わりに元へ戻す（ほかのテストへ状態を残さない）
        private readonly List<System.Action> restore = new List<System.Action>();

        public void Dispose() { foreach (var r in restore) r(); }

        private sealed class FakeCommands : IControllerSlotCommands
        {
            public bool Connected = true;
            public bool DualSense;
            public readonly List<string> Calls = new List<string>();
            public bool IsConnected(int slot) => Connected;
            public bool SetRumble(int slot, byte rightLightFast, byte leftHeavySlow) => true;
            public bool SetHeavyRumble(int slot, byte strength) { Calls.Add("H" + strength); return true; }
            public bool SetLightRumble(int slot, byte strength) { Calls.Add("L" + strength); return true; }
            public bool ApplyDualSenseRumbleOptions(int slot, int rumbleEmulationMode, byte hapticPowerLevel) => DualSense;
            public bool ResetGyroCalibration(int slot) => true;
        }

        private (RumbleTestController ctl, FakeCommands cmd, DS4Windows.UI.ViewModels.OutputSettingsViewModel output) Create(bool inverse = false, int boost = 100)
        {
            var cmd = new FakeCommands();
            var output = ProfileSettingsViewModelTestFactory.Create(0).Output;
            int oldBoost = output.RumbleBoost; bool oldInverse = output.InverseRumbleMotors; bool oldRescale = output.EnableGenericRumbleStrRescaleForDualSenseDevices;
            restore.Add(() => { output.RumbleBoost = oldBoost; output.InverseRumbleMotors = oldInverse; output.EnableGenericRumbleStrRescaleForDualSenseDevices = oldRescale; output.HeavyRumbleActive = false; output.LightRumbleActive = false; });
            output.InverseRumbleMotors = inverse;
            output.RumbleBoost = boost;
            output.HeavyRumbleActive = false;
            output.LightRumbleActive = false;
            return (new RumbleTestController(cmd, new EnvironmentService()), cmd, output);
        }

        [Fact]
        public void Heavy_StartThenStop()
        {
            var (ctl, cmd, output) = Create();
            Assert.Equal(RumbleTestOutcome.Started, ctl.Toggle(output, 0, heavyButtonPressed: true));
            Assert.True(output.HeavyRumbleActive);
            Assert.False(output.LightRumbleActive);
            Assert.Equal(RumbleTestOutcome.Stopped, ctl.Toggle(output, 0, heavyButtonPressed: true));
            Assert.False(output.HeavyRumbleActive);
            Assert.Equal(new[] { "H255", "H0" }, cmd.Calls);
        }

        [Fact]
        public void InverseMotors_HeavyButtonDrivesLightMotor()
        {
            var (ctl, cmd, output) = Create(inverse: true);
            ctl.Toggle(output, 0, heavyButtonPressed: true);
            Assert.True(output.LightRumbleActive);
            Assert.False(output.HeavyRumbleActive);
            Assert.Equal(new[] { "L255" }, cmd.Calls);
        }

        [Fact]
        public void Boost_ScalesStrength_AndIsCappedAt255()
        {
            var (ctl, cmd, output) = Create(boost: 50);
            ctl.Toggle(output, 0, false);
            Assert.Equal("L127", cmd.Calls[0]);

            var (ctl2, cmd2, output2) = Create(boost: 200);
            ctl2.Toggle(output2, 0, false);
            Assert.Equal("L255", cmd2.Calls[0]);
        }

        [Fact]
        public void DualSense_WithoutGenericRescale_UsesFullStrength()
        {
            var (ctl, cmd, output) = Create(boost: 50);
            cmd.DualSense = true;
            output.EnableGenericRumbleStrRescaleForDualSenseDevices = false;
            ctl.Toggle(output, 0, true);
            Assert.Equal("H255", cmd.Calls[0]);
        }

        [Fact]
        public void NotConnected_DoesNothing()
        {
            var (ctl, cmd, output) = Create();
            cmd.Connected = false;
            Assert.Equal(RumbleTestOutcome.None, ctl.Toggle(output, 0, true));
            Assert.Empty(cmd.Calls);
            Assert.False(output.HeavyRumbleActive);
        }
    }
}
