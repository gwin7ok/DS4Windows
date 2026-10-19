using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.7b-W1（Phase8-Step7-7-Plan.md §4 の W1）: MultiAction（タップ・長押し・ダブルタップ）の特性化。
    /// 旧 <c>MapCustomAction</c> の MultiAction の枝は、デバイスの前回のレポートを使うため、テスト用のデバイス（<see cref="TestDs4Device"/>）をスロットに置く。
    /// 時刻は旧経路が <c>DateTime.UtcNow</c> を使うため、実際に待つ（待ち時間には余裕を持たせる）。マクロは背景のタスクで実行されるため、記録の出現を待つ。
    /// 旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じ期待値。W1 の時点では、ビルダーが MultiAction の束縛を作らないため、新経路の指定でも旧経路が処理する。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step77bW1MultiActionDualPathTests
    {
        private const string Details = "65/65,66/66,67/67";   // タップ → キー 65、長押し → キー 66、ダブルタップ → キー 67

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        /// <summary>スロットにテスト用のデバイスを置き、フレームごとに「前回のレポート」を渡して、1 フレームずつ流す。</summary>
        private sealed class Harness : IDisposable
        {
            private readonly TestDs4Device _device = TestDs4Device.Create();
            private DS4State _previous = SpecialActionTestScope.Neutral();
            private readonly int _savedDebounce;

            public Harness(bool migrated)
            {
                Scope = new SpecialActionTestScope(migrated);
                Scope.Scope.Control.DS4Controllers[Scope.Device] = _device;
                _savedDebounce = Scope.Scope.Settings.DebouncingMs[Scope.Device];
                Scope.Scope.Settings.DebouncingMs[Scope.Device] = 0;
            }

            public SpecialActionTestScope Scope { get; }

            public DS4State Step(bool l1)
            {
                DS4State input = Frame(l1);
                _device.Previous = _previous;
                DS4State mapped = Scope.RunFrame(input);
                _previous = input;
                return mapped;
            }

            /// <summary>指定の時間、10ms おきにフレームを流す。</summary>
            public void Run(bool l1, int milliseconds)
            {
                var sw = Stopwatch.StartNew();
                do
                {
                    Step(l1);
                    Thread.Sleep(10);
                }
                while (sw.ElapsedMilliseconds < milliseconds);
            }

            public void SetDebounce(int ms) => Scope.Scope.Settings.DebouncingMs[Scope.Device] = ms;

            public uint Native(uint key) => Scope.Scope.KbmMapping.macroKeyTranslate ? Scope.Scope.KbmMapping.GetRealEventKey(key) : key;

            public int Presses => Scope.Scope.Recorder.KeyPresses.Count;

            public uint[] PressedKeys => Scope.Scope.Recorder.KeyPresses.ToArray();

            /// <summary>マクロの再生（背景のタスク）で、押されたキーが指定の数になるまで待つ（最大 1.5 秒）。</summary>
            public void WaitForPresses(int count)
            {
                var sw = Stopwatch.StartNew();
                while (Presses < count && sw.ElapsedMilliseconds < 1500)
                    Thread.Sleep(10);
            }

            public void Dispose()
            {
                Scope.Scope.Settings.DebouncingMs[Scope.Device] = _savedDebounce;
                Scope.Scope.Control.DS4Controllers[Scope.Device] = null;
                Scope.Dispose();
            }
        }

        private static SpecialAction AddMulti(SpecialActionTestScope t, string details = Details, bool freshPressGuard = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s77b_multi_" + Guid.NewGuid().ToString("N"), "L1", "MultiAction", details, 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            return sa;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ATap_PlaysTheFirstMacro_OnlyAfterTheIntervalHasPassed(bool migrated)
        {
            using var h = new Harness(migrated);
            AddMulti(h.Scope);

            h.Step(false);
            h.Step(true);
            Thread.Sleep(30);
            h.Step(false);

            Assert.Equal(0, h.Presses);   // 離した直後は、まだ実行しない（ダブルタップの余地を残すための遅延）

            h.Run(false, 400);
            h.WaitForPresses(1);

            Assert.Equal(new[] { h.Native(65) }, h.PressedKeys);
            h.Run(false, 300);
            Assert.Equal(1, h.Presses);   // 1 回だけ
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AHold_PlaysTheSecondMacroOnce_AndTheReleaseDoesNotPlayAnother(bool migrated)
        {
            using var h = new Harness(migrated);
            AddMulti(h.Scope);

            h.Step(false);
            h.Run(true, 800);   // 500ms 以上押し続ける
            h.WaitForPresses(1);

            Assert.Equal(new[] { h.Native(66) }, h.PressedKeys);

            h.Step(false);
            h.Run(false, 400);
            Assert.Equal(new[] { h.Native(66) }, h.PressedKeys);   // 離しても、タップのマクロは実行しない
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ADoubleTap_PlaysTheThirdMacroOnly(bool migrated)
        {
            using var h = new Harness(migrated);
            AddMulti(h.Scope);

            h.Step(false);
            h.Step(true);
            Thread.Sleep(20);
            h.Step(false);
            Thread.Sleep(20);
            h.Step(true);   // 前回のタップの終わりから間隔以内に、もう一度押す
            h.Step(true);
            h.WaitForPresses(1);

            Assert.Equal(new[] { h.Native(67) }, h.PressedKeys);

            h.Step(false);
            h.Run(false, 400);
            Assert.Equal(new[] { h.Native(67) }, h.PressedKeys);   // タップのマクロは実行しない
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyMacros_PlayNothing_AndDoNotThrow(bool migrated)
        {
            using var h = new Harness(migrated);
            AddMulti(h.Scope, ",,");

            h.Step(false);
            h.Step(true);
            Thread.Sleep(30);
            h.Step(false);
            h.Run(false, 400);
            h.Run(true, 700);
            h.Step(false);

            Assert.Equal(0, h.Presses);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheDebouncingMs_ExtendsTheTapInterval(bool migrated)
        {
            using var h = new Harness(migrated);
            h.SetDebounce(200);
            AddMulti(h.Scope);

            h.Step(false);
            h.Step(true);
            Thread.Sleep(30);
            h.Step(false);
            h.Run(false, 250);

            Assert.Equal(0, h.Presses);   // 150ms ＋ 200ms ＝ 350ms を過ぎるまでは実行しない

            h.Run(false, 400);
            h.WaitForPresses(1);

            Assert.Equal(new[] { h.Native(65) }, h.PressedKeys);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheFreshPressGuard_IsNotConsulted(bool migrated)
        {
            using var h = new Harness(migrated);
            AddMulti(h.Scope, freshPressGuard: true);

            h.Step(false);
            h.Step(true);
            Thread.Sleep(30);
            h.Step(false);
            h.Run(false, 400);
            h.WaitForPresses(1);

            Assert.Equal(new[] { h.Native(65) }, h.PressedKeys);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhileTheTriggerIsHeld_ItsOwnOutputIsErased(bool migrated)
        {
            using (var plain = new Harness(migrated))
            {
                plain.Step(false);
                Assert.True(plain.Step(true).L1);   // 特殊アクションがなければ、そのまま出力される
            }

            using var h = new Harness(migrated);
            AddMulti(h.Scope);

            h.Step(false);
            Assert.False(h.Step(true).L1);          // MultiAction のトリガーのボタンの通常の出力は、押している間、消える
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenAnEarlierSpecialActionEndsTheFrame_ThePressEdgeIsMissed(bool migrated)
        {
            // 前の特殊アクション（Profile の切替）がフレームを打ち切ると、その後ろの MultiAction は、その押下のエッジを見逃す（旧ループの return）。
            // 次のフレーム以降は「前回も押している」ため、長押しにもならない（旧経路の性質。新経路も旧ループの位置で動くため同じ）
            using var h = new Harness(migrated);
            h.Scope.InstallProfileRecorder();
            SpecialAction profile = h.Scope.Add(new SpecialAction("p8s77b_profile_" + Guid.NewGuid().ToString("N"), "L1", "Profile", "p8s77b-target", 0, string.Empty));
            h.Scope.StateOf(profile).RequiresFreshPressAfterReset = false;
            h.Scope.StateOf(profile).IsExecuting = false;
            AddMulti(h.Scope);

            h.Step(false);
            h.Run(true, 800);
            h.Step(false);
            h.Run(false, 400);

            Assert.Equal(0, h.Presses);
        }
    }
}
