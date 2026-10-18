using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7b W1（Phase7b-Plan.md §2 項目 1、Q1＝A、Q2＝B）: 「Unload on trigger release」で元のプロファイルに戻らない不具合の回帰ガード。
    /// 原因: プロファイル切替の適用が <c>ActionManager.ClearDeviceState</c> を呼び、内部の <c>Mapping.HandleDeviceDisconnect</c> が
    /// <c>deviceRuntime[device]</c> を作り直して、復帰の合図（<c>UntriggerAction</c>）も消していた。
    /// 是正: <c>ClearDeviceState</c> は合図を退避して戻す。実際の切断（<c>Mapping.HandleDeviceDisconnect</c> の直接呼び出し）では消える。
    /// 専用のデバイス添字（7）を使い、終了時に状態を戻す。
    /// </summary>
    public class Phase7bW1ProfileUntriggerPreservationTests
    {
        private const int Device = 7;

        private static Mapping GetMapping()
        {
            var mapping = TestHost.GetService<IMapping>() as Mapping;
            Assert.NotNull(mapping);
            return mapping;
        }

        private static SpecialAction NewProfileAction(string name)
        {
            var action = new SpecialAction(name, "Cross", "Profile", "TestProfile", 0, "AutomaticUntrigger");
            Assert.True(action.automaticUntrigger);
            return action;
        }

        [Fact]
        public void CaptureAndRestore_RoundTripsTheUntriggerAction()
        {
            var mapping = GetMapping();
            var action = NewProfileAction("p7b-roundtrip");
            try
            {
                mapping.deviceRuntime[Device].UntriggerAction = action;
                mapping.deviceRuntime[Device].UntriggerIndex = 3;

                var snapshot = mapping.CaptureProfileUntrigger(Device);
                Assert.Same(action, snapshot.Action);
                Assert.Equal(3, snapshot.Index);

                mapping.HandleDeviceDisconnect(Device);
                Assert.Null(mapping.deviceRuntime[Device].UntriggerAction);

                mapping.RestoreProfileUntrigger(Device, snapshot);
                Assert.Same(action, mapping.deviceRuntime[Device].UntriggerAction);
                Assert.Equal(3, mapping.deviceRuntime[Device].UntriggerIndex);
            }
            finally
            {
                mapping.deviceRuntime[Device].UntriggerAction = null;
                mapping.deviceRuntime[Device].UntriggerIndex = -1;
            }
        }

        [Fact]
        public void Restore_WithEmptySnapshot_DoesNotOverwriteAnExistingAction()
        {
            var mapping = GetMapping();
            var existing = NewProfileAction("p7b-existing");
            try
            {
                mapping.deviceRuntime[Device].UntriggerAction = existing;
                mapping.deviceRuntime[Device].UntriggerIndex = 5;

                mapping.RestoreProfileUntrigger(Device, default);

                Assert.Same(existing, mapping.deviceRuntime[Device].UntriggerAction);
                Assert.Equal(5, mapping.deviceRuntime[Device].UntriggerIndex);
            }
            finally
            {
                mapping.deviceRuntime[Device].UntriggerAction = null;
                mapping.deviceRuntime[Device].UntriggerIndex = -1;
            }
        }

        [Fact]
        public void CaptureAndRestore_IgnoreOutOfRangeDevices()
        {
            var mapping = GetMapping();
            Assert.Null(mapping.CaptureProfileUntrigger(-1).Action);
            Assert.Null(mapping.CaptureProfileUntrigger(int.MaxValue).Action);
            mapping.RestoreProfileUntrigger(-1, new ProfileUntriggerSnapshot(NewProfileAction("p7b-range"), 0));
            mapping.RestoreProfileUntrigger(int.MaxValue, new ProfileUntriggerSnapshot(NewProfileAction("p7b-range"), 0));
        }

        [Fact]
        public void DefaultActionManager_ClearDeviceState_KeepsTheUntriggerAction()
        {
            var mapping = GetMapping();
            var manager = new DefaultActionManager(TestRepositoryFactory.CreateDefaultActionFactory(), TestRepositoryFactory.MappingProvider);
            var action = NewProfileAction("p7b-clear");
            try
            {
                mapping.deviceRuntime[Device].UntriggerAction = action;
                mapping.deviceRuntime[Device].UntriggerIndex = 2;

                manager.ClearDeviceState(Device);

                Assert.Same(action, mapping.deviceRuntime[Device].UntriggerAction);
                Assert.Equal(2, mapping.deviceRuntime[Device].UntriggerIndex);
            }
            finally
            {
                mapping.deviceRuntime[Device].UntriggerAction = null;
                mapping.deviceRuntime[Device].UntriggerIndex = -1;
            }
        }

        [Fact]
        public void ActualDisconnect_StillClearsTheUntriggerAction()
        {
            var mapping = GetMapping();
            var action = NewProfileAction("p7b-disconnect");
            try
            {
                mapping.deviceRuntime[Device].UntriggerAction = action;
                mapping.deviceRuntime[Device].UntriggerIndex = 1;

                mapping.HandleDeviceDisconnect(Device);

                Assert.Null(mapping.deviceRuntime[Device].UntriggerAction);
                Assert.Equal(-1, mapping.deviceRuntime[Device].UntriggerIndex);
            }
            finally
            {
                mapping.deviceRuntime[Device].UntriggerAction = null;
                mapping.deviceRuntime[Device].UntriggerIndex = -1;
            }
        }
    }
}
