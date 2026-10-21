namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I6b: Controls タブに割り当てられる、SA 専用の種別（<see cref="DS4ControlSettings.ActionType.Special"/>）のうち、<b>実行時に束縛を作る種別</b>の規則の表。
    /// 以前は <c>ControlBindingBuilder.BuiltSpecialTypes</c>（暫定の配列）と、<c>ActionTypeCatalog</c> の「Controls で保存できる」の指定が、別々に持っていた。
    /// 群ごとの縦通し（Phase10-Step4-Plan.md §5）で、保存・実行・画面が通った種別から足す。Check Battery と Multi-action を Controls へ移す作業（W7・W8 相当）では、ここに足す。
    /// Macro は Macro の割り当て（3 項目の入れ物）で扱うため含めない。
    /// </summary>
    public static class ControlsSpecialTypeTable
    {
        /// <summary>種別が、Controls で保存でき、実行時に束縛が作られるか。</summary>
        public static bool IsSupported(SpecialAction.ActionTypeId type)
        {
            switch (type)
            {
                case SpecialAction.ActionTypeId.Program:
                case SpecialAction.ActionTypeId.Profile:
                case SpecialAction.ActionTypeId.DisconnectBT:
                case SpecialAction.ActionTypeId.GyroCalibrate:
                case SpecialAction.ActionTypeId.BatteryCheck:   // Phase10-Step6 W7 相当
                case SpecialAction.ActionTypeId.MultiAction:   // Phase10-Step6 W8 相当
                    return true;
                default:
                    return false;
            }
        }
    }
}
