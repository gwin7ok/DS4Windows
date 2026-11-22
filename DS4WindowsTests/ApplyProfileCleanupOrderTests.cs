#nullable enable
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services.Slots;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイル適用後の掃除の順序を、呼び出しの記録で確かめる（<see cref="ProfileApplicationService"/>）。
    /// ClearAllEntries → ClearForDevice → ClearDeviceState → 事前確保（GetStateFor）→ 束縛の差し替え（Replace）。
    /// 順序を入れ替えると、アクションの実体の再作成や、復帰の合図の退避・復元が壊れる。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ApplyProfileCleanupOrderTests
    {
        private const int Slot = 3;

        private static (ProfileApplicationService Service, List<string> Order, IProfileRepository Repository) Create(bool loadResult = true)
        {
            var order = new List<string>();
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings);

            var xmlStore = RecordingProxy.Create<IProfileXmlStore>();
            RecordingProxy.Of(xmlStore).Returns["LoadProfileXml"] = loadResult;

            var actions = RecordingProxy.Create<IProfileActionProvider>();
            var actionsProxy = RecordingProxy.Of(actions);
            actionsProxy.Returns["GetProfileActionNames"] = new List<string> { "SA1" };
            actionsProxy.Returns["GetProfileAction"] = new SpecialAction("SA1", "A", "None", "x");

            var manager = RecordingProxy.Create<IManagedActionManager>();
            RecordingProxy.Of(manager).OnCall = name => order.Add("manager." + name);
            var keyButtons = RecordingProxy.Create<IKeyButtonControllerRegistry>();
            RecordingProxy.Of(keyButtons).OnCall = name => order.Add("keyButtons." + name);
            var registry = RecordingProxy.Create<IActionRegistry>();
            RecordingProxy.Of(registry).OnCall = name => order.Add("registry." + name);

            var service = new ProfileApplicationService(settings, RecordingProxy.Create<IProfileLoadTarget>(), repository,
                new EnvironmentService(), xmlStore, actions, TestGlobals.Store, keyButtons, registry,
                () => RecordingProxy.Create<IActionBindingBuilder>(), manager);
            return (service, order, repository);
        }

        [Fact]
        public void TheCleanup_RunsInTheFixedOrder_AndPublishesTheBindingsLast()
        {
            var (service, order, _) = Create();

            Assert.True(service.ApplyProfileWithoutHalt(Slot, "P"));

            string[] expected =
            {
                "manager.ClearAllEntries", "keyButtons.ClearForDevice", "manager.ClearDeviceState",
                "manager.GetStateFor", "keyButtons.GetOrCreateForAction", "registry.Replace",
            };
            Assert.Equal(expected, order.FindAll(o => System.Array.IndexOf(expected, o) >= 0));
        }

        [Fact]
        public void AFailedLoad_DoesNotCleanUpAnything()
        {
            var (service, order, _) = Create(loadResult: false);

            Assert.False(service.ApplyProfileWithoutHalt(Slot, "P"));

            Assert.Empty(order);
        }

        [Fact]
        public void TheAppliedName_IsRecorded_BeforeTheCleanup()
        {
            var (service, _, repository) = Create();
            string? before = repository.SelectedProfile[Slot];
            string? beforeOlder = repository.OlderProfilePath[Slot];
            try
            {
                service.ApplyProfileWithoutHalt(Slot, "P-Applied");

                Assert.Equal("P-Applied", repository.SelectedProfile[Slot]);
                Assert.Equal("P-Applied", repository.OlderProfilePath[Slot]);   // 一時プロファイルでなければ、直前のプロファイルも更新する
            }
            finally
            {
                repository.SelectedProfile[Slot] = before;
                repository.OlderProfilePath[Slot] = beforeOlder;
            }
        }
    }
}
