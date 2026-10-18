using System;
using System.Collections.Generic;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step4 W4〜Step8): W4 で Mapping に注入する（旧ループのフック Owns／ProcessOwned、抑制の集合の所有、Stage3 の後の ApplyFrameOutputs）。
    // OnProfileApplied は Step5（ProfileApplicationService の完了処理へ接続）、OnDeviceDisconnected は Step6（ControlService の切断処理へ接続）で本番に接続する。
    // それまでは単体テストからのみ呼ばれる（規約 2.4）。ProcessTriggers は最終形の入口で、Step8 で旧ループのフックと入れ替える。
    // 復帰（UntriggerPolicy）の処理は、Step7.5（Profile）で足す。それまでは UntriggerPolicy.None の束縛だけを扱う。
    /// <summary>
    /// Phase8-Step4 W3: 統一トリガーディスパッチのエンジン（Phase8-Step2-Design.md §9、§14〜§17、§23.1）。
    /// 種別ごとの分岐は持たない。差は <see cref="BindingPolicy"/> と出力アクションで表す。状態（<c>BeingTriggered</c> ほか）は <c>ActionInstanceState</c> に残し、
    /// エンジンが持つのは、デバイスごとの抑制の集合と、束縛ごとの実行時の配列（前フレームの成立、このフレームの評価結果）だけ。
    /// 処理するのは、<see cref="BindingMigrationOptions.MigratedTypes"/> に含まれる種別の束縛のみ（既定は空＝何も処理しない）。
    /// スレッド（Q8＝A）: <c>ProcessOwned</c>／<c>ProcessTriggers</c>／<c>ApplyFrameOutputs</c>／<c>AddSuppressed</c>／<c>ReleaseSuppressed</c> は、デバイスごとのロックの下で動く。
    /// <c>IsSuppressed</c> は、従来の <c>CheckForSpecialActionSuppression</c> と同じく、ロックなしで集合を読む。
    /// </summary>
    public sealed class TriggerDispatchEngine : ITriggerDispatchEngine
    {
        private readonly IActionRegistry _registry;
        private readonly IManagedActionManager _manager;
        private readonly BindingMigrationOptions _options;
        private readonly IVirtualKBM _virtualKbm;
        private readonly DeviceRuntime[] _devices = new DeviceRuntime[ControllerLimits.MaxControllerCount];

        public TriggerDispatchEngine(IActionRegistry registry, IManagedActionManager manager, BindingMigrationOptions options, IVirtualKBM virtualKbm)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _virtualKbm = virtualKbm; // null でもよい（出力アクションが OutputHandler を使えないだけ）

            for (int device = 0; device < _devices.Length; device++)
                _devices[device] = new DeviceRuntime(device, virtualKbm);
        }

        // ---- 旧ループのフック ----

        public bool Owns(int device, SpecialAction action)
        {
            if (action == null || (uint)device >= (uint)_devices.Length) return false;
            if (!_options.IsMigrated(action.typeID)) return false;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, null);
                return rt.Index.TryGetValue(action, out int i) && IsOwnedBinding(rt.Bindings[i]);
            }
        }

        public FrameResult ProcessOwned(int device, SpecialAction action, ITriggerInput input, IFrameOutput frame)
        {
            if (action == null || input == null || (uint)device >= (uint)_devices.Length) return FrameResult.Continue;
            if (!_options.IsMigrated(action.typeID)) return FrameResult.Continue;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, input);
                if (!rt.Index.TryGetValue(action, out int i)) return FrameResult.Continue;

                IActionBinding binding = rt.Bindings[i];
                if (!IsOwnedBinding(binding)) return FrameResult.Continue;

                return ProcessBinding(device, rt, i, binding, input, frame);
            }
        }

        // ---- 最終形の入口 ----

        public void ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame)
        {
            if (input == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, input);
                ReleaseSuppressedCore(rt, input);

                IReadOnlyList<IActionBinding> bindings = rt.Bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    IActionBinding binding = bindings[i];
                    if (!IsOwnedBinding(binding)) continue;

                    if (ProcessBinding(device, rt, i, binding, input, frame) == FrameResult.EndFrame)
                        break;
                }
            }
        }

        public void ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, input);

                IReadOnlyList<IActionBinding> bindings = rt.Bindings;
                EngineContext ctx = rt.Context;
                ctx.Begin(frame);

                for (int i = 0; i < bindings.Count; i++)
                {
                    IActionBinding binding = bindings[i];
                    if (!IsOwnedBinding(binding)) continue;

                    // 同じフレームの評価結果を再利用する（遅延の基準時刻を二重に更新しない）。このフレームで評価されていなければ、未成立
                    bool established = rt.Stamp[i] == rt.Frame && rt.Established[i];
                    bool rising = established && !rt.Prev[i];

                    IReadOnlyList<IOutputAction> outputs = binding.Outputs;
                    for (int o = 0; o < outputs.Count; o++)
                    {
                        if (outputs[o] is IFrameOutputAction frameOutput)
                        {
                            try { frameOutput.ApplyFrame(ctx, established, rising); }
                            catch (Exception ex) { LogException(ex); }
                        }
                    }

                    rt.Prev[i] = established;
                }

                // フレームの終わり: 評価結果の有効期間と、排他グループの「先に成立した」記録を、次のフレームへ持ち越さない
                rt.Frame++;
                rt.GroupFired.Clear();
            }
        }

        // ---- 抑制の集合 ----

        public bool IsSuppressed(int device, DS4Controls control)
        {
            if ((uint)device >= (uint)_devices.Length) return false;
            return _devices[device].Suppressed.Contains(control);
        }

        public void AddSuppressed(int device, IReadOnlyList<DS4Controls> controls)
        {
            if (controls == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                for (int i = 0; i < controls.Count; i++)
                    rt.Suppressed.Add(controls[i]);
            }
        }

        public void ReleaseSuppressed(int device, ITriggerInput input)
        {
            if (input == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                ReleaseSuppressedCore(rt, input);
            }
        }

        // 現行の MapCustomAction の冒頭の解除パスと同じ: 実際に離されたボタンだけを、1 つずつ外す
        private static void ReleaseSuppressedCore(DeviceRuntime rt, ITriggerInput input)
        {
            if (rt.Suppressed.Count == 0) return;

            try
            {
                rt.ReleaseInput = input;
                rt.Suppressed.RemoveWhere(rt.ReleasePredicate);
            }
            catch { }
            finally
            {
                rt.ReleaseInput = null;
            }
        }

        // ---- 後始末 ----

        public void OnProfileApplied(int device)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            // 適用スレッドは、入力スレッドのものである状態を直接書かない。次の処理の先頭でリセットする（Phase8-Step2-Design.md §17）
            _devices[device].ResetRequested = true;
        }

        public void OnDeviceDisconnected(int device)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                _registry.Clear(device);
                ResetState(rt, null);
                rt.ResetRequested = false;
                rt.Bindings = null; // 次の処理で、空の一覧から作り直す
            }
        }

        // ---- 内部 ----

        private bool IsOwnedBinding(IActionBinding binding)
        {
            SpecialAction source = binding?.Source;
            return source != null && _options.IsMigrated(source.typeID);
        }

        // 登録簿の一覧が差し替わっていたら実行時の配列を作り直し、リセットの要求があれば状態を初期化する
        private void Prepare(DeviceRuntime rt, int device, ITriggerInput input)
        {
            IReadOnlyList<IActionBinding> snapshot = _registry.GetBindings(device);
            if (!ReferenceEquals(snapshot, rt.Bindings))
                Rebuild(rt, snapshot);

            if (rt.ResetRequested)
            {
                rt.ResetRequested = false;
                ResetState(rt, input);
            }
        }

        private static void Rebuild(DeviceRuntime rt, IReadOnlyList<IActionBinding> snapshot)
        {
            int count = snapshot.Count;
            rt.Bindings = snapshot;
            rt.Prev = new bool[count];
            rt.Established = new bool[count];
            rt.Stamp = new int[count];
            rt.Index = new Dictionary<SpecialAction, int>(count);
            for (int i = 0; i < count; i++)
            {
                SpecialAction source = snapshot[i]?.Source;
                if (source != null && !rt.Index.ContainsKey(source))
                    rt.Index.Add(source, i);
            }

            rt.Frame++; // 古い評価結果を使わない
            rt.GroupFired.Clear();
        }

        private static void ResetState(DeviceRuntime rt, ITriggerInput input)
        {
            rt.Suppressed.Clear();
            rt.GroupFired.Clear();
            if (rt.Prev != null)
            {
                Array.Clear(rt.Prev, 0, rt.Prev.Length);
                Array.Clear(rt.Established, 0, rt.Established.Length);
            }

            rt.Frame++;

            IReadOnlyList<IActionBinding> bindings = rt.Bindings;
            if (bindings != null)
            {
                for (int i = 0; i < bindings.Count; i++)
                    bindings[i]?.Input?.Reset();
            }

            input?.Delay?.Reset();
        }

        // Phase8-Step2-Design.md §15.2 の 1 束縛の処理
        private FrameResult ProcessBinding(int device, DeviceRuntime rt, int i, IActionBinding binding, ITriggerInput input, IFrameOutput frame)
        {
            try
            {
                SpecialAction source = binding.Source;
                ActionInstanceState state = _manager.GetStateFor(source, device);
                if (state == null) return FrameResult.Continue;

                IInputAction inputAction = binding.Input;
                BindingPolicy policy = binding.Policy;

                TriggerState result = inputAction.Evaluate(input);
                bool established = result.IsEstablished;
                rt.Established[i] = established;
                rt.Stamp[i] = rt.Frame;

                UpdateFreshPressTimer(state, established, input.TickCount64);

                if (established)
                {
                    IReadOnlyList<DS4Controls> triggers = inputAction.Triggers;

                    if (policy.SuppressesTriggerButtons)
                    {
                        for (int t = 0; t < triggers.Count; t++)
                            rt.Suppressed.Add(triggers[t]);
                    }

                    if (policy.ResetsTriggerOutput && frame != null)
                    {
                        for (int t = 0; t < triggers.Count; t++)
                            frame.ResetTriggerControl(triggers[t]);
                    }

                    // 排他グループ（シフト）: 同じグループで先に成立した束縛だけが、以降の処理に進む
                    string group = policy.ExclusionGroup;
                    if (group != null && !rt.GroupFired.Add(group))
                        return FrameResult.Continue;

                    if (policy.RespectsFreshPressGuard && state.RequiresFreshPressAfterReset)
                        return FrameResult.Continue;

                    if (policy.BlocksWhileExecuting && state.IsExecuting)
                        return FrameResult.Continue;

                    EngineContext ctx = rt.Context;
                    IReadOnlyList<IOutputAction> outputs = binding.Outputs;

                    if (!state.BeingTriggered)
                    {
                        // 立ち上がりのエッジ
                        ctx.Begin(frame);
                        if (policy.BlocksWhileExecuting) state.IsExecuting = true;
                        try
                        {
                            lock (state) { state.SetBeingTriggeredInternal(true); }
                            for (int o = 0; o < outputs.Count; o++)
                                outputs[o].Execute(ctx);
                        }
                        finally
                        {
                            if (policy.BlocksWhileExecuting) state.IsExecuting = false;
                        }

                        if (ctx.EndFrameRequested) return FrameResult.EndFrame;
                    }
                    else
                    {
                        // 押している間
                        ctx.Begin(frame);
                        for (int o = 0; o < outputs.Count; o++)
                        {
                            if (outputs[o] is IHeldOutputAction held)
                                held.WhileHeld(ctx);
                        }

                        if (ctx.EndFrameRequested) return FrameResult.EndFrame;
                    }
                }
                else if (state.BeingTriggered)
                {
                    // 離したエッジ
                    lock (state) { state.SetBeingTriggeredInternal(false); }
                    if (policy.StopOnRelease)
                    {
                        EngineContext ctx = rt.Context;
                        ctx.Begin(frame);
                        IReadOnlyList<IOutputAction> outputs = binding.Outputs;
                        for (int o = 0; o < outputs.Count; o++)
                            outputs[o].Stop(ctx);

                        if (ctx.EndFrameRequested) return FrameResult.EndFrame;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return FrameResult.Continue;
        }

        // 現行の MapCustomAction と同じ: 未成立が FreshPressReleaseHoldMs 連続して初めて、Fresh-press ガードを解除する
        private static void UpdateFreshPressTimer(ActionInstanceState state, bool established, long tickCount64)
        {
            if (!established)
            {
                if (state.UntriggeredTimestampTicks == 0)
                    state.UntriggeredTimestampTicks = tickCount64;
                else if (tickCount64 - state.UntriggeredTimestampTicks >= ActionInstanceState.FreshPressReleaseHoldMs)
                    state.RequiresFreshPressAfterReset = false;
            }
            else
            {
                state.UntriggeredTimestampTicks = 0;
            }
        }

        private static void LogException(Exception ex)
        {
            try
            {
                AppLogger.LogToGui($"MapCustomAction exception: {ex}", true);
            }
            catch { }
        }

        // デバイスごとの実行時の状態
        private sealed class DeviceRuntime
        {
            public readonly object Lock = new object();
            public readonly HashSet<DS4Controls> Suppressed = new HashSet<DS4Controls>();
            public readonly HashSet<string> GroupFired = new HashSet<string>();
            public readonly EngineContext Context;
            public readonly Predicate<DS4Controls> ReleasePredicate;

            public IReadOnlyList<IActionBinding> Bindings;
            public bool[] Prev;
            public bool[] Established;
            public int[] Stamp;
            public Dictionary<SpecialAction, int> Index;
            public int Frame = 1;
            public ITriggerInput ReleaseInput;
            public volatile bool ResetRequested;

            public DeviceRuntime(int device, IVirtualKBM virtualKbm)
            {
                Context = new EngineContext(device, virtualKbm);
                ReleasePredicate = control => !ReleaseInput.IsActive(control);
            }
        }

        // 出力アクションに渡す文脈（デバイスごとに再利用。割り当て 0）
        private sealed class EngineContext : IOutputContext
        {
            public EngineContext(int device, IVirtualKBM handler)
            {
                Device = device;
                OutputHandler = handler;
            }

            public int Device { get; }
            public IVirtualKBM OutputHandler { get; }
            public IFrameOutput Frame { get; private set; }
            public bool EndFrameRequested { get; private set; }

            public void Begin(IFrameOutput frame)
            {
                Frame = frame;
                EndFrameRequested = false;
            }

            public void RequestEndFrame()
            {
                EndFrameRequested = true;
            }
        }
    }
}
