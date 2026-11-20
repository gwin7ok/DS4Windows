#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // デバイスごとの実行時の状態
    internal sealed class DeviceRuntime
    {
        public readonly object Lock = new object();
        public readonly HashSet<DS4Controls> Suppressed = new HashSet<DS4Controls>();
        public readonly HashSet<string> GroupFired = new HashSet<string>();
        public readonly EngineContext Context;
        public readonly Predicate<DS4Controls> ReleasePredicate;

        public IReadOnlyList<IActionBinding> Bindings = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する
        public bool[] Prev = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する（それまでは null）
        public bool[] Established = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する
        public int[] Stamp = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する
        public Dictionary<SpecialAction, int> Index = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する
        public int[]? ControlIndex;   // Controls タブの束縛（7.8b）: DS4Controls → Bindings の先頭の添字（なしは -1）
        public int[]? ControlTail;    // 7.8e: DS4Controls → 鎖の最後の添字（Rebuild の間だけ使う）
        public int[] ControlNext = null!;   // 最初の Prepare（Rebuild）が、使う前に必ず設定する。    // 7.8e: 束縛の添字 → 同じボタンの次の束縛の添字（なしは -1）
        public volatile int ControlCount;
        public int[]? CompanionHead;  // SA 本体の添字 → 付随する束縛（SA の Extras）の先頭の添字（なしは -1。付随する束縛がなければ null）
        public int[]? CompanionNext;  // 付随する束縛の添字 → 同じ SA 本体の次の付随する束縛の添字（なしは -1）
        public int Frame = 1;
        public ITriggerInput? ReleaseInput;
        public volatile IActionBinding? Pending;   // 復帰の合図（切替前の束縛）。適用では消えず、物理切断で消える
        public volatile bool ResetRequested;

        public DeviceRuntime(int device, IVirtualKBM virtualKbm)
        {
            Context = new EngineContext(device, virtualKbm, this);
            ReleasePredicate = control => !ReleaseInput!.IsDigitalActive(control);   // 述語は DeviceRuntimeLifecycle.ReleaseSuppressed の間だけ呼ばれ、そのとき ReleaseInput は設定済み
              // R-5: 成立の判定（閾値なし）に合わせる
        }
    }
}
