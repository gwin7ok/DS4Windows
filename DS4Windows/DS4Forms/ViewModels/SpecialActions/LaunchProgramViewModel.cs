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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class LaunchProgramViewModel : NotifyDataErrorBase, IActionTypeEditor
    {
        private string filepath;
        private double delay;
        private string arguments;

        public string Filepath
        {
            get => filepath;
            set
            {
                if (filepath == value) return;
                filepath = value;
                FilepathChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FilepathChanged;
        public double Delay { get => delay; set => delay = value; }
        public string Arguments { get => arguments; set => arguments = value; }

        public ImageSource ProgramIcon
        {
            get
            {
                ImageSource exeicon = null;
                string path = filepath;
                if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".exe")
                {
                    using (Icon ico = Icon.ExtractAssociatedIcon(path))
                    {
                        exeicon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        exeicon.Freeze();
                    }
                }

                return exeicon;
            }
        }
        public event EventHandler ProgramIconChanged;

        public string ProgramName
        {
            get
            {
                string temp = "";
                if (!string.IsNullOrEmpty(filepath))
                {
                    temp = Path.GetFileNameWithoutExtension(filepath);
                }

                return temp;
            }
        }
        public event EventHandler ProgramNameChanged;

        public LaunchProgramViewModel(ISpecialActionRepository specialActionRepository)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
            FilepathChanged += LaunchProgramViewModel_FilepathChanged;
        }

        private void LaunchProgramViewModel_FilepathChanged(object sender, EventArgs e)
        {
            ProgramIconChanged?.Invoke(this, EventArgs.Empty);
            ProgramNameChanged?.Invoke(this, EventArgs.Empty);
        }

        public void LoadAction(SpecialAction action)
        {
            filepath = action.details;
            delay = action.delayTime;
            arguments = action.extra;
        }

        // Phase10-Step6 I6b: Controls のモード。型のある項目との往復（以前は ControlSettingsEditTarget が書いていた）
        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            Filepath = item.ProgramPath;
            Delay = item.HoldSeconds;
            Arguments = ComposeArguments(item.ProgramArguments, item.HideWindow);
        }

        // Phase10-Step4 W5: Launch Program。パスは未選択（null）でも空にする
        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type)
        {
            SplitArguments(Arguments, out string args, out bool hideWindow);
            return new ControlSpecialItem
            {
                Type = type,
                ProgramPath = Filepath ?? string.Empty,
                HoldSeconds = Delay,
                ProgramArguments = args,
                HideWindow = hideWindow,
            };
        }

        /// <summary>
        /// 引数の欄の文字列から、ウィンドウを隠す語（<c>$hidden</c>。大文字小文字不問）を、SA の起動処理と同じく 1 回だけ取り除く。
        /// 語があれば <paramref name="hideWindow"/> が真で、<paramref name="arguments"/> は、取り除いた残り（空白はそのまま）。
        /// </summary>
        public static void SplitArguments(string text, out string arguments, out bool hideWindow)
        {
            text = text ?? string.Empty;
            int pos = text.IndexOf(DS4Windows.Services.ControlBindingBuilder.HiddenKeyword, StringComparison.OrdinalIgnoreCase);
            hideWindow = pos >= 0;
            arguments = hideWindow ? text.Remove(pos, DS4Windows.Services.ControlBindingBuilder.HiddenKeyword.Length) : text;
        }

        /// <summary>引数の欄に表示する文字列。ウィンドウを隠すときは、先頭に語を付ける（起動時の引数は同じ。語の位置は先頭に移る）。</summary>
        public static string ComposeArguments(string arguments, bool hideWindow)
            => (hideWindow ? DS4Windows.Services.ControlBindingBuilder.HiddenKeyword : string.Empty) + (arguments ?? string.Empty);

        // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
        private readonly ISpecialActionRepository specialActionRepository;

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            specialActionRepository.SaveAction(action.name, action.controls, 2, $"{filepath}?{delay.ToString("#.##", ConfigFileCulture.DecimalCulture)}", edit, delay, arguments, actionId: action.id);
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> filepathErrors = new List<string>();

            // Phase10-Step4c W3: パスが未選択（null）・空は、必須の値がないエラー（保存を止める。以前は null で NullReferenceException）。
            // ファイルが存在しないのは警告のみ（別のドライブ、あとで作る場合があるため。表示するが、保存は止めない）
            if (string.IsNullOrWhiteSpace(filepath))
            {
                valid = false;
                filepathErrors.Add("Filepath empty");
            }
            else if (!File.Exists(filepath))
            {
                filepathErrors.Add("File at path does not exist");
            }

            if (filepathErrors.Count > 0)
            {
                errors["Filepath"] = filepathErrors;
                RaiseErrorsChanged("Filepath");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Filepath");
            }
        }
    }
}
