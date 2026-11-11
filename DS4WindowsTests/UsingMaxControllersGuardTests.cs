using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10b W4（Q4＝A・Q6＝A）: <c>ControlService.USING_MAX_CONTROLLERS</c>（静的シム）を削除し、
    /// <c>IEnvironmentService.UsingMaxControllers</c> の注入に切り替えたことと、孤立定数の整理の回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class UsingMaxControllersGuardTests
    {
        private readonly ITestOutputHelper _output;

        public UsingMaxControllersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheStaticShims_AreGone()
        {
            Assert.Null(typeof(DS4Windows.Processing.Services.RuntimeBootstrapper).GetProperty("USING_MAX_CONTROLLERS", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            // Phase7-Step10e W2: CURRENT_DS4_CONTROLLER_LIMIT も削除した
            Assert.Null(typeof(DS4Windows.Processing.Services.RuntimeBootstrapper).GetProperty("CURRENT_DS4_CONTROLLER_LIMIT", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Fact]
        public void TheHolder_FollowsTheEnvironment()
        {
            var env = new EnvironmentService();
            var holder = new AutoProfileHolder(new PathService(), env);
            Assert.Equal(env.UsingMaxControllers, holder.UsingMaxControllers);
            Assert.Throws<System.ArgumentNullException>(() => new AutoProfileHolder(new PathService(), null));
        }

        [Fact]
        public void TheDto_WritesControllers4To8_OnlyWhenTheEnvironmentUsesMaxControllers()
        {
            var env = new EnvironmentService();
            var holder = new AutoProfileHolder(new PathService(), env);
            holder.AutoProfileColl.Clear();
            var entity = new AutoProfileEntity("C:\\test\\a.exe", "t");
            for (int i = 0; i < entity.ProfileNames.Length; i++) entity.ProfileNames[i] = "P" + (i + 1);
            holder.AutoProfileColl.Add(entity);

            var dto = new AutoProfilesDTO();
            dto.MapFrom(holder);

            var written = dto.programEntries.Single();
            Assert.Equal("P1", written.Controller1);
            Assert.Equal("P3", written.Controller3);
            if (env.UsingMaxControllers)
            {
                Assert.Equal("P4", written.Controller4);
                Assert.Equal("P8", written.Controller8);
            }
            else
            {
                Assert.Equal(string.Empty, written.Controller8);
            }
        }

        [Fact]
        public void TheViewModel_FollowsTheEnvironment()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var factory = TestHost.GetService<IShellViewModelFactory>();
            var env = TestHost.GetService<IEnvironmentService>();
            var vm = factory.CreateAutoProfilesViewModel(TestRepositoryFactory.CreateAutoProfileHolder(),
                new ProfileList(TestHost.GetService<IPathService>()));
            Assert.Equal(env.UsingMaxControllers, vm.UsingExpandedControllers);
        }

        [Fact]
        public void ProgramItem_ShowsTheExpandedControllerRows_OnlyWhenTheCreatorSaysSo()
        {
            // 存在しないパスなら、アイコンの読み込みは行われない
            var expanded = new ProgramItem(@"C:\no\such\file.exe", true);
            var normal = new ProgramItem(@"C:\no\such\file.exe", false);
            Assert.Equal(System.Windows.Visibility.Visible, expanded.ExpandedControllersVisible);
            Assert.Equal(System.Windows.Visibility.Collapsed, normal.ExpandedControllersVisible);
        }

        [Fact]
        public void TheOrphanCurveConstants_AreGone_AndTheCheckEveryDefaultsAreConnected()
        {
            Assert.Equal(1, BackingStore.DEFAULT_CHECK_EVERY_VALUE);
            Assert.Equal(1, BackingStore.DEFAULT_CHECK_EVERY_UNIT);
            var fresh = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            Assert.Equal(BackingStore.DEFAULT_CHECK_EVERY_VALUE, fresh.checkEveryValue);
            Assert.Equal(BackingStore.DEFAULT_CHECK_EVERY_UNIT, fresh.checkEveryUnit);

            foreach (string name in new[] { "DEFAULT_STICK_OUTPUT_CURVE", "DEFAULT_STICK_OUTPUT_CURVE_ID", "DEFAULT_SA_OUTPUT_CURVE", "DEFAULT_SA_OUTPUT_CURVE_ID" })
                Assert.Null(typeof(BackingStore).GetField(name));
        }
    }
}
