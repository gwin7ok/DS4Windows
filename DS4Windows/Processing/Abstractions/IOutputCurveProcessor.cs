namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(1) (c)（Phase9-A1-Plan.md §2）: スティックのスクエア化と出力カーブ、トリガーの出力カーブ。
    /// 以前は <c>Mapping.SetCurveAndDeadzone</c> の中に、左右のスティックで、ほぼ同じコードが 2 回、L2／R2 で同じコードが 2 回あった。左右とも、同じメソッドを呼ぶ。状態は持たない。
    /// 呼び出し側は、「モードが有効で、入力が中立でない」ときだけ呼ぶ（呼び出さないフレームには、呼び出しの費用がかからない）。
    /// </summary>
    public interface IOutputCurveProcessor
    {
        /// <summary>
        /// スクエアスティック（円の入力を四角へ広げる）。<paramref name="roundness"/> は丸み（<c>SquareStickInfo.lsRoundness</c>／<c>rsRoundness</c>）。
        /// </summary>
        void ProcessSquareStick(double roundness, ref byte x, ref byte y);

        /// <summary>
        /// スティックの出力カーブ（モード 1〜6）。<paramref name="deadzoneType"/> は、デッドゾーンの種類（カーブの上限の求め方が変わる）。
        /// <paramref name="bezier"/> は、モード 6 のときだけ使う（<c>arrayBezierLUT</c> を引く）。
        /// </summary>
        void ProcessStickCurve(int mode, StickDeadZoneInfo.DeadZoneType deadzoneType, BezierCurve bezier, ref byte x, ref byte y);

        /// <summary>トリガーの出力カーブ（モード 1〜6）。<paramref name="bezier"/> は、モード 6 のときだけ使う。</summary>
        void ProcessTriggerCurve(int mode, BezierCurve bezier, ref byte value);
    }
}
