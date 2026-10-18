using DS4Windows;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7b W6（Phase7b-Plan.md §2 項目 2、§8。Q7＝A、Q8＝B、Q9＝A）: L2／R2 のデッドゾーンの計算方式（Scaled／Cutoff）の検査。
    /// - <see cref="TriggerCutoffCurve"/> の式（デッドゾーン以下は 0。超えたら、押し込み量と、アンチデッドゾーンの大きい方）。
    /// - <c>Mapping.SetCurveAndDeadzone</c> が、モードに応じて、現行（Scaled）と新方式（Cutoff）を使い分ける。
    /// - XML: 既定（Scaled）のときは要素を書かない。Cutoff のときだけ書き、往復できる。不明な値は Scaled。
    /// </summary>
    public class Phase7bW6TriggerDeadZoneModeTests
    {
        // ---- Cutoff の式 ----

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 0)]
        [InlineData(204, 0)]   // デッドゾーンちょうどは 0
        [InlineData(205, 205)] // 超えた直後から、押し込み量そのまま
        [InlineData(230, 230)]
        [InlineData(255, 255)]
        public void Cutoff_DeadZone80Percent_ZeroBelow_ThenTheActualValue(int input, int expected)
        {
            Assert.Equal(expected, TriggerCutoffCurve.Apply((byte)input, 204, 0, 100, 100.0));
        }

        [Fact]
        public void Cutoff_ZeroDeadZone_PassesTheInputThrough()
        {
            Assert.Equal(0, TriggerCutoffCurve.Apply(0, 0, 0, 100, 100.0));
            Assert.Equal(1, TriggerCutoffCurve.Apply(1, 0, 0, 100, 100.0));
            Assert.Equal(128, TriggerCutoffCurve.Apply(128, 0, 0, 100, 100.0));
        }

        [Fact]
        public void Cutoff_AntiDeadZone_TakesTheLargerOfTheSettingAndTheActualValue()
        {
            // アンチデッドゾーン 90%（229.5 → 230）。実際の 205（80%）より大きいので、アンチデッドゾーンが出る
            Assert.Equal(230, TriggerCutoffCurve.Apply(205, 204, 90, 100, 100.0));
            // アンチデッドゾーン 10%（25.5）。実際の 205 の方が大きいので、実際の値が出る
            Assert.Equal(205, TriggerCutoffCurve.Apply(205, 204, 10, 100, 100.0));
            // デッドゾーン以下は、アンチデッドゾーンがあっても 0
            Assert.Equal(0, TriggerCutoffCurve.Apply(204, 204, 90, 100, 100.0));
            Assert.Equal(0, TriggerCutoffCurve.Apply(0, 0, 90, 100, 100.0));
        }

        [Fact]
        public void Cutoff_MaxZone_ReachesFullAtTheMaxZoneInput()
        {
            // 最大ゾーン 50%（127.5）。デッドゾーン 50 を超えた入力 100 は 100/127.5、入力 200 は頭打ちで 255
            Assert.Equal(200, TriggerCutoffCurve.Apply(100, 50, 0, 50, 100.0));
            Assert.Equal(255, TriggerCutoffCurve.Apply(200, 50, 0, 50, 100.0));
            // 最大ゾーン 0% は、デッドゾーンを超えたら全開
            Assert.Equal(255, TriggerCutoffCurve.Apply(60, 50, 0, 0, 100.0));
        }

        [Fact]
        public void Cutoff_MaxOutput_CapsTheOutput_ButTheAntiDeadZoneStillWinsWhenLarger()
        {
            Assert.Equal(128, TriggerCutoffCurve.Apply(255, 204, 0, 100, 50.0)); // 127.5 → 128
            Assert.Equal(204, TriggerCutoffCurve.Apply(255, 204, 80, 100, 50.0)); // アンチデッドゾーン 80% が最大出力 50% より大きい（現行 Scaled と同様）
        }

        // ---- Mapping の使い分け ----

        private const int Device = 7;

        private static DS4State Run(TriggerDeadZoneMode mode, byte input, byte deadZone, int antiDeadZone)
        {
            var settings = TestHost.GetService<IProfileSettingsService>();
            var mapping = TestHost.GetService<IMapping>();
            var info = settings.L2ModInfo[Device];
            try
            {
                info.Reset();
                info.deadZone = deadZone;
                info.antiDeadZone = antiDeadZone;
                info.deadZoneMode = mode;

                var cState = new DS4State { L2 = input };
                var dState = new DS4State();
                mapping.SetCurveAndDeadzone(Device, cState, dState, settings);
                return dState;
            }
            finally
            {
                info.Reset();
            }
        }

        [Fact]
        public void Mapping_Cutoff_OutputsTheActualValueAboveTheDeadZone_AndZeroBelow()
        {
            Assert.Equal(205, Run(TriggerDeadZoneMode.Cutoff, 205, 204, 0).L2);
            Assert.Equal(0, Run(TriggerDeadZoneMode.Cutoff, 204, 204, 0).L2);
            Assert.Equal(0, Run(TriggerDeadZoneMode.Cutoff, 100, 204, 0).L2);
            Assert.Equal(230, Run(TriggerDeadZoneMode.Cutoff, 205, 204, 90).L2);
        }

        [Fact]
        public void Mapping_Scaled_KeepsTheCurrentBehavior_StretchingFromTheDeadZone()
        {
            // 現行: デッドゾーンを超えた点が 0% の起点。205 は (205−204)／(255−204)≒2%（約 5）になる（Cutoff の 205 とは違う）
            byte scaled = Run(TriggerDeadZoneMode.Scaled, 205, 204, 0).L2;
            Assert.InRange((int)scaled, 4, 6);
            Assert.Equal(0, Run(TriggerDeadZoneMode.Scaled, 204, 204, 0).L2);
            Assert.Equal(255, Run(TriggerDeadZoneMode.Scaled, 255, 204, 0).L2);
        }

        // ---- 既定値・XML ----

        [Fact]
        public void DefaultMode_IsScaled_AndResetRestoresIt()
        {
            var info = new TriggerDeadZoneZInfo();
            Assert.Equal(TriggerDeadZoneMode.Scaled, info.deadZoneMode);
            Assert.Equal(TriggerDeadZoneMode.Scaled, TriggerDeadZoneZInfo.DEFAULT_DEADZONE_MODE);

            info.DeadZoneMode = TriggerDeadZoneMode.Cutoff;
            Assert.Equal(TriggerDeadZoneMode.Cutoff, info.deadZoneMode);
            info.Reset();
            Assert.Equal(TriggerDeadZoneMode.Scaled, info.deadZoneMode);
        }

        [Fact]
        public void Xml_DefaultMode_IsNotWritten_AndCutoffRoundTrips()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                // 既定: 要素を書かない（既存のプロファイルの XML は変わらない）
                string defaultXml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "P7bDefault");
                Assert.DoesNotContain("DeadZoneMode", defaultXml);
                Assert.Equal(TriggerDeadZoneMode.Scaled, PersistenceHarness.LoadProfile(ctx, defaultXml, "P7bDefaultLoad").l2ModInfo[PersistenceHarness.Slot].deadZoneMode);

                // L2 だけ Cutoff: L2 の要素だけを書き、往復できる
                BackingStore store = PersistenceHarness.NewStore();
                store.l2ModInfo[PersistenceHarness.Slot].deadZoneMode = TriggerDeadZoneMode.Cutoff;
                string xml = PersistenceHarness.SaveProfile(ctx, store, "P7bCutoff");
                Assert.Contains("<L2DeadZoneMode>Cutoff</L2DeadZoneMode>", xml);
                Assert.DoesNotContain("R2DeadZoneMode", xml);

                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "P7bCutoffLoad");
                Assert.Equal(TriggerDeadZoneMode.Cutoff, loaded.l2ModInfo[PersistenceHarness.Slot].deadZoneMode);
                Assert.Equal(TriggerDeadZoneMode.Scaled, loaded.r2ModInfo[PersistenceHarness.Slot].deadZoneMode);

                // R2 も Cutoff
                store.r2ModInfo[PersistenceHarness.Slot].deadZoneMode = TriggerDeadZoneMode.Cutoff;
                string both = PersistenceHarness.SaveProfile(ctx, store, "P7bBoth");
                Assert.Contains("<R2DeadZoneMode>Cutoff</R2DeadZoneMode>", both);
            }
        }

        [Fact]
        public void Xml_UnknownModeValue_FallsBackToScaled()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.l2ModInfo[PersistenceHarness.Slot].deadZoneMode = TriggerDeadZoneMode.Cutoff;
                string xml = PersistenceHarness.SaveProfile(ctx, store, "P7bUnknown")
                    .Replace("<L2DeadZoneMode>Cutoff</L2DeadZoneMode>", "<L2DeadZoneMode>Bogus</L2DeadZoneMode>");

                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "P7bUnknownLoad");
                Assert.Equal(TriggerDeadZoneMode.Scaled, loaded.l2ModInfo[PersistenceHarness.Slot].deadZoneMode);
            }
        }
    }
}
