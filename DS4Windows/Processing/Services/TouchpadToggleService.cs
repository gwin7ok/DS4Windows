using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: <see cref="ITouchpadToggleService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService.CheckForTouchToggle</c>／<c>StartTPOff</c>／<c>TouchpadSlide</c> を、そのまま移したもの。
    /// 値の特性化は <c>Phase9EInputReportPartsTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class TouchpadToggleService : ITouchpadToggleService
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly int _controllerSlotLimit;

        private readonly bool[] _touchreleased = new bool[ControllerLimits.MaxControllerCount];
        private readonly bool[] _touchslid = new bool[ControllerLimits.MaxControllerCount];

        public TouchpadToggleService(IProfileSettingsService profileSettings, IEnvironmentService environmentService)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            if (environmentService == null) throw new ArgumentNullException(nameof(environmentService));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
            for (int i = 0; i < _touchreleased.Length; i++)
            {
                _touchreleased[i] = true;
            }
        }

        public void CheckForToggle(int deviceID, ControllerState cState, ControllerState pState)
        {
            if (_profileSettings.TouchOutMode[deviceID] != TouchpadOutMode.Controls && cState.Touch1 && pState.PS)
            {
                if (_profileSettings.TouchpadActiveArray[deviceID] && _touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = false;
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.TouchpadMovementOff, LogOptions.UserFacing);
                    AppLogger.LogToTray(DS4Windows.Properties.Resources.TouchpadMovementOff);
                    _touchreleased[deviceID] = false;
                }
                else if (_touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = true;
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.TouchpadMovementOn, LogOptions.UserFacing);
                    AppLogger.LogToTray(DS4Windows.Properties.Resources.TouchpadMovementOn);
                    _touchreleased[deviceID] = false;
                }
            }
            else
                _touchreleased[deviceID] = true;
        }

        public void StartTouchpadOff(int deviceID)
        {
            if (deviceID < _controllerSlotLimit)
            {
                _profileSettings.TouchpadActiveArray[deviceID] = false;
            }
        }

        public string Slide(int ind, ControllerState cState, TouchpadMouse touchPad, bool connected)
        {
            string slidedir = "none";
            if (connected && cState.Touch2 &&
               !(touchPad.dragging || touchPad.dragging2))
            {
                if (touchPad.slideright && !_touchslid[ind])
                {
                    slidedir = "right";
                    _touchslid[ind] = true;
                }
                else if (touchPad.slideleft && !_touchslid[ind])
                {
                    slidedir = "left";
                    _touchslid[ind] = true;
                }
                else if (!touchPad.slideleft && !touchPad.slideright)
                {
                    slidedir = "";
                    _touchslid[ind] = false;
                }
            }
            return slidedir;
        }

    }
}
