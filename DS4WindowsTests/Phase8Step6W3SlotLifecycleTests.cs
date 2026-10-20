using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step6 W3（Phase8-Step6-Plan.md §3.3・§4）: 切断（<c>ReleaseSlot</c>）と接続（<c>PublishSlot</c>）の口を確かめる。
    /// 偽のエンジンで呼び出しを数え、実ホストで、登録簿とエンジンの状態が消えること、スロットのプロファイル選択が不変であること、再接続で束縛が戻ることを確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step6W3SlotLifecycleTests : IDisposable
    {
        private const int Slot = 0;
        private const string ActionName = "Phase8Step6_BtnAction";

        private sealed class RecordingEngine : ITriggerDispatchEngine
        {
            public readonly List<int> Disconnected = new List<int>();
            public bool Throw { get; set; }

            public void OnDeviceDisconnected(int device)
            {
                Disconnected.Add(device);
                if (Throw) throw new InvalidOperationException("engine failure");
            }

            public bool Owns(int device, SpecialAction action) => false;
            public bool OwnsControl(int device, DS4Controls control) => false;
            public void ProcessControl(int device, DS4Controls control, ITriggerInput input, IFrameOutput frame) { }
            public FrameResult ProcessOwned(int device, SpecialAction action, ITriggerInput input, IFrameOutput frame) => FrameResult.Continue;
            public void ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame) { }
            public void ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame) { }
            public bool IsSuppressed(int device, DS4Controls control) => false;
            public void AddSuppressed(int device, IReadOnlyList<DS4Controls> controls) { }
            public void ReleaseSuppressed(int device, ITriggerInput input) { }
            public void OnProfileApplied(int device) { }
            public void ProcessUntrigger(int device, ITriggerInput input) { }
        }

        private readonly BackingStore _store = TestGlobals.Store;
        private readonly List<string> _origNames;
        private readonly Dictionary<string, SpecialAction> _origDict;
        private readonly Dictionary<string, int> _origIndexDict;
        private readonly SpecialAction _action;
        private readonly string _origOlder, _origSelected, _origPath;

        public Phase8Step6W3SlotLifecycleTests()
        {
            _origNames = _store.profileActions[Slot];
            _origDict = new Dictionary<string, SpecialAction>(_store.profileActionDict[Slot]);
            _origIndexDict = new Dictionary<string, int>(_store.profileActionIndexDict[Slot]);
            _origOlder = _store.olderProfilePath[Slot];
            _origSelected = _store.selectedProfile[Slot];
            _origPath = _store.profilePath[Slot];

            // ボタン種別の特殊アクションを 1 件、スロットのプロファイルに入れる（プロファイルの読込を通さず、設定の状態を直接作る）
            _action = new SpecialAction(ActionName, "Cross/L1", "Button", ((int)X360Controls.A).ToString(), 0, "Press\n");
            _store.profileActions[Slot] = new List<string> { ActionName };
            _store.profileActionDict[Slot][ActionName] = _action;
            _store.profileActionIndexDict[Slot][ActionName] = 0;
            _store.profileActionCount[Slot] = 1;
        }

        public void Dispose()
        {
            TestHost.GetService<IActionRegistry>().Clear(Slot);
            TestHost.GetService<ITriggerDispatchEngine>().OnDeviceDisconnected(Slot);
            _store.profileActions[Slot] = _origNames;
            _store.profileActionDict[Slot].Clear();
            foreach (var kv in _origDict) _store.profileActionDict[Slot][kv.Key] = kv.Value;
            _store.profileActionIndexDict[Slot].Clear();
            foreach (var kv in _origIndexDict) _store.profileActionIndexDict[Slot][kv.Key] = kv.Value;
            _store.profileActionCount[Slot] = _origNames.Count;
            _store.olderProfilePath[Slot] = _origOlder;
            _store.selectedProfile[Slot] = _origSelected;
            _store.profilePath[Slot] = _origPath;
        }

        // ---- 偽のエンジン ----

        [Fact]
        public void ReleaseSlot_CallsTheEngineOnlyForSlotsInRange()
        {
            var engine = new RecordingEngine();
            ProfileSlotApplier applier = TestRepositoryFactory.CreateProfileSlotApplier(engine: engine);
            int limit = new EnvironmentService().ControllerSlotLimit;

            foreach (int slot in new[] { -1, limit, ControllerLimits.TestProfileIndex, 1000 })
                applier.ReleaseSlot(slot);
            Assert.Empty(engine.Disconnected);

            applier.ReleaseSlot(limit - 1);
            applier.ReleaseSlot(0);
            Assert.Equal(new[] { limit - 1, 0 }, engine.Disconnected);
        }

        [Fact]
        public void ReleaseSlot_WhenTheEngineThrows_DoesNotStopTheDisconnect()
        {
            var engine = new RecordingEngine { Throw = true };
            ProfileSlotApplier applier = TestRepositoryFactory.CreateProfileSlotApplier(engine: engine);

            applier.ReleaseSlot(Slot);   // 例外を投げない

            Assert.Equal(new[] { Slot }, engine.Disconnected);
        }

        [Fact]
        public void PublishSlot_AsksTheApplicationServiceOnlyForSlotsInRange()
        {
            var service = new RecordingProfileApplicationService();
            ProfileSlotApplier applier = TestRepositoryFactory.CreateProfileSlotApplier(service);
            int limit = new EnvironmentService().ControllerSlotLimit;

            foreach (int slot in new[] { -1, limit, ControllerLimits.TestProfileIndex })
                applier.PublishSlot(slot);
            Assert.Empty(service.PublishBindingsCalls);

            applier.PublishSlot(Slot);
            Assert.Equal(new[] { Slot }, service.PublishBindingsCalls);
        }

        // ---- 実ホスト ----

        [Fact]
        public void RealHost_ReleaseSlot_ClearsTheRegistryAndTheEngineState_ButNotTheProfileSelection()
        {
            var registry = TestHost.GetService<IActionRegistry>();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();
            ProfileSlotApplier applier = TestRepositoryFactory.CreateProfileSlotApplier();

            applier.PublishSlot(Slot);
            Assert.Single(registry.GetBindings(Slot));
            engine.AddSuppressed(Slot, new[] { DS4Controls.Cross });
            Assert.True(engine.IsSuppressed(Slot, DS4Controls.Cross));

            _store.olderProfilePath[Slot] = "OlderSentinel";
            _store.selectedProfile[Slot] = "SelectedSentinel";
            _store.profilePath[Slot] = "PathSentinel";

            applier.ReleaseSlot(Slot);

            Assert.Empty(registry.GetBindings(Slot));
            Assert.False(engine.IsSuppressed(Slot, DS4Controls.Cross));
            Assert.Equal("OlderSentinel", _store.olderProfilePath[Slot]);   // 切断で「通常のプロファイル」を消さない（Q3＝A）
            Assert.Equal("SelectedSentinel", _store.selectedProfile[Slot]);
            Assert.Equal("PathSentinel", _store.profilePath[Slot]);
        }

        [Fact]
        public void RealHost_AReconnectionWithoutAnApply_GetsTheBindingsBack()
        {
            var registry = TestHost.GetService<IActionRegistry>();
            ProfileSlotApplier applier = TestRepositoryFactory.CreateProfileSlotApplier();

            applier.PublishSlot(Slot);
            var before = Assert.IsType<TriggerBinding>(Assert.Single(registry.GetBindings(Slot)));

            applier.ReleaseSlot(Slot);             // 切断
            Assert.Empty(registry.GetBindings(Slot));

            applier.PublishSlot(Slot);             // 再接続（一時プロファイル／AutoProfile のため適用しない）
            var after = Assert.IsType<TriggerBinding>(Assert.Single(registry.GetBindings(Slot)));
            Assert.Same(before.Source, after.Source);
            Assert.NotSame(before, after);
        }

        [Fact]
        public void TheHost_ResolvesTheSlotApplierWithTheEngine()
        {
            Assert.IsType<ProfileSlotApplier>(TestHost.GetService<IProfileSlotApplier>());
            Assert.NotNull(TestHost.GetService<ControlService>());
        }

        // ---- 構造 ----

        [Fact]
        public void TheSource_ReleasesTheSlotOnlyOnThePhysicalRemoval_AndPublishesOnAReconnectionWithoutAnApply()
        {
            string controlService = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (controlService == null || mapping == null) return;

            string text = File.ReadAllText(controlService);
            string removal = MethodBody(text, "protected void On_DS4Removal(");
            int handle = removal.IndexOf("_mapping.HandleDeviceDisconnect(ind);", StringComparison.Ordinal);
            int release = removal.IndexOf("_profileSlotApplier.ReleaseSlot(ind);", StringComparison.Ordinal);
            Assert.True(handle >= 0 && release > handle, "ReleaseSlot は HandleDeviceDisconnect の後に呼ぶ");
            Assert.DoesNotContain("ReleaseSlot", MethodBody(text, "public bool Stop("));

            string connect = MethodBody(text, "private void PrepareConnectedInputControllerSettingEvents(");
            int apply = connect.IndexOf("_profileSlotApplier.ApplyToSlot(index, profileToApply", StringComparison.Ordinal);
            int publish = connect.IndexOf("_profileSlotApplier.PublishSlot(index);", StringComparison.Ordinal);
            Assert.True(apply >= 0 && publish > apply, "適用しない経路（else）で PublishSlot を呼ぶ");

            // 適用のたびにも呼ばれる HandleDeviceDisconnect には、エンジンの切断を足していない
            string handler = MethodBody(File.ReadAllText(mapping), "public void HandleDeviceDisconnect(");
            Assert.DoesNotContain("OnDeviceDisconnected", handler);
            Assert.DoesNotContain("_engine", handler);
        }

        private static string MethodBody(string text, string signature)
        {
            int start = text.IndexOf(signature, StringComparison.Ordinal);
            Assert.True(start >= 0, signature + " が見つからない");
            int open = text.IndexOf('{', start);
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text.Substring(start, i - start + 1);
            }

            throw new InvalidOperationException(signature + " の終わりが見つからない");
        }
    }
}
