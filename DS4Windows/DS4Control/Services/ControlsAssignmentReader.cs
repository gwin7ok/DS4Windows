using System.Collections.Generic;
using DS4Windows.Actions;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase10-Step6 I3（Phase10-Step6-Plan.md §4）: 読み込みの部品（Controls タブ側）。Controls のボタン 1 つの割り当て（<see cref="DS4ControlSettings"/>）のうち、
    /// SA と共通の種別（現在は SA 専用の種別 5 つ［<see cref="ControlBindingBuilder.TryInterpretSpecial"/>］）を、共通の割り当て定義（<see cref="AssignmentDefinition"/>）にする。
    /// Key と Macro は I4b で追加した（<see cref="TryReadKey"/>、<see cref="TryReadMacro"/>）。Controls 固有の種別（パッドへの出力、マウス、アナログマウス、素通し）は、定義にしない（2026-10-21 ユーザー決定）。
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

        /// <summary>
        /// Phase10-Step6 I4b: Key の割り当てを定義にする。解釈済みの値（別名キー、ScanCode、Toggle）は定義の <see cref="AssignmentDefinition.Key"/> が運ぶ。
        /// 実行時の状態の入れ物は、旧経路の合成と同じ名前・種別（<c>Synthetic_Key_{device}_{control}</c>。状態は名前で引かれる）。
        /// Key の新経路の条件（<see cref="ControlBindingBuilder.TryInterpretKey"/>）を満たさないときは偽。
        /// </summary>
        public static bool TryReadKey(int device, DS4ControlSettings dcs, out AssignmentDefinition definition, out SpecialAction source)
        {
            definition = null;
            source = null;
            if (!ControlBindingBuilder.TryInterpretKey(dcs, out KeyBindingValues values)) return false;

            string name = $"Synthetic_Key_{device}_{(int)dcs.control}";
            var identity = new SpecialAction(name, "Synthetic", "Key", "Key", 0, string.Empty);
            identity.typeID = SpecialAction.ActionTypeId.Key;
            identity.type = "Key";
            identity.details = values.LogicalKey.ToString();
            DS4KeyType keyType = DS4KeyType.None;
            if (values.ScanCode) keyType |= DS4KeyType.ScanCode;
            if (values.Toggle) keyType |= DS4KeyType.Toggle;
            identity.keyType = keyType;

            source = identity;
            definition = new AssignmentDefinition(name, name, AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Key,
                new List<DS4Controls> { dcs.control }, key: values, keyType: keyType);
            return true;
        }

        /// <summary>
        /// Phase10-Step6 I4b: Macro の割り当てを定義にする。手順と keyType は定義へ、「Run on trigger release」などの 3 項目（型のある項目）は <see cref="AssignmentDefinition.Item"/> へ。
        /// 実行時の状態の入れ物は、旧経路の合成と同じ名前（<c>Synthetic_Macro_{device}_{control}</c>。再生のキューの識別子は、ボタンごとに別）。
        /// Macro の新経路の条件（<see cref="ControlBindingBuilder.TryInterpretMacro"/>）を満たさないときは偽。
        /// </summary>
        public static bool TryReadMacro(int device, DS4ControlSettings dcs, out AssignmentDefinition definition, out SpecialAction source)
        {
            definition = null;
            source = null;
            if (!ControlBindingBuilder.TryInterpretMacro(dcs, out int[] steps)) return false;

            string name = $"Synthetic_Macro_{device}_{(int)dcs.control}";
            var identity = new SpecialAction(name, "Synthetic", "Macro", string.Join("/", steps), 0, string.Empty);
            identity.typeID = SpecialAction.ActionTypeId.Macro;
            identity.type = "Macro";
            identity.keyType = dcs.keyType;
            identity.controls = name;

            // Phase10-Step4 W3: Macro の SA 専用の 3 項目（Run on trigger release／Synchronized Run／Keep key state）。項目がなければ（従来の Macro）、すべて偽
            ControlSpecialItem options = dcs.special != null && dcs.special.Type == SpecialAction.ActionTypeId.Macro ? dcs.special : null;
            identity.pressRelease = options != null && options.RunOnRelease;
            identity.synchronized = options != null && options.Synchronized;
            identity.keepKeyState = options != null && options.KeepKeyState;

            source = identity;
            definition = new AssignmentDefinition(name, name, AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Macro,
                new List<DS4Controls> { dcs.control }, item: options, macro: steps, keyType: dcs.keyType);
            return true;
        }
    }
}
