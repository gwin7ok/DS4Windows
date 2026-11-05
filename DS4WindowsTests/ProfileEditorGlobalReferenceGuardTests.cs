using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step8: <c>ProfileEditor.xaml.cs</c> の Global 直接参照の解消を固定するソース走査ガード。
    /// 解消したグループを Step8-2 以降で順に <see cref="ResolvedMembers"/> へ追加していく
    /// （最終的に、定数［TEST_PROFILE_INDEX／ASSEMBLY_RESOURCE_PREFIX／RESOURCES_PREFIX］以外の
    /// <c>Global.</c> 参照が 0 件になる。Phase6-Step8-Plan.md §0A.2）。
    /// </summary>
    public class ProfileEditorGlobalReferenceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileEditorGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>解消済みのメンバー（Global 経由で再び参照してはならないもの）。</summary>
        private static readonly string[] ResolvedMembers =
        {
            // Step8-2（G1: レイアウト・言語）→ IAppSettingsService
            "ProfileEditorLeftWidth",
            "ProfileEditorRightWidth",
            "SpecialActionNameColWidth",
            "SpecialActionTriggerColWidth",
            "SpecialActionDetailColWidth",
            "FormWidth",
            "FormHeight",
            "UseLang",
            // Step8-3（G2〜G8）→ IProfileRepository／IProfileSettingsService／各 ViewModel
            "ProfilePath",
            "LoadBlankDevProfile",
            "ProfileActions",
            "LSModInfo",
            "RSModInfo",
            "outDevTypeTemp",
            "SaveProfile",
            "InverseRumbleMotors",
            "OutContType",
            "ProfileSettingsServiceInstance",
            "GetAction",
            "CacheExtraProfileInfo",
            "CacheProfileCustomsFlags",
            "appdatapath",
        };

        [Fact]
        public void ProfileEditor_ReceivesAppSettingsServiceByConstructor_AndMainWindowPassesIt()
        {
            // Phase7-Step5 W5: 旧 AppHost.GetService の 4 件をコンストラクター引数に替えた
            // Phase14d-Step3: 引数の並びは、ソースの文字列ではなくリフレクションで確かめる（書式に依存しない）
            Type[] ps = typeof(DS4Windows.UI.ProfileEditor).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray();
            Assert.Equal(typeof(int), ps[0]);
            Assert.Equal(typeof(DS4Windows.Core.Abstractions.IAppSettingsService), ps[1]);
            Assert.Equal(typeof(DS4Windows.Core.Abstractions.IProfileSettingsService), ps[2]);
            Assert.Equal(typeof(DS4Windows.UI.Abstractions.IEditorWindowFactory), ps[3]);

            // 受けた設定の使い方と、null の扱い・画面の開き方（コードの形。WPF の画面は単体テストで開けないため、ソースで確かめる）
            string editor = SourceText.ReadCode("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (editor == null) return;
            Assert.Contains("throw new ArgumentNullException(nameof(appSettingsService))", editor);
            Assert.Contains("appSettingsService.UseLang", editor);
            string main = SourceText.ReadCode("DS4Windows/UI/MainWindow.xaml.cs");
            Assert.Contains("editorWindowFactory.CreateProfileEditor(targetDevice)", main);
        }
    }
}
