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
using System.Windows.Controls;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for DupBox.xaml
    /// </summary>
    public partial class DupBox : UserControl
    {
        private string? oldfilename;
        public string OldFilename
        {
            get => oldfilename!;   // 複製元の名前は、画面を開く前に渡される
            set
            {
                oldfilename = value;
                // S9-14: 入力欄の初期値は複製元の名前（そのまま編集して別名にする）
                profileTxt.Text = value ?? string.Empty;
                profileTxt.SelectAll();
                profileTxt.Focus();
            }
        }

        public event EventHandler? Cancel;
        public delegate void SaveHandler(DupBox sender, string profilename);
        public event SaveHandler? Save;

        // 本コントロールは MainWindow.xaml から生成されるため、引数なしのコンストラクタが必要。
        // 依存は Initialize で受け取る（MainWindow のコンストラクタが 1 回だけ呼ぶ）。
        // プロファイルの複製（複製元の名前, 複製先の名前）。ファイルのコピーと新しい ProfileId の割り当ては、渡す側（Composition）が行う。
        private Action<string, string>? duplicateProfile;

        public DupBox()
        {
            InitCommands();
            InitializeComponent();
        }

        public void Initialize(Action<string, string> duplicateProfile)
        {
            if (this.duplicateProfile != null) throw new InvalidOperationException("DupBox is already initialized.");
            ArgumentNullException.ThrowIfNull(duplicateProfile);
            this.duplicateProfile = duplicateProfile;
        }

        private void SaveBtn()
        {
            string profile = profileTxt.Text;
            // 初期値が複製元の名前になったため、変えずに保存すると自分自身へのコピーになる（File.Copy が失敗する）。無効な名前として扱う
            if (!string.IsNullOrWhiteSpace(profile) &&
                !string.Equals(profile, oldfilename, StringComparison.OrdinalIgnoreCase) &&
                profile.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) == -1)
            {
                duplicateProfile!(oldfilename!, profile);   // Initialize と OldFilename は、画面を開く前に渡される
                Save?.Invoke(this, profile);
            }
            else
            {
                MessageBox.Show(Properties.Resources.ValidName, Properties.Resources.NotValid,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void CancelBtn()
        {
            Cancel?.Invoke(this, EventArgs.Empty);
        }
    }
}
