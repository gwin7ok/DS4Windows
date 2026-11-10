using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step5 W3 以降: Service Locator の解消（Q4〜Q9）の回帰ガード。Wave ごとに追記する。
    /// - W3: 出力スロットの契約（<c>LoadOutputSlots</c>／<c>SaveOutputSlots</c>）の接続、<c>Global.ProfileXmlStoreInstance</c> と
    ///   <c>Global.NotificationServiceInstance</c> の削除（<c>AppLogger.NotificationService</c> への置き換え）。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>Phase7-Step5-Plan.md</c>。
    /// </summary>
    public class ServiceLocatorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ServiceLocatorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void OutputSlotPersistence_GoesThroughTheContract()
        {
            string app = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            string control = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            if (app == null || control == null) return;

            Assert.Contains("_outputSlotService.LoadOutputSlots()", app);
            Assert.DoesNotContain("LoadPermanentSlotsConfig", app);
            Assert.DoesNotContain("LoadPermanentSlotsConfig", control);
            Assert.Contains("_outputSlotService.SaveOutputSlots()", control);
            Assert.DoesNotContain("OutputSlotPersist.", control);
        }

        [Fact]
        public void ControlServiceAndBackingStore_ReceiveTheirCollaborators()
        {
            // Phase14d-Step3: ソースの文字列ではなく、コンストラクターの引数と、コンテナが組み立てた結果を確かめる（AppHost を使わないことは RemovedApiRuleTests）
            Assert.Contains(typeof(DS4Windows.Processing.Services.DeviceRegistryHooks).GetConstructors().Single().GetParameters(),
                p => p.ParameterType == typeof(DS4Windows.Core.Abstractions.IElevatedProcessLauncher));
            var store = TestHost.GetService<DS4Windows.Core.Settings.BackingStore>();
            Assert.Same(TestHost.GetService<DS4Windows.Core.Abstractions.IProcessInspector>(), store.ProcessInspector);
        }

        [Fact]
        public void App_ResolvesServicesOnlyThroughTheSingleEntryPoint()
        {
            string app = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            if (app == null) return;

            // AppHost.GetService は ResolvePostHostService の 1 件だけ。ServiceProviderHolder も使わない
            Assert.Single(Regex.Matches(app, @"AppHost\.GetService"));
            Assert.Contains("_logSettingsApplierProxy.Attach(logHolder)", app);
        }

        [Fact]
        public void MainWindowAndProfileEditor_DoNotUseTheLocator_AndAreCreatedThroughFactories()
        {
            string main = SourceText.ReadCode("DS4Windows/UI/MainWindow.xaml.cs");
            string editor = SourceText.ReadCode("DS4Windows/UI/ProfileEditor.xaml.cs");
            string app = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            string registration = RegistrationSource.ReadAll(stripComments: true);   // Phase14c-Step6（R-18）: 層ごとの partial ファイルをつないだもの（コメントは除く）
            if (main == null || editor == null || app == null || registration == null) return;

            Assert.DoesNotContain("AppHost", main);
            Assert.DoesNotContain("AppHost", editor);
            Assert.DoesNotContain("new MainWindow(", app);
            Assert.DoesNotContain("new DS4Windows.UI.MainWindow(", app);
            Assert.Contains("ResolvePostHostService<IMainWindowFactory>().Create(parser)", app);
            Assert.Contains("services.AddSingleton<IMainWindowFactory, MainWindowFactory>();", registration);
        }

        [Fact]
        public void AppLogger_UsesItsOwnNotificationService_SetByTheApp()
        {
            string logger = SourceText.ReadCode("DS4Windows/Core/Logging/AppLogger.cs");
            string app = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            if (logger == null || app == null) return;

            Assert.DoesNotContain("NotificationServiceInstance", logger);
            Assert.Matches(@"public\s+static\s+DS4Windows\.Core\.Abstractions\.INotificationService\s+NotificationService", logger);
            Assert.Matches(@"AppLogger\.NotificationService\s*=\s*ResolvePostHostService<INotificationService>\(\)", app);
        }
    }
}
