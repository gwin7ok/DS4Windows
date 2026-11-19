#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V3: 画面（XAML）の操作は、イベントのハンドラー（<c>Click="…"</c> など）ではなく、<c>Command</c> のバインディングで ViewModel へ結ぶ。
    /// 見た目だけの処理（ホバー、描画、フォーカス、ドラッグ、ウィンドウの位置・大きさの保存）のハンドラーは、画面の部品として残してよい。
    /// 画面ごとのイベント属性の数を <see cref="ViewCommandBindingBaseline"/> に凍結し、実数との一致を検査する（減らしたら同じコミットで基準も更新する。
    /// 新しい画面・増えた属性は、基準へ足さずに <c>Command</c> へ直す）。<c>Command</c> のバインディングの総数は、下限を割らないことも検査する。
    /// 環境変数 <c>DS4W_VIEWCMD_BASELINE_OUT</c> に書き出し先のパスを入れて実行すると、実測値を書き出す。
    /// </summary>
    public class ViewCommandBindingRatchetTests
    {
        private const string BaselineOutEnv = "DS4W_VIEWCMD_BASELINE_OUT";

        private static readonly Regex EventAttribute = new Regex(
            @"\s(?:\w+\.)?(?:Click|DoubleClick|MouseDoubleClick|MouseDown|MouseUp|MouseMove|MouseEnter|MouseLeave|MouseWheel|MouseLeftButtonDown|MouseLeftButtonUp|" +
            @"PreviewMouse\w+|PreviewKey\w+|KeyDown|KeyUp|SelectionChanged|Checked|Unchecked|Loaded|Unloaded|Closing|Closed|StateChanged|SizeChanged|" +
            @"TextChanged|ValueChanged|GotFocus|LostFocus|Drop|DragOver|DragEnter|Initialized|ContentRendered|Activated|Deactivated|LocationChanged)=""[A-Za-z_]\w*""",
            RegexOptions.CultureInvariant);

        private static readonly Regex CommandBinding = new Regex(@"\sCommand=""\{Binding\b", RegexOptions.CultureInvariant);

        private static readonly Regex XamlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant);

        [Fact]
        public void EventAttributesPerScreenMatchTheBaseline()
        {
            var screens = Measure(out _);
            if (screens == null) return;

            string? outPath = Environment.GetEnvironmentVariable(BaselineOutEnv);
            if (!string.IsNullOrEmpty(outPath))
            {
                File.WriteAllLines(outPath, screens.Select(p => $"{p.Key}={p.Value}"));
            }

            var messages = new StringBuilder();
            foreach (var screen in screens.Where(p => p.Value > 0).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ViewCommandBindingBaseline.EventAttributes.TryGetValue(screen.Key, out int expected);
                if (screen.Value != expected)
                    messages.AppendLine($"  {screen.Key}: 実数 {screen.Value} / 基準 {expected}");
            }

            foreach (var gone in ViewCommandBindingBaseline.EventAttributes.Keys.Where(k => !screens.TryGetValue(k, out int n) || n == 0))
                messages.AppendLine($"  {gone}: 実数 0 / 基準 {ViewCommandBindingBaseline.EventAttributes[gone]}（基準から消すこと）");

            Assert.True(messages.Length == 0,
                "画面のイベント属性の数が基準（ViewCommandBindingBaseline）と一致しません。増えたものは Command へ直し、減ったものは基準も減らすこと。\n" + messages);
        }

        [Fact]
        public void CommandBindingsDoNotFallBelowTheFloor()
        {
            Measure(out int commands);
            if (commands < 0) return;
            Assert.True(commands >= ViewCommandBindingBaseline.CommandBindingFloor,
                $"Command のバインディングが下限 {ViewCommandBindingBaseline.CommandBindingFloor} を下回りました（実数 {commands}）。");
        }

        private static Dictionary<string, int>? Measure(out int commands)
        {
            commands = -1;
            string? root = SourceFileLocator.RepoRoot();
            if (root == null) return null;

            string uiDir = Path.Combine(root, "DS4Windows", "UI");
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            int totalCommands = 0;
            foreach (string file in Directory.EnumerateFiles(uiDir, "*.xaml", SearchOption.AllDirectories))
            {
                string text = XamlComment.Replace(File.ReadAllText(file), string.Empty);
                string key = Path.GetRelativePath(uiDir, file).Replace(Path.DirectorySeparatorChar, '/');
                result[key] = EventAttribute.Matches(text).Count;
                totalCommands += CommandBinding.Matches(text).Count;
            }

            commands = totalCommands;
            return result;
        }
    }
}
