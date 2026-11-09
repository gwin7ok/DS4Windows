using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step6-3: `ProfileEditor.xaml.cs` の `Reload` と `RefreshEditorBindings`（プリセット適用後）が、
    /// プロファイル読み込み後の出力種別でマッピング一覧のボタン名表記を明示的に揃えている
    /// （`mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType)` を呼ぶ）ことを固定する回帰ガード。
    /// `mappingListVM` はプロファイル読み込み前の出力種別で生成されるため、この呼び出しがないと、
    /// 出力種別コンボボックスの SelectionChanged が発生しない組み合わせでボタン名が食い違う（Phase6-Step6-Plan.md §0.1）。
    /// `ProfileEditor` は WPF の UserControl で単体駆動が困難なため、ソース走査で担保する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class ProfileEditorMappingDevTypeGuardTests
    {
        private static readonly string[] MethodMarkers =
        {
            "public void Reload(",
            "private void RefreshEditorBindings(",
        };

        private const string RequiredCall = "mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType)";

        private readonly ITestOutputHelper _output;

        public ProfileEditorMappingDevTypeGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ReloadAndRefreshEditorBindings_UpdateMappingDevTypeAfterLoading()
        {
            string text = SourceText.ReadCode("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (text == null) return;

            foreach (string marker in MethodMarkers)
            {
                string body = SourceText.Method(text, marker);
                int updateLate = body.IndexOf("profileSettingsVM.UpdateLateProperties()", StringComparison.Ordinal);
                int devType = body.IndexOf(RequiredCall, StringComparison.Ordinal);

                Assert.True(devType >= 0, $"{marker} が {RequiredCall} を呼んでいない");
                Assert.True(updateLate >= 0 && devType > updateLate,
                    $"{marker} で {RequiredCall} が UpdateLateProperties() の後に呼ばれていない（読み込み後の出力種別を使うため）");
            }
        }
    }
}
