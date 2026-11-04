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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class LoadProfileViewModel : NotifyDataErrorBase, IActionTypeEditor
    {
        private bool autoUntrigger;
        private ProfileList profileList;
        private int profileIndex;
        private bool normalTrigger = true;

        public bool AutoUntrigger { get => autoUntrigger; set => autoUntrigger = value; }
        public int ProfileIndex
        {
            get => profileIndex;
            set
            {
                if (profileIndex == value) return;
                profileIndex = value;
                ProfileIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ProfileIndexChanged;

        public bool UnloadEnabled { get => profileIndex > 0; }
        public event EventHandler UnloadEnabledChanged;

        public ProfileList ProfileList { get => profileList; }
        public bool NormalTrigger { get => normalTrigger; set => normalTrigger = value; }

        public LoadProfileViewModel(ProfileList profileList, ISpecialActionRepository specialActionRepository)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            this.profileList = profileList;

            ProfileIndexChanged += LoadProfileViewModel_ProfileIndexChanged;
        }

        public void LoadAction(SpecialAction action)
        {
            autoUntrigger = action.automaticUntrigger;
            string profilename = action.details;
            ProfileEntity item = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profilename);
            if (item != null)
            {
                profileIndex = profileList.ProfileListCol.IndexOf(item) + 1;
            }
        }

        // Phase10-Step6 I6b: Controls のモード。型のある項目との往復（以前は ControlSettingsEditTarget が書いていた）
        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            // 名前から一覧の位置を探す（0 は未選択）。名前を変えた・消したプロファイルは、見つからず、未選択になる
            ProfileIndex = IndexOfProfile(item.ProfileName);
            AutoUntrigger = item.AutomaticUntrigger;
        }

        // Phase10-Step4 W6: Load Profile。未選択のときは、空の名前（束縛は作られない。割り当て編集画面は、未選択の Save を許さない）
        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type)
        {
            return new ControlSpecialItem
            {
                Type = type,
                ProfileName = SelectedProfileName() ?? string.Empty,
                AutomaticUntrigger = AutoUntrigger,
            };
        }

        private int IndexOfProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;

            var list = ProfileList?.ProfileListCol;
            if (list == null) return 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Name, name, StringComparison.CurrentCultureIgnoreCase)) return i + 1;
            }

            return 0;
        }

        // 選んでいるプロファイルの名前。未選択（添字 0）、一覧の範囲外のときは null
        private string SelectedProfileName()
        {
            var list = ProfileList?.ProfileListCol;
            if (list == null || ProfileIndex <= 0 || ProfileIndex > list.Count) return null;

            return list[ProfileIndex - 1].Name;
        }

        private void LoadProfileViewModel_ProfileIndexChanged(object sender, EventArgs e)
        {
            UnloadEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
        private readonly ISpecialActionRepository specialActionRepository;

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            if (profileIndex > 0)
            {
                string profilename = profileList.ProfileListCol[profileIndex - 1].Name;
                if (action.ucontrols == null)
                {
                    action.ucontrols = string.Empty;
                }

                specialActionRepository.SaveAction(action.name, action.controls, 3, profilename, edit,
                    extras: action.ucontrols +
                    (autoUntrigger ? (action.ucontrols.Length > 0 ? "/" : "") + "AutomaticUntrigger" : ""), actionId: action.id);
            }
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> profileIndexErrors = new List<string>();

            if (profileIndex == 0)
            {
                valid = false;   // Phase10-Step4c W3: 必須の値がないエラーは、保存を止める
                profileIndexErrors.Add("No profile given");
                errors["ProfileIndex"] = profileIndexErrors;
                RaiseErrorsChanged("ProfileIndex");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("ProfileIndex");
            }
        }
    }
}
