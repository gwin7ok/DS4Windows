using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Context passed to output actions containing device and handler references.
    /// </summary>
    public interface IOutputContext
    {
        int Device { get; }
        IVirtualKBM OutputHandler { get; }

        /// <summary>
        /// Phase8-Step4 W1: このフレームの出力（Button の毎フレームの出力と、トリガーのボタンの出力の消去に使う）。エンジンが渡す文脈でだけ有効で、
        /// それ以外（従来の呼び出し）は null。
        /// </summary>
        IFrameOutput Frame { get; }

        /// <summary>
        /// Phase8-Step4 W1: フレームの走査を打ち切る要求（Profile の切替・DisconnectBT の実行後。Phase8-Step2-Intent-Decisions.md 項目 5）。
        /// エンジンが渡す文脈でだけ効き、それ以外では何もしない。
        /// </summary>
        void RequestEndFrame();
    }
}
