using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 編集画面の種別の一覧。並び・種別の ID は、従来の <c>SpecialActEditorViewModel.typeAssoc</c>
    /// （と、<c>SpecialActionEditor.xaml</c> のドロップダウンの項目の並び）と同じ。画面の添字（<c>ActionTypeIndex</c>）と種別の ID の、相互変換を担う。
    /// 状態を持たない（不変）。
    /// </summary>
    public sealed class ActionTypeCatalog
    {
        private readonly ActionTypeDescriptor[] descriptors;
        private readonly SpecialAction.ActionTypeId[] typeIds;

        /// <summary>
        /// Phase10-Step3: Controls のモードの項目（<c>Unbound</c>）を足した一覧の添字。足していない一覧（SA のモード）では -1。
        /// </summary>
        public int UnboundIndex { get; }

        /// <param name="includeControlOnlyItems">
        /// Phase10-Step3: <c>true</c>（Controls のモード）のとき、末尾に「Unbound」の項目（添字 10）を足す。出力ピッカーの Unbound ボタンと同じ設定
        /// （Button の Unbound。出力先がない）を、種別の一覧から選べるようにするため。Default は、添字 0（Controls では、「種別を選んでください」ではなく
        /// 「Default」と表示する）が表す。足さない一覧（SA のモード、既定）は、従来の <c>typeAssoc</c> と同じ並び。
        /// </param>
        public ActionTypeCatalog(bool includeControlOnlyItems = false)
        {
            const ActionEditorModes Both = ActionEditorModes.Both;
            const ActionEditorModes SaOnly = ActionEditorModes.SpecialAction;

            // 並びは、SpecialActionEditor.xaml の actionTypeCombo の項目と同じ（0 は「種別を選んでください」）
            var items = new System.Collections.Generic.List<ActionTypeDescriptor>
            {
                new ActionTypeDescriptor(0, SpecialAction.ActionTypeId.None, "SpecialActionEdit.SelectAction", Both, ActionEditorModes.None),
                new ActionTypeDescriptor(1, SpecialAction.ActionTypeId.Macro, "SpecialActionEdit.RecordMacro", Both, Both),
                new ActionTypeDescriptor(2, SpecialAction.ActionTypeId.Program, "SpecialActionEdit.LaunchProgram", Both, SaOnly),
                new ActionTypeDescriptor(3, SpecialAction.ActionTypeId.Profile, "SpecialActionEdit.LoadProfile", Both, SaOnly),
                new ActionTypeDescriptor(4, SpecialAction.ActionTypeId.Key, "SpecialActionEdit.PressKey", Both, Both),   // Button もこの項目で編集する
                new ActionTypeDescriptor(5, SpecialAction.ActionTypeId.DisconnectBT, "SpecialActionEdit.DisconnectBT", Both, SaOnly),
                new ActionTypeDescriptor(6, SpecialAction.ActionTypeId.BatteryCheck, "SpecialActionEdit.CheckBattery", Both, SaOnly),
                new ActionTypeDescriptor(7, SpecialAction.ActionTypeId.MultiAction, "SpecialActionEdit.MultiAction", Both, SaOnly),
                new ActionTypeDescriptor(8, SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, "SpecialActionEdit.SixAxisWheelCalib", Both, SaOnly),
                new ActionTypeDescriptor(9, SpecialAction.ActionTypeId.GyroCalibrate, "SpecialActionEdit.CalibrateGyro", Both, SaOnly),
            };

            UnboundIndex = -1;
            if (includeControlOnlyItems)
            {
                const ActionEditorModes ControlOnly = ActionEditorModes.Control;
                UnboundIndex = items.Count;
                // 種別の ID は Button（Unbound は、Button の割り当ての 1 つ）。IndexOf(Button) は、Key の項目を返すため、衝突しない
                items.Add(new ActionTypeDescriptor(UnboundIndex, SpecialAction.ActionTypeId.Button, "Unbound", ControlOnly, ControlOnly));
            }

            descriptors = items.ToArray();

            typeIds = new SpecialAction.ActionTypeId[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
                typeIds[i] = descriptors[i].TypeId;
        }

        /// <summary>全項目（添字の順）。</summary>
        public IReadOnlyList<ActionTypeDescriptor> All => descriptors;

        /// <summary>添字 → 種別の ID の配列（従来の <c>typeAssoc</c> と同じ並び）。</summary>
        public SpecialAction.ActionTypeId[] TypeIds => typeIds;

        /// <summary>添字の項目。範囲外は、例外。</summary>
        public ActionTypeDescriptor At(int index)
        {
            if ((uint)index >= (uint)descriptors.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return descriptors[index];
        }

        /// <summary>
        /// 種別の ID から、画面の添字を求める。一覧にない種別は 0（「種別を選んでください」）。
        /// <see cref="SpecialAction.ActionTypeId.Button"/> は、一覧に独立した項目がなく、Key の項目（Press Key のタブ）で編集するため、Key の添字を返す。
        /// </summary>
        public int IndexOf(SpecialAction.ActionTypeId typeId)
        {
            if (typeId == SpecialAction.ActionTypeId.Button) typeId = SpecialAction.ActionTypeId.Key;

            for (int i = 0; i < typeIds.Length; i++)
            {
                if (typeIds[i] == typeId) return i;
            }

            return 0;
        }

        /// <summary>指定のモードで、一覧に出す項目（添字の順）。</summary>
        public IEnumerable<ActionTypeDescriptor> For(ActionEditorModes mode)
        {
            foreach (ActionTypeDescriptor descriptor in descriptors)
            {
                if (descriptor.IsAvailableIn(mode)) yield return descriptor;
            }
        }
    }
}
