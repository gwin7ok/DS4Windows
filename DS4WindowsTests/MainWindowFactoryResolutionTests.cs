using System;
using System.Linq;
using DS4Windows.Composition;
using DS4Windows.Core.Utilities;
using DS4Windows.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="MainWindowFactory"/> は <see cref="ActivatorUtilities"/> で窓をつくるため、窓のコンストラクター引数が
    /// （起動引数を除いて）すべてコンテナから解決できることを、窓を作らずに確かめる（窓の生成は UI スレッドが要る）。
    /// </summary>
    public class MainWindowFactoryResolutionTests
    {
        [Fact]
        public void EveryMainWindowDependencyExceptTheParser_ResolvesFromTheHost()
        {
            var ctor = typeof(MainWindow).GetConstructors().Single();
            foreach (var p in ctor.GetParameters().Where(p => p.ParameterType != typeof(ArgumentParser)))
                Assert.True(TestHost.GetService(p.ParameterType) != null, p.ParameterType.Name + " がコンテナから解決できない");
        }
    }
}
