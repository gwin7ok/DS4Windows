using System;
using System.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// UI（4-a）が具象の <c>ControlService</c> を参照しないこと（規則 L8。規約 1 章「UI は第 2 層の契約だけに依存する」）。
    /// <c>ControlService</c> は第 1 層・第 3 層の具象（<c>DS4Device</c>、<c>OutputSlotManager</c> ほか）への抜け道になるため、UI が持つと依存の向きが保てない。
    /// 今ある参照は <see cref="FrozenSources"/> に凍結し、実数との一致を検査する（多くても少なくても失敗）。
    /// 是正で減ったら同じコミットでここからも消す。新しい参照は許可リストへ足さずに、第 2 層の契約へ置き換える。
    /// 型の参照は <see cref="LayerDependencyScanner"/>（メソッド本体を含む実際の型参照）で調べる。
    /// </summary>
    public class UiControlServiceReferenceRuleTests
    {
        private const string ControlServiceType = "DS4Windows.Processing.ControlService";

        /// <summary>参照元の型の完全名（序数順）。</summary>
        private static readonly string[] FrozenSources =
        {
            "DS4Windows.UI.BindingWindow",
            "DS4Windows.UI.ControllerReadingsControl",
            "DS4Windows.UI.ControllerRegisterOptionsWindow",
            "DS4Windows.UI.MainWindow",
            "DS4Windows.UI.ProfileEditor",
            "DS4Windows.UI.RecordBox",
            "DS4Windows.UI.Services.EditorWindowFactory",
            "DS4Windows.UI.Services.MainWindowFactory",
            "DS4Windows.UI.Services.ViewModelFactory",
            "DS4Windows.UI.StickCalibrationWindow",
            "DS4Windows.UI.ViewModels.ControllerListViewModel",
            "DS4Windows.UI.ViewModels.ControllerRegDeviceOptsViewModel",
            "DS4Windows.UI.ViewModels.RecordBoxViewModel",
            "DS4Windows.UI.ViewModels.TrayIconViewModel",
        };

        [Fact]
        public void UiTypesReferencingControlService_MatchTheFrozenList()
        {
            string[] actual = LayerDependencyScanner.Edges
                .Where(e => e.Target == ControlServiceType
                            && LayerMap.Classify(NamespaceOf(e.Source)) == ArchLayer.UI)
                .Select(e => e.Source)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            string[] added = actual.Except(FrozenSources, StringComparer.Ordinal).ToArray();
            string[] removed = FrozenSources.Except(actual, StringComparer.Ordinal).ToArray();

            Assert.True(added.Length == 0 && removed.Length == 0,
                "UI から ControlService への参照が許可リストと一致しません。" + Environment.NewLine
                + "増えた（第 2 層の契約へ置き換えること。許可リストへは足さない）: " + string.Join(", ", added) + Environment.NewLine
                + "減った（許可リストからも消すこと）: " + string.Join(", ", removed) + Environment.NewLine
                + "実測:" + Environment.NewLine + string.Join("," + Environment.NewLine, actual.Select(s => "            \"" + s + "\"")));
        }

        private static string NamespaceOf(string typeFullName)
        {
            int i = typeFullName.LastIndexOf('.');
            return i < 0 ? string.Empty : typeFullName.Substring(0, i);
        }
    }
}
