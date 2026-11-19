namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="CodeHealthRatchetTests"/> の基準値（コードの健全さを数字で守る項目）。
    /// 数字を減らしたら、同じコミットでここも更新する（増やす方向の更新は、是正の代わりにしない）。
    /// 項目を足すときは、<see cref="CodeHealthBaseline"/> に定数を足し、ここに同名の検査を足す。
    /// </summary>
    internal static class CodeHealthBaseline
    {
        /// <summary>
        /// <c>Core</c> の中で、上位層（Processing・UI・Output・Composition・Input）の名前空間を <c>using</c> するファイル。依存は上位→下位（1 章）。
        /// 残り 1 件: <c>ProfileGyroMouseLoadSection</c> が、既定値の定数 <c>MouseCursor.GYRO_MOUSE_DEADZONE</c>（Processing.Touchpad）を読む。
        /// 定数なので、型の検査（<c>LayerDependencyRuleTests</c>）には現れない。既定値を Core の側（設定の既定値）へ移し、
        /// <c>MouseCursor</c> がそれを参照する向きにすれば 0 になる（後続の Phase で扱う）。
        /// </summary>
        public const int CoreUpperLayerUsingFiles = 1;

        /// <summary>
        /// <c>Core</c> の中で、<c>System.Windows*</c> を <c>using</c> するファイル。横断基盤は WPF に依存しないのが目標。
        /// 残り 3 件: <c>ProfilePresets</c>（<c>KeyInterop</c>・<c>Key</c> で仮想キーコードを得る）、<c>AutoProfileHolder</c>・<c>ProfileList</c>
        /// （<c>BindingOperations.EnableCollectionSynchronization</c>）。前者は仮想キーコードの定数化、後者は同期の窓口（UI 側の契約）で 0 になる（後続の Phase で扱う）。
        /// </summary>
        public const int CoreWindowsUsingFiles = 3;

        /// <summary>別名 <c>using NonFormTimer = System.Timers.Timer;</c> を宣言しているのに、ファイルの中で使っていない行。</summary>
        public const int UnusedNonFormTimerAliasLines = 0;
    }
}
