using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase10-Step6 I3（Phase10-Step6-Plan.md §4）: 読み込みの部品（Controls タブ側）。Controls のボタン 1 つの割り当て（<see cref="DS4ControlSettings"/>）のうち、
    /// SA と共通の種別（現在は SA 専用の種別 5 つ［<see cref="ControlBindingBuilder.TryInterpretSpecial"/>］）を、共通の割り当て定義（<see cref="AssignmentDefinition"/>）にする。
    /// Key と Macro は I4 で builder と一緒に移す。Controls 固有の種別（パッドへの出力、マウス、アナログマウス、素通し）は、定義にしない（2026-10-21 ユーザー決定）。
    /// </summary>
    public static class ControlsAssignmentReader
    {
        /// <summary>
        /// 定義と、実行時の状態の入れ物（合成の <see cref="SpecialAction"/>。名前は <c>Synthetic_Special_{device}_{control}</c>）を作る。
        /// 共通の種別の割り当てでないとき（<see cref="ControlBindingBuilder.TryInterpretSpecial"/> が偽）は偽。
        /// </summary>
        public static bool TryReadSpecial(int device, DS4ControlSettings dcs, out AssignmentDefinition definition, out SpecialAction source)
        {
            definition = null;
            source = null;
            if (!ControlBindingBuilder.TryInterpretSpecial(dcs, out ControlSpecialItem item)) return false;

            string name = $"Synthetic_Special_{device}_{(int)dcs.control}";
            var triggers = new List<DS4Controls> { dcs.control };
            source = SpecialActionItemMapper.ToSpecialAction(name, triggers, item);
            definition = new AssignmentDefinition(name, name, AssignmentOrigin.Controls, item.Type, triggers, item.HoldSeconds, item);
            return true;
        }
    }
}
