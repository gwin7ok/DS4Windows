using DS4Windows.Processing.Abstractions;
using System;
using DS4Windows.Composition;
using DS4Windows.Core.Settings;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step16（決定 D11）: UI が <c>App</c> を直接呼ばず、<see cref="IUiThemeAndLanguage"/>（実装は UI/Services の <see cref="AppThemeAndCultureService"/>）を使う。
    /// </summary>
    public class UiThemeAndLanguageTests
    {
        [Fact]
        public void TheContainer_ResolvesTheAdapterAsASingleton()
        {
            var first = TestHost.GetService<IUiThemeAndLanguage>();
            Assert.IsType<AppThemeAndCultureService>(first);
            Assert.Same(first, TestHost.GetService<IUiThemeAndLanguage>());
        }

        [Fact]
        public void TheContainer_ResolvesTheIpcResultWriterAsASingleton()
        {
            // Phase14b-Step16: CLI の問い合わせの結果の書き込み（旧 MainWindow → (Application.Current as App).WriteIPCResultDataMMF）
            var first = TestHost.GetService<IIpcResultWriter>();
            Assert.IsType<IpcResultWriterAdapter>(first);
            Assert.Same(first, TestHost.GetService<IIpcResultWriter>());
        }

        [Fact]
        public void WithoutTheApp_TheAdapterDoesNothing_AndDoesNotThrow()
        {
            // テストでは Application.Current（App）が無い。旧呼び出し（Application.Current as App）が null のときと同じく何もしない
            var adapter = new AppThemeAndCultureService(new BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            EventHandler handler = (s, e) => { };

            // 他のテストが WPF の Application を作っている場合は、実際に切り替わってしまうため、呼ばない
            if (System.Windows.Application.Current == null)
            {
                adapter.ChangeTheme(AppThemeChoice.Dark);
                adapter.ApplyLanguage("ja");
            }
            adapter.ThemeChanged += handler;
            adapter.ThemeChanged -= handler;
        }

        [Fact]
        public void ImageLocationPaths_RaisesItsChangedEvents_WhenTheAttachedSourceChangesTheTheme()
        {
            var source = new FakeThemeSource();
            var paths = new ImageLocationPaths();
            int raised = 0;
            paths.NewProfileChanged += (s, e) => raised++;
            paths.ImportProfileChanged += (s, e) => raised++;

            paths.AttachThemeSource(source);
            source.RaiseThemeChanged();

            Assert.Equal(2, raised);
            Assert.Throws<ArgumentNullException>(() => paths.AttachThemeSource(null));
        }

        private sealed class FakeThemeSource : IUiThemeAndLanguage
        {
            public event EventHandler ThemeChanged;
            public void ChangeTheme(AppThemeChoice themeChoice, bool fireChanged = true) { }
            public void ApplyLanguage(string cultureCode) { }
            public void RaiseThemeChanged() => ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
