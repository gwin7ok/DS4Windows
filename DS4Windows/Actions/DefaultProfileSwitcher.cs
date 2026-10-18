using System;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        private readonly IProfileApplicationService profileAppService;

        public DefaultProfileSwitcher()
        {
        }

        public DefaultProfileSwitcher(IProfileApplicationService profileAppService)
        {
            this.profileAppService = profileAppService;
        }

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            profileAppService?.ApplyFromAction(deviceIndex, action);
        }

        public void RestoreProfile(int deviceIndex)
        {
            profileAppService?.RestoreFromAction(deviceIndex);
        }

        public void ClearState(int deviceIndex)
        {
            profileAppService?.ClearPendingRestore(deviceIndex);
        }
    }
}