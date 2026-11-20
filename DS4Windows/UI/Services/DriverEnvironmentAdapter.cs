using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary><see cref="IDriverEnvironment"/> を、環境のサービス（<see cref="IEnvironmentService"/>）へ委譲して実装する。</summary>
    public sealed class DriverEnvironmentAdapter : IDriverEnvironment
    {
        private readonly IEnvironmentService _environment;

        public DriverEnvironmentAdapter(IEnvironmentService environment)
        {
            ArgumentNullException.ThrowIfNull(environment);
            _environment = environment;
        }

        public bool IsWin10OrGreater() => _environment.IsWin10OrGreater();
        public bool IsWin8OrGreater() => _environment.IsWin8OrGreater();
        public void RefreshViGEmBusInfo() => _environment.RefreshViGEmBusInfo();
        public bool IsViGEmBusInstalled() => _environment.IsViGEmBusInstalled();
        public bool IsHidHideInstalled() => _environment.IsHidHideInstalled();
        public bool IsFakerInputInstalled() => _environment.IsFakerInputInstalled();
    }
}
