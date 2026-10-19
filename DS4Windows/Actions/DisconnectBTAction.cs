using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6b: DisconnectBT（コントローラーの無線接続の切断）の出力。
    /// 旧 <c>MapCustomAction</c> の DisconnectBT の分岐と比べた違い（Phase8-Step7-6-Plan.md Q3＝B。承認済み）:
    ///  - 旧経路は、押している間、切断が完了するまで毎フレーム <c>DisconnectBT()</c> とトレースログを繰り返していた。新経路は、切断の要求が**成功した**ら繰り返さない
    ///    （成功のあとも、切断が完了するまでの間、フレームの打ち切りだけは毎フレーム続ける＝旧経路の <c>return</c> と同じ）。**失敗した**ら、押している間、次のフレームで再試行する。
    /// 同じ点: 同期済み・非充電の Bluetooth だけが対象。トレースログ（立ち上がりの 4 行）→ 切断 → トリガーのボタンのキーを離す → フレームの打ち切り（Intent-Decisions 項目 5）の順。
    /// ドングル（SONYWA）: 押している間は「切断待ち」にし、離した後、無操作（アイドル）になったら <c>DisconnectDongle()</c> とキーの解放を行う（旧経路の <c>pressRelease</c> の流用を、このインスタンスのフラグに置き換えた）。
    /// 実機のコントローラーの操作は <see cref="IControllerDisconnectPort"/> 越し。
    /// </summary>
    public sealed class DisconnectBTAction : IHeldOutputAction, IFrameOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IControllerDisconnectPort _port;
        private readonly TriggerButtonKeyReleaser _keyReleaser;
        private bool _disconnectRequested;      // 切断の要求が成功した（離すまで、繰り返さない）
        private bool _dongleReleasePending;     // ドングル: 離した後の切断待ち

        public DisconnectBTAction(SpecialAction sa, int deviceIndex, IControllerDisconnectPort port, IProfileSettingsService settings)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _keyReleaser = new TriggerButtonKeyReleaser(settings ?? throw new ArgumentNullException(nameof(settings)));
        }

        public string Id => _sa.name ?? "DisconnectBT";

        /// <summary>立ち上がり。</summary>
        public void Execute(IOutputContext ctx) => Attempt(ctx);

        /// <summary>押している間。要求が成功済みなら、フレームの打ち切りだけを続ける（切断が完了するまで）。失敗していたら再試行する。</summary>
        public void WhileHeld(IOutputContext ctx)
        {
            if (_disconnectRequested)
            {
                ctx?.RequestEndFrame();
                return;
            }

            Attempt(ctx);
        }

        /// <summary>離した。次に押したときに、もう一度切断できるようにする。</summary>
        public void Stop(IOutputContext ctx)
        {
            _disconnectRequested = false;
        }

        /// <summary>毎フレーム（成立の有無によらず）。ドングルの、離した後の切断待ちを担う。</summary>
        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (active || !_dongleReleasePending) return;

            try
            {
                int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);
                if (!_port.IsDongleSynced(dev) || !_port.IsIdle(dev)) return;

                _port.DisconnectDongle(dev);
                _keyReleaser.Release(_sa, dev, ctx?.OutputHandler);
                _dongleReleasePending = false;
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"DisconnectBTAction.ApplyFrame failed: {ex}"); } catch { }
            }
        }

        private void Attempt(IOutputContext ctx)
        {
            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);

            try
            {
                DisconnectTargetKind kind = _port.GetTargetKind(dev);
                if (kind == DisconnectTargetKind.Bluetooth)
                {
                    LogAttempt(ctx, dev);
                    if (_port.DisconnectBluetooth(dev))
                    {
                        _disconnectRequested = true;
                        _keyReleaser.Release(_sa, dev, ctx?.OutputHandler);
                        ctx?.RequestEndFrame();   // 旧ループの return と同じ: 切断のあとは、このフレームの残りを処理しない
                    }
                }
                else if (kind == DisconnectTargetKind.Dongle)
                {
                    _dongleReleasePending = true;
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"DisconnectBTAction.Execute failed: {ex}"); } catch { }
            }
        }

        // 旧 MapCustomAction の DisconnectBT の分岐と同じ: LogSpecialActionTrace（Trace の 4 行）。Debug のログはない
        private void LogAttempt(IOutputContext ctx, int dev)
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
