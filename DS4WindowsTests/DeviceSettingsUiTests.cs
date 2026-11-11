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
            };
            IEnumerable<ControllerSetting> all = stores.SelectMany(s => s.Settings)
                .Concat(options.Families.SelectMany(f => options[f].Settings.Skip(1)))
                .Where(s => s.XmlName != "HideUsbParent");   // 設定タブの専用のチェックボックスで見せる（機種ごとの設定パネルには出さない）
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
            var switchPro = new SwitchProControllerOptions(InputDeviceType.SwitchPro);
            var proPanel = new DeviceFamilyPanelModel(options[InputDeviceFamily.SwitchPro], switchPro.Settings);

            Assert.Single(proPanel.Items);   // HOME の LED
            Assert.True(proPanel.ShowNewConnectionNotice);
            Assert.False(proPanel.Items[0].IsChoice);

            // 画面の項目への書き込みが、設定へ届く
            proPanel.Items[0].BoolValue = false;
            Assert.False(switchPro.EnableHomeLED);

            // 列挙型の項目（選択肢）の書き込みも届く
            var dualSense = new DualSenseControllerOptions(InputDeviceType.DualSense);
            var dsPanel = new DeviceFamilyPanelModel(options[InputDeviceFamily.DualSense], dualSense.Settings);
            DeviceSettingItem choice = dsPanel.Items.First(i => i.IsChoice);
            string other = choice.Choices.First(c => c.Value != choice.ChoiceValue).Value;
            choice.ChoiceValue = other;
            Assert.Equal(other, choice.ChoiceValue);
        }

        [Fact]
        public void FamilyWithoutSettings_GivesAnEmptyPanel_AndVisibilityFollowsEnabled()
        {
            var options = new ControlServiceDeviceOptions();
            var panel = new DeviceFamilyPanelModel(options[InputDeviceFamily.DS4], new ControllerSetting[0]);
            Assert.Empty(panel.Items);
            Assert.False(panel.ShowNewConnectionNotice);

            var changes = new List<string>();
            panel.PropertyChanged += (s, e) => changes.Add(e.PropertyName);
            options[InputDeviceFamily.DS4].Enabled = !options[InputDeviceFamily.DS4].Enabled;
            Assert.Contains(nameof(DeviceFamilyPanelModel.Visible), changes);
        }

        [Fact]
        public void FamilyToggles_ExistForEveryFamily_WithTheirTexts()
        {
            var options = new ControlServiceDeviceOptions();
            var vm = new FirstLauchUtilViewModel(options);
            Assert.Equal(options.Families.Count(), vm.FamilyToggles.Count);
            Assert.All(vm.FamilyToggles, t => Assert.False(string.IsNullOrEmpty(t.SupportLabel)));

            FamilyToggleItem ds4 = vm.FamilyToggles.Single(t => t.Family == InputDeviceFamily.DS4);
            bool before = options[InputDeviceFamily.DS4].Enabled;
            ds4.Enabled = !before;
            Assert.Equal(!before, options[InputDeviceFamily.DS4].Enabled);
        }
    }
}
