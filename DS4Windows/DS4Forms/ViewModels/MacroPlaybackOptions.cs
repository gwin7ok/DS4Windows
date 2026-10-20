using System.ComponentModel;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step1 W1（Phase10-Step1-Plan.md §3.1）: マクロの再生オプション（繰り返し、Scan Code）の共通の ViewModel。
    /// Controls の <c>RecordBox</c> と SA の Macro タブが、同じ部品（<c>MacroPlaybackOptionsPanel</c>。W2 以降）で編集する。
    /// 保存形式は変えない。値は、<see cref="DS4KeyType"/> の <see cref="DS4KeyType.RepeatMacro"/>（Phase8-Fix-MacroRepeatUnify で
    /// 旧 <c>HoldMacro</c> から統合）と <see cref="DS4KeyType.ScanCode"/> の有無で表す。
    /// </summary>
    public class MacroPlaybackOptions : INotifyPropertyChanged
    {
        /// <summary>ドロップダウンの添字。Play Once。</summary>
        public const int PlayOnceIndex = 0;

        /// <summary>ドロップダウンの添字。Repeat While Held。</summary>
        public const int RepeatWhileHeldIndex = 1;

        private bool repeatWhileHeld;
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
                OnPropertyChanged(nameof(RepeatWhileHeld));
                OnPropertyChanged(nameof(PlayModeIndex));
            }
        }

        /// <summary>
        /// ドロップダウンの添字（<see cref="PlayOnceIndex"/>＝Play Once、<see cref="RepeatWhileHeldIndex"/>＝Repeat While Held）。
        /// 1 以外（-1 を含む）は、Play Once として扱う。
        /// </summary>
        public int PlayModeIndex
        {
            get => repeatWhileHeld ? RepeatWhileHeldIndex : PlayOnceIndex;
            set => RepeatWhileHeld = value == RepeatWhileHeldIndex;
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

        /// <summary><paramref name="keyType"/> の <c>RepeatMacro</c>／<c>ScanCode</c> から、値を読み込む（ほかのビットは見ない）。</summary>
        public void LoadFrom(DS4KeyType keyType)
        {
            RepeatWhileHeld = keyType.HasFlag(DS4KeyType.RepeatMacro);
            UseScanCode = keyType.HasFlag(DS4KeyType.ScanCode);
        }

        /// <summary>
        /// <paramref name="keyType"/> の <c>RepeatMacro</c>／<c>ScanCode</c> のビットだけを、現在の値にそろえて返す。
        /// ほかのビット（<c>Macro</c>、<c>Toggle</c>、<c>Unbound</c> など）は、そのまま保つ。
        /// </summary>
        public DS4KeyType ApplyTo(DS4KeyType keyType)
        {
            keyType = repeatWhileHeld ? keyType | DS4KeyType.RepeatMacro : keyType & ~DS4KeyType.RepeatMacro;
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
