using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: 特殊アクションの割り当て（ProfileActions）の保存。Step5 が組み立ての入力に使う節のため、単独のクラスにする。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileActionsSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            XmlNode xmlProfileActions = m_Xdoc.CreateNode(XmlNodeType.Element, "ProfileActions", null);
            xmlProfileActions.InnerText = string.Join("/", store.profileActions[device]); rootElement.AppendChild(xmlProfileActions);

            // Phase10-Step4d W3（Phase10-Step4d-Plan.md §3.2）: 使う SA の固定 ID を、名前の一覧と同じ並びで書く（名前の一覧は、古いバージョンのために残す）。
            // 読み込みは、ID を主にして名前を引き直す（SA の名前変更に追従する）。見つからない SA の ID は空（名前だけの参照）。1 つも ID がなければ、要素を書かない
            var ids = new System.Collections.Generic.List<string>();
            bool anyId = false;
            foreach (string actionName in store.profileActions[device])
            {
                string id = FindActionId(store, actionName);
                anyId |= id.Length > 0;
                ids.Add(id);
            }

            if (anyId)
            {
                XmlNode xmlProfileActionIds = m_Xdoc.CreateNode(XmlNodeType.Element, "ProfileActionIds", null);
                xmlProfileActionIds.InnerText = string.Join("/", ids);
                rootElement.AppendChild(xmlProfileActionIds);
            }
        }

        // 名前に対応する SA の ID。見つからなければ空（BackingStore.GetAction は、見つからないと仮の SA を返すため使わない）
        private static string FindActionId(BackingStore store, string actionName)
        {
            string search = NormalizeActionName(actionName);
            for (int i = 0, count = store.actions.Count; i < count; i++)
            {
                SpecialAction action = store.actions[i];
                if (action != null && string.Equals(NormalizeActionName(action.name), search, StringComparison.OrdinalIgnoreCase))
                    return action.id ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
