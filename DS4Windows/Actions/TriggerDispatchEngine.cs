using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step4 W4〜Step8): W4 で Mapping に注入する（旧ループのフック Owns／ProcessOwned、抑制の集合の所有、Stage3 の後の ApplyFrameOutputs）。
    // OnProfileApplied は Step5（ProfileApplicationService の完了処理へ接続）、OnDeviceDisconnected は Step6（ControlService の切断処理へ接続）で本番に接続する。
    // それまでは単体テストからのみ呼ばれる（規約 2.4）。ProcessTriggers は最終形の入口で、Step8 で旧ループのフックと入れ替える。
    // 復帰（UntriggerPolicy）: Automatic（Unload on trigger release）のみ。Buttons（解除トリガー）は Phase8-Step8 8b で機能を廃止し、8d で型から削除した。
    /// <summary>
    /// Phase8-Step4 W3: 統一トリガーディスパッチのエンジン（Phase8-Step2-Design.md §9、§14〜§17、§23.1）。
    /// 種別ごとの分岐は持たない。差は <see cref="BindingPolicy"/> と出力アクションで表す。状態（<c>BeingTriggered</c> ほか）は <c>ActionInstanceState</c> に残し、
    /// エンジンが持つのは、デバイスごとの抑制の集合と、束縛ごとの実行時の配列（前フレームの成立、このフレームの評価結果）だけ。
    /// 束縛が登録されていれば、常に処理する（Phase8-Step8 8d: 種別ごとの移行のスイッチ <c>BindingMigrationOptions</c> は削除した）。
    /// スレッド（Q8＝A）: <c>ProcessOwned</c>／<c>ProcessTriggers</c>／<c>ApplyFrameOutputs</c>／<c>AddSuppressed</c>／<c>ReleaseSuppressed</c> は、デバイスごとのロックの下で動く。
    /// <c>IsSuppressed</c> は、従来の <c>CheckForSpecialActionSuppression</c> と同じく、ロックなしで集合を読む。
    /// </summary>
    public sealed class TriggerDispatchEngine : ITriggerDispatchEngine
    {
        private readonly IActionRegistry _registry;
        private readonly IManagedActionManager _manager;
        private readonly IVirtualKBM _virtualKbm;
        private readonly DeviceRuntime[] _devices = new DeviceRuntime[ControllerLimits.MaxControllerCount];
        private static readonly int ControlTableSize = (int)Enum.GetValues(typeof(DS4Controls)).Cast<DS4Controls>().Max() + 1;

        public TriggerDispatchEngine(IActionRegistry registry, IManagedActionManager manager, IVirtualKBM virtualKbm)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _virtualKbm = virtualKbm; // null でもよい（出力アクションが OutputHandler を使えないだけ）

            for (int device = 0; device < _devices.Length; device++)
                _devices[device] = new DeviceRuntime(device, virtualKbm);
        }

        // ---- 旧ループのフック ----

        public bool Owns(int device, SpecialAction action)
        {
            if (action == null || (uint)device >= (uint)_devices.Length) return false;
            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, null);
                return rt.Index.TryGetValue(action, out int i) && IsOwnedBinding(rt.Bindings[i]);
            }
        }

        // 7.8b: そのボタンの束縛が登録されていれば真。毎フレーム・全ボタンで呼ばれるため、束縛が 1 つもないとき（既定）は、ロックも割り当てもなしで偽を返す
        public bool OwnsControl(int device, DS4Controls control)
        {
            if ((uint)device >= (uint)_devices.Length) return false;

            DeviceRuntime rt = _devices[device];
            if (ReferenceEquals(_registry.GetBindings(device), rt.Bindings) && rt.ControlCount == 0 && !rt.ResetRequested)
                return false;

            lock (rt.Lock)
            {
                Prepare(rt, device, null);
                int c = (int)control;
                if (rt.ControlCount == 0 || (uint)c >= (uint)rt.ControlIndex.Length) return false;

                int i = rt.ControlIndex[c];
                return i >= 0 && IsOwnedBinding(rt.Bindings[i]);
            }
        }

        public void ProcessControl(int device, DS4Controls control, ITriggerInput input, IFrameOutput frame)
        {
            if (input == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, input);
                int c = (int)control;
                if (rt.ControlCount == 0 || (uint)c >= (uint)rt.ControlIndex.Length) return;

                // Phase8-Step7 7.8e: 1 つのボタンに複数の束縛（Extras → 通常の割り当て）を持てる。ビルダーが並べた順に、鎖をたどって処理する
                for (int i = rt.ControlIndex[c]; i >= 0; i = rt.ControlNext[i])
                {
                    IActionBinding binding = rt.Bindings[i];
                    if (!IsOwnedBinding(binding)) continue;

                    ProcessBinding(device, rt, i, binding, input, frame);
                }
            }
        }

        public FrameResult ProcessOwned(int device, SpecialAction action, ITriggerInput input, IFrameOutput frame)
        {
            if (action == null || input == null || (uint)device >= (uint)_devices.Length) return FrameResult.Continue;
            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                Prepare(rt, device, input);
                if (!rt.Index.TryGetValue(action, out int i)) return FrameResult.Continue;

                IActionBinding binding = rt.Bindings[i];
                if (!IsOwnedBinding(binding)) return FrameResult.Continue;

                // Phase10-Step6 I5 の修正: SA に付随する束縛（SA の Extras）は、SA 本体の直前に処理する（本体が途中で終了を要求しても、必ず処理する）
                if (rt.CompanionHead != null)
                {
                    for (int j = rt.CompanionHead[i]; j >= 0; j = rt.CompanionNext[j])
                    {
                        IActionBinding companion = rt.Bindings[j];
                        if (IsOwnedBinding(companion)) ProcessBinding(device, rt, j, companion, input, frame);
                    }
                }

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
                ctx.Begin(frame, input);

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
                        else if (rt.Stamp[i] == rt.Frame && outputs[o] is IEvaluatedFrameOutputAction evaluatedOutput)
                        {
                            // Phase8-Step7 7.8f-1: このフレームで評価された束縛だけ（旧経路が dcs を処理したフレームだけ、アナログの値を出す）
                            try { evaluatedOutput.ApplyEvaluatedFrame(ctx, established); }
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

        // ---- 復帰（Phase8-Step7 7.5b）----

        public void ProcessUntrigger(int device, ITriggerInput input)
        {
            if (input == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            if (rt.Pending == null) return;   // 合図なし: ロックも割り当てもなし

            IActionBinding binding = null;
            lock (rt.Lock)
            {
                binding = rt.Pending;
                if (binding == null) return;

                try
                {
                    SpecialAction action = binding.Source;
                    AppLogger.LogDebug($"SpecialAction UNTRIGGER check: device={device}, untriggeraction={(action != null ? action.name : "null")}, untriggerindex={binding.Index}");

                    // 自動（Unload on trigger release）: トリガーのうち、どれか 1 つでも離れたら成立（旧 Mapping と同じ）
                    bool activated = false;
                    var triggers = action.trigger;
                    for (int i = 0, count = triggers.Count; i < count; i++)
                    {
                        if (!input.IsDigitalActive(triggers[i]))   // R-5: SA の割り当ての判定（閾値なし）に合わせる
                        {
                            activated = true;
                            break;
                        }
                    }

                    if (!activated)
                    {
                        // 復帰待ちの間: 切替アクションの BeingTriggered を下げ続ける（Intent 項目 6）
                        ActionInstanceState state = _manager.GetStateFor(action, device);
                        if (state != null)
                        {
                            lock (state) { state.SetBeingTriggeredInternal(false); }
                        }

                        return;
                    }

                    // 復帰の適用も ClearDeviceState を通る。復帰の前に合図を消す（旧経路と同じ順序。Phase7b W1）
                    rt.Pending = null;
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    return;
                }
            }

            // 復帰の適用はロックの外で行う（旧経路と同じ。適用が、同じスレッドでエンジンの他のメソッドを呼んでも、待たない）
            try
            {
                EngineContext ctx = rt.Context;
                ctx.Begin(null, input);
                IReadOnlyList<IOutputAction> outputs = binding.Outputs;
                for (int o = 0; o < outputs.Count; o++)
                    outputs[o].Stop(ctx);
            }
            catch (Exception ex)
            {
                LogException(ex);
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
                ResetOutputs(rt, rt.Bindings, null);   // 7.8e（Q6＝B）: 押したままの切断で、効果が残らないように、出力を元に戻す
                ResetState(rt, null);
                rt.Pending = null;   // 物理切断では、復帰の合図も消す（適用では消さない。ResetState は触らない）
                rt.ResetRequested = false;
                rt.Bindings = null; // 次の処理で、空の一覧から作り直す
            }
        }

        // ---- 内部 ----

        private bool IsOwnedBinding(IActionBinding binding)
        {
            return binding?.Source != null;
        }

        // 登録簿の一覧が差し替わっていたら実行時の配列を作り直し、リセットの要求があれば状態を初期化する
        private void Prepare(DeviceRuntime rt, int device, ITriggerInput input)
        {
            IReadOnlyList<IActionBinding> snapshot = _registry.GetBindings(device);
            if (!ReferenceEquals(snapshot, rt.Bindings))
            {
                ResetOutputs(rt, rt.Bindings, input);   // 7.8e（Q6＝B）: 差し替え前の出力が、押したまま有効にした効果を元に戻す
                Rebuild(rt, snapshot);
            }

            if (rt.ResetRequested)
            {
                rt.ResetRequested = false;
                ResetOutputs(rt, rt.Bindings, input);
                ResetState(rt, input);
            }
        }

        // Phase8-Step7 7.8e（Q6＝B）: 束縛の差し替え・適用・切断のときに、IResettableOutputAction を実装する出力へ後始末を知らせる。
        // 有効にしていない出力は何もしない。割り当てが起きるのは、差し替えの瞬間だけ（毎フレームではない）
        private static void ResetOutputs(DeviceRuntime rt, IReadOnlyList<IActionBinding> bindings, ITriggerInput input)
        {
            if (bindings == null) return;

            for (int i = 0; i < bindings.Count; i++)
            {
                IReadOnlyList<IOutputAction> outputs = bindings[i]?.Outputs;
                if (outputs == null) continue;

                for (int o = 0; o < outputs.Count; o++)
                {
                    if (outputs[o] is IResettableOutputAction resettable)
                    {
                        try
                        {
                            rt.Context.Begin(null, input);
                            resettable.OnReset(rt.Context);
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                    }
                }
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
            rt.ControlNext = new int[count];
            Array.Fill(rt.ControlNext, -1);
            rt.ControlCount = 0;
            rt.CompanionHead = null;
            rt.CompanionNext = null;
            for (int i = 0; i < count; i++)
            {
                SpecialAction source = snapshot[i]?.Source;
                if (source != null && !rt.Index.ContainsKey(source))
                    rt.Index.Add(source, i);

                // Controls タブの束縛（7.8b）: ボタン → 束縛の添字の表
                if (snapshot[i]?.Input is IControlBoundInput controlInput)   // Phase10-Step6 W8 相当: 型ではなく印で見分ける（Controls の Multi-action も載せる）
                {
                    if (rt.ControlIndex == null || rt.ControlCount == 0)
                    {
                        rt.ControlIndex = rt.ControlIndex ?? new int[ControlTableSize];
                        rt.ControlTail = rt.ControlTail ?? new int[ControlTableSize];
                        Array.Fill(rt.ControlIndex, -1);
                        Array.Fill(rt.ControlTail, -1);
                    }

                    int control = (int)controlInput.Triggers[0];
                    if ((uint)control < (uint)rt.ControlIndex.Length)
                    {
                        // 7.8e: 同じボタンの束縛は、一覧の順に鎖でつなぐ（先頭が ControlIndex、次が ControlNext）
                        if (rt.ControlIndex[control] < 0) rt.ControlIndex[control] = i;
                        else rt.ControlNext[rt.ControlTail[control]] = i;
                        rt.ControlTail[control] = i;
                        rt.ControlCount++;
                    }
                }
            }

            // Phase10-Step6 I5 の修正: SA に付随する束縛（SA の Extras）を、SA 本体の添字につなぐ（SA 本体の ProcessOwned が、一緒に処理する）
            for (int j = 0; j < count; j++)
            {
                SpecialAction primary = (snapshot[j] as TriggerBinding)?.PrimarySource;
                if (primary == null || !rt.Index.TryGetValue(primary, out int main) || main == j) continue;

                if (rt.CompanionHead == null)
                {
                    rt.CompanionHead = new int[count];
                    rt.CompanionNext = new int[count];
                    Array.Fill(rt.CompanionHead, -1);
                    Array.Fill(rt.CompanionNext, -1);
                }

                rt.CompanionNext[j] = rt.CompanionHead[main];
                rt.CompanionHead[main] = j;
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

                // Controls タブの束縛（7.8b。Q3＝A）: 成立の有無によらず、毎フレーム、割り当てたボタンの通常の出力を消す（旧 ResetToDefaultValue）
                if (policy.ErasesOutputEveryFrame && frame != null)
                {
                    IReadOnlyList<DS4Controls> erased = inputAction.Triggers;
                    for (int t = 0; t < erased.Count; t++)
                        frame.ResetTriggerControl(erased[t]);
                }

                // ジェスチャー（Phase8-Step7 7.7b。MultiAction のタップ・長押し・ダブルタップ）: 出力を「ジェスチャーで選ぶ」束縛では、
                // ガード・成立の判定・BeingTriggered によらず、Gesture が 0 でないフレームだけ、Outputs[Gesture - 1] を実行する
                // （旧経路の MultiAction は、ガードを見ず、BeingTriggered を使わない）。どのジェスチャーでどの出力かは、束縛を作る側（ビルダー）が決めている。
                if (policy.RoutesOutputsByGesture && result.Gesture != TriggerState.NoGesture)
                {
                    IReadOnlyList<IOutputAction> routed = binding.Outputs;
                    int routedIndex = result.Gesture - 1;
                    if ((uint)routedIndex < (uint)routed.Count)
                    {
                        EngineContext gestureContext = rt.Context;
                        gestureContext.Begin(frame, input);
                        try { routed[routedIndex].Execute(gestureContext); }
                        catch (Exception ex) { LogException(ex); }
                    }
                }

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

                    // 出力をジェスチャーで選ぶ束縛（MultiAction）は、通常の出力の実行をしない（抑制と出力の消去は、上で済んでいる）
                    if (policy.RoutesOutputsByGesture)
                        return FrameResult.Continue;

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
                        ctx.Begin(frame, input);

                        // 実行の条件（任意。Profile の連鎖の防止）。満たさないときは、状態を変えずに素通しする（旧経路の、実行の条件の if と同じ位置）
                        IReadOnlyList<IOutputAction> gated = binding.Outputs;
                        for (int o = 0; o < gated.Count; o++)
                        {
                            if (gated[o] is IGatedOutputAction gate && !gate.CanExecute(ctx))
                                return FrameResult.Continue;
                        }

                        if (policy.LogsRisingEdge) LogRisingEdge(device, source);

                        if (policy.BlocksWhileExecuting) state.IsExecuting = true;
                        try
                        {
                            // 復帰の合図（旧経路と同じ。出力を実行する前に設定する）。Buttons は、ビルダーが束縛を作らないため、ここでは扱わない
                            if (policy.Untrigger == UntriggerPolicy.Automatic) rt.Pending = binding;

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
                        ctx.Begin(frame, input);
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
                    if (policy.LogsReleaseEdge) LogReleaseEdge(device, source);
                    if (policy.StopOnRelease)
                    {
                        EngineContext ctx = rt.Context;
                        ctx.Begin(frame, input);
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

        // Phase10-Step4c W2（項目 4）: 立ち上がりのログ。出力アクションが自分で出さない種別（BindingPolicy.LogsRisingEdge）だけ。SA の出力アクションのログと同じ形
        // （出力の組み合わせの行は、フレームの出力が立ち上がりの時点では確定しない［毎フレームの出力は、あとで適用される］ため、出さない）
        private static void LogRisingEdge(int device, SpecialAction source)
        {
            try
            {
                if (AppLogger.IsTraceEnabled)
                {
                    AppLogger.LogTrace($"Trigger detected for SA '{source.name}' on device {device}");
                    AppLogger.LogTrace($"SA '{source.name}' maps to Type:{source.type} Details:{source.details}");
                    AppLogger.LogTrace($"Trigger keys for SA '{source.name}': <none>");
                }

                if (AppLogger.IsDebugEnabled)
                    AppLogger.LogDebug($"SpecialAction {source.type}: device={device}, name={source.name}");
            }
            catch { }
        }

        // Phase10-Step4c W2（項目 4）: 離したエッジのログ。全種別共通（BindingPolicy.LogsReleaseEdge）。Key の KeyReleased は、Key の出力アクションが別に出す
        private static void LogReleaseEdge(int device, SpecialAction source)
        {
            try
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"Trigger released for SA '{source.name}' on device {device}");

                if (AppLogger.IsDebugEnabled)
                    AppLogger.LogDebug($"SpecialAction {source.type} released: device={device}, name={source.name}");
            }
            catch { }
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
            public int[] ControlIndex;   // Controls タブの束縛（7.8b）: DS4Controls → Bindings の先頭の添字（なしは -1）
            public int[] ControlTail;    // 7.8e: DS4Controls → 鎖の最後の添字（Rebuild の間だけ使う）
            public int[] ControlNext;    // 7.8e: 束縛の添字 → 同じボタンの次の束縛の添字（なしは -1）
            public volatile int ControlCount;
            public int[] CompanionHead;  // Phase10-Step6 I5 の修正: SA 本体の添字 → 付随する束縛（SA の Extras）の先頭の添字（なしは -1。付随する束縛がなければ null）
            public int[] CompanionNext;  // 付随する束縛の添字 → 同じ SA 本体の次の付随する束縛の添字（なしは -1）
            public int Frame = 1;
            public ITriggerInput ReleaseInput;
            public volatile IActionBinding Pending;   // 復帰の合図（切替前の束縛）。適用では消えず、物理切断で消える
            public volatile bool ResetRequested;

            public DeviceRuntime(int device, IVirtualKBM virtualKbm)
            {
                Context = new EngineContext(device, virtualKbm, this);
                ReleasePredicate = control => !ReleaseInput.IsActive(control);
            }
        }

        // 出力アクションに渡す文脈（デバイスごとに再利用。割り当て 0）
        private sealed class EngineContext : IOutputContext, IOutputContextInput, IPendingUntriggerQuery
        {
            private readonly DeviceRuntime _owner;

            public EngineContext(int device, IVirtualKBM handler, DeviceRuntime owner)
            {
                Device = device;
                OutputHandler = handler;
                _owner = owner;
            }

            public SpecialAction PendingUntriggerAction => _owner.Pending?.Source;

            public int Device { get; }
            public IVirtualKBM OutputHandler { get; }
            public IFrameOutput Frame { get; private set; }
            public bool EndFrameRequested { get; private set; }

            public ITriggerInput Input { get; private set; }

            public void Begin(IFrameOutput frame, ITriggerInput input)
            {
                Frame = frame;
                Input = input;
                EndFrameRequested = false;
            }

            public void RequestEndFrame()
            {
                EndFrameRequested = true;
            }
        }
    }
}
