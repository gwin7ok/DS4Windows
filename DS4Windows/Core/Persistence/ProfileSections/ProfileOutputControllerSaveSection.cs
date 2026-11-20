using System.Xml;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 出力コントローラーの種別（OutputContDevice）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileOutputControllerSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            XmlNode xmlOutContDevice = m_Xdoc.CreateNode(XmlNodeType.Element, "OutputContDevice", null); xmlOutContDevice.InnerText = OutContTypeInfo.SaveKey(store.outputTypeSlots[device].ContType); rootElement.AppendChild(xmlOutContDevice);
        }
    }
}
