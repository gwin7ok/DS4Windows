using System;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1・§5）: 種別の一覧（<see cref="ActionTypeCatalog"/>）。並び・種別の ID は、従来の
    /// <c>SpecialActEditorViewModel.typeAssoc</c> と、<c>AssignmentEditor.xaml</c> のドロップダウンの項目の並びと同じ。
    /// </summary>
    public class Phase10Step2W1ActionTypeCatalogTests
    {
        private static readonly SpecialAction.ActionTypeId[] LegacyTypeAssoc =
        {
            SpecialAction.ActionTypeId.None, SpecialAction.ActionTypeId.Macro,
            SpecialAction.ActionTypeId.Program, SpecialAction.ActionTypeId.Profile,
            SpecialAction.ActionTypeId.Key, SpecialAction.ActionTypeId.DisconnectBT,
            SpecialAction.ActionTypeId.BatteryCheck, SpecialAction.ActionTypeId.MultiAction,
            SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, SpecialAction.ActionTypeId.GyroCalibrate
        };

        [Fact]
        public void TheTypeIds_AreTheLegacyTypeAssocInTheSameOrder()
        {
            var catalog = new ActionTypeCatalog();

            Assert.Equal(LegacyTypeAssoc, catalog.TypeIds);
            Assert.Equal(LegacyTypeAssoc, catalog.All.Select(d => d.TypeId).ToArray());
        }

        [Fact]
        public void TheIndexes_AreThePositions_AndTheLabelsFollowTheXamlOrder()
        {
            var catalog = new ActionTypeCatalog();

            Assert.Equal(Enumerable.Range(0, 10), catalog.All.Select(d => d.Index));
            Assert.Equal(new[]
            {
                "SpecialActionEdit.SelectAction", "SpecialActionEdit.RecordMacro", "SpecialActionEdit.LaunchProgram",
                "SpecialActionEdit.LoadProfile", "SpecialActionEdit.PressKey", "SpecialActionEdit.DisconnectBT",
                "SpecialActionEdit.CheckBattery", "SpecialActionEdit.MultiAction", "SpecialActionEdit.SixAxisWheelCalib",
                "SpecialActionEdit.CalibrateGyro"
            }, catalog.All.Select(d => d.LabelKey).ToArray());
        }

        [Fact]
        public void IndexOf_ReturnsThePosition_OfEveryTypeInTheList()
        {
            var catalog = new ActionTypeCatalog();

            for (int i = 0; i < LegacyTypeAssoc.Length; i++)
                Assert.Equal(i, catalog.IndexOf(LegacyTypeAssoc[i]));
        }

        [Fact]
        public void IndexOf_Button_IsTheKeyItem_BecauseThePressKeyTabEditsBoth()
        {
            var catalog = new ActionTypeCatalog();

            Assert.Equal(catalog.IndexOf(SpecialAction.ActionTypeId.Key), catalog.IndexOf(SpecialAction.ActionTypeId.Button));
            Assert.Equal(4, catalog.IndexOf(SpecialAction.ActionTypeId.Button));
        }

        [Fact]
        public void At_ReturnsTheItem_AndRejectsAnIndexOutOfRange()
        {
            var catalog = new ActionTypeCatalog();

            Assert.Equal(SpecialAction.ActionTypeId.Program, catalog.At(2).TypeId);
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.At(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.At(10));
        }

        [Fact]
        public void EveryType_IsAvailableInBothModes_ByTheApprovedPolicy()
        {
            var catalog = new ActionTypeCatalog();

            Assert.All(catalog.All, d => Assert.True(d.IsAvailableIn(ActionEditorModes.SpecialAction)));
            Assert.All(catalog.All, d => Assert.True(d.IsAvailableIn(ActionEditorModes.Control)));
            Assert.Equal(10, catalog.For(ActionEditorModes.Both).Count());
        }

        [Fact]
        public void SavableTypes_AreEveryRealTypeForSpecialActions_AndOnlyKeyAndMacroForControlsUntilStep3()
        {
            var catalog = new ActionTypeCatalog();

            // SA: 「種別を選んでください」（None）以外は、すべて保存できる
            Assert.False(catalog.At(0).IsSavableIn(ActionEditorModes.SpecialAction));
            Assert.All(catalog.All.Skip(1), d => Assert.True(d.IsSavableIn(ActionEditorModes.SpecialAction)));

            // Controls: Key・Button・Macro に加えて、Phase10-Step4 の群ごとの縦通しで、保存・実行・画面が通った SA 専用の種別を足していく
            // （W4: Disconnect BT、Calibrate sixaxis wheel、Calibrate gyro。W5: Launch Program。W6: Load Profile）
            var controlsSavable = catalog.All.Where(d => d.IsSavableIn(ActionEditorModes.Control)).Select(d => d.TypeId).ToArray();
            Assert.Equal(new[]
            {
                SpecialAction.ActionTypeId.Macro,
                SpecialAction.ActionTypeId.Program,
                SpecialAction.ActionTypeId.Profile,
                SpecialAction.ActionTypeId.Key,
                SpecialAction.ActionTypeId.DisconnectBT,
                SpecialAction.ActionTypeId.BatteryCheck,   // Phase10-Step6 W7 相当
                SpecialAction.ActionTypeId.MultiAction,   // Phase10-Step6 W8 相当
                SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate,
                SpecialAction.ActionTypeId.GyroCalibrate,
            }, controlsSavable);
        }

        [Fact]
        public void TheDescriptor_RejectsNoMode()
        {
            var catalog = new ActionTypeCatalog();

            Assert.False(catalog.At(1).IsAvailableIn(ActionEditorModes.None));
            Assert.False(catalog.At(1).IsSavableIn(ActionEditorModes.None));
        }
    }
}
