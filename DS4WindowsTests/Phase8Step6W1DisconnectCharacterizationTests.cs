using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step6 W1（Phase8-Step6-Plan.md §3.1。Q6＝A）: 切断・再接続まわりの<b>現行の挙動</b>を固定する（コードは変えない）。
    /// Step6 の根拠になる事実:
    /// 1. <c>Mapping.HandleDeviceDisconnect</c> はスロットのプロファイル選択の状態（<c>OlderProfilePath</c> ほか）に触れない。
    /// 2. プロファイル適用の後始末（<c>ClearDeviceState</c>）も、同じ切断の後始末を通る（だから、ここにエンジンの切断を足せない）。
    /// 3. 物理的な切断（<c>On_DS4Removal</c>）だけが <c>HandleDeviceDisconnect</c> を呼び、サービスの停止（<c>Stop</c>）は呼ばない。
    /// 4. <c>OlderProfilePath</c> は、初回接続の既定、画面（リンクの解除、名前変更）などで使われる「通常のプロファイル」。
    /// 5. <c>ClearState</c>／<c>ClearPendingRestore</c> の本番の呼び出しは 0 件だった（W3 で削除した。Q3＝A）。
    /// 専用のデバイス添字（7）を使い、終了時に状態を戻す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step6W1DisconnectCharacterizationTests
    {
        private const int Device = 7;

        private static Mapping GetMapping()
        {
            var mapping = TestHost.GetService<IMapping>() as Mapping;
            Assert.NotNull(mapping);
            return mapping;
        }

        [Fact]
        public void HandleDeviceDisconnect_DoesNotChangeTheProfileSelectionState()
        {
            BackingStore store = TestGlobals.Store;
            IProfileSettingsService settings = TestGlobals.ProfileSettingsServiceInstance;
            string older = store.olderProfilePath[Device], selected = store.selectedProfile[Device], path = store.profilePath[Device];
            bool useTemp = settings.GetUseTempProfile(Device);
            string tempName = settings.GetTempProfileName(Device);
            try
            {
                store.olderProfilePath[Device] = "OlderSentinel";
                store.selectedProfile[Device] = "SelectedSentinel";
                store.profilePath[Device] = "PathSentinel";
                settings.SetUseTempProfile(Device, true);
                settings.SetTempProfileName(Device, "TempSentinel");

                GetMapping().HandleDeviceDisconnect(Device);

                Assert.Equal("OlderSentinel", store.olderProfilePath[Device]);
                Assert.Equal("SelectedSentinel", store.selectedProfile[Device]);
                Assert.Equal("PathSentinel", store.profilePath[Device]);
                Assert.True(settings.GetUseTempProfile(Device));
                Assert.Equal("TempSentinel", settings.GetTempProfileName(Device));
            }
            finally
            {
                store.olderProfilePath[Device] = older;
                store.selectedProfile[Device] = selected;
                store.profilePath[Device] = path;
                settings.SetUseTempProfile(Device, useTemp);
                settings.SetTempProfileName(Device, tempName);
            }
        }

        [Fact]
        public void ClearDeviceState_OfTheProfileApply_RunsTheSameCleanupAsADisconnect()
        {
            Mapping mapping = GetMapping();
            var managed = TestHost.GetService<IManagedActionManager>();
            DeviceRuntimeState before = mapping.deviceRuntime[Device];

            managed.ClearDeviceState(Device);

            // 適用の後始末が HandleDeviceDisconnect を通り、deviceRuntime が作り直される（復帰の合図だけは退避・復元される。Phase7b W1）
            Assert.NotSame(before, mapping.deviceRuntime[Device]);
        }

        [Fact]
        public void OnlyThePhysicalRemoval_CallsHandleDeviceDisconnect_AndTheServiceStopDoesNot()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);

            // Phase9 9-E E-6e: 切断の処理は DeviceDisconnectHandler.OnRemoval へ移った（ControlService.On_DS4Removal は委譲）
            string handlerPath = SourceFileLocator.Find("DS4Windows/DS4Control/Services/DeviceDisconnectHandler.cs");
            if (handlerPath == null) return;
            string removal = MethodBody(File.ReadAllText(handlerPath), "public void OnRemoval(");
            Assert.Contains("_mapping.HandleDeviceDisconnect(ind);", removal);

            // Phase9 9-E E-6f2: Stop の本体は ServiceLifecycle へ移った
            string lifecyclePath = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ServiceLifecycle.cs");
            if (lifecyclePath == null) return;
            string stop = MethodBody(File.ReadAllText(lifecyclePath), "public bool Stop(");
            Assert.DoesNotContain("HandleDeviceDisconnect", stop);
            Assert.DoesNotContain("On_DS4Removal", stop);
        }

        [Fact]
        public void TheFirstConnection_UsesTheOlderProfilePath_AndAReconnectionUsesTheSelectedProfile()
        {
            // Phase9 9-E E-6a: プロファイルの選択は ConnectionProfileSelector へ移った
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ConnectionProfileSelector.cs");
            if (path == null) return;

            string body = MethodBody(File.ReadAllText(path), "public bool ApplyOnConnect(");
            int first = body.IndexOf("IsFirstConnection(index, connectedMac)", StringComparison.Ordinal);
            int older = body.IndexOf("profileToApply = _profileRepository.OlderProfilePath[index];", StringComparison.Ordinal);
            int selected = body.IndexOf("profileToApply = _profileRepository.SelectedProfile[index];", StringComparison.Ordinal);
            Assert.True(first >= 0 && older > first && selected > older, "初回接続は OlderProfilePath、再接続は SelectedProfile");

            // 一時プロファイル／AutoProfile を使うとき（useAutoProfile）は、プロファイルを適用し直さない
            Assert.Contains("if (!useAutoProfile)", body);
            Assert.Contains("_profileSlotApplier.ApplyToSlot(index, profileToApply", body);
        }

        [Fact]
        public void TheFirstConnectionFlag_IsResetOnlyAtTheAppStartup()
        {
            string app = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            string root = RepoRoot();
            if (app == null || root == null) return;

            var callers = Sources(root)
                .Where(f => File.ReadAllText(f).Contains(".ResetConnectionFlags();"))
                .Select(f => Rel(root, f)).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "DS4Windows/App.xaml.cs" }, callers);
        }

        [Fact]
        public void OlderProfilePath_IsUsedByTheKnownFiles_Only()
        {
            string root = RepoRoot();
            if (root == null) return;

            string[] files = Sources(root)
                .Where(f => Code(f).IndexOf("olderProfilePath", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => Rel(root, f)).OrderBy(x => x).ToArray();

            Assert.Equal(new[]
            {
                "DS4Windows/App.xaml.cs",                                        // 起動時の既定の設定
                "DS4Windows/DI/IProfileRepository.cs",
                "DS4Windows/DS4Control/BackingStore.cs",
                "DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs",                // アプリ設定の読込
                "DS4Windows/DS4Control/Services/AppSettingsXmlSerializer.cs",
                "DS4Windows/DS4Control/Services/ConnectionProfileSelector.cs",   // 初回接続の既定（Phase9 9-E E-6a: ControlService から移した）
                "DS4Windows/DS4Control/Services/ProfileApplicationService.cs",   // 適用での更新、RestoreFromAction
                "DS4Windows/DS4Control/Services/ProfileRepository.cs",
                "DS4Windows/DS4Forms/ViewModels/CompositeDeviceModel.cs",        // 画面の選択、リンクの解除
            }, files);
        }

        [Fact]
        public void ClearState_AndClearPendingRestore_AreRemoved()
        {
            // Phase8-Step6 W3（Q3＝A。承認済み）: 本番の呼び出しが 0 件だった 2 つのメンバーを削除した。
            // 切断で OlderProfilePath を空にするとスロットの「通常のプロファイル」が失われるため、接続しなかった。
            Assert.Null(typeof(IProfileSwitcher).GetMethod("ClearState"));
            Assert.Null(typeof(IProfileApplicationService).GetMethod("ClearPendingRestore"));
            Assert.Null(typeof(DefaultProfileSwitcher).GetMethod("ClearState"));
            Assert.Null(typeof(ProfileApplicationService).GetMethod("ClearPendingRestore"));
        }

        // ---- 補助 ----

        private static string RepoRoot()
        {
            string controlService = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (controlService == null) return null;
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(controlService)));
        }

        private static IEnumerable<string> Sources(string root)
            => Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

        // コメントの行を除いたソース（コメントでの言及は、呼び出しとして数えない）
        private static string Code(string file)
            => string.Join(Environment.NewLine, File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//")));

        private static string Rel(string root, string file)
            => file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, '/').Replace(Path.DirectorySeparatorChar, '/');

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
