using System;
using System.Globalization;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileSettingsServiceTests
    {
        [Fact]
        public void Defaults_ShouldMatchInitialValues()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            Assert.Equal(9, service.TouchpadActiveArray.Length);
            Assert.Equal(9, service.UseTempProfileArray.Length);
            Assert.Equal(9, service.TempProfileDistanceArray.Length);
            Assert.Equal(9, service.UseDInputOnlyArray.Length);

            for (int i = 0; i < 9; i++)
            {
                Assert.True(service.GetTouchpadActive(i));
                Assert.False(service.GetUseTempProfile(i));
                Assert.Equal(string.Empty, service.GetTempProfileName(i));
                Assert.False(service.TempProfileDistanceArray[i]);
                Assert.True(service.UseDInputOnlyArray[i]);
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.False(service.GetLinkedProfileCheck(i));
            }
        }

        [Fact]
        public void SetAndGet_ShouldUpdateCorrectSlot()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            service.TouchpadActiveArray[2] = false;
            service.SetUseTempProfile(3, true);
            service.SetTempProfileName(4, "TestProfile");
            service.SetTempProfileDistance(1, true);
            service.UseDInputOnlyArray[0] = false;
            service.SetLinkedProfileCheck(5, true);

            Assert.False(service.GetTouchpadActive(2));
            Assert.True(service.GetTouchpadActive(1));

            Assert.True(service.GetUseTempProfile(3));
            Assert.False(service.GetUseTempProfile(2));

            Assert.Equal("TestProfile", service.GetTempProfileName(4));
            Assert.Equal(string.Empty, service.GetTempProfileName(0));

            Assert.True(service.TempProfileDistanceArray[1]);
            Assert.False(service.UseDInputOnlyArray[0]);
            Assert.True(service.GetLinkedProfileCheck(5));
        }

        [Fact]
        public void SettingChangedEvent_ShouldFire()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();
            ProfileSettingChangedEventArgs eventArgs = null;
            service.ProfileSettingChanged += (s, e) => eventArgs = e;

            service.SetUseTempProfile(1, true);

            Assert.NotNull(eventArgs);
            Assert.Equal(1, eventArgs.DeviceIndex);
            Assert.Equal(nameof(service.UseTempProfileArray), eventArgs.SettingName);
            Assert.Equal(false, eventArgs.OldValue);
            Assert.Equal(true, eventArgs.NewValue);
        }

        [Fact]
        public void ResetToDefaults_ShouldRestoreValues()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            service.TouchpadActiveArray[0] = false;
            service.SetUseTempProfile(0, true);
            service.SetTempProfileName(0, "Temp");
            service.SetLinkedProfileCheck(0, true);

            service.ResetToDefaults(0);

            Assert.True(service.GetTouchpadActive(0));
            Assert.False(service.GetUseTempProfile(0));
            Assert.Equal(string.Empty, service.GetTempProfileName(0));
            Assert.False(service.GetLinkedProfileCheck(0));
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            var touchpad = service.GetTouchpadActive(99);
            Assert.True(touchpad);

            service.SetUseTempProfile(99, true);
            Assert.False(service.GetUseTempProfile(99));
            service.SetTempProfileName(-1, "Invalid");
            Assert.Equal(string.Empty, service.GetTempProfileName(-1));
        }

        [Fact]
        public void TriggerCurveMode_ShouldRoundTripThroughService()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            service.SetL2OutCurveMode(0, 2);
            service.SetR2OutCurveMode(0, 3);

            Assert.Equal(2, service.GetL2OutCurveMode(0));
            Assert.Equal(3, service.GetR2OutCurveMode(0));

            service.SetL2OutCurveMode(0, 0);
            service.SetR2OutCurveMode(0, 0);
            Assert.Equal(0, service.GetL2OutCurveMode(0));
            Assert.Equal(0, service.GetR2OutCurveMode(0));
        }

        [Fact]
        public void OutContType_ShouldShareBackingStoreWithGlobalShim()
        {
            // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク7）の回帰防止テスト。
            // IOutputSlotService.GetOutputDeviceType が参照していた孤立配列（Global非連動）に
            // 起因するバグの再発を検知するため、ProfileSettingsService.OutContType が
            // Global.OutContType（m_Config.outputDevType）と同一の実体を参照していることを
            // 意図的に確認する（copilot-instructions.md §3.3-4 における「孤立プロパティ」検知の
            // ためのテストであり、削除・書き換えを行ってはならない）。
            var service = TestRepositoryFactory.CreateProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            Assert.Same(service.OutContType, Global.OutContType);

            OutContType original = service.OutContType[0];
            try
            {
                service.OutContType[0] = OutContType.DS4;
                Assert.Equal(OutContType.DS4, Global.OutContType[0]);

                Global.OutContType[0] = OutContType.X360;
                Assert.Equal(OutContType.X360, service.OutContType[0]);
            }
            finally
            {
                service.OutContType[0] = original;
            }
        }
    }
}