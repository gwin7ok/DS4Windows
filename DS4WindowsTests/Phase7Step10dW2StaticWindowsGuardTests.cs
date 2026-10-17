using System;
using System.Linq;
using System.Reflection;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W2（Q5・Q6＝A）: 環境・パスの窓口、Windows の判定、定数・文化を <c>Global</c> から持ち主の型へ移したことの回帰ガード。
    /// </summary>
    public class Phase7Step10dW2StaticWindowsGuardTests
    {
        private static readonly string[] RemovedFromGlobal =
        {
            "exeversion", "exelocation", "AdminNeeded", "IsAdministrator", "GetInstanceIdFromDevicePath", "CheckIfVirtualDevice",
            "GetStringDeviceProperty", "GetStringArrayDeviceProperty", "configFileDecimalCulture", "CONFIG_VERSION", "APP_CONFIG_VERSION",
            "PROBING_PATH", "LANGUAGE_ASSEMBLY_NAME", "CUSTOM_EXE_CONFIG_FILENAME", "XML_EXTENSION",
        };

        [Fact]
        public void Global_NoLongerDeclaresTheMovedMembers()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var names = typeof(Global).GetMembers(all).Select(m => m.Name).ToHashSet();
            foreach (string removed in RemovedFromGlobal)
            {
                Assert.DoesNotContain(removed, names);
            }
        }

        [Fact]
        public void ConfigFileCulture_IsEnUsWithDotDecimalSeparator()
        {
            Assert.Equal("en-US", ConfigFileCulture.DecimalCulture.Name);
            Assert.Equal("1.5", 1.5.ToString(ConfigFileCulture.DecimalCulture));
        }

        [Fact]
        public void MovedHelpers_AreStaticAndStateless()
        {
            foreach (Type t in new[] { typeof(AdminCheck), typeof(DevicePathProbe), typeof(ConfigFileCulture) })
            {
                Assert.True(t.IsAbstract && t.IsSealed, t.Name + " は静的クラスであること");
            }

            Assert.NotNull(typeof(AdminCheck).GetMethod("AdminNeeded"));
            Assert.NotNull(typeof(AdminCheck).GetMethod("IsAdministrator"));
            Assert.NotNull(typeof(DevicePathProbe).GetMethod("GetInstanceIdFromDevicePath"));
            Assert.NotNull(typeof(DevicePathProbe).GetMethod("CheckIfVirtualDevice"));
        }

        [Fact]
        public void EnvironmentService_DelegatesToTheMovedHelpers()
        {
            var service = new EnvironmentService();
            Assert.Equal(AdminCheck.IsAdministrator(), service.IsAdministrator());
            Assert.Equal(PathService.ExecutableVersionState, service.ApplicationVersion);
        }
    }
}
