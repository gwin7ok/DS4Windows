#nullable enable
namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// アプリの起動の 1 手順ずつ。順序と早期の終了の判断は <see cref="AppStartupSequence"/> が持ち、各手順の中身は <c>App</c> が実装する。
    /// 状態（起動引数、初回起動か、選んだ言語）は、実装側が持つ。
    /// </summary>
    public interface IAppStartupSteps
    {
        /// <summary>ホストを作る前の準備（通知の登録、保存場所の準備など）。</summary>
        void RunPreHostBootstrap();

        /// <summary>管理者権限で再起動された「導入の完了」の処理をしたら真（その場合、以降は何もしない）。</summary>
        bool TryHandleCompleteInstall();

        /// <summary>起動引数を読み、起動の種類（補助の起動を含む）に応じて処理する。</summary>
        void ParseAndHandleOptions();

        /// <summary>補助の起動などで、通常の起動を続けないとき真。</summary>
        bool ExitRequested { get; }

        void SetProcessPriorities();

        /// <summary>この起動が唯一のインスタンスなら真。偽なら、別のインスタンスへ知らせて終える。</summary>
        bool EnsureSingleInstance();

        void RotateLogsAndLogStartupDiagnostics();

        /// <summary>タイマーの精度、キーボードのリピート、ViGEmBus の情報、単一起動の通知の待ち受け。</summary>
        void InitializeProcessWideSettings();

        /// <summary>マウスホイールを OS の設定に合わせる。最初の画面ができる前に、1 回だけ。</summary>
        void RegisterPageScrollWheel();

        /// <summary>DI のホストを、明示的に作る。</summary>
        void CreateHost();

        /// <summary>入力の制御（<c>IRuntimeBootstrapper</c> など）を、別スレッドで作って待つ。ホストの作成の後。</summary>
        void CreateControlService();

        /// <summary>起動後に使うサービスを、ホストから解決する。<see cref="CreateControlService"/> の直後。</summary>
        void InitializePostHostServices();

        /// <summary>設定の「初回起動か」を読む。<see cref="InitializePostHostServices"/> の後。</summary>
        void ReadFirstRunFlag();

        /// <summary>初回起動の言語の選択。取り消されたら偽（以降は何もしない）。</summary>
        bool RunFirstRunLanguageSelection();

        /// <summary>初回起動の保存場所の選択と、フォルダーの用意。取り消されたら偽（以降は何もしない）。</summary>
        bool RunFirstRunSaveLocationAndSkeleton();

        void EnsureMacrosFolder();

        void LoadAppSettings();

        void ConfigureLoggingAndDiagnostics();

        void RunFirstLaunchSetup();

        /// <summary>プロファイルの固定 ID と SA の読み込み、言語・テーマの適用。メイン画面の生成の前。</summary>
        void PrepareProfilesCultureAndTheme();

        void ShowMainWindowAndFinish();
    }
}
