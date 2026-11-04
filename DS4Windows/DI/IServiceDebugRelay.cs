using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase13-Step10 10-e（Phase13-Step10-Plan.md）: 各サービスの <c>DebugMessage</c>（ログ）を 1 つにまとめる中継。
    /// 以前は <c>ControlService</c> が、6 つのサービス（HidHide、UDP モーション、遅延の監視、タッチパッドの切り替え、接続時の設定、切断・同期）を、ログの中継のためだけに引数で受け、
    /// それぞれの <c>DebugMessage</c> を自分の <c>LogDebug</c>（Debug イベント）へ流していた。いまは、中継がまとめて受け、<c>ControlService</c> は 1 つだけを購読する。
    /// ログの文言・レベル・経路（<c>ControlService.LogDebug</c> → <c>Debug</c> イベント → GUI のログ、ログファイル）は変えない。
    /// </summary>
    public interface IServiceDebugRelay
    {
        /// <summary>中継する 6 つのサービスの <c>DebugMessage</c>（メッセージ、警告か）。</summary>
        event Action<string, bool> DebugMessage;
    }
}
