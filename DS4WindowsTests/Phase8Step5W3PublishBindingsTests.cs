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
    /// Phase8-Step5 W3（Phase8-Step5-Plan.md §3.3・§4）: プロファイルの適用 → 束縛の組み立て → 登録簿への差し替えの結合を確かめる。
    /// 適用サービスには偽の登録簿・偽のビルダーを渡して呼び出しを数え、実ホストでは、移行対象が空／一部のときの登録簿の中身を確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step5W3PublishBindingsTests : IDisposable
    {
        private const int Slot = 0;
        private const string Normal = "Phase8Step5_Normal";
        private const string Missing = "Phase8Step5_Missing";
        private const string ActionName = "Phase8Step5_BtnAction";

        private sealed class RecordingRegistry : IActionRegistry
        {
            public readonly List<(int Device, IReadOnlyList<IActionBinding> Bindings)> Replaced = new List<(int, IReadOnlyList<IActionBinding>)>();
            public readonly List<int> Cleared = new List<int>();
            public IReadOnlyList<IActionBinding> GetBindings(int device) => Array.Empty<IActionBinding>();
            public void Replace(int device, IReadOnlyList<IActionBinding> bindings) => Replaced.Add((device, bindings));
            public void Clear(int device) => Cleared.Add(device);
        }

        private sealed class FakeBuilder : IActionBindingBuilder
        {
            public readonly List<int> Calls = new List<int>();
            public bool Throw { get; set; }
            public readonly IReadOnlyList<IActionBinding> Result = new IActionBinding[0];

            public IReadOnlyList<IActionBinding> Build(int device)
            {
                Calls.Add(device);
                if (Throw) throw new InvalidOperationException("builder failure");
                return Result;
            }
        }

        private readonly string _dir;
        private readonly string _originalAppData;
        private readonly ControlService _control;
        private readonly IProfileSettingsService _settings;
        private readonly ProfileRepository _repo;
        private readonly BackingStore _store;
        private readonly string _origProfilePath;
        private readonly string _origOlder;
        private readonly string _origSelected;
        private readonly List<string> _origActions;
        private readonly SpecialAction _action;
        private readonly BindingMigrationOptions _options;
        private readonly SpecialAction.ActionTypeId[] _origMigrated;

        public Phase8Step5W3PublishBindingsTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase8Step5_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _originalAppData = TestGlobals.AppDataPath;
            TestGlobals.AppDataPath = _dir;

            DS4WinWPF.AppHost.CreateHost();
            _control = TestHost.GetService<ControlService>();
            _settings = TestGlobals.ProfileSettingsServiceInstance;
            _store = TestGlobals.Store;
            _repo = TestRepositoryFactory.CreateProfileRepository(_settings);
            _options = TestHost.GetService<BindingMigrationOptions>();
            _origMigrated = _options.MigratedTypes.ToArray();
            _options.MigratedTypes.Clear();

            _origProfilePath = _repo.ProfilePath[Slot];
            _origOlder = _repo.OlderProfilePath[Slot];
            _origSelected = _repo.SelectedProfile[Slot];

            // ボタン種別の特殊アクションを 1 件定義し、それを使うプロファイルを保存の経路で作る
            _action = new SpecialAction(ActionName, "Cross/L1", "Button", ((int)X360Controls.A).ToString(), 0, "Press\n");
            _store.actions.Add(_action);
            _origActions = _store.profileActions[Slot];
            _store.profileActions[Slot] = new List<string> { ActionName };
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml().SaveProfile(Slot, Normal));
        }

        public void Dispose()
        {
            _options.MigratedTypes.Clear();
            foreach (var t in _origMigrated) _options.MigratedTypes.Add(t);
            TestHost.GetService<IActionRegistry>().Clear(Slot);
            _store.profileActions[Slot] = _origActions;
            _store.actions.Remove(_action);
            _repo.ProfilePath[Slot] = _origProfilePath;
            _repo.OlderProfilePath[Slot] = _origOlder;
            _repo.SelectedProfile[Slot] = _origSelected;
            TestGlobals.AppDataPath = _originalAppData;
            try { Directory.Delete(_dir, true); } catch { }
        }

        private ProfileApplicationService Service(IActionRegistry registry = null, IActionBindingBuilder builder = null)
            => TestRepositoryFactory.CreateProfileApplicationService(_settings, () => _control, _repo, new EnvironmentService(),
                actionRegistry: registry, bindingBuilderProvider: builder == null ? (Func<IActionBindingBuilder>)null : () => builder);

        // ---- 偽の登録簿・ビルダーで、呼び出しを数える ----

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Apply_Normal_ReplacesOnceWithTheBuilderResult(bool withHalt)
        {
            var registry = new RecordingRegistry();
            var builder = new FakeBuilder();
            ProfileApplicationService service = Service(registry, builder);

            bool result = withHalt
                ? service.ApplyProfile(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog")
                : service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog");

            Assert.True(result);
            Assert.Equal(new[] { Slot }, builder.Calls);
            var call = Assert.Single(registry.Replaced);
            Assert.Equal(Slot, call.Device);
            Assert.Same(builder.Result, call.Bindings);
            Assert.Empty(registry.Cleared);
        }

        [Fact]
        public void Apply_MissingProfile_DoesNotPublish()
        {
            var registry = new RecordingRegistry();
            var builder = new FakeBuilder();

            bool result = Service(registry, builder).ApplyProfileWithoutHalt(Slot, Missing, false, false, ProfileChangeSource.Manual, "prolog");

            Assert.False(result);
            Assert.Empty(builder.Calls);
            Assert.Empty(registry.Replaced);
        }

        [Fact]
        public void EveryApply_BuildsAgain_AndReplacesTheList()
        {
            var registry = new RecordingRegistry();
            var builder = new FakeBuilder();
            ProfileApplicationService service = Service(registry, builder);

            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "p"));
            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "p"));

            Assert.Equal(2, registry.Replaced.Count);
            Assert.Equal(2, builder.Calls.Count);
        }

        [Fact]
        public void PublishBindings_OutOfRangeDevices_DoNothing()
        {
            var registry = new RecordingRegistry();
            var builder = new FakeBuilder();
            ProfileApplicationService service = Service(registry, builder);
            int limit = new EnvironmentService().ControllerSlotLimit;

            foreach (int device in new[] { -1, limit, ControllerLimits.TestProfileIndex, 1000 })
                service.PublishBindings(device);

            Assert.Empty(builder.Calls);
            Assert.Empty(registry.Replaced);
            Assert.Empty(registry.Cleared);

            service.PublishBindings(limit - 1);   // 範囲の内側の最後のスロットは受け入れる
            Assert.Equal(new[] { limit - 1 }, builder.Calls);
        }

        [Fact]
        public void BuilderFailure_ClearsTheDevice_AndDoesNotFailTheApply()
        {
            var registry = new RecordingRegistry();
            var builder = new FakeBuilder { Throw = true };
            ProfileApplicationService service = Service(registry, builder);

            bool result = service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog");

            Assert.True(result);   // Q5＝A: 組み立ての失敗で、適用を失敗にしない
            Assert.Empty(registry.Replaced);
            Assert.Equal(new[] { Slot }, registry.Cleared);
            Assert.Equal(Normal, _repo.SelectedProfile[Slot]);
        }

        // ---- 実ホストの登録簿・ビルダー ----

        [Fact]
        public void RealHost_NothingMigrated_TheRegistryStaysEmpty()
        {
            var registry = TestHost.GetService<IActionRegistry>();
            ProfileApplicationService service = Service();

            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog"));

            Assert.Empty(registry.GetBindings(Slot));
        }

        [Fact]
        public void RealHost_ButtonMigrated_TheRegistryHoldsTheBindingOfTheProfile()
        {
            var registry = TestHost.GetService<IActionRegistry>();
            _options.MigratedTypes.Add(SpecialAction.ActionTypeId.Button);
            ProfileApplicationService service = Service();

            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog"));

            IReadOnlyList<IActionBinding> bindings = registry.GetBindings(Slot);
            var binding = Assert.IsType<TriggerBinding>(Assert.Single(bindings));
            Assert.Equal(ActionName, binding.Source.name);
            Assert.IsType<ButtonOutputAction>(Assert.Single(binding.Outputs));
            Assert.Equal(new[] { DS4Controls.Cross, DS4Controls.L1 }, ((ComboTriggerInputAction)binding.Input).Triggers.ToArray());

            // 適用のたびに新しいリストへ差し替わる。古い参照は壊れない
            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog"));
            IReadOnlyList<IActionBinding> again = registry.GetBindings(Slot);
            Assert.NotSame(bindings, again);
            Assert.Single(bindings);
            Assert.Single(again);

            // 移行対象から外して適用し直すと、空に戻る（ロールバック）
            _options.MigratedTypes.Clear();
            Assert.True(service.ApplyProfileWithoutHalt(Slot, Normal, false, false, ProfileChangeSource.Manual, "prolog"));
            Assert.Empty(registry.GetBindings(Slot));
        }

        // ---- 構造 ----

        [Fact]
        public void TheHost_ResolvesTheBuilderAndTheApplicationService_WithoutACycle()
        {
            Assert.NotNull(TestHost.GetService<IActionBindingBuilder>());
            Assert.Same(TestHost.GetService<IActionBindingBuilder>(), TestHost.GetService<IActionBindingBuilder>());
            Assert.NotNull(TestHost.GetService<IProfileApplicationService>());
            Assert.NotNull(TestHost.GetService<Func<IActionBindingBuilder>>());
        }

        [Fact]
        public void TheSource_PublishesAfterTheCleanup_AndThePipePathPublishesAfterALoadSuccess()
        {
            string service = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ProfileApplicationService.cs");
            string window = SourceFileLocator.Find("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (service == null || window == null) return;

            string text = File.ReadAllText(service);
            int complete = text.IndexOf("private void CompleteProfileApplication(", StringComparison.Ordinal);
            int prealloc = text.IndexOf("PreallocateForProfileApply(device)", complete, StringComparison.Ordinal);
            int publish = text.IndexOf("PublishBindings(device);", complete, StringComparison.Ordinal);
            Assert.True(complete > 0 && prealloc > complete && publish > prealloc, "束縛の差し替えは、掃除（PreallocateForProfileApply）の後に行う");

            string main = File.ReadAllText(window);
            Assert.Matches(@"if \(profileRepo\.LoadTempProfile\(tdevice, strData\[2\], true, controlService\)\)\s*profileAppService\.PublishBindings\(tdevice\);", main);
        }
    }
}
