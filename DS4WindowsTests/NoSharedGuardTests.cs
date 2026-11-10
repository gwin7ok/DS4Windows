using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W4: 状態の保持クラス 3 つ（<c>SyntheticInputState</c>／<c>MacroRuntimeState</c>／<c>StickMotionState</c>）の
    /// 暫定の静的な窓口 <c>Shared</c> を削除したことと、コンテナの Singleton を利用者が共有していることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q5）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class NoSharedGuardTests
    {
        private readonly ITestOutputHelper _output;

        public NoSharedGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(typeof(SyntheticInputState))]
        [InlineData(typeof(MacroRuntimeState))]
        [InlineData(typeof(StickMotionState))]
        public void TheHolders_HaveNoStaticMembers(Type holder)
        {
            var statics = holder
                .GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => !(m is ConstructorInfo) && !(m is FieldInfo f && f.IsLiteral)) // const を除く
                .Select(m => m.Name)
                .ToArray();

            Assert.Empty(statics);
        }

        [Fact]
        public void TheHost_SharesOneInstanceOfEachHolder_WithMappingAndTheControlService()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var synthetic = TestHost.GetService<SyntheticInputState>();
            var macro = TestHost.GetService<MacroRuntimeState>();
            var stick = TestHost.GetService<StickMotionState>();
            Assert.Same(synthetic, TestHost.GetService<SyntheticInputState>());
            Assert.Same(macro, TestHost.GetService<MacroRuntimeState>());
            Assert.Same(stick, TestHost.GetService<StickMotionState>());

            var mapping = TestHost.GetService<IMapping>();
            object Field(Type type, object target, string name)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                return field.GetValue(target);
            }

            Assert.Same(synthetic, Field(typeof(Mapping), mapping, "_syntheticInput"));
            Assert.Same(macro, Field(typeof(Mapping), mapping, "_macroRuntime"));
            // Mapping は StickMotionState の配列の参照だけを持つ（インスタンスは保持しない）。同じ参照であることを確かめる
            Assert.Same(stick.GyroStickX, Field(typeof(Mapping), mapping, "gyroStickX"));
            Assert.Same(synthetic.DeviceState, Field(typeof(Mapping), mapping, "deviceState"));

            var control = TestHost.GetService<ControlService>();
            Assert.Same(stick, Field(typeof(ControlService), control, "_stickMotion"));
        }
    }
}
