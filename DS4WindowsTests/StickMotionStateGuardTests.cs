using System.Linq;
using System;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// スティック・ジャイロ・トリガーの処理状態を <c>StickMotionState</c> へ切り出したことと、
    /// 外から使われる入れ子の型（<c>PostMapStickData</c> ほか）をトップレベルへ出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は docs-forDIMG/MadeByAgent/Archive/Phase7-Step7-Plan.md（Q2・Q3・Q5）。
    /// </summary>
    public class StickMotionStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public StickMotionStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NewInstance_HasTheSameInitialValuesAsTheOldStaticFields()
        {
            var state = new StickMotionState();
            int count = DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount;

            Assert.Equal(count, state.Devices.Length);
            Assert.All(state.Devices, d =>
            {
                Assert.NotNull(d.MapStickActionData);
                Assert.NotNull(d.FlickMappingData);
                Assert.NotNull(d.L2TwoStageMappingData);
                Assert.NotNull(d.R2TwoStageMappingData);
                Assert.NotNull(d.DeltaAccelProcessors.LSProcessor);
                Assert.NotNull(d.DeltaAccelProcessors.RSProcessor);
            });

            // L2 と R2 は別の instance
            Assert.NotSame(state.Devices[0].L2TwoStageMappingData, state.Devices[0].R2TwoStageMappingData);
        }

        // 転送プロパティは削除した。Mapping のコンストラクターが、受け取った保持クラスの配列を、同じ参照のまま readonly で持つこと
        [Fact]
        public void MappingCopies_ReferenceTheSameArraysAsTheHolder()
        {
            var state = new StickMotionState();
            var mapper = TestMappingFactory.Create(stickMotion: state);

            object Field(string name)
            {
                var field = typeof(Mapping).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(field);
                return field.GetValue(mapper);
            }

            Assert.Same(state.Devices, Field("_motions"));
        }

        [Fact]
        public void Mapping_HoldsNoStickMotionStateOfItsOwn_AndNoNestedStateTypes()
        {
            // ソースの文字列ではなく、Mapping のフィールド（readonly と型）と入れ子の型を確かめる（配列が保持クラスと同じ参照であることは MappingCopies_ReferenceTheSameArraysAsTheHolder）
            Type mapping = typeof(DS4Windows.Processing.Mapping);
            foreach ((string field, Type type) in new[]
            {
                ("_motions", typeof(DS4Windows.Processing.DeviceStickMotion[])),
            })
            {
                System.Reflection.FieldInfo f = mapping.GetField(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
                Assert.True(f != null, "Mapping に " + field + " がない");
                Assert.Equal(type, f.FieldType);
                Assert.True(f.IsInitOnly, field + " が readonly でない");
            }
            var nested = mapping.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Select(t => t.Name).ToArray();
            foreach (string name in new[] { "PostMapStickData", "FlickStickMappingData", "TwoStageTriggerMappingData", "DeltaSettingsProcessor", "DeltaSettingsProcessorGroup", "Click" })
                Assert.DoesNotContain(name, nested);
        }

        [Fact]
        public void Registration_RegistersTheSharedState()
        {
            // 登録の文字列ではなく、コンテナが同じインスタンスを返すことを確かめる
            var first = TestHost.GetService<DS4Windows.Processing.StickMotionState>();
            Assert.NotNull(first);
            Assert.Same(first, TestHost.GetService<DS4Windows.Processing.StickMotionState>());
        }
    }
}
