using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-4: <c>ProfileSettingsViewModel</c>・<c>PresetMenuHelper</c>・<c>ViewModelFactory</c> の
    /// Global 直接参照・Service Locator 解消の回帰ガード（<c>Phase6-Step9-Plan.md</c> §0A.2 表D、決定1＝S9-A、決定2＝C1）。
    /// - 非定数の <c>Global.</c> 参照、<c>AppHost</c>、<c>Program.rootHub</c> が再混入していないこと。
    /// - コンストラクタの依存が必須であること（<c>ControlService</c> のみ null 許容）。
    /// - 編集スロットと実機を分ける <c>targetDevice</c> の引数が維持されていること。
    /// ソースファイルが見つからない環境では、ソース走査を行わず合格とする。
    /// </summary>
    public class ProfileSettingsViewModelGlobalReferenceGuardTests
    {

        private readonly ITestOutputHelper _output;

        // ProfileSettingsViewModel のコンストラクタは pack://application URI（Uri／BitmapImage）を生成する。
        // pack スキームは System.Windows.Application の生成で登録されるため、ほかのテストクラスが先に Application を
        // 生成したかどうか（実行順）によって、このクラスの ViewModelFactory 経由のテストが
        // UriFormatException（Invalid port specified）で失敗していた。ほかの ViewModel テストと同じ準備を明示する。
        static ProfileSettingsViewModelGlobalReferenceGuardTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (System.Windows.Application.Current == null)
            {
                try
                {
                    _ = new System.Windows.Application();
                }
                catch { }
            }

            if (System.Windows.Application.ResourceAssembly == null)
            {
                try
                {
                    System.Windows.Application.ResourceAssembly = typeof(DS4Windows.Composition.AppHost).Assembly;
                }
                catch { }
            }
        }

        public ProfileSettingsViewModelGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructors_HaveNoOptionalServiceArguments_AndKeepTargetDevice()
        {
            // Phase14d-Step3: ソースの文字列ではなく、コンストラクターの引数を確かめる
            System.Reflection.ParameterInfo[] ps = typeof(DS4Windows.UI.ViewModels.ProfileSettingsViewModel).GetConstructors().Single().GetParameters();
            Assert.Contains(ps, p => p.ParameterType == typeof(DS4Windows.Core.Abstractions.IEnvironmentService));
            Assert.Contains(ps, p => p.ParameterType == typeof(DS4Windows.Core.Abstractions.IPathService));
            // targetDevice 以外は省略可能にしない（既定値の指定は 1 つだけ）
            System.Reflection.ParameterInfo optional = Assert.Single(ps.Where(p => p.IsOptional));
            Assert.Equal("targetDevice", optional.Name);
            Assert.Equal(-1, optional.DefaultValue);
            // PresetMenuHelper は Phase6-Step10b W3 で別ファイルへ分離した
            Assert.All(typeof(DS4Windows.UI.ViewModels.PresetMenuHelper).GetConstructors().Single().GetParameters(), p => Assert.False(p.IsOptional, p.Name + " が省略可能"));
        }

        [Fact]
        public void ViewModelFactory_HasNoServiceLocatorInConstructor()
        {
            // Phase14d-Step3: ソースの文字列ではなく、コンストラクターの引数を確かめる（AppHost を使わないことは RemovedApiRuleTests）
            // 窓口（ViewModelFactory）は用途別の 3 つのファクトリーへ委譲するため、4 つ全部のコンストラクターを合わせて確かめる
            System.Reflection.ParameterInfo[] ps = new[]
            {
                typeof(DS4Windows.UI.Services.ViewModelFactory),
                typeof(DS4Windows.UI.Services.ShellViewModelFactory),
                typeof(DS4Windows.UI.Services.ProfileEditViewModelFactory),
                typeof(DS4Windows.UI.Services.ActionViewModelFactory),
            }.SelectMany(t => t.GetConstructors().Single().GetParameters()).ToArray();
            Assert.DoesNotContain(ps, p => p.IsOptional);
            // 遅延（Func）で受けず、第 2 層の契約（マクロ記録の窓口）を直接受ける。ControlService は受けない
            Assert.Contains(ps, p => p.ParameterType == typeof(DS4Windows.Processing.Abstractions.IMacroRecordingState));
        }

        [Fact]
        public void Constructor_RequiresServices_ExceptControlService()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var outputSlots = TestRepositoryFactory.CreateOutputSlotService();
            var repo = TestRepositoryFactory.CreateProfileRepository();
            var loader = TestRepositoryFactory.CreateProfilePresetLoader();
            var env = new EnvironmentService();
            var path = new PathService();

            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, null, null, outputSlots, repo, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, settings, null, null, repo, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, settings, null, outputSlots, null, loader, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, settings, null, outputSlots, repo, null, env, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, settings, null, outputSlots, repo, loader, null, path));
            Assert.Throws<ArgumentNullException>(() =>
                new ProfileSettingsViewModel(TestLightbar.State, 0, settings, null, outputSlots, repo, loader, env, null));
            Assert.Throws<ArgumentNullException>(() => new PresetMenuHelper(0, null));
        }

        [Fact]
        public void ViewModelFactory_ResolvedFromContainer_CreatesViewModelWithEnvironmentService()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            ProfileSettingsViewModel vm = factory.CreateProfileSettingsViewModel(0);

            Assert.NotNull(vm);
            Assert.Equal(TestHost.GetService<IEnvironmentService>().IsUsingMinViGEm117333(),
                vm.UsingMinViGEm173333);
        }
    }
}
