using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
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

        public bool CanSwitch(SpecialAction action)
        {
            return profileAppService == null || profileAppService.HasLoadableTarget(action);
        }

        public void RestoreProfile(int deviceIndex)
        {
            profileAppService?.RestoreFromAction(deviceIndex);
        }

    }
}