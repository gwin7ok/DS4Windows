using System;

namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 Xbox 360 パッド 1 台の操作。ドライバーの型を使わない値だけで表す（ボタンのビットは <see cref="Xbox360Buttons"/>）。</summary>
    public interface IXbox360PadTarget
    {
        byte LeftTrigger { set; }
        byte RightTrigger { set; }
        short LeftThumbX { set; }
        short LeftThumbY { set; }
        short RightThumbX { set; }
        short RightThumbY { set; }

        /// <summary>XInput のユーザー番号。<see cref="Connect"/> の後に読む（取れないときは例外）。</summary>
        int UserIndex { get; }

        void SetButtonsFull(ushort buttons);
        void SubmitReport();
        void ResetReport();
        void Connect();
        void Disconnect();

        /// <summary>
        /// 振動の戻りを受け取る。返した <see cref="IDisposable"/> を破棄すると購読をやめる。
        /// 引数は（大きいモーター、小さいモーター）。
        /// </summary>
        IDisposable SubscribeFeedback(Action<byte, byte> onRumble);
    }
}
