using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step9 W1（Q1＝A）: 互換 <c>DS4Windows.AppHost</c>（<c>DI/Compat/AppHost.cs</c>）を削除したことの回帰ガード。
    /// 正本は <c>DS4Windows.Composition.AppHost</c> だけ。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class CompatAppHostRemovedGuardTests
    {
        private readonly ITestOutputHelper _output;

        public CompatAppHostRemovedGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NoAppHostType_ExistsInTheDS4WindowsNamespace()
        {
            var type = typeof(DS4Windows.Core.Settings.BackingStore).Assembly.GetType("DS4Windows.AppHost");
            Assert.Null(type);
            Assert.NotNull(typeof(DS4Windows.Core.Settings.BackingStore).Assembly.GetType("DS4Windows.Composition.AppHost"));
        }
    }
}
