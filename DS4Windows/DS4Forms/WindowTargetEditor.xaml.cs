using System;
using System.Windows;
using System.Windows.Controls;
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
