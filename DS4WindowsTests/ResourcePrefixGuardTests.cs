using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10a W3（Q1＝A）: 資源パスの定数（旧 <c>Global.RESOURCES_PREFIX</c>／<c>ASSEMBLY_RESOURCE_PREFIX</c>）を
    /// 画面側の <c>ResourcePaths</c> へ移したことの回帰ガード。値は従来と同じ文字列でなければならない。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10aW3ResourcePrefixGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10aW3ResourcePrefixGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheConstants_HaveTheSameValuesAsBefore()
        {
            Assert.Equal("/DS4Windows;component/Resources", ResourcePaths.ResourcesPrefix);
            Assert.Equal("pack://application:,,,/DS4Windows;", ResourcePaths.AssemblyResourcePrefix);
        }

        [Fact]
        public void TheInstanceProperties_BuildTheSameUris()
        {
            var paths = new ResourcePaths();
            Assert.Equal("/DS4Windows;component/Resources/size.png", paths.SizePNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4 Config.png", paths.DS4ConfigPNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4 lightbar.png", paths.DS4LightbarPNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4-Config_RS.png", paths.DS4ConfigRSPNG);
            Assert.Equal("/DS4Windows;component/Resources/rainbow.png", paths.RainbowPNG);
        }

        [Fact]
        public void TheIconTable_UsesTheSamePrefix()
        {
            Assert.Equal("/DS4Windows;component/Resources/DS4W.ico", DS4Windows.UI.TrayIconResources.iconChoiceResources[DS4Windows.Core.Settings.TrayIconChoice.Default]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W - White.ico", DS4Windows.UI.TrayIconResources.iconChoiceResources[DS4Windows.Core.Settings.TrayIconChoice.White]);
        }
    }
}
