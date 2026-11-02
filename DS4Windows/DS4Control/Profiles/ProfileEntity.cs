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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4WinWPF
{
    // Phase11 W6 K-W6-9: INotifyPropertyChanged を実装した。以前は旧式の NameChanged イベントだけで名前の変更を通知しており、
    // WPF は {Binding Name} に、リフレクションによる旧式の購読（ValueChangedEventManager）を使っていた。リンクのコンボの選択項目が
    // 一覧から削除されると、選択欄の中身の型が入れ替わる途中で、この購読が TargetException（Object does not match target type）で
    // 失敗し、アプリが落ちた。NameChanged は、既存の購読先のために残す。
    public class ProfileEntity : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event EventHandler NameChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        // Phase6-Step10-4-1: Global.appdatapath の直接参照を廃止し、IPathService を ProfileList から受け取る
        private readonly DS4Windows.DI.IPathService pathService;

        public ProfileEntity(DS4Windows.DI.IPathService pathService)
        {
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        public event EventHandler ProfileDeleted;

        public void DeleteFile()
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                string filepath = pathService.AppDataPath + @"\Profiles\" + name + ".xml";
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    ProfileDeleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void RenameProfile(string newProfileName)
        {
            string oldFilePath = Path.Combine(pathService.AppDataPath,
                "Profiles", $"{name}.xml");

            string newFilePath = Path.Combine(pathService.AppDataPath,
                "Profiles", $"{newProfileName}.xml");

            if (File.Exists(oldFilePath) && !File.Exists(newFilePath))
            {
                File.Move(oldFilePath, newFilePath);
                // Send NameChanged event so controls get updated with new name
                Name = newProfileName;
            }
        }
    }
}
