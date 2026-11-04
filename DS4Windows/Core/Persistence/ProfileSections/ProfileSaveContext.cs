using System;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3、Q4＝A）: プロファイル 1 回分の保存で、節が共有する状態。
    /// 節ごとの局所変数（各要素のノード）は、各節が自前で持つ。
    /// </summary>
    internal sealed class ProfileSaveContext
    {
        public ProfileSaveContext(BackingStore store, XmlDocument xdoc, XmlElement rootElement, int device,
            LightbarSettingInfo lightbarSettings, LightbarDS4WinInfo lightInfo)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Xdoc = xdoc ?? throw new ArgumentNullException(nameof(xdoc));
            RootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
            Device = device;
            LightbarSettings = lightbarSettings;
            LightInfo = lightInfo;
        }

        public BackingStore Store { get; }
        public XmlDocument Xdoc { get; }
        public XmlElement RootElement { get; }
        public int Device { get; }
        public LightbarSettingInfo LightbarSettings { get; }
        public LightbarDS4WinInfo LightInfo { get; }
    }
}
