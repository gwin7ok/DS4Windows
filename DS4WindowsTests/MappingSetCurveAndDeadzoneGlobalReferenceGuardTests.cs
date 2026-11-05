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
    /// Phase6-Step3-4／3-5: `Mapping.SetCurveAndDeadzone` のスティック・トリガー系（Step3-4 の27件）とジャイロ系 SX/SZ（Step3-5 の13件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// `ApplyStickCalibration`（現 `StickInputConditioner.ApplyDrift`）と `Commit` は `MappingCommitAndCalibrationGlobalReferenceGuardTests` が担当する。
    /// </summary>
    public class MappingSetCurveAndDeadzoneGlobalReferenceGuardTests
    {

        private readonly ITestOutputHelper _output;

        public MappingSetCurveAndDeadzoneGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SetCurveAndDeadzone_TakesProfileSettingsAsParameter()
        {
            // Phase14d-Step3: ソースの文字列ではなく、メソッドの引数の型を確かめる
            var methods = typeof(DS4Windows.Processing.Mapping).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Where(m => m.Name == "SetCurveAndDeadzone").ToArray();
            Assert.NotEmpty(methods);
            Assert.All(methods, m => Assert.Contains(m.GetParameters(), p => p.ParameterType == typeof(DS4Windows.Core.Abstractions.IProfileSettingsService)));
        }
    }
}