using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6a（Phase9-E6-Plan.md）: <see cref="ConnectionProfileSelector"/>（旧 <c>ControlService.PrepareConnectedInputControllerSettingEvents</c> の前半）の値の特性化。
    /// 一時プロファイルのときは束縛の差し替えだけ、初回接続は OlderProfilePath、再接続は直前のプロファイル。
    /// 共有のストアを一時的に書き換えるため、ほかのテストが使わない添字（6）を使い、終わりに元へ戻す。
    /// </summary>
    public class Phase9E6ConnectionProfileSelectorTests
    {
        private const int Slot = 6;

        private sealed class RecordingApplier : IProfileSlotApplier
        {
            public readonly List<string> Calls = new List<string>();
            public bool Result = true;

            public bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source)
            {
                Calls.Add($"apply:{slotIndex}:{profileName}:{source}");
                return Result;
            }

            public void ReleaseSlot(int slotIndex) { Calls.Add("release:" + slotIndex); }

            public void PublishSlot(int slotIndex) { Calls.Add("publish:" + slotIndex); }
        }

        private static ConnectionProfileSelector NewSelector(RecordingApplier applier, out IProfileRepository repository)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            repository = TestRepositoryFactory.CreateProfileRepository(settings);
            return new ConnectionProfileSelector(settings, repository, TestRepositoryFactory.CreateDeviceStateService(), applier);
        }

        [Fact]
        public void AutoProfile_OnlyPublishesTheSlot_AndDoesNotApply()
        {
            var applier = new RecordingApplier();
            var selector = NewSelector(applier, out _);

            bool loaded = selector.ApplyOnConnect(Slot, true, true, "00:11:22:33:44:55");

            Assert.False(loaded);
            Assert.Equal(new[] { "publish:" + Slot }, applier.Calls);
        }

        [Fact]
        public void FirstConnection_AppliesTheOlderProfile_ThenReconnectionUsesTheRememberedOne()
        {
            var applier = new RecordingApplier();
            var selector = NewSelector(applier, out var repository);
            string oldOlder = repository.OlderProfilePath[Slot];
            string oldSelected = repository.SelectedProfile[Slot];
            try
            {
                repository.OlderProfilePath[Slot] = "P9E6-First";
                const string Mac = "AA:BB:CC:DD:EE:06";

                bool first = selector.ApplyOnConnect(Slot, false, true, Mac);
                Assert.True(first);
                Assert.Equal("apply:" + Slot + ":P9E6-First:" + ProfileChangeSource.ControlService, applier.Calls[0]);

                // 適用のたびに記憶される（SelectedProfileChanged の購読は ControlService 側）。ここでは、記憶された名前を直接入れて再接続を確かめる
                var state = TestRepositoryFactory.CreateDeviceStateService();
                var selector2 = new ConnectionProfileSelector(TestRepositoryFactory.CreateProfileSettingsService(), repository, state, applier);
                selector2.ApplyOnConnect(Slot, false, true, Mac);   // 初回
                state.RememberProfile(Mac, "P9E6-Remembered");
                applier.Calls.Clear();

                selector2.ApplyOnConnect(Slot, false, true, Mac);   // 再接続
                Assert.Equal("apply:" + Slot + ":P9E6-Remembered:" + ProfileChangeSource.ControlService, applier.Calls[0]);
            }
            finally
            {
                repository.OlderProfilePath[Slot] = oldOlder;
                repository.SelectedProfile[Slot] = oldSelected;
            }
        }

        [Fact]
        public void WithoutAValidSerial_TheMacAddressIsIgnored_AndTheFirstConnectionIsPerSlot()
        {
            var applier = new RecordingApplier();
            var selector = NewSelector(applier, out var repository);
            string oldOlder = repository.OlderProfilePath[Slot];
            string oldSelected = repository.SelectedProfile[Slot];
            try
            {
                repository.OlderProfilePath[Slot] = "P9E6-NoSerial";

                selector.ApplyOnConnect(Slot, false, false, "ignored");

                Assert.Equal("apply:" + Slot + ":P9E6-NoSerial:" + ProfileChangeSource.ControlService, applier.Calls[0]);
            }
            finally
            {
                repository.OlderProfilePath[Slot] = oldOlder;
                repository.SelectedProfile[Slot] = oldSelected;
            }
        }

        [Fact]
        public void ReturnsWhatTheApplierReturns()
        {
            var applier = new RecordingApplier { Result = false };
            var selector = NewSelector(applier, out _);

            Assert.False(selector.ApplyOnConnect(Slot, false, false, null));
        }
    }
}
