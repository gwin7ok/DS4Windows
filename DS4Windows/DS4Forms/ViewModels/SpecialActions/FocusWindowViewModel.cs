using System;
using System.Collections.Generic;
using System.ComponentModel;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase12-Step2 2e（R-4）: FocusWindow（指定のウィンドウを前面へ出す）の編集。対象は、プログラムのパスとウィンドウタイトル
    /// （どちらも省略可。両方を指定したときは両方に一致するウィンドウ。Auto-Profile と同じ書式）。パスもタイトルも空は、どのウィンドウにも一致してしまうため、保存を止める。
    /// SA は <c>SaveActionNew</c> のモード 11（Details＝パス、Extras＝タイトル、Delay＝押し続ける秒数）で保存する。
    /// </summary>
    public class FocusWindowViewModel : NotifyDataErrorBase, IActionTypeEditor, INotifyPropertyChanged
    {
        private const string PathProperty = nameof(WindowPath);
        private const string TitleProperty = nameof(WindowTitle);

        private readonly ISpecialActionRepository specialActionRepository;
        private string windowPath = string.Empty;
        private string windowTitle = string.Empty;
        private double delay;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowPath
        {
            get => windowPath;
            set
            {
                value = value ?? string.Empty;
                if (windowPath == value) return;
                windowPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PathProperty));
            }
        }

        public string WindowTitle
        {
            get => windowTitle;
            set
            {
                value = value ?? string.Empty;
                if (windowTitle == value) return;
                windowTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(TitleProperty));
            }
        }

        public double Delay { get => delay; set => delay = value; }

        public FocusWindowViewModel(ISpecialActionRepository specialActionRepository)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
        }

        public void LoadAction(SpecialAction action)
        {
            WindowPath = action.details;
            WindowTitle = action.extra;
            delay = action.delayTime;
        }

        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            WindowPath = item.WindowPath;
            WindowTitle = item.WindowTitle;
            Delay = item.HoldSeconds;
        }

        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type)
        {
            return new ControlSpecialItem
            {
                Type = type,
                WindowPath = windowPath,
                WindowTitle = windowTitle,
                HoldSeconds = Delay,
            };
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            specialActionRepository.SaveAction(action.name, action.controls, 11, windowPath, edit, delay, windowTitle, actionId: action.id);
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            if (!string.IsNullOrWhiteSpace(windowPath) || !string.IsNullOrWhiteSpace(windowTitle)) return true;

            // 両方が空は、必須の値がないエラー（保存を止める）。入力欄（パス・タイトル）に赤い枠で示す
            errors[PathProperty] = new List<string> { "Program path or window title is required" };
            errors[TitleProperty] = new List<string> { "Program path or window title is required" };
            RaiseErrorsChanged(PathProperty);
            RaiseErrorsChanged(TitleProperty);
            return false;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged(PathProperty);
                RaiseErrorsChanged(TitleProperty);
            }
        }
    }
}
