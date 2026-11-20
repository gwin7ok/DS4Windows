#nullable enable

using System;
using System.Xml;
using static DS4Windows.Core.Utilities.Util;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// スペシャルアクションの割り当て（ProfileActions）の保存。Step5 が組み立ての入力に使う節のため、単独のクラスにする。
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

            // 使う SA の固定 ID を、名前の一覧と同じ並びで書く（名前の一覧は、古いバージョンのために残す）。
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
