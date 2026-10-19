using System;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W2（Phase8-Step4b-Plan.md §3.2）: タッチパッドの基本設定（<c>lowerRCOn</c>〜<c>TouchpadClickPassthru</c>）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileTouchpadBasicLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/lowerRCOn"); bool.TryParse(Item.InnerText, out store.lowerRCOn[device]); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/tapSensitivity"); byte.TryParse(Item.InnerText, out store.tapSensitivity[device]); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/doubleTap"); bool.TryParse(Item.InnerText, out store.doubleTap[device]); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/scrollSensitivity"); int.TryParse(Item.InnerText, out store.scrollSensitivity[device]); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadInvert"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.touchpadInvert[device] = Math.Min(Math.Max(temp, 0), 3); }
            catch { store.touchpadInvert[device] = 0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadClickPassthru"); bool.TryParse(Item.InnerText, out store.touchClickPassthru[device]); }
            catch { missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
