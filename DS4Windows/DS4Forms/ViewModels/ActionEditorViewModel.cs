using System.Collections.Generic;
using System.Linq;
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
        private readonly TriggerCatalog triggerCatalog;
        private readonly ActionEditorModes mode;
        private readonly TriggerOptionViewModel[] triggerOptions;
        private int actionTypeIndex = 0;
        private bool existingName;

        public ActionEditorViewModel(int deviceNum, IActionTriggerSource triggerSource, IActionEditTarget editTarget,
            ActionTypeCatalog typeCatalog, TriggerCatalog triggerCatalog, ActionEditorModes mode)
        {
            this.deviceNum = deviceNum;
            this.triggerSource = triggerSource ?? throw new System.ArgumentNullException(nameof(triggerSource));
            this.editTarget = editTarget ?? throw new System.ArgumentNullException(nameof(editTarget));
            this.typeCatalog = typeCatalog ?? throw new System.ArgumentNullException(nameof(typeCatalog));
            this.triggerCatalog = triggerCatalog ?? throw new System.ArgumentNullException(nameof(triggerCatalog));
            this.mode = mode;

            // Phase10-Step2 W2: トリガーの一覧は、データ駆動（旧: 名前つきのチェックボックス 49 個）。チェックの状態は、供給元が持つ
            triggerOptions = triggerCatalog.Items.Select(item => new TriggerOptionViewModel(item, triggerSource)).ToArray();
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

        /// <summary>名前の欄を、読み取り専用にするか（<see cref="ActionNameEditable"/> の逆。XAML の <c>IsReadOnly</c> に結ぶ）。</summary>
        public bool ActionNameReadOnly => !editTarget.NameEditable;

        /// <summary>この画面のモード（SA のトリガーの編集か、Controls のボタン 1 つの割り当ての編集か）。</summary>
        public ActionEditorModes Mode => mode;

        /// <summary>
        /// 種別の一覧の、添字の項目を、選べるか。0（「種別を選んでください」）は常に選べる。ほかは、このモードで<b>現在保存できる</b>種別だけ
        /// （Controls は、保存形式が持つ Key・Button・Macro だけ。Step3 の保存形式の拡張で増やす）。一覧には、全種別を出す（承認済みの方針）。
        /// </summary>
        public bool IsTypeEnabled(int index)
        {
            return index == 0 || typeCatalog.At(index).IsSavableIn(mode);
        }

        public IActionTriggerSource TriggerSource => triggerSource;
        public IActionEditTarget EditTarget => editTarget;
        public ActionTypeCatalog TypeCatalog => typeCatalog;
        public TriggerCatalog TriggerCatalog => triggerCatalog;

        /// <summary>画面に出すトリガーの一覧（チェックボックスの行。<c>ItemsSource</c> に結ぶ）。</summary>
        public IReadOnlyList<TriggerOptionViewModel> TriggerOptions => triggerOptions;

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

            // 一覧にない名前は、外す（旧: コードビハインドが、対応するチェックボックスがなければ、リストから外していた）
            foreach (string name in triggerSource.Triggers.ToArray())
            {
                if (!triggerCatalog.Contains(name)) triggerSource.SetTrigger(name, false);
            }

            foreach (string name in triggerSource.UnloadTriggers.ToArray())
            {
                if (!triggerCatalog.Contains(name)) triggerSource.SetUnloadTrigger(name, false);
            }

            foreach (TriggerOptionViewModel option in triggerOptions)
            {
                option.Refresh();
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
