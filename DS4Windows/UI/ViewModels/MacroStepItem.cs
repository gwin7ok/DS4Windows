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

#nullable enable

using DS4Windows.Core.Model;
using DS4Windows.Processing.Macro;
using System;

namespace DS4Windows.UI.ViewModels
{

    public class MacroStepItem
    {
        // 手順の種類ごとの画像のパス（RecordBoxViewModel が、画面の資源から引いて設定する。未設定のときは空）
        private static string[] imageSources = new string[] { string.Empty, string.Empty, string.Empty };

        public static void CacheImgLocations(DS4Windows.UI.Abstractions.IImageResourceLookup? imageResources)
        {
            if (imageResources == null) return;

            imageSources = new string[]
            {
                $"{ResourcePaths.ResourcesPrefix}/{imageResources.Find("KeyDownImg")}",
                $"{ResourcePaths.ResourcesPrefix}/{imageResources.Find("KeyUpImg")}",
                $"{ResourcePaths.ResourcesPrefix}/{imageResources.Find("ClockImg")}",
            };
        }

        private MacroStep step;
        private string image;

        public string Image { get => image; }
        public MacroStep Step { get => step; }
        public int DisplayValue
        {
            get
            {
                int result = step.Value;
                if (step.ActType == MacroStep.StepType.Wait)
                {
                    result -= 300;
                }

                return result;
            }
            set
            {
                int result = value;
                if (step.ActType == MacroStep.StepType.Wait)
                {
                    result += 300;
                }

                step.Value = result;
            }
        }

        public int RumbleHeavy
        {
            get
            {
                int result = step.Value;
                result -= 1000000;
                string temp = result.ToString();
                result = int.Parse(temp.Substring(0, 3));
                return result;
            }
            set
            {
                int result = step.Value;
                result -= 1000000;
                int curHeavy = result / 1000;
                int curLight = result - (curHeavy * 1000);
                result = curLight + (value * 1000) + 1000000;
                step.Value = result;
            }
        }

        public int RumbleLight
        {
            get
            {
                int result = step.Value;
                result -= 1000000;
                string temp = result.ToString();
                result = int.Parse(temp.Substring(3, 3));
                return result;
            }
            set
            {
                int result = step.Value;
                result -= 1000000;
                int curHeavy = result / 1000;
                result = value + (curHeavy * 1000) + 1000000;
                step.Value = result;
            }
        }

        // マウス移動ステップの編集（モード、X、Y）。範囲外の座標は MacroPointerMove が端に丸める
        public int PointerModeIndex
        {
            get => MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move) ? (int)move.Mode : 0;
            set
            {
                MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move);
                step.Value = new MacroPointerMove((MacroPointerMoveMode)Math.Min(Math.Max(value, 0), 1), move.X, move.Y).ToMacroValue();
            }
        }

        public int PointerX
        {
            get => MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move) ? move.X : 0;
            set
            {
                MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move);
                step.Value = new MacroPointerMove(move.Mode, value, move.Y).ToMacroValue();
            }
        }

        public int PointerY
        {
            get => MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move) ? move.Y : 0;
            set
            {
                MacroPointerMove.TryFromMacroValue(step.Value, out MacroPointerMove move);
                step.Value = new MacroPointerMove(move.Mode, move.X, value).ToMacroValue();
            }
        }

        public MacroStepItem(MacroStep step)
        {
            this.step = step;
            image = imageSources[(int)step.ActType];
        }

        public void UpdateLightbarValue(DS4Color color)
        {
            step.Value = 1000000000 + (color.red * 1000000) + (color.green * 1000) + color.blue;
        }

        public DS4Color LightbarColorValue()
        {
            int temp = step.Value - 1000000000;
            int r = temp / 1000000;
            temp -= (r * 1000000);
            int g = temp / 1000;
            temp -= (g * 1000);
            int b = temp;
            return new DS4Color((byte)r, (byte)g, (byte)b);
        }
    }
}