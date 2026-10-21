using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I3（Phase10-Step6-Plan.md §4）: 読み込みの部品（SpecialActions タブ側）。<see cref="SpecialAction"/> を、共通の割り当て定義（<see cref="AssignmentDefinition"/>）にする。
    /// 種別ごとの文字列の解釈は <see cref="SpecialActionItemMapper"/>（Program、Profile ほか）と、<see cref="ActionBindingBuilder.InterpretKey"/>／<see cref="ActionBindingBuilder.InterpretButton"/>（Key、Button）が行う。
    /// 定義は設定の静的な内容だけで、実行時の状態（<c>prevProfileName</c>、<c>tappedOnce</c> など）は、引き続き <see cref="SpecialAction"/> のオブジェクトが持つ。
    /// </summary>
    public static class SpecialActionAssignmentReader
    {
        /// <summary>
        /// 定義にする。SA が null、種別が不明（None）、トリガーのボタンがない SA は null（旧経路でも束縛を作らない）。
        /// <paramref name="origin"/> が Controls のとき、トリガーは 1 つ目だけを使う（Controls の合成の SA は、トリガーがそのボタン 1 つ）。
        /// </summary>
        public static AssignmentDefinition Read(SpecialAction sa, AssignmentOrigin origin = AssignmentOrigin.SpecialAction)
        {
            if (sa == null || sa.typeID == SpecialAction.ActionTypeId.None) return null;
            if (sa.trigger == null || sa.trigger.Count == 0) return null;

            DS4Controls[] triggers = origin == AssignmentOrigin.Controls
                ? new[] { sa.trigger[0] }
                : sa.trigger.ToArray();

            return new AssignmentDefinition(sa.id, sa.name, origin, sa.typeID, triggers, sa.delayTime < 0 ? 0 : sa.delayTime,
                SpecialActionItemMapper.ToItem(sa),
                key: sa.typeID == SpecialAction.ActionTypeId.Key ? ActionBindingBuilder.InterpretKey(sa) : (KeyBindingValues?)null,
                button: sa.typeID == SpecialAction.ActionTypeId.Button ? ActionBindingBuilder.InterpretButton(sa) : (ButtonBindingValues?)null,
                macro: sa.macro, keyType: sa.keyType, extras: ReadExtras(sa));
        }

        // Phase10-Step6 I5b: SA に付けた Extras（Controls と同じ書式）。付けていない・読めないときは null
        private static ExtrasBindingValues? ReadExtras(SpecialAction sa)
            => Services.ControlBindingBuilder.TryInterpretExtras(sa.outputExtras, out ExtrasBindingValues values) ? values : (ExtrasBindingValues?)null;
    }
}
