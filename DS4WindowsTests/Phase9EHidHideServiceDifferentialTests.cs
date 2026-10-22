using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: <see cref="HidHideService"/> を、旧実装（<c>ControlService.CheckHidHidePresence</c>／<c>UpdateHidHideAttributes</c> を逐語的に写したもの。
    /// ドライバーの窓口は <see cref="FakeHidHideApi"/>、ログは一覧に記録）と<b>突き合わせる差分テスト</b>。
    /// ホワイトリストの確認（HidHide の未導入、制御デバイスが開けない、インバース、既にある／ない × 追加／削除）の書き込みとログ、有効状態と非表示の一覧の読み直しを調べる。
    /// ドライバーと DS4Device が必要な <c>IsAffected</c> の判定そのものは、実機でなければ確かめられないため、ここでは「無効のとき false」だけを調べる。
    /// </summary>
    public class Phase9EHidHideServiceDifferentialTests
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "QueryDosDeviceW")]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        /// <summary>旧 <c>ControlService</c> の HidHide 系の逐語的な写し。</summary>
        private sealed class OldHidHide
        {
            private readonly IEnvironmentService _environmentService;
            private readonly IPathService _pathService;
            private readonly Func<IHidHideApi> _open;
            public readonly List<string> Logs = new List<string>();
            public readonly HashSet<string> hidDeviceHidingAffectedDevs = new HashSet<string>();
            public readonly HashSet<string> hidDeviceHidingExemptedDevs = new HashSet<string>();
            public bool hidDeviceHidingForced = false;
            public bool hidDeviceHidingEnabled = false;

            public OldHidHide(IEnvironmentService environmentService, IPathService pathService, Func<IHidHideApi> open)
            {
                _environmentService = environmentService;
                _pathService = pathService;
                _open = open;
            }

            private void Log(string data) => Logs.Add(data);

            public void CheckHidHidePresence(string ExePath = "", string ExeName = "Autoprofile Exe", bool AddExe = true) // Default value for D4W Startup
            {
                if (_environmentService.HidHideInstalled)
                {
                    Log("HidHide control device found");
                    using (IHidHideApi hidHideDevice = _open())
                    {
                        if (!hidHideDevice.IsOpen())
                        {
                            return;
                        }
                        // Catch Blank Values and initialize for Startup. Also catches empty Values.
                        // Also Catches Empty values in auto-profiler, and defaults to trying to re-add D4W. Will fail harmlessly later.
                        if (ExePath == "") { ExePath = _pathService.ExecutablePath; ExeName = "DS4Windows"; AddExe = true; }

                        // Check for inverse application cloak. If setting is being used in HidHide,
                        // skip checking HidHide whitelist for DS4Windows.
                        bool inverseAppCloak = hidHideDevice.GetWhiteListInverseState();
                        if (inverseAppCloak)
                        {
                            return;
                        }


                        List<string> dosPaths = hidHideDevice.GetWhitelist();

                        int maxPathCheckLength = 512;
                        StringBuilder sb = new StringBuilder(maxPathCheckLength);

                        DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(ExePath));
                        // Check if exe is placed in a junction symlink directory (done with Scoop).
                        // Good enough
                        if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                            dirInfo.LinkTarget != null)
                        {
                            // App directory is a junction. Find real directory and get proper path
                            // for inserting into HidHide
                            ExePath = Path.Combine(dirInfo.LinkTarget, Path.GetFileName(ExePath));
                        }

                        string driveLetter = Path.GetPathRoot(ExePath).Replace("\\", "");
                        uint _ = QueryDosDevice(driveLetter, sb, maxPathCheckLength);
                        //int error = Marshal.GetLastWin32Error();

                        string dosDrivePath = sb.ToString();
                        // Strip a possible \??\ prefix.
                        if (dosDrivePath.StartsWith(@"\??\"))
                        {
                            dosDrivePath = dosDrivePath.Remove(0, 4);
                        }

                        string partial = ExePath.Replace(driveLetter, "");
                        // Need to trim starting '\\' from path2 or Path.Combine will
                        // treat it as an absolute path and only return path2
                        string realPath = Path.Combine(dosDrivePath, partial.TrimStart('\\'));
                        bool exists = dosPaths.Contains(realPath);
                        if (!exists && AddExe)
                        {
                            Log($"{ExeName} not found in HidHide whitelist. Adding to list");
                            dosPaths.Add(realPath);
                            hidHideDevice.SetWhitelist(dosPaths);
                        }
                        if (exists && !AddExe)
                        {
                            Log($"{ExeName} found in HidHide whitelist. Removing from list");
                            dosPaths.Remove(realPath);
                            hidHideDevice.SetWhitelist(dosPaths);
                        }
                    }
                }
            }

            public void UpdateHidHideAttributes()
            {
                if (_environmentService.HidHideInstalled)
                {
                    hidDeviceHidingAffectedDevs.Clear();
                    hidDeviceHidingExemptedDevs.Clear(); // No known equivalent in HidHide
                    hidDeviceHidingForced = false; // No known equivalent in HidHide
                    hidDeviceHidingEnabled = false;

                    using (IHidHideApi hidHideDevice = _open())
                    {
                        if (!hidHideDevice.IsOpen())
                        {
                            return;
                        }

                        bool active = hidHideDevice.GetActiveState();
                        List<string> instances = hidHideDevice.GetBlacklist();

                        hidDeviceHidingEnabled = active;
                        foreach (string instance in instances)
                        {
                            hidDeviceHidingAffectedDevs.Add(instance.ToUpper());
                        }
                    }
                }
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool _saved = EnvironmentService.HidHideInstalledState;
            public Scope(bool installed) { EnvironmentService.HidHideInstalledState = installed; }
            public void Dispose() { EnvironmentService.HidHideInstalledState = _saved; }
        }

        private static FakeHidHideApi NewFake(bool opened, bool inverse, IEnumerable<string> whitelist, bool active = false, IEnumerable<string> blacklist = null)
        {
            return new FakeHidHideApi
            {
                Opened = opened, Inverse = inverse, Whitelist = new List<string>(whitelist), Active = active,
                Blacklist = new List<string>(blacklist ?? new string[0]),
            };
        }

        private static string TempExe() => Path.Combine(Path.GetTempPath(), "DS4Windows.exe");

        /// <summary>実行ファイルのパスが、HidHide のホワイトリストに入る形（DOS デバイスのパス）を、旧実装の追加の結果から取り出す。</summary>
        private static string RealPathOf(string exePath)
        {
            var probe = NewFake(true, false, new string[0]);
            var old = new OldHidHide(TestHost.GetService<IEnvironmentService>(), TestHost.GetService<IPathService>(), probe.Open);
            old.CheckHidHidePresence(exePath, "probe", true);
            return probe.SetCalls.Single().Single();
        }

        [Fact]
        public void CheckPresence_MatchesTheOldImplementation_ForEveryCombination()
        {
            IEnvironmentService env = TestHost.GetService<IEnvironmentService>();
            IPathService paths = TestHost.GetService<IPathService>();
            string exe = TempExe();
            string real;
            using (new Scope(true)) real = RealPathOf(exe);
            Assert.EndsWith("DS4Windows.exe", real);

            foreach (bool installed in new[] { false, true })
            foreach (bool opened in new[] { true, false })
            foreach (bool inverse in new[] { false, true })
            foreach (bool exists in new[] { false, true })
            foreach (bool addExe in new[] { true, false })
            foreach (bool emptyPath in new[] { false, true })
            {
                using var scope = new Scope(installed);
                // 空のパスのときは DS4Windows 自身（IPathService.ExecutablePath）を扱う。既にある場合のために、そのパスの変換結果も用意する
                string target = emptyPath ? paths.ExecutablePath : exe;
                string realTarget = real;
                if (emptyPath)
                {
                    using (new Scope(true)) realTarget = RealPathOf(target);   // 変換結果を得るため、一時的に「導入済み」にする
                }
                var whitelist = new List<string> { @"\\Device\\Other\\a.exe" };
                if (exists) whitelist.Add(realTarget);

                FakeHidHideApi oldApi = NewFake(opened, inverse, whitelist);
                FakeHidHideApi newApi = NewFake(opened, inverse, whitelist);
                var old = new OldHidHide(env, paths, oldApi.Open);
                var logs = new List<string>();
                var service = new HidHideService(env, paths, newApi);
                service.DebugMessage += (data, warning) => logs.Add(data);

                string where = $"installed={installed} opened={opened} inverse={inverse} exists={exists} add={addExe} empty={emptyPath}";
                string path = emptyPath ? "" : exe;
                old.CheckHidHidePresence(path, "TestExe", addExe);
                service.CheckPresence(path, "TestExe", addExe);

                Assert.True(oldApi.SetCalls.Count == newApi.SetCalls.Count, where + ": 書き込みの回数が違う");
                for (int i = 0; i < oldApi.SetCalls.Count; i++)
                {
                    Assert.True(oldApi.SetCalls[i].SequenceEqual(newApi.SetCalls[i]), where + ": 書き込んだホワイトリストが違う");
                }

                Assert.True(oldApi.Whitelist.SequenceEqual(newApi.Whitelist), where + ": 最終のホワイトリストが違う");
                Assert.True(old.Logs.SequenceEqual(logs), where + ": ログが違う");
                Assert.True(oldApi.OpenCount == newApi.OpenCount && oldApi.DisposeCount == newApi.DisposeCount, where + ": 開閉の回数が違う");
            }
        }

        [Fact]
        public void CheckPresence_AddsTheExeToTheWhitelist_WhenItIsNotThereYet()
        {
            using var scope = new Scope(true);
            var api = NewFake(true, false, new string[0]);
            var service = new HidHideService(TestHost.GetService<IEnvironmentService>(), TestHost.GetService<IPathService>(), api);

            service.CheckPresence(TempExe(), "TestExe", true);

            Assert.Single(api.SetCalls);
            Assert.EndsWith("DS4Windows.exe", api.Whitelist.Single());
        }

        [Fact]
        public void CheckPresence_DoesNothing_WhenHidHideIsNotInstalled()
        {
            using var scope = new Scope(false);
            var api = NewFake(true, false, new string[0]);
            var service = new HidHideService(TestHost.GetService<IEnvironmentService>(), TestHost.GetService<IPathService>(), api);

            service.CheckPresence(TempExe(), "TestExe", true);

            Assert.Equal(0, api.OpenCount);
            Assert.Empty(api.SetCalls);
        }

        [Fact]
        public void UpdateAttributes_MatchesTheOldImplementation()
        {
            IEnvironmentService env = TestHost.GetService<IEnvironmentService>();
            IPathService paths = TestHost.GetService<IPathService>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            foreach (bool installed in new[] { false, true })
            foreach (bool opened in new[] { true, false })
            foreach (bool active in new[] { true, false })
            {
                using var scope = new Scope(installed);
                var blacklist = new[] { "usb\\vid_054c&pid_09cc\\a", "Hid\\Abc" };
                FakeHidHideApi oldApi = NewFake(opened, false, new string[0], active, blacklist);
                FakeHidHideApi newApi = NewFake(opened, false, new string[0], active, blacklist);
                var old = new OldHidHide(env, paths, oldApi.Open);
                var service = new HidHideService(env, paths, newApi);

                old.UpdateHidHideAttributes();
                service.UpdateAttributes();
                // もう一度（前回の内容が消えてから入り直すこと）
                old.UpdateHidHideAttributes();
                service.UpdateAttributes();

                string where = $"installed={installed} opened={opened} active={active}";
                var newAffected = (HashSet<string>)typeof(HidHideService).GetField("hidDeviceHidingAffectedDevs", flags).GetValue(service);
                var newEnabled = (bool)typeof(HidHideService).GetField("hidDeviceHidingEnabled", flags).GetValue(service);
                Assert.True(old.hidDeviceHidingAffectedDevs.SetEquals(newAffected), where + ": 非表示の一覧が違う");
                Assert.True(old.hidDeviceHidingEnabled == newEnabled, where + ": 有効状態が違う");
                Assert.True(oldApi.OpenCount == newApi.OpenCount && oldApi.DisposeCount == newApi.DisposeCount, where + ": 開閉の回数が違う");
            }
        }

        [Fact]
        public void IsAffected_IsFalse_ForNullOrWhenHidingIsNotEnabled()
        {
            var service = new HidHideService(TestHost.GetService<IEnvironmentService>(), TestHost.GetService<IPathService>(), NewFake(true, false, new string[0]));
            Assert.False(service.IsAffected(null));
        }

        [Fact]
        public void Constructor_RejectsNull()
        {
            IEnvironmentService env = TestHost.GetService<IEnvironmentService>();
            IPathService paths = TestHost.GetService<IPathService>();
            var api = NewFake(true, false, new string[0]);
            Assert.Throws<ArgumentNullException>(() => new HidHideService(null, paths, api));
            Assert.Throws<ArgumentNullException>(() => new HidHideService(env, null, api));
            Assert.Throws<ArgumentNullException>(() => new HidHideService(env, paths, null));
        }
    }
}
