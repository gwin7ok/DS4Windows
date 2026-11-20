using System;
using System.Collections.Generic;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// マウス加速・起動プログラム・DInput のみ・SA ステアリング・ジャイロの制御（MouseAcceleration〜）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileMotionSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            foreach (ProfileSetting setting in ProfileMotionSettings.All)
            {
                setting.Save(context);
            }

            // 廃止された「ジャイロスワイプ」の設定は、保持した内容をそのまま書き出す（なければ、従来と同じ既定値の要素）
            store.gyroSlots[device].RemovedSwipe.AppendSettingsTo(m_Xdoc, rootElement);

            // Ensure profileActions list exists. Do NOT auto-add "Disconnect Controller"
            // here — respect the stored list so users can intentionally disable
            // the Bluetooth-disconnect special action for an existing profile.
            if (store.profileActions[device] == null)
            {
                store.profileActions[device] = new List<string>();
            }
        }
    }
}
