#nullable enable
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Slots;
using DS4Windows.Processing.Services.Devices;
using DS4Windows.Core.Model;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 接続時のプロファイルの選び方（<see cref="ConnectionProfileSelector"/>）と、適用のたびの記憶（<see cref="AppliedProfileRecorder"/>）を挙動で確かめる。
    /// 初回接続は Linked か OlderProfilePath、再接続はそのコントローラーの記憶（なければスロットの SelectedProfile）。
    /// 判定はコントローラー（MAC）ごとで、スロットごとではない。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ConnectionProfileChoiceTests
    {
        private const int Slot = 5;
        private const string Mac = "AA:BB:CC:DD:EE:05";

        private sealed class Rig
        {
            public readonly IProfileSettingsService Settings = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly IProfileRepository Repository;
            public readonly DeviceStateService State = TestRepositoryFactory.CreateDeviceStateService();
            public readonly List<string> Applied = new List<string>();
            public readonly ConnectionProfileSelector Selector;

            public Rig()
            {
                Repository = TestRepositoryFactory.CreateProfileRepository(Settings);
                Repository.OlderProfilePath[Slot] = "Older";
                Repository.SelectedProfile[Slot] = "SlotSelected";
                var applier = RecordingProxy.Create<IProfileSlotApplier>();
                var proxy = RecordingProxy.Of(applier);
                proxy.Returns["ApplyToSlot"] = true;
                proxy.OnCall = name => { };
                Applier = proxy;
                Selector = new ConnectionProfileSelector(Settings, Repository, State, applier);
            }

            public readonly RecordingProxy Applier;
            public string? LastApplied => (string?)Applier.ArgsOf("ApplyToSlot").LastOrDefault()?[1];
        }

        [Fact]
        public void TheFirstConnection_AppliesTheOlderProfile_AndMarksTheControllerConnected()
        {
            var rig = new Rig();

            rig.Selector.ApplyOnConnect(Slot, false, true, Mac);

            Assert.Equal("Older", rig.LastApplied);
            Assert.False(rig.State.IsFirstConnection(Slot, Mac));
            Assert.Equal("Older", rig.State.GetRememberedProfile(Mac));   // 適用したものを記憶する
            Assert.Equal("Older", rig.Repository.SelectedProfile[Slot]);
        }

        [Fact]
        public void AReconnection_WithoutAMemory_FallsBackToTheSlotsSelectedProfile()
        {
            var rig = new Rig();
            rig.State.MarkConnected(Slot, Mac);   // 初回は済んでいるが、記憶はない

            rig.Selector.ApplyOnConnect(Slot, false, true, Mac);

            Assert.Equal("SlotSelected", rig.LastApplied);
        }

        [Fact]
        public void AReconnection_UsesTheControllersRememberedProfile_NotTheSlots()
        {
            var rig = new Rig();
            rig.State.MarkConnected(Slot, Mac);
            rig.State.RememberProfile(Mac, "Remembered");

            rig.Selector.ApplyOnConnect(Slot, false, true, Mac);

            Assert.Equal("Remembered", rig.LastApplied);
        }

        [Fact]
        public void AnotherController_InTheSameSlot_IsAFirstConnection()
        {
            var rig = new Rig();
            rig.State.MarkConnected(Slot, Mac);
            rig.State.RememberProfile(Mac, "Remembered");

            rig.Selector.ApplyOnConnect(Slot, false, true, "AA:BB:CC:DD:EE:99");

            Assert.Equal("Older", rig.LastApplied);
        }

        [Fact]
        public void AutoProfile_DoesNotApplyOrMarkTheController()
        {
            var rig = new Rig();

            rig.Selector.ApplyOnConnect(Slot, true, true, Mac);

            Assert.Empty(rig.Applier.ArgsOf("ApplyToSlot"));
            Assert.Single(rig.Applier.ArgsOf("PublishSlot"));
            Assert.True(rig.State.IsFirstConnection(Slot, Mac));
        }

        [Fact]
        public void ALinkedProfile_WinsOnTheFirstConnection_AndIsShownInTheUi()
        {
            var rig = new Rig();
            rig.Repository.ChangeLinkedProfile(Mac, "LinkedOne");
            try
            {
                rig.Selector.ApplyOnConnect(Slot, false, true, Mac);

                Assert.Equal("LinkedOne", rig.LastApplied);
                Assert.Equal("LinkedOne", rig.Repository.LinkedProfileUI[Slot]);
                Assert.True(rig.Settings.GetLinkedProfileCheck(Slot));
            }
            finally
            {
                rig.Repository.RemoveLinkedProfile(Mac);
            }
        }

        [Fact]
        public void WithoutALinkedProfile_TheLinkedUiIsCleared()
        {
            var rig = new Rig();
            rig.Repository.LinkedProfileUI[Slot] = "Stale";
            rig.Settings.SetLinkedProfileCheck(Slot, true);

            rig.Selector.ApplyOnConnect(Slot, false, true, Mac);

            Assert.Equal(string.Empty, rig.Repository.LinkedProfileUI[Slot]);
            Assert.False(rig.Settings.GetLinkedProfileCheck(Slot));
        }

        [Fact]
        public void TheRecorder_RemembersEachAppliedProfile_PerController()
        {
            var rig = new Rig();
            var slots = new DeviceSlotState();
            var device = TestDs4Device.Create();
            device.SetMac(Mac);
            slots.Controllers[Slot] = device;
            new AppliedProfileRecorder(rig.Repository, rig.Settings, rig.State, slots).Attach();

            rig.Repository.RaiseSelectedProfileChanged(Slot, "Applied1");

            Assert.Equal("Applied1", rig.State.GetRememberedProfile(Mac));
        }

        [Fact]
        public void TheRecorder_IgnoresTempProfiles_AndMissingDevices()
        {
            var rig = new Rig();
            var slots = new DeviceSlotState();
            var device = TestDs4Device.Create();
            device.SetMac(Mac);
            slots.Controllers[Slot] = device;
            new AppliedProfileRecorder(rig.Repository, rig.Settings, rig.State, slots).Attach();
            rig.Settings.SetUseTempProfile(Slot, true);
            try
            {
                rig.Repository.RaiseSelectedProfileChanged(Slot, "AutoProfile");   // 一時プロファイルは、通常のプロファイルとして記憶しない
                rig.Repository.RaiseSelectedProfileChanged(Slot + 1, "NoDevice");   // 機器のないスロット
                rig.Repository.RaiseSelectedProfileChanged(-1, "Invalid");

                Assert.Null(rig.State.GetRememberedProfile(Mac));
            }
            finally
            {
                rig.Settings.SetUseTempProfile(Slot, false);
            }
        }
    }
}
