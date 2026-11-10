using System;
using System.Collections.Generic;
using DS4Windows.Output.Backend;

namespace DS4WindowsTests
{
    /// <summary>
    /// ドライバー（ViGEmBus）なしで仮想パッドの出力を確かめるための偽の <see cref="IVirtualPadBackend"/>。
    /// 作った対象（<see cref="Xbox360Targets"/>、<see cref="Ds4Targets"/>）が受けた操作を記録する。
    /// </summary>
    public sealed class FakeVirtualPadBackend : IVirtualPadBackend
    {
        /// <summary>false にすると、<see cref="Start"/> に失敗した状態（<see cref="IsStarted"/> が false のまま）になる。</summary>
        public bool StartSucceeds { get; set; } = true;

        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public bool IsStarted { get; private set; }

        public List<FakeXbox360Target> Xbox360Targets { get; } = new List<FakeXbox360Target>();
        public List<FakeDs4Target> Ds4Targets { get; } = new List<FakeDs4Target>();

        public void Start()
        {
            StartCount++;
            IsStarted = StartSucceeds;
        }

        public void Stop()
        {
            StopCount++;
            IsStarted = false;
        }

        public IXbox360PadTarget CreateXbox360Target()
        {
            RequireStarted();
            var target = new FakeXbox360Target();
            Xbox360Targets.Add(target);
            return target;
        }

        public IDs4PadTarget CreateDs4Target()
        {
            RequireStarted();
            var target = new FakeDs4Target();
            Ds4Targets.Add(target);
            return target;
        }

        private void RequireStarted()
        {
            if (!IsStarted) throw new InvalidOperationException("The virtual pad backend is not started.");
        }
    }
}
