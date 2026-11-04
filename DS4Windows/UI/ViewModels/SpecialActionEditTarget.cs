using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: SA のモードの編集の対象。従来の <c>SpecialActEditorViewModel</c> の、名前・新規／編集・
    /// 名前の検証（空でないこと、重複していないこと）を、そのまま持つ。
    /// </summary>
    public sealed class SpecialActionEditTarget : IActionEditTarget
    {
        private readonly ISpecialActionRepository specialActionRepository;

        // Phase6-Step10-3-2: Global.GetActions の置換先（必須引数。ActionList は Global.GetActions と同じリスト）
        public SpecialActionEditTarget(ISpecialActionRepository specialActionRepository, SpecialAction savedAction)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            SavedAction = savedAction;
            EditMode = savedAction != null;
        }

        public string Name { get; set; }

        public bool NameEditable => true;

        public bool EditMode { get; }

        public SpecialAction SavedAction { get; }

        public IReadOnlyList<string> ValidateName(out bool alreadyExists)
        {
            alreadyExists = false;
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("No name provided");
            }
            else if (!EditMode || SavedAction.name != Name)
            {
                // Perform existing name check when creating a new action
                // or if the action name has changed. Use normalized names so
                // that case/whitespace differences are treated as duplicates.
                string normNew = DS4Windows.Util.NormalizeActionName(Name);
                foreach (SpecialAction sA in specialActionRepository.ActionList)
                {
                    string normExisting = DS4Windows.Util.NormalizeActionName(sA?.name);
                    if (string.Equals(normExisting, normNew, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Existing action with name already exists");
                        alreadyExists = true;
                        break;
                    }
                }
            }

            return errors;
        }
    }
}
