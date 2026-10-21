using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0（Phase10-Step4c-Plan.md §2 項目 4・§4 W0）: 立ち上がりのログ（SA 形式: Trace の <c>Trigger detected for SA '…'</c> ほか、Debug の <c>SpecialAction …</c>）を
    /// 出す Controls の束縛の種類を固定する特性化テスト。現行は、Controls の既存の種別（Key／Macro／Pad／Mouse／AnalogMouse／Extras）は出さず、
    /// SA 専用の種別（Special。W4〜W6）は、SA と同じ形で出す。製品コードは変えない。W2（項目 4: Controls の既存の種別にも出す）で、既存の種別の期待値を書き換える。
    /// NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替える（既存のログのテストと同じ方式）。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0LogTableTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        private static DS4State Pressed(params DS4Controls[] controls)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            foreach (DS4Controls c in controls)
            {
                switch (c)
                {
                    case DS4Controls.L1: state.L1 = true; break;
                    case DS4Controls.R1: state.R1 = true; break;
                    case DS4Controls.Cross: state.Cross = true; break;
                    case DS4Controls.Circle: state.Circle = true; break;
                    case DS4Controls.Square: state.Square = true; break;
                    case DS4Controls.Triangle: state.Triangle = true; break;
                    case DS4Controls.Share: state.Share = true; break;
                }
            }

            return state;
        }

        private static void RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);
        }

        private static List<string> Capture(Action action)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p10s4cw0") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                action();
                return target.Logs.ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        // SA 形式の立ち上がりのログ: Trace の「Trigger detected for SA '<名前>'」、Debug の「SpecialAction <種別>: …name=<名前>」
        private static bool HasSaStyleLog(IEnumerable<string> logs, string name)
            => logs.Any(l => l.Contains("Trigger detected for SA '" + name + "'") || (l.Contains("SpecialAction ") && l.Contains("name=" + name)));

        [Fact]
        public void TheExistingControlsKinds_LogNoSaStyleLines()
        {
            List<string> logs = Capture(() =>
            {
                using var t = new ControlsKeyTestScope();
                t.MapKey(DS4Controls.L1, 65);
                t.MapMacro(DS4Controls.R1, new[] { 65, 65 });
                t.MapButton(DS4Controls.Cross, X360Controls.A);
                t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse);
                t.MapButton(DS4Controls.Square, X360Controls.MouseUp);
                t.MapExtras(DS4Controls.Triangle, "1,2,0,0,0,0,0,0,0");
                t.PublishControlsOnly();

                RunFor(t, Pressed(), 150);   // Fresh-press ガード（Macro）
                RunFor(t, Pressed(DS4Controls.L1, DS4Controls.R1, DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square, DS4Controls.Triangle), 400);
                RunFor(t, Pressed(), 100);
            });

            foreach ((string prefix, DS4Controls control) in new[]
            {
                ("Synthetic_Key", DS4Controls.L1), ("Synthetic_Macro", DS4Controls.R1), ("Synthetic_Pad", DS4Controls.Cross),
                ("Synthetic_Mouse", DS4Controls.Circle), ("Synthetic_AnalogMouse", DS4Controls.Square), ("Synthetic_Extras", DS4Controls.Triangle),
            })
            {
                string name = $"{prefix}_{Device}_{(int)control}";
                Assert.False(HasSaStyleLog(logs, name), name + " が SA 形式のログを出した（W2 より前は出さない）");
            }
        }

        [Fact]
        public void TheSpecialKinds_LogTheSaStyleLines_OnTheRisingEdge()
        {
            var gyro = new RecordingGyroCalibrationPort();
            List<string> logs = Capture(() =>
            {
                using var t = new ControlsKeyTestScope();
                t.MapSpecial(DS4Controls.Share, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.GyroCalibrate });
                var factory = new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), TestHost.GetService<IProfileSwitcher>(),
                    TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>(), gyroCalibration: gyro);
                t.PublishWith(new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, factory));

                RunFor(t, Pressed(), 150);
                RunFor(t, Pressed(DS4Controls.Share), 300);
                RunFor(t, Pressed(DS4Controls.Share), 300);   // 押し続けても、立ち上がりのログは 1 回だけ
            });

            string name = $"Synthetic_Special_{Device}_{(int)DS4Controls.Share}";
            Assert.Single(gyro.Calls);
            Assert.True(HasSaStyleLog(logs, name), "Special が SA 形式のログを出していない");
            Assert.Equal(1, logs.Count(l => l.Contains("Trigger detected for SA '" + name + "'")));
            Assert.Equal(1, logs.Count(l => l.Contains("SpecialAction GyroCalibrate:") && l.Contains("name=" + name)));
        }
    }
}
