using System.Linq;
using System;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 合成入力の状態（globalState／deviceState／syncStateLock）を <c>SyntheticInputState</c> へ切り出したことと、
    /// <c>BackingStore</c> から <c>Mapping</c> への逆参照を <c>ActionsLoaded</c> イベントへ置き換えたことの回帰ガード。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は docs-forDIMG/MadeByAgent/Archive/Phase7-Step7-Plan.md（Q2〜Q5）。
    /// </summary>
    public class SyntheticInputStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public SyntheticInputStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Mapping_HoldsNoSyntheticStateOfItsOwn_OnlyForwarders()
        {
            // ソースの文字列ではなく、Mapping のフィールド（readonly と型）と、保持クラスと同じ参照であることを確かめる
            Type mapping = typeof(DS4Windows.Processing.Mapping);
            Assert.DoesNotContain("SyntheticState", mapping.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Select(t => t.Name));
            foreach ((string field, Type type) in new[]
            {
                ("_syntheticInput", typeof(DS4Windows.Processing.Synthetic.SyntheticInputState)),
                ("syncStateLock", typeof(System.Threading.ReaderWriterLockSlim)),
                ("deviceState", typeof(DS4Windows.Processing.Synthetic.SyntheticState[])),
            })
            {
                System.Reflection.FieldInfo f = mapping.GetField(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
                Assert.True(f != null, "Mapping に " + field + " がない");
                Assert.Equal(type, f.FieldType);
                Assert.True(f.IsInitOnly, field + " が readonly でない");
            }
            var instance = TestHost.GetService<DS4Windows.Processing.Abstractions.IMapping>();
            var holder = TestHost.GetService<DS4Windows.Processing.Synthetic.SyntheticInputState>();
            Assert.Same(holder.SyncStateLock, mapping.GetField("syncStateLock", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).GetValue(instance));
            Assert.Same(holder.DeviceState, mapping.GetField("deviceState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).GetValue(instance));
        }

        [Fact]
        public void BackingStore_DoesNotReferenceMapping_AndRaisesActionsLoadedInBothPaths()
        {
            string store = SourceText.ReadCodeWithPartials("DS4Windows/Core/Settings/BackingStore.cs");
            if (store == null) return;

            Assert.False(Regex.IsMatch(store, @"\bMapping\."), "BackingStore が Mapping を参照している");
            Assert.Contains("public event Action? ActionsLoaded;", store);
            // Actions.xml の読込（2 経路）は SpecialActionsXmlSerializer へ移した。発行元（イベントとメソッド）は BackingStore のまま
            Assert.Contains("public void RaiseActionsLoaded()", store);
            string actionsXml = SourceText.ReadCode("DS4Windows/Core/Persistence/SpecialActionsXmlSerializer.cs");
            if (actionsXml == null) return;
            Assert.Equal(2, Regex.Matches(actionsXml, @"store\.RaiseActionsLoaded\(\);").Count);
        }

        [Fact]
        public void Registration_SubscribesTheResetOnce_AndRegistersTheSharedState()
        {
            // 登録の文字列ではなく、コンテナが組み立てた BackingStore の購読者が、コンテナの ActionsLoadedRuntimeReset であることを確かめる
            // （購読が 1 つだけになる差し替えの振る舞いは BackingStoreActionsLoadedHandlerTests）
            var store = TestHost.GetService<DS4Windows.Core.Settings.BackingStore>();
            var handler = (Action)typeof(DS4Windows.Core.Settings.BackingStore).GetField("_actionsLoadedHandler", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).GetValue(store);
            Assert.NotNull(handler);
            Assert.Same(TestHost.GetService<DS4Windows.Processing.Services.Slots.ActionsLoadedRuntimeReset>(), handler.Target);
            Assert.Same(TestHost.GetService<DS4Windows.Processing.Synthetic.SyntheticInputState>(), TestHost.GetService<DS4Windows.Processing.Synthetic.SyntheticInputState>());
        }

        [Fact]
        public void ResetHandler_ResetsTheSyntheticState_BeforeClearingTheToggles()
        {
            var synthetic = new DS4Windows.Processing.Synthetic.SyntheticInputState();
            synthetic.GlobalState.currentClicks.leftCount = 3;
            synthetic.DeviceState[0].currentClicks.rightCount = 2;
            var dirtyDevice = synthetic.DeviceState[0];

            var manager = RecordingProxy.Create<DS4Windows.Processing.Actions.IManagedActionManager>();
            int leftWhenToggledCleared = -1;
            var recorder = RecordingProxy.Of(manager);
            recorder.OnCall = name =>
            {
                if (name == "ClearAllToggledOn") leftWhenToggledCleared = synthetic.GlobalState.currentClicks.leftCount;
            };

            new DS4Windows.Processing.Services.Slots.ActionsLoadedRuntimeReset(synthetic, manager).Reset();

            Assert.Equal(0, synthetic.GlobalState.currentClicks.leftCount);
            Assert.Equal(0, synthetic.DeviceState[0].currentClicks.rightCount);
            Assert.NotSame(dirtyDevice, synthetic.DeviceState[0]);   // 新品へ差し替える
            Assert.Equal(0, leftWhenToggledCleared);   // トグル状態の解除の時点では、合成状態はもうリセット済み
            Assert.Single(recorder.Calls, c => c.Name == "ClearAllToggledOn");
        }

        [Fact]
        public void ResetHandler_StillResetsTheSyntheticState_WhenClearingTheTogglesFails()
        {
            var synthetic = new DS4Windows.Processing.Synthetic.SyntheticInputState();
            synthetic.GlobalState.currentClicks.leftCount = 3;
            var manager = RecordingProxy.Create<DS4Windows.Processing.Actions.IManagedActionManager>();
            RecordingProxy.Of(manager).OnCall = name => { if (name == "ClearAllToggledOn") throw new InvalidOperationException("test"); };

            new DS4Windows.Processing.Services.Slots.ActionsLoadedRuntimeReset(synthetic, manager).Reset();   // 例外は外へ出さない

            Assert.Equal(0, synthetic.GlobalState.currentClicks.leftCount);
        }
    }
}
