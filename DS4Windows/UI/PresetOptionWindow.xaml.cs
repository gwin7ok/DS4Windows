#nullable enable

using System;
using System.Windows;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for PresetOptionWindow.xaml
    /// </summary>
    public partial class PresetOptionWindow : Window
    {
        private MessageBoxResult result = MessageBoxResult.Cancel;
        public MessageBoxResult Result { get => result; }

        private int deviceIndex;
        private PresetOptionViewModel presetOptionVM;
        private bool delayPresetApply;
        public bool DelayPresetApply { get => delayPresetApply; set => delayPresetApply = value; }

        public PresetOptionWindow(PresetOptionViewModel viewModel)
        {
            InitCommands();
            InitializeComponent();

            ArgumentNullException.ThrowIfNull(viewModel);
            presetOptionVM = viewModel;

            // Hide tab headers. Tab content will still be visible
            IntroTab.Visibility = Visibility.Collapsed;
            PresetListTab.Visibility = Visibility.Collapsed;
        }

        public void SetupData(int index)
        {
            deviceIndex = index;
            PresetListTab.DataContext = presetOptionVM;
        }

        public void ToPresetsScreen()
        {
            screensTabControl.SelectedItem = PresetListTab;
        }

        public void ApplyPreset()
        {
            presetOptionVM.ApplyPreset(deviceIndex);
        }

        private void NoPresetBtn()
        {
            Close();
        }

        private void YesPresetBtn()
        {
            screensTabControl.SelectedItem = PresetListTab;
            //PresetListTab.DataContext = presetOptionVM;
        }

        private void PresetCancelBtn()
        {
            Close();
        }

        private void PresetApplyBtn()
        {
            if (!delayPresetApply)
            {
                presetOptionVM.ApplyPreset(deviceIndex);
            }

            result = MessageBoxResult.OK;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PresetListTab.DataContext = null;
        }
    }
}
