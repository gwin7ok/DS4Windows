using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Xbox 系コントローラーの部品。設定の有効／無効のほか、接続のとき、設定「USB の親を隠す」に従って、HidHide で元の XInput の機器を隠す（二重入力を防ぐ）。
    /// 設定がオフのときは、前に足した分があれば戻す。HidHide を使えないときは、何も変えず、理由をログに出す。
    /// </summary>
    public sealed class XboxDeviceModule : InputDeviceModuleBase
    {
        private readonly IXboxParentHider hider;

        public XboxDeviceModule(IXboxParentHider hider)
        {
            this.hider = hider ?? throw new ArgumentNullException(nameof(hider));
        }

        protected override InputDeviceFamily? Family => InputDeviceFamily.Xbox;

        public override void OnDeviceInit(InputDeviceCore device, ControlServiceDeviceOptions options)
        {
            bool hide = options.Get<XboxDeviceOptions>(InputDeviceFamily.Xbox).HideUsbParent;
            XboxParentHideResult result = hider.Apply(device.HidDevice.DevicePath, hide);
            LogResult(result, hide);
        }

        private static void LogResult(XboxParentHideResult result, bool hide)
        {
            switch (result)
            {
                case XboxParentHideResult.Hidden:
                    AppLogger.LogUser(AppLogLevel.Info, "Log_HidHide_XboxHidden");
                    break;
                case XboxParentHideResult.AlreadyHidden:
                    AppLogger.LogUser(AppLogLevel.Info, "Log_HidHide_XboxAlreadyHidden");
                    break;
                case XboxParentHideResult.Restored:
                    AppLogger.LogUser(AppLogLevel.Info, "Log_HidHide_XboxRestored");
                    break;
                case XboxParentHideResult.HidHideNotInstalled when hide:
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_HidHide_XboxNotHidden_NotInstalled");
                    break;
                case XboxParentHideResult.NoAccess when hide:
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_HidHide_XboxNotHidden_NoAccess");
                    break;
                case XboxParentHideResult.InverseWhitelist:
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_HidHide_XboxNotHidden_Inverse");
                    break;
                case XboxParentHideResult.CloakOff:
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_HidHide_XboxNotHidden_CloakOff");
                    break;
                case XboxParentHideResult.WriteFailed:
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_HidHide_XboxNotHidden_WriteFailed");
                    break;
            }
        }
    }
}
