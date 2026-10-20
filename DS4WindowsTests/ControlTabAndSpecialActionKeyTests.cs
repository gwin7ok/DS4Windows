using System;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class ControlTabAndSpecialActionKeyTests
    {
        [Fact]
        public void SpecialAction_PressMode_KeyRepeat_IntegrationTest()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Press_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30"; // 0x1E ('A')

            var adapter = new KeyButtonActionControllerAdapter(0, sa);
            var binding = new KeyActionBinding(sa);

            var triggerDown = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 押下エッジ (Start)
            adapter.Start(binding, triggerDown);
            var state = ActionManager.GetStateFor(sa, 0);
            Assert.NotNull(state);

            // Phase8-Step1 W3（B-5）: Press は押下で 1 回だけ押し、まだ離していない（保持中）。トグル状態は使わない
            Assert.Equal(new uint[] { 0x1E }, mockKbm.KeyPressCalls.ToArray());
            Assert.Empty(mockKbm.KeyReleaseCalls);
            Assert.False(state.IsToggledOn);

            var triggerUp = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = false,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 解放エッジ (Handle: IsEdgeEstablished = false)
            adapter.Handle(binding, triggerUp);

            // 離すと、押していたキーを 1 回だけ解放する（長押しのリピートは、初回遅延の前に離したので始まっていない）
            Assert.Equal(new uint[] { 0x1E }, mockKbm.KeyReleaseCalls.ToArray());
            Assert.Equal(new uint[] { 0x1E }, mockKbm.KeyPressCalls.ToArray());
            Assert.False(state.IsToggledOn);
        }

        [Fact]
        public void SpecialAction_ToggleMode_TogglesStateCorrectly()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("Key_Toggle_Test", "Cross", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30";

            var inner = new KeyButtonActionController(0, KeyButtonActionController.Mode.Toggle, "Key_Toggle_Test");
            var adapter = new KeyButtonActionControllerAdapter(inner);
            var binding = new KeyActionBinding(sa);

            var triggerDown = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = true,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 1回目の押下 (Start)
            adapter.Start(binding, triggerDown);

            var triggerUp = new TriggerContextImpl
            {
                Device = 0,
                IsEdgeEstablished = false,
                LogicalValue = 0x1E,
                NativeValue = 0x1E,
                OutputHandler = mockKbm,
                Timestamp = DateTime.UtcNow
            };

            // 1回目の解放 (Handle) -> トグル維持
            adapter.Handle(binding, triggerUp);

            // 2回目の押下/停止 (Stop) -> トグル解除
            adapter.Stop(binding, triggerDown);

            // Phase8-Step1 W3（B-5）: トグル OFF で、リピートを止めて、押していたキーを解放する（リピーターは非同期に押下を送るため、押下の回数は固定しない）
            Assert.Contains((uint)0x1E, mockKbm.KeyReleaseCalls.ToArray());
        }

    }
}
