using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// Phase14b-Step16（決定 D11）: 画面がアプリ全体のテーマと表示言語を切り替える窓口。
    /// 実装は Composition の <c>AppThemeLanguageAdapter</c>（アプリの起点 <c>App</c> へ委譲する）。
    /// 旧: UI（<c>MainWindow</c>、<c>LanguagePackControl</c>、<c>ImageLocationPaths</c>）が <c>Application.Current as App</c> で <c>App</c> を直接呼んでいた。
    /// </summary>
    public interface IUiThemeAndLanguage
    {
        /// <summary>テーマを切り替える（旧 <c>App.ChangeTheme</c>）。<paramref name="fireChanged"/> が true なら <see cref="ThemeChanged"/> を発火する。</summary>
        void ChangeTheme(AppThemeChoice themeChoice, bool fireChanged = true);

        /// <summary>表示言語を切り替える（旧 <c>App.ApplyLanguageSettingPublic</c>）。空文字は既定の英語 UI。</summary>
        void ApplyLanguage(string cultureCode);

        /// <summary>テーマが切り替わった（旧 <c>App.ThemeChanged</c>）。</summary>
        event EventHandler ThemeChanged;
    }
}
