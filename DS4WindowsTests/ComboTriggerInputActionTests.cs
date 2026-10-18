using System;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step3 W3（Phase8-Step3-Plan.md §2.3）: <see cref="ComboTriggerInputAction"/> のテスト。
    /// 時刻は <see cref="TriggerInput"/> に注入し、実時間のスリープは使わない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class ComboTriggerInputActionTests
    {
        private const int Device = 5;
        private static readonly DateTime T = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        private sealed class Harness : IDisposable
        {
            private readonly IProfileSettingsService _settings;
            private readonly int _savedDebounce;
            private readonly DS4StateFieldMapping _fields = new DS4StateFieldMapping();
            private readonly DS4State _state = new DS4State();
            private DateTime _now = T;

            public Harness()
            {
                _settings = Settings();
                _savedDebounce = _settings.DebouncingMs[Device];
                _settings.DebouncingMs[Device] = 0;
                Settings = _settings;
                Input = new TriggerInput(Device, _settings, () => _now);
            }

            public IProfileSettingsService Settings { get; }
            public TriggerInput Input { get; }

            /// <summary>時刻を T からの経過ミリ秒に設定し、指定のボタンだけ押された入力にする。</summary>
            public void Frame(int elapsedMs, params DS4Controls[] pressed)
            {
                _now = T.AddMilliseconds(elapsedMs);
                Array.Clear(_fields.buttons, 0, _fields.buttons.Length);
                foreach (DS4Controls c in pressed) _fields.buttons[(int)c] = true;
                Input.Update(_state, null, null, null, _fields);
            }

            public void Dispose() => _settings.DebouncingMs[Device] = _savedDebounce;
        }

        private static ComboTriggerInputAction Combo(Harness h, double delay = 0.0, bool invert = false, params DS4Controls[] triggers)
            => new ComboTriggerInputAction("t", triggers.Length == 0 ? new[] { DS4Controls.L1 } : triggers, h.Settings, delay, invert);

        // ---- AND 条件 ----

        [Fact]
        public void AllTriggers_MustBeActive()
        {
            using var h = new Harness();
            var action = Combo(h, 0.0, false, DS4Controls.L1, DS4Controls.R1);

            h.Frame(0);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(10, DS4Controls.L1);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(20, DS4Controls.R1);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(30, DS4Controls.L1, DS4Controls.R1);
            Assert.True(action.Evaluate(h.Input).IsEstablished);
            h.Frame(40, DS4Controls.L1, DS4Controls.R1, DS4Controls.Cross);
            Assert.True(action.Evaluate(h.Input).IsEstablished);
        }

        [Fact]
        public void Constructor_ValidatesAndCopiesTheTriggers()
        {
            using var h = new Harness();
            var list = new System.Collections.Generic.List<DS4Controls> { DS4Controls.L1, DS4Controls.R1 };
            var action = new ComboTriggerInputAction("n", list, h.Settings);

            list.Clear();
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.R1 }, action.Triggers);
            Assert.Equal("n", action.Name);

            Assert.Throws<ArgumentNullException>(() => new ComboTriggerInputAction("n", null, h.Settings));
            Assert.Throws<ArgumentException>(() => new ComboTriggerInputAction("n", new DS4Controls[0], h.Settings));
            Assert.Throws<ArgumentNullException>(() => new ComboTriggerInputAction("n", list = new System.Collections.Generic.List<DS4Controls> { DS4Controls.L1 }, null));
            Assert.Throws<ArgumentNullException>(() => action.Evaluate(null));
        }

        // ---- 遅延 ----

        [Fact]
        public void Delay_EstablishesOnlyAfterTheDelayHasElapsed_AndRestartsAfterARelease()
        {
            using var h = new Harness();
            var action = Combo(h, 0.5);

            h.Frame(0);                                    // 未成立: 基準時刻（Old）を T に更新
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(100, DS4Controls.L1);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(400, DS4Controls.L1);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(500, DS4Controls.L1);                  // ちょうど 0.5 秒: 成立
            Assert.True(action.Evaluate(h.Input).IsEstablished);

            h.Frame(700);                                  // 離す: 基準時刻が更新される
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(800, DS4Controls.L1);                  // 押し直し: また 0.5 秒待つ
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(1200, DS4Controls.L1);
            Assert.True(action.Evaluate(h.Input).IsEstablished);
        }

        [Fact]
        public void Delay_IgnoresInvertOnRelease_LikeTheCurrentBranch()
        {
            using var h = new Harness();
            var action = Combo(h, 0.2, true);

            h.Frame(0);
            action.Evaluate(h.Input);
            h.Frame(300, DS4Controls.L1);
            Assert.True(action.Evaluate(h.Input).IsEstablished);   // 反転せず、押している間に成立
            h.Frame(400);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
        }

        [Fact]
        public void Delay_ClockIsSharedThroughTheInput()
        {
            using var h = new Harness();
            var a = Combo(h, 0.5, false, DS4Controls.L1);
            var b = Combo(h, 0.5, false, DS4Controls.R1);

            h.Frame(200, DS4Controls.L1);
            a.Evaluate(h.Input);
            Assert.Equal(T.AddMilliseconds(200), h.Input.Delay.Now);   // a が基準時刻（Now）を更新

            h.Frame(500, DS4Controls.L1);                               // R1 は離れている: b が基準時刻（Old）を更新する
            b.Evaluate(h.Input);
            Assert.Equal(T.AddMilliseconds(500), h.Input.Delay.Old);
        }

        // ---- 離したときに成立（RunOnRelease）----

        [Fact]
        public void InvertOnRelease_ArmsOnPress_AndFiresOnceWhenReleased()
        {
            using var h = new Harness();
            var action = Combo(h, 0.0, true);

            h.Frame(0);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(10, DS4Controls.L1);                           // 押した: 準備
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(20, DS4Controls.L1);                           // 押し続け
            Assert.False(action.Evaluate(h.Input).IsEstablished);
            h.Frame(30);                                           // 離した: 1 回だけ成立
            Assert.True(action.Evaluate(h.Input).IsEstablished);
            h.Frame(40);                                           // その後は不成立
            Assert.False(action.Evaluate(h.Input).IsEstablished);
        }

        [Fact]
        public void InvertOnRelease_Reset_DisarmsTheAction()
        {
            using var h = new Harness();
            var action = Combo(h, 0.0, true);

            h.Frame(10, DS4Controls.L1);
            action.Evaluate(h.Input);
            action.Reset();
            h.Frame(20);
            Assert.False(action.Evaluate(h.Input).IsEstablished);  // 準備が消えているので、離しても成立しない
        }

        [Fact]
        public void InvertOnRelease_NeedsEveryTriggerReleased_ForARelease()
        {
            using var h = new Harness();
            var action = Combo(h, 0.0, true, DS4Controls.L1, DS4Controls.R1);

            h.Frame(10, DS4Controls.L1, DS4Controls.R1);
            action.Evaluate(h.Input);
            h.Frame(20, DS4Controls.L1);                           // 一部だけ離した = AND が崩れた: 現行と同じく成立
            Assert.True(action.Evaluate(h.Input).IsEstablished);
        }

        // ---- 割り当て 0 ----

        [Fact]
        public void Evaluate_DoesNotAllocate()
        {
            using var h = new Harness();
            var plain = Combo(h, 0.0, false, DS4Controls.L1, DS4Controls.R1);
            var delayed = Combo(h, 0.5);
            var inverted = Combo(h, 0.0, true);
            h.Frame(100, DS4Controls.L1, DS4Controls.R1);

            for (int i = 0; i < 50; i++) { plain.Evaluate(h.Input); delayed.Evaluate(h.Input); inverted.Evaluate(h.Input); }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++)
            {
                plain.Evaluate(h.Input);
                delayed.Evaluate(h.Input);
                inverted.Evaluate(h.Input);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(allocated == 0, $"Evaluate が {allocated} バイトを割り当てた");
        }
    }
}
