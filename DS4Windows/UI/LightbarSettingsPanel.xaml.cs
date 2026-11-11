using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「ライトバー」タブの中身。DataContext は <see cref="LightbarSettingsViewModel"/>。
    /// 色の選択では、選択中の色を実機のライトバーへ一時的に出し（強制表示）、閉じたら戻す。
    /// </summary>
    public partial class LightbarSettingsPanel : UserControl
    {
        public LightbarSettingsPanel()
        {
            InitializeComponent();
            emptyColorGB.Visibility = Visibility.Collapsed;
            // 「最大の明るさ」の書式は表示言語の文言（XAML の StringFormat は固定の英語のため、ここで差し替える）
            maxLightText.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("MaxSatRainbow")
            {
                FallbackValue = 100,
                StringFormat = DS4Windows.UI.Services.LocalizedText.Get("LightbarMaxLight")
            });
        }

        private LightbarSettingsViewModel ViewModel => (LightbarSettingsViewModel)DataContext;

        /// <summary>「バッテリー残量で色を変える」の設定に合わせて、色の枠の見出しと「空の色」の枠の表示を揃える。</summary>
        public void RefreshColorByBatteryLayout(LightbarSettingsViewModel lightbar)
        {
            if (lightbar.ColorBatteryPercent)
            {
                colorGB.Header = Translations.Strings.Full;
                emptyColorGB.Visibility = Visibility.Visible;
            }
            else
            {
                colorGB.Header = Translations.Strings.Color;
                emptyColorGB.Visibility = Visibility.Hidden;
            }
        }

        private void ColorByBatteryPerCk_Click(object sender, RoutedEventArgs e)
        {
            RefreshColorByBatteryLayout(ViewModel);
        }

        private void FlashColorBtn_Click(object sender, RoutedEventArgs e)
        {
            PickColor(ViewModel.FlashColorMedia, ViewModel.UpdateFlashColor);
        }

        private void LowColorBtn_Click(object sender, RoutedEventArgs e)
        {
            PickColor(ViewModel.LowColorMedia, ViewModel.UpdateLowColor);
        }

        private void ChargingColorBtn_Click(object sender, RoutedEventArgs e)
        {
            PickColor(ViewModel.ChargingColorMedia, ViewModel.UpdateChargingColor);
        }

        /// <summary>
        /// 色の選択画面を開く。選択中の色は実機のライトバーへ一時的に出し、閉じたら戻して、最後に選んだ色を設定へ書く。
        /// </summary>
        private void PickColor(Color current, Action<Color> apply)
        {
            LightbarSettingsViewModel vm = ViewModel;
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            dialog.colorPicker.SelectedColor = current;
            vm.StartForcedColor(current);
            dialog.ColorChanged += (sender2, color) => { vm.UpdateForcedColor(color); };
            dialog.ShowDialog();
            vm.EndForcedColor();
            apply(dialog.colorPicker.SelectedColor.GetValueOrDefault());
        }

        private void RainbowBtn_Click(object sender, RoutedEventArgs e)
        {
            LightbarSettingsViewModel vm = ViewModel;
            bool active = vm.Rainbow != 0.0;
            if (active)
            {
                vm.Rainbow = 0.0;
                colorByBatteryPerCk.Content = Properties.Resources.ColorByBattery;
                colorGB.IsEnabled = true;
                emptyColorGB.IsEnabled = true;
            }
            else
            {
                vm.Rainbow = 5.0;
                colorByBatteryPerCk.Content = Properties.Resources.DimByBattery;
                colorGB.IsEnabled = false;
                emptyColorGB.IsEnabled = false;
            }
        }
    }
}
