﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 機種の設定画面が、設定項目の定義（<see cref="ControllerSetting"/>）と系統の一覧から作られること。
    /// 項目の見せ方（ラベルのキー、選択肢）が、保存される値と食い違わないことと、実際の画面（XAML）が組み上がることを確かめる。
    /// </summary>
    public class DeviceSettingsUiTests
    {
        [Fact]
        public void EveryUiSetting_HasALabel_AndEnumChoicesMatchTheEnum()
        {
            var options = new ControlServiceDeviceOptions();
            var stores = new ControllerOptionsStore[]
            {
                new DS4ControllerOptions(InputDeviceType.DS4),
                new DualSenseControllerOptions(InputDeviceType.DualSense),
                new SwitchProControllerOptions(InputDeviceType.SwitchPro),
                new JoyConControllerOptions(InputDeviceType.JoyConL),
            };
            IEnumerable<ControllerSetting> all = stores.SelectMany(s => s.Settings)
                .Concat(options.Families.SelectMany(f => options[f].Settings.Skip(1)));
            foreach (ControllerSetting s in all)
            {
                Assert.NotNull(s.Ui);
                Assert.False(string.IsNullOrEmpty(s.Ui.LabelKey));
                Assert.Equal(s.IsEnum, s.Ui.Choices != null);
                if (s.IsEnum)
                {
                    // 選択肢の値は、いま読める値を含み、どれも解析できる（保存される値と食い違わない）
                    Assert.Contains(s.Ui.Choices, c => c.Value == s.Read());
                    foreach (SettingChoice c in s.Ui.Choices)
                    {
                        Assert.NotNull(s.Parse(c.Value));
                    }
                }
            }
        }

        [Fact]
        public void PanelForTheSelectedController_ListsFamilyThenControllerItems()
        {
            var options = new ControlServiceDeviceOptions();
            var joyCon = new JoyConControllerOptions(InputDeviceType.JoyConL);
            var panel = new DeviceFamilyPanelModel(options[InputDeviceFamily.JoyCon], joyCon.Settings);

            Assert.Equal(3, panel.Items.Count);   // 結合の方式、ジャイロの提供側、HOME の LED
            Assert.True(panel.ShowNewConnectionNotice);
            Assert.True(panel.Items[0].IsChoice);
            Assert.True(panel.Items[1].IsChoice);
            Assert.False(panel.Items[2].IsChoice);

            // 画面の項目への書き込みが、設定へ届く
            panel.Items[0].ChoiceValue = "Split";
            Assert.Equal(JoyConDeviceOptions.LinkMode.Split, options.Get<JoyConDeviceOptions>(InputDeviceFamily.JoyCon).LinkedMode);
            panel.Items[2].BoolValue = false;
            Assert.False(joyCon.EnableHomeLED);
        }

        [Fact]
        public void FamilyWithoutSettings_GivesAnEmptyPanel_AndVisibilityFollowsEnabled()
        {
            var options = new ControlServiceDeviceOptions();
            var panel = new DeviceFamilyPanelModel(options[InputDeviceFamily.DS3], new ControllerSetting[0]);
            Assert.Empty(panel.Items);
            Assert.False(panel.ShowNewConnectionNotice);

            var changes = new List<string>();
            panel.PropertyChanged += (s, e) => changes.Add(e.PropertyName);
            options[InputDeviceFamily.DS3].Enabled = !options[InputDeviceFamily.DS3].Enabled;
            Assert.Contains(nameof(DeviceFamilyPanelModel.Visible), changes);
        }

        [Fact]
        public void FamilyToggles_ExistForEveryFamily_WithTheirTexts()
        {
            var options = new ControlServiceDeviceOptions();
            var vm = new FirstLauchUtilViewModel(options);
            Assert.Equal(options.Families.Count(), vm.FamilyToggles.Count);
            Assert.All(vm.FamilyToggles, t => Assert.False(string.IsNullOrEmpty(t.SupportLabel)));
            Assert.Contains(vm.FamilyToggles, t => t.Family == InputDeviceFamily.DS3 && t.HasHelpUrl);

            FamilyToggleItem ds4 = vm.FamilyToggles.Single(t => t.Family == InputDeviceFamily.DS4);
            bool before = options[InputDeviceFamily.DS4].Enabled;
            ds4.Enabled = !before;
            Assert.Equal(!before, options[InputDeviceFamily.DS4].Enabled);
        }
    }
}
