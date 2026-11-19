
namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: BT のポーリング・スティックとトリガーの出力（曲線、四角形、アンチスナップバック、出力モード）・タッチパッドのマウス／スティック・DualSense（BTPollRate〜）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileOutputCurveSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            foreach (ProfileSetting setting in ProfileOutputCurveSettings.All)
            {
                setting.Save(context);
            }
        }
    }
}
