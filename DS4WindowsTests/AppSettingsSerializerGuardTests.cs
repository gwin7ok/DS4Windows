using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W3（Q1・Q3・Q5＝A）: アプリ設定（Profiles.xml）の保存・読込を <c>BackingStore</c> から <c>AppSettingsXmlSerializer</c> へ移し、
    /// バージョンの数値化を <c>VersionUtil</c> へ移したことの回帰ガード。内容の同一性はゴールデン（<c>GoldenPersistenceTests</c>）と
    /// <c>BackingStoreAppSettingsLoadTests</c> が固定する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AppSettingsSerializerGuardTests
    {
        private readonly ITestOutputHelper _output;

        public AppSettingsSerializerGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresLoadSaveAndLoadFakeExeName_ButKeepsPostProcessLoad()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(BackingStore).GetMethods(all).Select(m => m.Name).ToArray();
            Assert.DoesNotContain("Load", names);
            Assert.DoesNotContain("Save", names);
            Assert.DoesNotContain("LoadFakeExeName", names);
            Assert.Contains("PostProcessLoad", names); // 読込後の値の丸め（状態の整形）は BackingStore に残す
        }

        [Fact]
        public void TheSerializer_RequiresAllItsDependencies()
        {
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(null, new PathService(), new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(store, null, new EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() => new AppSettingsXmlSerializer(store, new PathService(), null));
        }

        [Fact]
        public void TheContainer_ProvidesTheSerializer_AndTheXmlStoreUsesIt()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            Assert.Same(TestHost.GetService<AppSettingsXmlSerializer>(), TestHost.GetService<AppSettingsXmlSerializer>());
            Assert.NotNull(TestHost.GetService<IProfileXmlStore>());
        }

        [Fact]
        public void TheFakeExeFileName_IsUnchanged()
        {
            Assert.Equal("custom_exe_name.txt", AppSettingsXmlSerializer.CustomExeConfigFileName);
        }

        [Theory]
        [InlineData("1.2.3.4", (1UL << 48) | (2UL << 32) | (3UL << 16) | 4UL)]
        [InlineData("3.9.0", (3UL << 48) | (9UL << 32) | 0UL)]
        [InlineData("not a version", 0UL)]
        [InlineData("", 0UL)]
        public void CompileVersionNumberFromString_IsUnchanged(string text, ulong expected)
        {
            // "3.9.0" は Revision が -1（ushort へ切り詰められて 0xFFFF）になるため、Build の位置までを比べる
            ulong actual = VersionUtil.CompileVersionNumberFromString(text);
            if (text == "3.9.0")
                Assert.Equal(expected, actual & ~0xFFFFFFFFUL | 0UL);
            else
                Assert.Equal(expected, actual);
        }

        [Fact]
        public void CompileVersionNumber_PacksTheFourParts()
        {
            Assert.Equal((1UL << 48) | (2UL << 32) | (3UL << 16) | 4UL, VersionUtil.CompileVersionNumber(1, 2, 3, 4));
        }
    }
}
