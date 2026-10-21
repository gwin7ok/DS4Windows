using System;
using System.Collections.Generic;
using System.Text;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-A(2)-A（Phase9-A2-Plan.md）: <c>Mapping.SetCurveAndDeadzone</c> の「ジャイロ → コントロール」（<c>GyroOutMode.Controls</c> の SX／SZ の補正）の<b>値の特性化テスト</b>（ゴールデン方式）。
    /// ジャイロの加速度（X・Z）の格子と、設定の場面（デッドゾーン、最大ゾーン、アンチデッドゾーン、感度、出力カーブ 1〜6）を組み合わせ、<b>今のコードの出力</b>をそのまま記録する。
    /// X と Z が同じ設定のときの値の違い（X と Z の非対称）は、別のゴールデンに記録する（共通化の前に、差があるかが分かる。差があれば、そのまま保つ）。
    /// 切り出し（<c>IGyroControlsProcessor</c>）で、値が 1 つでも変わると失敗する。
    /// </summary>
    public class Phase9A2GyroControlsGoldenTests
    {
        private const int Slot = 0;
        private static readonly int[] AccelGrid = { -200, -128, -100, -64, -30, -10, -3, 0, 3, 10, 30, 64, 100, 128, 200 };

        private readonly ITestOutputHelper _output;

        public Phase9A2GyroControlsGoldenTests(ITestOutputHelper output)
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

        // ---- 設定の場面 ----

        private static void Axis(BackingStore store, bool x, double dead, double max, double anti, double sens)
        {
            if (x)
            {
                store.SXDeadzone[Slot] = dead;
                store.SXMaxzone[Slot] = max;
                store.SXAntiDeadzone[Slot] = anti;
                store.SXSens[Slot] = sens;
            }
            else
            {
                store.SZDeadzone[Slot] = dead;
                store.SZMaxzone[Slot] = max;
                store.SZAntiDeadzone[Slot] = anti;
                store.SZSens[Slot] = sens;
            }
        }

        private static void Both(BackingStore store, double dead, double max, double anti, double sens)
        {
            Axis(store, true, dead, max, anti, sens);
            Axis(store, false, dead, max, anti, sens);
        }

        private static IEnumerable<Scenario> Scenarios()
        {
            (string Name, double Dead, double Max, double Anti, double Sens)[] zones =
            {
                ("none", 0.0, 1.0, 0.0, 1.0),
                ("sens-1.5", 0.0, 1.0, 0.0, 1.5),
                ("sens-0.5", 0.0, 1.0, 0.0, 0.5),
                ("dz-0.1", 0.1, 1.0, 0.0, 1.0),
                ("dz-0.3-anti-0.2", 0.3, 1.0, 0.2, 1.0),
                ("dz-0.1-max-0.8", 0.1, 0.8, 0.0, 1.0),
                ("dz-0.1-anti-0.1-max-0.7-sens-1.3", 0.1, 0.7, 0.1, 1.3),
                ("max-0.5-only", 0.0, 0.5, 0.0, 1.0),
                ("anti-0.3-only", 0.0, 1.0, 0.3, 1.0),
                ("dz-0.5-max-0.6", 0.5, 0.6, 0.0, 1.0),
            };

            foreach (var z in zones)
            {
                var zone = z;
                yield return new Scenario($"gyro {zone.Name}", s => Both(s, zone.Dead, zone.Max, zone.Anti, zone.Sens));
            }

            for (int mode = 1; mode <= 5; mode++)
            {
                int curve = mode;
                yield return new Scenario($"gyro none out-curve-{curve}", s =>
                {
                    Both(s, 0.0, 1.0, 0.0, 1.0);
                    s.setSXOutCurveMode(Slot, curve);
                    s.setSZOutCurveMode(Slot, curve);
                });
                yield return new Scenario($"gyro dz-0.1-anti-0.1 out-curve-{curve}", s =>
                {
                    Both(s, 0.1, 1.0, 0.1, 1.0);
                    s.setSXOutCurveMode(Slot, curve);
                    s.setSZOutCurveMode(Slot, curve);
                });
            }

            yield return new Scenario("gyro none out-curve-6-bezier", s =>
            {
                Both(s, 0.0, 1.0, 0.0, 1.0);
                s.sxOutBezierCurveObj[Slot].InitBezierCurve("0.7,0.28,1.0,0.55", BezierCurve.AxisType.SA, true);
                s.szOutBezierCurveObj[Slot].InitBezierCurve("0.25,0.1,0.25,1.0", BezierCurve.AxisType.SA, true);
                s.setSXOutCurveMode(Slot, 6);
                s.setSZOutCurveMode(Slot, 6);
            });

            // X と Z で設定が違う場面（取り違えの検出）
            yield return new Scenario("gyro X and Z differ", s =>
            {
                Axis(s, true, 0.1, 1.0, 0.0, 1.2);
                Axis(s, false, 0.3, 0.8, 0.2, 0.7);
                s.setSXOutCurveMode(Slot, 2);
                s.setSZOutCurveMode(Slot, 3);
            });
            yield return new Scenario("gyro only X curve", s =>
            {
                Both(s, 0.0, 1.0, 0.0, 1.0);
                s.setSXOutCurveMode(Slot, 4);
            });
            yield return new Scenario("gyro only Z curve", s =>
            {
                Both(s, 0.0, 1.0, 0.0, 1.0);
                s.setSZOutCurveMode(Slot, 5);
            });
        }

        // ---- 実行 ----

        private static (int X, int Z) Run(Mapping mapper, BackingStore store, int accelX, int accelZ)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(store);
            var cState = new DS4State();
            var dState = new DS4State();
            cState.Motion.accelX = accelX;
            cState.Motion.accelZ = accelZ;
            cState.Motion.outputGyroControls = true;
            DS4State result = mapper.SetCurveAndDeadzone(Slot, cState, dState, settings);
            return (result.Motion.outputAccelX, result.Motion.outputAccelZ);
        }

        private static BackingStore NewStore(Scenario scenario)
        {
            var store = new BackingStore(new EnvironmentService());
            store.gyroOutMode[Slot] = GyroOutMode.Controls;
            scenario.Configure(store);
            return store;
        }

        [Fact]
        public void TheGyroControls_MatchTheGolden_ForEveryScenario()
        {
            var text = new StringBuilder();
            var mapper = TestMappingFactory.Create();
            foreach (Scenario scenario in Scenarios())
            {
                BackingStore store = NewStore(scenario);
                text.AppendLine("## " + scenario.Name);
                foreach (int x in AccelGrid)
                {
                    foreach (int z in AccelGrid)
                    {
                        var r = Run(mapper, store, x, z);
                        text.AppendLine($"{x},{z} -> X {r.X} Z {r.Z}");
                    }
                }
            }

            GoldenFile.AssertMatches("phase9a2-gyro-controls.state.txt", text.ToString(), _output);
        }

        /// <summary>X と Z に同じ設定・同じ入力（x＝z）を与えたときの値の違い（X と Z の非対称）の一覧。差がなければ「差なし」。差は、そのまま保つ。</summary>
        [Fact]
        public void TheDifferencesBetweenXAndZ_MatchTheGolden()
        {
            var text = new StringBuilder();
            var mapper = TestMappingFactory.Create();
            foreach (Scenario scenario in Scenarios())
            {
                BackingStore store = NewStore(scenario);
                int differences = 0;
                var first = new List<string>();
                foreach (int v in AccelGrid)
                {
                    var r = Run(mapper, store, v, v);
                    if (r.X != r.Z)
                    {
                        differences++;
                        if (first.Count < 5) first.Add($"{v}: X {r.X} Z {r.Z}");
                    }
                }

                text.AppendLine($"## {scenario.Name}: {(differences == 0 ? "差なし" : differences + " 点で差")}");
                foreach (string line in first) text.AppendLine("  " + line);
            }

            GoldenFile.AssertMatches("phase9a2-gyro-controls-xz.state.txt", text.ToString(), _output);
        }
    }
}
