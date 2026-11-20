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

        public void ShowMessage(string message, string title = null, DialogIcon icon = DialogIcon.None)
        {
            Show(message, title, MessageBoxButton.OK, icon);
        }

        public bool Confirm(string message, string title = null, DialogIcon icon = DialogIcon.Question)
        {
            return Show(message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
        }

        public string PickOpenFile(FileDialogOptions options)
        {
            return Run(() =>
            {
                var dialog = new OpenFileDialog { CheckFileExists = options?.CheckFileExists ?? true };
                Apply(dialog, options);
                return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
            });
        }

        public string PickFolder()
        {
            return Run(() =>
            {
                var dialog = new VistaFolderBrowserDialog();
                return dialog.ShowDialog(Owner()) == true ? dialog.SelectedPath : null;
            });
        }

        public string PickSaveFile(FileDialogOptions options)
        {
            return Run(() =>
            {
                var dialog = new SaveFileDialog();
                Apply(dialog, options);
                return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
            });
        }

        private static void Apply(FileDialog dialog, FileDialogOptions options)
        {
            if (options == null) return;
            if (options.Title != null) dialog.Title = options.Title;
            if (options.Filter != null) dialog.Filter = options.Filter;
            if (options.InitialDirectory != null) dialog.InitialDirectory = options.InitialDirectory;
            if (options.FileName != null) dialog.FileName = options.FileName;
            if (options.DefaultExtension != null) dialog.DefaultExt = options.DefaultExtension;
        }

        private MessageBoxResult Show(string message, string title, MessageBoxButton buttons, DialogIcon icon)
        {
            return Run(() =>
            {
                Window owner = Owner();
                MessageBoxImage image = ToImage(icon);
                string caption = title ?? string.Empty;
                return owner != null
                    ? MessageBox.Show(owner, message, caption, buttons, image)
                    : MessageBox.Show(message, caption, buttons, image);
            });
        }

        private T Run<T>(Func<T> func)
        {
            T result = default;
            _dispatcher.Invoke(() => result = func());
            return result;
        }

        private static Window Owner()
        {
            Application app = Application.Current;
            if (app == null) return null;
            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
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
