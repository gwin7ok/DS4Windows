using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{

    // Minimal trigger interface. Concrete trigger implementations can carry richer semantics.
    // TODO(Phase8-Step3): Step3 で ComboTriggerInputAction（D2＝B1）を導入するのと同時に、SingleButtonTrigger とともに削除する。同じ役割の並行実装で、呼び出し元 0 件（2025-12-18、5e231d18）。TriggerType／TriggerContext は利用中のため残す。
    public interface ITrigger
    {
        TriggerType Type { get; }
        TriggerContext Context { get; }
    }
}
