using System;
using System.IO;
using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.7a（Phase8-Step7-7-Plan.md Q1＝A。ユーザーの決定）: XboxGameDVR 種別の削除の確認。
    /// 実行時にマクロが実行されず、編集画面から新しく作れず、保存でも書き出されない機能だったため、機能ごと削除した。
    /// </summary>
    public class Phase8Step77aXboxGameDvrRemovalTests
    {
        [Fact]
        public void TheTypeIsGone_FromTheEnumeration()
        {
            Assert.DoesNotContain(Enum.GetNames(typeof(SpecialAction.ActionTypeId)), n => n == "XboxGameDVR");
        }

        [Fact]
        public void AnOldEntryOfTheRemovedType_BecomesAnUnknownEntry_AndDoesNothing()
        {
            // 古い Actions.xml に Type が XboxGameDVR の項目が残っていても、例外にならず、種別が不明（None）になる
            var sa = new SpecialAction("p8s77a_old", "L1", "XboxGameDVR", "0,1,2,65", 0, string.Empty);

            Assert.Equal(SpecialAction.ActionTypeId.None, sa.typeID);
            Assert.Equal("XboxGameDVR", sa.type);
        }

        [Fact]
        public void TheMultiActionType_StillKeepsItsDetailsAsGiven()
        {
            // MultiAction の details（タップ、長押し、ダブルタップのマクロ）は、そのまま保たれる（旧経路は、この文字列をそのままマクロとして実行する）
            var sa = new SpecialAction("p8s77a_multi", "PS", "MultiAction", "272/400/272,65/65,", 0, string.Empty);

            Assert.Equal(SpecialAction.ActionTypeId.MultiAction, sa.typeID);
            Assert.Equal("272/400/272,65/65,", sa.details);
        }

        [Fact]
        public void TheSources_NoLongerMentionTheRemovedType()
        {
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            string specialAction = SourceFileLocator.Find("DS4Windows/DS4Control/SpecialAction.cs");
            if (mapping == null || specialAction == null) return;

            Assert.DoesNotContain("XboxGameDVR", File.ReadAllText(mapping));
            string text = File.ReadAllText(specialAction);
            Assert.DoesNotContain("ActionTypeId.XboxGameDVR", text);
            Assert.DoesNotContain("type == \"XboxGameDVR\"", text);
        }
    }
}
