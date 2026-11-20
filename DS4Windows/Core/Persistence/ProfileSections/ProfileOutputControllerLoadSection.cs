using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 出力コントローラーの種別（OutputContDevice）の読込と、必要なときの出力デバイスの状態確認。確認の結果（xinputPlug／xinputStatus）は文脈に書き戻し、読込の最後の PostLoadSnippet が使う。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileOutputControllerLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            IProfileLoadTarget control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            DS4Windows.Core.Abstractions.IEnvironmentService _environment = context.Environment;
            bool xinputChange = context.XinputChange;
            OutContType oldContType = context.OldContType;
            bool xinputPlug = false;
            bool xinputStatus = false;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice"); store.outputTypeSlots[device].ContType = OutContTypeInfo.FromSaveKey(Item.InnerText); }
            catch { store.outputTypeSlots[device].ContType = OutContTypeInfo.DefaultType; missingSetting = true; }

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange && device < _environment.ControllerSlotLimit)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            context.XinputPlug = xinputPlug;
            context.XinputStatus = xinputStatus;
            context.MissingSetting = missingSetting;
        }
    }
}
