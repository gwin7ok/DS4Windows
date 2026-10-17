using System;
using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W4（Q7＝A）: アプリ層の静的な窓口（イベント・表示言語・自動プロファイルの作成・更新確認のスキップ判定）を
    /// 持ち主の型へ移したことの回帰ガード。
    /// </summary>
    public class Phase7Step10dW4AppLayerStaticsGuardTests
    {
        [Fact]
        public void Global_NoLongerDeclaresTheMovedMembers()
        {
            // Phase7-Step10d W5: Global 型ごと削除した（Phase7Step10dW5GlobalRemovedGuardTests が型の不在を確認する）
            Assert.Null(typeof(BackingStore).Assembly.GetType("DS4Windows.Global"));
        }

        [Fact]
        public void DeviceStateService_RaisesItsOwnSerialChange_AndIsolatesInstances()
        {
            var a = TestRepositoryFactory.CreateDeviceStateService();
            var b = TestRepositoryFactory.CreateDeviceStateService();
            int firedA = 0, firedB = 0;
            a.DeviceSerialChange += (s, e) => firedA++;
            b.DeviceSerialChange += (s, e) => firedB++;

            a.OnDeviceSerialChange(this, 1, "AA");

            Assert.Equal(1, firedA);
            Assert.Equal(0, firedB);
        }

        [Fact]
        public void Changelog_TakesTheSkippedVersionAsAnArgument()
        {
            var method = typeof(Changelog).GetMethod("CheckNewerVersionExists");
            Assert.NotNull(method);
            Assert.Equal(new[] { "version", "lastVersionChecked", "allowCached", "respectSkip" },
                method.GetParameters().Select(p => p.Name).ToArray());
        }

        [Fact]
        public void UiCulture_IgnoresACultureNameThatCannotBeSet()
        {
            var original = System.Globalization.CultureInfo.DefaultThreadCurrentUICulture;
            try
            {
                UiCulture.Set(null); // 例外は握りつぶし、カルチャは変えない（旧 Global.SetCulture と同じ）
                Assert.Equal(original, System.Globalization.CultureInfo.DefaultThreadCurrentUICulture);
            }
            finally
            {
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = original;
            }
        }
    }
}
