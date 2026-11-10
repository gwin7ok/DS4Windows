using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// テスト用: アクションの束縛のビルダーを、窓口（ポート）を直接渡して組み立てる。
    /// 本番では、窓口の束ねは <see cref="ISpecialActionOutputFactory"/> が持つため、ここで出力の部品を作ってからビルダーへ渡す。
    /// </summary>
    internal static class TestActionBindingBuilders
    {
        public static ActionBindingBuilder Create(IProfileActionProvider profileActions, IProfileSettingsService profileSettings,
            IManagedActionManager manager, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository = null,
            IMapping mapping = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null,
            IMacroStringPlayer macroStringPlayer = null,
            IControlBindingBuilder controlBuilder = null, ISpecialActionOutputFactory outputFactory = null,
            IControlExtrasPort extrasPort = null, IKeyButtonControllerRegistry controllers = null)
        {
            if (profileActions == null) throw new System.ArgumentNullException(nameof(profileActions));
            outputFactory = outputFactory ?? new SpecialActionOutputFactory(profileSettings, manager, controllers, macroPlayer, processLauncher,
                profileSwitcher, actionRepository, mapping, gyroCalibration, disconnectPort, batteryCheckPort, macroStringPlayer);
            return new ActionBindingBuilder(profileActions, profileSettings, outputFactory, controlBuilder, extrasPort);
        }
    }
}
