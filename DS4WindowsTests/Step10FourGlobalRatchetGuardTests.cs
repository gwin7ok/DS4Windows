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
            // Phase6-Step10b W4: ActionManager.cs の Global 1 件は ActionEntry.cs へ移った（合計は不変）
            ["DS4Windows/Actions/ActionEntry.cs"] = (0, 0, 1),
            ["DS4Windows/Actions/DefaultActionManager.cs"] = (0, 0, 1),
            ["DS4Windows/Actions/DefaultMacroPlayer.cs"] = (0, 0, 1),
            ["DS4Windows/App.xaml.cs"] = (12, 4, 1),
            ["DS4Windows/BezierCurveEditor/BezierCurve.cs"] = (0, 0, 1),
            // Phase6-Step10b W2: DS4Windows.AppHost（互換ラッパー）を DI/Compat/AppHost.cs へ分離。件数を引き継いだ（4＝2＋2。合計は不変）
            ["DS4Windows/DI/AppHost.cs"] = (0, 2, 0),
            ["DS4Windows/DI/Compat/AppHost.cs"] = (0, 2, 0),
            ["DS4Windows/DI/ServiceRegistration.cs"] = (1, 0, 3),
            ["DS4Windows/DS4Control/ControlService.cs"] = (0, 1, 2),
            ["DS4Windows/DS4Control/DS4LightBar.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs"] = (0, 0, 1),
            // Phase6-Step10b W1: 1 ファイル 1 型の分割に伴い、件数を新設ファイルへ引き継いだ（合計は不変: ActionsDTO 6＝4＋2、AutoProfilesDTO 4＝0＋4、ProfileDTO 10＝2＋8）
            ["DS4Windows/DS4Control/DTOXml/ActionsDTO.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs"] = (0, 0, 16),
            ["DS4Windows/DS4Control/DTOXml/AutoProfileEntrySerializer.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlButtonAssignmentSerializer.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlExtrasAssignmentSerializer.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlKeyAssignmentSerializer.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlKeyTypeAssignmentSerializer.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlLightbarMacroAssignmentSerializer.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/DS4ControlMacroAssignmentSerializer.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/ProfileDTO.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/DTOXml/SpecialActionSerializer.cs"] = (0, 0, 2),
            // Phase6-Step10b W4: Log.cs（AppLogger／ProfileChangeSource／ProfileChangedEventArgs）は型ごとに分割。Global 1 件は AppLogger.cs
            ["DS4Windows/DS4Control/AppLogger.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Mapping.cs"] = (1, 6, 53),
            ["DS4Windows/DS4Control/Mouse.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/MouseCursor.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/OutputKBM/VirtualKBMFactory.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/OutputSlotPersist.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/ProfileMigration.cs"] = (0, 0, 4),
            // Phase6-Step10b W5: ScpUtil.cs（29 型）を型ごとに分割。件数を引き継いだ（Global 233＝Global 47＋BackingStore 182＋Changelog 4、AppHost 12＝11＋1。rootHub 9 は Global。合計は不変）
            ["DS4Windows/DS4Control/BackingStore.cs"] = (0, 1, 182),
            ["DS4Windows/DS4Control/Changelog.cs"] = (0, 0, 4),
            // Phase6-Step12 Wave 1: 孤立コード 24 件を削除（Global 47 → 45）。Wave 2: 互換アクセサ 152 件を削除（Global 45 → 38）。Wave 3: *Instance のフォールバック群 19 件を削除（Global 38 → 26、AppHost 11 → 5、rootHub 9 → 6）。Wave 4: BackingStore 直転送ラッパー 58 件を削除（AppHost 5 → 4。(6, 4, 26)）
            ["DS4Windows/DS4Control/Global.cs"] = (6, 4, 26),
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
            // 26: 名前変更の伝播（ReplaceProfileSwitchTarget。Phase6-Step10-4-5d）で、Global.store への委譲を 2 件追加（薄いシムの性質。24 → 26）
            ["DS4Windows/DS4Control/Services/ProfileRepository.cs"] = (0, 0, 26),
            ["DS4Windows/DS4Control/Services/ProfileSettingsService.cs"] = (0, 0, 5),
            ["DS4Windows/DS4Control/Services/ProfileSlotApplier.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/ProfileXmlStore.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/SyntheticDispatcher.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/Util.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Xbox360OutDevice.cs"] = (0, 0, 1),
            // Phase6-Step10b W3: 1 ファイル 1 型の分割に伴い、件数を新設ファイルへ引き継いだ（合計は不変）
            //   MainWindow 7＝1＋ImageLocationPaths 6、ProfileEditor 37＝32＋ResourcePaths 5、ControllerListViewModel 4＝0＋CompositeDeviceModel 4、
            //   MappingListViewModel 3＝0＋MappedControl 3、RecordBoxViewModel 7＝1＋MacroStepItem 6
            ["DS4Windows/DS4Forms/ImageLocationPaths.cs"] = (0, 0, 6),
            ["DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/MainWindow.xaml.cs"] = (0, 17, 1),
            ["DS4Windows/DS4Forms/ProfileEditor.xaml.cs"] = (1, 4, 32),
            ["DS4Windows/DS4Forms/ResourcePaths.cs"] = (0, 0, 5),
            ["DS4Windows/DS4Forms/ViewModels/CompositeDeviceModel.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Forms/ViewModels/MacroStepItem.cs"] = (0, 0, 6),
            ["DS4Windows/DS4Forms/ViewModels/MappedControl.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs"] = (0, 0, 12),
            ["DS4Windows/DS4Library/DS4Device.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Library/DS4Devices.cs"] = (0, 0, 6),
            // Phase6-Step10b W4: DS4Sixaxis.cs は DS4SixAxis.cs へ改名（型名に合わせた大文字・小文字の変更。Dictionary は大文字・小文字を区別する）
            ["DS4Windows/DS4Library/DS4SixAxis.cs"] = (1, 0, 1),
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
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // <root>/DS4Windows/DS4Control/Global.cs → <root>
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
