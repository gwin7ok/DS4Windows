using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: HidHide ドライバーの制御デバイス（<c>\\.\HidHide</c>）の窓口。HidHide の設定を読み書きする。
    /// 実装は <c>HidHideAPIDevice</c>（internal）を包む。ドライバーなしで、ホワイトリストの確認などの判断をテストするための境界。
    /// </summary>
    public interface IHidHideApi : IDisposable
    {
        /// <summary>制御デバイスを開けているか。</summary>
        bool IsOpen();

        /// <summary>HidHide によるデバイスの非表示が有効か。</summary>
        bool GetActiveState();

        /// <summary>非表示にするデバイスのインスタンス ID の一覧。</summary>
        List<string> GetBlacklist();

        /// <summary>HidHide の設定で、アプリケーションのホワイトリストが反転（インバース）されているか。</summary>
        bool GetWhiteListInverseState();

        /// <summary>デバイスを見られるアプリケーションの一覧（DOS デバイス形式のパス）。</summary>
        List<string> GetWhitelist();

        /// <summary>ホワイトリストを置き換える。</summary>
        bool SetWhitelist(List<string> instances);
    }
}
