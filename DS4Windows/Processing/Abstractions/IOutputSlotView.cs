using System;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 出力スロット（仮想コントローラー 1 台分）を、UI が表示と設定のために見る面。
    /// UI が出力層の具象型（<c>OutSlotDevice</c>、<c>Xbox360OutDevice</c>）を知らずに済むようにする。
    /// 通知の送り主は、この面自身。
    /// </summary>
    public interface IOutputSlotView
    {
        /// <summary>スロット番号（0 始まり）。</summary>
        int Index { get; }

        /// <summary>接続中の仮想コントローラーの種別名。未接続なら "Empty"。</summary>
        string CurrentTypeName { get; }

        /// <summary>今の種別。接続の要求の前に、UI が選んだ種別を書く。</summary>
        OutContType CurrentType { get; set; }

        /// <summary>仮想コントローラーの接続状態。</summary>
        OutputSlotAttachedStatus AttachedStatus { get; }

        /// <summary>常設か動的かの設定。</summary>
        OutputSlotReserveStatus ReserveStatus { get; set; }

        /// <summary>入力コントローラーが割り当てられているか。</summary>
        OutputSlotInputBound InputBound { get; }

        /// <summary>常設のときの種別。</summary>
        OutContType PermanentType { get; set; }

        /// <summary>XInput のスロット番号を表示できる種別（Xbox 360 でその機能がある）か。</summary>
        bool SupportsXInputSlotNumber { get; }

        /// <summary>XInput のスロット番号（0 始まり）。Xbox 360 でなければ、または未確定なら -1。</summary>
        int XInputSlotNumber { get; }

        /// <summary>割り当てられている入力コントローラーのスロット番号（0 始まり）。ないとき -1。</summary>
        int InputIndex { get; }

        /// <summary>割り当てられている入力コントローラーの表示名。</summary>
        string InputDisplayString { get; }

        /// <summary>常設の種別が変わった。</summary>
        event EventHandler PermanentTypeChanged;

        /// <summary>入力コントローラーの割り当てが変わった。</summary>
        event EventHandler InputBoundChanged;
    }
}
