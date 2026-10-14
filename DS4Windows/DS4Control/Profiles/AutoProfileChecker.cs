using System;
using System.Security;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF
{
    [SuppressUnmanagedCodeSecurity]
    public class AutoProfileChecker
    {
        private readonly IAutoProfileService _service;

        public int AutoProfileDebugLogLevel
        {
            get => _service.AutoProfileDebugLogLevel;
            set => _service.AutoProfileDebugLogLevel = value;
        }

        public bool Running
        {
            get => _service.Running;
            set => _service.Running = value;
        }

        public delegate void ChangeServiceHandler(AutoProfileChecker sender, bool state);
        public event ChangeServiceHandler RequestServiceChange;

        public AutoProfileChecker(AutoProfileHolder holder, DS4Windows.DI.IProfileSettingsService profileSettings = null)
        {
            var appService = DS4WinWPF.AppHost.GetService<IAutoProfileService>();
            if (appService != null)
            {
                _service = appService;
            }
            else
            {
                // Phase6-Step10-4-2b: AutoProfileService の依存を必須化したため、ここで new はできない。
                // TODO(Step10-4-4): 本クラスは呼出元 0 件。経緯の調査後、削除の可否を判断する（規約 §2.4）。
                throw new System.InvalidOperationException("IAutoProfileService is not registered.");
            }

            _service.RequestServiceChange += (state) =>
            {
                RequestServiceChange?.Invoke(this, state);
            };
        }

        public void Process()
        {
            _service.CheckProfiles();
        }
    }
}
