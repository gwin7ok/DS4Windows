using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase10-Step1 W2（Phase10-Step1-Plan.md §3.1）: マクロの再生オプション（Execute Mode、Scan Code）の共通の画面部品。
    /// <see cref="MacroPlaybackOptions"/> を DataContext に受ける。Controls の <c>RecordBox</c>（W2）と、SA の Macro タブ（W3）が使う。
    /// </summary>
    public partial class MacroPlaybackOptionsPanel : UserControl
    {
        /// <summary>
        /// Execute Mode のドロップダウンを、呼び出し側が一時的に無効にするための指定（<c>RecordBox</c> の記録中。従来の
        /// <c>macroModeCombo.IsEnabled = on &amp;&amp; Repeatable</c> と同じ）。<see cref="MacroPlaybackOptions.Repeatable"/> が偽なら、常に無効。
        /// </summary>
        public static readonly DependencyProperty AllowModeEditProperty = DependencyProperty.Register(
            nameof(AllowModeEdit), typeof(bool), typeof(MacroPlaybackOptionsPanel),
            new PropertyMetadata(true, (d, e) => ((MacroPlaybackOptionsPanel)d).UpdateModeEnabled()));

        /// <summary>Scan Code のチェックボックスを出すか（<c>RecordBox</c> の <c>showscan</c> の値）。</summary>
        public static readonly DependencyProperty ShowScanCodeProperty = DependencyProperty.Register(
            nameof(ShowScanCode), typeof(bool), typeof(MacroPlaybackOptionsPanel),
            new PropertyMetadata(true, (d, e) => ((MacroPlaybackOptionsPanel)d).scanCodeCk.Visibility =
                (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        private MacroPlaybackOptions options;

        public bool AllowModeEdit
        {
            get => (bool)GetValue(AllowModeEditProperty);
            set => SetValue(AllowModeEditProperty, value);
        }

        public bool ShowScanCode
        {
            get => (bool)GetValue(ShowScanCodeProperty);
            set => SetValue(ShowScanCodeProperty, value);
        }

        public MacroPlaybackOptionsPanel()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (options != null) options.PropertyChanged -= OnOptionsPropertyChanged;
            options = e.NewValue as MacroPlaybackOptions;
            if (options != null) options.PropertyChanged += OnOptionsPropertyChanged;
            UpdateModeEnabled();
        }

        private void OnOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MacroPlaybackOptions.Repeatable)) UpdateModeEnabled();
        }

        private void UpdateModeEnabled()
        {
            playModeCombo.IsEnabled = AllowModeEdit && (options?.Repeatable ?? true);
        }
    }
}
