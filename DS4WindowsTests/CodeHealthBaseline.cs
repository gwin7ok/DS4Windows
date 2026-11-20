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
        /// 層ごとに有効にしていく。増やす方向だけで、減らさない。プロジェクト全体の <c>&lt;Nullable&gt;</c> へ切り替えたら、この表は不要になる。
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
            "Core/Settings/",
            "Core/Persistence/",
            "Processing/Actions/",
            "UI/Abstractions/",
            "UI/Converters/",
            "UI/Services/",
            "UI/Controls/",
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
            "Processing/PostMapStickData.cs",
            "Processing/ProfileUntriggerSnapshot.cs",
            "Processing/RepeatHelper.cs",
            "Processing/StickMotionState.cs",
            "App.xaml.cs",
        };

        /// <summary><c>#nullable enable</c> のファイルで、理由コメントのない後置の <c>!</c>（null 許容の抑止）。理由は同じ行か直前の行の <c>//</c> に書く。</summary>
        public const int NullForgivingWithoutReason = 0;

        /// <summary>何も書かない空の <c>catch</c>（<c>Core/Persistence/ProfileSections</c> を除く）。0 を保つ。理由のコメントか、型の限定を書く（<c>Log-Policy.md</c> §12）。</summary>
        public const int BlankEmptyCatches = 0;

        /// <summary><c>Core/Persistence/ProfileSections</c> の何も書かない空の <c>catch</c>。RV5 の宣言化で 1 つの部品へ畳むまで凍結し、畳んだら 0 にする。</summary>
        public const int BlankEmptyCatchesInProfileSections = 36;

        /// <summary>本体がコメントだけの空の <c>catch</c>。理由の質は <c>PhaseRV2-Catch-Inventory.md</c> で人が見る。減らす方向だけ。</summary>
        public const int CommentOnlyEmptyCatches = 250;

        /// <summary><c>try { ログ } catch { }</c> のように、ログ呼び出しを包む行。<c>AppLogger</c> が例外を出さないため、0 を保つ。</summary>
        public const int LogWrappingTryLines = 0;

        /// <summary>イベントハンドラーの形（第 1 引数が <c>object</c>）ではない <c>async void</c>。<c>async Task</c> にして、呼び出し側で <c>await</c> する。</summary>
        public const int NonHandlerAsyncVoid = 0;

        /// <summary>経緯の参照（<c>Phase</c> ＋数字）を含む行（本体の .cs）。減らしたら基準も下げる。増やさない。</summary>
        public const int PhaseReferenceLines = 225;

        /// <summary><c>#pragma warning disable</c> で null 許容の警告（CS86xx・CS87xx）を消している行。使わない。</summary>
        public const int NullableWarningPragmas = 0;
    }
}
