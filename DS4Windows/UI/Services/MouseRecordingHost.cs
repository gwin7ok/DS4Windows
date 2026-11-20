#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Macro;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// マクロ記録画面（RecordBox）が使う、マウスの移動の記録の窓口。画面全体のマウス入力の監視（<see cref="IMouseInputHook"/>）と、
    /// 記録のロジック（<see cref="MacroPointerRecorder"/>）の生成を包み、画面が業務のサービスの型を直接参照しないようにする。
    /// イベントは、<see cref="Start"/> を呼んだスレッド（UI スレッド）で起こる。ハンドラーはすぐに戻ること。
    /// </summary>
    public sealed class MouseRecordingHost
    {
        private readonly IWindowSystem windowSystem;
        private readonly IMouseInputHook mouseHook;

        public MouseRecordingHost(IWindowSystem windowSystem, IMouseInputHook mouseHook)
        {
            ArgumentNullException.ThrowIfNull(windowSystem);
            this.windowSystem = windowSystem;
            ArgumentNullException.ThrowIfNull(mouseHook);
            this.mouseHook = mouseHook;
        }

        public event Action<int, int> PointerMoved
        {
            add => mouseHook.PointerMoved += value;
            remove => mouseHook.PointerMoved -= value;
        }

        public event Action<int, bool, int, int> ButtonChanged
        {
            add => mouseHook.ButtonChanged += value;
            remove => mouseHook.ButtonChanged -= value;
        }

        public MacroPointerRecorder CreateRecorder(MacroPointerMoveMode mode)
            => new MacroPointerRecorder(mode, windowSystem, Environment.ProcessId);

        public bool Start() => mouseHook.Start();

        public void Stop() => mouseHook.Stop();
    }
}
