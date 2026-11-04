using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W0（Phase10-Step4d-Plan.md §4 W0・W5・W6）: 実行時の状態を<b>名前で持っている箇所</b>の数を、ファイルごとに固定する進捗の目安（ラチェット）。
    /// 対象は、プロファイル側の <c>ProfilePath</c>／<c>OlderProfilePath</c>（W5 で ID に替える）と、SA 側の <c>profileActions</c>／<c>ProfileActions</c>（W6 で ID に替える）。
    /// 件数が<b>実数と一致する</b>ことを検査する（<c>Phase7Step10eW5RatchetEqualityGuardTests</c> と同じ考え方）。W5・W6 で参照を ID に替えて件数が減ったら、基準値も同時に更新する。
    /// コメントの行は数えない。ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase10Step4dW0RuntimeKeyRatchetTests
    {
        // (ProfilePath＋OlderProfilePath の件数, profileActions＋ProfileActions の件数)
        private static readonly Dictionary<string, (int Path, int Actions)> Baseline = new Dictionary<string, (int, int)>
        {
            ["DS4Windows/Processing/Actions/ActionBindingBuilder.cs"] = (0, 3),
            ["DS4Windows/App.xaml.cs"] = (6, 0),
            ["DS4Windows/Core/Abstractions/IProfileRepository.cs"] = (2, 1),
            ["DS4Windows/Core/Abstractions/IProfileSettingsService.cs"] = (0, 1),
            ["DS4Windows/Core/Settings/BackingStore.cs"] = (9, 9),   // Phase10-Step4d W5b: 保管を ID にした窓口（ProfileSlotNames）の宣言と、登録簿との結び（AttachProfileIdentity）で 2 件増えた。名前の配列そのものは、もうない
            ["DS4Windows/Core/Persistence/Dto/AppSettingsDTO.cs"] = (18, 0),   // Phase10-Step4d W4: ID から現在の名前に引き直す代入で 2 件増えた（W5 で ID に替える）
            ["DS4Windows/Processing/Mapping.cs"] = (0, 3),
            ["DS4Windows/Processing/ProfileActions.cs"] = (0, 1),
            ["DS4Windows/Core/Persistence/AppSettingsXmlSerializer.cs"] = (6, 0),
            ["DS4Windows/Processing/Services/AutoProfileService.cs"] = (2, 0),
            ["DS4Windows/Processing/Services/ConnectionProfileSelector.cs"] = (1, 0),   // Phase9 9-E E-6a: ControlService から移した（合計は増えていない）
            ["DS4Windows/Processing/Services/InputReportProcessor.cs"] = (1, 0),   // Phase13-Step5b: ControlService.On_Report の本文を移した（合計は増えていない）
            ["DS4Windows/Processing/Services/IpcCommandProcessor.cs"] = (2, 0),   // Phase13-Step11 11-b: IPC コマンド（loadprofile／query）の実行を MainWindow.Ipc.cs から移した（合計は増えていない）
            ["DS4Windows/Core/Persistence/LegacyProfileXmlSerializer.cs"] = (1, 0),
            ["DS4Windows/Processing/Services/ProfileActionProvider.cs"] = (0, 2),
            ["DS4Windows/Processing/Services/ProfileApplicationService.cs"] = (7, 4),
            ["DS4Windows/Core/Persistence/ProfilePresets.cs"] = (0, 7),
            ["DS4Windows/Core/Persistence/ProfileRepository.cs"] = (6, 4),
            ["DS4Windows/Core/Persistence/ProfileSections/ProfileActionsLoadSection.cs"] = (0, 7),
            ["DS4Windows/Core/Persistence/ProfileSections/ProfileActionsSaveSection.cs"] = (0, 3),   // Phase10-Step4d W3: SA の固定 ID を書く繰り返しで 1 件増えた（W6 で ID に替える）
            ["DS4Windows/Core/Persistence/ProfileSections/ProfileMotionSaveSection.cs"] = (0, 2),
            ["DS4Windows/Core/Settings/ProfileSettingsService.cs"] = (0, 3),
            ["DS4Windows/Core/Persistence/SpecialActionRepository.cs"] = (0, 2),
            ["DS4Windows/UI/MainWindow.xaml.cs"] = (2, 0),   // Phase10-Step4d W7: 名前変更の追従で、スロットの配列を書き換える代入（ProfilePath／OlderProfilePath）を撤去して 2 件減った。Phase9 9-F F-2b: IPC の 2 件を MainWindow.Ipc.cs へ移した
            ["DS4Windows/UI/ProfileEditor.xaml.cs"] = (1, 4),
            ["DS4Windows/UI/ViewModels/CompositeDeviceModel.cs"] = (5, 0),
            ["DS4Windows/UI/ViewModels/ControllerListViewModel.cs"] = (3, 0),
            ["DS4Windows/UI/ViewModels/SpecialActionsListViewModel.cs"] = (0, 7),
            ["DS4Windows/UI/ViewModels/TrayIconViewModel.cs"] = (1, 0),
        };

        private static readonly Regex PathPattern = new Regex(@"\b[Pp]rofilePath\b|\b[Oo]lderProfilePath\b", RegexOptions.Compiled);
        private static readonly Regex ActionsPattern = new Regex(@"\b[Pp]rofileActions\b", RegexOptions.Compiled);

        private static string FindRoot()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            if (anchor == null) return null;
            // .../DS4Windows/Core/Settings/BackingStore.cs → リポジトリのルート
            return SourceFileLocator.RootFromAnchor(anchor);
        }

        [Fact]
        public void TheNumberOfNameKeyedRuntimeReferences_MatchesTheBaseline_PerFile()
        {
            string root = FindRoot();
            if (root == null) return;

            var actual = new Dictionary<string, (int Path, int Actions)>();
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (relative.Contains("/obj/") || relative.Contains("/bin/")) continue;

                int path = 0, actions = 0;
                foreach (string line in File.ReadLines(file))
                {
                    string t = line.TrimStart();
                    if (t.StartsWith("//") || t.StartsWith("*") || t.StartsWith("/*")) continue;

                    int comment = line.IndexOf("//", StringComparison.Ordinal);
                    string code = comment >= 0 ? line.Substring(0, comment) : line;
                    path += PathPattern.Matches(code).Count;
                    actions += ActionsPattern.Matches(code).Count;
                }

                if (path > 0 || actions > 0) actual[relative] = (path, actions);
            }

            var differences = new List<string>();
            foreach (string key in Baseline.Keys.Union(actual.Keys).OrderBy(k => k, StringComparer.Ordinal))
            {
                (int Path, int Actions) expected = Baseline.TryGetValue(key, out var b) ? b : (0, 0);
                (int Path, int Actions) found = actual.TryGetValue(key, out var a) ? a : (0, 0);
                if (!expected.Equals(found))
                    differences.Add($"{key}: 基準値 {expected} / 実数 {found}");
            }

            Assert.True(differences.Count == 0, "名前をキーにする実行時の参照の件数が基準値と違う（W5・W6 で ID に替えたなら、基準値を更新する。増やしてはならない）:\n" + string.Join("\n", differences));
        }
    }
}
