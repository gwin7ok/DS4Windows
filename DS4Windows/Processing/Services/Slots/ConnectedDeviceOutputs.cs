using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IConnectedDeviceOutputs"/> の実装（DI の Singleton。状態なし）。<c>DeviceConnector.PrepareConnected</c> にあった、
    /// 出力側の準備の部分を、そのまま（条件・順序を変えずに）移したもの。
    /// </summary>
    public sealed class ConnectedDeviceOutputs : IConnectedDeviceOutputs
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IAppSettingsService _appSettings;
        private readonly IOscService _oscService;
        private readonly IVirtualPadService _virtualPad;
        private readonly OutContType[] _activeOutDevType;   // 配列の参照は再代入されない（要素の書き込みのみ）
        private readonly IUdpMotionService _udpMotion;

        public ConnectedDeviceOutputs(IProfileSettingsService profileSettings, IAppSettingsService appSettings, IOscService oscService,
            IVirtualPadService virtualPad, IOutputDeviceTypeState outputDeviceTypes, IUdpMotionService udpMotion)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _activeOutDevType = (outputDeviceTypes ?? throw new ArgumentNullException(nameof(outputDeviceTypes))).ActiveOutDevType;
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
        }

        public void NotifyPlugged(int index)
        {
            if (_appSettings.UseOscSender)
            {
                _oscService.SendPlug(index, true);
            }
        }

        public void PlugVirtualPad(int index, InputDeviceCore device)
        {
            if (!_profileSettings.GetDInputOnly(index) && device.isSynced())
            {
                _virtualPad.PluginOutDev(index, device);
            }
            else
            {
                _profileSettings.UseDInputOnlyArray[index] = true;
                _activeOutDevType[index] = OutContType.None;
            }
        }

        public void PrepareMotion(int index, InputDeviceCore device)
        {
            if (_udpMotion.HasServer && index < _udpMotion.SlotCount)
            {
                _udpMotion.PrepareDeviceMotion(device, index);
            }
        }
    }
}
