using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    /// <summary>プロファイルの適用・切断・束縛の再構築・抑止の管理。</summary>
    public partial class TriggerDispatchEngine
    {
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
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            finally
            {
                rt.ReleaseInput = null;
            }
        }

        public void OnProfileApplied(int device)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            // 適用スレッドは、入力スレッドのものである状態を直接書かない。次の処理の先頭でリセットする
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
                rt.Bindings = null!; // 次の Prepare が必ず作り直す。 次の処理で、空の一覧から作り直す
            }
        }

        // 登録簿の一覧が差し替わっていたら実行時の配列を作り直し、リセットの要求があれば状態を初期化する
        private void Prepare(DeviceRuntime rt, int device, ITriggerInput? input)
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

        // 束縛の差し替え・適用・切断のときに、IResettableOutputAction を実装する出力へ後始末を知らせる。
        // 有効にしていない出力は何もしない。割り当てが起きるのは、差し替えの瞬間だけ（毎フレームではない）
        private static void ResetOutputs(DeviceRuntime rt, IReadOnlyList<IActionBinding>? bindings, ITriggerInput? input)
        {
            if (bindings == null) return;

            for (int i = 0; i < bindings.Count; i++)
            {
                IReadOnlyList<IOutputAction>? outputs = bindings[i]?.Outputs;
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
                SpecialAction? source = snapshot[i]?.Source;
                if (source != null && !rt.Index.ContainsKey(source))
                    rt.Index.Add(source, i);

                // Controls タブの束縛（7.8b）: ボタン → 束縛の添字の表
                if (snapshot[i]?.Input is IControlBoundInput controlInput)   // 型ではなく印で見分ける（Controls の Multi-action も載せる）
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
                        else rt.ControlNext[rt.ControlTail![control]] = i;   // ControlIndex と同時に作る
                        rt.ControlTail![control] = i;   // 同上
                        rt.ControlCount++;
                    }
                }
            }

            // SA に付随する束縛（SA の Extras）を、SA 本体の添字につなぐ（SA 本体の ProcessOwned が、一緒に処理する）
            for (int j = 0; j < count; j++)
            {
                SpecialAction? primary = (snapshot[j] as TriggerBinding)?.PrimarySource;
                if (primary == null || !rt.Index.TryGetValue(primary, out int main) || main == j) continue;

                if (rt.CompanionHead == null)
                {
                    rt.CompanionHead = new int[count];
                    rt.CompanionNext = new int[count];
                    Array.Fill(rt.CompanionHead, -1);
                    Array.Fill(rt.CompanionNext, -1);
                }

                rt.CompanionNext![j] = rt.CompanionHead[main];   // CompanionHead と同時に作った
                rt.CompanionHead[main] = j;
            }

            rt.Frame++; // 古い評価結果を使わない
            rt.GroupFired.Clear();
        }

        private static void ResetState(DeviceRuntime rt, ITriggerInput? input)
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
    }
}
