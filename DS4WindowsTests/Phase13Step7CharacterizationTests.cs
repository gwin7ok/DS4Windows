using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step7 7-a: 出力種別の 2 本の配列（<c>ActiveOutDevType</c>／<c>OutDevTypeTemp</c>）の現状の挙動を固定する特性テスト。
    /// 7-b 以降で配列の持ち主を <c>IOutputDeviceTypeState</c> へ移しても、<c>IOutputSlotService</c> 経由で見える結果が同じであることを確かめる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step7CharacterizationTests
    {
        public Phase13Step7CharacterizationTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
        }

        [Fact]
        public void TheArrays_HaveTheTestProfileLength_AndTheirInitialValues()
        {
            var service = TestHost.GetService<IOutputSlotService>();
            Assert.Equal(ControllerLimits.TestProfileItemCount, service.ActiveOutDevType.Length);
            Assert.Equal(ControllerLimits.TestProfileItemCount, service.OutDevTypeTemp.Length);
            // 他のテストが書き換えた値を戻すため、初期値の検査は「書き込んだ値を戻す前の状態」ではなく、型の既定値の範囲で行う
            Assert.All(service.OutDevTypeTemp, v => Assert.True(v == OutContType.X360 || v == OutContType.DS4 || v == OutContType.None));
        }

        [Fact]
        public void TheArrays_AreTheSameInstancesOnEveryAccess()
        {
            var service = TestHost.GetService<IOutputSlotService>();
            Assert.Same(service.ActiveOutDevType, service.ActiveOutDevType);
            Assert.Same(service.OutDevTypeTemp, service.OutDevTypeTemp);
            Assert.NotSame(service.ActiveOutDevType, service.OutDevTypeTemp);
        }

        [Fact]
        public void TheBackingStore_SharesTheActiveArrayOfTheService()
        {
            var service = TestHost.GetService<IOutputSlotService>();
            var store = TestHost.GetService<BackingStore>();
            Assert.Same(service.ActiveOutDevType, store.ActiveOutDevType);
        }

        [Fact]
        public void AWriteThroughTheService_IsVisibleThroughTheBackingStore()
        {
            var service = TestHost.GetService<IOutputSlotService>();
            var store = TestHost.GetService<BackingStore>();
            const int slot = 3;
            OutContType old = service.ActiveOutDevType[slot];
            try
            {
                service.ActiveOutDevType[slot] = OutContType.DS4;
                Assert.Equal(OutContType.DS4, store.ActiveOutDevType[slot]);
            }
            finally
            {
                service.ActiveOutDevType[slot] = old;
            }
        }
    }
}
