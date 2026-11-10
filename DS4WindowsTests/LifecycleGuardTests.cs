using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9）: 開始・停止・ホットプラグの切り出しの守り。
    /// 本体と状態・イベントが <c>ServiceLifecycle</c> へ移り、<c>ControlService</c> は転送だけを持つこと。
    /// <b>安全弁（ガードレール）の順序</b>: <c>Start</c>（ViGEm の開始 → KBM の初期化 → 2 秒待ち → 排他モード → HidHide → OSC／UDP → 恒久スロット → 列挙 → <c>running</c> → UDP の待ち受け → 通知）、
    /// <c>Stop</c>（<c>running = false</c> → 事前通知 → デバイス停止と仮想パッドの取り外し → 登録簿の停止 → OSC／UDP の停止 → 出力キューの待ち → <c>OutputSlotManager.Stop</c> → 待ち → <c>StopViGEm</c> → KBM の切断 → 通知）が変わっていないこと。
    /// </summary>
    public class LifecycleGuardTests
    {
        private readonly ITestOutputHelper _output;

        public LifecycleGuardTests(ITestOutputHelper output)
        {
            _output = output;
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
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-6f2 で ServiceLifecycle へ移した）");
        }

        [Fact]
        public void ControlService_HasNoLifecycleForwarders_BecauseTheCallersUseIServiceLifecycle()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (text == null) return;

            // 利用者（画面・App・テスト）は IServiceLifecycle を直接使う。開始・停止・ホットプラグと 5 つのイベントの転送は、利用者が 0 件になったため削除した
            Assert.DoesNotContain("public bool Start(", text);
            Assert.DoesNotContain("public bool Stop(", text);
            Assert.DoesNotContain("public bool HotPlug()", text);
            Assert.DoesNotContain("public void PrepareAbort()", text);
            Assert.DoesNotContain("public event EventHandler ServiceStarted", text);
            Assert.DoesNotContain("public event HotplugControllerHandler HotplugController", text);
            Assert.DoesNotContain("public bool running", text);
            Assert.DoesNotContain("public bool suspending", text);
            Assert.DoesNotContain("public bool loopControllers", text);
            Assert.DoesNotContain("public bool inServiceTask", text);
            Assert.DoesNotContain("public int activeControllers", text);
            // 仮想パッドの失敗での停止は BackendFailureHandler が _lifecycle を直接使う
            string handler = SourceText.ReadCode("DS4Windows/Processing/Services/BackendFailureHandler.cs");
            if (handler == null) return;
            Assert.Contains("_lifecycle.LoopControllers = false;", handler);
            Assert.Contains("while (_lifecycle.InServiceTask)", handler);
            Assert.Contains("_lifecycle.Stop();", handler);
        }

        [Fact]
        public void Start_KeepsTheSafetyOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
            if (text == null) return;

            int start = text.IndexOf("public bool Start(");
            int stop = text.IndexOf("public bool Stop(");
            Assert.True(start > 0 && stop > start);

            AssertOrder(text, start, stop, new[]
            {
                "InServiceTask = true;",
                "_outputs.StartVirtualOutputs(showlog)",
                "_deviceRegistry.IsExclusiveMode = _appSettings.UseExclusiveMode;",
                "_outputs.UpdateHidHide();",
                "_outputs.StartNetworkOutputs();",
                "LoopControllers = true;",
                "_virtualPad.AssignInitialDevices();",
                "_deviceRegistry.FindControllers();",
                "_deviceConnector.BeginPrepare(device, showlog: true);",
                "_deviceConnector.PrepareConnected(numControllers, device, index: i);",
                "Running = true;",
                "_outputs.StartUdpListening();",
                "InServiceTask = false;",
                "_appSettings.RunHotPlug = true;",
                "ServiceStarted?.Invoke(this, EventArgs.Empty);",
                "RunningChanged?.Invoke(this, EventArgs.Empty);",
                "process.PriorityClass = ProcessPriorityOptions.Classes[_appSettings.ProcessPriority];",
            }, "Start");
        }

        [Fact]
        public void Stop_KeepsTheSafetyOrder_IncludingTheDriverDestructionOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
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
                "_outputs.ResetDeviceState(i);",
                "_latencyMonitor.Reset(i);",
                "_deviceRegistry.StopControllers();",
                "slots.SlotManager.ClearControllerList();",
                "_outputs.StopNetworkOutputs();",
                "_outputs.StopVirtualOutputs(anyUnplugged);",
                "InServiceTask = false;",
                "ActiveControllers = 0;",
                "ServiceStopped?.Invoke(this, EventArgs.Empty);",
                "RunningChanged?.Invoke(this, EventArgs.Empty);",
            }, "Stop");
        }

        /// <summary>
        /// 出力側のサービスの開始・停止の内側の順序（ドライバーの開始 → キーボード・マウスの初期化 → 2 秒待ち、
        /// 停止時の出力キューの待ち → スロットの停止 → 取り外しの待ち → ドライバーの破棄 → キーボード・マウスの切断）。
        /// ServiceLifecycle からの呼び出しの順序は、上の 2 つのテストが固定する。
        /// </summary>
        [Fact]
        public void OutputServicesLifecycle_KeepsTheDriverStartAndDestructionOrder()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Services/OutputServicesLifecycle.cs");
            if (text == null) return;

            int start = text.IndexOf("public bool StartVirtualOutputs(");
            int hide = text.IndexOf("public void UpdateHidHide(");
            Assert.True(start > 0 && hide > start);
            AssertOrder(text, start, hide, new[]
            {
                "_virtualPad.StartViGEm();",
                "_kbmInitializer.Initialize();",
                "Thread.Sleep(2000);",
                "\"Log_Svc_UacConflict\"",
                "\"Log_Svc_KbmHandler\"",
                "\"Log_Svc_VigemConnected\"",
            }, "StartVirtualOutputs");

            int network = text.IndexOf("public void StartNetworkOutputs(");
            int udpListen = text.IndexOf("public void StartUdpListening(");
            Assert.True(network > 0 && udpListen > network);
            AssertOrder(text, network, udpListen, new[]
            {
                "_oscService.ChangeListenerStatus(true);",
                "_oscService.ChangeSenderStatus(true);",
                "_udpMotion.ChangeStatus(true, false);",
            }, "StartNetworkOutputs");

            int stopNet = text.IndexOf("public void StopNetworkOutputs(");
            int stopOut = text.IndexOf("public void StopVirtualOutputs(");
            Assert.True(stopNet > 0 && stopOut > stopNet);
            AssertOrder(text, stopNet, stopOut, new[]
            {
                "_oscService.ChangeListenerStatus(false);",
                "_oscService.ChangeSenderStatus(false);",
                "_udpMotion.ChangeStatus(false, true);",
            }, "StopNetworkOutputs");

            AssertOrder(text, stopOut, text.Length, new[]
            {
                "while (_virtualPad.IsOutputQueueRunning)",
                "_virtualPad.StopOutputSlots(immediate: true);",
                "Thread.Sleep(OutputSlotManager.DELAY_TIME);",
                "_virtualPad.StopViGEm();",
                "_virtualKBM.Disconnect();",
            }, "StopVirtualOutputs");
        }

        [Fact]
        public void TheOutputServicesLifecycle_IsRegisteredAsASingleton()
        {
            var outputs = TestHost.GetService<IOutputServicesLifecycle>();
            Assert.IsType<OutputServicesLifecycle>(outputs);
            Assert.Same(outputs, TestHost.GetService<IOutputServicesLifecycle>());
        }

        [Fact]
        public void TheProcessPriorityList_IsDefinedOnlyInProcessPriorityOptions_AndKeepsTheSavedOrder()
        {
            string ui = SourceText.ReadCode("DS4Windows/UI/MainWindow.xaml.cs");
            string lifecycle = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
            string options = SourceText.ReadCode("DS4Windows/Core/Platform/ProcessPriorityOptions.cs");
            if (ui == null || lifecycle == null || options == null) return;

            Assert.DoesNotContain("ProcessPriorityClass.AboveNormal", ui);
            Assert.DoesNotContain("ProcessPriorityClass.AboveNormal", lifecycle);
            Assert.Contains("ProcessPriorityOptions.Classes", ui);
            Assert.Contains("ProcessPriorityOptions.Classes", lifecycle);

            var list = Regex.Match(options, @"Classes\s*=\s*new\[\]\s*\{(?<items>[^}]*)\}", RegexOptions.Singleline);
            Assert.True(list.Success);
            string[] names = Regex.Matches(list.Groups["items"].Value, @"ProcessPriorityClass\.(\w+)").Cast<Match>().Select(x => x.Groups[1].Value).ToArray();
            Assert.Equal(new[] { "Normal", "AboveNormal", "High", "RealTime" }, names);
        }

        [Fact]
        public void TheLifecycle_IsRegistered_StartsInTheStoppedState_AndHasNoStaticEntryPoints()
        {
            var lifecycle = TestHost.GetService<IServiceLifecycle>();
            Assert.IsType<ServiceLifecycle>(lifecycle);
            Assert.Same(lifecycle, TestHost.GetService<IServiceLifecycle>());

            // 起動直後の状態（旧: running = false、loopControllers = true、inServiceTask = false、activeControllers = 0）
            var fresh = new ServiceLifecycle(TestLightbar.State, TestHost.GetService<DS4Windows.Processing.Abstractions.ILightbarService>(), TestHost.GetService<IAppSettingsService>(), TestHost.GetService<IEnvironmentService>(),
                TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IDs4DeviceRegistry>(),
                TestHost.GetService<IVirtualPadService>(), TestHost.GetService<ILatencyMonitor>(),
                TestHost.GetService<IDeviceConnector>(), TestHost.GetService<IDeviceSlotState>(),
                TestHost.GetService<IEventThreadDispatcher>(), TestHost.GetService<IOutputServicesLifecycle>());
            Assert.False(fresh.Running);
            Assert.True(fresh.LoopControllers);
            Assert.False(fresh.InServiceTask);
            Assert.Equal(0, fresh.ActiveControllers);
            Assert.False(fresh.Suspending);

            string text = SourceText.ReadCode("DS4Windows/Processing/Services/ServiceLifecycle.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, @"\bControlService\b"), "ServiceLifecycle が ControlService を参照している");
        }

        /// <summary>
        /// スリープの出入り（<see cref="IServiceLifecycle.Suspending"/>）は、ライトバーの停止中の印も同時に切り替える。
        /// 画面（MainWindow）はライトバーの状態を持たず、この窓口だけを操作する。
        /// </summary>
        [Fact]
        public void Suspending_SwitchesTheLightbarShuttingDownFlagTogether()
        {
            var lightbar = new DS4Windows.Core.Model.LightbarOverrides();
            var lifecycle = new ServiceLifecycle(lightbar, TestHost.GetService<DS4Windows.Processing.Abstractions.ILightbarService>(), TestHost.GetService<IAppSettingsService>(), TestHost.GetService<IEnvironmentService>(),
                TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IDs4DeviceRegistry>(),
                TestHost.GetService<IVirtualPadService>(), TestHost.GetService<ILatencyMonitor>(),
                TestHost.GetService<IDeviceConnector>(), TestHost.GetService<IDeviceSlotState>(),
                TestHost.GetService<IEventThreadDispatcher>(), TestHost.GetService<IOutputServicesLifecycle>());

            Assert.False(lightbar.shuttingdown);

            lifecycle.Suspending = true;
            Assert.True(lifecycle.Suspending);
            Assert.True(lightbar.shuttingdown);

            lifecycle.Suspending = false;
            Assert.False(lifecycle.Suspending);
            Assert.False(lightbar.shuttingdown);
        }
    }
}
