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

using DS4Windows;
using DS4Windows.DI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class MappedControl
    {
        // Phase6-Step10-2-5: Global.GetDS4CSetting／Global.defaultButtonMapping の置換先（必須引数）
        private readonly IProfileSettingsService profileSettingsService;
        private int devIndex;
        private OutContType devType;
        private DS4Controls control;
        private DS4ControlSettings setting;
        private string controlName;
        private string mappingName;

        public int DevIndex { get => devIndex; }
        public DS4Controls Control { get => control; }
        public DS4ControlSettings Setting { get => setting; }
        public string ControlName { get => controlName; }
        public string MappingName { get => mappingName; }
        public OutContType DevType
        {
            get => devType;
            set
            {
                devType = value;
                DevTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler DevTypeChanged;

        public event EventHandler MappingNameChanged;

        public MappedControl(int devIndex, DS4Controls control, string controlName,
            OutContType devType, IProfileSettingsService profileSettingsService, bool initMap = false)
        {
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            this.devIndex = devIndex;
            this.devType = devType;
            this.control = control;
            this.controlName = controlName;
            setting = profileSettingsService.GetDS4CSetting(devIndex, control);
            //mappingName = "?";
            if (initMap)
            {
                mappingName = GetMappingString();
            }

            DevTypeChanged += MappedControl_DevTypeChanged;
        }

        private void MappedControl_DevTypeChanged(object sender, EventArgs e)
        {
            UpdateMappingName();
        }

        public void UpdateMappingName()
        {
            mappingName = GetMappingString();

            MappingNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string SpecialTypeDisplayName(SpecialAction.ActionTypeId type)
        {
            switch (type)
            {
                case SpecialAction.ActionTypeId.Program: return Translations.Strings.SpecialActionEdit_LaunchProgram;
                case SpecialAction.ActionTypeId.Profile: return Translations.Strings.SpecialActionEdit_LoadProfile;
                case SpecialAction.ActionTypeId.DisconnectBT: return Translations.Strings.SpecialActionEdit_DisconnectBT;
                case SpecialAction.ActionTypeId.GyroCalibrate: return Translations.Strings.SpecialActionEdit_CalibrateGyro;
                case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate: return Translations.Strings.SpecialActionEdit_SixAxisWheelCalib;
                default: return type.ToString();
            }
        }

        public string GetMappingString()
        {
            string temp = Properties.Resources.Unassigned;
            ControlActionData action = setting.action;
            bool sc = setting.keyType.HasFlag(DS4KeyType.ScanCode);
            bool extra = control >= DS4Controls.GyroXPos && control <= DS4Controls.SwipeDown;
            DS4ControlSettings.ActionType actionType = setting.actionType;
            if (actionType != DS4ControlSettings.ActionType.Default)
            {
                if (actionType == DS4ControlSettings.ActionType.Key)
                {
                    //return (Keys)int.Parse(action.ToString()) + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                    temp = KeyInterop.KeyFromVirtualKey(action.actionKey) + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                }
                else if (actionType == DS4ControlSettings.ActionType.Macro)
                {
                    temp = Properties.Resources.Macro + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                }
                else if (actionType == DS4ControlSettings.ActionType.Special)
                {
                    // Phase10-Step4 W4: SA 専用の種別。エディターの種別の一覧と同じ表示名（言語リソース）。
                    // TODO(Phase10-Step4 W7 以降): 群が増えるたびに、表示名を足す（Battery、Multi-action）。未対応の種別は、種別名をそのまま出す
                    temp = setting.special != null ? SpecialTypeDisplayName(setting.special.Type) : Properties.Resources.Unassigned;
                }
                else if (actionType == DS4ControlSettings.ActionType.Button)
                {
                    string tag;
                    tag = X360ControlTables.getX360ControlString((X360Controls)action.actionBtn, devType);
                    temp = tag;
                }
                else
                {
                    temp = X360ControlTables.getX360ControlString(profileSettingsService.GetDefaultButtonMapping()[(int)control], devType);
                }
            }
            else if (!extra)
            {
                X360Controls tempOutControl = profileSettingsService.GetDefaultButtonMapping()[(int)control];
                if (tempOutControl != X360Controls.None)
                {
                    temp = X360ControlTables.getX360ControlString(tempOutControl, devType);
                }
            }

            return temp;
        }

    }
}
