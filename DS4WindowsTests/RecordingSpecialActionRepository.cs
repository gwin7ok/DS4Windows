using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W1: <see cref="ISpecialActionRepository"/> の、記録用の偽物。<c>SaveAction</c> の引数を、そのまま記録する。
    /// <see cref="ActionList"/> は、名前の重複の検査に使う既存のアクションの一覧（テストが設定する）。
    /// </summary>
    internal sealed class RecordingSpecialActionRepository : ISpecialActionRepository
    {
        public int SaveCount;
        public string SavedName;
        public string SavedControls;
        public int SavedMode;
        public string SavedDetails;
        public bool SavedEdit;
        public double SavedDelay;
        public string SavedExtras;

        public string ActionsPath => string.Empty;
        public IReadOnlyList<SpecialAction> Actions => ActionList;
        public List<SpecialAction> ActionList { get; } = new List<SpecialAction>();
        public bool LoadActions() => true;
        public void CreateStandardActions() { }
        public SpecialAction GetActionOrPlaceholder(string actionName) => null;

        public void SaveAction(string name, string controls, int mode, string details, bool edit, double delayTime = 0.0, string extras = "")
        {
            SaveCount++;
            SavedName = name;
            SavedControls = controls;
            SavedMode = mode;
            SavedDetails = details;
            SavedEdit = edit;
            SavedDelay = delayTime;
            SavedExtras = extras;
        }

        public bool RemoveActionAndSave(string actionName) => true;
        public event EventHandler ActionsChanged { add { } remove { } }
    }
}
