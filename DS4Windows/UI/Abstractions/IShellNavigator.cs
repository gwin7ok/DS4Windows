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

        /// <summary>プロファイル一覧で、新しいプロファイルの編集を始める。</summary>
        void NewProfile();

        /// <summary>選択中のプロファイルを編集する。</summary>
        void EditProfile();

        /// <summary>選択中のプロファイルを削除する（確認つき）。</summary>
        void DeleteProfile();

        /// <summary>選択中のプロファイルの名前を変える。</summary>
        void RenameProfile();

        /// <summary>選択中のプロファイルを複製する（名前の入力欄を出す）。</summary>
        void DuplicateProfile();

        /// <summary>プロファイルをファイルから取り込む。</summary>
        void ImportProfile();

        /// <summary>選択中のプロファイルをファイルへ書き出す。</summary>
        void ExportProfile();

        /// <summary>機器（スロット）に割り当てたプロファイルを編集する。</summary>
        void EditDeviceProfile(int deviceIndex);

        /// <summary>機器（スロット）用の新しいプロファイルの編集を始める。</summary>
        void NewDeviceProfile(int deviceIndex);

        /// <summary>タッチパッドのスワイプによるプロファイル切替の、定期の確認を始める・止める。</summary>
        void SetHotkeysActive(bool active);

        /// <summary>設定画面の「起動時に実行」の詳細（実行の間隔など）を出す・隠す。</summary>
        void SetRunStartPanelVisible(bool visible);

        /// <summary>ログを、ファイルへ書き出す（保存先を選ばせる）。</summary>
        void ExportLog();

        /// <summary>ログの一覧と、最下部の 1 行メッセージを消す。</summary>
        void ClearLog();

        /// <summary>ドライバーの導入のあと、設定画面のドライバーの状態の表示を更新する。</summary>
        void RefreshDriverCheck();

        /// <summary>ログ一覧で選んだ行の全文を、別の窓で見せる。</summary>
        void ShowLogMessage();
    }
}
