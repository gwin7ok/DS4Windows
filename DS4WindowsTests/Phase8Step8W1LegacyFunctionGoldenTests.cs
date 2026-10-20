using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step8 W1（Phase8-Step8-Plan.md §3 の方針 1。Q4＝A）: 旧関数（<c>getBoolSpecialActionMapping</c>、<c>GetBoolActionMapping</c>、<c>GetBoolMapping</c>、
    /// <c>GetByteMapping</c>、<c>GetXYAxisMapping</c>）の戻り値の表を、削除（8a・8b）の前にゴールデンへ固定する。
    /// (1) <c>LegacyFunctionGolden</c>: 旧関数の表が、ゴールデンと一致する（初回は生成して失敗する。内容を確認してコミットし、再実行する）。
    /// (2) <c>NewFunctionsMatchTheGolden</c>: 新経路（<c>TriggerInput</c>）の表が、ゴールデンの該当の列と一致する。旧関数を削除した後も、これが「旧と同じ値」の根拠になる。
    /// ゴールデンは <c>DS4WindowsTests/Golden/Phase7Step10c/step8-legacy-functions-*.txt</c>。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step8W1LegacyFunctionGoldenTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step8W1LegacyFunctionGoldenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Passthru)]
        public void LegacyFunctionGolden(GyroOutMode mode)
        {
            GoldenFile.AssertMatches(LegacyFunctionTable.FileName(mode), LegacyFunctionTable.BuildLegacy(mode), _output);
        }

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Passthru)]
        public void NewFunctionsMatchTheGolden(GyroOutMode mode)
        {
            string golden = GoldenFile.ReadOrNull(LegacyFunctionTable.FileName(mode));
            if (golden == null)
            {
                _output.WriteLine("ゴールデンがまだない（LegacyFunctionGolden が生成する）。照合を省略した: " + mode);
                return;
            }

            string expected = GoldenFile.Normalize(LegacyFunctionTable.MaskBoolMapping(golden));
            string actual = GoldenFile.Normalize(LegacyFunctionTable.BuildNew(mode));

            string[] e = expected.Split('\n');
            string[] a = actual.Split('\n');
            Assert.Equal(e.Length, a.Length);
            for (int i = 0; i < e.Length; i++)
            {
                if (e[i] == a[i]) continue;

                // 最初の違いのコントロールを示す
                if (!e[i].StartsWith("#"))
                {
                    string[] et = e[i].Substring(4).Split(',');
                    string[] at = a[i].Substring(4).Split(',');
                    for (int t = 0; t < et.Length; t++)
                    {
                        if (et[t] != at[t])
                            throw new Xunit.Sdk.XunitException($"{(DS4Controls)t}（{i} 行目, mode={mode}）で、旧関数のゴールデンと違う: 期待 {et[t]}／実際 {at[t]}");
                    }
                }

                Assert.Equal(e[i], a[i]);
            }
        }
    }
}
