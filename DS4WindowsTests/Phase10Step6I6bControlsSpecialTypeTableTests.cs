using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I6b（Phase10-Step6-Plan.md §4）: Controls で実行時に束縛を作る SA 専用の種別の規則の表（<see cref="ControlsSpecialTypeTable"/>）と、
    /// 割り当て編集画面の種別の一覧（<see cref="ActionTypeCatalog"/>）の「Controls で保存できる」が、同じ規則に従うこと。種別ごとの入力（<see cref="IActionTypeEditor"/>）の
    /// 型のある項目との往復の既定の動作。
    /// </summary>
    public class Phase10Step6I6bControlsSpecialTypeTableTests
    {
        [Theory]
        [InlineData(SpecialAction.ActionTypeId.Program, true)]
        [InlineData(SpecialAction.ActionTypeId.Profile, true)]
        [InlineData(SpecialAction.ActionTypeId.DisconnectBT, true)]
        [InlineData(SpecialAction.ActionTypeId.GyroCalibrate, true)]
        [InlineData(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, true)]
        [InlineData(SpecialAction.ActionTypeId.BatteryCheck, false)]
        [InlineData(SpecialAction.ActionTypeId.MultiAction, false)]
        [InlineData(SpecialAction.ActionTypeId.Macro, false)]   // Macro は Macro の割り当て（3 項目の入れ物）で扱う
        [InlineData(SpecialAction.ActionTypeId.Key, false)]
        public void TheTable_ListsTheTypesThatControlsBuilds(SpecialAction.ActionTypeId type, bool expected)
        {
            Assert.Equal(expected, ControlsSpecialTypeTable.IsSupported(type));
        }

        [Fact]
        public void TheCatalog_FollowsTheTable_ForEverySaSpecificType()
        {
            var catalog = new ActionTypeCatalog();

            foreach (ActionTypeDescriptor descriptor in catalog.All)
            {
                switch (descriptor.TypeId)
                {
                    case SpecialAction.ActionTypeId.Program:
                    case SpecialAction.ActionTypeId.Profile:
                    case SpecialAction.ActionTypeId.DisconnectBT:
                    case SpecialAction.ActionTypeId.BatteryCheck:
                    case SpecialAction.ActionTypeId.MultiAction:
                    case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                    case SpecialAction.ActionTypeId.GyroCalibrate:
                        Assert.Equal(ControlsSpecialTypeTable.IsSupported(descriptor.TypeId), descriptor.IsSavableIn(ActionEditorModes.Control));
                        Assert.True(descriptor.IsSavableIn(ActionEditorModes.SpecialAction));
                        break;
                }
            }
        }

        private sealed class DefaultOnlyEditor : IActionTypeEditor
        {
            public void LoadAction(SpecialAction action) { }
            public void SaveAction(SpecialAction action, bool edit = false) { }
            public bool IsValid(SpecialAction action) => true;
        }

        [Fact]
        public void AnEditorWithoutTheControlItemMembers_UsesTheDefaults()
        {
            IActionTypeEditor editor = new DefaultOnlyEditor();

            editor.LoadControlItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program });   // 何もしない（例外にしない）
            ControlSpecialItem item = editor.ToControlItem(SpecialAction.ActionTypeId.BatteryCheck);

            Assert.Equal(SpecialAction.ActionTypeId.BatteryCheck, item.Type);
            Assert.Equal(0, item.HoldSeconds);
        }

        [Fact]
        public void TheEditorSet_RoutesTheControlItemByType_AndDefaultsWhenThereIsNoEditor()
        {
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [SpecialAction.ActionTypeId.Key] = new DefaultOnlyEditor(),
            });

            ControlSpecialItem none = set.ToControlItem(SpecialAction.ActionTypeId.Profile);   // 対応する入力なし
            Assert.Equal(SpecialAction.ActionTypeId.Profile, none.Type);

            set.LoadControlItem(null);   // 何もしない
            set.LoadControlItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program });   // 対応する入力なし
        }
    }
}
