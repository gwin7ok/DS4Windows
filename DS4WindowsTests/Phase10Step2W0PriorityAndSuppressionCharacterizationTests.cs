using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W0（Phase10-Step2-Plan.md §5。Phase10-Assignment-UI-Unification-Plan.md §7）: Controls の割り当てと SA が、同じボタンにあるときの
    /// 優先順位と抑制の、現行の挙動の特性化。製品コードは変えない。フレーム列（押す・離す）を <c>Mapping.MapCustom</c> に入力し、次を観察する。
    /// <list type="bullet">
    /// <item><description>Controls の Key の実行: 合成 SpecialAction（<c>Synthetic_Key_{device}_{control}</c>）の <c>BeingTriggered</c>（<c>ControlsKeyTestScope</c> と同じ観測）。</description></item>
    /// <item><description>SA の成立: SA の <c>BeingTriggered</c>。</description></item>
    /// <item><description>素通し（Default）の出力: <c>RunFrame</c> が返す出力の状態。</description></item>
    /// </list>
    /// 現行の挙動が §7 と食い違うテストが見つかった場合は、期待値を変えず、結果を報告する（Q5）。
    /// 再現できない規則（遅延つき・Run on release・MultiAction の時間依存、R8 のプロファイル切替、R10 の Controls の SA 専用の種別）は、計画書の台帳に記す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step2W0PriorityAndSuppressionCharacterizationTests
    {
        private const int ControlsKeyCross = 70;
        private const int ControlsKeyCircle = 71;
        private const int ControlsKeyL2 = 73;

        private sealed class Rig : IDisposable
        {
            private readonly List<DS4Controls> _touched = new List<DS4Controls>();

            public SpecialActionTestScope Sa { get; } = new SpecialActionTestScope();

            public int Device => Sa.Device;

            /// <summary>Controls のボタンを Key にする。SA を足す前に呼ぶ（SA の追加が束縛を組み立て直す）。SA がないテストは <see cref="Publish"/> も呼ぶ。</summary>
            public void MapKey(DS4Controls control, int key)
            {
                DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, control);
                dcs.Reset();
                dcs.actionType = DS4ControlSettings.ActionType.Key;
                dcs.action.actionKey = key;
                dcs.keyType = DS4KeyType.None;
                _touched.Add(control);
            }

            public void Publish() => Sa.PublishBindings();

            public bool ControlsDispatched(DS4Controls control, int key)
            {
                SpecialAction synthetic = SyntheticKeyActionFactory.Create(Device, (int)control, (uint)key, false, false);
                return ActionManager.IsBeingTriggered(synthetic, Device);
            }

            /// <summary>
            /// Key の SA を足す。新しい SA の状態は、Fresh-press ガード（プロファイル適用直後の押しっぱなしで連鎖発火しないための安全弁）が
            /// オンで始まり、オンのあいだは出力を実行しない（成立の判定と、トリガーのボタンの抑制は行う）。既存のテスト
            /// （<c>Phase7cW2SpecialActionTriggerCharacterizationTests</c>）と同じく、押しっぱなしの直後ではない状況として、ガードを解除する。
            /// </summary>
            public SpecialAction AddKey(string triggers, bool toggle = false)
            {
                SpecialAction sa = Sa.AddKey(triggers, toggle);
                Sa.StateOf(sa).RequiresFreshPressAfterReset = false;
                return sa;
            }

            public bool Triggered(SpecialAction sa) => ActionManager.IsBeingTriggered(sa, Device);

            /// <summary>指定のボタンを押した 1 フレームを流す（L2 は 0〜255 の値）。</summary>
            public DS4State Run(DS4Controls[] held = null, byte l2 = 0)
            {
                DS4State input = SpecialActionTestScope.Neutral();
                if (held != null)
                {
                    foreach (DS4Controls c in held)
                    {
                        switch (c)
                        {
                            case DS4Controls.Cross: input.Cross = true; break;
                            case DS4Controls.Circle: input.Circle = true; break;
                            case DS4Controls.Square: input.Square = true; break;
                            default: throw new ArgumentException("未対応のボタン: " + c);
                        }
                    }
                }

                input.L2 = l2;
                return Sa.RunFrame(input);
            }

            public void Dispose()
            {
                foreach (DS4Controls c in _touched)
                    TestGlobals.Store.GetDS4CSetting(Device, c)?.Reset();

                ActionManager.ClearDeviceState(Device);   // 実行状態を、次のテストへ残さない
                Sa.Dispose();
            }
        }

        private static readonly DS4Controls[] Nothing = new DS4Controls[0];
        private static readonly DS4Controls[] Cross = { DS4Controls.Cross };
        private static readonly DS4Controls[] CrossCircle = { DS4Controls.Cross, DS4Controls.Circle };
        private static readonly DS4Controls[] CrossCircleSquare = { DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square };

        // ---- 対照: SA がなければ、Controls の割り当てが普通に実行される（観測の確認）----

        [Fact]
        public void Baseline_WithoutAnySpecialAction_TheControlsKeyIsDispatched()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            t.Publish();

            t.Run(Nothing);
            Assert.False(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));

            t.Run(Cross);
            Assert.True(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));

            t.Run(Nothing);
            Assert.False(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));
        }

        // ---- §7.1 条件 1: 同じボタンに Controls と SA があるときは、SA が優先 ----

        [Fact]
        public void Condition1_SingleButtonSpecialAction_WinsOverTheControlsAssignmentOfTheSameButton()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            SpecialAction sa = t.AddKey("Cross");

            t.Run(Cross);

            Assert.True(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));
        }

        // ---- §7.1 条件 2: 成立の手前までに押したボタンは実行し、成立したときの最後のボタンは実行しない ----

        [Fact]
        public void Condition2_EarlierButtonRuns_AndTheLastButtonThatCompletesTheTriggerDoesNot()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            t.MapKey(DS4Controls.Circle, ControlsKeyCircle);
            SpecialAction sa = t.AddKey("Cross/Circle");

            t.Run(Cross);   // 成立の手前: Cross だけ
            Assert.False(t.Triggered(sa));
            Assert.True(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));      // 先に押したボタンは実行される

            t.Run(CrossCircle);   // Circle が最後のボタン。ここで成立
            Assert.True(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.Circle, ControlsKeyCircle));  // 最後のボタンは、同じフレームから実行されない
        }

        // ---- R2: 同じフレームで同時に押したときは「最後」がなく、トリガーの全ボタンが抑制される ----

        [Fact]
        public void R2_PressedInTheSameFrame_AllTriggerButtonsAreSuppressed()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            t.MapKey(DS4Controls.Circle, ControlsKeyCircle);
            SpecialAction sa = t.AddKey("Cross/Circle");

            t.Run(CrossCircle);

            Assert.True(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));
            Assert.False(t.ControlsDispatched(DS4Controls.Circle, ControlsKeyCircle));
        }

        // ---- R3: 成立前に出た出力は取り消さない。そのボタンを離すまで続く ----

        [Fact]
        public void R3_TheEarlierButtonsOutputIsNotCancelled_UntilTheButtonIsReleased()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            t.MapKey(DS4Controls.Circle, ControlsKeyCircle);
            t.AddKey("Cross/Circle");

            t.Run(Cross);
            Assert.True(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));

            t.Run(CrossCircle);   // SA が成立。Cross の出力は、取り消されない（押しっぱなしのまま）
            Assert.True(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));

            t.Run(Nothing);       // 両方を離すと、Cross の出力も止まる
            Assert.False(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));
        }

        // ---- R4: SA どうしの重なりは、成立したものをすべて実行する ----

        [Fact]
        public void R4_OverlappingSpecialActions_BothAreExecutedWhenBothAreEstablished()
        {
            using var t = new Rig();
            SpecialAction small = t.AddKey("Cross/Circle");
            SpecialAction large = t.AddKey("Cross/Circle/Square");

            t.Run(CrossCircle);
            Assert.True(t.Triggered(small));
            Assert.False(t.Triggered(large));

            t.Run(CrossCircleSquare);
            Assert.True(t.Triggered(small));   // ボタン数の少ない SA も、成立が続く
            Assert.True(t.Triggered(large));   // 両方が実行される
        }

        // ---- R5: 素通し（Default）も、成立したら抑制される（先に押した分は素通しで出る）----

        [Fact]
        public void R5_DefaultPassthrough_EarlierButtonPassesThrough_AndIsCutOnceTheTriggerIsEstablished()
        {
            using var t = new Rig();
            t.AddKey("Cross/Circle");   // Controls の割り当ては、どちらも Default

            DS4State beforeEstablished = t.Run(Cross);
            Assert.True(beforeEstablished.Cross);      // 成立の手前: 素通しで出る

            DS4State established = t.Run(CrossCircle);
            Assert.False(established.Cross);           // 成立後: トリガーの全ボタンの出力が 0 に戻る
            Assert.False(established.Circle);
        }

        // ---- R6: アナログ（L2 の軸）。閾値に達する前は Controls が出て、成立後は SA が優先 ----
        // Phase10-Step4c W4（ユーザー承認、2026-10-21）: Controls の Key（デジタルな出力）は SA と同じ閾値（100 を超える）で判定する。
        // そのため、Key では、閾値に達する前の Controls の出力はなくなった（閾値までは、どちらも成立しない）。アナログな出力（パッドの軸）は従来の判定のまま

        [Fact]
        public void R6_AnalogTrigger_ControlsKeyAndTheSpecialActionShareTheThreshold()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.L2, ControlsKeyL2);
            SpecialAction sa = t.AddKey("L2");

            t.Run(Nothing, l2: 50);   // 100 以下。Controls の Key も、SA も成立しない
            Assert.False(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.L2, ControlsKeyL2));

            t.Run(Nothing, l2: 200);  // 閾値を超えて成立。SA が優先し、Controls の Key は出ない
            Assert.True(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.L2, ControlsKeyL2));
        }

        // ---- R7: 先に押したボタンを離して、再び押したとき、SA が再成立し、そのボタンが「最後」として抑制される ----

        [Fact]
        public void R7_ReleasingAndRepressingAButton_ReEstablishesTheTrigger_AndTheRepressedButtonIsSuppressed()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Circle, ControlsKeyCircle);
            SpecialAction sa = t.AddKey("Cross/Circle");

            t.Run(CrossCircle);
            Assert.True(t.Triggered(sa));

            t.Run(Cross);          // Circle を離す。トリガーが崩れる
            Assert.False(t.Triggered(sa));

            t.Run(CrossCircle);    // Circle を再び押す
            Assert.True(t.Triggered(sa));
            Assert.False(t.ControlsDispatched(DS4Controls.Circle, ControlsKeyCircle));
        }

        // ---- R9: プロファイルで有効でない SA は、束縛がなく、抑制しない ----

        [Fact]
        public void R9_ASpecialActionThatIsNotEnabledInTheProfile_DoesNotSuppressTheControlsAssignment()
        {
            using var t = new Rig();
            t.MapKey(DS4Controls.Cross, ControlsKeyCross);
            SpecialAction sa = t.AddKey("Cross");
            t.Sa.ProfileActionNames.Remove(sa.name);   // プロファイルの有効な SA の一覧から外す（Actions.xml 側には残る）
            t.Publish();

            t.Run(Cross);

            Assert.False(t.Triggered(sa));
            Assert.True(t.ControlsDispatched(DS4Controls.Cross, ControlsKeyCross));
        }
    }
}
