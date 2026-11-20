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
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels.SpecialActions
{
    public class CheckBatteryViewModel : NotifyDataErrorBase, IActionTypeEditor
    {
        private double delay;
        private bool notification;
        private bool lightbar = true;
        private DS4Color emptyColor = new DS4Color(255, 0, 0);
        private DS4Color fullColor = new DS4Color(0, 255, 0);

        public double Delay { get => delay; set => delay = value; }
        public bool Notification { get => notification; set => notification = value; }
        public bool Lightbar { get => lightbar; set => lightbar = value; }
        public DS4Color EmptyColor
        {
            get => emptyColor;
            set
            {
                if (emptyColor.Equals(value)) return;
                emptyColor = value;
                EmptyColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler EmptyColorChanged;
        public DS4Color FullColor
        {
            get => fullColor;
            set
            {
                if (fullColor.Equals(value)) return;
                fullColor = value;
                FullColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FullColorChanged;

        public void UpdateForcedColor(DS4Color color, int device)
        {
            if (device < environmentService.ControllerSlotLimit)
            {
                _lightbar.forcedColor[device] = color;
                _lightbar.forcedFlash[device] = 0;
                _lightbar.forcelight[device] = true;
            }
        }

        public void StartForcedColor(DS4Color color, int device)
        {
            if (device < environmentService.ControllerSlotLimit)
            {
                _lightbar.forcedColor[device] = color;
                _lightbar.forcedFlash[device] = 0;
                _lightbar.forcelight[device] = true;
            }
        }

        public void EndForcedColor(int device)
        {
            if (device < environmentService.ControllerSlotLimit)
            {
                _lightbar.forcedColor[device] = new DS4Color(0, 0, 0);
                _lightbar.forcedFlash[device] = 0;
                _lightbar.forcelight[device] = false;
            }
        }

        public void LoadAction(SpecialAction action)
        {
            string[] details = action.details.Split(',');
            delay = action.delayTime;
            bool.TryParse(details[1], out notification);
            bool.TryParse(details[2], out lightbar);
            emptyColor = new DS4Color(byte.Parse(details[3]), byte.Parse(details[4]), byte.Parse(details[5]));
            fullColor = new DS4Color(byte.Parse(details[6]), byte.Parse(details[7]), byte.Parse(details[8]));
        }

        // Phase10-Step6 W7 相当: Controls のモード。型のある項目との往復（SA と同じ入力・同じ検証。色は Color ⇄ DS4Color）
        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            delay = item.HoldSeconds;
            notification = item.BatteryNotification;
            lightbar = item.BatteryLightbar;

            // 2 色とも既定（黒）のときは、色が保存されていない項目。入力の既定の色（空＝赤、満タン＝緑）のままにする
            if (!item.EmptyColor.Equals(default(DS4Color)) || !item.FullColor.Equals(default(DS4Color)))
            {
                EmptyColor = item.EmptyColor;
                FullColor = item.FullColor;
            }
        }

        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type)
        {
            return new ControlSpecialItem
            {
                Type = type,
                HoldSeconds = delay,
                BatteryNotification = notification,
                BatteryLightbar = lightbar,
                EmptyColor = new DS4Color(emptyColor.red, emptyColor.green, emptyColor.blue),
                FullColor = new DS4Color(fullColor.red, fullColor.green, fullColor.blue),
            };
        }

        // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
        private readonly ISpecialActionRepository specialActionRepository;
        // Phase7-Step10e W2: スロット数の上限は、静的な ControlService.CURRENT_DS4_CONTROLLER_LIMIT ではなく、注入された環境サービスから読む
        private readonly IEnvironmentService environmentService;

        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public CheckBatteryViewModel(DS4Windows.Core.Model.LightbarOverrides lightbar, ISpecialActionRepository specialActionRepository, IEnvironmentService environmentService)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(specialActionRepository);
            this.specialActionRepository = specialActionRepository;
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            string details = $"{delay.ToString("#.##", ConfigFileCulture.DecimalCulture)}|{notification}|{lightbar}|{emptyColor.red}|{emptyColor.green}|{emptyColor.blue}|" +
                $"{fullColor.red}|{fullColor.green}|{fullColor.blue}";

            specialActionRepository.SaveAction(action.name, action.controls, 6, details, edit, delay, actionId: action.id);
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> notificationErrors = new List<string>();
            List<string> lightbarErrors = new List<string>();

            // Phase10-Step4c W3: 必須の値がないエラー（どちらの方法も選んでいない、ライトバーの 2 色が同じ）は、保存を止める
            if (!notification && !lightbar)
            {
                valid = false;
                notificationErrors.Add("Need status option");
                lightbarErrors.Add("Need status option");
            }
            else if (lightbar)
            {
                if (emptyColor.Equals(fullColor))
                {
                    valid = false;
                    lightbarErrors.Add("Need to set two different colors");
                }
            }

            if (notificationErrors.Count > 0)
            {
                errors["Notification"] = notificationErrors;
                RaiseErrorsChanged("Notification");
            }
            if (lightbarErrors.Count > 0)
            {
                errors["Lightbar"] = lightbarErrors;
                RaiseErrorsChanged("Lightbar");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Notification");
                RaiseErrorsChanged("Lightbar");
            }
        }
    }
}
