using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-A(1) (a)（Phase9-A1-Plan.md §3）: <c>Mapping.SetCurveAndDeadzone</c> のスティックとトリガーの補正の<b>値の特性化テスト</b>（ゴールデン方式）。
    /// 格子の入力（スティックは X・Y を 12 点ずつの 144 点、トリガーは 10 点）と、設定の場面（デッドゾーンの種類・値、アンチデッドゾーン、最大ゾーン、最大出力、
    /// 感度、スクエアスティック、出力カーブ、回転、ファズ、トリガーの補正）を組み合わせ、<b>今のコードの出力</b>をそのままゴールデンに記録する。
    /// 左右のスティックが同じ設定のときの値の違い（左右の非対称）は、別のゴールデンに記録する（共通化の前に、差があるかが分かる。差があれば、そのまま保つ）。
    /// 共通化（9-A(1) の (b)〜(d)）で、値が 1 つでも変わると失敗する。値を変える意図があるときは、旧実装と比較し、承認を得てから、ゴールデンを更新する。
    /// アンチスナップバックは時刻に依存するため、対象外（9-A(2) の前に、時刻を差し替えられるようにしてから扱う）。ジャイロ→コントロールは 9-A(2)。
    /// </summary>
    public class Phase9A1SetCurveAndDeadzoneGoldenTests
    {
        private const int Slot = 0;
        private static readonly byte[] StickGrid = { 0, 1, 32, 64, 100, 127, 128, 129, 160, 200, 254, 255 };
        private static readonly byte[] TriggerGrid = { 0, 1, 10, 50, 100, 127, 128, 200, 254, 255 };

        private readonly ITestOutputHelper _output;

        public Phase9A1SetCurveAndDeadzoneGoldenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private sealed class Scenario
        {
            public Scenario(string name, Action<BackingStore> configure)
            {
                Name = name;
                Configure = configure;
            }

            public string Name { get; }
            public Action<BackingStore> Configure { get; }
        }

        private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        // ---- 設定の場面 ----

        private static void Stick(BackingStore store, StickDeadZoneInfo.DeadZoneType type, int dead, int anti, int maxZone, double maxOutput, bool force, double verticalScale = 100.0)
        {
            foreach (StickDeadZoneInfo info in new[] { store.lsModInfo[Slot], store.rsModInfo[Slot] })
            {
                info.deadzoneType = type;
                info.deadZone = dead;
                info.antiDeadZone = anti;
                info.maxZone = maxZone;
                info.maxOutput = maxOutput;
                info.maxOutputForce = force;
                info.verticalScale = verticalScale;

                // 軸ごとのデッドゾーン（Axial）。X と Y で値を変えて、入れ替わりを検出できるようにする
                info.xAxisDeadInfo.deadZone = dead;
                info.xAxisDeadInfo.antiDeadZone = anti;
                info.xAxisDeadInfo.maxZone = maxZone;
                info.xAxisDeadInfo.maxOutput = maxOutput;
                info.yAxisDeadInfo.deadZone = dead + 5;
                info.yAxisDeadInfo.antiDeadZone = anti + 5;
                info.yAxisDeadInfo.maxZone = Math.Max(10, maxZone - 5);
                info.yAxisDeadInfo.maxOutput = Math.Max(10.0, maxOutput - 5.0);
            }
        }

        private static IEnumerable<Scenario> StickScenarios()
        {
            var radial = StickDeadZoneInfo.DeadZoneType.Radial;
            var axial = StickDeadZoneInfo.DeadZoneType.Axial;

            (string Name, int Dead, int Anti, int MaxZone, double MaxOutput, bool Force)[] zones =
            {
                ("none", 0, 0, 100, 100.0, false),
                ("dz10", 10, 0, 100, 100.0, false),
                ("dz10-anti20", 10, 20, 100, 100.0, false),
                ("dz40-anti20-max80", 40, 20, 80, 100.0, false),
                ("dz10-anti20-out70", 10, 20, 100, 70.0, false),
                ("dz10-anti20-out70-force", 10, 20, 100, 70.0, true),
                ("dz25-anti15-max90-out85-force", 25, 15, 90, 85.0, true),
            };

            foreach (var type in new[] { radial, axial })
            {
                foreach (var z in zones)
                {
                    var zone = z;
                    var t = type;
                    yield return new Scenario($"stick {t} {zone.Name}", s => Stick(s, t, zone.Dead, zone.Anti, zone.MaxZone, zone.MaxOutput, zone.Force));
                }
            }

            yield return new Scenario("stick radial dz10-anti20 vertical-scale-150", s => Stick(s, radial, 10, 20, 100, 100.0, false, 150.0));
            yield return new Scenario("stick radial dz10-anti20 vertical-scale-60", s => Stick(s, radial, 10, 20, 100, 100.0, false, 60.0));
            yield return new Scenario("stick radial dz10 sensitivity-1.5", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.LSSens[Slot] = 1.5;
                s.RSSens[Slot] = 1.5;
            });
            yield return new Scenario("stick radial dz10 sensitivity-0.6", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.LSSens[Slot] = 0.6;
                s.RSSens[Slot] = 0.6;
            });
            yield return new Scenario("stick axial dz10 sensitivity-1.5 (ignored for axial)", s =>
            {
                Stick(s, axial, 10, 0, 100, 100.0, false);
                s.LSSens[Slot] = 1.5;
                s.RSSens[Slot] = 1.5;
            });

            yield return new Scenario("stick radial dz10 square-roundness-5", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.squStickInfo[Slot].lsMode = true;
                s.squStickInfo[Slot].rsMode = true;
                s.squStickInfo[Slot].lsRoundness = 5.0;
                s.squStickInfo[Slot].rsRoundness = 5.0;
            });
            yield return new Scenario("stick radial dz10 square-roundness-12", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.squStickInfo[Slot].lsMode = true;
                s.squStickInfo[Slot].rsMode = true;
                s.squStickInfo[Slot].lsRoundness = 12.0;
                s.squStickInfo[Slot].rsRoundness = 12.0;
            });

            for (int mode = 1; mode <= 5; mode++)
            {
                int curve = mode;
                yield return new Scenario($"stick radial dz10 out-curve-{curve}", s =>
                {
                    Stick(s, radial, 10, 0, 100, 100.0, false);
                    s.setLsOutCurveMode(Slot, curve);
                    s.setRsOutCurveMode(Slot, curve);
                });
            }

            yield return new Scenario("stick radial dz10 rotation-30", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.LSRotation[Slot] = 30.0;
                s.RSRotation[Slot] = 30.0;
            });
            yield return new Scenario("stick radial dz10 rotation-minus-45", s =>
            {
                Stick(s, radial, 10, 0, 100, 100.0, false);
                s.LSRotation[Slot] = -45.0;
                s.RSRotation[Slot] = -45.0;
            });
        }

        private static IEnumerable<Scenario> TriggerScenarios()
        {
            (string Name, byte Dead, int Anti, int MaxZone, double MaxOutput)[] zones =
            {
                ("none", 0, 0, 100, 100.0),
                ("dz20", 20, 0, 100, 100.0),
                ("dz20-anti10", 20, 10, 100, 100.0),
                ("dz20-anti10-max90", 20, 10, 90, 100.0),
                ("dz20-anti10-max90-out60", 20, 10, 90, 60.0),
                ("dz50-anti30-max70-out80", 50, 30, 70, 80.0),
            };

            foreach (var z in zones)
            {
                var zone = z;
                yield return new Scenario($"trigger {zone.Name}", s =>
                {
                    foreach (TriggerDeadZoneZInfo info in new[] { s.l2ModInfo[Slot], s.r2ModInfo[Slot] })
                    {
                        info.deadZone = zone.Dead;
                        info.antiDeadZone = zone.Anti;
                        info.maxZone = zone.MaxZone;
                        info.maxOutput = zone.MaxOutput;
                    }
                });
            }

            yield return new Scenario("trigger sensitivity-1.4", s =>
            {
                s.l2Sens[Slot] = 1.4;
                s.r2Sens[Slot] = 1.4;
            });
            yield return new Scenario("trigger sensitivity-0.7", s =>
            {
                s.l2Sens[Slot] = 0.7;
                s.r2Sens[Slot] = 0.7;
            });

            for (int mode = 1; mode <= 5; mode++)
            {
                int curve = mode;
                yield return new Scenario($"trigger out-curve-{curve}", s =>
                {
                    s.setL2OutCurveMode(Slot, curve);
                    s.setR2OutCurveMode(Slot, curve);
                });
            }

            yield return new Scenario("trigger dz20-anti10-max90 out-curve-2", s =>
            {
                foreach (TriggerDeadZoneZInfo info in new[] { s.l2ModInfo[Slot], s.r2ModInfo[Slot] })
                {
                    info.deadZone = 20;
                    info.antiDeadZone = 10;
                    info.maxZone = 90;
                }

                s.setL2OutCurveMode(Slot, 2);
                s.setR2OutCurveMode(Slot, 2);
            });
        }

        // ---- 実行 ----

        private static DS4State Run(Mapping mapper, BackingStore store, byte lx, byte ly, byte rx, byte ry, byte l2, byte r2)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(store);
            var cState = new DS4State();
            var dState = new DS4State();
            cState.LX = lx; cState.LY = ly; cState.RX = rx; cState.RY = ry;
            cState.L2 = l2; cState.R2 = r2;
            return mapper.SetCurveAndDeadzone(Slot, cState, dState, settings);
        }

        private static BackingStore NewStore(Scenario scenario)
        {
            var store = new BackingStore(new EnvironmentService());
            scenario.Configure(store);
            return store;
        }

        private static string StickDump(Scenario scenario, bool leftOnly = false)
        {
            var mapper = TestMappingFactory.Create();
            BackingStore store = NewStore(scenario);
            var text = new StringBuilder();
            text.AppendLine("## " + scenario.Name);
            foreach (byte x in StickGrid)
            {
                foreach (byte y in StickGrid)
                {
                    DS4State result = Run(mapper, store, x, y, x, y, 0, 0);
                    text.AppendLine($"{x},{y} -> L {result.LX},{result.LY} R {result.RX},{result.RY}");
                }
            }

            return text.ToString();
        }

        private static string TriggerDump(Scenario scenario)
        {
            var mapper = TestMappingFactory.Create();
            BackingStore store = NewStore(scenario);
            var text = new StringBuilder();
            text.AppendLine("## " + scenario.Name);
            foreach (byte v in TriggerGrid)
            {
                DS4State result = Run(mapper, store, 128, 128, 128, 128, v, v);
                text.AppendLine($"{v} -> L2 {result.L2} R2 {result.R2}");
            }

            return text.ToString();
        }

        [Fact]
        public void TheStickCorrection_MatchesTheGolden_ForEveryScenario()
        {
            var text = new StringBuilder();
            foreach (Scenario scenario in StickScenarios())
                text.Append(StickDump(scenario));

            GoldenFile.AssertMatches("phase9a1-sticks.state.txt", text.ToString(), _output);
        }

        [Fact]
        public void TheTriggerCorrection_MatchesTheGolden_ForEveryScenario()
        {
            var text = new StringBuilder();
            foreach (Scenario scenario in TriggerScenarios())
                text.Append(TriggerDump(scenario));

            GoldenFile.AssertMatches("phase9a1-triggers.state.txt", text.ToString(), _output);
        }

        /// <summary>
        /// 左右のスティックに同じ設定・同じ入力を与えたときの値の違い（左右の非対称）の一覧。差がなければ「差なし」。
        /// 共通化の前に、左右のコードに差があるかが分かる。差は、そのまま保つ（共通化しても、この一覧が変わらないこと）。
        /// </summary>
        [Fact]
        public void TheDifferencesBetweenTheLeftAndRightStick_MatchTheGolden()
        {
            var text = new StringBuilder();
            foreach (Scenario scenario in StickScenarios())
            {
                var mapper = TestMappingFactory.Create();
                BackingStore store = NewStore(scenario);
                int differences = 0;
                var first = new List<string>();
                foreach (byte x in StickGrid)
                {
                    foreach (byte y in StickGrid)
                    {
                        DS4State r = Run(mapper, store, x, y, x, y, 0, 0);
                        if (r.LX != r.RX || r.LY != r.RY)
                        {
                            differences++;
                            if (first.Count < 5) first.Add($"{x},{y}: L {r.LX},{r.LY} R {r.RX},{r.RY}");
                        }
                    }
                }

                text.AppendLine($"## {scenario.Name}: {(differences == 0 ? "差なし" : differences + " 点で差")}");
                foreach (string line in first) text.AppendLine("  " + line);
            }

            foreach (Scenario scenario in TriggerScenarios())
            {
                var mapper = TestMappingFactory.Create();
                BackingStore store = NewStore(scenario);
                int differences = 0;
                foreach (byte v in TriggerGrid)
                {
                    DS4State r = Run(mapper, store, 128, 128, 128, 128, v, v);
                    if (r.L2 != r.R2) differences++;
                }

                text.AppendLine($"## {scenario.Name} (L2 と R2): {(differences == 0 ? "差なし" : differences + " 点で差")}");
            }

            GoldenFile.AssertMatches("phase9a1-left-right.state.txt", text.ToString(), _output);
        }

        /// <summary>ファズ（スティックの小さな揺れを無視する）は、前のフレームの値に依存する。時刻に依存しないため、決まった列の出力を記録する。</summary>
        [Fact]
        public void TheFuzz_ForASequenceOfFrames_MatchesTheGolden()
        {
            var text = new StringBuilder();
            (byte X, byte Y)[] sequence =
            {
                (128, 128), (130, 128), (133, 129), (140, 130), (141, 131), (128, 128), (200, 128), (201, 129), (203, 128), (128, 128), (60, 200), (61, 201), (255, 0),
            };

            foreach (int fuzz in new[] { 2, 6 })
            {
                var mapper = TestMappingFactory.Create();
                var store = new BackingStore(new EnvironmentService());
                Stick(store, StickDeadZoneInfo.DeadZoneType.Radial, 0, 0, 100, 100.0, false);
                store.lsModInfo[Slot].fuzz = fuzz;
                store.rsModInfo[Slot].fuzz = fuzz;

                text.AppendLine($"## fuzz {fuzz}");
                foreach (var frame in sequence)
                {
                    DS4State r = Run(mapper, store, frame.X, frame.Y, frame.X, frame.Y, 0, 0);
                    text.AppendLine($"{frame.X},{frame.Y} -> L {r.LX},{r.LY} R {r.RX},{r.RY}");
                }
            }

            GoldenFile.AssertMatches("phase9a1-fuzz.state.txt", text.ToString(), _output);
        }
    }
}
