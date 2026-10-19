using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6d: SASteeringWheelEmulationCalibrate（ジャイロのステアリング操作の較正の開始と確定）の出力。
    /// 旧 <c>MapCustomAction</c> の分岐と同じ（押している間、**毎フレーム**判定する）:
    ///  - 較正の状態が 0（待機）で、前回の操作から 3 秒以上たっていれば、状態を 1（較正の開始の要求）にする。
    ///  - 状態が 2（較正の中）で、前回の操作から 3 秒以上たっていれば、状態を 3（較正の確定の要求）にする。
    ///  - 状態を変えるとき、Trace の 4 行（旧 <c>LogSpecialActionTrace</c>）を出し、<see cref="SpecialAction.firstTap"/> に今の時刻を入れる（3 秒の間隔の基準）。Debug のログはない。
    /// 立ち上がり（<see cref="Execute"/>）と押している間（<see cref="WhileHeld"/>）で同じ判定を行う（旧経路は成立の最初のフレームからこの判定をしていた）。離したときは何もしない。
    /// 状態（<c>DS4Device.WheelRecalibrateActiveState</c>）の読み書きは <see cref="IWheelCalibrationPort"/> 越し。時刻は、入力（<c>ITriggerInput.UtcNow</c>。本番は <c>DateTime.UtcNow</c>）から取る。
    /// </summary>
    public sealed class SteeringWheelCalibrateAction : IHeldOutputAction
    {
        private static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(3000);

        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IWheelCalibrationPort _port;

        public SteeringWheelCalibrateAction(SpecialAction sa, int deviceIndex, IWheelCalibrationPort port)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public string Id => _sa.name ?? "SteeringWheelCalibrate";

        public void Execute(IOutputContext ctx) => Step(ctx);

        public void WhileHeld(IOutputContext ctx) => Step(ctx);

        public void Stop(IOutputContext ctx)
        {
        }

        private void Step(IOutputContext ctx)
        {
            try
            {
                int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);
                int state = _port.GetCalibrationState(dev);
                if (state < 0) return;   // コントローラーがいない（旧経路は例外で、そのフレームの処理が打ち切られていた）

                DateTime now = (ctx as IOutputContextInput)?.Input?.UtcNow ?? DateTime.UtcNow;
                if (state == 0 && now > _sa.firstTap + MinimumInterval)
                {
                    LogTransition(ctx, dev);
                    _sa.firstTap = now;
                    _port.SetCalibrationState(dev, 1);   // 較正の開始
                }
                else if (state == 2 && now > _sa.firstTap + MinimumInterval)
                {
                    LogTransition(ctx, dev);
                    _sa.firstTap = now;
                    _port.SetCalibrationState(dev, 3);   // 較正の確定
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"SteeringWheelCalibrateAction failed: {ex}"); } catch { }
            }
        }

        // 旧 MapCustomAction の分岐と同じ: 状態を変える場所でだけ、LogSpecialActionTrace（Trace の 4 行）を出す
        private void LogTransition(IOutputContext ctx, int dev)
        {
            try
            {
                if (!AppLogger.IsTraceEnabled) return;

                AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {dev}");
                AppLogger.LogTrace($"SA '{_sa.name}' maps to Type:{_sa.type} Details:{_sa.details}");
                // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");
                var parts = new List<string>();
                (ctx?.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
            }
            catch { }
        }
    }
}
