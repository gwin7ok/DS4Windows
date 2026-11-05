using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-5: `Mapping.ApplyStickCalibration`（ドリフト補正 8件）と `Mapping.Commit`（出力確定 24件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// `Commit` は出力先の `IVirtualKBM` を実 DI ホストから解決しており、テストから実行すると実際に入力を送出してしまうため、
    /// 挙動テストの代わりに本ガード（ソース走査）と実機確認で担保する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingCommitAndCalibrationGlobalReferenceGuardTests
    {
        // Phase9 9-A(2)-B: ドリフト補正は Mapping.ApplyStickCalibration から StickInputConditioner.ApplyDrift へ移した（検査の対象も、移動先のファイルにした）
        private const string CalibrationPath = "DS4Windows/Processing/Services/StickInputConditioner.cs";
        private const string CalibrationStart = "public DS4State ApplyDrift(";
        // Phase8-Step8 8a: held の表を削除したため、較正の直後の文書コメント（Map DS4 Buttons/Axes…）を終端にする
        private const string CalibrationEnd = "public void Fuzz(";
        // Phase9 MapCustom の分割 M1: Commit の本体は SyntheticInputCommitter へ移した（検査の対象も、移動先のファイルにした）
        private const string CommitPath = "DS4Windows/Processing/Services/SyntheticInputCommitter.cs";
        private const string CommitStart = "public void Commit(";
        // Phase7-Step8 W1: MapClick を SyntheticInputState.CountClick へ移したため、Commit の次のメンバーを終端にする
        // Phase8-Fix-MacroRepeatUnify（D5）: DS4ControltoInt を削除したため、次のメンバー（TValue）を終端にする
        private const string CommitEnd = "_virtualKbm.Sync();";

        private readonly ITestOutputHelper _output;

        public MappingCommitAndCalibrationGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた、指定範囲のソーステキストを返す。ファイルが見つからなければ null。</summary>
        private string ReadRangeWithoutComments(string startMarker, string endMarker, string relativePath = "DS4Windows/Processing/Mapping.cs")
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            int start = text.IndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{startMarker} が見つからない（メソッド名・シグネチャの変更？）");
            int end = text.IndexOf(endMarker, start, StringComparison.Ordinal);
            Assert.True(end > start, $"{endMarker} が {startMarker} の後ろに見つからない（並び順の変更？）");

            string body = text.Substring(start, end - start);
            body = Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            body = Regex.Replace(body, @"//[^\r\n]*", string.Empty);
            return body;
        }

        [Fact]
        public void ApplyDrift_TakesProfileSettingsAsParameter()
        {
            // 検査範囲はシグネチャ行から始まるため、引数リストも含まれる。
            string body = ReadRangeWithoutComments(CalibrationStart, CalibrationEnd, CalibrationPath);
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }

        [Fact]
        public void Commit_TakesProfileSettingsAsParameter()
        {
            string body = ReadRangeWithoutComments(CommitStart, CommitEnd, CommitPath);
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }

        [Fact]
        public void Commit_ReadsOutputKbmMappingFromPassedSettings()
        {
            // 置換漏れの検出（Global 参照が残っていないことの裏返しとして、引数経由の参照が存在することも確認する）。
            string body = ReadRangeWithoutComments(CommitStart, CommitEnd, CommitPath);
            if (body == null) return;

            Assert.Contains("settings.OutputKBMMapping.", body);
        }
    }
}