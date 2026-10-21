using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 Steering 削除 W4（Phase9-SteeringWheel-Removal-Plan.md）: ステアリングの設定（<c>SASteeringWheelEmulationAxis</c>／<c>SASteeringWheelEmulationRange</c>／<c>SASteeringWheelFuzz</c>／<c>SASteeringWheelSmoothingOptions</c>）は、
    /// プロファイル XML に書かれなくなり、古いプロファイルにこれらの要素が残っていても、無視されて、ほかの設定が正しく読み込まれること。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase9SteeringW4LegacyProfileTests
    {
        private static readonly string[] RemovedElements =
        {
            "SASteeringWheelEmulationAxis", "SASteeringWheelEmulationRange", "SASteeringWheelFuzz", "SASteeringWheelSmoothingOptions",
            "SASteeringWheelUseSmoothing", "SASteeringWheelSmoothMinCutoff", "SASteeringWheelSmoothBeta",
        };

        private const string LegacySteering =
            "<SASteeringWheelEmulationAxis>LX</SASteeringWheelEmulationAxis><SASteeringWheelEmulationRange>900</SASteeringWheelEmulationRange>" +
            "<SASteeringWheelFuzz>7</SASteeringWheelFuzz><SASteeringWheelSmoothingOptions><SASteeringWheelUseSmoothing>True</SASteeringWheelUseSmoothing>" +
            "<SASteeringWheelSmoothMinCutoff>0.25</SASteeringWheelSmoothMinCutoff><SASteeringWheelSmoothBeta>0.3</SASteeringWheelSmoothBeta></SASteeringWheelSmoothingOptions>";

        [Fact]
        public void ASavedProfile_HasNoSteeringElements()
        {
            using var ctx = new PersistenceGoldenContext();
            string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "SteeringW4Saved");

            foreach (string element in RemovedElements)
                Assert.DoesNotContain("<" + element, xml);
        }

        [Fact]
        public void ALegacyProfileWithSteeringElements_LoadsTheOtherSettings_AndTheResaveDropsTheSteeringElements()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore original = PersistenceHarness.NewStore();
            original.gyroSensitivity[PersistenceHarness.Slot] = 123;   // 既定でない値（ほかの設定が壊れずに読まれることの目印）
            string xml = PersistenceHarness.SaveProfile(ctx, original, "SteeringW4Legacy");

            int close = xml.LastIndexOf("</DS4Windows>");
            Assert.True(close > 0);
            string legacy = xml.Insert(close, LegacySteering);
            Assert.Contains("<SASteeringWheelEmulationAxis", legacy);   // 旧要素を挿入できたこと（比較が空振りでないこと）

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, legacy, "SteeringW4LegacyLoad");

            Assert.Equal(123, loaded.gyroSensitivity[PersistenceHarness.Slot]);

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "SteeringW4Resaved");
            foreach (string element in RemovedElements)
                Assert.False(Regex.IsMatch(resaved, "<" + element + @"\b"), element + " が再保存した XML に残っている");
        }
    }
}
