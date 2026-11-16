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
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IAppSettingsService",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IEnvironmentService",
            "DS4Windows.UI.MainWindow|DS4Windows.Core.Abstractions.IPathService",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IControllerSlotView",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch",
            "DS4Windows.UI.MainWindow|DS4Windows.Processing.Abstractions.IServiceLifecycle",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IEnvironmentService",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IProfileRepository",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Abstractions.IProfileSettingsService",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.DS4ControlSettings",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.ProfileSlotNames",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.SpecialAction",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Core.Settings.StickDeadZoneInfo",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Processing.Abstractions.IControllerSlotCommands",
            "DS4Windows.UI.ProfileEditor|DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch",
        };
    }
}
