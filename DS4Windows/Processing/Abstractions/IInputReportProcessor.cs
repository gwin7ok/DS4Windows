using DS4Windows.Core.Model;
using System;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step5b: 入力レポートの処理の窓口。旧 <c>ControlService.On_Report</c>（約 200 行）と、その作業用の状態
    /// （マップ後の状態、公開用の状態、処理遅延の計測、マクロ記録中のフラグ）の持ち主。実装は <c>InputReportProcessor</c>（DI の Singleton）。
    /// <c>DeviceConnector</c> が、デバイスの <c>Report</c> イベントの購読から <see cref="OnReport"/> を呼ぶ。
    /// </summary>
    public interface IInputReportProcessor
    {
        /// <summary>入力レポートが届いたときの処理（旧 <c>ControlService.On_Report</c>）。<paramref name="ind"/> が -1 のときは何もしない。</summary>
        void OnReport(InputDeviceCore device, EventArgs e, int ind);

        /// <summary>スロットごとの、マッピング後の状態（旧 <c>ControlService.MappedState</c>）。</summary>
        ControllerState GetMappedState(int ind);

        /// <summary>スロットごとの、マッピングに渡す公開用の状態（旧 <c>ControlService.ExposedState</c>）。</summary>
        ControllerStateExposed[] ExposedState { get; }

        /// <summary>マクロの記録中は、マッピングを飛ばす（旧 <c>ControlService.recordingMacro</c>。<c>RecordBox</c> が書く）。</summary>
        bool RecordingMacro { get; set; }

        /// <summary>ControllerReadings タブの表示中だけ true になる、処理遅延の計測の有効化（旧 <c>ControlService.IsMeasuringProcessingDelay</c>）。</summary>
        bool IsMeasuringProcessingDelay { get; set; }

        /// <summary>スロットごとの処理遅延（ms。旧 <c>ControlService.ProcessingDelayMs</c>）。</summary>
        double[] ProcessingDelayMs { get; }

        /// <summary>スロットの処理遅延（ms）を返す。範囲外は 0（旧 <c>ControlService.GetProcessingDelay</c>）。</summary>
        double GetProcessingDelay(int deviceIndex);
    }
}
