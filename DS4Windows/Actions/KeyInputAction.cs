using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step3): Step3 で ComboTriggerInputAction（D2＝B1）を導入するのと同時に削除する。Key 専用のプレースホルダー（Evaluate は null を返すだけ）で、2025-12-19（efcfa29e）から呼び出し元 0 件。Phase8-Step1-Orphan-Decisions.md §2 B。
    public class KeyInputAction : IInputAction
    {
        private readonly SpecialAction sa;
        public KeyInputAction(SpecialAction sa) { this.sa = sa; }
        public string Name => sa?.name ?? "KeyInput";

        public ITriggerContext Evaluate(DS4State state)
        {
            // Legacy trigger evaluation remains in Mapping/KeyAction; this is a placeholder for future use.
            return null;
        }

        public void Reset() { }
    }
}
