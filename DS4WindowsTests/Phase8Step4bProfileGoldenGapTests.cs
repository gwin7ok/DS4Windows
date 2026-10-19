using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4b W1（Phase8-Step4b-Plan.md §3.1。4b-0）: プロファイルの読込・保存（<c>LegacyProfileXmlSerializer.LoadProfile</c>／<c>SaveProfile</c>）の
    /// 節ごとの分解（W2 以降）の前に、既存のゴールデン（<c>Phase7Step10cGoldenPersistenceTests</c>）で未検証だった節を固定する。
    /// <list type="bullet">
    /// <item><description>gap: 状態の自動入力（<c>BackingStoreStateFiller</c>）が変えられない項目（曲線のモード＝private なフィールド、ライトバーのモード、
    /// 特殊アクション［<c>ProfileActions</c>］、ステアリングホイールの平滑化、タッチパッドの反転の無効トリガー、外周の結合不感帯）を、明示的に変えた状態。</description></item>
    /// <item><description>legacy: 旧形式の読込専用の要素（<c>UseSAforMouse</c>、<c>UseTPforControls</c>、<c>GyroSmoothing</c>、<c>GyroSmoothingWeight</c>、<c>ShiftModifier</c>）の経路。</description></item>
    /// <item><description>minimal: 要素がほとんどない XML（欠けた設定の経路。読込後に欠けた設定を追記して保存し直す動作を含む）。</description></item>
    /// </list>
    /// ゴールデンは、製品のコードを変えない W1 の時点で生成する（初回はファイルを生成して失敗する。内容を確認してコミットし、再実行すること）。
    /// 分解の各 Wave では、これらのゴールデンの文面を更新しない。
    /// </summary>
    public class Phase8Step4bProfileGoldenGapTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step4bProfileGoldenGapTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string State(PersistenceGoldenContext ctx, BackingStore store)
            => ctx.Normalize(BackingStoreStateDumper.Dump(store));

        private static string StateDiffFromDefault(PersistenceGoldenContext ctx, BackingStore store)
            => BackingStoreStateDumper.Diff(State(ctx, PersistenceHarness.NewStore()), State(ctx, store));

        /// <summary>自動入力で変化させた状態に、自動入力が触れない項目を足して、すべて非既定値にする。</summary>
        private static BackingStore NewGapStore()
        {
            BackingStore store = PersistenceHarness.NewStore();
            BackingStoreStateFiller.Fill(store, PersistenceHarness.Slot);
            int slot = PersistenceHarness.Slot;

            // 曲線のモード（private なフィールド。設定用のメソッドでだけ変えられる）
            store.setLsOutCurveMode(slot, 3);
            store.setRsOutCurveMode(slot, 2);
            store.setL2OutCurveMode(slot, 4);
            store.setR2OutCurveMode(slot, 5);
            store.setSXOutCurveMode(slot, 1);
            store.setSZOutCurveMode(slot, 6);

            store.lightbarSettingInfo[slot].mode = LightbarMode.Passthru;

            // 特殊アクション（Step5 の入力になる節）
            store.profileActions[slot].Clear();
            store.profileActions[slot].Add("Golden_Macro");
            store.profileActions[slot].Add("Golden_Program");
            store.profileActions[slot].Add("Golden_Key");

            store.wheelSmoothInfo[slot].Enabled = true;
            store.touchDisInvertTriggers[slot] = new int[] { 1, 2 };
            store.lsModInfo[slot].outerBindDeadZone = 12.0;
            store.rsModInfo[slot].outerBindDeadZone = 34.0;
            return store;
        }

        private static string GapXml(PersistenceGoldenContext ctx)
        {
            // 入力は、ゴールデンの保存結果（なければ、いま作った状態を保存した結果）。保存側の変化に影響されず、読込だけを固定する
            string xml = GoldenFile.ReadOrNull("profile-gap.xml");
            return xml ?? PersistenceHarness.SaveProfile(ctx, NewGapStore(), "GoldenGapInput");
        }

        // ---- gap ----

        [Fact]
        public void Gap_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = PersistenceHarness.SaveProfile(ctx, NewGapStore(), "GoldenGap");
                GoldenFile.AssertMatches("profile-gap.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void Gap_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, GapXml(ctx), "GoldenGapLoad");
                GoldenFile.AssertMatches("profile-gap.state.txt", StateDiffFromDefault(ctx, loaded), _output);
            }
        }

        [Fact]
        public void Gap_LoadThenSave_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, GapXml(ctx), "GoldenGapLoad2");
                string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "GoldenGapResaved");
                GoldenFile.AssertMatches("profile-gap.resaved.xml", ctx.Normalize(resaved), _output);
            }
        }

        // ---- legacy（旧形式の読込専用の要素）----

        private static string LegacyXml(PersistenceGoldenContext ctx)
        {
            string xml = GapXml(ctx);
            // 新しい形式の出力モードの要素を取り除き（旧形式のフォールバックの経路を通す）、旧形式の要素を足す
            xml = Regex.Replace(xml, "<GyroOutputMode>.*?</GyroOutputMode>", string.Empty, RegexOptions.Singleline);
            xml = Regex.Replace(xml, "<TouchpadOutputMode>.*?</TouchpadOutputMode>", string.Empty, RegexOptions.Singleline);
            const string legacy =
                "<UseSAforMouse>True</UseSAforMouse><UseTPforControls>True</UseTPforControls>" +
                "<GyroSmoothing>True</GyroSmoothing><GyroSmoothingWeight>0.75</GyroSmoothingWeight><ShiftModifier>2</ShiftModifier>";
            int close = xml.LastIndexOf("</DS4Windows>");
            Assert.True(close > 0, "ルート要素の終わりが見つからない");
            return xml.Insert(close, legacy);
        }

        [Fact]
        public void Legacy_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, LegacyXml(ctx), "GoldenLegacyLoad");
                GoldenFile.AssertMatches("profile-legacy.state.txt", StateDiffFromDefault(ctx, loaded), _output);
            }
        }

        [Fact]
        public void Legacy_LoadThenSave_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, LegacyXml(ctx), "GoldenLegacyLoad2");
                string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "GoldenLegacyResaved");
                GoldenFile.AssertMatches("profile-legacy.resaved.xml", ctx.Normalize(resaved), _output);
            }
        }

        // ---- minimal（欠けた設定の経路）----

        private const string MinimalXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<DS4Windows app_version=\"0.0.0.0\">\n  <touchToggle>False</touchToggle>\n</DS4Windows>\n";

        [Fact]
        public void Minimal_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, MinimalXml, "GoldenMinimalLoad");
                GoldenFile.AssertMatches("profile-minimal.state.txt", StateDiffFromDefault(ctx, loaded), _output);
            }
        }

        [Fact]
        public void Minimal_Load_RewritesTheFileWithTheMissingSettings_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                PersistenceHarness.LoadProfile(ctx, MinimalXml, "GoldenMinimalRewrite");

                // 欠けた設定があり、読込が成功したとき、読込元のファイルに欠けた設定を追記して保存し直す（LoadProfile の最後）
                string rewritten = File.ReadAllText(Path.Combine(ctx.AppData, "Profiles", "GoldenMinimalRewrite.xml"));
                GoldenFile.AssertMatches("profile-minimal.rewritten.xml", ctx.Normalize(rewritten), _output);
            }
        }
    }
}
