/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#nullable enable

using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for LanguagePackControl.xaml
    /// </summary>
    public partial class LanguagePackControl : UserControl
    {
        private LanguagePackViewModel? langPackVM;
        private Func<bool>? _saveSettings;
        private DS4Windows.UI.Abstractions.IUiThemeAndLanguage? _themeLanguage;   // 旧 Application.Current as App

        // 本コントロールは MainWindow.xaml から生成されるため、引数なしのコンストラクタが必要。
        // 依存は Initialize で受け取る（MainWindow のコンストラクタが、生成直後に呼ぶ）。
        public LanguagePackControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 依存を受け取り、言語パックの一覧の取得を開始する（置換前はコンストラクタで行っていた処理）。
        /// MainWindow のコンストラクタから 1 回だけ呼ぶ。
        /// </summary>
        public void Initialize(Func<bool> saveSettings,
            LanguagePackViewModel viewModel, DS4Windows.UI.Abstractions.IUiThemeAndLanguage themeLanguage)
        {
            if (saveSettings == null) throw new ArgumentNullException(nameof(saveSettings));
            if (themeLanguage == null) throw new ArgumentNullException(nameof(themeLanguage));
            if (_saveSettings != null) throw new InvalidOperationException("LanguagePackControl is already initialized.");

            _saveSettings = saveSettings;
            _themeLanguage = themeLanguage;
            ArgumentNullException.ThrowIfNull(viewModel);
            langPackVM = viewModel;
            this.DataContext = null;
            langPackVM.ScanFinished += LangPackVM_ScanFinished;
            langPackVM.SelectedIndexChanged += CheckForCultureChange;

            langPackVM.ScanForLangPacks();
        }

        private void CheckForCultureChange(object? sender, EventArgs e)
        {
            // Enable apply button when selection changes
            applyLangBtn.IsEnabled = true;
        }

        private void ApplyLangBtn_Click(object sender, RoutedEventArgs e)
        {
            if (langPackVM!.ChangeLanguagePack())   // Initialize の後でだけ押せる
            {
                // Apply language change immediately
                {
                    LangPackItem selectedItem = langPackVM.LangPackList![langPackVM.SelectedIndex];   // 言語を変えられるのは、一覧ができた後
                    string cultureCode = selectedItem.Name;
                    
                    // Apply to both Global and UI
                    _themeLanguage!.ApplyLanguage(cultureCode);   // 同上
                    
                    // Save to config
                    _saveSettings!();   // 同上
                    
                    MessageBox.Show(Translations.Strings.ResourceManager.GetString("LangPackControl.LanguageApplied") ?? "Language applied successfully",
                        "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                applyLangBtn.IsEnabled = false;
            }
        }

        private void LangPackVM_ScanFinished(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                cbCulture.IsEnabled = true;
                this.DataContext = langPackVM;
            }));
        }
    }
}
