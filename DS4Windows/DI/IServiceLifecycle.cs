using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6f2（Phase9-E6-Plan.md §9.2）: サービスの開始・停止・ホットプラグと、その状態・イベント。
    /// 以前は <c>ControlService.Start</c>／<c>Stop</c>／<c>HotPlug</c>／<c>PrepareAbort</c> と、公開フィールド <c>running</c> ほか 4 つ、イベント 5 つだった。
    /// <c>ControlService</c> は、同名の公開メンバーを転送で残す（UI が直接使うため。イベントの送り主は <c>ControlService</c>）。
    /// </summary>
    public interface IServiceLifecycle
    {
        /// <summary>サービスが動作中か（旧 <c>running</c>）。</summary>
        bool Running { get; set; }

        /// <summary>デバイスの列挙のループを続けるか（旧 <c>loopControllers</c>。ViGEm の失敗で false にされる）。</summary>
        bool LoopControllers { get; set; }

        /// <summary>開始・停止・ホットプラグの最中か（旧 <c>inServiceTask</c>。ViGEm の失敗の処理が、終わるまで待つ）。</summary>
        bool InServiceTask { get; set; }

        /// <summary>接続中のコントローラーの数（旧 <c>activeControllers</c>）。</summary>
        int ActiveControllers { get; set; }

        /// <summary>システムのサスペンド中か（旧 <c>suspending</c>。MainWindow が設定する。停止時に Bluetooth を切断する判断に使う）。</summary>
        bool Suspending { get; set; }

        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        event EventHandler ServiceStarted;

        event EventHandler PreServiceStop;

        event EventHandler ServiceStopped;

        event EventHandler RunningChanged;

        /// <summary>ホットプラグでコントローラーがスロットに入ったとき（デバイスとスロット番号）。</summary>
        event Action<DS4Device, int> HotplugController;

        bool Start(bool showlog = true);

        bool Stop(bool showlog = true, bool immediateUnplug = false);

        bool HotPlug();

        void PrepareAbort();
    }
}
