#nullable enable

using System;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// プロファイル 1 回分の保存で、節が共有する状態。
    /// 節ごとの局所変数（各要素のノード）は、各節が自前で持つ。
    /// </summary>
    internal sealed class ProfileSaveContext
    {
        public ProfileSaveContext(BackingStore store, XmlDocument xdoc, XmlElement rootElement, int device,
            LightbarSettingInfo lightbarSettings, LightbarDS4WinInfo lightInfo)
        {
            ArgumentNullException.ThrowIfNull(store);
            Store = store;
            ArgumentNullException.ThrowIfNull(xdoc);
            Xdoc = xdoc;
            ArgumentNullException.ThrowIfNull(rootElement);
            RootElement = rootElement;
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
