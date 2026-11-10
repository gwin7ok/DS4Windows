using DS4Windows.Processing.Touchpad;
using System;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step2（R-04）: <see cref="TouchButtonActivationMode"/> を <c>Mouse</c> のネスト型から <c>Core/Settings</c> のトップレベルの型へ移した。
    /// 型の形（名前空間、基底型、値）と、プロファイルの保存・読込での値の名前が変わらないことを検査する。
    /// </summary>
    [Xunit.Collection(SharedHostStateCollection.Name)]
    public class TouchButtonActivationModeTests
    {
        [Fact]
        public void Type_IsTopLevelInCoreSettings_WithTheSameValues()
        {
            Type type = typeof(TouchButtonActivationMode);

            Assert.False(type.IsNested);
            Assert.Equal("DS4Windows.Core.Settings", type.Namespace);
            Assert.Equal(typeof(ushort), Enum.GetUnderlyingType(type));
            Assert.Equal(new[] { "Click", "Touch", "Release" }, Enum.GetNames(type));
            Assert.Equal((ushort)0, (ushort)TouchButtonActivationMode.Click);
            Assert.Equal((ushort)1, (ushort)TouchButtonActivationMode.Touch);
            Assert.Equal((ushort)2, (ushort)TouchButtonActivationMode.Release);
        }

        [Fact]
        public void Mouse_NoLongerDeclaresTheNestedType()
        {
            Assert.Null(typeof(TouchpadMouse).GetNestedType("TouchButtonActivationMode"));
        }

        [Theory]
        [InlineData(TouchButtonActivationMode.Click)]
        [InlineData(TouchButtonActivationMode.Touch)]
        [InlineData(TouchButtonActivationMode.Release)]
        public void Profile_SavesTheValueName_AndLoadsItBack(TouchButtonActivationMode mode)
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.touchpadButtonMode[PersistenceHarness.Slot] = mode;

                string xml = PersistenceHarness.SaveProfile(ctx, store, "Phase14bTouchButtonMode");
                Assert.Contains($"<TouchpadButtonMode>{mode}</TouchpadButtonMode>", xml);

                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "Phase14bTouchButtonMode");
                Assert.Equal(mode, loaded.touchpadButtonMode[PersistenceHarness.Slot]);
            }
        }
    }
}
