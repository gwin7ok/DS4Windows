
namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: タッチパッド・トリガー・スティックの不感帯・最大域・軸別・デルタ加速・レインボー・ChargingType（lowerRCOn〜ChargingType）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileTriggerStickSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            foreach (ProfileSetting setting in ProfileTriggerStickSettings.All)
            {
                setting.Save(context);
            }
        }
    }
}
