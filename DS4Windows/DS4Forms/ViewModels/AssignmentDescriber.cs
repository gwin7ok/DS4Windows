using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase9 UI 変更（Phase9-ControlsList-Display-Proposal.md。ユーザー承認 2026-10-22）: 割り当ての説明文を作る、唯一の場所。
    /// Controls タブの一覧、出力ピッカーのボタンのラベル（<c>PressKeyViewModel.DescribeText</c>）、SA タブの一覧のアクション列が、同じ書式を使う。
    /// 書式: 基本は「出力名 (Press／Toggle)」。括弧の中に、モード・オプションを順に足す。固定語（Press、Toggle、steps ほか）は、英語固定（ユーザー決定）。
    /// 状態を持たず、画面の部品（ListView、サービス）に依存しない（ボタン名の取得だけ、関数で受ける）。
    /// </summary>
    public static class AssignmentDescriber
    {
        public const string DefaultText = "Default";
        public const string UnboundText = "Unbound";

        /// <summary>Default のままで、そのボタンに対応する出力（Xbox のボタンなど）がないときの表示。Unbound（出力を明示的に無効にした設定）と区別する。</summary>
        public const string DefaultNoMatchText = "Default (No matching button)";
        public const string ExtrasSuffix = " + Extras";

        public static string Mode(bool toggle) => toggle ? "Toggle" : "Press";

        /// <summary>キー: 「W (Press)」「Q (SC) (Toggle)」。</summary>
        public static string ForKey(int virtualKey, bool scanCode, bool toggle)
            => $"{KeyInterop.KeyFromVirtualKey(virtualKey)}{(scanCode ? " (SC)" : string.Empty)} ({Mode(toggle)})";

        /// <summary>ボタン（パッド、マウス、ホイール、マウス移動）: 「A (Press)」「Left Mouse Button (Toggle)」。</summary>
        public static string ForButton(string buttonName, bool toggle) => $"{buttonName} ({Mode(toggle)})";

        /// <summary>Default: 「Default (A)」。既定の出力名がなければ「Default」。Toggle のとき（保存すると、既定の出力への Button ＋ Toggle になる）は「 (Toggle)」を足す。</summary>
        public static string ForDefault(string defaultOutputName, bool toggle = false)
            => (string.IsNullOrEmpty(defaultOutputName) ? DefaultText : $"{DefaultText} ({defaultOutputName})") + (toggle ? " (Toggle)" : string.Empty);

        /// <summary>Macro: 「Macro: 12 steps (Play once)」。再生の指定、Scan Code、SA 専用の 3 項目を、この順に足す。</summary>
        public static string ForMacro(int stepCount, DS4KeyType keyType, bool runOnRelease, bool synchronized, bool keepKeyState)
        {
            // R-2: Repeat (Toggle) を追加（RepeatLatchMacro を優先）
            string mode = keyType.HasFlag(DS4KeyType.RepeatLatchMacro) ? "Repeat (toggle)" : keyType.HasFlag(DS4KeyType.RepeatMacro) ? "Repeat while held" : "Play once";
            string text = $"Macro: {stepCount} steps ({mode})";
            if (keyType.HasFlag(DS4KeyType.ScanCode)) text += " (SC)";
            if (runOnRelease) text += " (Run on release)";
            if (synchronized) text += " (Synchronized)";
            if (keepKeyState) text += " (Keep key state)";
            return text;
        }

        public static string WithExtras(string text, bool hasExtras) => hasExtras ? text + ExtrasSuffix : text;

        /// <summary>押し続ける秒数（0 のときは出さない）: 「 (hold 1.0s)」。</summary>
        public static string Hold(double seconds) => seconds > 0 ? $" (hold {seconds:0.0}s)" : string.Empty;

        // ---- Controls ----

        /// <summary>Controls タブの 1 ボタンの割り当て。</summary>
        /// <param name="buttonName">X360 の出力名を、表示用の文字列にする関数（デバイスの種類による名前）。</param>
        /// <param name="defaultOutput">そのボタンの既定の出力。</param>
        /// <param name="isExtraControl">ジャイロ・スワイプなど、既定の出力を持たない入力か。</param>
        /// <param name="unassignedText">割り当てがない（既定の出力もない）ときの表示（アプリの言語の「未割り当て」）。</param>
        public static string ForControl(DS4ControlSettings setting, Func<X360Controls, string> buttonName, X360Controls defaultOutput, bool isExtraControl, string unassignedText)
        {
            if (setting == null) return unassignedText;

            string text;
            bool toggle = setting.keyType.HasFlag(DS4KeyType.Toggle);
            switch (setting.actionType)
            {
                case DS4ControlSettings.ActionType.Key:
                    text = ForKey(setting.action.actionKey, setting.keyType.HasFlag(DS4KeyType.ScanCode), toggle);
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    ControlSpecialItem options = setting.special != null && setting.special.Type == SpecialAction.ActionTypeId.Macro ? setting.special : null;
                    text = ForMacro(setting.action.actionMacro?.Length ?? 0, setting.keyType,
                        options != null && options.RunOnRelease, options != null && options.Synchronized, options != null && options.KeepKeyState);
                    break;
                case DS4ControlSettings.ActionType.Special:
                    text = setting.special != null ? ForSpecialItem(setting.special) : unassignedText;
                    break;
                case DS4ControlSettings.ActionType.Button:
                    text = setting.action.actionBtn == X360Controls.Unbound ? UnboundText : ForButton(buttonName(setting.action.actionBtn), toggle);
                    break;
                default:
                    // Phase9 UI 変更: 対応するボタンがない Default は「Default (No matching button)」（以前は「未割り当て」）
                    text = isExtraControl || defaultOutput == X360Controls.None ? DefaultNoMatchText : ForDefault(buttonName(defaultOutput));
                    break;
            }

            return WithExtras(text, !setting.IsExtrasEmpty(setting.extras));
        }

        /// <summary>Controls の SA 専用の種別（型のある項目）。</summary>
        public static string ForSpecialItem(ControlSpecialItem item)
        {
            switch (item.Type)
            {
                case SpecialAction.ActionTypeId.Program:
                    return "Launch Program: " + FileNameOf(item.ProgramPath) + Hold(item.HoldSeconds);
                case SpecialAction.ActionTypeId.Profile:
                    return "Load Profile: " + item.ProfileName + (item.AutomaticUntrigger ? " (Auto untrigger)" : string.Empty) + Hold(item.HoldSeconds);
                case SpecialAction.ActionTypeId.DisconnectBT:
                    return "Disconnect Controller" + Hold(item.HoldSeconds);
                case SpecialAction.ActionTypeId.BatteryCheck:
                    return "Check Battery" + Hold(item.HoldSeconds);
                case SpecialAction.ActionTypeId.GyroCalibrate:
                    return "Calibrate Gyro" + Hold(item.HoldSeconds);
                case SpecialAction.ActionTypeId.MultiAction:
                    var parts = new List<string>(3);
                    if (item.TapMacro != null && item.TapMacro.Count > 0) parts.Add($"Tap={item.TapMacro.Count} steps");
                    if (item.HoldMacro != null && item.HoldMacro.Count > 0) parts.Add($"Hold={item.HoldMacro.Count} steps");
                    if (item.DoubleTapMacro != null && item.DoubleTapMacro.Count > 0) parts.Add($"DoubleTap={item.DoubleTapMacro.Count} steps");
                    return "Multi-action" + (parts.Count > 0 ? ": " + string.Join(", ", parts) : string.Empty);
                default:
                    return item.Type.ToString();
            }
        }

        // ---- SA タブ ----

        /// <summary>SA タブの一覧の「アクション」列。種別名に、詳細を続ける。</summary>
        public static string ForSpecialAction(SpecialAction sa, Func<X360Controls, string> buttonName)
        {
            if (sa == null) return string.Empty;

            string text;
            double hold = sa.delayTime;
            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Key:
                    {
                        int.TryParse(sa.details, out int vk);
                        bool toggle = sa.keyType.HasFlag(DS4KeyType.Toggle) || (sa.KeyButtonSwitchMode.HasValue && sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle);
                        text = "Key: " + ForKey(vk, sa.keyType.HasFlag(DS4KeyType.ScanCode), toggle);
                        break;
                    }
                case SpecialAction.ActionTypeId.Button:
                    {
                        bool toggle = sa.KeyButtonSwitchMode.HasValue && sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                        string name = int.TryParse(sa.details, out int btn) ? buttonName((X360Controls)btn) : sa.details;
                        text = "Button: " + ForButton(name, toggle) + (sa.analogStrength != 100 ? $" (Strength {sa.analogStrength}%)" : string.Empty);
                        break;
                    }
                case SpecialAction.ActionTypeId.Macro:
                    text = ForMacro(sa.macro?.Count ?? 0, sa.keyType, sa.pressRelease, sa.synchronized, sa.keepKeyState);
                    break;
                case SpecialAction.ActionTypeId.Program:
                    text = "Launch Program: " + FileNameOf(sa.details);
                    break;
                case SpecialAction.ActionTypeId.Profile:
                    text = "Load Profile: " + sa.details + (sa.automaticUntrigger ? " (Auto untrigger)" : string.Empty);
                    break;
                case SpecialAction.ActionTypeId.DisconnectBT:
                    text = "Disconnect Controller";
                    break;
                case SpecialAction.ActionTypeId.BatteryCheck:
                    text = "Check Battery";
                    break;
                case SpecialAction.ActionTypeId.GyroCalibrate:
                    text = "Calibrate Gyro";
                    break;
                case SpecialAction.ActionTypeId.MultiAction:
                    text = "Multi-action";
                    break;
                default:
                    text = sa.type ?? string.Empty;
                    break;
            }

            return WithExtras(text + Hold(hold), !string.IsNullOrEmpty(sa.outputExtras));
        }

        private static string FileNameOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            try { return Path.GetFileName(path); }
            catch { return path; }
        }
    }
}
