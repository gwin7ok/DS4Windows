#nullable enable
namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// メイン画面から他の画面（子ウィンドウ）を開く窓口。ViewModel は <c>Window</c> を作らず、この契約で頼む。
    /// 実装は画面（View）が持ち、自分を親（Owner）にして開く。ViewModel とは、画面ができたあとに結ぶ（画面が ViewModel より後に作られるため）。
    /// </summary>
    public interface IShellNavigator
    {
        /// <summary>バージョン情報の画面を、モーダルで開く。</summary>
        void ShowAbout();

        /// <summary>更新履歴の画面を、モーダルで開く。</summary>
        void ShowChangelog();

        /// <summary>デバイスの登録オプションの画面を開く。</summary>
        void ShowDeviceOptions();

        /// <summary>更新（インストール）の画面を、モーダルで開く。</summary>
        void ShowUpdater(string version);
    }
}
