using System;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 入力 1 フレームごとに呼ばれる「モーション送出」の呼び出しを、機器へ付け外しできる契約（UDP のモーションサーバーが使う）。
    /// 付け外しは、機器のイベントのスレッドで行うため、呼び出し側は <see cref="queueEvent"/> へ積んで実行する（順序とスレッドは従来と同じ）。
    /// </summary>
    public interface IMotionSource : IPadStatusSource
    {
        /// <summary>機器のイベントのスレッドで実行する処理を積む。</summary>
        void queueEvent(Action act);

        /// <summary>フレームごとの呼び出しを登録する（すでにあれば置き換える）。</summary>
        void StartMotionCallback(Action onReport);

        /// <summary>登録を外して忘れる。</summary>
        void StopMotionCallback();

        /// <summary>登録は覚えたまま、呼び出しを一時的に外す。</summary>
        void PauseMotionCallback();

        /// <summary>一時的に外した呼び出しを、もとに戻す。</summary>
        void ResumeMotionCallback();
    }
}
