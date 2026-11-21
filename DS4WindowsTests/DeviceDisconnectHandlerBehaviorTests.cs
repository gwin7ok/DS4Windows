#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// コントローラーを抜いたときの後始末（<see cref="DeviceDisconnectHandler.OnRemoval"/>）の、順序と結果を挙動で確かめる。
    /// 合成入力の確定 → マッピングの切断処理 → スロットの解放の順序（ガードレール: 切断時クリーンアップ）と、
    /// 一時プロファイルの状態を残さないこと（再接続で通常のルールを使うため）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class DeviceDisconnectHandlerBehaviorTests
    {
        private const int Slot = 2;

        private sealed class Rig
        {
            public readonly List<string> Order = new List<string>();
            public readonly DeviceSlotState Host = new DeviceSlotState();
            public readonly IProfileSettingsService Settings = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly TestDs4Device Device = TestDs4Device.Create();
            public readonly RecordingProxy Mapping, Applier;
            public readonly DeviceDisconnectHandler Handler;

            public Rig()
            {
                typeof(DS4Windows.Input.InputDeviceCore).GetField("removeLocker")!.SetValue(Device, new object());   // 未初期化の偽物の機器に、ロックの対象を与える
                Host.Controllers[Slot] = Device;

                var mapping = RecordingProxy.Create<IMapping>();
                Mapping = RecordingProxy.Of(mapping);
                Mapping.OnCall = name => Order.Add("mapping." + name);
                var applier = RecordingProxy.Create<IProfileSlotApplier>();
                Applier = RecordingProxy.Of(applier);
                Applier.OnCall = name => Order.Add("applier." + name);

                var outputTypes = RecordingProxy.Create<IOutputDeviceTypeState>();
                RecordingProxy.Of(outputTypes).Returns["get_ActiveOutDevType"] = new OutContType[ControllerLimits.MaxControllerCount];

                var osc = RecordingProxy.Create<IOscService>();
                RecordingProxy.Of(osc).OnCall = name => Order.Add("osc." + name);
                var latency = RecordingProxy.Create<ILatencyMonitor>();
                RecordingProxy.Of(latency).OnCall = name => Order.Add("latency." + name);
                var virtualPad = RecordingProxy.Create<IVirtualPadService>();
                RecordingProxy.Of(virtualPad).OnCall = name => Order.Add("virtualPad." + name);
                Settings.UseDInputOnlyArray[Slot] = false;   // 仮想パッドを使っていたスロット（取り外しの対象）

                Handler = new DeviceDisconnectHandler(Settings, mapping, applier,
                    osc, RecordingProxy.Create<IAppSettingsService>(),
                    latency, RecordingProxy.Create<IDeviceStateService>(),
                    virtualPad, outputTypes,
                    RecordingProxy.Create<IEnvironmentService>(), Host);
            }
        }

        [Fact]
        public void TheRemoval_CommitsTheSyntheticInput_ThenHandlesTheDisconnect_ThenReleasesTheSlot()
        {
            var rig = new Rig();

            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);

            int commit = rig.Order.IndexOf("mapping.Commit");
            int handle = rig.Order.IndexOf("mapping.HandleDeviceDisconnect");
            int release = rig.Order.IndexOf("applier.ReleaseSlot");
            Assert.True(commit >= 0 && handle > commit && release > handle, string.Join(" → ", rig.Order));
            Assert.Equal(new object?[] { Slot }, rig.Mapping.ArgsOf("HandleDeviceDisconnect").Single());
            Assert.Equal(new object?[] { Slot }, rig.Applier.ArgsOf("ReleaseSlot").Single());
        }

        [Fact]
        public void TheRemoval_KeepsTheWholeCleanupOrder()
        {
            var rig = new Rig();

            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);

            // 仮想パッドの取り外し → 合成入力のコミット → マッピングの切断 → スロットの解放 → OSC の状態の初期化 → 遅延の計測の初期化
            string[] expected =
            {
                "virtualPad.UnplugOutDev", "mapping.Commit", "mapping.HandleDeviceDisconnect", "applier.ReleaseSlot",
                "osc.ResetDeviceState", "latency.Reset",
            };
            Assert.Equal(expected, rig.Order.Where(expected.Contains).ToArray());
        }

        [Fact]
        public void TheRemoval_FreesTheSlot_AndMarksTheDeviceRemoved()
        {
            var rig = new Rig();

            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);

            Assert.Null(rig.Host.Controllers[Slot]);
            Assert.True(rig.Device.IsRemoved);
            Assert.False(rig.Device.Synced);
        }

        [Fact]
        public void TheRemoval_ClearsTheTempProfileState_OfTheDisconnectedSlot()
        {
            var rig = new Rig();
            rig.Settings.SetTempProfileName(Slot, "AutoGame");
            rig.Settings.SetUseTempProfile(Slot, true);
            rig.Settings.SetTempProfileDistance(Slot, true);
            rig.Settings.SetTempProfileName(Slot + 1, "Other");   // ほかのスロットには触れない
            rig.Settings.SetUseTempProfile(Slot + 1, true);

            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);

            Assert.Equal(string.Empty, rig.Settings.GetTempProfileName(Slot));
            Assert.False(rig.Settings.GetUseTempProfile(Slot));
            Assert.False(rig.Settings.TempProfileDistanceArray[Slot]);
            Assert.Equal("Other", rig.Settings.GetTempProfileName(Slot + 1));
            Assert.True(rig.Settings.GetUseTempProfile(Slot + 1));
        }

        [Fact]
        public void ASecondRemovalOfTheSameDevice_DoesNothing()
        {
            var rig = new Rig();
            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);
            int calls = rig.Order.Count;

            rig.Handler.OnRemoval(rig.Device, EventArgs.Empty);   // 空いたスロットには、もう見つからない

            Assert.Equal(calls, rig.Order.Count);
        }
    }
}
