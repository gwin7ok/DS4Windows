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
            IsCustomizedChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string SpecialTypeDisplayName(SpecialAction.ActionTypeId type)
        {
            switch (type)
            {
                case SpecialAction.ActionTypeId.Program: return Translations.Strings.SpecialActionEdit_LaunchProgram;
                case SpecialAction.ActionTypeId.Profile: return Translations.Strings.SpecialActionEdit_LoadProfile;
                case SpecialAction.ActionTypeId.DisconnectBT: return Translations.Strings.SpecialActionEdit_DisconnectBT;
                case SpecialAction.ActionTypeId.GyroCalibrate: return Translations.Strings.SpecialActionEdit_CalibrateGyro;
                default: return type.ToString();
            }
        }

        /// <summary>
        /// Phase9 UI 変更: 割り当ての説明文（<see cref="AssignmentDescriber"/>。出力ピッカーのボタンのラベル、SA タブの一覧と同じ書式）。
        /// </summary>
        public string GetMappingString()
        {
            bool extra = control >= DS4Controls.GyroXPos && control <= DS4Controls.SwipeDown;
            return AssignmentDescriber.ForControl(setting, x => X360ControlTables.getX360ControlString(x, devType),
                profileSettingsService.GetDefaultButtonMapping()[(int)control], extra, Properties.Resources.Unassigned);
        }

        /// <summary>Default 以外の割り当て、または Extras があるか（一覧で太字にする）。</summary>
        public bool IsCustomized => setting.actionType != DS4ControlSettings.ActionType.Default || !setting.IsExtrasEmpty(setting.extras);

        public event EventHandler IsCustomizedChanged;

    }
}
