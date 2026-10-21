using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.Translations;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase10-Step1 W2（Phase10-Step1-Plan.md §3.1）: マクロの再生オプション（Execute Mode、Scan Code）の共通の画面部品。
    /// <see cref="MacroPlaybackOptions"/> を DataContext に受ける。Controls の <c>RecordBox</c>（W2）と、SA の Macro タブ（W3）が使う。
    /// <para>
    /// XAML を持たない（コードだけで組み立てる）。XAML の部品を、同じプロジェクトの別の XAML（<c>RecordBox.xaml</c>、<c>AssignmentEditor.xaml</c>）から
    /// 参照すると、クリーンビルドで WPF の一時プロジェクト（<c>*_wpftmp.csproj</c>）が、この部品の生成コードや BAML を見つけられず失敗した
    /// （「InitializeComponent が存在しない」「BG1002: .baml が見つかりません」。2026-10-21、Build-Debug.ps1 の再試行でも直らなかった）。コードだけの部品にして避ける。
    /// </para>
    /// </summary>
    public class MacroPlaybackOptionsPanel : UserControl
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

        /// <summary>Scan Code のチェックボックスの水平位置（<c>RecordBox</c> は中央、SA の Macro タブは左）。</summary>
        public static readonly DependencyProperty ScanCodeAlignmentProperty = DependencyProperty.Register(
            nameof(ScanCodeAlignment), typeof(HorizontalAlignment), typeof(MacroPlaybackOptionsPanel),
            new PropertyMetadata(HorizontalAlignment.Center, (d, e) => ((MacroPlaybackOptionsPanel)d).scanCodeCk.HorizontalAlignment =
                (HorizontalAlignment)e.NewValue));

        private readonly ComboBox playModeCombo;
        private readonly CheckBox scanCodeCk;
        private MacroPlaybackOptions options;

        public HorizontalAlignment ScanCodeAlignment
        {
            get => (HorizontalAlignment)GetValue(ScanCodeAlignmentProperty);
            set => SetValue(ScanCodeAlignmentProperty, value);
        }

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
            playModeCombo = new ComboBox { Margin = new Thickness(0, 4, 0, 0) };
            playModeCombo.Items.Add(new ComboBoxItem { Content = Strings.RecordBox_PlayOnce });
            playModeCombo.Items.Add(new ComboBoxItem { Content = Strings.RecordBox_RepeatWhileHeld });
            playModeCombo.SetBinding(Selector.SelectedIndexProperty,
                new Binding(nameof(MacroPlaybackOptions.PlayModeIndex)) { FallbackValue = 0 });

            scanCodeCk = new CheckBox
            {
                Content = Strings.ScanCode,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            scanCodeCk.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(MacroPlaybackOptions.UseScanCode)));

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Execute Mode:" });
            panel.Children.Add(playModeCombo);
            panel.Children.Add(scanCodeCk);
            Content = panel;

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
