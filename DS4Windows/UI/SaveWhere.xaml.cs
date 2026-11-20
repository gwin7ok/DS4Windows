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
using System.Windows;


namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for SaveWhere.xaml
    /// </summary>
    public partial class SaveWhere : Window
    {
        /// <summary>選ばれた保存場所の適用。引数は、プログラムのフォルダを選んだか、古い設定を残すか。サービスを持つ呼び出し側（App）が行う。</summary>
        public delegate void ChoiceApplier(bool programFolder, bool keepOldSettings);

        private readonly ChoiceApplier _applyChoice;
        public bool ChoiceMade { get; set; }

        public SaveWhere(bool multisavespots, bool adminNeeded, ChoiceApplier applyChoice)
        {
            ArgumentNullException.ThrowIfNull(applyChoice);
            _applyChoice = applyChoice;

            InitCommands();
            InitializeComponent();
            if (!multisavespots)
            {
                multipleSavesDockP.Visibility = Visibility.Collapsed;
            }

            if (adminNeeded)
            {
                progFolderPanel.IsEnabled = false;
            }
        }

        private void ProgFolderBtn()
        {
            _applyChoice(true, dontDeleteCk.IsChecked != false);
            ChoiceMade = true;
            Close();
        }

        private void AppdataBtn()
        {
            _applyChoice(false, dontDeleteCk.IsChecked != false);
            ChoiceMade = true;
            Close();
        }
    }
}
