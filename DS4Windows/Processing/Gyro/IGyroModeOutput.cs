#nullable enable

using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// ジャイロの出力の種類（Controls／Mouse）ごとの出力。
    /// <see cref="GyroOutputProcessor"/> が、プロファイルの出力モードに合う 1 つを選び、トリガーの判定の結果を渡して呼ぶ。
    /// </summary>
    internal interface IGyroModeOutput
    {
        /// <summary>この出力を、いま実行してよいか（Mouse は感度が 0 のとき実行しない。実行しないときは、トリガーの判定も行わない）。</summary>
        bool IsEnabled { get; }

        /// <summary>1 回の動きのイベントを処理する。<paramref name="shouldOutput"/> は、トリガーの判定が「出力してよい」としたか。</summary>
        void Process(SixAxisEventArgs arg, ControllerState state, bool shouldOutput);
    }
}
