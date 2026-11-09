using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// ViewModel の変更通知の共通の土台。プロパティごとに「名前＋Changed」のイベントを書かず、
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> へ集める。値の保持は派生クラスのフィールド、または設定サービスが担う。
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 値が変わったときだけ、フィールドを更新して通知する。変わらなければ何もせず false を返す。
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
