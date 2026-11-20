using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: 全般・ライトバー・振動・バッテリー・タッチパッドのジッター補正（touchToggle〜touchpadJitterCompensation）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileGeneralSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {

            foreach (ProfileSetting setting in ProfileGeneralSettings.All)
            {
                setting.Save(context);
            }
        }
    }
}
