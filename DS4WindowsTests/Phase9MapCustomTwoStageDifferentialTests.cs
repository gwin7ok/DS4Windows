using System;
using System.Collections.Generic;
using System.Text;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 MapCustom の分割 M2（Phase9-MapCustom-Plan.md §3）: <see cref="TwoStageTriggerProcessor"/>（旧 <c>Mapping.ProcessTwoStageTrigger</c>）の差分テスト。
    /// 旧メソッドの逐語コピー（未使用の引数 <c>cState</c> を除く。コメントは省略）と、5 つのモード × 入力の列（引きの量、経過時間）で、
    /// 返す割り当て（浅い引き／深い引き）と、状態（<see cref="TwoStageTriggerMappingData"/> の全項目）が各ステップで一致することを確かめる。
    /// 時刻は <c>DateTime.Now</c> から一定のずれ（0 または +100 秒）を加えた時計を両方に渡す（HipFire の待ち時間の判定を決定的にする）。
    /// </summary>
    public class Phase9MapCustomTwoStageDifferentialTests
    {
        private const int Device = 3;

        // ---- 旧 Mapping.ProcessTwoStageTrigger の逐語コピー ----
        private sealed class OldProcessor
        {
            private readonly IProfileSettingsService _profileSettings;
            private readonly Func<DateTime> _now;

            public OldProcessor(IProfileSettingsService profileSettings, Func<DateTime> now)
            {
                _profileSettings = profileSettings;
                _now = now;
            }

            public void ProcessTwoStageTrigger(int device, DS4State cState, byte triggerValue, byte triggerRawValue,
                ref DS4ControlSettings inputSoftPull, ref DS4ControlSettings inputFullPull, TriggerOutputSettings outputSettings,
                TwoStageTriggerMappingData twoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull)
            {
                DS4ControlSettings dcsTemp = inputSoftPull;
                DS4ControlSettings dcsFullPull = null;
                TwoStageTriggerMappingData triggerData = twoStageData;

                switch (outputSettings.twoStageMode)
                {
                    case TwoStageTriggerMode.Normal:
                        if (triggerRawValue == 255)
                        {
                            dcsFullPull = inputFullPull;
                        }

                        break;
                    case TwoStageTriggerMode.ExclusiveButtons:
                        if (triggerRawValue == 255)
                        {
                            dcsFullPull = inputFullPull;
                            dcsTemp = null;
                            triggerData.actionStateMode =
                                TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                            triggerData.outputActive = true;
                        }
                        else if (triggerValue != 0 && triggerData.actionStateMode !=
                            TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly)
                        {
                            triggerData.actionStateMode =
                                TwoStageTriggerMappingData.EngageButtonsMode.Both;
                            triggerData.outputActive = true;
                        }
                        else if (triggerValue == 0 && triggerData.outputActive)
                        {
                            triggerData.Reset();
                        }

                        break;
                    case TwoStageTriggerMode.HairTrigger:
                        dcsTemp = null;

                        triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.Both;

                        if (triggerRawValue == 255)
                        {
                            dcsTemp = inputSoftPull;
                            dcsFullPull = inputFullPull;
                            triggerData.softPullActActive = true;
                            triggerData.fullPullActActive = true;
                            triggerData.outputActive = true;
                        }
                        else if (triggerValue != 0 && !triggerData.fullPullActActive)
                        {
                            dcsTemp = inputSoftPull;
                            triggerData.softPullActActive = true;
                            triggerData.outputActive = true;
                        }
                        else if (triggerValue == 0 && triggerData.outputActive)
                        {
                            triggerData.Reset();
                        }

                        break;

                    case TwoStageTriggerMode.HipFire:
                        dcsTemp = null;

                        if (triggerValue != 0 && !triggerData.startCheck)
                        {
                            triggerData.StartProcessing();
                            dcsTemp = null;
                        }
                        else if (triggerValue != 0 && !triggerData.outputActive)
                        {
                            bool outputActive = triggerData.checkTime +
                                TimeSpan.FromMilliseconds(outputSettings.hipFireMS) + TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[device]) < _now();
                            if (outputActive)
                            {
                                triggerData.outputActive = true;

                                if (triggerRawValue == 255)
                                {
                                    dcsFullPull = inputFullPull;
                                    triggerData.fullPullActActive = true;
                                    triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                                }
                                else if (triggerValue != 0)
                                {
                                    dcsTemp = inputSoftPull;
                                    triggerData.softPullActActive = true;
                                    triggerData.actionStateMode = TwoStageTriggerMappingData.EngageButtonsMode.Both;
                                }
                            }
                        }
                        else if (triggerData.outputActive)
                        {
                            if (triggerRawValue == 255)
                            {
                                dcsFullPull = inputFullPull;
                                triggerData.fullPullActActive = true;
                                if (triggerData.actionStateMode == TwoStageTriggerMappingData.EngageButtonsMode.Both)
                                {
                                    dcsTemp = inputSoftPull;
                                }
                            }
                            else if (triggerValue != 0 && triggerData.actionStateMode ==
                                TwoStageTriggerMappingData.EngageButtonsMode.Both)
                            {
                                triggerData.fullPullActActive = false;

                                dcsTemp = inputSoftPull;
                                triggerData.softPullActActive = true;
                            }
                            else if (triggerValue == 0)
                            {
                                triggerData.Reset();
                            }
                        }
                        else if (triggerData.startCheck)
                        {
                            triggerData.Reset();
                        }

                        break;
                    case TwoStageTriggerMode.HipFireExclusiveButtons:
                        dcsTemp = null;

                        if (triggerValue != 0 && !triggerData.startCheck)
                        {
                            triggerData.StartProcessing();
                            dcsTemp = null;
                        }
                        else if (triggerValue != 0 && !triggerData.outputActive)
                        {
                            bool outputActive = triggerData.checkTime + TimeSpan.FromMilliseconds(outputSettings.hipFireMS) + TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[device]) < _now();
                            if (outputActive)
                            {
                                triggerData.outputActive = true;

                                if (triggerRawValue == 255)
                                {
                                    dcsFullPull = inputFullPull;
                                    triggerData.fullPullActActive = true;
                                    triggerData.actionStateMode =
                                        TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly;
                                }
                                else if (triggerValue != 0)
                                {
                                    dcsTemp = inputSoftPull;
                                    triggerData.softPullActActive = true;
                                    triggerData.actionStateMode =
                                        TwoStageTriggerMappingData.EngageButtonsMode.SoftPullOnly;
                                }
                            }
                        }
                        else if (triggerData.outputActive)
                        {
                            if (triggerRawValue == 255 &&
                                triggerData.actionStateMode == TwoStageTriggerMappingData.EngageButtonsMode.FullPullOnly)
                            {
                                dcsFullPull = inputFullPull;
                            }
                            else if (triggerValue != 0 && triggerData.actionStateMode ==
                                TwoStageTriggerMappingData.EngageButtonsMode.SoftPullOnly)
                            {
                                dcsTemp = inputSoftPull;
                            }
                            else if (triggerValue == 0)
                            {
                                triggerData.Reset();
                            }
                        }
                        else if (triggerData.startCheck)
                        {
                            triggerData.Reset();
                        }

                        break;
                    default:
                        break;
                }

                outputSoftPull = dcsTemp;
                outputFullPull = dcsFullPull;
            }
        }

        private sealed class Clock
        {
            public double OffsetMs;
            public DateTime Get() => DateTime.Now.AddMilliseconds(OffsetMs);
        }

        // 1 ステップの入力: （引きの量、生の値、時計のずれ）
        private static readonly (byte Value, byte Raw, double OffsetMs)[] Steps =
        {
            (0, 0, 0), (0, 0, 100000),
            (120, 120, 0), (120, 120, 100000),
            (255, 255, 0), (255, 255, 100000),
            (200, 255, 0), (200, 200, 100000),
        };

        private static string StateOf(TwoStageTriggerMappingData d)
        {
            return $"{d.startCheck},{d.outputActive},{d.softPullActActive},{d.fullPullActActive},{d.actionStateMode},{d.previousActiveButtons}";
        }

        private static IEnumerable<int[]> Sequences(int length, int alphabet)
        {
            int[] codes = new int[length];
            while (true)
            {
                yield return (int[])codes.Clone();
                int k = 0;
                while (k < length && ++codes[k] == alphabet)
                {
                    codes[k] = 0;
                    k++;
                }

                if (k == length) yield break;
            }
        }

        [Theory]
        [InlineData(TwoStageTriggerMode.Disabled)]
        [InlineData(TwoStageTriggerMode.Normal)]
        [InlineData(TwoStageTriggerMode.ExclusiveButtons)]
        [InlineData(TwoStageTriggerMode.HairTrigger)]
        [InlineData(TwoStageTriggerMode.HipFire)]
        [InlineData(TwoStageTriggerMode.HipFireExclusiveButtons)]
        public void TheNewProcessor_MatchesTheOldMethod_ForEveryModeAndInputSequence(TwoStageTriggerMode mode)
        {
            IProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService();
            settings.DebouncingMs[Device] = 0;
            var settingsOut = new TriggerOutputSettings { twoStageMode = mode, hipFireMS = 100 };
            var softBinding = new DS4ControlSettings(DS4Controls.L2);
            var fullBinding = new DS4ControlSettings(DS4Controls.L2FullPull);

            int checkedCount = 0;
            foreach (int length in new[] { 1, 2, 3, 4 })
            {
                foreach (int[] codes in Sequences(length, Steps.Length))
                {
                    var oldClock = new Clock();
                    var newClock = new Clock();
                    var oldProcessor = new OldProcessor(settings, oldClock.Get);
                    var newProcessor = new TwoStageTriggerProcessor(settings, newClock.Get);
                    var oldData = new TwoStageTriggerMappingData();
                    var newData = new TwoStageTriggerMappingData();

                    foreach (int code in codes)
                    {
                        var step = Steps[code];
                        oldClock.OffsetMs = newClock.OffsetMs = step.OffsetMs;

                        DS4ControlSettings oldSoft = softBinding, oldFull = fullBinding;
                        DS4ControlSettings newSoft = softBinding, newFull = fullBinding;
                        oldProcessor.ProcessTwoStageTrigger(Device, null, step.Value, step.Raw, ref oldSoft, ref oldFull, settingsOut, oldData,
                            out DS4ControlSettings oldOutSoft, out DS4ControlSettings oldOutFull);
                        newProcessor.Process(Device, step.Value, step.Raw, ref newSoft, ref newFull, settingsOut, newData,
                            out DS4ControlSettings newOutSoft, out DS4ControlSettings newOutFull);

                        Assert.Same(oldOutSoft, newOutSoft);
                        Assert.Same(oldOutFull, newOutFull);
                        Assert.Equal(StateOf(oldData), StateOf(newData));
                    }

                    checkedCount++;
                }
            }

            Assert.True(checkedCount > 4000);
        }

        [Fact]
        public void HipFire_ActivatesTheSoftPullOnlyAfterTheDelay()
        {
            IProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService();
            settings.DebouncingMs[Device] = 0;
            var clock = new Clock();
            var processor = new TwoStageTriggerProcessor(settings, clock.Get);
            var data = new TwoStageTriggerMappingData();
            var outSettings = new TriggerOutputSettings { twoStageMode = TwoStageTriggerMode.HipFire, hipFireMS = 100 };
            var soft = new DS4ControlSettings(DS4Controls.L2);
            var full = new DS4ControlSettings(DS4Controls.L2FullPull);

            // 押し始め: まだ待ち（どちらも無効）
            DS4ControlSettings s = soft, f = full;
            processor.Process(Device, 120, 120, ref s, ref f, outSettings, data, out var os, out var of);
            Assert.Null(os);
            Assert.Null(of);

            // 待ち時間を過ぎる: 浅い引きが有効になる
            clock.OffsetMs = 100000;
            s = soft; f = full;
            processor.Process(Device, 120, 120, ref s, ref f, outSettings, data, out os, out of);
            Assert.Same(soft, os);
            Assert.Null(of);
        }
    }
}
