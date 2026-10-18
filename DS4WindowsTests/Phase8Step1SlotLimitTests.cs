using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DS4Windows;
using DS4WinWPF.DS4Control;
using DS4WinWPF.DS4Control.DTOXml;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W2b（Phase8-Step1-SlotLimit-Fix-Plan.md）: 「デバイス上限が 4 前提」だった箇所の回帰ガード。
    /// 上限は <see cref="ControllerLimits.MaxControllerCount"/>（8）。
    ///  - A-1: <c>Mapping.nowAction</c>／<c>oldnowAction</c> が 4 要素で、<c>delayTime</c> のアクションがデバイス 4〜7 で例外になっていた（上流の 2015 年から。8 スロット化の更新漏れ）。
    ///  - B-1: <c>OutputSlotPersistDTO.MapTo</c> が出力スロットの添字を 0〜3 に限り、5〜8 番目の恒久スロットが復元されなかった。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step1SlotLimitTests
    {
        // ---- 配列の長さのガード ----

        [Fact]
        public void Mapping_PerDeviceArrays_HaveOneElementPerSlot()
        {
            var mapping = TestMappingFactory.Host() as Mapping;
            Assert.NotNull(mapping);
            string[] perDevice =
            {
                // Phase8-Step4 W4: nowAction／oldnowAction／suppressedTriggerButtons は、_triggerInputs（Delay）とエンジンへ移した
                "_triggerInputs", "_frameOutputs", "oldnowKeyAct", "held", "fadetimer", "prevFadetimer", "lastColor",
                "deviceRuntime", "customMapQueue",
            };

            foreach (string name in perDevice)
            {
                FieldInfo field = typeof(Mapping).GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.True(field != null, $"Mapping.{name} が見つからない");
                var array = field.GetValue(mapping) as Array;
                Assert.True(array != null, $"Mapping.{name} が配列ではない");
                Assert.True(array.Length == ControllerLimits.MaxControllerCount,
                    $"Mapping.{name} の長さが {array.Length}（期待: {ControllerLimits.MaxControllerCount}）");
            }
        }

        [Fact]
        public void Mapping_HasNoInstanceArrayWithFourElements()
        {
            // 4 要素の配列を新たに増やしていないこと（デバイスごとの配列を 4 で作る取りこぼしの再発防止）。
            // デバイス数と無関係な 4 要素の配列が必要になったら、名前を許可リストに足して理由を書くこと。
            var mapping = TestMappingFactory.Host() as Mapping;
            var allowed = new HashSet<string>();
            var found = new List<string>();
            foreach (FieldInfo field in typeof(Mapping).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.FieldType.IsArray) continue;
                if (field.GetValue(mapping) is Array array && array.Length == 4 && !allowed.Contains(field.Name))
                    found.Add(field.Name);
            }

            Assert.True(found.Count == 0, "4 要素の配列: " + string.Join(", ", found));
        }

        [Fact]
        public void DS4LightBar_StaticPerDeviceArrays_HaveOneElementPerSlot()
        {
            // デバイスと無関係な定数表（バッテリー表示の時間表など）は許可リストに入れる（理由を書くこと）
            var notPerDevice = new HashSet<string> { "BatteryIndicatorDurations" };
            var wrong = new List<string>();
            foreach (FieldInfo field in typeof(DS4LightBar).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.FieldType.IsArray || notPerDevice.Contains(field.Name)) continue;
                if (field.GetValue(null) is Array array && array.Length != ControllerLimits.MaxControllerCount)
                    wrong.Add($"{field.Name}={array.Length}");
            }

            Assert.True(wrong.Count == 0, $"長さが {ControllerLimits.MaxControllerCount} ではない: " + string.Join(", ", wrong));
        }

        // ---- delayTime のアクションが、拡張スロット（デバイス 5 ＝ 添字 5）で例外にならない ----

        private static List<string> CaptureErrors(Action body)
        {
            var errors = new List<string>();
            EventHandler<DebugEventArgs> sink = (sender, e) =>
            {
                if (e != null && e.Data != null &&
                    (e.Data.Contains("Synchronous Button SA handling error") || e.Data.Contains("MapCustomAction exception")))
                {
                    lock (errors) errors.Add(e.Data);
                }
            };
            AppLogger.GuiLog += sink;
            try { body(); }
            finally { AppLogger.GuiLog -= sink; }
            lock (errors) return new List<string>(errors);
        }

        private static void ResetDelayClock(SpecialActionTestScope t)
        {
            t.Delay.Reset();
        }

        [Theory]
        [InlineData("Button")]
        [InlineData("Key")]
        public void DelayedAction_OnAnExpandedSlot_DoesNotThrow(string type)
        {
            using var t = new SpecialActionTestScope();
            ResetDelayClock(t);
            string details = type == "Button" ? ((int)X360Controls.A).ToString() : "65";
            var sa = t.Add(new SpecialAction("p8s1_delay_" + Guid.NewGuid().ToString("N"), "L1", type, details, 0.2, "Press\n"));
            t.StateOf(sa).RequiresFreshPressAfterReset = false;
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;

            List<string> errors = CaptureErrors(() =>
            {
                t.RunFrame(SpecialActionTestScope.Neutral());
                for (int i = 0; i < 5; i++) t.RunFrame(held);
                t.RunFrame(SpecialActionTestScope.Neutral());
            });

            ResetDelayClock(t);
            Assert.True(errors.Count == 0, "エラーが記録された: " + (errors.Count > 0 ? errors[0] : string.Empty));
        }

        // ---- delayTime の挙動（デバイス添字に依存しない。台帳 §2 の分岐 A）----

        [Fact]
        public void DelayedButtonPress_FiresOnlyAfterTheDelayHasPassed()
        {
            using var t = new SpecialActionTestScope();
            ResetDelayClock(t);
            var sa = t.Add(new SpecialAction("p8s1_delay_btn_" + Guid.NewGuid().ToString("N"), "L1", "Button",
                ((int)X360Controls.A).ToString(), 0.2, "Press\n"));
            t.StateOf(sa).RequiresFreshPressAfterReset = false;
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;

            try
            {
                t.RunFrame(SpecialActionTestScope.Neutral()); // 離している間に、遅延の基準時刻（oldnowAction）が現在に更新される

                DS4State early = t.RunFrame(held); // 押した直後は、遅延が経っていないので不成立（素通し）
                Assert.False(early.Cross);
                Assert.True(early.L1);

                Thread.Sleep(450); // 遅延（0.2 秒）より十分に長く待つ
                DS4State late = t.RunFrame(held); // 遅延が経ったので成立
                Assert.True(late.Cross);
                Assert.False(late.L1);
            }
            finally
            {
                ResetDelayClock(t);
            }
        }

        // ---- 出力スロットの恒久の復元（B-1）----

        private static OutputSlotManager NewSlotManager(int limit)
        {
            return new OutputSlotManager(() => new Version(1, 0), _ => SASteeringWheelEmulationAxisType.None, limit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(7)]
        public void PermanentOutputSlot_IsRestored_ForEverySlotIndex(int index)
        {
            OutputSlotManager source = NewSlotManager(ControllerLimits.MaxControllerCount);
            source.OutputSlots[index].CurrentReserveStatus = OutSlotDevice.ReserveStatus.Permanent;
            source.OutputSlots[index].PermanentType = OutContType.X360;

            var dto = new OutputSlotPersistDTO();
            dto.MapFrom(source);
            Assert.Single(dto.SlotItems);
            Assert.Equal(index, dto.SlotItems[0].Index);

            OutputSlotManager destination = NewSlotManager(ControllerLimits.MaxControllerCount);
            dto.MapTo(destination);

            Assert.Equal(OutSlotDevice.ReserveStatus.Permanent, destination.OutputSlots[index].CurrentReserveStatus);
            Assert.Equal(OutContType.X360, destination.OutputSlots[index].PermanentType);
        }

        [Fact]
        public void PermanentOutputSlot_BeyondTheManagersSlotCount_IsIgnored()
        {
            var dto = new OutputSlotPersistDTO();
            dto.SlotItems.Add(new OutputSlotSerializer { Index = 6, DeviceType = OutContType.DS4 });
            OutputSlotManager destination = NewSlotManager(4); // 旧 OS（上限 4）の環境へ、8 スロットで保存した設定を読む

            dto.MapTo(destination); // 例外にならず、範囲外のスロットは無視する

            Assert.All(destination.OutputSlots, s => Assert.Equal(OutSlotDevice.ReserveStatus.Dynamic, s.CurrentReserveStatus));
        }
    }
}
