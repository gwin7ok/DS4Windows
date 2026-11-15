using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V1: 画面の部品（<c>DS4Windows.UI.ViewModels</c>）は、WPF の型（<c>System.Windows</c>）を参照しない（<c>ICommand</c> だけ例外）。
    /// 今ある違反は <see cref="ViewModelWpfReferenceBaseline"/> に凍結し、実数との一致を検査する（減らすことが目標）。
    /// </summary>
    public class ViewModelWpfReferenceRatchetTests
    {
        private const string BaselineOutEnv = "DS4W_VMWPF_BASELINE_OUT";

        [Fact]
        public void ViewModels_ReferenceWpfTypesOnlyAsListedInTheBaseline()
        {
            var actual = LayerDependencyScanner.ReferencesBetweenNamespaces("DS4Windows.UI.ViewModels", "System.Windows")
                .Where(p => !p.EndsWith("|System.Windows.Input.ICommand", StringComparison.Ordinal))
                .ToList();

            var added = actual.Except(ViewModelWpfReferenceBaseline.Entries, StringComparer.Ordinal).ToList();
            var removed = ViewModelWpfReferenceBaseline.Entries.Except(actual, StringComparer.Ordinal).ToList();
            if (added.Count == 0 && removed.Count == 0) return;

            string outPath = Environment.GetEnvironmentVariable(BaselineOutEnv);
            if (!string.IsNullOrEmpty(outPath))
                File.WriteAllText(outPath, string.Join(Environment.NewLine, actual), new UTF8Encoding(true));

            var message = new StringBuilder();
            message.AppendLine("ViewModel の WPF の型の参照が、許可リスト（ViewModelWpfReferenceBaseline）と一致しません。");
            message.AppendLine($"増えた（是正すること。許可リストへは追加しない）: {added.Count} 件");
            foreach (string e in added.Take(50)) message.AppendLine("  + " + e);
            message.AppendLine($"減った（許可リストからも消すこと）: {removed.Count} 件");
            foreach (string e in removed.Take(50)) message.AppendLine("  - " + e);
            Assert.True(false, message.ToString());
        }
    }
}
