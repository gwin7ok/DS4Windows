using System;
using System.Reflection;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step3 W2（Phase8-Step3-Plan.md §2.2、Q3＝A・Q4＝A）: <see cref="TriggerInput"/> のテスト。
    /// 最重要は、<c>IsActive</c> が現行の <c>Mapping.getBoolSpecialActionMapping</c>（private）と、全コントロール・多数の入力で同じ結果になること
    /// （テスト専用のリフレクションで直接比較する。Step8 で <c>Mapping</c> の元のメソッドを消すときは、境界値の表に切り替える）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class TriggerInputTests
    {
        private const int Device = 5;

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        private static DS4StateFieldMapping NewFields() => new DS4StateFieldMapping();

        /// <summary>ジャイロ制御の設定を一時的に変える（終了時に戻す）。</summary>
        private sealed class GyroModeScope : IDisposable
        {
            private readonly IProfileSettingsService _settings;
            private readonly GyroOutMode _saved;

            public GyroModeScope(IProfileSettingsService settings, GyroOutMode mode)
            {
                _settings = settings;
                _saved = settings.GyroOutputMode[Device];
                settings.GyroOutputMode[Device] = mode;
            }

            public void Dispose() => _settings.GyroOutputMode[Device] = _saved;
        }

        // ---- 現行の判定との一致（全コントロール × 多数の入力 × ジャイロ制御の有無）----

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Passthru)]
        public void IsActive_MatchesTheCurrentMappingEvaluation_ForEveryControl(GyroOutMode mode)
        {
            IProfileSettingsService settings = Settings();
            Mapping mapping = TestMappingFactory.Create(settings);
            MethodInfo legacy = typeof(Mapping).GetMethod("getBoolSpecialActionMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(legacy);

            var input = new TriggerInput(Device, settings);
            var random = new Random(8001);

            using (new GyroModeScope(settings, mode))
            {
                for (int iteration = 0; iteration < 400; iteration++)
                {
                    DS4State state = new DS4State
                    {
                        LX = (byte)random.Next(256), LY = (byte)random.Next(256),
                        RX = (byte)random.Next(256), RY = (byte)random.Next(256),
                    };
                    DS4StateFieldMapping fields = NewFields();
                    for (int i = 0; i < fields.buttons.Length; i++)
                    {
                        fields.buttons[i] = random.Next(2) == 0;
                        fields.axisdirs[i] = (byte)random.Next(256);
                        fields.triggers[i] = (byte)random.Next(256);
                        fields.gryodirs[i] = random.Next(-150, 151);
                        fields.swipedirbools[i] = random.Next(2) == 0;
                    }

                    input.Update(state, null, null, null, fields);

                    for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                    {
                        var control = (DS4Controls)c;
                        bool expected = (bool)legacy.Invoke(mapping, new object[] { Device, control, state, null, null, fields });
                        Assert.True(expected == input.IsActive(control), $"{control}（iteration={iteration}, mode={mode}）で現行の判定と異なる");
                    }
                }
            }
        }

        // ---- 境界値（現行の仕様の固定）----

        [Fact]
        public void IsActive_Axis_UsesThe55WideThresholdsAroundCenter()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            DS4StateFieldMapping fields = NewFields();

            // 負の向きは cState の値（< 73）、正の向きは表の値（> 183）
            foreach ((byte value, bool negative, bool positive) in new[]
            {
                ((byte)72, true, false), ((byte)73, false, false), ((byte)128, false, false), ((byte)183, false, false), ((byte)184, false, true),
            })
            {
                var state = new DS4State { LX = value, LY = value, RX = value, RY = value };
                fields.axisdirs[(int)DS4Controls.LXPos] = value;
                fields.axisdirs[(int)DS4Controls.LYPos] = value;
                fields.axisdirs[(int)DS4Controls.RXPos] = value;
                fields.axisdirs[(int)DS4Controls.RYPos] = value;
                input.Update(state, null, null, null, fields);

                Assert.Equal(negative, input.IsActive(DS4Controls.LXNeg));
                Assert.Equal(negative, input.IsActive(DS4Controls.LYNeg));
                Assert.Equal(negative, input.IsActive(DS4Controls.RXNeg));
                Assert.Equal(negative, input.IsActive(DS4Controls.RYNeg));
                Assert.Equal(positive, input.IsActive(DS4Controls.LXPos));
                Assert.Equal(positive, input.IsActive(DS4Controls.LYPos));
                Assert.Equal(positive, input.IsActive(DS4Controls.RXPos));
                Assert.Equal(positive, input.IsActive(DS4Controls.RYPos));
            }
        }

        [Fact]
        public void IsActive_Triggers_AreActiveAbove100()
        {
            var input = new TriggerInput(Device, Settings());
            DS4StateFieldMapping fields = NewFields();
            var state = new DS4State();

            fields.triggers[(int)DS4Controls.L2] = 100;
            fields.triggers[(int)DS4Controls.R2] = 101;
            input.Update(state, null, null, null, fields);

            Assert.False(input.IsActive(DS4Controls.L2));
            Assert.True(input.IsActive(DS4Controls.R2));
        }

        [Fact]
        public void IsActive_Gyro_NeedsTheControlsMode_AndPlusMinus67()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            DS4StateFieldMapping fields = NewFields();
            var state = new DS4State();
            fields.gryodirs[(int)DS4Controls.GyroXPos] = 68;
            fields.gryodirs[(int)DS4Controls.GyroXNeg] = -68;
            fields.gryodirs[(int)DS4Controls.GyroZPos] = 67;
            fields.gryodirs[(int)DS4Controls.GyroZNeg] = -67;
            input.Update(state, null, null, null, fields);

            using (new GyroModeScope(settings, GyroOutMode.Controls))
            {
                Assert.True(input.IsActive(DS4Controls.GyroXPos));
                Assert.True(input.IsActive(DS4Controls.GyroXNeg));
                Assert.False(input.IsActive(DS4Controls.GyroZPos)); // ちょうど 67 は、傾いたとみなさない
                Assert.False(input.IsActive(DS4Controls.GyroZNeg));
            }

            using (new GyroModeScope(settings, GyroOutMode.Passthru))
            {
                Assert.False(input.IsActive(DS4Controls.GyroXPos)); // ジャイロ制御でないときは、常に不成立
                Assert.False(input.IsActive(DS4Controls.GyroXNeg));
            }
        }

        [Fact]
        public void IsActive_BeforeAnyUpdate_ReturnsFalse_AndOutOfRangeControlsAreInactive()
        {
            var input = new TriggerInput(Device, Settings());
            Assert.False(input.IsActive(DS4Controls.Cross));

            DS4StateFieldMapping fields = NewFields();
            fields.buttons[(int)DS4Controls.Cross] = true;
            input.Update(new DS4State(), null, null, null, fields);

            Assert.True(input.IsActive(DS4Controls.Cross));
            Assert.False(input.IsActive((DS4Controls)(DS4StateFieldMapping.LAST_DS4_ACTION + 1)));
            Assert.False(input.IsActive(unchecked((DS4Controls)(-1))));
        }

        // ---- 前フレーム（遅延）----

        [Fact]
        public void WasActive_ReadsThePreviousFrame_AndFollowsTheNextUpdate()
        {
            var input = new TriggerInput(Device, Settings());
            var previous = new DS4State { Cross = true };
            var current = new DS4State { Cross = false };
            DS4StateFieldMapping fields = NewFields();
            fields.buttons[(int)DS4Controls.Cross] = false;

            input.Update(current, previous, new DS4StateExposed(current), null, fields);

            Assert.False(input.IsActive(DS4Controls.Cross));
            Assert.True(input.WasActive(DS4Controls.Cross));
            Assert.False(input.WasActive(DS4Controls.Circle));

            // 次のフレームでは、前フレームの表が作り直される
            var nextPrevious = new DS4State { Circle = true };
            input.Update(current, nextPrevious, new DS4StateExposed(current), null, fields);
            Assert.False(input.WasActive(DS4Controls.Cross));
            Assert.True(input.WasActive(DS4Controls.Circle));
        }

        [Fact]
        public void WasActive_WithoutAPreviousState_ReturnsFalse()
        {
            var input = new TriggerInput(Device, Settings());
            input.Update(new DS4State(), null, null, null, NewFields());

            Assert.False(input.WasActive(DS4Controls.Cross));
        }

        // ---- 時刻とクロック ----

        [Fact]
        public void Time_CanBeInjected_AndDefaultsToTheRealClock()
        {
            IProfileSettingsService settings = Settings();
            var fixedTime = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
            var injected = new TriggerInput(Device, settings, () => fixedTime, () => 1234L);

            Assert.Equal(fixedTime, injected.UtcNow);
            Assert.Equal(1234L, injected.TickCount64);

            var real = new TriggerInput(Device, settings);
            Assert.InRange((real.UtcNow - DateTime.UtcNow).TotalSeconds, -5, 5);
            Assert.InRange(Math.Abs(real.TickCount64 - Environment.TickCount64), 0, 5000);
        }

        [Fact]
        public void DelayClock_IsOnePerInput_AndSharedAcrossCallers()
        {
            IProfileSettingsService settings = Settings();
            var a = new TriggerInput(Device, settings);
            var b = new TriggerInput(Device, settings);

            Assert.NotNull(a.Delay);
            Assert.Same(a.Delay, a.Delay);
            Assert.NotSame(a.Delay, b.Delay);
            Assert.Equal(Device, a.Device);
        }

        [Fact]
        public void Constructor_RequiresTheSettings()
        {
            Assert.Throws<ArgumentNullException>(() => new TriggerInput(Device, null));
        }

        // ---- 割り当て 0 ----

        [Fact]
        public void IsActive_AndWasActive_DoNotAllocate()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            DS4StateFieldMapping fields = NewFields();
            var current = new DS4State { LX = 10 };
            var previous = new DS4State { Cross = true };
            input.Update(current, previous, new DS4StateExposed(current), null, fields);

            // ウォームアップ（JIT、前フレームの表の展開）
            for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
            {
                input.IsActive((DS4Controls)c);
                input.WasActive((DS4Controls)c);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 2000; i++)
            {
                for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                {
                    input.IsActive((DS4Controls)c);
                    input.WasActive((DS4Controls)c);
                }
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(allocated == 0, $"IsActive／WasActive が {allocated} バイトを割り当てた");
        }
    }
}
