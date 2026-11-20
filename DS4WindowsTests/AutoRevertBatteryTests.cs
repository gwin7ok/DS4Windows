#nullable enable
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 自動プロファイルから元のプロファイルへ戻すときの通知は、バッテリー残量を「N/A」固定にせず、
    /// 接続中のコントローラーの値を使う（未接続のときだけ「N/A」）。実際に戻す処理を動かして、渡される通知文で確かめる。
    /// </summary>
    public class AutoRevertBatteryTests
    {
        private sealed class Inspector : IProcessInspector
        {
            public string Path = string.Empty;
            public bool IsProcessRunning(string exePath) => false;

            public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
            {
                processPath = Path;
                windowTitle = string.Empty;
                return true;
            }
        }

        private static string? RevertNotice(DeviceSlotState? slots, int batteryPercent)
        {
            var inspector = new Inspector { Path = @"c:\games\testgame.exe" };
            var appService = RecordingProxy.Create<IProfileApplicationService>();
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var holder = TestRepositoryFactory.CreateAutoProfileHolder();
            var entity = new AutoProfileEntity(@"c:\games\testgame.exe", string.Empty);
            entity.ProfileNames[0] = "GameProfile";
            holder.AutoProfileColl.Add(entity);

            if (slots?.Controllers[0] is TestDs4Device device) device.SetBattery(batteryPercent);
            var service = TestRepositoryFactory.CreateAutoProfileService(holder, appService, settings, inspector, deviceState: slots);

            service.CheckProfiles();
            settings.SetUseTempProfile(0, true);
            settings.SetTempProfileName(0, "GameProfile");
            TestGlobals.Store.Behavior.autoProfileRevertDefaultProfile = true;
            inspector.Path = @"c:\windows\explorer.exe";
            service.CheckProfiles();

            // ApplyProfile(deviceIndex, profileName, isTemp, launchProgram, source, prolog) のうち、一時でない（復帰の）呼び出し
            object?[] revert = RecordingProxy.Of(appService).ArgsOf("ApplyProfile").First(a => !(bool)a[2]!);
            return (string?)revert[5];
        }

        [Fact]
        public void TheRevertNotice_UsesTheDeviceBattery()
        {
            var slots = new DeviceSlotState();
            slots.Controllers[0] = TestDs4Device.Create();

            // 表示の言語（日本語・英語）に依らず、残量の数値で確かめる
            Assert.Contains("55%", RevertNotice(slots, 55));
        }

        [Fact]
        public void TheRevertNotice_ShowsNA_OnlyWithoutAController()
        {
            Assert.Contains("N/A%", RevertNotice(new DeviceSlotState(), 0));
        }
    }
}
