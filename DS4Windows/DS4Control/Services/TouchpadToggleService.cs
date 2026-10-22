using System;
using DS4Windows.DI;

namespace DS4Windows.Services
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

        public event Action<string, bool> DebugMessage;

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

        public void CheckForToggle(int deviceID, DS4State cState, DS4State pState)
        {
            if (_profileSettings.TouchOutMode[deviceID] != TouchpadOutMode.Controls && cState.Touch1 && pState.PS)
            {
                if (_profileSettings.TouchpadActiveArray[deviceID] && _touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = false;
                    LogDebug(DS4WinWPF.Properties.Resources.TouchpadMovementOff);
                    AppLogger.LogToTray(DS4WinWPF.Properties.Resources.TouchpadMovementOff);
                    _touchreleased[deviceID] = false;
                }
                else if (_touchreleased[deviceID])
                {
                    _profileSettings.TouchpadActiveArray[deviceID] = true;
                    LogDebug(DS4WinWPF.Properties.Resources.TouchpadMovementOn);
                    AppLogger.LogToTray(DS4WinWPF.Properties.Resources.TouchpadMovementOn);
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

        public string Slide(int ind, DS4State cState, Mouse touchPad, bool connected)
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

        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }
    }
}
