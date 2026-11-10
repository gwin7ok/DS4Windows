using DS4Windows.Processing.Gyro;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W4: スティック・ジャイロ・トリガーの処理状態を <c>StickMotionState</c> へ切り出したことと、
    /// 外から使われる入れ子の型（<c>PostMapStickData</c> ほか）をトップレベルへ出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2・Q3・Q5）。
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

            Assert.Equal(count, state.GyroStickX.Length);
            Assert.Equal(count, state.GyroStickY.Length);
            Assert.All(state.GyroStickX, v => Assert.Equal(128, v));
            Assert.All(state.GyroStickY, v => Assert.Equal(128, v));

            Assert.Equal(count, state.MapStickActionData.Length);
            Assert.All(state.MapStickActionData, d => Assert.NotNull(d));
            Assert.Equal(count, state.FlickMappingData.Length);
            Assert.All(state.FlickMappingData, d => Assert.NotNull(d));
            Assert.Equal(count, state.L2TwoStageMappingData.Length);
            Assert.Equal(count, state.R2TwoStageMappingData.Length);
            Assert.All(state.L2TwoStageMappingData, d => Assert.NotNull(d));
            Assert.All(state.R2TwoStageMappingData, d => Assert.NotNull(d));
            Assert.Equal(count, state.DeltaAccelProcessors.Length);
            Assert.All(state.DeltaAccelProcessors, g => { Assert.NotNull(g.LSProcessor); Assert.NotNull(g.RSProcessor); });

            // L2 と R2 は別の instance（旧: 別々の配列）
            Assert.NotSame(state.L2TwoStageMappingData[0], state.R2TwoStageMappingData[0]);
        }

        // Phase7-Step8 W2: 転送プロパティは削除した。Mapping のコンストラクターが、受け取った保持クラスの配列を、同じ参照のまま readonly で持つこと
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

            Assert.Same(state.GyroStickX, Field("gyroStickX"));
            Assert.Same(state.GyroStickY, Field("gyroStickY"));
            Assert.Same(state.MapStickActionData, Field("mapStickActionData"));
            Assert.Same(state.FlickMappingData, Field("flickMappingData"));
            Assert.Same(state.L2TwoStageMappingData, Field("l2TwoStageMappingData"));
            Assert.Same(state.R2TwoStageMappingData, Field("r2TwoStageMappingData"));
            Assert.Same(state.DeltaAccelProcessors, Field("deltaAccelProcessors"));
        }

        [Fact]
        public void Mapping_HoldsNoStickMotionStateOfItsOwn_AndNoNestedStateTypes()
        {
            // Phase14d-Step3: ソースの文字列ではなく、Mapping のフィールド（readonly と型）と入れ子の型を確かめる（配列が保持クラスと同じ参照であることは MappingCopies_ReferenceTheSameArraysAsTheHolder）
            Type mapping = typeof(DS4Windows.Processing.Mapping);
            foreach ((string field, Type type) in new[]
            {
                ("gyroStickX", typeof(byte[])),
                ("mapStickActionData", typeof(DS4Windows.Processing.PostMapStickData[])),
                ("flickMappingData", typeof(DS4Windows.Core.Settings.FlickStickMappingData[])),
                ("deltaAccelProcessors", typeof(DS4Windows.Processing.Gyro.DeltaSettingsProcessorGroup[])),
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
            // Phase14d-Step3: 登録の文字列ではなく、コンテナが同じインスタンスを返すことを確かめる
            var first = TestHost.GetService<DS4Windows.Processing.StickMotionState>();
            Assert.NotNull(first);
            Assert.Same(first, TestHost.GetService<DS4Windows.Processing.StickMotionState>());
        }
    }
}
