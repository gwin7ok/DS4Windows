using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10a W1・W2（Q2＝A）: <c>BackingStore</c> と制御層・DTO・サービス・Actions が、コントローラー数などの定数を
    /// <c>Global</c> ではなく <c>ControllerLimits</c> から取ることと、再宣言された定数が <c>ControllerLimits</c> と一致することの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10aW2ControllerLimitsGuardTests
    {
        private static readonly string[] Files =
        {
            "DS4Windows/Core/Settings/BackingStore.cs",
            "DS4Windows/Processing/Actions/ActionEntry.cs",
            "DS4Windows/Processing/Actions/DefaultActionManager.cs",
            "DS4Windows/Processing/Actions/DefaultMacroPlayer.cs",
            "DS4Windows/App.xaml.cs",
            "DS4Windows/Processing/ControlService.cs",
            "DS4Windows/Input/DS4LightBar.cs",
            "DS4Windows/Core/Persistence/Dto/AppSettingsDTO.cs",
            "DS4Windows/Core/Persistence/Dto/AutoProfileEntrySerializer.cs",
            "DS4Windows/Output/OutputSlotManager.cs",
            "DS4Windows/Core/Profiles/AutoProfileEntity.cs",
            "DS4Windows/Processing/Services/DeviceStateService.cs",
            "DS4Windows/Core/Platform/EnvironmentService.cs",
            "DS4Windows/Output/Services/OutputSlotService.cs",
            "DS4Windows/UI/MainWindow.xaml.cs",
            "DS4Windows/UI/MainWindow.Ipc.cs",   // Phase9 9-F F-2b: IPC の処理を移した
            "DS4Windows/UI/ProfileEditor.xaml.cs",
        };

        private static readonly Regex Constant = new Regex(
            @"(?<![\w])(?:DS4Windows\.)?Global\.(MAX_DS4_CONTROLLER_COUNT|TEST_PROFILE_ITEM_COUNT|TEST_PROFILE_INDEX|OLD_XINPUT_CONTROLLER_COUNT|DS4_STICK_AXIS_MIDPOINT)\b",
            RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Phase7Step10aW2ControllerLimitsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheFiles_DoNotReferenceTheGlobalConstants()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs"); // Phase7-Step10d W5: Global.cs は削除したため、同じフォルダーの BackingStore.cs を起点にする
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = SourceFileLocator.RootFromAnchor(anchor);
            var offenders = Files
                .Where(f => Constant.IsMatch(SourceText.StripComments(File.ReadAllText(Path.Combine(root, f)))))
                .ToArray();
            Assert.True(offenders.Length == 0, "Global の定数への参照が残っている: " + string.Join(", ", offenders));
        }

        [Fact]
        public void TheRedeclaredConstants_MatchControllerLimits()
        {
            Assert.Equal(ControllerLimits.MaxControllerCount, ControlService.MAX_DS4_CONTROLLER_COUNT);
            Assert.Equal(ControllerLimits.MaxControllerCount, ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT);
            Assert.Equal(ControllerLimits.TestProfileItemCount, ProfileSettingsService.TEST_PROFILE_ITEM_COUNT);
        }
    }
}
