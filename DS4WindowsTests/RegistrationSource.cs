using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14c-Step6（R-18）: DI の登録のソース。登録は層ごとの partial ファイル（<c>DS4Windows/Composition/ServiceRegistration*.cs</c>）に分かれているため、
    /// 登録の文字列を検査するテストは、全部をファイル名の順につないだ文字列を使う。ソースが見つからない環境では <c>null</c>。
    /// <paramref name="stripComments"/> が true のときは、コメント（<c>/* */</c> と <c>//</c>）を除く（コメントの中の旧名で誤検出しないため）。
    /// </summary>
    internal static class RegistrationSource
    {
        public const string MainFile = "DS4Windows/Composition/ServiceRegistration.cs";

        public static string ReadAll(bool stripComments = false)
        {
            string main = SourceFileLocator.Find(MainFile);
            if (main == null) return null;

            string text = string.Join("\n", Directory.GetFiles(Path.GetDirectoryName(main), "ServiceRegistration*.cs")
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(File.ReadAllText));
            if (!stripComments) return text;

            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }
    }
}
