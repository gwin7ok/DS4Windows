using System.ComponentModel;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step1 W1（Phase10-Step1-Plan.md §3.1）: マクロの再生オプション（繰り返し、Scan Code）の共通の ViewModel。
    /// Controls の <c>RecordBox</c> と SA の Macro タブが、同じ部品（<c>MacroPlaybackOptionsPanel</c>。W2 以降）で編集する。
    /// 値は、<see cref="DS4KeyType"/> の <see cref="DS4KeyType.RepeatMacro"/>（Phase8-Fix-MacroRepeatUnify で
    /// 旧 <c>HoldMacro</c> から統合）、<see cref="DS4KeyType.RepeatLatchMacro"/>（R-2: 繰り返しをトグルする）、<see cref="DS4KeyType.ScanCode"/> の有無で表す。
    /// 実行時の表現であり、保存形式（<c>MacroExecuteMode</c>）との変換は保存・読込の層が行う（R2-MacroToggleRepeat-Plan.md §3.1）。
    /// </summary>
    public class MacroPlaybackOptions : INotifyPropertyChanged
    {
        /// <summary>ドロップダウンの添字。Play Once。</summary>
        public const int PlayOnceIndex = 0;

        /// <summary>ドロップダウンの添字。Repeat While Held。</summary>
        public const int RepeatWhileHeldIndex = 1;

        /// <summary>ドロップダウンの添字。Repeat (Toggle)（R-2）。</summary>
        public const int RepeatToggleIndex = 2;

        private bool repeatWhileHeld;
        private bool repeatToggle;
        private bool useScanCode;
        private bool repeatable;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <param name="repeatable">繰り返しを選べるか。Press Key／MultiAction のマクロは偽（画面で、ドロップダウンを無効にする）。</param>
        public MacroPlaybackOptions(bool repeatable = true)
        {
            this.repeatable = repeatable;
        }

        /// <summary>
        /// 繰り返しを選べるか（画面の有効・無効だけに使う）。偽でも、<see cref="RepeatWhileHeld"/> の値は消さない
        /// （従来の <c>RecordBox</c> は、ドロップダウンを無効にしても、読み込んだ <c>RepeatMacro</c> を保存時に書き戻していた。挙動を保つ）。
        /// </summary>
        public bool Repeatable
        {
            get => repeatable;
            set
            {
                if (repeatable == value) return;
                repeatable = value;
                OnPropertyChanged(nameof(Repeatable));
            }
        }

        /// <summary>押している間、繰り返す（<see cref="DS4KeyType.RepeatMacro"/>）。</summary>
        public bool RepeatWhileHeld
        {
            get => repeatWhileHeld;
            set
            {
                if (repeatWhileHeld == value) return;
                repeatWhileHeld = value;
                bool toggleCleared = value && repeatToggle;   // モードは 1 つだけ
                if (toggleCleared) repeatToggle = false;
                OnPropertyChanged(nameof(RepeatWhileHeld));
                if (toggleCleared) OnPropertyChanged(nameof(RepeatToggle));
                OnPropertyChanged(nameof(PlayModeIndex));
            }
        }

        /// <summary>
        /// 押すたびに、繰り返しの開始・停止を切り替える（<see cref="DS4KeyType.RepeatLatchMacro"/>。R-2）。
        /// <see cref="RepeatWhileHeld"/> と同時には真にならない（真にすると、<see cref="RepeatWhileHeld"/> は偽になる）。
        /// </summary>
        public bool RepeatToggle
        {
            get => repeatToggle;
            set
            {
                if (repeatToggle == value) return;
                repeatToggle = value;
                bool heldCleared = value && repeatWhileHeld;   // モードは 1 つだけ
                if (heldCleared) repeatWhileHeld = false;
                OnPropertyChanged(nameof(RepeatToggle));
                if (heldCleared) OnPropertyChanged(nameof(RepeatWhileHeld));
                OnPropertyChanged(nameof(PlayModeIndex));
            }
        }

        /// <summary>
        /// ドロップダウンの添字（<see cref="PlayOnceIndex"/>＝Play Once、<see cref="RepeatWhileHeldIndex"/>＝Repeat While Held、
        /// <see cref="RepeatToggleIndex"/>＝Repeat (Toggle)）。範囲外（-1 を含む）は、Play Once として扱う。
        /// </summary>
        public int PlayModeIndex
        {
            get => repeatToggle ? RepeatToggleIndex : repeatWhileHeld ? RepeatWhileHeldIndex : PlayOnceIndex;
            set
            {
                // 先に両方を下げてから、選ばれた方を上げる（通知は、プロパティの setter に任せる）
                if (value != RepeatWhileHeldIndex) RepeatWhileHeld = false;
                if (value != RepeatToggleIndex) RepeatToggle = false;
                if (value == RepeatWhileHeldIndex) RepeatWhileHeld = true;
                else if (value == RepeatToggleIndex) RepeatToggle = true;
            }
        }

        /// <summary>スキャンコードで送る（<see cref="DS4KeyType.ScanCode"/>）。</summary>
        public bool UseScanCode
        {
            get => useScanCode;
            set
            {
                if (useScanCode == value) return;
                useScanCode = value;
                OnPropertyChanged(nameof(UseScanCode));
            }
        }

        /// <summary>
        /// <paramref name="keyType"/> の <c>RepeatMacro</c>／<c>RepeatLatchMacro</c>／<c>ScanCode</c> から、値を読み込む（ほかのビットは見ない）。
        /// <c>RepeatMacro</c> と <c>RepeatLatchMacro</c> が両方立っている（通常は起きない）ときは、<c>RepeatLatchMacro</c> を優先する。
        /// </summary>
        public void LoadFrom(DS4KeyType keyType)
        {
            RepeatWhileHeld = keyType.HasFlag(DS4KeyType.RepeatMacro);
            RepeatToggle = keyType.HasFlag(DS4KeyType.RepeatLatchMacro);
            UseScanCode = keyType.HasFlag(DS4KeyType.ScanCode);
        }

        /// <summary>
        /// <paramref name="keyType"/> の <c>RepeatMacro</c>／<c>RepeatLatchMacro</c>／<c>ScanCode</c> のビットだけを、現在の値にそろえて返す。
        /// ほかのビット（<c>Macro</c>、<c>Toggle</c>、<c>Unbound</c> など）は、そのまま保つ。
        /// </summary>
        public DS4KeyType ApplyTo(DS4KeyType keyType)
        {
            keyType = repeatWhileHeld ? keyType | DS4KeyType.RepeatMacro : keyType & ~DS4KeyType.RepeatMacro;
            keyType = repeatToggle ? keyType | DS4KeyType.RepeatLatchMacro : keyType & ~DS4KeyType.RepeatLatchMacro;
            keyType = useScanCode ? keyType | DS4KeyType.ScanCode : keyType & ~DS4KeyType.ScanCode;
            return keyType;
        }

        /// <summary><paramref name="keyType"/> から読み込んだ新しいインスタンスを返す。</summary>
        public static MacroPlaybackOptions FromKeyType(DS4KeyType keyType, bool repeatable = true)
        {
            var options = new MacroPlaybackOptions(repeatable);
            options.LoadFrom(keyType);
            return options;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
