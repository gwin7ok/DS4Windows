using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-4／3-5: `Mapping.SetCurveAndDeadzone` のスティック・トリガー系（Step3-4 の27件）とジャイロ系 SX/SZ（Step3-5 の13件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// `ApplyStickCalibration`（現 `StickInputConditioner.ApplyDrift`）と `Commit` は `MappingCommitAndCalibrationGlobalReferenceGuardTests` が担当する。
    /// </summary>
    public class MappingSetCurveAndDeadzoneGlobalReferenceGuardTests
    {
        private const string StartMarker = "public DS4State SetCurveAndDeadzone(";
        // Phase9 9-A(2)-B: ApplyStickCalibration を StickInputConditioner へ移したため、次のメンバー（Map DS4 Buttons/Axes の文書コメント）を終端にする
        private const string EndMarker = "/// Map DS4 Buttons/Axes to other DS4 Buttons/Axes";

        private readonly ITestOutputHelper _output;

        public MappingSetCurveAndDeadzoneGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた `SetCurveAndDeadzone` 本体のテキストを返す。見つからなければ null。</summary>
        private string ReadMethodSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/Processing/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            int start = text.IndexOf(StartMarker, StringComparison.Ordinal);
            int end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, "SetCurveAndDeadzone が見つからない（メソッド名の変更？）");
            Assert.True(end > start, "次のメンバー（Map DS4 Buttons/Axes）が SetCurveAndDeadzone の後ろに見つからない（並び順の変更？）");

            string body = text.Substring(start, end - start);
            body = Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            body = Regex.Replace(body, @"//[^\r\n]*", string.Empty);
            return body;
        }

        [Fact]
        public void SetCurveAndDeadzone_TakesProfileSettingsAsParameter()
        {
            string body = ReadMethodSourceWithoutComments();
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }
    }
}