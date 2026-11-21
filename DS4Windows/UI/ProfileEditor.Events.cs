using System;
using System.ComponentModel;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>ViewModel のイベントの登録と解除。</summary>
    public partial class ProfileEditor
    {
        private void SetupEvents()
        {
            outputPanel.OutConTypeCombo.SelectionChanged += OutConTypeCombo_SelectionChanged;
            outputPanel.RumbleTestClick += RumbleTestBtn_Click;
            generalPanel.UseControllerReadoutClick += UseControllerReadoutCk_Click;
            generalPanel.LaunchProgramBrowseClick += LaunchProgBrowseBtn_Click;
            mappingListBox.SelectionChanged += MappingListBox_SelectionChanged;
            Closed += ProfileEditor_Closed;

            profileSettingsVM.SixAxis.PropertyChanged += ProfileSettingsVM_PropertyChanged;
            sixAxisPanel.CustomCurveEditorRequested += SixAxisPanel_CustomCurveEditorRequested;
            gyroPanel.TiltControlClicked += GyroPanel_TiltControlClicked;
            gyroPanel.CalibrationRequested += GyroCalibration_Click;
            profileSettingsVM.Touchpad.PropertyChanged += TouchpadVM_PropertyChanged;
            touchpadPanel.SwipeControlClicked += TouchpadPanel_SwipeControlClicked;
            profileSettingsVM.Triggers.PropertyChanged += TriggersVM_PropertyChanged;
            profileSettingsVM.Sticks.Left.PropertyChanged += SticksVM_PropertyChanged;
            profileSettingsVM.Sticks.Right.PropertyChanged += SticksVM_PropertyChanged;
            sticksPanel.CustomCurveEditorRequested += SticksPanel_CustomCurveEditorRequested;
            sticksPanel.CalibrationRequested += CalibrateStick_OnClick;
            triggersPanel.CustomCurveEditorRequested += TriggersPanel_CustomCurveEditorRequested;

        }

        /// <summary>
        /// 6 軸の設定の値が変わったとき、試験表示（デッドゾーン）へ反映する。
        /// </summary>
        private void ProfileSettingsVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SixAxisSettingsViewModel.SXDeadZone): UpdateReadingsSXDeadZone(sender, EventArgs.Empty); break;
                case nameof(SixAxisSettingsViewModel.SZDeadZone): UpdateReadingsSZDeadZone(sender, EventArgs.Empty); break;
            }
        }

        /// <summary>
        /// 左右のスティックの軸ごとのデッドゾーンが変わったとき、試験表示へ反映する。
        /// </summary>
        private void AxialStick_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            bool left = ReferenceEquals(sender, sticksPanel.AxialLeft.AxialVM);
            switch (e.PropertyName)
            {
                case nameof(AxialStickControlViewModel.DeadZoneX):
                    if (left) UpdateReadingsLsDeadZoneX(sender, EventArgs.Empty); else UpdateReadingsRsDeadZoneX(sender, EventArgs.Empty);
                    break;
                case nameof(AxialStickControlViewModel.DeadZoneY):
                    if (left) UpdateReadingsLsDeadZoneY(sender, EventArgs.Empty); else UpdateReadingsRsDeadZoneY(sender, EventArgs.Empty);
                    break;
            }
        }

        /// <summary>デッドゾーンの種類（円形／軸ごと）に合わせて、入力の表示の値と形（楕円／長方形）を更新する。</summary>
        private void ApplyReadingsDeadZoneShape()
        {
            var dead = ReadingsDeadZoneSnapshot.Create(profileSettingsVM, sticksPanel.AxialLeft.AxialVM, sticksPanel.AxialRight.AxialVM);
            if (dead.LsDeadX is { } lsDeadX && dead.LsDeadY is { } lsDeadY)
            {
                conReadingsUserCon.LsDeadX = lsDeadX;
                conReadingsUserCon.LsDeadY = lsDeadY;
            }
            if (dead.RsDeadX is { } rsDeadX && dead.RsDeadY is { } rsDeadY)
            {
                conReadingsUserCon.RsDeadX = rsDeadX;
                conReadingsUserCon.RsDeadY = rsDeadY;
            }
            conReadingsUserCon.LsDeadAxial = profileSettingsVM.Sticks.Left.IsAxialDeadZone;
            conReadingsUserCon.RsDeadAxial = profileSettingsVM.Sticks.Right.IsAxialDeadZone;
        }

        private void SticksVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // デッドゾーンを変えたら、コントローラーの読み取り値の表示（試験表示）へ反映する。左右は、通知の送り手で見分ける
            bool left = ReferenceEquals(sender, profileSettingsVM.Sticks.Left);
            switch (e.PropertyName)
            {
                case nameof(StickSideViewModel.DeadTypeIndex):
                    ApplyReadingsDeadZoneShape(); break;
                case nameof(StickSideViewModel.DeadZone):
                    if (left) UpdateReadingsLsDeadZone(sender, EventArgs.Empty); else UpdateReadingsRsDeadZone(sender, EventArgs.Empty);
                    break;
                case nameof(StickSideViewModel.DriftXAxis):
                case nameof(StickSideViewModel.DriftYAxis):
                    if (left) UpdateReadingsLSDrift(sender, EventArgs.Empty); else UpdateReadingsRSDrift(sender, EventArgs.Empty);
                    break;
            }
        }

        private void TriggersVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // デッドゾーンを変えたら、コントローラーの読み取り値の表示（試験表示）へ反映する
            switch (e.PropertyName)
            {
                case nameof(TriggerSettingsViewModel.L2DeadZone): UpdateReadingsL2DeadZone(sender, EventArgs.Empty); break;
                case nameof(TriggerSettingsViewModel.R2DeadZone): UpdateReadingsR2DeadZone(sender, EventArgs.Empty); break;
            }
        }

        private void UnregisterEvents()
        {
            outputPanel.OutConTypeCombo.SelectionChanged -= OutConTypeCombo_SelectionChanged;
            outputPanel.RumbleTestClick -= RumbleTestBtn_Click;
            generalPanel.UseControllerReadoutClick -= UseControllerReadoutCk_Click;
            generalPanel.LaunchProgramBrowseClick -= LaunchProgBrowseBtn_Click;
            mappingListBox.SelectionChanged -= MappingListBox_SelectionChanged;
            Closed -= ProfileEditor_Closed;

            profileSettingsVM.SixAxis.PropertyChanged -= ProfileSettingsVM_PropertyChanged;
            sixAxisPanel.CustomCurveEditorRequested -= SixAxisPanel_CustomCurveEditorRequested;
            gyroPanel.TiltControlClicked -= GyroPanel_TiltControlClicked;
            gyroPanel.CalibrationRequested -= GyroCalibration_Click;
            profileSettingsVM.Touchpad.PropertyChanged -= TouchpadVM_PropertyChanged;
            touchpadPanel.SwipeControlClicked -= TouchpadPanel_SwipeControlClicked;
            profileSettingsVM.Triggers.PropertyChanged -= TriggersVM_PropertyChanged;
            profileSettingsVM.Sticks.Left.PropertyChanged -= SticksVM_PropertyChanged;
            profileSettingsVM.Sticks.Right.PropertyChanged -= SticksVM_PropertyChanged;
            sticksPanel.CustomCurveEditorRequested -= SticksPanel_CustomCurveEditorRequested;
            sticksPanel.CalibrationRequested -= CalibrateStick_OnClick;
            triggersPanel.CustomCurveEditorRequested -= TriggersPanel_CustomCurveEditorRequested;
            sticksPanel.AxialLeft.AxialVM.PropertyChanged -= AxialStick_PropertyChanged;
            sticksPanel.AxialRight.AxialVM.PropertyChanged -= AxialStick_PropertyChanged;


            inputTimer?.Stop();
            if (inputTimer != null) inputTimer.Elapsed -= InputDS4;
            inputTimer = null;

            StopEditorBindings();
        }
    }
}
