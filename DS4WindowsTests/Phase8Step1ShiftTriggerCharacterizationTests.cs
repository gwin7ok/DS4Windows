using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W2（Phase8-Step1-Plan.md 項目 B-3）: Controls タブの「シフト」（<c>shiftTrigger</c>。副ボタンを押している間だけ、別の割り当てに切り替わる）の特性化テスト。
    /// シフトトリガーの番号は <c>Mapping.shiftTriggerMapping</c> の添字（12＝L1）。
    /// 固定する挙動: シフトのボタンを押していないときは通常の割り当て、押している間はシフトの割り当てが使われる。シフトの割り当てが既定（未設定）なら、シフトトリガーを押しても通常のまま。
    /// 台帳: <c>Phase8-Step1-Trigger-Spec-Ledger.md</c>（Controls タブの割り当ては Phase8 の統合ディスパッチの対象）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step1ShiftTriggerCharacterizationTests
    {
        private const int ShiftTriggerL1 = 12; // Mapping.shiftTriggerMapping[12] == DS4Controls.L1

        private static DS4ControlSettings ConfigureCrossToCircle(MappingCharacterizationScope scope)
        {
            scope.RemapToButton(DS4Controls.Cross, X360Controls.B); // 通常: Cross → B（DS4 の Circle）
            return TestGlobals.Store.GetDS4CSetting(MappingCharacterizationScope.Device, DS4Controls.Cross);
        }

        [Fact]
        public void Shift_NotHeld_UsesTheRegularAssignment()
        {
            using var scope = new MappingCharacterizationScope();
            DS4ControlSettings dcs = ConfigureCrossToCircle(scope);
            dcs.shiftActionType = DS4ControlSettings.ActionType.Button;
            dcs.shiftAction.actionBtn = X360Controls.X; // シフト: Cross → X（DS4 の Square）
            dcs.shiftTrigger = ShiftTriggerL1;

            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Cross = true;
            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Circle);
            Assert.False(output.Square);
        }

        [Fact]
        public void Shift_Held_UsesTheShiftAssignment()
        {
            using var scope = new MappingCharacterizationScope();
            DS4ControlSettings dcs = ConfigureCrossToCircle(scope);
            dcs.shiftActionType = DS4ControlSettings.ActionType.Button;
            dcs.shiftAction.actionBtn = X360Controls.X;
            dcs.shiftTrigger = ShiftTriggerL1;

            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Cross = true;
            input.L1 = true; // シフトのボタンを押している
            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Square);
            Assert.False(output.Circle);
        }

        [Fact]
        public void Shift_WithTheShiftAssignmentLeftDefault_KeepsTheRegularAssignmentEvenWhenHeld()
        {
            using var scope = new MappingCharacterizationScope();
            DS4ControlSettings dcs = ConfigureCrossToCircle(scope);
            dcs.shiftTrigger = ShiftTriggerL1; // shiftActionType は既定（Default）のまま

            DS4State input = MappingCharacterizationScope.NeutralState();
            input.Cross = true;
            input.L1 = true;
            DS4State output = scope.RunMapCustom(input);

            Assert.True(output.Circle);
            Assert.False(output.Square);
        }
    }
}
