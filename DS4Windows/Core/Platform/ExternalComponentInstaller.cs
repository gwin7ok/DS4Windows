#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HttpProgress;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// 外部コンポーネントの導入の流れ: 一時ファイルの掃除 → ダウンロード（進捗を通知）→ 導入プログラムの起動 →
    /// 終了の監視（一定の間隔で確認）→ 導入の確認 → 一時ファイルの削除。結果は <see cref="InstallProgress"/> で通知する。
    /// 画面は、通知を文言とボタンの状態へ写すだけ。
    /// </summary>
    public sealed class ExternalComponentInstaller
    {
        /// <summary>導入プログラムの終了を確認する間隔（従来のタイマーの既定値と同じ）。</summary>
        public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

        private readonly HttpClient _httpClient;
        private readonly IInstallerProcessLauncher _launcher;
        private readonly TimeProvider _time;
        private readonly string _workDirectory;

        public ExternalComponentInstaller(HttpClient httpClient, IInstallerProcessLauncher launcher, TimeProvider time, string workDirectory)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(launcher);
            ArgumentNullException.ThrowIfNull(time);
            ArgumentNullException.ThrowIfNull(workDirectory);
            _httpClient = httpClient;
            _launcher = launcher;
            _time = time;
            _workDirectory = workDirectory;
        }

        /// <summary>
        /// 導入を行う。取り消されたときは、通知を出さずに戻る（画面が閉じられたとき）。
        /// 例外（ネットワーク・ファイルの失敗）は呼び出し側へ伝える。
        /// </summary>
        public async Task InstallAsync(ExternalComponentSpec spec, IProgress<InstallProgress> progress, CancellationToken cancellation)
        {
            ArgumentNullException.ThrowIfNull(spec);
            ArgumentNullException.ThrowIfNull(progress);

            string finalPath = Path.Combine(_workDirectory, spec.FileName);
            string tempPath = finalPath + ".tmp";
            DeleteIfExists(finalPath);
            DeleteIfExists(tempPath);

            Progress<ICopyProgress> copyProgress = new Progress<ICopyProgress>(x =>
                progress.Report(new InstallProgress(InstallStage.Downloading, x.PercentComplete)));

            bool downloaded;
            try
            {
                using (FileStream downloadStream = new FileStream(tempPath, FileMode.CreateNew))
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(spec.DownloadUrl, downloadStream, copyProgress, cancellation).ConfigureAwait(true);
                    downloaded = response.IsSuccessStatusCode;
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return;   // 画面が閉じられた。ダウンロードを止め、通知は出さない
            }

            if (downloaded)
            {
                File.Move(tempPath, finalPath);
            }

            Func<bool>? hasExited = File.Exists(finalPath) ? _launcher.Start(finalPath) : null;
            if (hasExited == null)
            {
                progress.Report(new InstallProgress(InstallStage.Failed));
                return;
            }

            progress.Report(new InstallProgress(InstallStage.Installing));

            while (!hasExited())
            {
                try
                {
                    await Task.Delay(PollInterval, _time, cancellation).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    return;   // 画面が閉じられた。以後は何も通知しない
                }
            }

            bool installed = spec.IsInstalled();
            DeleteIfExists(finalPath);
            progress.Report(new InstallProgress(installed ? InstallStage.Completed : InstallStage.Failed));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
