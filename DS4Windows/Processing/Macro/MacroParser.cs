/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Windows.Input;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Macro
{
    public class MacroParser : IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        private bool loaded;
        private List<MacroStep> macroSteps;
        private int[] inputMacro;
        private Dictionary<int, bool> keydown = new Dictionary<int, bool>();
        public static Dictionary<int, string> macroInputNames = new Dictionary<int, string>()
        {
            [256] = "Left Mouse Button",
            [257] = "Right Mouse Button",
            [258] = "Middle Mouse Button",
            [259] = "4th Mouse Button",
            [260] = "5th Mouse Button",
            [261] = "A Button",
            [262] = "B Button",
            [263] = "X Button",
            [264] = "Y Button",
            [265] = "Start",
            [266] = "Back",
            [267] = "Up Button",
            [268] = "Down Button",
            [269] = "Left Button",
            [270] = "Right Button",
            [271] = "Guide",
            [272] = "Left Bumper",
            [273] = "Right Bumper",
            [274] = "Left Trigger",
            [275] = "Right Trigger",
            [276] = "Left Stick",
            [277] = "Right Stick",
            [278] = "LS Right",
            [279] = "LS Left",
            [280] = "LS Down",
            [281] = "LS Up",
            [282] = "RS Right",
            [283] = "RS Left",
            [284] = "RS Down",
            [285] = "RS Up",
            [286] = "Touchpad Click",
            [287] = "Touch Started",
            [288] = "Touch Ended",
        };

        // K-W6（S9 の追加修正）: コントローラーのボタン系のマクロ値と、出力（X360Controls）の対応。
        // Emulated Controller が DS4 のときの表記（Cross、L1 ほか）を、X360ControlTables.ds4DefaultNames から引くために使う。
        // スティックの軸（278〜285）、マウス（256〜260）、Touch Started／Ended（287・288）は、出力タイプによらず従来の名前のまま
        private static readonly Dictionary<int, X360Controls> macroValueOutputs = new Dictionary<int, X360Controls>()
        {
            [261] = X360Controls.A,
            [262] = X360Controls.B,
            [263] = X360Controls.X,
            [264] = X360Controls.Y,
            [265] = X360Controls.Start,
            [266] = X360Controls.Back,
            [267] = X360Controls.DpadUp,
            [268] = X360Controls.DpadDown,
            [269] = X360Controls.DpadLeft,
            [270] = X360Controls.DpadRight,
            [271] = X360Controls.Guide,
            [272] = X360Controls.LB,
            [273] = X360Controls.RB,
            [274] = X360Controls.LT,
            [275] = X360Controls.RT,
            [276] = X360Controls.LS,
            [277] = X360Controls.RS,
            [286] = X360Controls.TouchpadClick,
        };

        /// <summary>
        /// マクロ値（256〜288）のボタン名。Emulated Controller が DS4 のときは DS4 の表記、それ以外（Xbox 360、省略）は従来の Xbox の表記。
        /// </summary>
        public static string InputName(int value, OutContType outType = OutContType.X360)
        {
            if (outType == OutContType.DS4 && macroValueOutputs.TryGetValue(value, out X360Controls output) &&
                X360ControlTables.ds4DefaultNames.TryGetValue(output, out string ds4Name))
            {
                return ds4Name;
            }

            macroInputNames.TryGetValue(value, out string name);
            return name;
        }

        private readonly OutContType outType;

        public List<MacroStep> MacroSteps { get => macroSteps; }

        public MacroParser(int[] macro, OutContType outType = OutContType.X360)
        {
            macroSteps = new List<MacroStep>();
            inputMacro = macro;
            this.outType = outType;
        }

        public void LoadMacro()
        {
            if (loaded)
            {
                return;
            }

            keydown.Clear();
            for (int i = 0; i < inputMacro.Length; i++)
            {
                int value = inputMacro[i];
                MacroStep step = ParseStep(value);
                macroSteps.Add(step);
            }

            loaded = true;
        }

        public List<string> GetMacroStrings()
        {
            if (!loaded)
            {
                LoadMacro();
            }

            List<string> result = new List<string>();
            foreach (MacroStep step in macroSteps)
            {
                result.Add(step.Name);
            }

            return result;
        }

        private MacroStep ParseStep(int value)
        {
            string name = string.Empty;
            MacroStep.StepType type = MacroStep.StepType.ActDown;
            MacroStep.StepOutput outType = MacroStep.StepOutput.Key;

            if (MacroPointerMove.IsPointerMoveValue(value))
            {
                // Phase12-Step3 3b（R-3）: マウスポインターの移動。ライトバー（最大 1,255,255,255）より上の範囲なので、先に判定する
                outType = MacroStep.StepOutput.PointerMove;
                type = MacroStep.StepType.ActDown;
                name = MacroPointerMove.TryFromMacroValue(value, out MacroPointerMove move) ? move.DisplayName : "Move (unsupported)";
            }
            else if (value >= 1000000000)
            {
                outType = MacroStep.StepOutput.Lightbar;
                if (value > 1000000000)
                {
                    type = MacroStep.StepType.ActDown;
                    string lb = value.ToString().Substring(1);
                    byte r = (byte)(int.Parse(lb[0].ToString()) * 100 + int.Parse(lb[1].ToString()) * 10 + int.Parse(lb[2].ToString()));
                    byte g = (byte)(int.Parse(lb[3].ToString()) * 100 + int.Parse(lb[4].ToString()) * 10 + int.Parse(lb[5].ToString()));
                    byte b = (byte)(int.Parse(lb[6].ToString()) * 100 + int.Parse(lb[7].ToString()) * 10 + int.Parse(lb[8].ToString()));
                    name = $"Lightbar Color: {r},{g},{b}";
                }
                else
                {
                    type = MacroStep.StepType.ActUp;
                    name = "Reset Lightbar";
                }
            }
            else if (value >= 1000000)
            {
                outType = MacroStep.StepOutput.Rumble;
                if (value > 1000000)
                {
                    type = MacroStep.StepType.ActDown;
                    string r = value.ToString().Substring(1);
                    byte heavy = (byte)(int.Parse(r[0].ToString()) * 100 + int.Parse(r[1].ToString()) * 10 + int.Parse(r[2].ToString()));
                    byte light = (byte)(int.Parse(r[3].ToString()) * 100 + int.Parse(r[4].ToString()) * 10 + int.Parse(r[5].ToString()));
                    name = $"Rumble {heavy}, {light} ({Math.Round((heavy * .75f + light * .25f) / 2.55f, 1)}%)";
                }
                else
                {
                    type = MacroStep.StepType.ActUp;
                    name = "Stop Rumble";
                }
            }
            else if (value >= 300) // ints over 300 used to delay
            {
                type = MacroStep.StepType.Wait;
                outType = MacroStep.StepOutput.None;
                name = $"Wait {(value - 300).ToString()} ms";
            }
            else
            {
                // anything above 255 is not a key value
                outType = value <= 255 ? MacroStep.StepOutput.Key : MacroStep.StepOutput.Button;
                keydown.TryGetValue(value, out bool isdown);
                if (!isdown)
                {
                    type = MacroStep.StepType.ActDown;
                    keydown.Add(value, true);
                    if (outType == MacroStep.StepOutput.Key)
                    {
                        name = KeyInterop.KeyFromVirtualKey(value).ToString();
                    }
                    else
                    {
                        name = InputName(value, this.outType);
                    }
                }
                else
                {
                    type = MacroStep.StepType.ActUp;
                    keydown.Remove(value);
                    if (outType == MacroStep.StepOutput.Key)
                    {
                        name = KeyInterop.KeyFromVirtualKey(value).ToString();
                    }
                    else
                    {
                        name = InputName(value, this.outType);
                    }
                }
            }

            MacroStep step = new MacroStep(value, name, type, outType);
            return step;
        }

        public void Reset()
        {
            loaded = false;
        }
    }
}
