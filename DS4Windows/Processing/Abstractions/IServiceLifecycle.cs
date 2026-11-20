using System;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// サービスの開始・停止・ホットプラグと、その状態・イベント。
    /// 以前は <c>ControlService.Start</c>／<c>Stop</c>／<c>HotPlug</c>／<c>PrepareAbort</c> と、公開フィールド <c>running</c> ほか 4 つ、イベント 5 つだった。
    /// <c>ControlService</c> は、同名の公開メンバーを転送で残す（UI が直接使うため。イベントの送り主は <c>ControlService</c>）。
    /// </summary>
    public interface IServiceLifecycle
    {
        /// <summary>サービスが動作中か。</summary>
        bool Running { get; set; }

        /// <summary>デバイスの列挙のループを続けるか。</summary>
        bool LoopControllers { get; set; }

        /// <summary>開始・停止・ホットプラグの最中か。</summary>
        bool InServiceTask { get; set; }

        /// <summary>接続中のコントローラーの数。</summary>
        int ActiveControllers { get; set; }

        /// <summary>システムのサスペンド中か。</summary>
        bool Suspending { get; set; }

        event EventHandler ServiceStarted;

        event EventHandler PreServiceStop;

        event EventHandler ServiceStopped;

        event EventHandler RunningChanged;

        /// <summary>ホットプラグでコントローラーがスロットに入ったとき（デバイスとスロット番号）。</summary>
        event Action<InputDeviceCore, int> HotplugController;

        bool Start(bool showlog = true);

        bool Stop(bool showlog = true, bool immediateUnplug = false);

        bool HotPlug();

        void PrepareAbort();
    }
}
