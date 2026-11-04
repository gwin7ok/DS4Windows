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
        private const string TitleProperty = nameof(TitleText);   // 画面の入力欄の結びつき名（検証エラーの付け先）

        private readonly ISpecialActionRepository specialActionRepository;
        private string windowPath = string.Empty;
        private string titleText = string.Empty;
        private WindowTitleMatchMode titleMatchMode = WindowTitleMatchMode.Exact;
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

        /// <summary>
        /// 保存用のウインドウ名（Phase12-Step2 2h: 一致方法の印を含む。<c>^</c> 前方、<c>$</c> 後方、<c>*</c> 部分、印なし＝完全）。
        /// 画面の入力は、印を除いた <see cref="TitleText"/> と <see cref="TitleMatchMode"/> に分けて持ち、この値は両方から組み立てる。
        /// </summary>
        public string WindowTitle
        {
            get => WindowTargetText.ComposeTitle(titleText, titleMatchMode);
            set
            {
                string text = WindowTargetText.ParseTitle(value, out WindowTitleMatchMode mode);
                if (titleText == text && titleMatchMode == mode) return;
                titleText = text;
                titleMatchMode = mode;
                RaiseTitleChanged(true, true);
            }
        }

        /// <summary>入力欄に出す、印を除いたウインドウ名。入力に印が混ざっても、一致方法は一覧の選択を優先し、印は取り除く。</summary>
        public string TitleText
        {
            get => titleText;
            set
            {
                string text = WindowTargetText.ParseTitle(value, out _);
                if (titleText == text)
                {
                    // 印を取り除いたために、入力欄の表示とずれた場合は、表示を戻す
                    if (value != text) RaiseTitleChanged(true, false);
                    return;
                }

                titleText = text;
                RaiseTitleChanged(value != text, false);
            }
        }

        public WindowTitleMatchMode TitleMatchMode
        {
            get => titleMatchMode;
            set
            {
                if (titleMatchMode == value) return;
                titleMatchMode = value;
                RaiseTitleChanged(false, true);
            }
        }

        private void RaiseTitleChanged(bool textChanged, bool modeChanged)
        {
            if (textChanged) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleText)));
            if (modeChanged) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleMatchMode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
        }

        public double Delay { get => delay; set => delay = value; }

        /// <summary>Phase12-Step2 2j: 前面化のあと、ウィンドウの中央へマウスポインターを移動する。</summary>
        public bool CenterCursor { get; set; }

        public FocusWindowViewModel(ISpecialActionRepository specialActionRepository)
        {
            this.specialActionRepository = specialActionRepository ?? throw new ArgumentNullException(nameof(specialActionRepository));
        }

        public void LoadAction(SpecialAction action)
        {
            WindowPath = action.details;
            WindowTitle = action.extra;
            delay = action.delayTime;
            CenterCursor = action.centerCursor;
        }

        public void LoadControlItem(ControlSpecialItem item)
        {
            if (item == null) return;

            WindowPath = item.WindowPath;
            WindowTitle = item.WindowTitle;
            Delay = item.HoldSeconds;
            CenterCursor = item.CenterCursor;
        }

        public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type)
        {
            return new ControlSpecialItem
            {
                Type = type,
                WindowPath = windowPath,
                WindowTitle = WindowTitle,
                HoldSeconds = Delay,
                CenterCursor = CenterCursor,
            };
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            specialActionRepository.SaveAction(action.name, action.controls, 11, windowPath, edit, delay, WindowTitle, actionId: action.id, centerCursor: CenterCursor);
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            if (!string.IsNullOrWhiteSpace(windowPath) || !string.IsNullOrWhiteSpace(titleText)) return true;

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
