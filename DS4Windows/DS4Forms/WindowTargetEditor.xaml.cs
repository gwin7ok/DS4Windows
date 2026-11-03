using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Microsoft.Win32;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase12-Step2 2e（R-4）: 対象のウィンドウを指定する入力欄（プログラムのパスとウィンドウタイトル）。FocusWindow の編集画面で使う。
    /// 書式は Auto-Profile と同じ（<see cref="DS4Windows.WindowTargetMatcher"/>）。
    /// </summary>
    public partial class WindowTargetEditor : UserControl
    {
        // Path は UserControl.Path ではなく依存関係プロパティ名として使う（Control に同名の既定メンバーはない）
        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
            nameof(Path), typeof(string), typeof(WindowTargetEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(WindowTargetEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // Phase12-Step2 2h: タイトルの一致方法（一覧の添字と WindowTitleMatchMode の値は同じ並び）
        public static readonly DependencyProperty TitleMatchModeProperty = DependencyProperty.Register(
            nameof(TitleMatchMode), typeof(WindowTitleMatchMode), typeof(WindowTargetEditor),
            new FrameworkPropertyMetadata(WindowTitleMatchMode.Exact, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTitleMatchModeChanged));

        public WindowTitleMatchMode TitleMatchMode
        {
            get => (WindowTitleMatchMode)GetValue(TitleMatchModeProperty);
            set => SetValue(TitleMatchModeProperty, value);
        }

        private static void OnTitleMatchModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (WindowTargetEditor)d;
            editor.titleMatchCombo.SelectedIndex = (int)(WindowTitleMatchMode)e.NewValue;
        }

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public WindowTargetEditor()
        {
            InitializeComponent();
            titleMatchCombo.SelectedIndex = (int)TitleMatchMode;
        }

        private void TitleMatchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (titleMatchCombo.SelectedIndex >= 0) TitleMatchMode = (WindowTitleMatchMode)titleMatchCombo.SelectedIndex;
        }

        /// <summary>Phase12-Step2 2i: ドロップした位置のウィンドウの取得。表示側（<c>AssignmentEditor</c>）が設定する。未設定のときは、ターゲットマークは動かない。</summary>
        public WindowTargetPicker Picker { get; set; }

        private void TargetMark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Picker == null) return;

            targetMark.CaptureMouse();
            Mouse.OverrideCursor = Cursors.Cross;
            e.Handled = true;
        }

        private void TargetMark_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!targetMark.IsMouseCaptured) return;

            targetMark.ReleaseMouseCapture();
            e.Handled = true;

            // 取れたときは、フルパスとタイトル全文を入れ、一致方法は完全一致に戻す（入れたあとは手で変えられる）
            if (Picker != null && Picker.TryPickUnderCursor(out string path, out string title))
            {
                TitleMatchMode = WindowTitleMatchMode.Exact;
                Path = path;
                Title = title;
            }
        }

        private void TargetMark_LostMouseCapture(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void ExeNameOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            Path = WindowTargetText.ToExecutableNameOnly(Path);
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                DefaultExt = ".exe",
                Filter = "Exe (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Program",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            };

            if (dialog.ShowDialog() == true)
            {
                Path = dialog.FileName;
            }
        }
    }
}
