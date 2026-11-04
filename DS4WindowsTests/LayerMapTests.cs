using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: <see cref="LayerMap"/>（名前空間 → 層、層の組み合わせ → 規則、型名の正規化）の単体テスト。
    /// </summary>
    public class LayerMapTests
    {
        [Theory]
        [InlineData("DS4Windows.Input", ArchLayer.Input)]
        [InlineData("DS4Windows.Input.Devices", ArchLayer.Input)]
        [InlineData("DS4Windows.Processing", ArchLayer.Processing)]
        [InlineData("DS4Windows.Processing.Services", ArchLayer.Processing)]
        [InlineData("DS4Windows.Processing.Abstractions", ArchLayer.ProcessingAbstractions)]
        [InlineData("DS4Windows.Output.Services", ArchLayer.Output)]
        [InlineData("DS4Windows.UI.ViewModels", ArchLayer.UI)]
        [InlineData("DS4Windows.Composition", ArchLayer.Composition)]
        [InlineData("DS4Windows", ArchLayer.Composition)]
        [InlineData("DS4Windows.Core.Settings", ArchLayer.Core)]
        [InlineData("DS4Windows.Properties", ArchLayer.Shared)]
        [InlineData("DS4Windows.Translations", ArchLayer.Shared)]
        [InlineData("DS4Windows.CoreX", ArchLayer.External)]
        [InlineData("Sensorit.Base", ArchLayer.External)]
        [InlineData("System.Collections.Generic", ArchLayer.External)]
        [InlineData("", ArchLayer.External)]
        [InlineData(null, ArchLayer.External)]
        public void Classify_MapsNamespaceToLayer(string ns, ArchLayer expected)
        {
            Assert.Equal(expected, LayerMap.Classify(ns));
        }

        [Theory]
        [InlineData(ArchLayer.Core, ArchLayer.Processing, "L1")]
        [InlineData(ArchLayer.Core, ArchLayer.ProcessingAbstractions, "L1")]
        [InlineData(ArchLayer.Core, ArchLayer.UI, "L1")]
        [InlineData(ArchLayer.Core, ArchLayer.Composition, "L1")]
        [InlineData(ArchLayer.Core, ArchLayer.Input, "L2")]
        [InlineData(ArchLayer.Core, ArchLayer.Output, "L2")]
        [InlineData(ArchLayer.Input, ArchLayer.Processing, "L3")]
        [InlineData(ArchLayer.Input, ArchLayer.ProcessingAbstractions, "L3")]
        [InlineData(ArchLayer.Input, ArchLayer.Output, "L3")]
        [InlineData(ArchLayer.Input, ArchLayer.UI, "L3")]
        [InlineData(ArchLayer.Output, ArchLayer.Processing, "L4")]
        [InlineData(ArchLayer.Output, ArchLayer.UI, "L4")]
        [InlineData(ArchLayer.Processing, ArchLayer.UI, "L5")]
        [InlineData(ArchLayer.ProcessingAbstractions, ArchLayer.Composition, "L5")]
        [InlineData(ArchLayer.UI, ArchLayer.Composition, "L6")]
        [InlineData(ArchLayer.UI, ArchLayer.Input, "L7")]
        [InlineData(ArchLayer.UI, ArchLayer.Output, "L7")]
        public void RuleFor_ReturnsTheViolatedRule(ArchLayer source, ArchLayer target, string expected)
        {
            Assert.Equal(expected, LayerMap.RuleFor(source, target));
        }

        [Theory]
        [InlineData(ArchLayer.Processing, ArchLayer.Input)]
        [InlineData(ArchLayer.Processing, ArchLayer.Output)]
        [InlineData(ArchLayer.Processing, ArchLayer.Core)]
        [InlineData(ArchLayer.Output, ArchLayer.ProcessingAbstractions)]   // §3.4 R2: 第3層は第2層の契約を実装してよい
        [InlineData(ArchLayer.Output, ArchLayer.Input)]
        [InlineData(ArchLayer.Input, ArchLayer.Core)]
        [InlineData(ArchLayer.UI, ArchLayer.Processing)]
        [InlineData(ArchLayer.UI, ArchLayer.Core)]
        [InlineData(ArchLayer.Composition, ArchLayer.Input)]
        [InlineData(ArchLayer.Composition, ArchLayer.UI)]
        [InlineData(ArchLayer.Core, ArchLayer.Shared)]
        [InlineData(ArchLayer.Input, ArchLayer.External)]
        [InlineData(ArchLayer.Core, ArchLayer.Core)]
        public void RuleFor_AllowsPermittedDirections(ArchLayer source, ArchLayer target)
        {
            Assert.Null(LayerMap.RuleFor(source, target));
        }

        [Fact]
        public void RecordOnlyRules_AreL2AndL7()
        {
            Assert.Equal(new[] { "L2", "L7" }, LayerMap.RecordOnlyRules);
        }

        [Theory]
        [InlineData("DS4Windows.Processing.Mouse", "DS4Windows.Processing.Mouse")]
        [InlineData("DS4Windows.Processing.Mouse+TouchButtonActivationMode", "DS4Windows.Processing.Mouse")]
        [InlineData("DS4Windows.Composition.ServiceRegistration+<>c", "DS4Windows.Composition.ServiceRegistration")]
        [InlineData("DS4Windows.Composition.ServiceRegistration/<>c__DisplayClass1_0", "DS4Windows.Composition.ServiceRegistration")]
        [InlineData("System.Collections.Generic.List`1[[DS4Windows.Core.Settings.SpecialAction]]", "System.Collections.Generic.List`1")]
        public void ToTopLevelTypeName_StripsNestedAndGenericParts(string fullName, string expected)
        {
            Assert.Equal(expected, LayerMap.ToTopLevelTypeName(fullName));
        }
    }
}
