using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows.Processing;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14（名前空間・フォルダの再編）: 名前空間＝フォルダパスの検査（規約 §3.3 原則3）。
    /// 本体（DS4Windows プロジェクト）の .cs の名前空間が、<c>DS4Windows.&lt;フォルダ（ドット区切り）&gt;</c> と一致すること、
    /// XAML の <c>x:Class</c> が実在する型で、かつフォルダと一致することを検査する。
    /// 許可リストは空に近づける方向にのみ変更する（増やさない）。
    /// </summary>
    public class NamespaceFolderGuardTests
    {
        // 第三者由来の名前空間として残すもの（Sensorit.Base）。
        private static readonly string[] Allowed =
        {
            "Core/Utilities/LowpassFilter.cs",
            "Core/Utilities/OneEuroFilter.cs",
        };

        private static readonly Regex NamespaceRegex = new Regex(@"^[ \t﻿]*namespace[ \t]+([\w.]+)", RegexOptions.Multiline);
        private static readonly Regex XClassRegex = new Regex("x:Class=\"([\\w.]+)\"");

        private static string ProjectDir()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4WinWPF.csproj");
            return anchor == null ? null : Path.GetDirectoryName(anchor);
        }

        private static string Expected(string projectDir, string file)
        {
            string dir = Path.GetDirectoryName(Path.GetRelativePath(projectDir, file)) ?? string.Empty;
            return dir.Length == 0 ? "DS4Windows" : "DS4Windows." + dir.Replace('\\', '.').Replace('/', '.');
        }

        private static IEnumerable<string> ProductionFiles(string projectDir, string pattern)
        {
            return Directory.EnumerateFiles(projectDir, pattern, SearchOption.AllDirectories)
                .Where(f =>
                {
                    string rel = Path.GetRelativePath(projectDir, f).Replace('\\', '/');
                    return !rel.StartsWith("obj/") && !rel.StartsWith("bin/") && !rel.StartsWith("libs/");
                });
        }

        [Fact]
        public void EverySourceFile_DeclaresTheNamespaceThatMatchesItsFolder()
        {
            string projectDir = ProjectDir();
            if (projectDir == null) return;

            var offenders = new List<string>();
            foreach (string file in ProductionFiles(projectDir, "*.cs"))
            {
                string rel = Path.GetRelativePath(projectDir, file).Replace('\\', '/');
                if (Allowed.Contains(rel)) continue;
                Match m = NamespaceRegex.Match(File.ReadAllText(file));
                if (!m.Success) continue; // 型を持たないファイル（GlobalSuppressions など）
                string expected = Expected(projectDir, file);
                if (m.Groups[1].Value != expected)
                    offenders.Add(rel + " : " + m.Groups[1].Value + " (期待 " + expected + ")");
            }
            Assert.True(offenders.Count == 0, "名前空間がフォルダと一致しない:\n" + string.Join("\n", offenders));
        }

        [Fact]
        public void NoSourceFile_IsWithoutANamespace_UnlessItDeclaresNoType()
        {
            string projectDir = ProjectDir();
            if (projectDir == null) return;

            var typeRegex = new Regex(@"^\s*(public|internal)\s+((static|sealed|abstract|partial|readonly)\s+)*(class|interface|struct|enum|record|delegate)\s+\w+", RegexOptions.Multiline);
            var offenders = ProductionFiles(projectDir, "*.cs")
                .Where(f => !f.EndsWith(".g.cs") && !f.EndsWith(".g.i.cs"))
                .Select(f => (File: f, Text: File.ReadAllText(f)))
                .Where(x => !NamespaceRegex.IsMatch(x.Text) && typeRegex.IsMatch(x.Text))
                .Select(x => Path.GetRelativePath(projectDir, x.File))
                .ToArray();
            Assert.True(offenders.Length == 0, "名前空間のない型:\n" + string.Join("\n", offenders));
        }

        [Fact]
        public void EveryXamlClass_ExistsInTheAssembly_AndMatchesItsFolder()
        {
            string projectDir = ProjectDir();
            if (projectDir == null) return;

            var assembly = typeof(ControlService).Assembly;
            var offenders = new List<string>();
            foreach (string file in ProductionFiles(projectDir, "*.xaml"))
            {
                Match m = XClassRegex.Match(File.ReadAllText(file));
                if (!m.Success) continue;
                string full = m.Groups[1].Value;
                if (assembly.GetType(full) == null)
                    offenders.Add(Path.GetFileName(file) + " : x:Class=" + full + " が見つからない");
                string ns = full.Substring(0, full.LastIndexOf('.'));
                if (ns != Expected(projectDir, file))
                    offenders.Add(Path.GetFileName(file) + " : " + ns + " (期待 " + Expected(projectDir, file) + ")");
            }
            Assert.True(offenders.Count == 0, string.Join("\n", offenders));
        }

        [Fact]
        public void XamlClrNamespaces_OfTheProject_Exist()
        {
            string projectDir = ProjectDir();
            if (projectDir == null) return;

            var assembly = typeof(ControlService).Assembly;
            var known = new HashSet<string>(assembly.GetTypes().Select(t => t.Namespace).Where(n => n != null));
            var rx = new Regex("clr-namespace:(DS4Windows[\\w.]*)\"");
            var offenders = new List<string>();
            foreach (string file in ProductionFiles(projectDir, "*.xaml"))
            {
                foreach (Match m in rx.Matches(File.ReadAllText(file)))
                {
                    string ns = m.Groups[1].Value;
                    string folder = ns == "DS4Windows" ? projectDir : Path.Combine(projectDir, ns.Substring("DS4Windows.".Length).Replace('.', Path.DirectorySeparatorChar));
                    // 型を持つ名前空間か、フォルダとして実在する名前空間（Themes は型を持たない）。
                    if (!known.Contains(ns) && !Directory.Exists(folder))
                        offenders.Add(Path.GetFileName(file) + " : " + m.Groups[1].Value);
                }
            }
            Assert.True(offenders.Count == 0, "存在しない名前空間を指す xmlns:\n" + string.Join("\n", offenders));
        }
    }
}
