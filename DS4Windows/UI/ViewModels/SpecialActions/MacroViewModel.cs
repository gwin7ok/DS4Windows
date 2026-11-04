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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class MacroViewModel : NotifyDataErrorBase, System.ComponentModel.INotifyPropertyChanged, IActionTypeEditor
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        private bool runTriggerRelease;
        private bool syncRun;
        private bool keepKeyState;
        private List<int> macro = new List<int>(1);
        private string macrostring;

        // Phase10-Step1 W3: Execute Mode（繰り返し）と Scan Code は、共通の MacroPlaybackOptions に持つ（旧: UseScanCode、RepeatHeld）
        public MacroPlaybackOptions PlaybackOptions { get; } = new MacroPlaybackOptions();
        public bool RunTriggerRelease { get => runTriggerRelease; set { runTriggerRelease = value; OnPropertyChanged(nameof(RunTriggerRelease)); } }
        public bool SyncRun { get => syncRun; set { syncRun = value; OnPropertyChanged(nameof(SyncRun)); } }
        public bool KeepKeyState { get => keepKeyState; set { keepKeyState = value; OnPropertyChanged(nameof(KeepKeyState)); } }
        public List<int> Macro { get => macro; set { macro = value; OnPropertyChanged(nameof(Macro)); } }
        public string Macrostring { get => macrostring;
            set
            {
                macrostring = value;
                MacrostringChanged?.Invoke(this, EventArgs.Empty);
                OnPropertyChanged(nameof(Macrostring));
            }
        }

        public event EventHandler MacrostringChanged;

        public void LoadAction(SpecialAction action)
        {
            macro = action.macro;
            if (action.macro.Count > 0)
            {
                MacroParser macroParser = new MacroParser(action.macro.ToArray());
                macroParser.LoadMacro();
                macrostring = string.Join(", ", macroParser.GetMacroStrings());
            }

            PlaybackOptions.LoadFrom(action.keyType);
            runTriggerRelease = action.pressRelease;
            syncRun = action.synchronized;
            keepKeyState = action.keepKeyState;
        }

        public DS4ControlSettings PrepareSettings()
        {
            DS4ControlSettings settings = new DS4ControlSettings(DS4Controls.None);
            settings.action.actionMacro = macro.ToArray();
            settings.actionType = DS4ControlSettings.ActionType.Macro;
            // Phase8-Fix-MacroRepeatUnify（D1）: 「押している間繰り返す」は RepeatMacro だけで表す
            settings.keyType = PlaybackOptions.ApplyTo(DS4KeyType.Macro);

            return settings;
        }

        // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
        private readonly ISpecialActionRepository specialActionRepository;

        public MacroViewModel(ISpecialActionRepository specialActionRepository)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            PlaybackOptions.PropertyChanged += OnPlaybackOptionsChanged;
        }

        /// <summary>
        /// R-2: Repeat (Toggle) のときは、「離したときに実行（Run on trigger release）」を使えない（意味が曖昧になるため。計画書 §7 の決定）。
        /// 画面では、チェックボックスを無効にする。
        /// </summary>
        public bool CanRunTriggerRelease => !PlaybackOptions.RepeatToggle;

        private void OnPlaybackOptionsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MacroPlaybackOptions.RepeatToggle)) return;

            // Repeat (Toggle) を選んだら、「離したときに実行」は外す
            if (PlaybackOptions.RepeatToggle && runTriggerRelease) RunTriggerRelease = false;
            OnPropertyChanged(nameof(CanRunTriggerRelease));
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            List<string> extrasList = new List<string>();
            extrasList.Add(PlaybackOptions.UseScanCode ? "Scan Code" : null);
            extrasList.Add(runTriggerRelease && !PlaybackOptions.RepeatToggle ? "RunOnRelease" : null);   // R-2: Repeat (Toggle) とは併用しない
            extrasList.Add(syncRun ? "Sync" : null);
            extrasList.Add(keepKeyState ? "KeepKeyState" : null);
            // R-2: Execute Mode（Once／Held／Toggle）は、extras ではなく、独立した MacroExecuteMode 要素へ保存する（旧: extras の Repeat）
            MacroExecuteMode executeMode = MacroExecuteModes.FromKeyType(PlaybackOptions.ApplyTo(DS4KeyType.Macro));
            specialActionRepository.SaveAction(action.name, action.controls, 1, string.Join("/", macro), edit,
                extras: string.Join("/", extrasList.Where(s => !string.IsNullOrEmpty(s))), actionId: action.id, macroExecuteMode: executeMode);
        }

        public void UpdateMacroString()
        {
            string temp = "";
            if (macro.Count > 0)
            {
                MacroParser macroParser = new MacroParser(macro.ToArray());
                macroParser.LoadMacro();
                temp = string.Join(", ", macroParser.GetMacroStrings());
            }

            Macrostring = temp;
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            // Phase10-Step3（ユーザー承認、2026-10-21）: 手順が空のマクロも、保存できる（従来は「No macro defined」で保存できなかった）。
            // トリガーが成立すると、空のマクロが実行される（何も出力しない）。「押している間繰り返す」の空のマクロは、押している間、
            // 短い間隔を置いて繰り返す（Mapping.PlayMacroTask。間隔を置かないと、待ちなしの空回りになるため）
            return true;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Macro");
            }
        }
    }
}
