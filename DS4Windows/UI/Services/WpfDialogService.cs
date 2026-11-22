using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="IDialogService"/> の実装。WPF の <see cref="MessageBox"/> と、Win32 のファイル選択の窓を使う。
    /// 親の窓は、いま操作されている窓（なければメイン画面）。UI のスレッド以外から呼ばれたら、UI のスレッドへ渡して待つ。
    /// </summary>
    public sealed class WpfDialogService : IDialogService
    {
        private readonly IUiDispatcher _dispatcher;

        public WpfDialogService(IUiDispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);
            _dispatcher = dispatcher;
        }

        public void ShowMessage(string message, string? title = null, DialogIcon icon = DialogIcon.None)
        {
            Show(message, title, MessageBoxButton.OK, icon);
        }

        public bool Confirm(string message, string? title = null, DialogIcon icon = DialogIcon.Question)
        {
            return Show(message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
        }

        public DialogChoice ChooseOneOfThree(string message, string? title, string firstText, string secondText, string thirdText, DialogIcon icon = DialogIcon.Question)
        {
            return Run(() =>
            {
                Window? owner = Owner();
                var dialog = new TaskDialog
                {
                    WindowTitle = title ?? string.Empty,
                    Content = message,
                    MainIcon = ToTaskIcon(icon),
                    ButtonStyle = TaskDialogButtonStyle.Standard,
                };
                var first = new TaskDialogButton(firstText);
                var second = new TaskDialogButton(secondText);
                var third = new TaskDialogButton(thirdText);
                dialog.Buttons.Add(first);
                dialog.Buttons.Add(second);
                dialog.Buttons.Add(third);
                TaskDialogButton? clicked = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                if (clicked == first) return DialogChoice.First;
                if (clicked == second) return DialogChoice.Second;
                if (clicked == third) return DialogChoice.Third;
                return DialogChoice.Cancelled;
            });
        }

        public string? PromptText(string message, string? title, string initialText = "")
        {
            return Run(() =>
            {
                // Ookii の WPF 版には入力の窓がないため、1 行の入力だけの小さな窓をコードで組み立てる（XAML の部品を増やさない）
                var box = new System.Windows.Controls.TextBox { Text = initialText, Margin = new Thickness(0, 8, 0, 12), MinWidth = 320 };
                var ok = new System.Windows.Controls.Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new System.Windows.Controls.Button { Content = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ? "キャンセル" : "Cancel", IsCancel = true, MinWidth = 80 };
                var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);
                var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
                panel.Children.Add(new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
                panel.Children.Add(box);
                panel.Children.Add(buttons);
                var window = new Window
                {
                    Title = title ?? string.Empty,
                    Content = panel,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                Window? owner = Owner();
                if (owner != null) window.Owner = owner;
                ok.Click += (s, e) => window.DialogResult = true;
                window.Loaded += (s, e) => { box.Focus(); box.SelectAll(); };
                return window.ShowDialog() == true ? box.Text : null;
            });
        }

        public string? PickOpenFile(FileDialogOptions options)
        {
            return Run(() =>
            {
                var dialog = new OpenFileDialog { CheckFileExists = options?.CheckFileExists ?? true };
                Apply(dialog, options);
                return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
            });
        }

        public string? PickFolder()
        {
            return Run(() =>
            {
                var dialog = new VistaFolderBrowserDialog();
                return dialog.ShowDialog(Owner()) == true ? dialog.SelectedPath : null;
            });
        }

        public string? PickSaveFile(FileDialogOptions options)
        {
            return Run(() =>
            {
                var dialog = new SaveFileDialog();
                Apply(dialog, options);
                return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
            });
        }

        private static void Apply(FileDialog dialog, FileDialogOptions? options)
        {
            if (options == null) return;
            if (options.Title != null) dialog.Title = options.Title;
            if (options.Filter != null) dialog.Filter = options.Filter;
            if (options.InitialDirectory != null) dialog.InitialDirectory = options.InitialDirectory;
            if (options.FileName != null) dialog.FileName = options.FileName;
            if (options.DefaultExtension != null) dialog.DefaultExt = options.DefaultExtension;
        }

        private MessageBoxResult Show(string message, string? title, MessageBoxButton buttons, DialogIcon icon)
        {
            return Run(() =>
            {
                Window? owner = Owner();
                MessageBoxImage image = ToImage(icon);
                string caption = title ?? string.Empty;
                return owner != null
                    ? MessageBox.Show(owner, message, caption, buttons, image)
                    : MessageBox.Show(message, caption, buttons, image);
            });
        }

        private T Run<T>(Func<T> func)
        {
            T result = default!;   // UI スレッドでの実行が失敗した場合の既定値
            _dispatcher.Invoke(() => result = func());
            return result;
        }

        private static Window? Owner()
        {
            Application app = Application.Current;
            if (app == null) return null;
            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
        }

        private static TaskDialogIcon ToTaskIcon(DialogIcon icon)
        {
            switch (icon)
            {
                case DialogIcon.Information: return TaskDialogIcon.Information;
                case DialogIcon.Warning: return TaskDialogIcon.Warning;
                case DialogIcon.Error: return TaskDialogIcon.Error;
                case DialogIcon.Question: return TaskDialogIcon.Information;
                default: return TaskDialogIcon.Custom;
            }
        }

        private static MessageBoxImage ToImage(DialogIcon icon)
        {
            switch (icon)
            {
                case DialogIcon.Information: return MessageBoxImage.Information;
                case DialogIcon.Warning: return MessageBoxImage.Warning;
                case DialogIcon.Error: return MessageBoxImage.Error;
                case DialogIcon.Question: return MessageBoxImage.Question;
                default: return MessageBoxImage.None;
            }
        }
    }
}
