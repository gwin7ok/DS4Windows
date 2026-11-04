using System;
using System.Collections.Generic;
using System.Threading;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W1: <see cref="IActionRegistry"/> の実装（DI の Singleton）。デバイスごとの不変のスナップショットを、参照の差し替えで更新する。
    /// 既存の静的クラス <c>DS4Windows.ActionRegistry</c>（<c>SpecialAction</c> の一覧）とは別物。
    /// </summary>
    public sealed class ActionBindingRegistry : IActionRegistry
    {
        private static readonly IActionBinding[] Empty = new IActionBinding[0];

        private readonly IActionBinding[][] _bindings = new IActionBinding[ControllerLimits.MaxControllerCount][];

        public IReadOnlyList<IActionBinding> GetBindings(int device)
        {
            if (device < 0 || device >= _bindings.Length) return Empty;
            return Volatile.Read(ref _bindings[device]) ?? Empty;
        }

        public void Replace(int device, IReadOnlyList<IActionBinding> bindings)
        {
            if (device < 0 || device >= _bindings.Length)
                throw new ArgumentOutOfRangeException(nameof(device));

            IActionBinding[] snapshot = Empty;
            if (bindings != null && bindings.Count > 0)
            {
                snapshot = new IActionBinding[bindings.Count];
                for (int i = 0; i < snapshot.Length; i++)
                    snapshot[i] = bindings[i] ?? throw new ArgumentException("束縛に null を含められません。", nameof(bindings));
            }

            Volatile.Write(ref _bindings[device], snapshot);
        }

        public void Clear(int device)
        {
            if (device < 0 || device >= _bindings.Length) return;
            Volatile.Write(ref _bindings[device], Empty);
        }
    }
}
