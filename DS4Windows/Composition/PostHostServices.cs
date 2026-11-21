#nullable enable
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Services;

namespace DS4Windows.Composition
{
    /// <summary>
    /// ホストの構築後に、起動の流れ（<c>App</c>）が使うサービス。<c>App</c> は WPF の <c>Application</c> で
    /// コンストラクターへ注入できないため、組み立ての層（ここ）で、一度だけまとめて解決して渡す。
    /// 解決の順序は、従来の呼び出しの順のまま（ホストの初期化の順序に影響するため変えない）。
    /// 起動の流れの全体を持つ箱ではなく、<c>App</c> がホストの構築直後から終了までに使う協力者だけ。
    /// </summary>
    public sealed class PostHostServices
    {
        public required AppThemeAndCultureService ThemeCulture { get; init; }
        public required IpcMemoryMap Ipc { get; init; }
        public required IAppSettingsService AppSettings { get; init; }
        public required IPathService Path { get; init; }
        public required IEnvironmentService Environment { get; init; }
        public required IProfileRepository ProfileRepository { get; init; }
        public required IProfileIdentityService ProfileIdentity { get; init; }
        public required ISpecialActionRepository SpecialActions { get; init; }
        public required IAppearanceSettingsService Appearance { get; init; }
        public required IDeviceStateService DeviceState { get; init; }
        public required IOutputSlotService OutputSlots { get; init; }
        public required INotificationService Notifications { get; init; }
        public required LogSettingsApplierProxy LogSettingsApplier { get; init; }
        public required IRuntimeFeatureSwitch RuntimeFeatures { get; init; }

        private PostHostServices()
        {
        }

        /// <summary>
        /// ホストから解決する。解決できないものがあれば <see cref="InvalidOperationException"/>
        /// （ホストの構築の失敗は、従来から起動の失敗）。
        /// </summary>
        public static PostHostServices Resolve()
        {
            // 初期化子は、書いた順に評価される
            return new PostHostServices
            {
                ThemeCulture = Required<AppThemeAndCultureService>(),
                Ipc = Required<IpcMemoryMap>(),
                AppSettings = Required<IAppSettingsService>(),
                Path = Required<IPathService>(),
                Environment = Required<IEnvironmentService>(),
                ProfileRepository = Required<IProfileRepository>(),
                ProfileIdentity = Required<IProfileIdentityService>(),
                SpecialActions = Required<ISpecialActionRepository>(),
                Appearance = Required<IAppearanceSettingsService>(),
                DeviceState = Required<IDeviceStateService>(),
                OutputSlots = Required<IOutputSlotService>(),
                Notifications = Required<INotificationService>(),
                LogSettingsApplier = Required<LogSettingsApplierProxy>(),
                RuntimeFeatures = Required<IRuntimeFeatureSwitch>(),
            };
        }

        private static T Required<T>() where T : class
        {
            return AppHost.GetService<T>()
                ?? throw new InvalidOperationException($"{typeof(T).Name} could not be resolved from AppHost.");
        }
    }
}
