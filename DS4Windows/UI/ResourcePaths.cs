namespace DS4Windows.UI
{

    public class ResourcePaths
    {
        // Phase7-Step10a W3（Q1＝A）: 旧 Global.RESOURCES_PREFIX／ASSEMBLY_RESOURCE_PREFIX（値は同じ）。画面側の資源パスの組み立てに使う。
        public const string ResourcesPrefix = "/DS4Windows;component/Resources";
        public const string AssemblyResourcePrefix = "pack://application:,,,/DS4Windows;";

        public string SizePNG { get => $"{ResourcePaths.ResourcesPrefix}/size.png"; }
        public string DS4ConfigPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4 Config.png"; }
        public string DS4LightbarPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4 lightbar.png"; }
        public string DS4ConfigRSPNG { get => $"{ResourcePaths.ResourcesPrefix}/DS4-Config_RS.png"; }
        public string RainbowPNG { get => $"{ResourcePaths.ResourcesPrefix}/rainbow.png"; }
    }
}