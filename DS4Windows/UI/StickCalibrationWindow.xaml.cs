using System;
using System.Windows;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI;

public partial class StickCalibrationWindow : Window
{
    private Stick _stick;
    private int _device;
    private ProfileSettingsViewModel _profileSettingsVM;
    private readonly IControllerReadingsSource _readings;

    public StickCalibrationWindow(Stick stick, int device, ProfileSettingsViewModel profileSettingsVm,
        IControllerReadingsSource readings)
    {
        _stick = stick;
        _device = device;
        _profileSettingsVM = profileSettingsVm;
        // Phase6-Step10-2-4b: AppHost/Program.rootHub への後退を廃止（必須引数）
        _readings = readings ?? throw new ArgumentNullException(nameof(readings));
        InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var state = _readings.GetCurrentState(_device);

        const int neutralState = 128;
        if (_stick == Stick.Left)
        {
            var xAxisDrift = state.LX - neutralState;
            var yAxisDrift = state.LY - neutralState;

            _profileSettingsVM.Sticks.LeftStickDriftXAxis = Convert.ToSByte(xAxisDrift);
            _profileSettingsVM.Sticks.LeftStickDriftYAxis = Convert.ToSByte(yAxisDrift);

            MessageBox.Show($"Detected drift:\nX axis: {xAxisDrift}, Y axis: {yAxisDrift}",
                "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        if (_stick == Stick.Right)
        {
            var xAxisDrift = state.RX - neutralState;
            var yAxisDrift = state.RY - neutralState;

            _profileSettingsVM.Sticks.RightStickDriftXAxis = Convert.ToSByte(xAxisDrift);
            _profileSettingsVM.Sticks.RightStickDriftYAxis = Convert.ToSByte(yAxisDrift);

            MessageBox.Show($"Detected drift:\nX axis: {xAxisDrift}, Y axis: {yAxisDrift}",
                "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Close();
    }
}
