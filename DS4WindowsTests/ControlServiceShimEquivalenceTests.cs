using System;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消のために
    /// IAppSettingsService / IEnvironmentService へ追加した薄い委譲シムが、
    /// Global（m_Config / BackingStore）と完全に同一の実体を共有していることを検証する。
    ///
    /// SSOT 原則: DI サービスは独自フィールドを持たない。したがって
    /// 「Global で書いた値がサービスから読める」「サービスで書いた値が Global から読める」
    /// の双方向が成立しなければならない（孤立プロパティバグの再発防止）。
    /// 各テストは変更した Global 状態を必ず元に戻す。
    /// </summary>
    public class ControlServiceShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = TestRepositoryFactory.CreateAppSettingsService();
        private readonly EnvironmentService _environment = new EnvironmentService();

        /// <summary>Global で書いた値がサービスから読め、サービスで書いた値が Global から読めることを確認し、元に戻す。</summary>
        private static void AssertSharesStateWithGlobal<T>(
            Func<T> globalGet, Action<T> globalSet,
            Func<T> serviceGet, Action<T> serviceSet,
            T first, T second)
        {
            T original = globalGet();
            try
            {
                globalSet(first);
                Assert.Equal(first, serviceGet());

                serviceSet(second);
                Assert.Equal(second, globalGet());
            }
            finally
            {
                globalSet(original);
            }
        }

        [Fact]
        public void UseOscServer_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.useOSCServ, v => TestGlobals.Store.useOSCServ = v,
                () => _appSettings.UseOscServer, v => _appSettings.UseOscServer = v,
                true, false);
        }

        [Fact]
        public void UseOscSender_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.useOSCSend, v => TestGlobals.Store.useOSCSend = v,
                () => _appSettings.UseOscSender, v => _appSettings.UseOscSender = v,
                true, false);
        }

        [Fact]
        public void InterpretingOscMonitoring_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.interpretingOscMonitoring, v => TestGlobals.Store.interpretingOscMonitoring = v,
                () => _appSettings.InterpretingOscMonitoring, v => _appSettings.InterpretingOscMonitoring = v,
                true, false);
        }

        [Fact]
        public void OscServerPort_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.oscServPort, v => TestGlobals.Store.oscServPort = v,
                () => _appSettings.OscServerPort, v => _appSettings.OscServerPort = v,
                9101, 9102);
        }

        [Fact]
        public void OscSenderPort_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.oscSendPort, v => TestGlobals.Store.oscSendPort = v,
                () => _appSettings.OscSenderPort, v => _appSettings.OscSenderPort = v,
                9103, 9104);
        }

        [Fact]
        public void OscSenderAddress_SharesStateWithGlobal_AndKeepsTrimBehavior()
        {
            string original = TestGlobals.Store.oscSendAddress;
            try
            {
                TestGlobals.Store.oscSendAddress = "10.0.0.1";
                Assert.Equal("10.0.0.1", _appSettings.OscSenderAddress);

                // Global.setOSCSenderAddress は前後の空白を除去する。サービス経由でも同じ挙動であること。
                _appSettings.OscSenderAddress = "  10.0.0.2  ";
                Assert.Equal("10.0.0.2", TestGlobals.Store.oscSendAddress);
            }
            finally
            {
                if (original != null)
                {
                    TestGlobals.Store.oscSendAddress = original;
                }
            }
        }

        [Fact]
        public void QuickCharge_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.quickCharge, v => TestGlobals.Store.quickCharge = v,
                () => _appSettings.QuickCharge, v => _appSettings.QuickCharge = v,
                true, false);
        }

        [Fact]
        public void DCBTatStop_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.disconnectBTAtStop, v => TestGlobals.Store.disconnectBTAtStop = v,
                () => _appSettings.DCBTatStop, v => _appSettings.DCBTatStop = v,
                true, false);
        }

        [Fact]
        public void ProcessPriority_SharesStateWithGlobal()
        {
            AssertSharesStateWithGlobal(
                () => TestGlobals.Store.processPriority, v => TestGlobals.Store.processPriority = v,
                () => _appSettings.ProcessPriority, v => _appSettings.ProcessPriority = v,
                1, 3);
        }

        [Fact]
        public void RunHotPlug_RoundTripsThroughTheService_AndNotifiesOnChange()
        {
            // ControlService.Start/Stop は runHotPlug を IAppSettingsService.RunHotPlug 経由で書き込む。
            // Phase7-Step4 W3: 旧 Global.runHotPlug は、サービスのインスタンスの状態にした
            var changed = new System.Collections.Generic.List<string>();
            _appSettings.SettingChanged += (s, name) => changed.Add(name);

            _appSettings.RunHotPlug = true;
            Assert.True(_appSettings.RunHotPlug);

            _appSettings.RunHotPlug = false;
            Assert.False(_appSettings.RunHotPlug);

            Assert.Equal(2, changed.FindAll(n => n == nameof(DS4Windows.DI.IAppSettingsService.RunHotPlug)).Count);
        }

        [Fact]
        public void DeviceOptions_ReturnsSameInstanceAsGlobal()
        {
            Assert.Same(Global.DeviceOptions, _appSettings.DeviceOptions);
        }

        [Fact]
        public void UDPServerSmoothingMincutoffChanged_IsRaisedByService()
        {
            int fired = 0;
            EventHandler handler = (sender, e) => fired++;
            double original = _appSettings.UDPServerSmoothingMincutoff;
            _appSettings.UDPServerSmoothingMincutoffChanged += handler;
            try
            {
                _appSettings.UDPServerSmoothingMincutoff = original + 1.0;
                Assert.Equal(1, fired);

                // Phase7-Step3 W4: イベントはサービスのインスタンスイベント。購読解除後は発火しないこと。
                _appSettings.UDPServerSmoothingMincutoffChanged -= handler;
                _appSettings.UDPServerSmoothingMincutoff = original;
                Assert.Equal(1, fired);
            }
            finally
            {
                _appSettings.UDPServerSmoothingMincutoffChanged -= handler;
                _appSettings.UDPServerSmoothingMincutoff = original;
            }
        }

        [Fact]
        public void UDPServerSmoothingBetaChanged_IsRaisedByService()
        {
            int fired = 0;
            EventHandler handler = (sender, e) => fired++;
            double original = _appSettings.UDPServerSmoothingBeta;
            _appSettings.UDPServerSmoothingBetaChanged += handler;
            try
            {
                _appSettings.UDPServerSmoothingBeta = original + 1.0;
                Assert.Equal(1, fired);

                _appSettings.UDPServerSmoothingBetaChanged -= handler;
                _appSettings.UDPServerSmoothingBeta = original;
                Assert.Equal(1, fired);
            }
            finally
            {
                _appSettings.UDPServerSmoothingBetaChanged -= handler;
                _appSettings.UDPServerSmoothingBeta = original;
            }
        }

        [Fact]
        public void HidHideInstalled_ReflectsEnvironmentServiceState()
        {
            Assert.Equal(EnvironmentService.HidHideInstalledState, _environment.HidHideInstalled);
        }
    }
}