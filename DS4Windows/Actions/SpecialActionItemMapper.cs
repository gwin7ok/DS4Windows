using System;
using System.Collections.Generic;
using System.Globalization;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I2（Phase10-Step6-Plan.md §4）: <see cref="SpecialAction"/>（<c>details</c>／<c>extra</c> の文字列に種別ごとの意味を詰めた形）と、
    /// 型のある項目（<see cref="ControlSpecialItem"/>）の、<b>相互の変換</b>。種別ごとの文字列の解釈規則を、この 1 か所に集める。
    /// <para>
    /// 保存形式（<c>Actions.xml</c> と <see cref="SpecialAction"/>）は変えない（2026-10-22 ユーザー決定。旧版との互換を保つため）。文字列は、保存の境界にだけ残る。
    /// 対象の種別は、項目で表せるもの（Macro の 3 項目、Program、Profile、DisconnectBT、BatteryCheck、MultiAction、較正 2 種）。
    /// Key と Button は、解釈済みの値（<see cref="KeyBindingValues"/>／<see cref="ButtonBindingValues"/>）が別にあるため、項目では表さない。
    /// </para>
    /// </summary>
    // 接続済み（I3）: SpecialActionAssignmentReader が ToItem を、ControlsAssignmentReader が ToSpecialAction を使う（ControlBindingBuilder.CreateSpecialIdentity の置き換え）。
    // 現状の呼び出し側（SpecialActionOutputFactory／ControlBindingBuilder）は、まだ SpecialAction の文字列を直接読む・合成の SpecialAction を自前で作る。呼出元が 0 件でも、削除しない（CLAUDE.md 2.4）
    public static class SpecialActionItemMapper
    {
        private const string HiddenKeyword = "$hidden";

        /// <summary>
        /// SA を、型のある項目にする。対象外の種別（Key、Button、None）は null。
        /// 値が壊れているときは、その項目だけ既定の値にする（例外にしない）。SA の <c>uTrigger</c>（解除トリガー）は、機能廃止済みのため扱わない。
        /// </summary>
        public static ControlSpecialItem ToItem(SpecialAction sa)
        {
            if (sa == null) return null;

            var item = new ControlSpecialItem { Type = sa.typeID, HoldSeconds = sa.delayTime < 0 ? 0 : sa.delayTime };
            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Macro:
                    item.RunOnRelease = sa.pressRelease;
                    item.Synchronized = sa.synchronized;
                    item.KeepKeyState = sa.keepKeyState;
                    break;
                case SpecialAction.ActionTypeId.Program:
                    {
                        // 起動処理（LaunchProcessAction）と同じ規則: extra に "$hidden"（大文字小文字不問）を含むとき、その語を 1 回取り除き、ウィンドウを隠す
                        item.ProgramPath = sa.details ?? string.Empty;
                        string extra = sa.extra ?? string.Empty;
                        int pos = extra.Length > 0 ? extra.IndexOf(HiddenKeyword, StringComparison.OrdinalIgnoreCase) : -1;
                        if (pos >= 0)
                        {
                            item.HideWindow = true;
                            extra = extra.Remove(pos, HiddenKeyword.Length);
                        }

                        item.ProgramArguments = extra;
                    }

                    break;
                case SpecialAction.ActionTypeId.Profile:
                    item.ProfileName = sa.details ?? string.Empty;
                    item.ProfileId = sa.profileId ?? string.Empty;
                    item.AutomaticUntrigger = sa.automaticUntrigger;
                    break;
                case SpecialAction.ActionTypeId.BatteryCheck:
                    ReadBattery(sa.details, item);
                    break;
                case SpecialAction.ActionTypeId.MultiAction:
                    {
                        // details は「タップ,長押し,ダブルタップ」（各マクロは「/」区切りの整数）。足りない分は空
                        string[] macros = (sa.details ?? string.Empty).Split(',');
                        item.TapMacro = ParseSteps(macros.Length > 0 ? macros[0] : null);
                        item.HoldMacro = ParseSteps(macros.Length > 1 ? macros[1] : null);
                        item.DoubleTapMacro = ParseSteps(macros.Length > 2 ? macros[2] : null);
                    }

                    break;
                case SpecialAction.ActionTypeId.DisconnectBT:
                case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                case SpecialAction.ActionTypeId.GyroCalibrate:
                    break;
                default:
                    return null;
            }

            return item;
        }

        /// <summary>
        /// 項目から、合成の <see cref="SpecialAction"/>（SA の出力アクションが受け取る形）を作る。<see cref="ToItem"/> の逆。
        /// Controls の SA 専用の割り当て（<c>ControlBindingBuilder</c> が、Phase10-Step6 I3 まで自前で作っていた形［CreateSpecialIdentity。削除済み］と同じ。Macro の手順（<c>macro</c>）と再生の指定（<c>keyType</c>）は、項目にないため、呼び出し側が足す。
        /// </summary>
        public static SpecialAction ToSpecialAction(string name, IReadOnlyList<DS4Controls> triggers, ControlSpecialItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var sa = new SpecialAction(name, "Synthetic", "Special", string.Empty, 0, string.Empty);
            sa.typeID = item.Type;
            sa.type = item.Type.ToString();
            sa.controls = name;
            sa.delayTime = item.HoldSeconds;
            sa.trigger = triggers == null ? new List<DS4Controls>() : new List<DS4Controls>(triggers);

            switch (item.Type)
            {
                case SpecialAction.ActionTypeId.Macro:
                    sa.pressRelease = item.RunOnRelease;
                    sa.synchronized = item.Synchronized;
                    sa.keepKeyState = item.KeepKeyState;
                    break;
                case SpecialAction.ActionTypeId.Program:
                    // パスを details、引数を extra に入れる。ウィンドウを隠すときは、extra の先頭に "$hidden" を付ける
                    sa.details = item.ProgramPath ?? string.Empty;
                    sa.extra = (item.HideWindow ? HiddenKeyword : string.Empty) + (item.ProgramArguments ?? string.Empty);
                    break;
                case SpecialAction.ActionTypeId.Profile:
                    sa.details = item.ProfileName;
                    sa.profileId = string.IsNullOrEmpty(item.ProfileId) ? null : item.ProfileId;
                    sa.automaticUntrigger = item.AutomaticUntrigger;
                    break;
                case SpecialAction.ActionTypeId.BatteryCheck:
                    sa.details = WriteBattery(item);
                    break;
                case SpecialAction.ActionTypeId.MultiAction:
                    sa.details = string.Join("/", item.TapMacro) + "," + string.Join("/", item.HoldMacro) + "," + string.Join("/", item.DoubleTapMacro);
                    break;
            }

            return sa;
        }

        // details は「秒数,通知,ライトバー,空の R,G,B,満の R,G,B」（SA のコンストラクターが「|」を「,」に直す）。足りない・壊れているときは、既定の値
        private static void ReadBattery(string details, ControlSpecialItem item)
        {
            string[] parts = (details ?? string.Empty).Split(',');
            if (parts.Length < 9) return;

            if (bool.TryParse(parts[1], out bool notification)) item.BatteryNotification = notification;
            if (bool.TryParse(parts[2], out bool lightbar)) item.BatteryLightbar = lightbar;
            if (TryColor(parts[3], parts[4], parts[5], out DS4Color empty)) item.EmptyColor = empty;
            if (TryColor(parts[6], parts[7], parts[8], out DS4Color full)) item.FullColor = full;
        }

        // CheckBatteryViewModel.SaveAction が保存する形（秒数は "#.##"）と同じにする
        private static string WriteBattery(ControlSpecialItem item)
        {
            return $"{item.HoldSeconds.ToString("#.##", ConfigFileCulture.DecimalCulture)},{item.BatteryNotification},{item.BatteryLightbar},"
                + $"{item.EmptyColor.red},{item.EmptyColor.green},{item.EmptyColor.blue},{item.FullColor.red},{item.FullColor.green},{item.FullColor.blue}";
        }

        private static bool TryColor(string r, string g, string b, out DS4Color color)
        {
            color = default(DS4Color);
            if (!byte.TryParse(r, NumberStyles.None, CultureInfo.InvariantCulture, out byte red)
                || !byte.TryParse(g, NumberStyles.None, CultureInfo.InvariantCulture, out byte green)
                || !byte.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out byte blue))
            {
                return false;
            }

            color = new DS4Color(red, green, blue);
            return true;
        }

        // 「/」区切りの整数。1 つでも整数でなければ、空（壊れたマクロは、使わない。ControlSpecialItemXml と同じ規則）
        private static List<int> ParseSteps(string text)
        {
            var steps = new List<int>();
            if (string.IsNullOrEmpty(text)) return steps;

            foreach (string part in text.Split('/'))
            {
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int step)) return new List<int>();
                steps.Add(step);
            }

            return steps;
        }
    }
}
