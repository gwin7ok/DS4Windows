using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Conditioning
{
    /// <summary>
    /// <see cref="ITouchpadToggleService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService.CheckForTouchToggle</c>／<c>StartTPOff</c>／<c>TouchpadSlide</c> を、そのまま移したもの。
    /// 値の特性化は 対応するテストが守る。
    /// </summary>
    public sealed class TouchpadToggleService : ITouchpadToggleService
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly int _controllerSlotLimit;

        private readonly bool[] _touchreleased = new bool[ControllerLimits.MaxControllerCount];
        private readonly bool[] _touchslid = new bool[ControllerLimits.MaxControllerCount];

        public TouchpadToggleService(IProfileSettingsService profileSettings, IEnvironmentService environmentService)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            if (environmentService == null) throw new ArgumentNullException(nameof(environmentService));
            _controllerSlotLimit = environmentService.ControllerSlotLimit;
            for (int i = 0; i < _touchreleased.Length; i++)
            {
                _touchreleased[i] = true;
            }
        }

        public void CheckForToggle(int deviceID, ControllerState cState, ControllerState pState)
        {
            if (_profileSettings.ForSlot(deviceID).Touchpad.OutputMode != TouchpadOutMode.Controls && cState.Touch1 && pState.PS)
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
                if (touchPad.Gestures.SlideRight && !_touchslid[ind])
                {
                    slidedir = "right";
                    _touchslid[ind] = true;
                }
                else if (touchPad.Gestures.SlideLeft && !_touchslid[ind])
                {
                    slidedir = "left";
                    _touchslid[ind] = true;
                }
                else if (!touchPad.Gestures.SlideLeft && !touchPad.Gestures.SlideRight)
                {
                    slidedir = "";
                    _touchslid[ind] = false;
                }
            }
            return slidedir;
        }

    }
}
