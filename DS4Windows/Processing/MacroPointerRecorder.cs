using System;
using DS4Windows.Services;

namespace DS4Windows
{
    /// <summary>
    /// Phase12-Step3 3e（R-3）: マクロの記録で、マウスのポインターの移動とボタンの位置を、移動ステップの値へ変える部分（フックから切り離した純粋なロジック）。
    /// 次の決定（Phase12-Step3-Plan.md D3・D4）に従う。
    /// <list type="bullet">
    /// <item>移動は、前回の記録から <see cref="MinDistance"/> px 以上動き、かつ <see cref="MinIntervalMilliseconds"/> ms 以上経ったときだけ記録する。</item>
    /// <item>ボタンの押下・離上の直前に、位置が記録済みの最後の位置と違えば、その位置の移動を先に入れる（クリック位置の取りこぼしを防ぐ）。</item>
    /// <item>DS4Windows 自身のウィンドウの上の操作は、記録しない（Stop ボタンなど。記録ボックスの上のボタンは、画面側の従来の記録が扱う）。</item>
    /// <item>ウィンドウ基準では、ポインターの下にあるウィンドウの外枠の左上を原点にする（クリックで前面になるため、再生時の前面ウィンドウと一致する）。</item>
    /// <item>移動ステップは、<see cref="MaxMoveSteps"/> 個まで。</item>
    /// </list>
    /// </summary>
    public sealed class MacroPointerRecorder
    {
        public const int MinDistance = 5;
        public const long MinIntervalMilliseconds = 20;
        public const int MaxMoveSteps = 2000;

        private readonly MacroPointerMoveMode _mode;
        private readonly IWindowSystem _windowSystem;
        private readonly int _ownProcessId;

        private bool _hasLast;
        private int _lastX;
        private int _lastY;
        private long _lastTime;

        public MacroPointerRecorder(MacroPointerMoveMode mode, IWindowSystem windowSystem, int ownProcessId)
        {
            _mode = mode;
            _windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            _ownProcessId = ownProcessId;
        }

        /// <summary>記録した移動ステップの数。</summary>
        public int MoveCount { get; private set; }

        /// <summary>移動ステップの上限に達した。以後、移動は記録しない。</summary>
        public bool LimitReached => MoveCount >= MaxMoveSteps;

        /// <summary>ポインターが動いた。記録するなら true と、移動ステップの値を返す。</summary>
        public bool TryRecordMove(int x, int y, long timestampMilliseconds, out int macroValue)
        {
            macroValue = 0;
            if (LimitReached) return false;

            if (_hasLast)
            {
                if (timestampMilliseconds - _lastTime < MinIntervalMilliseconds) return false;
                if (Math.Abs(x - _lastX) < MinDistance && Math.Abs(y - _lastY) < MinDistance) return false;
            }

            if (!TryResolve(x, y, out macroValue)) return false;

            Remember(x, y, timestampMilliseconds);
            return true;
        }

        /// <summary>
        /// ボタンの押下・離上があった。ボタンを記録するなら true（自分のウィンドウの上などは false）。
        /// <paramref name="hasMove"/> が true のときは、ボタンの前に、<paramref name="moveValue"/> の移動ステップを入れる。
        /// </summary>
        public bool TryBeginButton(int x, int y, long timestampMilliseconds, out bool hasMove, out int moveValue)
        {
            hasMove = false;
            moveValue = 0;

            if (!TryResolve(x, y, out int value)) return false;

            bool alreadyThere = _hasLast && _lastX == x && _lastY == y;
            if (!alreadyThere && !LimitReached)
            {
                hasMove = true;
                moveValue = value;
                Remember(x, y, timestampMilliseconds);
            }

            return true;
        }

        private void Remember(int x, int y, long timestampMilliseconds)
        {
            _hasLast = true;
            _lastX = x;
            _lastY = y;
            _lastTime = timestampMilliseconds;
            MoveCount++;
        }

        /// <summary>画面の座標を、記録するモードの移動ステップの値へ変える。自分のウィンドウの上・基準のウィンドウが取れないときは false。</summary>
        private bool TryResolve(int x, int y, out int macroValue)
        {
            macroValue = 0;
            bool found = _windowSystem.TryGetWindowAtPoint(x, y, out int processId, out int originX, out int originY);
            if (found && processId == _ownProcessId) return false;

            if (_mode == MacroPointerMoveMode.WindowRelative)
            {
                if (!found) return false;
                macroValue = new MacroPointerMove(MacroPointerMoveMode.WindowRelative, x - originX, y - originY).ToMacroValue();
            }
            else
            {
                macroValue = new MacroPointerMove(MacroPointerMoveMode.Absolute, x, y).ToMacroValue();
            }

            return true;
        }
    }
}
