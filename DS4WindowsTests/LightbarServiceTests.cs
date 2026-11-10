using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ライトバーの色の計算（<see cref="LightbarService"/>）の特性化テスト。仮想時間（<see cref="ManualTimeProvider"/>）で、
    /// 固定色、虹色、電池連動の点滅、充電中の脈動、強制表示、終了時の消灯、無操作のフェードの結果を確かめる。
    /// 共有のホストのプロファイル設定（スロット 0）を書き換えるため、直列のコレクションで実行し、終わりに元へ戻す。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class LightbarServiceTests
    {
        private const int Slot = 0;

        private sealed class Scope : IDisposable
        {
            private readonly LightbarSettingInfo _info;
            private readonly LightbarMode _mode;
            private readonly LightbarDS4WinInfo _saved;

            public Scope()
            {
                Settings = TestHost.GetService<IProfileSettingsService>();
                _info = Settings.GetLightbarSettingsInfo(Slot);
                _mode = _info.mode;
                _saved = _info.ds4winSettings;
                _info.mode = LightbarMode.DS4Win;
                Info = _info.ds4winSettings = new LightbarDS4WinInfo { useCustomLed = true, m_CustomLed = new DS4Color(0, 0, 255) };
                Time = new ManualTimeProvider();
                Overrides = new LightbarOverrides();
                Service = new LightbarService(Settings, Overrides, Time);
                Device = new FakeLightbarDevice { LastActive = Time.GetUtcNow().UtcDateTime };
            }

            public IProfileSettingsService Settings { get; }
            public LightbarDS4WinInfo Info { get; }
            public ManualTimeProvider Time { get; }
            public LightbarOverrides Overrides { get; }
            public LightbarService Service { get; }
            public FakeLightbarDevice Device { get; }
            public LightbarSettingInfo SettingInfo => _info;

            public void Dispose()
            {
                _info.mode = _mode;
                _info.ds4winSettings = _saved;
            }
        }

        [Fact]
        public void ACustomColor_IsSetAsIs_WithNoFlash()
        {
            using var s = new Scope();

            s.Service.Update(s.Device, Slot);

            Assert.Equal(new DS4Color(0, 0, 255), s.Device.Last.LightBarColor);
            Assert.Equal(0, s.Device.Last.LightBarFlashDurationOn);
            Assert.Equal(0, s.Device.Last.LightBarFlashDurationOff);
        }

        [Fact]
        public void WhenTheModeIsNotDS4Win_TheDeviceIsNotTouched()
        {
            using var s = new Scope();
            s.SettingInfo.mode = LightbarMode.Passthru;

            s.Service.Update(s.Device, Slot);

            Assert.Equal(0, s.Device.SetCount);
        }

        [Fact]
        public void AForcedColor_WinsOverTheProfile_AndAddsTheForcedFlash()
        {
            using var s = new Scope();
            s.Overrides.forcelight[Slot] = true;
            s.Overrides.forcedColor[Slot] = new DS4Color(255, 0, 0);
            s.Overrides.forcedFlash[Slot] = 5;

            s.Service.Update(s.Device, Slot);

            Assert.Equal(new DS4Color(255, 0, 0), s.Device.Last.LightBarColor);
            Assert.Equal(20, s.Device.Last.LightBarFlashDurationOn);   // 25 - 5
            Assert.Equal(20, s.Device.Last.LightBarFlashDurationOff);
        }

        [Fact]
        public void WhileShuttingDown_TheLightbarIsOff()
        {
            using var s = new Scope();
            s.Overrides.defaultLight = true;
            s.Overrides.shuttingdown = true;

            s.Service.Update(s.Device, Slot);

            Assert.Equal(new DS4Color(0, 0, 0), s.Device.Last.LightBarColor);
            Assert.True(s.Device.Last.LightBarExplicitlyOff);
        }

        [Fact]
        public void ALowBattery_FlashesWithTheDurationsOfItsLevel()
        {
            using var s = new Scope();
            s.Info.flashAt = 30;
            s.Info.flashType = 0;
            s.Info.m_FlashLed = new DS4Color(255, 255, 0);
            s.Device.Battery = 25;   // レベル 2 → 点灯 56／消灯 224

            s.Service.Update(s.Device, Slot);

            Assert.Equal(new DS4Color(255, 255, 0), s.Device.Last.LightBarColor);
            Assert.Equal(56, s.Device.Last.LightBarFlashDurationOn);
            Assert.Equal(224, s.Device.Last.LightBarFlashDurationOff);
        }

        [Fact]
        public void TheRainbow_AdvancesWithTheVirtualTime()
        {
            using var s = new Scope();
            s.Info.useCustomLed = false;
            s.Info.rainbow = 1.0;   // 1 周が 1 秒
            s.Service.Update(s.Device, Slot);   // 時間が進んでいないので角度は 0 → 赤
            Assert.Equal(new DS4Color(255, 0, 0), s.Device.Last.LightBarColor);

            s.Time.Advance(TimeSpan.FromMilliseconds(500));
            s.Service.Update(s.Device, Slot);   // 半周 = 180 度

            Assert.Equal(new DS4Color(0, 255, 255), s.Device.Last.LightBarColor);   // 180 度はシアン
        }

        [Fact]
        public void TheChargingPulse_FadesWithTheVirtualTime()
        {
            using var s = new Scope();
            s.Info.chargingType = 1;
            s.Device.IsCharging = true;
            s.Device.Battery = 50;
            s.Service.Update(s.Device, Slot);   // 計測の開始。比率 0（そのままの色）
            Assert.Equal(new DS4Color(0, 0, 255), s.Device.Last.LightBarColor);

            s.Time.Advance(TimeSpan.FromMilliseconds(2000));
            s.Service.Update(s.Device, Slot);   // 2000ms → 区間 50 / 98 → 約 51% 暗くなる

            Assert.Equal(new DS4Color(0, 0, 125), s.Device.Last.LightBarColor);
        }

        [Fact]
        public void TheIdleFade_DimsTheColorAsTheTimeWithoutInputGrows()
        {
            using var s = new Scope();
            s.Info.useCustomLed = false;
            s.Info.ledAsBattery = true;
            s.Info.m_Led = new DS4Color(0, 0, 200);
            s.Info.m_LowLed = new DS4Color(0, 0, 200);
            s.Device.Battery = 100;
            s.Device.LastActive = s.Time.GetUtcNow().UtcDateTime;
            int timeout = s.Settings.GetIdleDisconnectTimeout(Slot);
            if (timeout <= 0) return;   // 無操作の切断が無効なプロファイルでは、フェードしない（計算の対象外）

            s.Service.Update(s.Device, Slot);
            DS4Color fresh = s.Device.Last.LightBarColor;
            s.Time.Advance(TimeSpan.FromSeconds(timeout));   // 比率 100% 以上 → 消灯
            s.Service.Update(s.Device, Slot);

            Assert.Equal(new DS4Color(0, 0, 200), fresh);
            Assert.Equal(new DS4Color(0, 0, 0), s.Device.Last.LightBarColor);
        }

        [Fact]
        public void ADistanceProfile_BlendsTheColorByTheRumble()
        {
            using var s = new Scope();
            s.Settings.DistanceProfiles[Slot] = true;
            try
            {
                s.Device.LeftHeavySlowRumble = 200;
                s.Service.Update(s.Device, Slot);
                DS4Color strong = s.Device.Last.LightBarColor;
                s.Device.LeftHeavySlowRumble = 0;
                s.Service.Update(s.Device, Slot);

                Assert.NotEqual(strong, s.Device.Last.LightBarColor);
            }
            finally
            {
                s.Settings.DistanceProfiles[Slot] = false;
            }
        }
    }
}
