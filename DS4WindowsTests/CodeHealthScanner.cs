using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="CodeHealthRatchetTests"/> が使う、ソースの文字列の数え方。
    /// 入力（相対パスと全文）を引数で受けるので、偽の入力で数え方そのものを検査できる（<c>CodeHealthScannerTests</c>）。
    /// </summary>
    internal static class CodeHealthScanner
    {
        private static readonly Regex UpperLayerUsing = new Regex(
            @"^using DS4Windows\.(Processing|UI|Output|Composition|Input)(\.\w+)*;", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex WindowsUsing = new Regex(
            @"^\s*using\s+(static\s+)?(\w+\s*=\s*)?System\.Windows(\.\w+)*\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex NonFormTimerAlias = new Regex(
            @"^[ \t]*using\s+NonFormTimer\s*=\s*System\.Timers\.Timer\s*;[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>数える対象の 1 ファイル。<see cref="RelativePath"/> は、本体の <c>DS4Windows/</c> からの相対パス（区切りは / でも \ でもよい）。</summary>
        public sealed class SourceFile
        {
            public SourceFile(string relativePath, string text)
            {
                RelativePath = relativePath;
                Text = text;
            }

            public string RelativePath { get; }

            public string Text { get; }
        }

        /// <summary>相対パスの最初のフォルダ名（<c>Core/Model/X.cs</c> なら <c>Core</c>）。ルート直下のファイルは空文字。</summary>
        public static string LayerOf(string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            int slash = normalized.IndexOf('/');
            return slash < 0 ? string.Empty : normalized.Substring(0, slash);
        }

        /// <summary><paramref name="layer"/> の中で、上位層の名前空間を <c>using</c> するファイルの数。</summary>
        public static int CountUpperLayerUsingFiles(IEnumerable<SourceFile> files, string layer)
        {
            return InLayer(files, layer).Count(f => UpperLayerUsing.IsMatch(f.Text));
        }

        /// <summary><paramref name="layer"/> の中で、<c>System.Windows*</c> を <c>using</c> するファイルの数。</summary>
        public static int CountWindowsUsingFiles(IEnumerable<SourceFile> files, string layer)
        {
            return InLayer(files, layer).Count(f => WindowsUsing.IsMatch(f.Text));
        }

        /// <summary>別名 <c>NonFormTimer</c> を宣言して、コメントを除いたファイルの中でほかに使っていない行の数。</summary>
        public static int CountUnusedNonFormTimerAliasLines(IEnumerable<SourceFile> files)
        {
            int unused = 0;
            foreach (SourceFile file in files)
            {
                int aliasLines = NonFormTimerAlias.Matches(file.Text).Count;
                if (aliasLines == 0) continue;
                string code = NonFormTimerAlias.Replace(SourceText.StripComments(file.Text), string.Empty);
                if (!Regex.IsMatch(code, @"\bNonFormTimer\b")) unused += aliasLines;
            }
            return unused;
        }

        private static IEnumerable<SourceFile> InLayer(IEnumerable<SourceFile> files, string layer)
        {
            return files.Where(f => string.Equals(LayerOf(f.RelativePath), layer, StringComparison.Ordinal));
        }
    }
}
