#nullable enable
namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// フォルダー・プログラム・Web ページを OS に開かせる窓口。ViewModel が <c>Process</c> を直接使わず、WPF なしでテストできるようにする契約
    /// （テストでは、呼び出しを記録する偽物を渡す）。起動の失敗（ファイルがない・権限がない）は、実装が記録して続ける。
    /// </summary>
    public interface IShellLauncher
    {
        /// <summary>フォルダー・ファイルを、関連づけられたプログラムで開く。</summary>
        void OpenPath(string path);

        /// <summary>プログラムを起動する。</summary>
        void RunProgram(string fileName, string? arguments = null);

        /// <summary>ファイルがあるときだけ、プログラムを起動する。</summary>
        void RunProgramIfExists(string path);

        /// <summary>管理者の権限でプログラムを起動し、終わるまで待つ（ドライバーの導入）。</summary>
        void RunElevatedAndWait(string fileName, string arguments);

        /// <summary>既定のブラウザーで Web ページを開く。</summary>
        void OpenUrl(string url);
    }
}
