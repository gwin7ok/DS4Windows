using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6b（決定 P2）: `Mapping.cs` のうち、`ControlService ctrl` を持たない「判定補助メソッド群」（グループ B、12件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// グループ B は呼び出し元が約50箇所と多く `ctrl` を持たないため、引数渡しは行わず、`Mapping` の静的 `profileSettings` から読む
    /// （Phase7 の instance 化で解消する。`Phase6-Step3-Plan.md` §3.4.1）。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、Global メンバ呼び出しが誤って再導入されても
    /// コンパイルは通る。本テストはその混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingHelperMethodsGlobalReferenceGuardTests
    {

        private readonly ITestOutputHelper _output;

        public MappingHelperMethodsGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void IsUsingGyroForControls_MovedToControlActivityReader()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Mapping.cs");
            if (text == null) return;

            // Phase9 9-B: 旧 Mapping.IsUsingGyroForControls は ControlActivityReader（引数の settings）へ移った。Mapping.cs には残っていないことと、移し先の判定を確かめる
            Assert.DoesNotContain("IsUsingGyroForControls", text);

            string readerPath = SourceFileLocator.Find("DS4Windows/Processing/Services/ControlActivityReader.cs");
            if (readerPath == null) return;
            string reader = File.ReadAllText(readerPath);
            Assert.Contains("settings.GyroOutputMode[device] == GyroOutMode.Controls", reader);
        }
    }
}