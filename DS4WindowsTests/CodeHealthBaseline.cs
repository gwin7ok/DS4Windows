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

        /// <summary>
        /// <c>#nullable enable</c> を、全ファイルに持たせるフォルダー（本体からの相対パス。末尾が <c>/</c> でなければ、そのファイル 1 つ）。
        /// 層ごとに有効にしていく（PhaseRV9）。増やす方向だけで、減らさない。プロジェクト全体の <c>&lt;Nullable&gt;</c> へ切り替えたら、この表は不要になる。
        /// </summary>
        public static readonly string[] NullableEnabledFolders =
        {
            "Core/Abstractions/",
            "Composition/",
            "Core/Model/",
            "Core/Logging/",
            "Core/Utilities/",
            "Core/Updater/",
            "Core/Profiles/",
            "Core/Platform/",
            "Input/",
            "Output/",
            "Processing/Abstractions/",
            "Processing/Gyro/",
            "Processing/Macro/",
            "Processing/Services/",
            "Processing/Synthetic/",
            "Processing/Touchpad/",
            "Processing/ControllerSlotManager.cs",
            "Processing/ControllerStateFieldMapping.cs",
            "Processing/DS4TimedStickAxisValue.cs",
            "Processing/DeviceRuntimeState.cs",
            "Processing/Mapping.cs",
            "Processing/MappingDoneEventArgs.cs",
            "Processing/PostMapStickData.cs",
            "Processing/ProfileUntriggerSnapshot.cs",
            "Processing/RepeatHelper.cs",
            "Processing/StickMotionState.cs",
            "App.xaml.cs",
        };

        /// <summary><c>#nullable enable</c> のファイルで、理由コメントのない後置の <c>!</c>（null 許容の抑止）。理由は同じ行か直前の行の <c>//</c> に書く。</summary>
        public const int NullForgivingWithoutReason = 0;

        /// <summary><c>#pragma warning disable</c> で null 許容の警告（CS86xx・CS87xx）を消している行。使わない。</summary>
        public const int NullableWarningPragmas = 0;
    }
}
