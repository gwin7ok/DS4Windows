using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    // Issue8-1(3)是正（仕様⑤: トリガー構成ボタンの出力抑制期間の延長）の回帰防止テスト。
    // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-Issue8-1-3-Fix-Plan.md
    //
    // 実運用の入力評価（MapCustomAction）自体を直接駆動するテストは困難だが、本Issueの是正対象である
    // 抑制の集合と CheckForSpecialActionSuppression（private、reflection経由で呼び出す）の
    // 2点に絞って、抑制ロジックそのものの正しさを検証する。
    // Phase8-Step4 W4: 抑制の集合の所有が Mapping（suppressedTriggerButtons）からエンジンに移ったため、エンジン経由で操作する。
    public class MappingSpecialActionSuppressionTests
    {
        // 他のテストと状態を共有しない、独立のエンジンと Mapping の instance を使う
        private static readonly ITriggerDispatchEngine Engine = new TriggerDispatchEngine(new ActionBindingRegistry(),
            TestHost.GetService<IManagedActionManager>(), new BindingMigrationOptions(), null);
        private static readonly Mapping Mapper = TestMappingFactory.Create(engine: Engine);

        // テスト間の副作用（他テストが同じデバイス添字・ボタンを使っている可能性）を避けるため、
        // 本テスト専用のデバイス添字・ボタンの組み合わせを使用する。
        private const int TestDevice = 3;

        private sealed class StubInput : ITriggerInput
        {
            public HashSet<DS4Controls> Active { get; } = new HashSet<DS4Controls>();
            public int Device => TestDevice;
            public bool IsActive(DS4Controls control) => Active.Contains(control);
            public bool WasActive(DS4Controls control) => false;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow => DateTime.UtcNow;
            public long TickCount64 => Environment.TickCount64;
        }

        // 押されているボタンがないものとして、抑制の集合を空にする（実運用の解除パスと同じ ReleaseSuppressed）
        private static void ClearSuppressed() => Engine.ReleaseSuppressed(TestDevice, new StubInput());

        private static bool InvokeCheckForSpecialActionSuppression(int device, DS4Controls control)
        {
            MethodInfo method = typeof(Mapping).GetMethod(
                "CheckForSpecialActionSuppression",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(method); // メソッド名変更等でシグネチャが失われた場合に検出する

            // Issue8-1(3)是正後の実装は cState/eState/tp/fieldMap を一切参照しないため、
            // null を渡しても安全に呼び出せる（抑制の集合の内容のみで判定する）。
            object result = method.Invoke(Mapper, new object[] { device, control, null, null, null, null });
            return (bool)result;
        }

        [Fact]
        public void CheckForSpecialActionSuppression_ReturnsFalse_WhenButtonNotSuppressed()
        {
            ClearSuppressed();

            bool suppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);

            Assert.False(suppressed);
        }

        [Fact]
        public void CheckForSpecialActionSuppression_ReturnsTrue_WhenButtonAddedToSuppressedSet()
        {
            // トリガー成立時（MapCustomAction内）に相当する操作を模擬する。
            Engine.AddSuppressed(TestDevice, new[] { DS4Controls.L2 });

            try
            {
                bool suppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);
                Assert.True(suppressed);
            }
            finally
            {
                ClearSuppressed();
            }
        }

        [Fact]
        public void SuppressedTriggerButtons_ReleasingOneButton_DoesNotAffectOtherStillHeldButton()
        {
            // Issue8-1(3)是正の核心シナリオ: L2+PSのコンボが成立し、PSだけが先に離された場合、
            // まだ押され続けているL2自身の抑制は解除されない（L2は個別に離されるまで抑制継続）。
            Engine.AddSuppressed(TestDevice, new[] { DS4Controls.L2, DS4Controls.PS });

            try
            {
                // PSのみを解除（実際の解除パスでは、IsActive が false を返したボタンのみを外す）。
                var input = new StubInput();
                input.Active.Add(DS4Controls.L2);
                Engine.ReleaseSuppressed(TestDevice, input);

                bool l2StillSuppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.L2);
                bool psStillSuppressed = InvokeCheckForSpecialActionSuppression(TestDevice, DS4Controls.PS);

                Assert.True(l2StillSuppressed);  // L2はまだ押され続けているため抑制継続
                Assert.False(psStillSuppressed); // PSは離されたため抑制解除
            }
            finally
            {
                ClearSuppressed();
            }
        }

        [Fact]
        public void SuppressedTriggerButtons_AreHeldPerDevice_AndOwnedByTheEngine()
        {
            // 抑制の集合はエンジンの所有（デバイスごと）。Mapping は集合を持たない（Phase8-Step4 W4）
            Assert.Null(typeof(Mapping).GetField("suppressedTriggerButtons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

            Engine.AddSuppressed(TestDevice, new[] { DS4Controls.L2 });
            try
            {
                for (int device = 0; device < ControllerLimits.MaxControllerCount; device++)
                    Assert.Equal(device == TestDevice, Engine.IsSuppressed(device, DS4Controls.L2));
            }
            finally
            {
                ClearSuppressed();
            }
        }
    }
}
