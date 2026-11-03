using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase12-Step3 3b（R-3）: マクロの「マウスポインターの移動」ステップ（モード、X、Y）と、マクロの整数 1 個との相互変換。
    /// マクロは、整数 1 個が 1 ステップ（Phase12-Step3-Plan.md D1）。この値は 1,300,000,000 以上（既存のライトバー［最大 1,255,255,255］と重ならない）に、
    /// <c>BaseValue + モード × 2^28 + (X + オフセット) × 2^14 + (Y + オフセット)</c> で詰める。X・Y は各 14 ビット。
    /// 範囲は、絶対が −4,096〜12,287（左・上のモニターの負の座標と、4K の 3 枚横並びを含む）、ウィンドウ基準が −8,192〜8,191。範囲外は端に丸める。
    /// 古い版は、この値を「ライトバーの色」と解釈する（色が崩れるだけ）。
    /// </summary>
    public readonly struct MacroPointerMove : IEquatable<MacroPointerMove>
    {
        /// <summary>移動ステップの値の下限。これ以上の値（Int32 の上限まで）が、移動ステップの範囲。</summary>
        public const int BaseValue = 1_300_000_000;

        private const int AxisBits = 14;
        private const int AxisCount = 1 << AxisBits;
        private const int ModeSpan = AxisCount * AxisCount;   // 2^28
        private const int AbsoluteOffset = 4096;
        private const int WindowRelativeOffset = 8192;

        public const int AbsoluteMin = -AbsoluteOffset;
        public const int AbsoluteMax = AxisCount - 1 - AbsoluteOffset;
        public const int WindowRelativeMin = -WindowRelativeOffset;
        public const int WindowRelativeMax = AxisCount - 1 - WindowRelativeOffset;

        public MacroPointerMoveMode Mode { get; }
        public int X { get; }
        public int Y { get; }

        /// <summary>座標は、モードごとの範囲へ丸める。</summary>
        public MacroPointerMove(MacroPointerMoveMode mode, int x, int y)
        {
            Mode = mode;
            X = Clamp(mode, x);
            Y = Clamp(mode, y);
        }

        /// <summary>マクロの値が、移動ステップの範囲（予約のモードを含む）にあるか。</summary>
        public static bool IsPointerMoveValue(int value) => value >= BaseValue;

        public static int MinOf(MacroPointerMoveMode mode) => mode == MacroPointerMoveMode.Absolute ? AbsoluteMin : WindowRelativeMin;

        public static int MaxOf(MacroPointerMoveMode mode) => mode == MacroPointerMoveMode.Absolute ? AbsoluteMax : WindowRelativeMax;

        /// <summary>マクロの値（整数）へ詰める。</summary>
        public int ToMacroValue()
        {
            int offset = OffsetOf(Mode);
            return BaseValue + ((int)Mode * ModeSpan) + ((X + offset) * AxisCount) + (Y + offset);
        }

        /// <summary>マクロの値から取り出す。範囲外の値、予約のモード（2）のときは false。</summary>
        public static bool TryFromMacroValue(int value, out MacroPointerMove move)
        {
            move = default;
            if (value < BaseValue) return false;

            int payload = value - BaseValue;
            int mode = payload / ModeSpan;
            if (mode != (int)MacroPointerMoveMode.Absolute && mode != (int)MacroPointerMoveMode.WindowRelative) return false;

            payload -= mode * ModeSpan;
            int offset = OffsetOf((MacroPointerMoveMode)mode);
            move = new MacroPointerMove((MacroPointerMoveMode)mode, (payload / AxisCount) - offset, (payload % AxisCount) - offset);
            return true;
        }

        /// <summary>リストに出す名前。例: <c>Move to (100, 200)</c>、<c>Move in window (10, -5)</c>。</summary>
        public string DisplayName => Mode == MacroPointerMoveMode.Absolute
            ? $"Move to ({X}, {Y})"
            : $"Move in window ({X}, {Y})";

        public bool Equals(MacroPointerMove other) => Mode == other.Mode && X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is MacroPointerMove other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Mode, X, Y);

        private static int OffsetOf(MacroPointerMoveMode mode) => mode == MacroPointerMoveMode.Absolute ? AbsoluteOffset : WindowRelativeOffset;

        private static int Clamp(MacroPointerMoveMode mode, int value) => Math.Min(Math.Max(value, MinOf(mode)), MaxOf(mode));
    }
}
