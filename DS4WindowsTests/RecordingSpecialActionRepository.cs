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
        public string SavedActionId;   // Phase10-Step4d W2

        public string ActionsPath => string.Empty;
        public IReadOnlyList<SpecialAction> Actions => ActionList;
        public List<SpecialAction> ActionList { get; } = new List<SpecialAction>();
        public bool LoadActions() => true;
        public void CreateStandardActions() { }
        public SpecialAction GetActionOrPlaceholder(string actionName) => null;

        public void SaveAction(string name, string controls, int mode, string details, bool edit, double delayTime = 0.0, string extras = "", string actionId = null)
        {
            SavedActionId = actionId;
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
        public string DuplicateAction(string actionName) => null;
        // Phase10-Step6 I5a: SA の保存のあとに呼ばれる Extras の保存を記録する
        public System.Collections.Generic.List<(string Name, string Extras)> ExtrasSaves { get; } = new System.Collections.Generic.List<(string, string)>();
        public bool SaveActionExtras(string actionName, string outputExtras) { ExtrasSaves.Add((actionName, outputExtras)); return true; }
        // Phase10-Step6 後の Step5: SA の保存のあとに呼ばれる、アナログな出力の強さの保存を記録する
        public System.Collections.Generic.List<(string Name, int Strength)> StrengthSaves { get; } = new System.Collections.Generic.List<(string, int)>();
        public bool SaveActionAnalogStrength(string actionName, int strength) { StrengthSaves.Add((actionName, strength)); return true; }
        public event EventHandler ActionsChanged { add { } remove { } }
    }
}
