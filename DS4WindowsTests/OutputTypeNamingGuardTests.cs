using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力の種類（Xbox 360／DS4）を、コードで名指しする場所を限る。種類ごとの違いは、値の表（<c>OutContTypeInfo</c>）と、
    /// 出力のカートリッジ・出力デバイスの操作へ集めてあり、それ以外の場所に <c>OutContType.X360</c>／<c>OutContType.DS4</c> を書かない
    /// （3 つ目の種類を足すときに、探して直す場所を増やさないため）。列挙型の値はコンパイル時に定数へ置き換わり、型の参照として残らないため、
    /// 型の検査（ArchUnitNET）ではなく、コメントを除いたソースの検査で守る。
    /// </summary>
    public class OutputTypeNamingGuardTests
    {
        // 名指しを許す場所（ファイル名）と、その理由
        private static readonly Dictionary<string, string> Allowed = new Dictionary<string, string>
        {
            ["Xbox360OutputCartridge.cs"] = "Xbox 360 の出力のカートリッジ（自分の種類を名乗る）",
            ["Ds4OutputCartridge.cs"] = "DS4 の出力のカートリッジ（自分の種類を名乗る）",
            ["OutContTypeInfo.cs"] = "種類ごとの値の表（保存キー、IPC の名前、ボタン名の表、既定の種類）",
            ["ControllerLayouts.cs"] = "出力ピッカーの図の対応表（種類 → 配置表と背景の画像）",
            ["ProfilePresets.Gyro.cs"] = "DS4 の出力の種類を選ぶ名前の付いたプリセット（LoadDefaultDS4MixedGyroMouseProfile など。ProfilePresets.cs を分けたもの）",
            ["ProfilePresets.cs"] = "DS4 の出力の種類を選ぶ名前の付いたプリセット（LoadBlankDS4Profile など）",
        };

        private static readonly Regex Naming = new Regex(@"\bOutContType\.(X360|DS4)\b", RegexOptions.Compiled);

        [Fact]
        public void TheTypesAreNamedOnlyWhereTheyAreAllowed()
        {
            IReadOnlyList<string> files = SourceText.ProductionFiles();
            if (files.Count == 0) return;   // ソースなしの環境

            var offenders = new List<string>();
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (Allowed.ContainsKey(name)) continue;

                string code = SourceText.StripComments(File.ReadAllText(file));
                if (Naming.IsMatch(code)) offenders.Add(name);
            }

            Assert.True(offenders.Count == 0,
                "出力の種類を名指ししてよいのは許可リストのファイルだけ。種類ごとの違いは OutContTypeInfo（値）か、カートリッジ・出力デバイス（処理）へ置くこと: " + string.Join(", ", offenders));
        }

        [Fact]
        public void EveryAllowedFile_StillNamesAType()
        {
            IReadOnlyList<string> files = SourceText.ProductionFiles();
            if (files.Count == 0) return;

            foreach (KeyValuePair<string, string> allowed in Allowed)
            {
                string file = files.SingleOrDefault(f => Path.GetFileName(f) == allowed.Key);
                Assert.True(file != null, allowed.Key + " が見つからない（許可リストを直すこと）");
                Assert.True(Naming.IsMatch(SourceText.StripComments(File.ReadAllText(file))),
                    allowed.Key + " は種類を名指ししなくなった。許可リストから外すこと（" + allowed.Value + "）");
            }
        }
    }
}
