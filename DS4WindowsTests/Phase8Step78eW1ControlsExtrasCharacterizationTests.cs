using System;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8e-W1（Phase8-Step7-8e-Plan.md §5）: Controls タブの Extras（<c>ProcessControlSettingAction</c> の Extras の枝）の特性化。Lightbar マクロは廃止した（Q8＝B）ため、対象外。
    /// 旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。W1 の時点では新経路が未実装のため、どちらでも旧経路が処理する。
    /// W2 で新経路が入るとき、承認済みの動作の変更（Q3＝B: 2 つ同時のとき、ボタンごとに元に戻す。Q6＝B: 押したままの適用・切断で元に戻す）の
    /// テストは、<c>migrated</c> で期待値を分ける（該当のテストに「W2」の注記がある）。
    /// 観察は、旧経路が書く状態で行う: <c>Mapping.extrasRumbleActive</c>（振動を出したかの記録。デバイスがいないため、実際の振動は起きない）、
    /// <c>DS4LightBar.forcelight／forcedColor／forcedFlash</c>（静的配列）、<c>ButtonMouseInfo.tempButtonSensitivity／activeButtonSensitivity</c>。
    /// 値は「強い振動, 弱い振動, ライトバー(1=有効), R, G, B, 点滅, 感度(1=有効), 感度」の 9 個。
    /// 他の Host 構築テストと同じコレクションで直列に実行する（静的配列を使うため）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78eW1ControlsExtrasCharacterizationTests
    {
        private const string Full = "100,50,1,10,20,30,5,1,7";   // 振動あり、ライトバー(10,20,30)、点滅 5、感度 7
        private readonly ITestOutputHelper _output;

        public Phase8Step78eW1ControlsExtrasCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State State(bool cross = false, bool circle = false, byte l2 = 0, byte lx = 128)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = cross;
            state.Circle = circle;
            state.L2 = l2;
            state.LX = lx;
            return state;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Press_AppliesRumbleLightbarAndSensitivity_AndTheRelease_RestoresThem(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            Assert.False(t.ForceLight);

            t.Run(State(cross: true));
            Assert.True(t.RumbleActive);
            Assert.True(t.ForceLight);
            Assert.Equal(10, t.ForcedColor.red);
            Assert.Equal(20, t.ForcedColor.green);
            Assert.Equal(30, t.ForcedColor.blue);
            Assert.Equal(5, t.ForcedFlash);
            Assert.Equal(7, t.MouseInfo.tempButtonSensitivity);
            Assert.Equal(7, t.MouseInfo.activeButtonSensitivity);

            t.Run(State());
            Assert.False(t.RumbleActive);
            Assert.False(t.ForceLight);
            Assert.Equal(0, t.ForcedFlash);
            Assert.Equal(-1, t.MouseInfo.tempButtonSensitivity);
            Assert.Equal(t.MouseInfo.buttonSensitivity, t.MouseInfo.activeButtonSensitivity);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheExtrasLightbar_IsForced_OnlyByItsOwnFlag(bool migrated)
        {
            // 承認済みの修正（Q7＝B）: 旧経路は、条件 `!dcs.LightbarMacro?.Active ?? false` が、LightbarMacro が null だと偽になり、
            // Extras のライトバーが効かなかった。Lightbar マクロ機能の廃止（Q8＝B）とあわせて、条件を外した。旧経路で処理する割り当ても、直った
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));

            Assert.True(t.RumbleActive);
            Assert.True(t.ForceLight);
            Assert.Equal(7, t.MouseInfo.tempButtonSensitivity);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ZeroRumble_DoesNotMarkRumbleActive_ButTheLightbarIsStillForced(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, "0,0,1,1,2,3,0,0,0");
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));

            Assert.False(t.RumbleActive);
            Assert.True(t.ForceLight);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LightbarFlagOff_DoesNotForceTheLight_ButTheReleaseStillClearsIt(bool migrated)
        {
            // 離したときの元に戻す処理は、この dcs がライトバーを強制していなくても、強制を解除する（旧経路の癖。変えない）
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, "5,5,0,1,2,3,0,0,0");
            t.PublishControlsOnly();

            t.Run(State());
            DS4LightBar.forcelight[t.Device] = true;   // ほかの強制色の使い手がいる、と仮定する
            t.Run(State(cross: true));
            Assert.True(t.RumbleActive);
            Assert.True(t.ForceLight);   // 外から立てたものは、そのまま

            t.Run(State());
            Assert.False(t.ForceLight);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Sensitivity_IsSetOnlyWhenNoTemporaryOneIsActive(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.MouseInfo.tempButtonSensitivity = 3;   // すでに一時の感度がある（ほかの割り当て）
            t.MouseInfo.activeButtonSensitivity = 3;
            t.Run(State(cross: true));

            Assert.Equal(3, t.MouseInfo.tempButtonSensitivity);   // 上書きしない

            t.Run(State());
            Assert.Equal(-1, t.MouseInfo.tempButtonSensitivity);   // 離すと、元に戻す（他者の一時の感度も解除する）
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhileHeld_RumbleAndLightbarAreAppliedEveryFrame(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));
            DS4LightBar.forcelight[t.Device] = false;
            DS4LightBar.forcedColor[t.Device] = default;
            t.Run(State(cross: true));

            Assert.True(t.ForceLight);
            Assert.Equal(10, t.ForcedColor.red);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Judgement_StickUsesTheSectorRule_AndL2IsPressedAboveZero(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.LXNeg, "0,0,1,1,1,1,0,0,0");
            t.MapExtras(DS4Controls.L2, "0,0,1,2,2,2,0,0,0");
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(lx: 100));   // 128 − 100 ＝ 28（SA の幅 55 には届かないが、扇形の判定では押下）
            Assert.True(t.ForceLight);
            Assert.Equal(1, t.ForcedColor.red);

            t.Run(State());
            Assert.False(t.ForceLight);

            t.Run(State(l2: 1));      // L2 は 0 より大きければ押下（SA は 100 超）
            Assert.True(t.ForceLight);
            Assert.Equal(2, t.ForcedColor.red);
        }

        [Theory]
                [InlineData(true)]
        public void TwoButtonsHeldTogether_AreRestoredOnlyAfterBothAreReleased_ForEitherOrder(bool migrated)
        {
            // 旧経路: held がデバイスに 1 つしかないため、2 つ同時に押すと、両方を離すまで元に戻す処理が走らない（どちらを先に離しても）。
            // 新経路: 承認済みの動作の変更（Q3＝B）。ボタンごとに元に戻すため、migrated の期待値を分ける。
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, "10,10,1,1,1,1,1,0,0");
            t.MapExtras(DS4Controls.Circle, "20,20,1,2,2,2,2,0,0");
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true, circle: true));
            bool circleIsLast = t.ForcedColor.red == 2;   // 後に処理されたほうの色が残る
            bool crossIsLast = t.ForcedColor.red == 1;
            Assert.True(circleIsLast || crossIsLast);
            _output.WriteLine($"[7.8e W1 {(migrated ? "migrated" : "legacy  ")}] 同時に押したとき、後に処理されるのは {(circleIsLast ? "Circle" : "Cross")}");

            // 順序 1: 後に処理されるほうを先に離す
            DS4State firstOnly = circleIsLast ? State(cross: true) : State(circle: true);
            t.Run(firstOnly);
            if (!migrated)
            {
                Assert.True(t.ForceLight);   // 旧経路: 両方を離すまで、元に戻らない
                Assert.True(t.RumbleActive);
            }
            else
            {
                // 新経路（承認済み。Q3＝B）: 離したボタンの分を元に戻す。押したままのほうは、同じフレームでは先に処理されるため、一瞬途切れ、次のフレームで再び適用する
                Assert.False(t.ForceLight);
                Assert.False(t.RumbleActive);
                t.Run(firstOnly);
                Assert.True(t.ForceLight);
                Assert.True(t.RumbleActive);
            }

            t.Run(State());
            Assert.False(t.ForceLight);
            Assert.False(t.RumbleActive);

            // 順序 2: 先に処理されるほうを先に離す（新経路でも、押したままの後のほうが同じフレームで再び適用するため、途切れない）
            t.Run(State(cross: true, circle: true));
            t.Run(circleIsLast ? State(circle: true) : State(cross: true));
            Assert.True(t.ForceLight);
            Assert.True(t.RumbleActive);
            t.Run(State());
            Assert.False(t.ForceLight);
            Assert.False(t.RumbleActive);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DefaultAssignmentWithExtras_DoesNotEraseTheButtonsNormalOutput(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            DS4State output = t.Run(State(cross: true));

            Assert.True(output.Cross);   // Default の割り当ては、通常の出力を消さない
            Assert.True(t.ForceLight);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void KeyAssignmentWithExtras_BothWork(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            DS4ControlSettings dcs = t.MapKey(DS4Controls.Cross, 65);
            dcs.extras = Full;
            t.PublishControlsOnly();
            SpecialAction sa = t.SyntheticFor(DS4Controls.Cross, 65);

            t.Run(State());
            DS4State output = t.Run(State(cross: true));

            Assert.True(t.IsBeingTriggered(sa));   // Key が動く
            Assert.False(output.Cross);            // 割り当てたボタンの通常の出力は消える
            Assert.True(t.ForceLight);             // Extras も動く
            Assert.True(t.RumbleActive);

            t.Run(State());
            Assert.False(t.IsBeingTriggered(sa));
            Assert.False(t.ForceLight);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ValuesThatAreNotNumbers_AreZero(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, "x,y,1,a,b,c,d,0,0");
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));

            Assert.False(t.RumbleActive);
            Assert.True(t.ForceLight);
            Assert.Equal(0, t.ForcedColor.red);
            Assert.Equal(0, t.ForcedFlash);
        }

        [Theory]
                [InlineData(true)]
        public void FewerThanNineValues_StopsHalfWay_ButStillRestoresOnRelease(bool migrated)
        {
            // 9 個未満だと、途中の欄の参照で例外になり（try で握りつぶす）、そこまでの効果だけが残る。新経路の所有の条件で除外する根拠
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, "5,5,1");
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));
            // Phase8-Step8 8a: 9 個未満の Extras は束縛を作らない＝何もしない（旧経路の「途中まで適用」は削除した。Phase8-Step8-Plan.md Q2）
            Assert.False(t.RumbleActive);
            Assert.False(t.ForceLight);

            t.Run(State());
            Assert.False(t.RumbleActive);   // 離すと、元に戻す
        }

        [Theory]
                [InlineData(true)]
        public void ProfileApplyWhileHeld_LeavesTheEffectsOnlyOnTheLegacyPath(bool migrated)
        {
            // 押したままプロファイルが切り替わり、新しい割り当てには Extras がない場合:
            // 旧経路は元に戻す処理が走らず、振動・強制色・感度が残る。新経路は、承認済みの修正（Q6＝B）で、束縛の差し替えのときに元に戻す
            using var t = new ControlsKeyTestScope(migrated);
            DS4ControlSettings dcs = t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));
            Assert.True(t.ForceLight);

            dcs.Reset();   // 新しいプロファイル（Extras なし）
            t.PublishControlsOnly();
            ActionManager.ClearDeviceState(t.Device);
            TestHost.GetService<DS4Windows.Actions.ITriggerDispatchEngine>().OnProfileApplied(t.Device);
            t.Run(State(cross: true));
            t.Run(State());

            _output.WriteLine($"[7.8e W1 {(migrated ? "migrated" : "legacy  ")}] 適用のあと: 強制色={t.ForceLight} 振動の記録={t.RumbleActive} 一時の感度={t.MouseInfo.tempButtonSensitivity}");
            if (!migrated)
            {
                Assert.True(t.ForceLight);
                Assert.True(t.RumbleActive);
                Assert.Equal(7, t.MouseInfo.tempButtonSensitivity);
            }
            else
            {
                Assert.False(t.ForceLight);
                Assert.False(t.RumbleActive);
                Assert.Equal(-1, t.MouseInfo.tempButtonSensitivity);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisconnectWhileHeld_IsReported(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));
            t.Mapping.HandleDeviceDisconnect(t.Device);

            _output.WriteLine($"[7.8e W1 {(migrated ? "migrated" : "legacy  ")}] 切断のあと: 強制色={t.ForceLight} 振動の記録={t.RumbleActive} 一時の感度={t.MouseInfo.tempButtonSensitivity}");
            // 記録のみ（W2 で、Q6＝B の期待値を固定する）。HandleDeviceDisconnect は静的配列を触らない場合がある
        }
    }
}
