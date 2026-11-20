#nullable enable

using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="BackingStore"/> の public な可変フィールドを、これ以上増やさないための検査（ラチェット）。
    /// 設定ファイルへ保存される値は、設定の領域（<c>LayoutSettings</c> など）のプロパティに置く。
    /// 残るのは、パス・スロットごとの配列・実行時の状態。減らしたら、同じコミットで基準も下げる（実数と一致を検査する）。
    /// </summary>
    public class BackingStorePublicFieldRatchetTests
    {
        private const int Baseline = 24;

        [Fact]
        public void PublicMutableFieldCountMatchesBaseline()
        {
            var names = typeof(BackingStore)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .Select(f => f.Name)
                .OrderBy(n => n)
                .ToList();
            Assert.True(names.Count == Baseline, $"public な可変フィールドの数が基準（{Baseline}）と違う: {names.Count}\n{string.Join(", ", names)}");
        }
    }
}
