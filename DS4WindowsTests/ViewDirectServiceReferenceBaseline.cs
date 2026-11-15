namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V2（画面［View。<c>DS4Windows.UI</c> 直下の型］は、業務のサービス・設定の型を直接参照せず、ViewModel を通す）の、今ある違反の許可リスト。
    /// "参照元|参照先" の形（序数順）。<see cref="ViewDirectServiceReferenceRatchetTests"/> が実数と一致することを検査する。
    /// 是正で減ったら、同じコミットでここからも消す。新しい違反は追加しない（是正する）。
    /// </summary>
    public static class ViewDirectServiceReferenceBaseline
    {
        public static readonly string[] Entries =
        {
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Abstractions.IEnvironmentService",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Abstractions.IProfileSettingsService",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Abstractions.ISpecialActionRepository",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Abstractions.IWindowSystem",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Settings.ControlActionData",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Settings.SpecialAction",
            "DS4Windows.UI.AssignmentEditor|DS4Windows.Core.Settings.SpecialAction+ActionTypeId",
            "DS4Windows.UI.BindingWindow|DS4Windows.Core.Abstractions.IProfileSettingsService",
            "DS4Windows.UI.BindingWindow|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.BindingWindow|DS4Windows.Core.Settings.OutputSlotSettings",
            "DS4Windows.UI.BindingWindow|DS4Windows.Core.Settings.ProfileSlotSettings",
            "DS4Windows.UI.BindingWindow|DS4Windows.Processing.Abstractions.IControllerSlotCommands",
            "DS4Windows.UI.ControllerReadingsControl|DS4Windows.Processing.Abstractions.ControllerReadingInfo",
            "DS4Windows.UI.ControllerReadingsControl|DS4Windows.Processing.Abstractions.IControllerReadingsSource",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IAppSettingsService",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IChangelogService",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IEnvironmentService",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IPathService",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.ControllerSlotChangedEventArgs",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IAutoProfileService",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IControllerSlotQuery",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IControllerSlotView",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IIpcCommandHost",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IIpcCommandProcessor",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IServiceLifecycle",
            "DS4Windows.UI.OutputSlotManagerControl|DS4Windows.Processing.Abstractions.IOutputSlotView",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IAppSettingsService",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IEnvironmentService",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IProfileRepository",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IProfileSettingsService",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IRemovedGyroSwipePrompt",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IRemovedShiftPrompt",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.GyroSlotSettings",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.ProfileSlotNames",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.ProfileSlotSettings",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.RemovedGyroSwipeNotice",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.RemovedShiftNotice",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.SpecialAction",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.StickDeadZoneInfo",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Processing.Abstractions.IControllerReadingsSource",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Processing.Abstractions.IControllerSlotCommands",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch",
            "DS4Windows.UI.RecordBoxWindow|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.RecordBox|DS4Windows.Core.Abstractions.IMouseInputHook",
            "DS4Windows.UI.RecordBox|DS4Windows.Core.Abstractions.IWindowSystem",
            "DS4Windows.UI.RecordBox|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.StickCalibrationWindow|DS4Windows.Processing.Abstractions.IControllerReadingsSource",
            "DS4Windows.UI.WelcomeDialog|DS4Windows.Core.Abstractions.IEnvironmentService",
        };
    }
}
