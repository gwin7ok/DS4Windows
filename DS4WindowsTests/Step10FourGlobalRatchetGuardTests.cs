using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-6: 全域の退行ガード（ラチェット）。<c>DS4Windows</c> 配下の全 <c>.cs</c> について、
    /// ファイルごとの <c>Program.rootHub</c>／<c>AppHost.GetService|Services</c>／<c>Global.*</c> の件数（コメントを除く）が、
    /// Step10-4 の完了時点（2026-10-15）の基準値以下であることを固定する。
    /// - 件数が減るのは許可する（基準値を更新して、さらに固定すること）。
    /// - 件数が増える、または基準値に無いファイルに現れる場合は失敗する（新しい直接参照を持ち込まない）。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// 基準値の内訳と経緯: <c>docs-forDIMG/MadeByAgent/Phase6-Step10-4-Plan.md</c> §34。
    /// </summary>
    public class Step10FourGlobalRatchetGuardTests
    {
        // (Program.rootHub, AppHost.GetService|Services, Global.*)
        private static readonly Dictionary<string, (int RootHub, int AppHost, int Global)> Baseline =
            new Dictionary<string, (int RootHub, int AppHost, int Global)>
        {
            ["DS4Windows/Actions/ActionManager.cs"] = (0, 0, 1),
            ["DS4Windows/Actions/DefaultActionManager.cs"] = (0, 0, 1),
            ["DS4Windows/Actions/DefaultMacroPlayer.cs"] = (0, 0, 1),
            ["DS4Windows/App.xaml.cs"] = (12, 4, 1),
            ["DS4Windows/BezierCurveEditor/BezierCurve.cs"] = (0, 0, 1),
            ["DS4Windows/DI/AppHost.cs"] = (0, 4, 0),
            ["DS4Windows/DI/ServiceRegistration.cs"] = (1, 0, 3),
            ["DS4Windows/DS4Control/ControlService.cs"] = (0, 1, 2),
            ["DS4Windows/DS4Control/DS4LightBar.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/ActionsDTO.cs"] = (0, 0, 6),
            ["DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs"] = (0, 0, 16),
            ["DS4Windows/DS4Control/DTOXml/AutoProfilesDTO.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/ProfileDTO.cs"] = (0, 0, 10),
            ["DS4Windows/DS4Control/Log.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Mapping.cs"] = (1, 6, 53),
            ["DS4Windows/DS4Control/Mouse.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/MouseCursor.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/OutputKBM/VirtualKBMFactory.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/OutputSlotPersist.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/ProfileMigration.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/ScpUtil.cs"] = (9, 12, 233),
            ["DS4Windows/DS4Control/Services/AppNotificationService.cs"] = (0, 0, 6),
            ["DS4Windows/DS4Control/Services/AppSettingsService.cs"] = (0, 0, 185),
            ["DS4Windows/DS4Control/Services/AppearanceSettingsService.cs"] = (0, 0, 8),
            ["DS4Windows/DS4Control/Services/AutoProfileService.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/DeviceStateService.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/Services/Ds4DeviceRegistryAdapter.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/EnvironmentService.cs"] = (0, 0, 21),
            ["DS4Windows/DS4Control/Services/OutputKBMHandlerAdapter.cs"] = (0, 0, 21),
            ["DS4Windows/DS4Control/Services/OutputKBMHandlerLifecycle.cs"] = (0, 0, 8),
            ["DS4Windows/DS4Control/Services/OutputSlotService.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/Services/PathService.cs"] = (0, 0, 8),
            ["DS4Windows/DS4Control/Services/ProfileApplicationService.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/Services/ProfilePresetLoader.cs"] = (0, 0, 10),
            ["DS4Windows/DS4Control/Services/ProfileRepository.cs"] = (0, 0, 24),
            ["DS4Windows/DS4Control/Services/ProfileSettingsService.cs"] = (0, 0, 5),
            ["DS4Windows/DS4Control/Services/ProfileSlotApplier.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/ProfileXmlStore.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/SyntheticDispatcher.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/Util.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Xbox360OutDevice.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/MainWindow.xaml.cs"] = (0, 17, 7),
            ["DS4Windows/DS4Forms/ProfileEditor.xaml.cs"] = (1, 4, 37),
            ["DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs"] = (0, 0, 12),
            ["DS4Windows/DS4Library/DS4Device.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Library/DS4Devices.cs"] = (0, 0, 6),
            ["DS4Windows/DS4Library/DS4Sixaxis.cs"] = (1, 0, 1),
            ["DS4Windows/DS4Library/DS4State.cs"] = (0, 0, 4),
            ["DS4Windows/HidLibrary/HidDevices.cs"] = (0, 0, 2),
        };

        private static readonly Regex RootHubPattern = new Regex(@"Program\.rootHub", RegexOptions.Compiled);
        private static readonly Regex AppHostPattern = new Regex(@"AppHost\.(?:GetService|Services)", RegexOptions.Compiled);
        private static readonly Regex GlobalPattern = new Regex(@"(?<![\w.])Global\.\w+", RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Step10FourGlobalRatchetGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void DirectReferences_DoNotExceedStep10FourBaseline()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/ScpUtil.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // <root>/DS4Windows/DS4Control/ScpUtil.cs → <root>
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string relative = "DS4Windows/" + Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                string text = StripComments(File.ReadAllText(file));

                int rootHub = RootHubPattern.Matches(text).Count;
                int appHost = AppHostPattern.Matches(text).Count;
                int global = GlobalPattern.Matches(text).Count;
                if (rootHub == 0 && appHost == 0 && global == 0) continue;

                Baseline.TryGetValue(relative, out var allowed);
                if (rootHub > allowed.RootHub)
                    violations.Add($"{relative}: Program.rootHub {rootHub} 件（基準 {allowed.RootHub}）");
                if (appHost > allowed.AppHost)
                    violations.Add($"{relative}: AppHost.GetService|Services {appHost} 件（基準 {allowed.AppHost}）");
                if (global > allowed.Global)
                    violations.Add($"{relative}: Global.* {global} 件（基準 {allowed.Global}）");
            }

            foreach (string v in violations) _output.WriteLine(v);
            Assert.True(violations.Count == 0,
                "基準値を超える直接参照がある（新しい Global／AppHost／Program.rootHub の参照を持ち込まず、依存を注入すること）:\n" +
                string.Join("\n", violations));
        }
    }
}
