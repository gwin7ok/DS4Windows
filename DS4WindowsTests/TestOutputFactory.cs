using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Actions.OutputBuilders;

namespace DS4WindowsTests
{
    /// <summary>
    /// テスト用: 窓口（ポート）の一部だけで <see cref="SpecialActionOutputFactory"/> を作る。
    /// 渡された窓口に対応する種別の部品だけを足す（窓口が null の種別は束縛を作らない。本番は、全種別の部品がコンテナから渡る）。
    /// </summary>
    internal static class TestOutputFactory
    {
        public static SpecialActionOutputFactory Create(IProfileSettingsService profileSettings,
            IManagedActionManager manager, IKeyButtonControllerRegistry controllers, IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher, ISpecialActionRepository actionRepository = null,
            IMapping mapping = null, IGyroCalibrationPort gyroCalibration = null,
            IControllerDisconnectPort disconnectPort = null, IBatteryCheckPort batteryCheckPort = null,
            IMacroStringPlayer macroStringPlayer = null, IWindowFocuser windowFocuser = null)
        {
            if (profileSettings == null) throw new ArgumentNullException(nameof(profileSettings));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (controllers == null) throw new ArgumentNullException(nameof(controllers));
            if (macroPlayer == null) throw new ArgumentNullException(nameof(macroPlayer));
            if (processLauncher == null) throw new ArgumentNullException(nameof(processLauncher));
            if (profileSwitcher == null) throw new ArgumentNullException(nameof(profileSwitcher));

            var builders = new List<ISpecialActionOutputBuilder>
            {
                new ButtonOutputBuilder(manager),
                new KeyOutputBuilder(profileSettings, manager, controllers, actionRepository),
                new MacroOutputBuilder(macroPlayer, manager, actionRepository),
                new ProgramOutputBuilder(processLauncher, actionRepository),
                new ProfileOutputBuilder(profileSwitcher, profileSettings, actionRepository, mapping),
            };
            if (gyroCalibration != null) builders.Add(new GyroCalibrateOutputBuilder(gyroCalibration, actionRepository));
            if (disconnectPort != null) builders.Add(new DisconnectBtOutputBuilder(disconnectPort, profileSettings));
            if (batteryCheckPort != null) builders.Add(new BatteryCheckOutputBuilder(batteryCheckPort, actionRepository));
            if (macroStringPlayer != null) builders.Add(new MultiActionOutputBuilder(macroStringPlayer));
            if (windowFocuser != null) builders.Add(new FocusWindowOutputBuilder(windowFocuser));
            return new SpecialActionOutputFactory(builders);
        }
    }
}
