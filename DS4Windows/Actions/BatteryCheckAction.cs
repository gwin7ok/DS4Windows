using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6c: BatteryCheck（電池残量の通知と、ライトバーの色による表示）の出力。
    /// 旧 <c>MapCustomAction</c> の BatteryCheck の分岐と同じ流れ（<c>details</c> は「|」または「,」区切り。[1]＝通知、[2]＝ライトバー、[3..5]＝空の色、[6..8]＝満タンの色）:
    ///  - 立ち上がり（<see cref="Execute"/>）: 通知 → 現在のライトバーの色を保存して強制色を有効にする → 色の遷移の 1 ステップ → Trace 4 行 → Debug「SpecialAction BatteryCheck: …」。
    ///  - 押している間（<see cref="WhileHeld"/>）: 毎フレーム、色の遷移を 2 ずつ進める（100 まで）。
    ///  - 離したとき（<see cref="Stop"/>）: 遷移の進みを 0 に戻し、強制色を無効にする。
    /// 旧経路との違い（Phase8-Step7-6-Plan.md Q8＝B。承認済み）: 旧経路が立ち上がりで出していた、コピーの名残りのログ（<c>LogActionDoneCountOnTrigger("WheelRecalibrate")</c>）は出さない。
    /// BatteryCheck が実行されたことは、既存の Debug「SpecialAction BatteryCheck: …」で分かる。
    /// 遷移の進み（旧 <c>Mapping.fadetimer</c>）と保存した色（旧 <c>Mapping.lastColor</c>）は、このインスタンスが持つ。
    /// 実機・静的な窓口の操作は <see cref="IBatteryCheckPort"/> 越し。
    /// </summary>
    public sealed class BatteryCheckAction : IHeldOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IBatteryCheckPort _port;
        private readonly int _actionIndex;
        private readonly ISpecialActionRepository _actionRepository;

        private bool _parsed;
        private bool _valid;
        private bool _notify;
        private bool _lightBar;
        private DS4Color _empty;
        private DS4Color _full;
        private int _fadeTimer;
        private DS4Color _lastColor;

        public BatteryCheckAction(SpecialAction sa, int deviceIndex, IBatteryCheckPort port, int actionIndex, ISpecialActionRepository actionRepository)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _actionIndex = actionIndex;
            _actionRepository = actionRepository;
        }

        public string Id => _sa.name ?? "BatteryCheck";

        public void Execute(IOutputContext ctx)
        {
            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);

            try
            {
                EnsureParsed();
                if (_valid)
                {
                    if (_notify) _port.NotifyBattery(dev);

                    if (_lightBar && _port.HasController(dev))
                    {
                        _lastColor = _port.GetLightBarColor(dev);
                        _port.SetForceLight(dev, true);
                    }

                    Step(dev);
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"BatteryCheckAction.Execute failed: {ex}"); } catch { }
            }

            LogRising(ctx, dev);
        }

        public void WhileHeld(IOutputContext ctx)
        {
            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);

            try
            {
                Step(dev);
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"BatteryCheckAction.WhileHeld failed: {ex}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);

            _fadeTimer = 0;
            try { _port.SetForceLight(dev, false); } catch { }
        }

        // 旧経路と同じ: ライトバーを使うとき、空の色と満タンの色の間を電池残量で決め、保存した色から、その色へ、2 ずつ（100 まで）遷移させる
        private void Step(int dev)
        {
            if (!_valid || !_lightBar || !_port.HasController(dev)) return;

            DS4Color trans = ColorUtil.getTransitionedColor(ref _empty, ref _full, _port.GetBattery(dev));
            if (_fadeTimer < 100)
            {
                _fadeTimer += 2;
                _port.SetForcedColor(dev, ColorUtil.getTransitionedColor(ref _lastColor, ref trans, _fadeTimer));
            }
        }

        // details は変わらないため、最初に 1 度だけ読む（旧経路は毎フレーム読み直していた）。不正な値なら、何もしない（旧経路は例外で打ち切られていた）
        private void EnsureParsed()
        {
            if (_parsed) return;
            _parsed = true;

            string[] dets = _sa.details.Split('|');
            if (dets.Length == 1)
                dets = _sa.details.Split(',');

            _notify = bool.Parse(dets[1]);
            _lightBar = bool.Parse(dets[2]);
            if (_lightBar)
            {
                _empty = new DS4Color(byte.Parse(dets[3]), byte.Parse(dets[4]), byte.Parse(dets[5]));
                _full = new DS4Color(byte.Parse(dets[6]), byte.Parse(dets[7]), byte.Parse(dets[8]));
            }

            _valid = true;
        }

        // 旧 MapCustomAction の BatteryCheck の分岐と同じ: LogSpecialActionTrace（Trace の 4 行）→ LogActionDoneCountOnTrigger("BatteryCheck")（Debug）
        private void LogRising(IOutputContext ctx, int dev)
        {
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

                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { }
                AppLogger.LogDebug($"SpecialAction BatteryCheck: device={dev}, name={_sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { }
        }
    }
}
