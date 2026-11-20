#nullable enable

using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary><see cref="IHidHideApi"/> の実装。<c>HidHideAPIDevice</c>（ドライバーの制御デバイス）を、そのまま包む。</summary>
    public sealed class HidHideApiAdapter : IHidHideApi
    {
        private readonly HidHideAPIDevice _device = new HidHideAPIDevice();

        public bool IsOpen() => _device.IsOpen();

        public bool GetActiveState() => _device.GetActiveState();

        public List<string> GetBlacklist() => _device.GetBlacklist();

        public bool GetWhiteListInverseState() => _device.GetWhiteListInverseState();

        public List<string> GetWhitelist() => _device.GetWhitelist();

        public bool SetWhitelist(List<string> instances) => _device.SetWhitelist(instances);

        public bool SetBlacklist(List<string> instances) => _device.SetBlacklist(instances);

        public void Dispose() => _device.Dispose();
    }
}
