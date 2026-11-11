using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step11 11-c（台帳 F-2c）: IPC コマンドの実行を、UI なしで検査する。受け付けの順序、引数の個数の条件、範囲の検査は旧 <c>MainWindow.Ipc.cs</c> のまま。
    /// 実サービスは <see cref="TestHost"/> から取り、UI に頼む操作は記録するだけの <see cref="RecordingHost"/> で受ける。
    /// </summary>
    public class IpcCommandProcessorTests
    {
        private class RecordingHost : IIpcCommandHost
        {
            public List<string> Calls = new List<string>();
            public string QueryResult;
            public void ToggleService() => Calls.Add("toggle");
            public void CloseApplication() => Calls.Add("close");
            public void DisconnectAllControllers() => Calls.Add("disconnectAll");
            public void DisconnectController(int index) => Calls.Add("disconnect:" + index);
            public void SetLightColor(int index, byte red, byte green, byte blue) => Calls.Add($"led:{index}:{red}:{green}:{blue}");
            public int FindProfileIndex(string profileName) => -1;
            public string GetProfileName(int index) => null;
            public int ControllerCount => 0;
            public IControllerSlotView GetControllerDevice(int index) => null;
            public void LogDebug(string message) => Calls.Add("log");
        }

        private sealed class RecordingWriter : IIpcResultWriter
        {
            private readonly RecordingHost _host;
            public RecordingWriter(RecordingHost host) { _host = host; }
            public void WriteQueryResult(string value) { _host.QueryResult = value; _host.Calls.Add("query"); }
        }

        private static IpcCommandProcessor Create(RecordingHost host = null) => new IpcCommandProcessor(
            TestHost.GetService<IServiceLifecycle>(), TestHost.GetService<IOutputSlotService>(), TestHost.GetService<IProfileRepository>(),
            TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IProfileApplicationService>(), TestHost.GetService<IProfileLoadTarget>(),
            TestHost.GetService<IPathService>(), TestHost.GetService<IDeviceSlotState>(), TestHost.GetService<IVirtualPadService>(),
            new RecordingWriter(host ?? new RecordingHost()));

        private static RecordingHost Run(params string[] command)
        {
            var host = new RecordingHost();
            Create(host).Execute(host, command);
            return host;
        }

        private static bool Running => TestHost.GetService<IServiceLifecycle>().Running;

        [Fact]
        public void Start_TogglesOnlyWhenNotRunning_Stop_TogglesOnlyWhenRunning()
        {
            // テストのホストではサービスは動いていない
            Assert.False(Running);
            Assert.Equal(new[] { "toggle" }, Run("start").Calls);
            Assert.Empty(Run("stop").Calls);
        }

        [Fact]
        public void Cycle_AlwaysToggles_Shutdown_ClosesTheApplication()
        {
            Assert.Equal(new[] { "toggle" }, Run("cycle").Calls);
            Assert.Equal(new[] { "close" }, Run("shutdown").Calls);   // 動いていないので切り替えはしない
        }

        [Fact]
        public void Disconnect_WithoutArgument_DisconnectsAll_WithArgument_UsesTheZeroBasedIndex()
        {
            Assert.Equal(new[] { "disconnectAll" }, Run("disconnect").Calls);
            Assert.Equal(new[] { "disconnect:0" }, Run("disconnect", "1").Calls);
            Assert.Equal(new[] { "disconnect:2" }, Run("disconnect", "3").Calls);
            Assert.Equal(new[] { "disconnect:0" }, Run("disconnect", "abc").Calls);   // 解釈できないときは、旧コードと同じく out の既定値 0（decrement しない）
        }

        [Fact]
        public void ChangeLedColor_NeedsFiveElements_AndARangeCheckedSlot()
        {
            Assert.Equal(new[] { "led:0:255:0:10" }, Run("changeledcolor", "1", "255", "0", "10").Calls);
            Assert.Empty(Run("changeledcolor", "1", "255", "0").Calls);                       // 引数が足りない
            Assert.Empty(Run("changeledcolor", "0", "1", "2", "3").Calls);                    // スロット番号は 1 から
            Assert.Empty(Run("changeledcolor", (DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount + 1).ToString(), "1", "2", "3").Calls);
            Assert.Equal(new[] { "led:0:0:0:0" }, Run("changeledcolor", "1", "x", "300", "-1").Calls);   // 解釈できない値は 0
        }

        [Fact]
        public void LoadProfile_NeedsThreeElements_AndAnExistingProfileFile()
        {
            Assert.Empty(Run("loadprofile", "1").Calls);
            Assert.Empty(Run("loadprofile", "1", "NoSuchProfile_" + Guid.NewGuid().ToString("N")).Calls);
            Assert.Empty(Run("loadtempprofile", "1", "NoSuchProfile_" + Guid.NewGuid().ToString("N")).Calls);
        }

        [Fact]
        public void OutputSlot_WithAnOutOfRangeSlotOrAnUnknownAction_DoesNothing()
        {
            Assert.Empty(Run("outputslot", "0", "unplug").Calls);
            Assert.Empty(Run("outputslot", "1").Calls);
            Assert.Null(Record.Exception(() => Run("outputslot", "1", "unknownaction")));
        }

        [Fact]
        public void Query_WritesTheResult_AndTheValueIsEmptyForAnUnknownNameOrAnOutOfRangeSlot()
        {
            RecordingHost running = Run("query", "1", "apprunning");
            Assert.Equal(new[] { "query" }, running.Calls);
            Assert.Equal(Running.ToString(), running.QueryResult);

            Assert.Equal(string.Empty, Run("query", "1", "nosuchproperty").QueryResult);
            Assert.Equal(string.Empty, Run("query", "0", "apprunning").QueryResult);
            Assert.Equal(string.Empty, Run("query", "1", "battery").QueryResult);          // 未接続のスロット
            Assert.Empty(Run("query", "1").Calls);                                          // 引数が足りないコマンドは何もしない（結果も書かない）
        }

        [Fact]
        public void UnknownOrEmptyCommands_DoNothing()
        {
            Assert.Empty(Run("nosuchcommand").Calls);
            Assert.Empty(Run(new string[0]).Calls);
            Assert.Null(Record.Exception(() => Create().Execute(new RecordingHost(), null)));
        }

        [Fact]
        public void NullHostOrDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Create().Execute(null, new[] { "start" }));
            Assert.Equal("lifecycle", Assert.Throws<ArgumentNullException>(() => new IpcCommandProcessor(
                null, TestHost.GetService<IOutputSlotService>(), TestHost.GetService<IProfileRepository>(),
                TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IProfileApplicationService>(), TestHost.GetService<IProfileLoadTarget>(),
                TestHost.GetService<IPathService>(), TestHost.GetService<IDeviceSlotState>(), TestHost.GetService<IVirtualPadService>(),
                TestHost.GetService<IIpcResultWriter>())).ParamName);
        }

        [Fact]
        public void TheProcessor_IsASingleton_AndMainWindowIsTheHost_WithNoIpcMethodsLeft()
        {
            Assert.IsType<IpcCommandProcessor>(TestHost.GetService<IIpcCommandProcessor>());
            Assert.Same(TestHost.GetService<IIpcCommandProcessor>(), TestHost.GetService<IIpcCommandProcessor>());
            Assert.True(typeof(IIpcCommandHost).IsAssignableFrom(typeof(MainWindow)));

            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var leftover = typeof(MainWindow).GetMethods(all).Select(m => m.Name).Where(n => n.StartsWith("Ipc")).ToList();
            Assert.True(leftover.Count == 0, "MainWindow に旧 Ipc* が残っている: " + string.Join(", ", leftover));
        }
    }
}
