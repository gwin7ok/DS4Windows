using System;
using System.Collections.Generic;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// UI 向けの、接続中のコントローラーの読み取りの窓口（規約 1 章。UI は第 1 層の具象と <c>ControlService</c> を持たない）。
    /// 機器の集合のロックは窓口の内側で取り、UI にはロックを渡さない。
    /// </summary>
    public interface IControllerSlotQuery
    {
        /// <summary>入力スロットの数。</summary>
        int SlotCount { get; }

        /// <summary>そのスロットのコントローラー。未接続・範囲外・サービス停止中は null。</summary>
        IControllerSlotView GetSlot(int slot);

        /// <summary>接続中のコントローラーの一覧。機器の集合のロックの内側で作った複製で、並びは接続の順。</summary>
        IReadOnlyList<ControllerSlotEntry> Snapshot();

        /// <summary>コントローラーが接続されスロットに入ったとき（サービスのスレッドで発生する）。</summary>
        event EventHandler<ControllerSlotChangedEventArgs> SlotChanged;
    }
}
