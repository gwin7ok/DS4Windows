using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V2: 画面（View。<c>DS4Windows.UI</c> 直下の型）は、業務のサービス・設定の型
    /// （<c>Core.Abstractions</c>・<c>Processing.Abstractions</c>・<c>Processing.Services</c>・<c>Core.Settings</c>・<c>Core.Persistence</c>）を
    /// 直接参照せず、ViewModel を通す。今ある違反は <see cref="ViewDirectServiceReferenceBaseline"/> に凍結し、実数との一致を検査する（減らすことが目標）。
    /// </summary>
    public class ViewDirectServiceReferenceRatchetTests
    {
        private static readonly string[] ServiceNamespaces =
        {
            "DS4Windows.Core.Abstractions", "DS4Windows.Processing.Abstractions", "DS4Windows.Processing.Services",
            "DS4Windows.Core.Settings", "DS4Windows.Core.Persistence",
        };

        [Fact]
        public void Views_ReferenceServiceTypesOnlyAsListedInTheBaseline()
        {
            var all = new List<string>();
            foreach (string ns in ServiceNamespaces)
                all.AddRange(LayerDependencyScanner.ReferencesBetweenNamespaces("DS4Windows.UI", ns));
            var actual = all.Where(p => Regex.IsMatch(p, @"^DS4Windows\.UI\.[^.|]+\|")).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();

            var added = actual.Except(ViewDirectServiceReferenceBaseline.Entries, StringComparer.Ordinal).ToList();
            var removed = ViewDirectServiceReferenceBaseline.Entries.Except(actual, StringComparer.Ordinal).ToList();
            if (added.Count == 0 && removed.Count == 0) return;

            var message = new StringBuilder();
            message.AppendLine("View の業務サービスの型の参照が、許可リスト（ViewDirectServiceReferenceBaseline）と一致しません。");
            message.AppendLine($"増えた（是正すること。許可リストへは追加しない）: {added.Count} 件");
            foreach (string e in added.Take(50)) message.AppendLine("  + " + e);
            message.AppendLine($"減った（許可リストからも消すこと）: {removed.Count} 件");
            foreach (string e in removed.Take(50)) message.AppendLine("  - " + e);
            Assert.True(false, message.ToString());
        }
    }
}
