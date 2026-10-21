using System;
using System.ComponentModel;
using System.Linq;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2）: トリガーの一覧の 1 行（チェックボックス 1 つ）の ViewModel。チェックの状態は、
    /// トリガーの供給元（<see cref="IActionTriggerSource"/>）が持つ値そのもの（このクラスは、値を重複して持たない）。
    /// 従来は、<c>AssignmentEditor</c> のコードビハインドが、名前つきのチェックボックス 49 個の <c>Click</c> で、リストを書き換えていた。
    /// </summary>
    public sealed class TriggerOptionViewModel : INotifyPropertyChanged
    {
        private readonly TriggerItem item;
        private readonly IActionTriggerSource source;

        public event PropertyChangedEventHandler PropertyChanged;

        public TriggerOptionViewModel(TriggerItem item, IActionTriggerSource source)
        {
            this.item = item ?? throw new ArgumentNullException(nameof(item));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>保存する名前。</summary>
        public string Name => item.Name;

        /// <summary>表示名。</summary>
        public string Label => item.Label;

        /// <summary>
        /// 選ばれているか。書き込みは、供給元へ中継する（供給元が変えられないとき、<see cref="IActionTriggerSource.CanChange"/> が偽なら、無視する）。
        /// </summary>
        public bool IsSelected
        {
            get => source.Triggers.Contains(item.Name);
            set
            {
                if (IsSelected == value) return;
                if (!source.SetTrigger(item.Name, value)) return;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        /// <summary>チェックを変えられるか（Controls のモードは偽）。</summary>
        public bool IsEnabled => source.CanChange;

        /// <summary>供給元の値が、外から変わった（読み込みなど）ときに、画面へ知らせる。</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
