#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// GyroCalibrate（ジャイロの較正のやり直し）の出力。立ち上がりのとき 1 回だけ実行する（保持中・離したときは何もしない）。
    /// 旧 <c>MapCustomAction</c> の GyroCalibrate の分岐と同じ順序: LogSpecialActionTrace（Trace の立ち上がり 4 行）→ 較正のやり直し → LogActionDoneCountOnTrigger（Debug の「SpecialAction GyroCalibrate: …」）。
    /// 実機のコントローラーの操作は <see cref="IGyroCalibrationPort"/> 越し。
    /// </summary>
    public sealed class GyroCalibrateAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IGyroCalibrationPort _port;
        private readonly int _actionIndex;
        private readonly ISpecialActionRepository _actionRepository;

        /// <param name="actionIndex">ログの「index」用。</param>
        /// <param name="actionRepository">ログの「ActionEntries」用。省略可（テスト）。</param>
        public GyroCalibrateAction(SpecialAction sa, int deviceIndex, IGyroCalibrationPort port, int actionIndex, ISpecialActionRepository actionRepository)
        {
            ArgumentNullException.ThrowIfNull(sa);
            _sa = sa;
            _deviceIndex = deviceIndex;
            ArgumentNullException.ThrowIfNull(port);
            _port = port;
            _actionIndex = actionIndex;
            _actionRepository = actionRepository;
        }

        public string Id => _sa.name ?? "GyroCalibrate";

        public void Execute(IOutputContext ctx)
        {
            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);

            try
            {
                if (AppLogger.IsTraceEnabled)
                {
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {dev}");
                    AppLogger.LogTrace($"SA '{_sa.name}' maps to Type:{_sa.type} Details:{_sa.details}");
                    // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");
                    var parts = new List<string>();
                    (ctx?.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
                }
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }

            try
            {
                _port.ResetContinuousCalibration(dev);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"GyroCalibrateAction.Execute failed: {ex}");
            }

            try
            {
                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"SpecialAction GyroCalibrate: device={dev}, name={_sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
