using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W1: <c>Mouse</c>／<c>ControlService</c> が保持クラスをコンストラクターで受けること、
    /// <c>MapClick</c>／<c>ResetRuntimeStateForKey</c> を <c>SyntheticInputState</c> へ移したこと、
    /// 本体の呼び出しが 0 件だった <c>Mapping</c> の 4 メンバーを削除したことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q5・Q7・Q9）。
    /// </summary>
    public class Phase7Step8W1StateHolderInjectionGuardTests
    {
        private const ushort Key = 0x4B;

        private readonly ITestOutputHelper _output;

        public Phase7Step8W1StateHolderInjectionGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CountClick_CountsEachKindOnTheGivenDevice_AndIgnoresNone()
        {
            var state = new SyntheticInputState();
            state.CountClick(2, Click.Left);
            state.CountClick(2, Click.Left);
            state.CountClick(2, Click.Right);
            state.CountClick(2, Click.WUP);
            state.CountClick(2, Click.None);

            Assert.Equal(2, state.DeviceState[2].currentClicks.leftCount);
            Assert.Equal(1, state.DeviceState[2].currentClicks.rightCount);
            Assert.Equal(1, state.DeviceState[2].currentClicks.wUpCount);
            Assert.Equal(0, state.DeviceState[2].currentClicks.middleCount);
            Assert.Equal(0, state.DeviceState[3].currentClicks.leftCount);
        }

        [Fact]
        public void ResetRuntimeStateForKey_RemovesOnlyThatKeyFromEveryDevice()
        {
            var state = new SyntheticInputState();
            for (int d = 0; d < state.DeviceState.Length; d++)
            {
                state.DeviceState[d].keyPresses[Key] = new SyntheticState.KeyPresses();
                state.DeviceState[d].keyPresses[0x4C] = new SyntheticState.KeyPresses();
                state.DeviceState[d].nativeKeyAlias[Key] = 1;
            }

            state.ResetRuntimeStateForKey(Key, TestActions.Manager);

            for (int d = 0; d < state.DeviceState.Length; d++)
            {
                Assert.False(state.DeviceState[d].keyPresses.ContainsKey(Key));
                Assert.False(state.DeviceState[d].nativeKeyAlias.ContainsKey(Key));
                Assert.True(state.DeviceState[d].keyPresses.ContainsKey(0x4C));
            }
        }

        [Fact]
        public void Mapping_NoLongerHasTheMovedOrDeadMembers()
        {
            string mapping = SourceText.ReadCode("DS4Windows/Processing/Mapping.cs");
            if (mapping == null) return;

            foreach (string name in new[]
            {
                "MapClick", "ResetRuntimeStateForKey", "DispatchProfileActionEdge",
                "TakePendingRestoreProfileName", "ApplyProfileDirect", "RestoreProfileDirect",
            })
            {
                Assert.DoesNotMatch(@"\b" + name + @"\b", mapping);
            }
        }

        [Fact]
        public void MouseAndControlService_ReceiveTheHoldersByConstructor()
        {
            string mouse = SourceText.ReadCode("DS4Windows/Processing/Mouse.cs");
            string control = SourceText.ReadCode("DS4Windows/Processing/ControlService.cs");
            string registration = RegistrationSource.ReadAll(stripComments: true);   // Phase14c-Step6（R-18）: 層ごとの partial ファイルをつないだもの（コメントは除く）
            if (mouse == null || control == null || registration == null) return;

            Assert.DoesNotContain("StickMotionState.Shared", mouse);
            Assert.DoesNotContain("SyntheticInputState.Shared", mouse);
            Assert.DoesNotMatch(@"\bMapping\.", mouse);
            Assert.DoesNotContain("StickMotionState.Shared", control);
            Assert.DoesNotContain("SyntheticInputState.Shared", control);
            Assert.Contains("sp.GetRequiredService<StickMotionState>()", registration);
            // Phase14c-Step3（R-06）: 送出役（SyntheticDispatcherService）は登録ラムダではなく、コンテナがコンストラクターで保持クラスを渡す形にした
            Assert.Contains("services.AddSingleton<ISyntheticDispatcher, SyntheticDispatcherService>();", registration);
            Assert.Contains(typeof(DS4Windows.Processing.SyntheticInputState), System.Linq.Enumerable.Select(typeof(DS4Windows.Processing.Services.SyntheticDispatcherService).GetConstructors()[0].GetParameters(), p => p.ParameterType));
        }

        [Fact]
        public void SpecialActionRepository_ResetsTheKeyStateAfterGlobalSaveAction_AndGlobalHasNoMappingReference()
        {
            string repository = SourceText.ReadCode("DS4Windows/Core/Persistence/SpecialActionRepository.cs");
            if (repository == null) return;

            int save = repository.IndexOf("SaveActionAndReload(", System.StringComparison.Ordinal);
            // Phase14b-Step14（D14）: リセットは契約 ISpecialActionRuntimeReset 経由（旧: _syntheticInput.ResetRuntimeStateForKey）。順序の検査は同じ
            int reset = repository.IndexOf("_runtimeReset.ResetRuntimeStateForKey(", System.StringComparison.Ordinal);
            Assert.True(save >= 0 && reset > save, "SaveActionAndReload（旧 Global.SaveAction）の後に ResetRuntimeStateForKey を呼ぶこと");
            Assert.Contains("mode == 4", repository);

        }
    }
}
