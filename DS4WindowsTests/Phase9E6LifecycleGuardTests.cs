using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9）: 開始・停止・ホットプラグの切り出しの守り。
    /// 本体と状態・イベントが <c>ServiceLifecycle</c> へ移り、<c>ControlService</c> は転送だけを持つこと。
    /// <b>安全弁（ガードレール）の順序</b>: <c>Start</c>（ViGEm の開始 → KBM の初期化 → 2 秒待ち → 排他モード → HidHide → OSC／UDP → 恒久スロット → 列挙 → <c>running</c> → UDP の待ち受け → 通知）、
    /// <c>Stop</c>（<c>running = false</c> → 事前通知 → デバイス停止と仮想パッドの取り外し → 登録簿の停止 → OSC／UDP の停止 → 出力キューの待ち → <c>OutputSlotManager.Stop</c> → 待ち → <c>StopViGEm</c> → KBM の切断 → 通知）が変わっていないこと。
    /// </summary>
    public class Phase9E6LifecycleGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9E6LifecycleGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private static void AssertOrder(string text, int from, int to, string[] steps, string what)
        {
            string region = text.Substring(from, to - from);
            int last = -1;
            foreach (string step in steps)
            {
                int index = region.IndexOf(step);
                Assert.True(index > last, $"{what}の順序が変わっている（'{step}'）");
                last = index;
            }
        }

        [Theory]
        [InlineData(@"\bStartViGEm\b")]
        [InlineData(@"\bStopViGEm\b")]
        [InlineData(@"\bAssignInitialDevices\b")]
        [InlineData(@"\.FindControllers\(")]
        [InlineData(@"\.StopControllers\(")]
        [InlineData(@"\bClearControllerList\b")]
        [InlineData(@"\bThread\.Sleep\(2000\)")]
        [InlineData(@"\bProcessPriorityClass\b")]
        [InlineData(@"\bMainWindow\b")]
        [InlineData(@"\bRunHotPlug\b")]
        public void ControlService_NoLongerContains_TheMovedLifecycle(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6f2 で ServiceLifecycle へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheForwarders_ForTheUiAndTheTests()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public bool Start(bool showlog = true)", text);
            Assert.Contains("return _lifecycle.Start(showlog);", text);
            Assert.Contains("public bool Stop(bool showlog = true, bool immediateUnplug = false)", text);
            Assert.Contains("return _lifecycle.Stop(showlog, immediateUnplug);", text);
            Assert.Contains("public bool HotPlug()", text);
            Assert.Contains("return _lifecycle.HotPlug();", text);
            Assert.Contains("public void PrepareAbort()", text);
            Assert.Contains("public bool running { get => _lifecycle.Running; set => _lifecycle.Running = value; }", text);
            Assert.Contains("public bool suspending { get => _lifecycle.Suspending; set => _lifecycle.Suspending = value; }", text);
            Assert.Contains("public bool loopControllers { get => _lifecycle.LoopControllers; set => _lifecycle.LoopControllers = value; }", text);
            Assert.Contains("public bool inServiceTask { get => _lifecycle.InServiceTask; set => _lifecycle.InServiceTask = value; }", text);
            Assert.Contains("public int activeControllers { get => _lifecycle.ActiveControllers; set => _lifecycle.ActiveControllers = value; }", text);

            // イベントは、送り主を ControlService にして再発火する
            Assert.Contains("ServiceStarted?.Invoke(this, e)", text);
            Assert.Contains("PreServiceStop?.Invoke(this, e)", text);
            Assert.Contains("ServiceStopped?.Invoke(this, e)", text);
            Assert.Contains("RunningChanged?.Invoke(this, e)", text);
            Assert.Contains("HotplugController?.Invoke(this, device, index)", text);
        }

        [Fact]
        public void Start_KeepsTheSafetyOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/ServiceLifecycle.cs");
            if (text == null) return;

            int start = text.IndexOf("public bool Start(");
            int stop = text.IndexOf("public bool Stop(");
            Assert.True(start > 0 && stop > start);

            AssertOrder(text, start, stop, new[]
            {
                "InServiceTask = true;",
                "_virtualPad.StartViGEm();",
                "InitializeOutputKbmHandler();",
                "Thread.Sleep(2000);",
                "_deviceRegistry.IsExclusiveMode = _appSettings.UseExclusiveMode;",
                "_hidHideService.UpdateAttributes();",
                "_oscService.ChangeListenerStatus(true);",
                "_oscService.ChangeSenderStatus(true);",
                "_udpMotion.ChangeStatus(true, false);",
                "LoopControllers = true;",
                "_virtualPad.AssignInitialDevices();",
                "_deviceRegistry.FindControllers();",
                "_deviceConnector.BeginPrepare(device, showlog: true);",
                "_deviceConnector.PrepareConnected(numControllers, device, index: i);",
                "Running = true;",
                "_udpMotion.StartListeningIfCreated();",
                "InServiceTask = false;",
                "_appSettings.RunHotPlug = true;",
                "ServiceStarted?.Invoke(this, EventArgs.Empty);",
                "RunningChanged?.Invoke(this, EventArgs.Empty);",
                "process.PriorityClass = ProcessPriorityClasses[_appSettings.ProcessPriority];",
            }, "Start");
        }

        [Fact]
        public void Stop_KeepsTheSafetyOrder_IncludingTheDriverDestructionOrder()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/ServiceLifecycle.cs");
            if (text == null) return;

            int stop = text.IndexOf("public bool Stop(");
            int hot = text.IndexOf("public bool HotPlug(");
            Assert.True(stop > 0 && hot > stop);

            AssertOrder(text, stop, hot, new[]
            {
                "Running = false;",
                "_appSettings.RunHotPlug = false;",
                "InServiceTask = true;",
                "PreServiceStop?.Invoke(this, EventArgs.Empty);",
                "_virtualPad.UnplugOutDev(i, tempDevice, immediate: immediateUnplug, force: true);",
                "_oscService.ResetDeviceState(i);",
                "_latencyMonitor.Reset(i);",
                "_deviceRegistry.StopControllers();",
                "slots.SlotManager.ClearControllerList();",
                "_oscService.ChangeListenerStatus(false);",
                "_oscService.ChangeSenderStatus(false);",
                "_udpMotion.ChangeStatus(false, true);",
                "while (_virtualPad.OutputSlotManager.RunningQueue)",
                "_virtualPad.OutputSlotManager.Stop(true);",
                "Thread.Sleep(OutputSlotManager.DELAY_TIME);",
                "_virtualPad.StopViGEm();",
                "_virtualKBM.Disconnect();",
                "InServiceTask = false;",
                "ActiveControllers = 0;",
                "ServiceStopped?.Invoke(this, EventArgs.Empty);",
                "RunningChanged?.Invoke(this, EventArgs.Empty);",
            }, "Stop");
        }

        [Fact]
        public void TheProcessPriorityList_HasTheSameOrderAsTheUiList()
        {
            string ui = ReadWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            string lifecycle = ReadWithoutComments("DS4Windows/DS4Control/Services/ServiceLifecycle.cs");
            if (ui == null || lifecycle == null) return;

            var uiList = Regex.Match(ui, @"ProcessPriorityClasses\s*=\s*\[(?<items>[^\]]*)\]", RegexOptions.Singleline);
            var lifecycleList = Regex.Match(lifecycle, @"ProcessPriorityClasses\s*=\s*\{(?<items>[^}]*)\}", RegexOptions.Singleline);
            Assert.True(uiList.Success && lifecycleList.Success);

            string[] Names(Match m) => Regex.Matches(m.Groups["items"].Value, @"ProcessPriorityClass\.(\w+)").Cast<Match>().Select(x => x.Groups[1].Value).ToArray();
            Assert.Equal(Names(uiList), Names(lifecycleList));
            Assert.Equal(4, Names(uiList).Length);
        }

        [Fact]
        public void TheLifecycle_IsRegistered_StartsInTheStoppedState_AndHasNoStaticEntryPoints()
        {
            var lifecycle = TestHost.GetService<IServiceLifecycle>();
            Assert.IsType<ServiceLifecycle>(lifecycle);
            Assert.Same(lifecycle, TestHost.GetService<IServiceLifecycle>());
            Assert.Same(TestHost.GetService<ControlService>(), TestHost.GetService<IServiceLifecycleHost>());

            // 起動直後の状態（旧: running = false、loopControllers = true、inServiceTask = false、activeControllers = 0）
            var fresh = new ServiceLifecycle(TestHost.GetService<IAppSettingsService>(), TestHost.GetService<IEnvironmentService>(),
                TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IDs4DeviceRegistry>(), TestHost.GetService<IVirtualKBM>(),
                TestHost.GetService<IOscService>(), TestHost.GetService<IUdpMotionService>(), TestHost.GetService<IHidHideService>(),
                TestHost.GetService<IVirtualPadService>(), TestHost.GetService<ILatencyMonitor>(), TestHost.GetService<IJoyConLinker>(),
                TestHost.GetService<IDeviceConnector>(), () => TestHost.GetService<IDeviceSlotHost>(), () => TestHost.GetService<IServiceLifecycleHost>());
            Assert.False(fresh.Running);
            Assert.True(fresh.LoopControllers);
            Assert.False(fresh.InServiceTask);
            Assert.Equal(0, fresh.ActiveControllers);
            Assert.False(fresh.Suspending);

            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/ServiceLifecycle.cs");
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.DoesNotContain("MainWindow", text);
            Assert.False(Regex.IsMatch(text, @"\bControlService\b"), "ServiceLifecycle が ControlService を参照している");
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), "ServiceLifecycle が Global を参照している");
        }
    }
}
