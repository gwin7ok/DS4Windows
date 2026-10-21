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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using System.ComponentModel;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class PressKeyViewModel : NotifyDataErrorBase, INotifyPropertyChanged, IActionTypeEditor
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private string switchMode = string.Empty;
        public string SwitchMode
        {
            get => switchMode;
            set
            {
                switchMode = value;
                // Update UI strings and toggle-related controls when SwitchMode changes
                UpdateDescribeText();
                UpdateToggleControls();
                OnPropertyChanged(nameof(SwitchMode));
            }
        }
        private int editorDeviceNum = -1;
        private DS4ControlSettings.ActionType lastActionType = DS4ControlSettings.ActionType.Key;
        private int lastActionBtn = -1;
        private string describeText;
        private DS4KeyType keyType;
        private int value;
        private int pressReleaseIndex = 0;
        private bool normalTrigger = true;
        public bool IsToggle => (keyType & DS4KeyType.Toggle) != 0;

        // Phase10-Step1 W4: Scan Code は、選択ダイアログのチェックボックスではなく、この画面（Press Key タブ）で設定する。
        // keyType とは別に持つ（ボタンを選んで keyType が 0 になっても、Scan Code の選択を失わず、キーに戻したとき使えるように）
        private bool useScanCode;
        public bool UseScanCode
        {
            get => useScanCode;
            set
            {
                if (useScanCode == value) return;
                useScanCode = value;
                OnPropertyChanged(nameof(UseScanCode));
                // キーが未選択のうちは、「Select a Key」の表示を、そのまま保つ（this.value は、選んだキーの値）
                if (this.value != 0) UpdateDescribeText();
            }
        }

        /// <summary>Scan Code が効くのは、キーの割り当てだけ（ボタン、Default のときは、チェックボックスを無効にする）。</summary>
        public bool IsScanCodeEnabled => lastActionType != DS4ControlSettings.ActionType.Button
            && lastActionType != DS4ControlSettings.ActionType.Default;

        /// <summary>
        /// Phase10-Step2 W4: 選ばれている割り当ての種別（<c>Key</c> か <c>Button</c>）。Controls のモードが、書き出しの分岐に使う。
        /// Phase10-Step3 W2: Controls のモードでは、<c>Default</c>（既定の割り当て）も取りうる（<see cref="ReadSettings"/> の <c>allowDefault</c>）。
        /// </summary>
        public DS4ControlSettings.ActionType SelectedActionType => lastActionType;

        /// <summary>Phase10-Step2 W4: 選ばれているボタン（<c>X360Controls</c> の整数値）。キーのときは -1。</summary>
        public int SelectedButton => lastActionBtn;
        public event EventHandler IsToggleChanged;

        public Visibility ShowToggleControls
        {
            get
            {
                return ((keyType & DS4KeyType.Toggle) != 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public event EventHandler ShowToggleControlsChanged;

        public string DescribeText
        {
            get
            {
                string result = "Select a Key";
                if (!string.IsNullOrEmpty(describeText))
                {
                    result = describeText;
                };

                return result;
            }
        }
        public event EventHandler DescribeTextChanged;
        public DS4KeyType KeyType { get => keyType; set => keyType = value; }
        public int Value { get => value; set => this.value = value; }
        public int PressReleaseIndex { get => pressReleaseIndex; set => pressReleaseIndex = value; }
        public bool NormalTrigger { get => normalTrigger; set => normalTrigger = value; }
        public bool UnloadError
        {
            get => errors.TryGetValue("UnloadError", out _);
        }

        public void LoadAction(SpecialAction action)
        {
            // device number may be set by the editor so we can pick the proper
            // emulated controller type for label strings (X360 vs DS4)
            keyType = action.keyType;
            useScanCode = keyType.HasFlag(DS4KeyType.ScanCode);
            // Read explicit SwitchMode if provided by SpecialAction; fallback to keyType flag
            if (action.KeyButtonSwitchMode.HasValue)
            {
                SwitchMode = action.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle ? "Toggle" : "Press";
            }
            else
            {
                SwitchMode = keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press";
            }
            // If action is Button, details contains button id
                if (action.typeID == SpecialAction.ActionTypeId.Button)
            {
                lastActionType = DS4ControlSettings.ActionType.Button;
                int.TryParse(action.details, out lastActionBtn);
                    try
                    {
                        // Prefer using editor's device-specific output type when available
                        string btnName;
                        if (editorDeviceNum >= 0 && editorDeviceNum < profileSettingsService.OutContType.Length)
                        {
                            btnName = X360ControlTables.getX360ControlString((X360Controls)lastActionBtn, profileSettingsService.OutContType[editorDeviceNum]);
                        }
                        else
                        {
                            btnName = X360ControlTables.getX360ControlString((X360Controls)lastActionBtn);
                        }

                        // Display mode suffix: (Toggle) or (Press)
                        describeText = btnName + (action.keyType.HasFlag(DS4KeyType.Toggle) ? " (Toggle)" : " (Press)");
                    }
                    catch { describeText = string.Empty; }
                value = 0;
            }
            else
            {
                int.TryParse(action.details, out value);
                lastActionType = DS4ControlSettings.ActionType.Key;
                lastActionBtn = -1;
            }

            if (action.pressRelease)
            {
                pressReleaseIndex = 1;
            }

            UpdateDescribeText();
            UpdateToggleControls();
        }

        /// <summary>Default が選ばれているときの表示（出力ピッカーの Default ボタンの表示と同じ）。</summary>
        public const string DefaultText = "Default";

        public void UpdateDescribeText()
        {
            // If last binding was a Button, prefer showing the button name.
            if (lastActionType == DS4ControlSettings.ActionType.Button && lastActionBtn >= 0)
            {
                try
                {
                    if (editorDeviceNum >= 0 && editorDeviceNum < profileSettingsService.OutContType.Length)
                    {
                        describeText = X360ControlTables.getX360ControlString((X360Controls)lastActionBtn, profileSettingsService.OutContType[editorDeviceNum]);
                    }
                    else
                    {
                        describeText = X360ControlTables.getX360ControlString((X360Controls)lastActionBtn);
                    }
                }
                catch
                {
                    describeText = Properties.Resources.Unassigned;
                }
            }
            else if (lastActionType == DS4ControlSettings.ActionType.Default)
            {
                describeText = DefaultText;
            }
            else
            {
                    describeText = KeyInterop.KeyFromVirtualKey(value).ToString() +
                        (useScanCode ? " (SC)" : "") +
                        (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase) ? " (Toggle)" : " (Press)");
            }

            DescribeTextChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(DescribeText));
            OnPropertyChanged(nameof(IsScanCodeEnabled));
        }

        public void UpdateToggleControls()
        {
            IsToggleChanged?.Invoke(this, EventArgs.Empty);
            ShowToggleControlsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetDeviceNum(int deviceNum)
        {
            editorDeviceNum = deviceNum;
        }

        /// <summary>
        /// 出力ピッカー（<c>BindingWindow</c>）へ渡す、一時的な設定を作る。
        /// Phase10-Step3 W2: Controls のモードでは、対象のボタン（<paramref name="control"/>）と Extras を載せ、いまの割り当て（Button、Default を含む）も渡す
        /// （ピッカーの Default ボタンが、そのボタンの既定値を使い、Extras のパネルに、いまの値が出るように）。
        /// <paramref name="control"/> が <c>None</c>（SA のモード）のときは、従来どおり（キーの割り当てとして渡す）。
        /// </summary>
        public DS4ControlSettings PrepareSettings(DS4Controls control = DS4Controls.None, string extras = null)
        {
            DS4ControlSettings settings = new DS4ControlSettings(control);
            if (control != DS4Controls.None && extras != null)
            {
                settings.extras = extras;
            }

            settings.action.actionKey = value;
            // Ensure the keyType passed to the binding window reflects the editor's SwitchMode
            var keyTypeToPass = keyType;
            if (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase))
            {
                keyTypeToPass |= DS4KeyType.Toggle;
            }
            else
            {
                keyTypeToPass &= ~DS4KeyType.Toggle;
            }
            // Phase10-Step1 W4: Scan Code も、タブの値を渡す（選択ダイアログは、値を通過させるだけ）
            keyTypeToPass = useScanCode ? keyTypeToPass | DS4KeyType.ScanCode : keyTypeToPass & ~DS4KeyType.ScanCode;
            settings.keyType = keyTypeToPass;
            settings.actionType = DS4ControlSettings.ActionType.Key;
            if (control != DS4Controls.None)
            {
                if (lastActionType == DS4ControlSettings.ActionType.Button && lastActionBtn >= 0)
                {
                    settings.actionType = DS4ControlSettings.ActionType.Button;
                    settings.action.actionBtn = (X360Controls)lastActionBtn;
                    settings.keyType &= ~DS4KeyType.Toggle;   // Button の Toggle は、割り当てエディターの Switch Mode が持つ（ピッカーは、書かない）
                    if (lastActionBtn == (int)X360Controls.Unbound) settings.keyType |= DS4KeyType.Unbound;
                }
                else if (lastActionType == DS4ControlSettings.ActionType.Default)
                {
                    settings.actionType = DS4ControlSettings.ActionType.Default;
                    settings.keyType = DS4KeyType.None;
                    settings.action.actionKey = 0;
                }
            }

            return settings;
        }

        /// <param name="allowDefault">
        /// Phase10-Step3 W2: <c>true</c>（Controls のモード）のとき、<c>Default</c> の割り当てを「Default が選ばれた」状態として取り込む。
        /// <c>false</c>（SA のモード。既定）では、従来どおり、キーの割り当てとして扱う（SA に「Default」はない）。
        /// </param>
        public void ReadSettings(DS4ControlSettings settings, int deviceNum = -1, bool allowDefault = false)
        {
            if (allowDefault && settings.actionType == DS4ControlSettings.ActionType.Default)
            {
                lastActionType = DS4ControlSettings.ActionType.Default;
                lastActionBtn = -1;
                value = 0;
                keyType = DS4KeyType.None;
                UpdateDescribeText();
                UpdateToggleControls();
                return;
            }

            // If the binding produced a Button action, show the button name
            if (settings.actionType == DS4ControlSettings.ActionType.Button)
            {
                lastActionType = DS4ControlSettings.ActionType.Button;
                lastActionBtn = (int)settings.action.actionBtn;
                // Try to use device-specific output type when available
                string btnName;
                try
                {
                    if (deviceNum >= 0)
                    {
                        btnName = X360ControlTables.getX360ControlString((X360Controls)settings.action.actionBtn, outputSlotService.OutDevTypeTemp[deviceNum]);
                    }
                    else
                    {
                        btnName = X360ControlTables.getX360ControlString((X360Controls)settings.action.actionBtn);
                    }
                }
                catch
                {
                    btnName = X360ControlTables.getX360ControlString((X360Controls)settings.action.actionBtn);
                }

                describeText = btnName;
                // clear numeric key value since this is a button mapping
                value = 0;
                keyType = 0;
                // Phase6-Step10-3c: 画面が購読している PropertyChanged（DescribeText）も発行して、戻った直後の表示を更新する
                // （従来は DescribeTextChanged だけを発行していたため、保存して開き直すまで古い表示のままだった）
                UpdateDescribeText();
                UpdateToggleControls();
                return;
            }

            // Default: treat as a key action
            value = (int)settings.action.actionKey;
            keyType = settings.keyType;
            useScanCode = keyType.HasFlag(DS4KeyType.ScanCode);
            OnPropertyChanged(nameof(UseScanCode));
            // Update SwitchMode editor value to reflect the binding window's toggle flag
            SwitchMode = keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press";
            lastActionType = DS4ControlSettings.ActionType.Key;
            lastActionBtn = -1;
            // Update the describe text so the UI immediately reflects the key binding
            UpdateDescribeText();
            UpdateToggleControls();
        }

        // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
        private readonly ISpecialActionRepository specialActionRepository;
        private readonly IProfileSettingsService profileSettingsService;
        private readonly IOutputSlotService outputSlotService;

        public PressKeyViewModel(ISpecialActionRepository specialActionRepository,
            IProfileSettingsService profileSettingsService, IOutputSlotService outputSlotService)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            // ボタン名の表示に使う出力コントローラー種別の取得元（profileSettingsService.OutContType／outputSlotService.OutDevTypeTemp の置換先）
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            this.outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            // Determine SwitchMode to save: use explicit editor selection when available,
            // otherwise fallback to existing keyType flag behavior.
            string modeToSave = !string.IsNullOrEmpty(switchMode) ? switchMode : (keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press");

            // If last binding was a Button, save as Button type
            // （Phase6-Step10-3c: SwitchMode（Press／Toggle）も保存する。従来は渡しておらず、Toggle を選んでも Press になっていた）
            if (lastActionType == DS4ControlSettings.ActionType.Button && lastActionBtn >= 0)
            {
                specialActionRepository.SaveAction(action.name, action.controls, 10, lastActionBtn.ToString(), edit,
                    extras: $"{modeToSave}\n{action.ucontrols}");
                return;
            }
            specialActionRepository.SaveAction(action.name, action.controls, 4,
                $"{value}{(useScanCode ? " Scan Code" : "")}", edit,
                extras: $"{modeToSave}\n{action.ucontrols}");
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> valueErrors = new List<string>();
            List<string> toggleErrors = new List<string>();

            if (lastActionType == DS4ControlSettings.ActionType.Key)
            {
                if (value == 0)
                {
                    valid = false;   // Phase10-Step4c W3: 必須の値がないエラーは、保存を止める
                    valueErrors.Add("No key defined");
                    errors["Value"] = valueErrors;
                    RaiseErrorsChanged("Value");
                }
            }
            else if (lastActionType == DS4ControlSettings.ActionType.Button)
            {
                if (lastActionBtn < 0)
                {
                    valid = false;
                    valueErrors.Add("No button defined");
                    errors["Value"] = valueErrors;
                    RaiseErrorsChanged("Value");
                }
            }
            // If explicit SwitchMode says Toggle then require unload triggers
            // Phase10-Step4c W3: このエラーは保存を止めない（解除トリガーの機能は Phase8-Step8 8b で廃止済みで、入力する画面もない。止めると Toggle の Key が保存できなくなる）。表示のみ
            if (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(action.ucontrols))
            {
                toggleErrors.Add("No unload triggers specified");
                errors["UnloadError"] = toggleErrors;
                RaiseErrorsChanged("UnloadError");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Value");
                RaiseErrorsChanged("UnloadError");
            }
        }
    }
}
