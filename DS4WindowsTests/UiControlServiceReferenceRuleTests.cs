using System;
using System.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// UI（4-a）が具象の <c>ControlService</c> を参照しないこと（規則 L8。規約 1 章「UI は第 2 層の契約だけに依存する」）。
    /// <c>ControlService</c> は第 1 層・第 3 層の具象（<c>DS4Device</c>、<c>OutputSlotManager</c> ほか）への抜け道になるため、UI が持つと依存の向きが保てない。
    /// 参照は 0 件で、許可リストはない。新しい参照は、第 2 層の契約（<c>IServiceLifecycle</c>、<c>IControllerSlotQuery</c> など）へ置き換える。
    /// 型の参照は <see cref="LayerDependencyScanner"/>（メソッド本体を含む実際の型参照）で調べる。
    /// </summary>
    public class UiControlServiceReferenceRuleTests
    {
        private const string ControlServiceType = "DS4Windows.Processing.ControlService";

        [Fact]
        public void NoUiTypeReferencesControlService()
        {
            string[] actual = LayerDependencyScanner.Edges
                .Where(e => e.Target == ControlServiceType
                            && LayerMap.Classify(NamespaceOf(e.Source)) == ArchLayer.UI)
                .Select(e => e.Source)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            Assert.True(actual.Length == 0,
                "UI から ControlService への参照があります（第 2 層の契約へ置き換えること）: " + string.Join(", ", actual));
        }

        private static string NamespaceOf(string typeFullName)
        {
            int i = typeFullName.LastIndexOf('.');
            return i < 0 ? string.Empty : typeFullName.Substring(0, i);
        }
    }
}
