namespace DS4Windows.Services
{
    /// <summary>
    /// プロファイル切替の通知（独自ウィンドウ／トースト）を出すかどうかの判定（純粋関数）。
    /// 2026-10-19（AutoProfile-NoController-Investigation.md 案A）: 自動プロファイル由来の切替は、
    /// 元のコード（AutoProfileChecker）と同じく AutoProfileSwitchNotifyChoice で決める。
    /// 2026-09-16 の「通知判定を UI 層へ委譲」（978104ff ほか）で、この判定が落ちていた。
    /// </summary>
    public static class AutoProfileNotificationPolicy
    {
        /// <param name="controllerItemExists">UI にそのスロットのコントローラー項目があるか（接続中か）。</param>
        public static bool ShouldNotify(ProfileChangeSource source,
            AutoProfileDisplayProfileSwitchChoices choice, bool controllerItemExists)
        {
            if (source != ProfileChangeSource.AutoProfile)
                return true;

            // 未接続のスロットへ適用した切替（接続前の事前適用、通常プロファイルへの復帰）は通知しない
            if (!controllerItemExists)
                return false;

            return choice == AutoProfileDisplayProfileSwitchChoices.Notification ||
                   choice == AutoProfileDisplayProfileSwitchChoices.LogAndNotification;
        }
    }
}
