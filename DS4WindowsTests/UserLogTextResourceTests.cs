using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows.Core.Logging;
using DS4Windows.Translations;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 利用者向けのログの文言（リソースの <c>Log_*</c> キー）の整合性と、<see cref="AppLogger.LogUser"/> の出し分けを守る。
    /// 書式の引数の数や順番の取り違え、日本語の訳の抜け、存在しないキーの参照を、ここで見つける。
    /// </summary>
    public class UserLogTextResourceTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
        private static readonly Regex KeyUse = new Regex(@"(?:LogUser\(\s*AppLogLevel\.\w+,|UserText\()\s*(?:[^""\r\n]*?)""(Log_\w+)""", RegexOptions.Compiled);
        private static readonly Regex AnyKey = new Regex(@"""(Log_\w+)""", RegexOptions.Compiled);

        private static IEnumerable<string> LogKeys()
        {
            var set = Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            foreach (System.Collections.DictionaryEntry e in set)
            {
                string key = (string)e.Key;
                if (key.StartsWith("Log_", StringComparison.Ordinal)) yield return key;
            }
        }

        private static string Get(string key, string culture)
        {
            return Strings.ResourceManager.GetString(key, CultureInfo.GetCultureInfo(culture));
        }

        private static List<int> Indexes(string text)
        {
            return Placeholder.Matches(text).Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(i => i).ToList();
        }

        [Fact]
        public void Every_log_key_has_a_japanese_translation_with_the_same_placeholders()
        {
            var keys = LogKeys().ToList();
            Assert.NotEmpty(keys);
            foreach (string key in keys)
            {
                string en = Strings.ResourceManager.GetString(key, CultureInfo.InvariantCulture);
                string ja = Get(key, "ja");
                Assert.False(string.IsNullOrEmpty(en), key + " (en)");
                Assert.NotEqual(en, ja);
                Assert.Equal(Indexes(en), Indexes(ja));
            }
        }

        [Fact]
        public void Every_log_key_used_in_source_exists_in_the_resources()
        {
            var files = SourceText.ProductionFiles();
            if (files.Count == 0) return; // ソースなしの環境
            var known = new HashSet<string>(LogKeys());
            var used = new HashSet<string>();
            foreach (string file in files)
            {
                string code = SourceText.StripComments(System.IO.File.ReadAllText(file));
                foreach (Match m in AnyKey.Matches(code)) used.Add(m.Groups[1].Value);
            }
            Assert.NotEmpty(used);
            Assert.Empty(used.Where(k => !known.Contains(k)));
            // 使われていないキーを残さない（訳だけが残るのを防ぐ）
            Assert.Empty(known.Where(k => !used.Contains(k)));
        }

        [Fact]
        public void LogUser_shows_the_display_language_on_screen_and_english_in_the_file()
        {
            var original = CultureInfo.CurrentUICulture;
            AppLogger.GuiLog += Capture;
            DebugEventArgs got = null;
            void Capture(object s, DebugEventArgs e)
            {
                if (e.Level == AppLogLevel.Warn && e.Data.Contains("Q9") || (e.FileData ?? "").Contains("Q9")) got = e;
            }
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja");
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedTimeout", "Q9");
            }
            finally
            {
                AppLogger.GuiLog -= Capture;
                CultureInfo.CurrentUICulture = original;
            }

            Assert.NotNull(got);
            Assert.Equal("Q9 はタイムアウトのため切断されました", got.Data);
            Assert.Equal("Q9 disconnected due to timeout", got.FileData);
            Assert.Equal(AppLogLevel.Warn, got.Level);
        }

        [Fact]
        public void LogUser_leaves_FileData_empty_when_both_languages_are_the_same()
        {
            var original = CultureInfo.CurrentUICulture;
            AppLogger.GuiLog += Capture;
            DebugEventArgs got = null;
            void Capture(object s, DebugEventArgs e)
            {
                if (e.Data.Contains("Q8")) got = e;
            }
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                AppLogger.LogUser(AppLogLevel.Info, "Log_Device_IdleDisconnect", "Q8");
            }
            finally
            {
                AppLogger.GuiLog -= Capture;
                CultureInfo.CurrentUICulture = original;
            }

            Assert.NotNull(got);
            Assert.Equal("Q8 disconnecting due to idle disconnect", got.Data);
            Assert.Null(got.FileData);
        }
    }
}
