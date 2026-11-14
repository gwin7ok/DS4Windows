using System;
using System.Windows.Input;

namespace DS4Windows.UI.ViewModels.Util
{
    /// <summary>
    /// <see cref="ICommand"/> の小さな実装。画面のボタン操作を ViewModel のメソッドへ結ぶ（XAML は <c>Command="{Binding …}"</c>）。
    /// 実行できるかの再評価は、WPF の <c>CommandManager</c> に任せず、状態を変えた側が <see cref="RaiseCanExecuteChanged"/> を呼ぶ
    /// （ViewModel を WPF なしでテストできるようにするため）。
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(execute == null ? null : new Action<object>(_ => execute()),
                   canExecute == null ? null : new Func<object, bool>(_ => canExecute()))
        {
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => canExecute == null || canExecute(parameter);

        public void Execute(object parameter)
        {
            if (CanExecute(parameter)) execute(parameter);
        }

        /// <summary>実行できるかが変わったことを、結びつけ先（ボタン）へ知らせる。</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
