#nullable enable
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Conditioning;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力レポート 1 件の処理（<see cref="InputReportProcessor.OnReport"/>）の順序と条件を、呼び出しの記録で確かめる。
    /// 遅延の監視 → タッチ切り替え → カーブ・デッドゾーン → マッピング → 仮想パッドへの送出 → 合成入力の確定 → ライトバー。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class InputReportOrderTests : System.IDisposable
    {
        // 設定の保管は共有のため、タッチ切り替えは各テストの前に既定へ戻し、終わりにも戻す
        public InputReportOrderTests() => Reset();
        public void Dispose() => Reset();
        private static void Reset() => TestRepositoryFactory.CreateProfileSettingsService().ForSlot(Slot).General.EnableTouchToggle = BackingStore.DEFAULT_TOUCH_TOGGLE;

        private const int Slot = 2;

        private sealed class Rig
        {
            public readonly List<string> Order = new List<string>();
            public readonly IProfileSettingsService Settings = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly RecordingProxy ActionProvider;
            public readonly InputReportProcessor Processor;
            public readonly RecordingConnectDevice Device = RecordingConnectDevice.Create();

            private T Fake<T>(string prefix, System.Action<RecordingProxy>? configure = null) where T : class
            {
                var fake = RecordingProxy.Create<T>();
                var proxy = RecordingProxy.Of(fake);
                proxy.OnCall = name => Order.Add(prefix + "." + name);
                configure?.Invoke(proxy);
                ProxyOf[typeof(T)] = proxy;
                return fake;
            }

            private readonly Dictionary<System.Type, RecordingProxy> ProxyOf = new Dictionary<System.Type, RecordingProxy>();

            public Rig()
            {
                Device.InitStates();
                Settings.UseDInputOnlyArray[Slot] = false;
                var mapping = Fake<IMapping>("mapping", p => p.Returns["SetCurveAndDeadzone"] = new ControllerState());
                var provider = Fake<IProfileActionProvider>("actions");
                ActionProvider = ProxyOf[typeof(IProfileActionProvider)];
                var appearance = Fake<IAppearanceSettingsService>("appearance");
                var appSettings = Fake<IAppSettingsService>("appSettings");
                Processor = new InputReportProcessor(Fake<ILightbarService>("lightbar"), new DeviceSlotState(),
                    Fake<ILatencyMonitor>("latency"), new FirstReportActionLogger(TestHost.GetService<IPathService>(), TestRepositoryFactory.CreateProfileRepository(Settings)),
                    appearance, Settings, mapping, Fake<ITouchpadToggleService>("toggle"), provider, appSettings,
                    Fake<IOscService>("osc"), Fake<IVirtualPadService>("virtualPad"));
            }

            public void UseCustomActions() => ActionProvider.Returns["GetProfileActionCount"] = 1;
        }

        [Fact]
        public void TheReport_RunsTheStagesInOrder_ToTheLightbarLast()
        {
            var rig = new Rig();
            rig.Settings.ForSlot(Slot).General.EnableTouchToggle = true;
            rig.UseCustomActions();

            rig.Processor.OnReport(rig.Device, System.EventArgs.Empty, Slot);

            string[] expected =
            {
                "latency.OnReport", "toggle.CheckForToggle", "mapping.SetCurveAndDeadzone", "mapping.MapCustom",
                "virtualPad.SendReport", "mapping.Commit", "lightbar.Update",
            };
            Assert.Equal(expected, rig.Order.Where(expected.Contains).ToArray());
        }

        [Fact]
        public void WithoutCustomActions_TheMappingStageIsSkipped_ButTheOutputStillRuns()
        {
            var rig = new Rig();
            rig.Settings.ForSlot(Slot).General.EnableTouchToggle = false;

            rig.Processor.OnReport(rig.Device, System.EventArgs.Empty, Slot);

            Assert.DoesNotContain("mapping.MapCustom", rig.Order);
            Assert.Contains("mapping.SetCurveAndDeadzone", rig.Order);
            Assert.Contains("virtualPad.SendReport", rig.Order);
            Assert.DoesNotContain("toggle.CheckForToggle", rig.Order);   // タッチ切り替えは、有効なときだけ
        }

        [Fact]
        public void WhileRecordingAMacro_TheMappingStageIsSkipped()
        {
            var rig = new Rig();
            rig.UseCustomActions();
            rig.Processor.RecordingMacro = true;

            rig.Processor.OnReport(rig.Device, System.EventArgs.Empty, Slot);

            Assert.DoesNotContain("mapping.MapCustom", rig.Order);
        }

        [Fact]
        public void ADInputOnlySlot_SendsNothingToTheVirtualPad_ButStillCommitsAndUpdatesTheLightbar()
        {
            var rig = new Rig();
            rig.Settings.UseDInputOnlyArray[Slot] = true;

            rig.Processor.OnReport(rig.Device, System.EventArgs.Empty, Slot);

            Assert.DoesNotContain("virtualPad.SendReport", rig.Order);
            Assert.Contains("mapping.Commit", rig.Order);
            Assert.Contains("lightbar.Update", rig.Order);
        }

        [Fact]
        public void AReportWithoutASlot_IsIgnored()
        {
            var rig = new Rig();

            rig.Processor.OnReport(rig.Device, System.EventArgs.Empty, -1);

            Assert.Empty(rig.Order);
        }
    }
}
