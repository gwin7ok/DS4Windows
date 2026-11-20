using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>割り当て（DS4ControlSettings）の解釈（TryInterpret…）。</summary>
    public partial class ControlBindingBuilder
    {
        /// <summary>
        /// Macro の新経路の条件（Key と同じ形。Macro、手順あり、Extras なし）を満たすか。満たすとき、手順を返す。
        /// </summary>
        public static bool TryInterpretMacro(DS4ControlSettings dcs, [NotNullWhen(true)] out int[]? steps)
        {
            steps = null;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Macro) return false;

            int[] macro = dcs.action.actionMacro;
            // 手順が空のマクロも、新経路が所有する（SA の Macro と同じ。トリガーが成立すると、空のマクロを実行し、何も出力しない。
            // 通常の出力は、他の割り当てと同じく毎フレーム消える）。以前は「旧経路が、手順がないと例外になる」ため所有しなかったが、旧経路の本体は
            // Phase8-Step8 8a で削除済みで、所有しないと、割り当てたのに通常の出力が出てしまっていた。手順そのものがない（null）ものは、従来どおり所有しない
            if (macro == null) return false;

            steps = macro;
            return true;
        }

        /// <summary>
        /// 割り当てが、Key の新経路の条件（クラスの説明）を満たすか。満たすときは、解釈済みの値を返す。
        /// </summary>
        public static bool TryInterpretKey(DS4ControlSettings dcs, out KeyBindingValues values)
        {
            values = default;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Key) return false;

            int key = dcs.action.actionKey;
            if (key < ushort.MinValue || key > ushort.MaxValue) return false;   // 旧経路は範囲外で例外になる。新経路は所有せず、旧経路の挙動を保つ

            DS4KeyType keyType = dcs.keyType;
            values = new KeyBindingValues((ushort)key, keyType.HasFlag(DS4KeyType.ScanCode), keyType.HasFlag(DS4KeyType.Toggle),
                dcs.action.actionAlias);
            return true;
        }

        /// <summary>
        /// Extras（カンマ区切りの 9 個の値）の解釈。空（<c>IsExtrasEmpty</c>）なら偽（Extras なし）。
        /// 9 個未満だと、旧経路は途中の欄の参照で例外になり、そこまでの効果だけが残る。その挙動を保つため、新経路は所有せず、偽を返す。
        /// 数値として読めない欄は 0。
        /// </summary>
        public static bool TryInterpretExtras(string? extras, out ExtrasBindingValues values)
        {
            values = default;
            if (string.IsNullOrEmpty(extras) || extras == "0,0,0,0,0,0,0,0,0") return false;

            string[] parts = extras.Split(',');
            if (parts.Length < 9) return false;

            var numbers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int n)) numbers[i] = n;
            }

            values = new ExtrasBindingValues(numbers[0], numbers[1], numbers[2] == 1, numbers[3], numbers[4], numbers[5], numbers[6], numbers[7] == 1, numbers[8]);
            return true;
        }

        /// <summary>
        /// Button のうち、パッド（X360 の軸・トリガー・ボタン）への割り当ての解釈。出力先が Unbound／None なら、消すだけ（<paramref name="eraseOnly"/>）。
        /// マウス・ホイール・タッチパッドクリックへの割り当て（7.8f-2・7.8f-3）と、Toggle つきは偽（Toggle つきは、<see cref="TryInterpretPadTarget"/> と Toggle の出力を組み合わせて扱う［7.8f-2］）。
        /// </summary>
        public static bool TryInterpretPad(DS4ControlSettings dcs, out PadBindingValues values, out bool eraseOnly)
        {
            values = default;
            eraseOnly = false;
            if (dcs == null) return false;
            if (dcs.keyType.HasFlag(DS4KeyType.Toggle)) return false;
            return TryInterpretPadTarget(dcs, out values, out eraseOnly);
        }

        /// <summary><see cref="TryInterpretPad"/> と同じ解釈で、Toggle の有無を見ない（Toggle は別の出力が担う）。</summary>
        public static bool TryInterpretPadTarget(DS4ControlSettings dcs, out PadBindingValues values, out bool eraseOnly)
        {
            values = default;
            eraseOnly = false;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            if (xbox == X360Controls.None || xbox == X360Controls.Unbound)
            {
                eraseOnly = true;
                return true;
            }

            if (xbox < X360Controls.LXNeg || xbox > X360Controls.Start) return false;

            DS4Controls output = X360ControlTables.reverseX360ButtonMapping[(int)xbox];
            PadTargetKind kind = output >= DS4Controls.LXNeg && output <= DS4Controls.RYPos ? PadTargetKind.Axis
                : output == DS4Controls.L2 || output == DS4Controls.R2 ? PadTargetKind.Trigger
                : PadTargetKind.Button;
            values = new PadBindingValues(dcs.control, output, kind);
            return true;
        }

        /// <summary>
        /// Button のうち、マウスのボタン（左・右・中・4・5）、タッチパッドクリック、デジタルのホイール（元がボタン）への割り当ての解釈。
        /// Toggle の有無は見ない（Toggle は別の出力が担う）。元がスティック・トリガー・ジャイロのホイール（アナログ）と、マウスの移動は偽（7.8f-3。旧経路が処理する）。
        /// </summary>
        public static bool TryInterpretMouse(DS4ControlSettings dcs, out X360Controls target)
        {
            target = X360Controls.None;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            switch (xbox)
            {
                case X360Controls.TouchpadClick:
                case X360Controls.LeftMouse:
                case X360Controls.RightMouse:
                case X360Controls.MiddleMouse:
                case X360Controls.FourthMouse:
                case X360Controls.FifthMouse:
                    target = xbox;
                    return true;
                case X360Controls.WUP:
                case X360Controls.WDOWN:
                    // 旧経路の isAnalog と同じ: 元が軸・トリガー・ジャイロならアナログのホイール
                    DS4Controls source = dcs.control;
                    bool analog = (source >= DS4Controls.LXNeg && source <= DS4Controls.RYPos)
                        || source == DS4Controls.L2 || source == DS4Controls.R2
                        || (source >= DS4Controls.GyroXPos && source <= DS4Controls.GyroZNeg);
                    if (analog) return false;
                    target = xbox;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Button のうち、マウスの移動（MouseUp／Down／Left／Right）と、元がスティック・トリガー・ジャイロのホイール（アナログ）への割り当ての解釈。
        /// Toggle の有無は見ない。元がボタンのホイール（デジタル）は <see cref="TryInterpretMouse"/> が扱う。
        /// </summary>
        public static bool TryInterpretAnalogMouse(DS4ControlSettings dcs, out X360Controls target)
        {
            target = X360Controls.None;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Button) return false;

            X360Controls xbox = dcs.action.actionBtn;
            switch (xbox)
            {
                case X360Controls.MouseUp:
                case X360Controls.MouseDown:
                case X360Controls.MouseLeft:
                case X360Controls.MouseRight:
                    target = xbox;
                    return true;
                case X360Controls.WUP:
                case X360Controls.WDOWN:
                    // TryInterpretMouse が偽（元がアナログ）のものだけ
                    if (TryInterpretMouse(dcs, out _)) return false;
                    target = xbox;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// SA 専用の種別の割り当てが、新経路の条件（<see cref="ControlsSpecialTypeTable"/> の種別で、型のある項目を持つ）を満たすか。
        /// </summary>
        public static bool TryInterpretSpecial(DS4ControlSettings dcs, [NotNullWhen(true)] out ControlSpecialItem? item)
        {
            item = null;
            if (dcs == null) return false;
            if (dcs.actionType != DS4ControlSettings.ActionType.Special) return false;
            if (dcs.special == null || !ControlsSpecialTypeTable.IsSupported(dcs.special.Type)) return false;

            // 切り替え先のない Load Profile は、束縛を作らない（作ると、切り替えに失敗し続けながら、割り当てたボタンの元の出力だけが消える）
            if (dcs.special.Type == SpecialAction.ActionTypeId.Profile && string.IsNullOrWhiteSpace(dcs.special.ProfileName)) return false;

            // パスもタイトルも空のフォーカスは、どのウィンドウにも一致してしまうため、束縛を作らない
            if (dcs.special.Type == SpecialAction.ActionTypeId.FocusWindow
                && string.IsNullOrWhiteSpace(dcs.special.WindowPath) && string.IsNullOrWhiteSpace(dcs.special.WindowTitle)) return false;

            item = dcs.special;
            return true;
        }
    }
}
