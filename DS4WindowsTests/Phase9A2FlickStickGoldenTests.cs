using System;
using System.Collections.Generic;
using System.Globalization;
using DS4Windows.DI;
using DS4Windows.Services;
using System.Text;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-A(2)-C（Phase9-A2-Plan.md）: フリックスティック（<c>Mapping.ProcessFlickStick</c>／<c>HandleFlickStickAngle</c>／<c>WarpEaseOut</c>）の<b>値の特性化テスト</b>（ゴールデン方式）。
    /// スティックの列（中立→フリック→保持→戻す、旋回、小さな動き）と、設定（しきい値、フリック時間、補正係数、最小角度）、フレーム間隔の場面を組み合わせ、
    /// 1 フレームごとのマウス X 移動量と、持ち越しの端数をそのまま記録する。時刻には依存しない（フレーム間隔は <c>elapsedTime</c> で与える）。
    /// 切り出し前は、private メソッド（<c>ProcessFlickStick</c>）をリフレクションで呼んでゴールデンを作った。切り出し後は、<c>IFlickStickProcessor</c> を呼ぶ（<c>Rig.Step</c> だけが違う）。ゴールデンは同じ（1 バイトも変わらない）。
    /// </summary>
    public class Phase9A2FlickStickGoldenTests
    {
        private const int Device = 0;
        private readonly ITestOutputHelper _output;

        public Phase9A2FlickStickGoldenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private sealed class Rig
        {
            private readonly IFlickStickProcessor _processor = new FlickStickProcessor();

            public Rig()
            {
                var motion = new StickMotionState();
                Data = motion.FlickMappingData[Device];
            }

            public FlickStickMappingData Data { get; }

            /// <summary>1 フレーム分。マウス X の増分を返す。切り出し前は、<c>Mapping.ProcessFlickStick</c>（private）をリフレクションで呼んでいた。</summary>
            public double Step(FlickStickSettings settings, double elapsedTime, byte x, byte y, byte prevX, byte prevY)
            {
                double mouseDeltaX = 0.0;
                _processor.Process(Data, settings, elapsedTime, x, y, prevX, prevY, ref mouseDeltaX);
                return mouseDeltaX;
            }
        }

        // ---- スティックの列 ----

        private static List<(byte X, byte Y)> Flicks()
        {
            var s = new List<(byte, byte)>();
            for (int i = 0; i < 3; i++) s.Add((128, 128));
            for (int i = 0; i < 40; i++) s.Add((128, 0));        // 上へフリックして保持
            for (int i = 0; i < 5; i++) s.Add((128, 128));
            for (int i = 0; i < 30; i++) s.Add((255, 128));      // 右へ
            for (int i = 0; i < 5; i++) s.Add((128, 128));
            for (int i = 0; i < 30; i++) s.Add((0, 255));        // 左下へ
            for (int i = 0; i < 5; i++) s.Add((128, 128));
            for (int i = 0; i < 20; i++) s.Add((200, 40));       // しきい値付近
            for (int i = 0; i < 5; i++) s.Add((128, 128));
            return s;
        }

        private static List<(byte X, byte Y)> Turns()
        {
            var s = new List<(byte, byte)> { (128, 128), (128, 128) };
            for (int i = 0; i < 144; i++)   // 5 度ずつ 2 周（時計回り）
            {
                double a = i * 5.0 * Math.PI / 180.0;
                s.Add(((byte)Math.Round(128 + 127 * Math.Sin(a)), (byte)Math.Round(128 - 127 * Math.Cos(a))));
            }

            for (int i = 0; i < 72; i++)    // 逆回り 1 周
            {
                double a = -i * 5.0 * Math.PI / 180.0;
                s.Add(((byte)Math.Round(128 + 127 * Math.Sin(a)), (byte)Math.Round(128 - 127 * Math.Cos(a))));
            }

            s.Add((128, 128));
            return s;
        }

        private static List<(byte X, byte Y)> Small()
        {
            var s = new List<(byte, byte)>();
            for (int i = 0; i < 60; i++) s.Add(((byte)(128 + (i % 7) * 5), (byte)(128 - (i % 5) * 4)));   // しきい値に届かない動き
            for (int i = 0; i < 30; i++) s.Add(((byte)(128 + (i * 3) % 120), 128));                       // 徐々に外へ
            return s;
        }

        private static IEnumerable<(string Name, List<(byte X, byte Y)> Seq)> Sequences()
        {
            yield return ("flicks", Flicks());
            yield return ("turns", Turns());
            yield return ("small", Small());
        }

        // ---- 設定 ----

        private static IEnumerable<(string Name, FlickStickSettings Settings, double Elapsed)> Scenarios()
        {
            (string, double threshold, double time, double calibration, double minAngle, double elapsed)[] table =
            {
                ("default", 0.9, 0.1, 5.3, 0.0, 0.004),
                ("default-16ms", 0.9, 0.1, 5.3, 0.0, 0.016),
                ("threshold-0.5", 0.5, 0.1, 5.3, 0.0, 0.004),
                ("time-0.3-calibration-10", 0.9, 0.3, 10.0, 0.0, 0.004),
                ("minangle-0.5", 0.9, 0.1, 5.3, 0.5, 0.004),
                ("minangle-2-time-0.05", 0.9, 0.05, 5.3, 2.0, 0.008),
            };

            foreach (var t in table)
            {
                yield return (t.Item1, new FlickStickSettings
                {
                    flickThreshold = t.threshold,
                    flickTime = t.time,
                    realWorldCalibration = t.calibration,
                    minAngleThreshold = t.minAngle,
                }, t.elapsed);
            }
        }

        [Fact]
        public void TheFlickStick_MatchesTheGolden_ForEveryScenarioAndSequence()
        {
            var text = new StringBuilder();
            foreach (var scenario in Scenarios())
            {
                foreach (var sequence in Sequences())
                {
                    var rig = new Rig();
                    text.AppendLine($"## {scenario.Name} / {sequence.Name}");
                    byte prevX = 128, prevY = 128;
                    int frame = 0;
                    foreach (var point in sequence.Seq)
                    {
                        double delta = rig.Step(scenario.Settings, scenario.Elapsed, point.X, point.Y, prevX, prevY);
                        text.AppendLine($"{frame++}: {point.X},{point.Y} -> {Num(delta)} rem {Num(rig.Data.flickAngleRemainder)}");
                        prevX = point.X;
                        prevY = point.Y;
                    }
                }
            }

            GoldenFile.AssertMatches("phase9a2-flick-stick.state.txt", text.ToString(), _output);
        }
    }
}
