using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6a: GyroCalibrate（ジャイロの較正のやり直し）の出力。立ち上がりのとき 1 回だけ実行する（保持中・離したときは何もしない）。
    /// 旧 <c>MapCustomAction</c> の GyroCalibrate の分岐と同じ順序: LogSpecialActionTrace（Trace の立ち上がり 4 行）→ 較正のやり直し → LogActionDoneCountOnTrigger（Debug の「SpecialAction GyroCalibrate: …」）。
    /// 実機のコントローラーの操作は <see cref="IGyroCalibrationPort"/> 越し（Phase8-Step7-6-Plan.md Q2＝A）。
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
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _port = port ?? throw new ArgumentNullException(nameof(port));
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
                    AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {dev}");
                    AppLogger.LogTrace($"SA '{_sa.name}' maps to Type:{_sa.type} Details:{_sa.details}");
                    // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                    AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");
                    var parts = new List<string>();
                    (ctx?.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
                }
            }
            catch { }

            try
            {
                _port.ResetContinuousCalibration(dev);
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"GyroCalibrateAction.Execute failed: {ex}"); } catch { }
            }

            try
            {
                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { }
                AppLogger.LogDebug($"SpecialAction GyroCalibrate: device={dev}, name={_sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
