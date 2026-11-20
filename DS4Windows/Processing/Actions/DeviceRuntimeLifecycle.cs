#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// デバイスごとの実行時の状態（<see cref="DeviceRuntime"/>）の、束縛の差し替え・状態の初期化・出力の後始末・抑止の解除。
    /// 状態は持たない。<see cref="TriggerDispatchEngine"/> が、デバイスのロックを取ったうえで呼ぶ。
    /// </summary>
    internal static class DeviceRuntimeLifecycle
    {
        private static readonly int ControlTableSize = (int)Enum.GetValues(typeof(DS4Controls)).Cast<DS4Controls>().Max() + 1;

    // 現行の MapCustomAction の冒頭の解除パスと同じ: 実際に離されたボタンだけを、1 つずつ外す
    internal static void ReleaseSuppressed(DeviceRuntime rt, ITriggerInput input)
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

    // 束縛の差し替え・適用・切断のときに、IResettableOutputAction を実装する出力へ後始末を知らせる。
    // 有効にしていない出力は何もしない。割り当てが起きるのは、差し替えの瞬間だけ（毎フレームではない）
    internal static void ResetOutputs(DeviceRuntime rt, IReadOnlyList<IActionBinding>? bindings, ITriggerInput? input)
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

    internal static void Rebuild(DeviceRuntime rt, IReadOnlyList<IActionBinding> snapshot)
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

    internal static void ResetState(DeviceRuntime rt, ITriggerInput? input)
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

        internal static void LogException(Exception ex)
        {
            try
            {
                AppLogger.Log(AppLogLevel.Error, $"MapCustomAction exception: {ex}", LogOptions.UserFacing);
            }
            catch { /* 診断の出力の失敗は無視してよい */ }
        }
    }
}
