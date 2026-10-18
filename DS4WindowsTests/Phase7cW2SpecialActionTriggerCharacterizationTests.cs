using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7c W2（Phase7c-Plan.md §3 W2）: 特殊アクションの「トリガー成立→実行」（<c>Mapping.MapCustom</c>／<c>MapCustomAction</c>／Stage3）の特性化テスト。
    /// 現在の挙動を固定することが目的で、Phase8 で経路を統一ディスパッチへ移しても、入力 → 出力・状態が変わらないことを保証する。
    /// 対象: ボタン種別の Press の出力、トリガーの AND 条件、トリガーボタンの抑制と解除、Fresh-press ガードの解除条件（80ms の連続した未成立）、
    ///       Profile／Program の実行ブロック（ガード中・実行中）、Stage3 の並行変更への耐性。
    /// 範囲外（Phase7c の続き）: Key の Press／Toggle の出力、`uTrigger`／`delayTime`／`pressRelease`、プロファイル切替の復帰とチェーン、`shiftTrigger`。
    /// `Mapping` の静的な状態と `ActionManager`（静的）を共有するため、<c>ServiceProviderHolder</c> のコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7cW2SpecialActionTriggerCharacterizationTests
    {
        // ---- ボタン種別: Press の出力と AND 条件 ----

        [Fact]
        public void ButtonPress_TriggerHeld_OutputsTargetButton_AndErasesTheTrigger()
        {
            using var t = new SpecialActionTestScope();
            t.AddButtonPress("L1");
            DS4State input = SpecialActionTestScope.Neutral();
            input.L1 = true;

            DS4State output = t.RunFrame(input);

            Assert.True(output.Cross);   // 出力先は X360 の A ＝ DS4 の Cross
            Assert.False(output.L1);     // トリガーのボタンは出力から消える
        }

        [Fact]
        public void ButtonPress_TriggerReleased_OutputsNothing()
        {
            using var t = new SpecialActionTestScope();
            t.AddButtonPress("L1");

            DS4State output = t.RunFrame(SpecialActionTestScope.Neutral());

            Assert.False(output.Cross);
            Assert.False(output.L1);
        }

        [Fact]
        public void ButtonPress_AndCondition_NeedsEveryTriggerButton()
        {
            using var t = new SpecialActionTestScope();
            t.AddButtonPress("L1/R1");

            DS4State onlyL1 = SpecialActionTestScope.Neutral();
            onlyL1.L1 = true;
            DS4State partial = t.RunFrame(onlyL1);
            Assert.False(partial.Cross);  // 片方だけでは不成立
            Assert.True(partial.L1);      // 不成立のときは素通し

            DS4State both = SpecialActionTestScope.Neutral();
            both.L1 = true;
            both.R1 = true;
            DS4State full = t.RunFrame(both);
            Assert.True(full.Cross);
            Assert.False(full.L1);
            Assert.False(full.R1);
        }

        [Fact]
        public void ButtonPress_IgnoresTheFreshPressGuard_ForPressMode()
        {
            // 現在の挙動の固定: Stage3 の Press はトリガー成立中のみ出力し、Fresh-press ガードを見ない（Toggle の反転だけがガードを見る）。
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            t.StateOf(sa).RequiresFreshPressAfterReset = true;
            DS4State input = SpecialActionTestScope.Neutral();
            input.L1 = true;

            DS4State output = t.RunFrame(input);

            Assert.True(output.Cross);
        }

        // ---- トリガーボタンの抑制と解除 ----

        [Fact]
        public void Suppression_AddsEveryTriggerButtonWhileEstablished_AndRemovesEachWhenReleased()
        {
            using var t = new SpecialActionTestScope();
            t.AddButtonPress("L1/R1");
            // Phase8-Step4 W4: 抑制の集合はエンジンの所有。IsSuppressed で観測する
            bool Suppressed(DS4Controls control) => t.Engine.IsSuppressed(t.Device, control);

            DS4State both = SpecialActionTestScope.Neutral();
            both.L1 = true;
            both.R1 = true;
            t.RunFrame(both);
            Assert.True(Suppressed(DS4Controls.L1));
            Assert.True(Suppressed(DS4Controls.R1));

            // R1 を離す: 冒頭の解除パスが、離されたボタンだけを個別に外す
            DS4State onlyL1 = SpecialActionTestScope.Neutral();
            onlyL1.L1 = true;
            t.RunFrame(onlyL1);
            Assert.True(Suppressed(DS4Controls.L1));
            Assert.False(Suppressed(DS4Controls.R1));

            t.RunFrame(SpecialActionTestScope.Neutral());
            Assert.False(Suppressed(DS4Controls.L1));
            Assert.False(Suppressed(DS4Controls.R1));
        }

        // ---- Fresh-press ガードの解除（トリガーが FreshPressReleaseHoldMs 連続して未成立になるまで） ----

        [Fact]
        public void FreshPress_WhileTriggerHeld_KeepsTheGuard_AndResetsTheOffTimer()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = true;
            state.UntriggeredTimestampTicks = Environment.TickCount64 - 5000; // 未成立が長く続いていた
            DS4State input = SpecialActionTestScope.Neutral();
            input.L1 = true;

            t.RunFrame(input);

            Assert.True(state.RequiresFreshPressAfterReset);
            Assert.Equal(0, state.UntriggeredTimestampTicks);
        }

        [Fact]
        public void FreshPress_FirstUntriggeredFrame_RecordsTheTimestamp_WithoutReleasingTheGuard()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = true;
            state.UntriggeredTimestampTicks = 0;

            t.RunFrame(SpecialActionTestScope.Neutral());

            Assert.NotEqual(0, state.UntriggeredTimestampTicks);
            Assert.True(state.RequiresFreshPressAfterReset);
        }

        [Fact]
        public void FreshPress_UntriggeredLongerThanTheHold_ReleasesTheGuard()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = true;
            state.UntriggeredTimestampTicks = Environment.TickCount64 - (ActionInstanceState.FreshPressReleaseHoldMs + 5000);

            t.RunFrame(SpecialActionTestScope.Neutral());

            Assert.False(state.RequiresFreshPressAfterReset);
        }

        [Fact]
        public void FreshPress_UntriggeredShorterThanTheHold_KeepsTheGuard()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            // 初回の実行（JIT など）の遅れで閾値をまたがないよう、ウォームアップしてから、記録時刻を「未来」にして経過を負にする
            t.RunFrame(SpecialActionTestScope.Neutral());
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = true;
            state.UntriggeredTimestampTicks = Environment.TickCount64 + 10_000;

            t.RunFrame(SpecialActionTestScope.Neutral());

            Assert.True(state.RequiresFreshPressAfterReset);
        }

        // ---- Profile／Program の実行ブロック（ガード中・実行中は、実行も BeingTriggered の設定もしない） ----

        [Theory]
        [InlineData("Profile", true, false)]
        [InlineData("Profile", false, true)]
        [InlineData("Program", true, false)]
        [InlineData("Program", false, true)]
        public void ProfileAndProgram_AreBlockedWhileTheGuardIsOnOrExecuting(string type, bool freshPressGuard, bool isExecuting)
        {
            using var t = new SpecialActionTestScope();
            // Program は存在しない実行ファイルにして、万一ブロックされなくても実プログラムを起動しない
            string details = type == "Program" ? @"C:\p7c-nonexistent\p7c.exe" : "p7c-nonexistent-profile";
            string extras = type == "Profile" ? "AutomaticUntrigger" : string.Empty;
            SpecialAction sa = t.Add(new SpecialAction("p7c_blocked_" + Guid.NewGuid().ToString("N"), "L1", type, details, 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = isExecuting;
            DS4State input = SpecialActionTestScope.Neutral();
            input.L1 = true;

            try
            {
                t.RunFrame(input);

                Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
                Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);
                Assert.True(state.IsExecuting == isExecuting, "ブロックされた実行は、IsExecuting を変えない");
            }
            finally
            {
                state.IsExecuting = false;
            }
        }

        // ---- キー種別（状態の遷移のみ。実際のキー送出は ActionManager／KeyAction の側で、ここでは見ない）----

        [Fact]
        public void KeyPress_EstablishedThenHeldThenReleased_TracksBeingTriggeredAndUntriggerIndex()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddKey("L1", toggle: false);
            int index = t.IndexOf(sa);
            t.StateOf(sa).RequiresFreshPressAfterReset = false;
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;

            t.RunFrame(held);
            Assert.True(ActionManager.IsBeingTriggered(sa, t.Device));
            Assert.Equal(index, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);

            t.RunFrame(held); // 保持中も成立のまま
            Assert.True(ActionManager.IsBeingTriggered(sa, t.Device));
            Assert.Equal(index, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);

            t.RunFrame(SpecialActionTestScope.Neutral()); // 離す
            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
            Assert.Equal(-1, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);
        }

        [Fact]
        public void KeyPress_WhileTheFreshPressGuardIsOn_DoesNotFire()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddKey("L1", toggle: false);
            t.StateOf(sa).RequiresFreshPressAfterReset = true;
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;

            t.RunFrame(held);

            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
            Assert.Equal(-1, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);
        }

        [Fact]
        public void KeyToggle_DoesNotRegisterAnUntriggerIndex()
        {
            // Toggle は、物理的に離したときではなく、次の成立で切り替わるため、UntriggerIndex を登録しない
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddKey("L1", toggle: true);
            t.StateOf(sa).RequiresFreshPressAfterReset = false;
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;

            t.RunFrame(held);

            Assert.Equal(-1, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);
        }

        // ---- Stage3 の並行変更への耐性（Phase7c W1 の回帰ガード）----

        [Fact]
        public void Stage3_SurvivesProfileActionListBeingReloadedConcurrently()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.AddButtonPress("L1");
            List<string> names = t.ProfileActionNames; // Mapping が生で読む List<string> と同一
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

            using var stop = new ManualResetEventSlim(false);
            var reloader = new Thread(() =>
            {
                // プロファイルの読込（LegacyProfileXmlSerializer）と同じ Clear→Add を繰り返す（要素数は 0 と 1 の間）
                while (!stop.IsSet)
                {
                    names.Clear();
                    names.Add(sa.name);
                }
            }) { IsBackground = true };

            try
            {
                DS4State input = SpecialActionTestScope.Neutral();
                input.L1 = true;
                t.RunFrame(input); // ウォームアップ
                reloader.Start();
                for (int i = 0; i < 3000; i++)
                    t.RunFrame(input);
            }
            finally
            {
                stop.Set();
                reloader.Join(5000);
                AppLogger.GuiLog -= sink;
            }

            lock (errors)
                Assert.True(errors.Count == 0, "Stage3／MapCustomAction がエラーを記録した: " + (errors.Count > 0 ? errors[0] : string.Empty));
        }
    }
}
