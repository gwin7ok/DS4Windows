using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4b W11（Phase8-Step4b-Plan.md §3.5、Q8＝A）: プロファイルの読込・保存の分解が元に戻らないように、行数の上限をソースの走査で固定する。
    /// 上限は、分解後の実測（LoadProfile 164 行、SaveProfile 55 行、節の最大 353 行、LegacyProfileXmlSerializer.cs 378 行）に余裕を足した値。
    /// 超えるときは、節を足して分けるか、Step の計画書で上限を見直すこと。
    /// </summary>
    public class Phase8Step4bW11LengthGuardTests
    {
        private const int MaxLoadProfileLines = 200;
        private const int MaxSaveProfileLines = 80;
        private const int MaxSectionFileLines = 500;
        private const int MaxSerializerFileLines = 450;

        private static string ServicesDir()
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "DS4Windows", "DS4WinWPF.csproj")))
                dir = Path.GetDirectoryName(dir);
            Assert.NotNull(dir);
            return Path.Combine(dir, "DS4Windows", "DS4Control", "Services");
        }

        private static int MethodLines(string[] lines, string signature)
        {
            int start = Array.FindIndex(lines, l => l.Contains(signature));
            Assert.True(start >= 0, signature + " が見つからない");
            int depth = 0;
            bool opened = false;
            for (int i = start; i < lines.Length; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{') { depth++; opened = true; }
                    else if (c == '}') depth--;
                }

                if (opened && depth == 0) return i - start + 1;
            }

            throw new InvalidOperationException(signature + " の終わりが見つからない");
        }

        [Fact]
        public void LoadProfileAndSaveProfile_StayShort()
        {
            string[] lines = File.ReadAllLines(Path.Combine(ServicesDir(), "LegacyProfileXmlSerializer.cs"));
            Assert.InRange(MethodLines(lines, "public bool LoadProfile("), 1, MaxLoadProfileLines);
            Assert.InRange(MethodLines(lines, "public bool SaveProfile("), 1, MaxSaveProfileLines);
            Assert.InRange(lines.Length, 1, MaxSerializerFileLines);
        }

        [Fact]
        public void EverySectionFile_StaysWithinTheLimit_AndHoldsOneType()
        {
            string[] files = Directory.GetFiles(Path.Combine(ServicesDir(), "ProfileSections"), "*.cs");
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                Assert.True(lines.Length <= MaxSectionFileLines, Path.GetFileName(file) + " が " + MaxSectionFileLines + " 行を超えている（" + lines.Length + " 行）");

                string name = Path.GetFileNameWithoutExtension(file);
                int types = lines.Count(l => l.TrimStart().StartsWith("internal sealed class ") || l.TrimStart().StartsWith("internal interface "));
                Assert.Equal(1, types);
                Assert.Contains(lines, l => l.Contains(" " + name) && (l.Contains("class ") || l.Contains("interface ")));
            }
        }

        [Fact]
        public void TheSectionClassesCoverTheLoadAndSaveGroups()
        {
            string[] names = Directory.GetFiles(Path.Combine(ServicesDir(), "ProfileSections"), "*.cs")
                .Select(Path.GetFileNameWithoutExtension).ToArray();
            Assert.Equal(15, names.Count(n => n.StartsWith("Profile") && n.EndsWith("LoadSection")));
            Assert.Equal(7, names.Count(n => n.StartsWith("Profile") && n.EndsWith("SaveSection")));
            Assert.Contains("ProfileActionsLoadSection", names);
            Assert.Contains("ProfileActionsSaveSection", names);
        }
    }
}
