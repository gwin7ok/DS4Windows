using System;
using System.Windows;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.Composition
{
    /// <summary>
    /// Phase14b-Step16（決定 D11）: <see cref="IUiThemeAndLanguage"/> の実装（DI の Singleton）。アプリの起点 <see cref="App"/> へ委譲する。
    /// 組み立て（Composition）だけが <see cref="App"/> を知る。<see cref="App"/> は呼び出しの時点で <see cref="Application.Current"/> から取る
    /// （コンテナの構築より前に App が作られ、テストでは App が無い。旧呼び出しも同じく <c>Application.Current as App</c> だった）。
    /// </summary>
    public sealed class AppThemeLanguageAdapter : IUiThemeAndLanguage
    {
        private static App CurrentApp => Application.Current as App;

        public void ChangeTheme(AppThemeChoice themeChoice, bool fireChanged = true)
            => CurrentApp?.ChangeTheme(themeChoice, fireChanged);

        public void ApplyLanguage(string cultureCode)
            => CurrentApp?.ApplyLanguageSettingPublic(cultureCode);

        public event EventHandler ThemeChanged
        {
            add { var app = CurrentApp; if (app != null) app.ThemeChanged += value; }
            remove { var app = CurrentApp; if (app != null) app.ThemeChanged -= value; }
        }
    }
}
