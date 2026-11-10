using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using WPFLocalizeExtension.Engine;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// アプリ全体のテーマ（資源の辞書の差し替え）と表示言語（スレッドのカルチャ、WPFLocalizeExtension）の切り替え。
    /// 起動の手順（<see cref="App"/>。コンテナの構築より前の -driverinstall の分岐を含む）と、実行時の切り替え
    /// （<see cref="IUiThemeAndLanguage"/> の契約）の両方が使う。表示言語の名前は、設定の持ち主（<see cref="BackingStore"/>。
    /// コンテナの Singleton と、起動前のブートストラップは同じインスタンス）へ書く。
    /// </summary>
    public sealed class AppThemeAndCultureService : IUiThemeAndLanguage
    {
        private static readonly Dictionary<AppThemeChoice, string> ThemeLocs = new Dictionary<AppThemeChoice, string>()
        {
            [AppThemeChoice.Default] = "UI/Themes/DefaultTheme.xaml",
            [AppThemeChoice.Light] = "UI/Themes/DefaultTheme.xaml",
            [AppThemeChoice.Dark] = "UI/Themes/DarkTheme.xaml",
        };

        private readonly BackingStore _store;

        public AppThemeAndCultureService(BackingStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public event EventHandler ThemeChanged;

        /// <summary>
        /// 表示言語を設定の持ち主と、スレッドと WPFLocalizeExtension へ反映する（空のカルチャ名は無視する）。
        /// </summary>
        public void ApplyLanguageSetting(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
                return;

            try
            {
                try
                {
                    // Ensure Logs folder exists under application base directory so initial NLog writes succeed
                    string baseLogs = Path.Combine(AppContext.BaseDirectory, "Logs");
                    if (!Directory.Exists(baseLogs)) Directory.CreateDirectory(baseLogs);
                }
                catch { }
                AppLogger.LogDebug($"ApplyLanguageSetting requested: {cultureCode}");
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureCode);
                // コンテナの構築より前（-driverinstall の分岐）からも呼ばれるため、DI の解決に頼らず、
                // 受け取った BackingStore（コンテナの Singleton と同一）へ書く。
                _store.useLang = cultureCode;
                UiCulture.Set(cultureCode);

                // Ensure thread and default thread cultures are set so UI localization applies
                try
                {
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
                catch { }

                // Enable WPFLocalizeExtension to apply culture to current thread resources
                try { LocalizeDictionary.Instance.SetCurrentThreadCulture = true; } catch { }
                LocalizeDictionary.Instance.Culture = culture;

                AppLogger.LogDebug($"ApplyLanguageSetting applied: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            }
            catch
            {
                // Skip if culture cannot be set
            }
        }

        /// <summary><see cref="IUiThemeAndLanguage.ApplyLanguage"/>。空文字は、既定の英語 UI（インバリアントカルチャ）にする。</summary>
        public void ApplyLanguage(string cultureCode)
        {
            // WPF のアプリが無いとき（単体テスト）は何もしない
            if (Application.Current == null)
                return;

            if (string.IsNullOrEmpty(cultureCode))
            {
                // 「No (English UI)」（カルチャ名が空）の選択。ApplyLanguageSetting は空文字を無視して何も変えないため、
                // 起動時に UseLang が空のときと同じ処理（SetUICulture。空文字はインバリアントカルチャ＝既定の英語 UI）を
                // 実行時にも行う（これがないと、再起動するまで UI が切り替わらない）。UseLang は呼び出し元が設定済み。
                SetUICulture(string.Empty);
                return;
            }

            ApplyLanguageSetting(cultureCode);
        }

        public void SetUICulture(string culture)
        {
            try
            {
                AppLogger.LogDebug($"SetUICulture requested: {culture}");
                CultureInfo ci = CultureInfo.GetCultureInfo(culture);
                LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
                LocalizeDictionary.Instance.Culture = ci;
                // fixes the culture in threads
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                DS4Windows.Properties.Resources.Culture = ci;
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                AppLogger.LogDebug($"SetUICulture applied: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");
            }
            catch (CultureNotFoundException) { /* Skip setting culture that we cannot set */ }
        }

        public void ChangeTheme(AppThemeChoice themeChoice, bool fireChanged = true)
        {
            // WPF のアプリが無いとき（単体テスト）は何もしない
            if (Application.Current == null)
                return;

            if (themeChoice == AppThemeChoice.Default)
            {
                Application.Current.Resources.MergedDictionaries.Clear();

                // 4.0.1: Default is the light theme (it used to follow the Windows apps theme mode, which made the
                // dark theme the default for many users). Choose Dark explicitly to use the dark theme.
                ThemeLocs.TryGetValue(AppThemeChoice.Light, out string loc);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(loc, uriKind: UriKind.Relative) });

                if (fireChanged)
                {
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (ThemeLocs.TryGetValue(themeChoice, out string loc))
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(loc, uriKind: UriKind.Relative) });

                if (fireChanged)
                {
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
