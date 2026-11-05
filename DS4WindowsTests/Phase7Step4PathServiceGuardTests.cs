using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step4 W3: 実行ファイルの情報と設定の保存場所（旧 <c>Global</c> の状態と <c>FindConfigLocation</c>／<c>SaveWhere</c>／<c>SaveDefault</c>）を
    /// <see cref="PathService"/> へ移した結果の特性化テストと回帰ガード。
    /// - <see cref="PathService.FindConfigLocation(BackingStore, string, string, Func{string, bool})"/> の 4 通りの分岐（旧 <c>Global.FindConfigLocation</c> の分岐そのまま）。
    /// - <c>Global</c> に残る 4 つの窓口（appdatapath／exedirpath／exelocation／exeversion）が <see cref="PathService"/> の状態を指すこと。
    /// - 削除済みの <c>Global</c> のメンバーが再び宣言されないこと。静的初期化の循環を作らないこと。
    /// 静的な状態（AppDataPathState、FirstRunState ほか）を書き換えるため、同じ状態を使うテストと直列に実行し、必ず元へ戻す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step4PathServiceGuardTests
    {
        private const string Exe = @"C:\Phase7Exe";
        private const string Roaming = @"C:\Phase7Roaming";

        private readonly ITestOutputHelper _output;

        public Phase7Step4PathServiceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>静的な状態を退避して、テストの後で元へ戻す。</summary>
        private sealed class StateScope : IDisposable
        {
            private readonly string _appData = PathService.AppDataPathState;
            private readonly bool _firstRun = PathService.FirstRunState;
            private readonly bool _multi = PathService.MultipleSaveLocationsState;

            public void Dispose()
            {
                PathService.AppDataPathState = _appData;
                PathService.FirstRunState = _firstRun;
                PathService.MultipleSaveLocationsState = _multi;
            }
        }

        private static Func<string, bool> Exists(bool programFolder, bool appData) =>
            path => path == Path.Combine(Exe, "Auto Profiles.xml") ? programFolder
                  : path == Path.Combine(Roaming, "Auto Profiles.xml") ? appData
                  : false;

        // ---- FindConfigLocation の分岐 ----

        [Fact]
        public void FindConfigLocation_BothFolders_MarksFirstRunAndMultipleLocations_AndDoesNotChooseASaveLocation()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                PathService.AppDataPathState = "untouched";
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(true, true));

                Assert.True(PathService.FirstRunState);
                Assert.True(PathService.MultipleSaveLocationsState);
                Assert.Equal("untouched", PathService.AppDataPathState);
            }
        }

        [Fact]
        public void FindConfigLocation_ProgramFolderOnly_ChoosesTheProgramFolder_AndKeepsTheFlags()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(true, false));

                Assert.Equal(Exe, PathService.AppDataPathState);
                Assert.Equal(Exe + "\\Profiles.xml", config.m_Profile);
                Assert.Equal(Exe + "\\Actions.xml", config.m_Actions);
                Assert.Equal(Exe + "\\LinkedProfiles.xml", config.m_linkedProfiles);
                Assert.Equal(Exe + "\\ControllerConfigs.xml", config.m_controllerConfigs);
                Assert.False(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void FindConfigLocation_AppDataOnly_ChoosesAppData_AndKeepsTheFlags()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                PathService.FirstRunState = true;
                PathService.MultipleSaveLocationsState = true;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(false, true));

                Assert.Equal(Roaming, PathService.AppDataPathState);
                Assert.Equal(Roaming + "\\Profiles.xml", config.m_Profile);
                // 旧実装は、この分岐でフラグを false に戻さない
                Assert.True(PathService.FirstRunState);
                Assert.True(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void FindConfigLocation_NeitherFolder_MarksFirstRun_ResetsMultipleLocations_AndDoesNotChooseASaveLocation()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                PathService.AppDataPathState = "untouched";
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = true;

                PathService.FindConfigLocation(config, Exe, Roaming, Exists(false, false));

                Assert.True(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
                Assert.Equal("untouched", PathService.AppDataPathState);
            }
        }

        [Fact]
        public void FindConfigLocation_SameFolderForBoth_IsTreatedAsTheProgramFolder()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                PathService.FirstRunState = false;
                PathService.MultipleSaveLocationsState = false;
                Func<string, bool> both = path => path == Path.Combine(Exe, "Auto Profiles.xml");

                PathService.FindConfigLocation(config, Exe, Exe, both);

                Assert.Equal(Exe, PathService.AppDataPathState);
                Assert.False(PathService.FirstRunState);
                Assert.False(PathService.MultipleSaveLocationsState);
            }
        }

        [Fact]
        public void SaveWhere_UpdatesTheStateAndAllFourSettingsFilePaths()
        {
            using (new StateScope())
            {
                var config = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());

                PathService.SaveWhere(config, @"C:\Phase7SaveWhere");

                Assert.Equal(@"C:\Phase7SaveWhere", PathService.AppDataPathState);
                Assert.Equal(@"C:\Phase7SaveWhere\Profiles.xml", config.m_Profile);
                Assert.Equal(@"C:\Phase7SaveWhere\Actions.xml", config.m_Actions);
                Assert.Equal(@"C:\Phase7SaveWhere\LinkedProfiles.xml", config.m_linkedProfiles);
                Assert.Equal(@"C:\Phase7SaveWhere\ControllerConfigs.xml", config.m_controllerConfigs);
            }
        }

        // ---- Global の窓口 ----

        [Fact]
        public void GlobalWindows_PointAtThePathServiceState()
        {
            using (new StateScope())
            {
                string originalExeDir = PathService.ExecutableDirectoryState;
                try
                {
                    PathService.AppDataPathState = @"C:\Phase7Window";
                    Assert.Equal(@"C:\Phase7Window", TestGlobals.AppDataPath);

                    TestGlobals.AppDataPath = @"C:\Phase7Window2";
                    Assert.Equal(@"C:\Phase7Window2", PathService.AppDataPathState);

                    TestGlobals.ExeDirPath = @"C:\Phase7ExeWindow";
                    Assert.Equal(@"C:\Phase7ExeWindow", PathService.ExecutableDirectoryState);

                }
                finally
                {
                    PathService.ExecutableDirectoryState = originalExeDir;
                }
            }
        }

        [Fact]
        public void PathService_Instances_ShareTheSameState()
        {
            using (new StateScope())
            {
                PathService.AppDataPathState = @"C:\Phase7Shared";

                Assert.Equal(@"C:\Phase7Shared", new PathService().AppDataPath);
                Assert.Equal(PathService.ExecutableDirectoryState, new PathService().ProgramDirectory);
                Assert.Equal(PathService.ExecutableLocationState, new PathService().ExecutablePath);
            }
        }

        // ---- 回帰ガード ----
    }
}
