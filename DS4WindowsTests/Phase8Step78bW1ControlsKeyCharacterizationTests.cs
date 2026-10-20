using System;
using System.Linq;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8b-W1（Phase8-Step7-8b-Plan.md §4）: Controls タブの Key（<c>ProcessControlSettingAction</c> の Key の枝）の特性化。
    /// 旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じ期待値。W1 の時点では新経路が未実装のため、新経路の指定でも旧経路が処理する。
    /// 押下の観測は、旧経路が使う合成 SpecialAction（<c>Synthetic_Key_{device}_{control}</c>）の状態（<c>BeingTriggered</c>／<c>IsToggledOn</c>）。状態は名前で引かれるため、W2 の新経路の識別用 SA も同じ状態を指す。
    /// 押下の判定は <c>GetBoolActionMapping</c>（スティックは角度の扇形、L2／R2 は 0 より大きい）で、特殊アクションのトリガー判定（±55、100 より大きい）とは違う。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78bW1ControlsKeyCharacterizationTests
    {
        private const int Key = 65;
        private readonly ITestOutputHelper _output;

        public Phase8Step78bW1ControlsKeyCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Press_BeingTriggeredFollowsTheButton(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.Cross, Key);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.Cross, Key);

            DS4State idle = ControlsKeyTestScope.Neutral();
            DS4State held = ControlsKeyTestScope.Neutral();
            held.Cross = true;

            t.Run(idle);
            Assert.False(t.IsBeingTriggered(sa));

            DS4State output = t.Run(held);
            Assert.True(t.IsBeingTriggered(sa));
            Assert.False(output.Cross);          // 割り当てたボタンの通常の出力は消える

            t.Run(held);
            Assert.True(t.IsBeingTriggered(sa));  // 押している間は保たれる

            t.Run(idle);
            Assert.False(t.IsBeingTriggered(sa));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Toggle_IsDispatchedAtThePress_AndTheToggleStateIsReported(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.Cross, Key, DS4KeyType.Toggle);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.Cross, Key, toggle: true);

            DS4State idle = ControlsKeyTestScope.Neutral();
            DS4State held = ControlsKeyTestScope.Neutral();
            held.Cross = true;

            t.Run(idle);
            Assert.False(t.IsToggledOn(sa));

            t.Run(held);
            bool onAtPress = t.IsToggledOn(sa);
            bool triggeredAtPress = t.IsBeingTriggered(sa);

            t.Run(held);
            t.Run(idle);
            bool onAtRelease = t.IsToggledOn(sa);

            // Toggle のオン／オフ（IsToggledOn）は、出力先のスタブ（リピーターの有無）で結果が分かれるため、値を記録するだけにする（W2 で旧・新の一致を比べる）
            _output.WriteLine($"[7.8b W1 {(migrated ? "migrated" : "legacy  ")}] toggle: onAtPress={onAtPress} onAtRelease={onAtRelease}");
            Assert.True(triggeredAtPress);
            Assert.False(t.IsBeingTriggered(sa));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StickDirection_UsesTheSectorRule_NotTheSpecialActionThreshold(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.LXNeg, Key);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.LXNeg, Key);

            // わずかに左へ倒した（128 − 100 ＝ 28。特殊アクションのトリガー判定の幅 55 には届かない）。角度の扇形の中なので、押している
            DS4State slightlyLeft = ControlsKeyTestScope.Neutral();
            slightlyLeft.LX = 100;
            DS4State output = t.Run(slightlyLeft);
            Assert.True(t.IsBeingTriggered(sa));
            Assert.Equal(128, output.LX);          // 方向の出力は消える

            DS4State idle = ControlsKeyTestScope.Neutral();
            t.Run(idle);
            Assert.False(t.IsBeingTriggered(sa));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MappedStickDirection_LeavesTheOppositeDirection_ToItsOwnAssignment(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.LXNeg, Key);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.LXNeg, Key);

            // 右へ倒した: 割り当てたのは「左」。押していない。反対の方向（LXPos）は未設定（Default）で、その出力が素通しされる（customMapQueue）ため、軸はそのまま出る。
            // （押していない軸の出力の消去が、軸の値として目に見えるのは、反対の方向を含まない傾きのときに限られる。消去の規則そのものは、エンジンの方針で引き継ぐ）
            DS4State right = ControlsKeyTestScope.Neutral();
            right.LX = 220;
            DS4State output = t.Run(right);

            Assert.False(t.IsBeingTriggered(sa));
            Assert.Equal(220, output.LX);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Trigger_IsPressedAboveZero_NotAboveOneHundred(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.L2, Key);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.L2, Key);

            DS4State slight = ControlsKeyTestScope.Neutral();
            slight.L2 = 1;   // 特殊アクションのトリガー判定（100 より大きい）には届かない
            DS4State output = t.Run(slight);

            Assert.True(t.IsBeingTriggered(sa));
            Assert.Equal(0, output.L2);            // トリガーの出力は消える

            t.Run(ControlsKeyTestScope.Neutral());
            Assert.False(t.IsBeingTriggered(sa));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AButtonWithoutAnAssignment_IsLeftAlone(bool migrated)
        {
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.L1, Key);
            t.Publish();

            DS4State input = ControlsKeyTestScope.Neutral();
            input.Cross = true;   // 未設定（Default）のボタン
            DS4State output = t.Run(input);

            Assert.True(output.Cross);
        }

        [Theory]
                [InlineData(true)]
        public void ADcsWithExtras_IsNotOwnedByTheNewPath(bool migrated)
        {
            // 7.8e: Extras つきの Key は、新経路が処理する（Extras の束縛 ＋ Key の束縛）。新経路の指定がなければ、束縛を作らず、旧経路が処理する
            using var t = new ControlsKeyTestScope(migrated);
            DS4ControlSettings dcs = t.MapKey(DS4Controls.Cross, Key);
            dcs.extras = "1,2,0,0,0,0,0,0,0";
            t.Publish();

            Assert.Equal(migrated ? 2 : 0, TestHost.GetService<DS4Windows.DI.IControlBindingBuilder>().Build(t.Device).Count);
        }

        [Theory]
                [InlineData(true)]
        public void SharesTheStateWithAKeySpecialActionOfTheSameKey(bool migrated)
        {
            // 旧経路の癖（FindSpecialActionForLogicalKey）: 同じ論理キーの Key 特殊アクションが登録されていれば、Controls の Key はその状態を使う。
            // Q4＝B（Phase8-Step2b-ControlsTab-Design.md §10）で、新経路では共有をやめる。W2 で、新経路の期待値をこのテストで切り替える
            var saved = ActionRegistry.AllActions().ToList();
            var keyAction = new SpecialAction("p8s78b_shared_" + Guid.NewGuid().ToString("N"), "R1", "Key", Key.ToString(), 0, "Press\n");
            ActionRegistry.Initialize(new[] { keyAction });
            try
            {
                using var t = new ControlsKeyTestScope(migrated);
                t.MapKey(DS4Controls.Cross, Key);
                t.Publish();
                SpecialAction synthetic = t.SyntheticFor(DS4Controls.Cross, Key);

                DS4State held = ControlsKeyTestScope.Neutral();
                held.Cross = true;
                t.Run(held);

                bool shares = !migrated;   // Q4＝B（W2）: 新経路は共有しない（自分専用の状態を使う）。旧経路は共有する
                Assert.Equal(shares, t.IsBeingTriggered(keyAction));
                Assert.Equal(!shares, t.IsBeingTriggered(synthetic));
            }
            finally
            {
                ActionRegistry.Initialize(saved);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ProfileApplyOrDisconnectWhileHeld_IsReported(bool migrated)
        {
            // 押したままプロファイルを適用した（ClearDeviceState）／切断した場合の、状態の変化を記録する（W2 で旧・新を比べる。期待値は結果を見て確定する）
            using var t = new ControlsKeyTestScope(migrated);
            t.MapKey(DS4Controls.Cross, Key);
            t.Publish();
            SpecialAction sa = t.SyntheticFor(DS4Controls.Cross, Key);

            DS4State held = ControlsKeyTestScope.Neutral();
            held.Cross = true;

            t.Run(held);
            bool before = t.IsBeingTriggered(sa);

            ActionManager.ClearDeviceState(t.Device);
            bool afterClear = t.IsBeingTriggered(sa);
            t.Run(held);
            bool nextFrameAfterClear = t.IsBeingTriggered(sa);

            t.Mapping.HandleDeviceDisconnect(t.Device);
            bool afterDisconnect = t.IsBeingTriggered(sa);
            t.Run(held);
            bool nextFrameAfterDisconnect = t.IsBeingTriggered(sa);

            _output.WriteLine($"[7.8b W1 {(migrated ? "migrated" : "legacy  ")}] held={before} afterClear={afterClear} nextFrame={nextFrameAfterClear} afterDisconnect={afterDisconnect} nextFrame={nextFrameAfterDisconnect}");
            Assert.True(before);
        }
    }
}
