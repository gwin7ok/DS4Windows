using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 共通の編集画面の ViewModel の土台。名前・トリガー・種別の添字と、その検証を持つ。
    /// 従来の <c>SpecialActEditorViewModel</c> の中身を、「トリガーの供給元」（<see cref="IActionTriggerSource"/>）、「編集の対象」
    /// （<see cref="IActionEditTarget"/>）、「種別の一覧」（<see cref="ActionTypeCatalog"/>）に分けて、委譲する形にした。
    /// SA のモードは <see cref="SpecialActEditorViewModel"/>（従来と同じ公開メンバー・同じ検証・同じエラーの文言）。
    /// Controls のモードは、W3・W4 で、供給元と対象を差し替えて作る（まだ使われない）。
    /// </summary>
    public class ActionEditorViewModel : NotifyDataErrorBase
    {
        private readonly int deviceNum;
        private readonly IActionTriggerSource triggerSource;
        private readonly IActionEditTarget editTarget;
        private readonly ActionTypeCatalog typeCatalog;
        private int actionTypeIndex = 0;
        private bool existingName;

        public ActionEditorViewModel(int deviceNum, IActionTriggerSource triggerSource, IActionEditTarget editTarget,
            ActionTypeCatalog typeCatalog)
        {
            this.deviceNum = deviceNum;
            this.triggerSource = triggerSource ?? throw new System.ArgumentNullException(nameof(triggerSource));
            this.editTarget = editTarget ?? throw new System.ArgumentNullException(nameof(editTarget));
            this.typeCatalog = typeCatalog ?? throw new System.ArgumentNullException(nameof(typeCatalog));
        }

        public int DeviceNum { get => deviceNum; }
        public int ActionTypeIndex { get => actionTypeIndex; set => actionTypeIndex = value; }

        /// <summary>名前。編集できない対象（Controls のモード）では、書き込みを無視する。</summary>
        public string ActionName
        {
            get => editTarget.Name;
            set
            {
                if (editTarget.NameEditable) editTarget.Name = value;
            }
        }

        /// <summary>名前を編集できるか（Controls のモードは偽）。</summary>
        public bool ActionNameEditable => editTarget.NameEditable;

        public IActionTriggerSource TriggerSource => triggerSource;
        public IActionEditTarget EditTarget => editTarget;
        public ActionTypeCatalog TypeCatalog => typeCatalog;

        /// <summary>添字 → 種別の ID の配列（従来の <c>typeAssoc</c> と同じ並び）。</summary>
        public SpecialAction.ActionTypeId[] TypeAssoc { get => typeCatalog.TypeIds; }
        public SpecialAction SavedAction { get => editTarget.SavedAction; }
        public bool EditMode { get => editTarget.EditMode; }

        public bool TriggerError
        {
            get
            {
                return errors.TryGetValue("TriggerError", out List<string> _);
            }
        }

        public bool ExistingName { get => existingName; }

        /// <summary>保存済みの SA から、トリガー・種別の添字・名前を読み込む（SA のモードだけ。Controls のモードは呼ばない）。</summary>
        public void LoadAction(SpecialAction action)
        {
            foreach (string s in action.controls.Split('/'))
            {
                triggerSource.SetTrigger(s, true);
            }

            if (action.ucontrols != null)
            {
                foreach (string s in action.ucontrols.Split('/'))
                {
                    if (s != "AutomaticUntrigger")
                    {
                        triggerSource.SetUnloadTrigger(s, true);
                    }
                }
            }

            editTarget.Name = action.name;

            // Button は、一覧に独立した項目がなく、Key の項目（Press Key のタブ）で編集する（IndexOf が、Key の添字を返す）
            actionTypeIndex = typeCatalog.IndexOf(action.typeID);
        }

        /// <summary>画面の入力を、SA に書き出す（名前、トリガー、解除トリガー［あれば］、種別）。</summary>
        public void SetAction(SpecialAction action)
        {
            action.name = editTarget.Name;
            action.controls = triggerSource.TriggersText;
            string unload = triggerSource.UnloadTriggersText;
            if (unload != null)
            {
                action.ucontrols = unload;
            }

            action.typeID = typeCatalog.TypeIds[actionTypeIndex];
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> actionNameErrors = new List<string>();
            List<string> triggerErrors = new List<string>();
            List<string> typeErrors = new List<string>();

            IReadOnlyList<string> nameErrors = editTarget.ValidateName(out bool alreadyExists);
            if (nameErrors.Count > 0)
            {
                valid = false;
                actionNameErrors.AddRange(nameErrors);
                if (alreadyExists) existingName = true;
            }

            if (!triggerSource.Validate(out string triggerError))
            {
                valid = false;
                triggerErrors.Add(triggerError);
            }

            if (ActionTypeIndex == 0)
            {
                valid = false;
                typeErrors.Add("Specify an action type");
            }

            if (actionNameErrors.Count > 0)
            {
                errors["ActionName"] = actionNameErrors;
                RaiseErrorsChanged("ActionName");
            }
            if (triggerErrors.Count > 0)
            {
                errors["TriggerError"] = triggerErrors;
                RaiseErrorsChanged("TriggerError");
            }
            if (typeErrors.Count > 0)
            {
                errors["ActionTypeIndex"] = typeErrors;
                RaiseErrorsChanged("ActionTypeIndex");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            existingName = false;

            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("ActionName");
                RaiseErrorsChanged("TriggerError");
                RaiseErrorsChanged("ActionTypeIndex");
            }
        }
    }
}
