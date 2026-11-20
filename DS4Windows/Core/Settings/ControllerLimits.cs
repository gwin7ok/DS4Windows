namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// コントローラーの台数・スティックの中点などの定数。
    /// <c>Global</c> の同名の定数は、この値を指す別名として残してある（呼び出し元約 250 件は Step10 で順に移す）。
    /// </summary>
    public static class ControllerLimits
    {
        /// <summary>同時に扱うコントローラーの最大数。</summary>
        public const int MaxControllerCount = 8;

        /// <summary>プロファイル項目の数。テスト用プロファイルを 1 つ含む。</summary>
        public const int TestProfileItemCount = MaxControllerCount + 1;

        /// <summary>テスト用プロファイルの添字。</summary>
        public const int TestProfileIndex = TestProfileItemCount - 1;

        /// <summary>旧 XInput のコントローラー数。</summary>
        public const int OldXInputControllerCount = 4;

        /// <summary>スティック軸の中点。</summary>
        public const byte StickAxisMidpoint = 128;
    }
}
