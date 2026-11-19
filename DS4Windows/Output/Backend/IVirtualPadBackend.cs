using System;

namespace DS4Windows.Output.Backend
{
    /// <summary>
    /// 仮想パッドを作るドライバー（現在は ViGEmBus）の窓口。ドライバーの型（<c>Nefarius.ViGEm.Client</c>）を参照してよいのは、
    /// この契約の実装（<c>Output/Backend</c>）だけに限る。第 3 層の他の型は、ここから受け取る <see cref="IXbox360PadTarget"/>／<see cref="IDs4PadTarget"/> だけを使う。
    /// 破棄の順序（ドライバー通信の出力キューを止めてから、<see cref="Stop"/> でクライアントを破棄する）は <c>ServiceLifecycle.Stop</c> が守る。
    /// </summary>
    public interface IVirtualPadBackend
    {
        /// <summary>ドライバーのクライアントが作られている（<see cref="Start"/> に成功し、<see cref="Stop"/> がまだ）。</summary>
        bool IsStarted { get; }

        /// <summary>クライアントを作る。失敗しても例外は投げず、<see cref="IsStarted"/> が false のままになる（呼び出し側が確かめる）。</summary>
        void Start();

        /// <summary>クライアントを破棄する。作られていなければ何もしない。</summary>
        void Stop();

        /// <summary>仮想 Xbox 360 パッドの対象を作る。<see cref="IsStarted"/> が false のときは <see cref="InvalidOperationException"/>。</summary>
        IXbox360PadTarget CreateXbox360Target();

        /// <summary>仮想 DS4 パッドの対象を作る。<see cref="IsStarted"/> が false のときは <see cref="InvalidOperationException"/>。</summary>
        IDs4PadTarget CreateDs4Target();
    }
}
