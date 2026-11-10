
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Output;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-e: テスト用の <see cref="IProfileLoadTarget"/>。共有のホストから、初めて使うときに解決して転送する。
    /// 旧: テストの既定値 <c>() =&gt; TestHost.GetService&lt;ControlService&gt;()</c>（<c>Func&lt;ControlService&gt;</c> の遅延）と同じ意味で、
    /// 生成の時点ではホストを構築しない（ホストを作る前に <c>TestGlobals</c> などを準備するテストがあるため）。
    /// </summary>
    internal sealed class LazyProfileLoadTarget : IProfileLoadTarget
    {
        private IProfileLoadTarget Target => TestHost.GetService<IProfileLoadTarget>();

        public IConfigurableDevice GetController(int ind) => Target.GetController(ind);

        public ITouchPadGyroToggles GetTouchPad(int ind) => Target.GetTouchPad(ind);

        public bool HasOutputDevice(int ind) => Target.HasOutputDevice(ind);

        public void PluginOutDev(int index, IConfigurableDevice device) => Target.PluginOutDev(index, device);

        public void UnplugOutDev(int index, IConfigurableDevice device, bool immediate = false, bool force = false)
            => Target.UnplugOutDev(index, device, immediate, force);

        public void CheckProfileOptions(int ind, IConfigurableDevice device, bool startUp = false)
            => Target.CheckProfileOptions(ind, device, startUp);

        public void StartTPOff(int deviceID) => Target.StartTPOff(deviceID);

        public void PreLoadReset(int ind) => Target.PreLoadReset(ind);
    }
}
