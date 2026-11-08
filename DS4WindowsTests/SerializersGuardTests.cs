using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W2（Q1・Q3＝A）: Actions.xml・LinkedProfiles.xml・ControllerConfigs.xml の保存・読込を
    /// <c>BackingStore</c> から 3 つのシリアライザーへ移したことの回帰ガード。
    /// 内容（保存結果と読込後の状態）の同一性は <c>GoldenPersistenceTests</c> が固定する。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class SerializersGuardTests
    {
        private static readonly string[] MovedMethods =
        {
            "CreateAction", "PrepareActionsXml", "SaveActions", "SaveActionNew", "SaveAction", "RemoveAction", "LoadActions",
            "createLinkedProfiles", "LoadLinkedProfiles", "SaveLinkedProfiles",
            "createControllerConfigs", "LoadControllerConfigsForDevice", "SaveControllerConfigsForDevice",
        };

        private readonly ITestOutputHelper _output;

        public SerializersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_NoLongerDeclaresTheMovedMethods()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var remaining = typeof(BackingStore).GetMethods(all).Select(m => m.Name).Where(n => MovedMethods.Contains(n)).ToArray();
            Assert.True(remaining.Length == 0, "BackingStore に移動済みのメソッドが残っている: " + string.Join(", ", remaining));
        }

        [Fact]
        public void TheSerializers_RequireTheBackingStore()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActionsXmlSerializer(null, TestActions.RuntimeReset));
            Assert.Throws<ArgumentNullException>(() => new LinkedProfilesXmlSerializer(null));
            Assert.Throws<ArgumentNullException>(() => new ControllerConfigsXmlSerializer(null));
        }

        [Fact]
        public void TheContainer_ProvidesSingletonSerializers_ToTheServices()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            Assert.Same(TestHost.GetService<SpecialActionsXmlSerializer>(), TestHost.GetService<SpecialActionsXmlSerializer>());
            Assert.NotNull(TestHost.GetService<LinkedProfilesXmlSerializer>());
            Assert.NotNull(TestHost.GetService<ControllerConfigsXmlSerializer>());
            Assert.NotNull(TestHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(TestHost.GetService<IProfileRepository>());
            Assert.NotNull(TestHost.GetService<IProfileXmlStore>());
        }
    }
}
