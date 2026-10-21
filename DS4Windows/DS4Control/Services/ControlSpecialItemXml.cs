using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase10-Step4 W1（Phase10-Step4-Plan.md §3.2）: <see cref="ControlSpecialItem"/> の、プロファイル XML（<c>&lt;Control&gt;&lt;Special&gt;</c>）への書き出しと読み込み。
    /// ボタンごとの要素（要素名＝ボタン名）に、<c>Type</c> 属性と、項目ごとの子要素を持つ。<b>既定の値の項目は、書かない</b>。
    /// 読み込みは、寛容: 種別が未知・Controls で使えない種別の要素は、読み飛ばす（<see cref="TryRead"/> が偽）。項目の値が壊れているときは、その項目だけ既定の値にする
    /// （ほかの項目、ほかのボタンの割り当ては、壊さない）。数値は、文化に依らない（インバリアント）。
    /// </summary>
    internal static class ControlSpecialItemXml
    {
        /// <summary>Controls に割り当てられる種別（<see cref="ControlSpecialItem.Type"/>）。</summary>
        private static readonly SpecialAction.ActionTypeId[] AllowedTypes =
        {
            SpecialAction.ActionTypeId.Macro,
            SpecialAction.ActionTypeId.Program,
            SpecialAction.ActionTypeId.Profile,
            SpecialAction.ActionTypeId.DisconnectBT,
            SpecialAction.ActionTypeId.BatteryCheck,
            SpecialAction.ActionTypeId.MultiAction,
            SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate,
            SpecialAction.ActionTypeId.GyroCalibrate,
        };

        public static XmlElement Write(XmlDocument doc, string controlName, ControlSpecialItem item)
        {
            XmlElement element = doc.CreateElement(controlName);
            element.SetAttribute("Type", item.Type.ToString());

            void Child(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                element.AppendChild(child);
            }

            if (item.HoldSeconds > 0) Child("HoldSeconds", item.HoldSeconds.ToString("R", CultureInfo.InvariantCulture));
            if (item.RunOnRelease) Child("RunOnRelease", "true");
            if (item.Synchronized) Child("Synchronized", "true");
            if (item.KeepKeyState) Child("KeepKeyState", "true");
            if (!string.IsNullOrEmpty(item.ProgramPath)) Child("ProgramPath", item.ProgramPath);
            if (!string.IsNullOrEmpty(item.ProgramArguments)) Child("ProgramArguments", item.ProgramArguments);
            if (item.HideWindow) Child("HideWindow", "true");
            if (!string.IsNullOrEmpty(item.ProfileName)) Child("ProfileName", item.ProfileName);
            if (item.AutomaticUntrigger) Child("AutomaticUntrigger", "true");
            if (!item.EmptyColor.Equals(default(DS4Color))) Child("EmptyColor", ColorText(item.EmptyColor));
            if (!item.FullColor.Equals(default(DS4Color))) Child("FullColor", ColorText(item.FullColor));
            if (item.TapMacro.Count > 0) Child("TapMacro", string.Join("/", item.TapMacro));
            if (item.HoldMacro.Count > 0) Child("HoldMacro", string.Join("/", item.HoldMacro));
            if (item.DoubleTapMacro.Count > 0) Child("DoubleTapMacro", string.Join("/", item.DoubleTapMacro));

            return element;
        }

        public static bool TryRead(XmlNode node, out ControlSpecialItem item)
        {
            item = null;
            if (node == null) return false;

            string typeText = node.Attributes?["Type"]?.Value;
            if (!Enum.TryParse(typeText, out SpecialAction.ActionTypeId type) || !AllowedTypes.Contains(type)) return false;

            var result = new ControlSpecialItem { Type = type };
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!(child is XmlElement)) continue;

                string text = child.InnerText ?? string.Empty;
                switch (child.Name)
                {
                    case "HoldSeconds":
                        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
                            && !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds >= 0)
                        {
                            result.HoldSeconds = seconds;
                        }

                        break;
                    case "RunOnRelease": result.RunOnRelease = IsTrue(text); break;
                    case "Synchronized": result.Synchronized = IsTrue(text); break;
                    case "KeepKeyState": result.KeepKeyState = IsTrue(text); break;
                    case "ProgramPath": result.ProgramPath = text; break;
                    case "ProgramArguments": result.ProgramArguments = text; break;
                    case "HideWindow": result.HideWindow = IsTrue(text); break;
                    case "ProfileName": result.ProfileName = text; break;
                    case "AutomaticUntrigger": result.AutomaticUntrigger = IsTrue(text); break;
                    case "EmptyColor": if (TryColor(text, out DS4Color empty)) result.EmptyColor = empty; break;
                    case "FullColor": if (TryColor(text, out DS4Color full)) result.FullColor = full; break;
                    case "TapMacro": result.TapMacro = ParseSteps(text); break;
                    case "HoldMacro": result.HoldMacro = ParseSteps(text); break;
                    case "DoubleTapMacro": result.DoubleTapMacro = ParseSteps(text); break;
                }
            }

            item = result;
            return true;
        }

        private static bool IsTrue(string text) => string.Equals(text?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        private static string ColorText(DS4Color c) => c.red + "," + c.green + "," + c.blue;

        private static bool TryColor(string text, out DS4Color color)
        {
            color = default(DS4Color);
            string[] parts = (text ?? string.Empty).Split(',');
            if (parts.Length != 3
                || !byte.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out byte r)
                || !byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out byte g)
                || !byte.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out byte b))
            {
                return false;
            }

            color = new DS4Color(r, g, b);
            return true;
        }

        /// <summary>マクロの手順（「/」区切りの整数）。1 つでも整数でなければ、空（壊れたマクロは、使わない）。</summary>
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
