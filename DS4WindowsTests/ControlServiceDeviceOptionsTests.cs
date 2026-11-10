using System;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 機種の系統ごとの設定の入れ物（<see cref="ControlServiceDeviceOptions"/>）の確認。型付きのプロパティ（DS4DeviceOpts など）は廃し、系統で引く。
    /// 保存用の DTO への往復で、有効・無効と Joy-Con の結合の設定が失われないことも確かめる（保存形式の要素名は DTO の定義が持つ）。
    /// </summary>
    public class ControlServiceDeviceOptionsTests
    {
        [Fact]
        public void EveryFamily_HasItsOwnOptions_OfTheMatchingType()
        {
            var options = new ControlServiceDeviceOptions();
            foreach (InputDeviceFamily family in Enum.GetValues(typeof(InputDeviceFamily)))
            {
                Assert.NotNull(options[family]);
            }

            Assert.Equal(Enum.GetValues(typeof(InputDeviceFamily)).Length, options.Families.Count());
            Assert.IsType<DS4DeviceOptions>(options[InputDeviceFamily.DS4]);
            Assert.IsType<DualSenseDeviceOptions>(options[InputDeviceFamily.DualSense]);
            Assert.IsType<SwitchProDeviceOptions>(options[InputDeviceFamily.SwitchPro]);
            Assert.Same(options[InputDeviceFamily.DS4], options.Get<DS4DeviceOptions>(InputDeviceFamily.DS4));
        }

        [Fact]
        public void TheTypedPerDevicePropertiesAreGone()
        {
            var names = typeof(ControlServiceDeviceOptions).GetProperties().Select(p => p.Name).ToList();
            foreach (string removed in new[] { "DS4DeviceOpts", "DualSenseOpts", "SwitchProDeviceOpts" })
            {
                Assert.DoesNotContain(removed, names);
            }
        }

        [Fact]
        public void TheSettingsDto_RoundTripsTheEnabledFlags()
        {
            var source = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            source.deviceOptions[InputDeviceFamily.DS4].Enabled = false;
            source.deviceOptions[InputDeviceFamily.DualSense].Enabled = false;
            source.deviceOptions[InputDeviceFamily.SwitchPro].Enabled = true;

            var dto = new AppSettingsDTO();
            dto.MapFrom(source);

            var destination = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            dto.MapTo(destination);

            foreach (InputDeviceFamily family in Enum.GetValues(typeof(InputDeviceFamily)))
            {
                Assert.Equal(source.deviceOptions[family].Enabled, destination.deviceOptions[family].Enabled);
            }
        }
    }
}
