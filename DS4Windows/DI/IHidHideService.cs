using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: HidHide（コントローラーを他のアプリから隠すドライバー）との連携。
    /// 以前は <c>ControlService</c> の HidHide 系のメンバー（ホワイトリストへの自分の登録、影響を受けるデバイスの一覧の更新と判定、状態 <c>hidDeviceHiding*</c>）だった。
    /// <c>ControlService.CheckHidHidePresence</c>／<c>UpdateHidHiddenAttributes</c> は、薄い委譲として残す（呼び出し側は 9-F まで変えない）。
    /// </summary>
    public interface IHidHideService
    {
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>
        /// HidHide のアプリケーションのホワイトリストに、実行ファイルがあるかを確かめ、<paramref name="AddExe"/> に従って追加／削除する。
        /// 空のパスのときは DS4Windows 自身を追加する（起動時）。HidHide が未導入、制御デバイスが開けない、インバースのときは何もしない。
        /// </summary>
        void CheckPresence(string ExePath = "", string ExeName = "Autoprofile Exe", bool AddExe = true);

        /// <summary>HidHide の有効状態と、非表示のデバイスの一覧を読み直す（旧 <c>UpdateHidHideAttributes</c>／<c>UpdateHidHiddenAttributes</c>）。</summary>
        void UpdateAttributes();

        /// <summary>デバイスが HidHide の非表示の対象か（HidHide が有効なときだけ判定する）。</summary>
        bool IsAffected(DS4Device dev);

        /// <summary>デバイスの排他状態を「HidHide の影響を受ける」にする（HidHide が導入済みのとき）。</summary>
        void MarkAffected(DS4Device dev);
    }
}
