using System;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase13-Step3: <see cref="IOutputKbmInitializer"/> の実装。旧 <c>ControlService.InitOutputKBMHandler</c> の手順
    /// （DetermineHandler → Connect → フォールバック／FakerInput のバージョン → InitializeMapping → PopulateConstants／PopulateMappings）を
    /// そのまま移した。経緯（旧 ServiceRegistration のコメント、Phase6-Step7-4）: 起動引数 -virtualkbm は、AppHost.CreateHost(config, parser) が IStartupArguments に
    /// 設定した実際の値を使う（コンテナへの登録ではなく保持役を経由する。2026-09-04［4c89cd91］以降、空のパーサーを渡していて無視された不具合の修正）。
    /// 旧 ControlService は生成時の値を保持したためホスト構築前に解決してはならなかったが、本クラスは呼び出し時に読むので、その制約はない。
    /// 起動引数は <c>Initialize()</c> の呼び出し時に <see cref="IStartupArguments"/> から読む（未設定なら空のパーサー）。
    /// </summary>
    public sealed class OutputKbmInitializer : IOutputKbmInitializer
    {
        private readonly IStartupArguments _startupArguments;
        private readonly IVirtualKBM _virtualKBM;
        private readonly IVirtualKBMLifecycle _kbmLifecycle;
        private readonly IProfileSettingsService _profileSettings;

        public OutputKbmInitializer(IStartupArguments startupArguments, IVirtualKBM virtualKBM, IVirtualKBMLifecycle kbmLifecycle,
            IProfileSettingsService profileSettings)
        {
            _startupArguments = startupArguments ?? throw new ArgumentNullException(nameof(startupArguments));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _kbmLifecycle = kbmLifecycle ?? throw new ArgumentNullException(nameof(kbmLifecycle));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        // 起動引数が設定されていない場合（引数なしの CreateHost()、テスト）は、従来どおり空のパーサー（既定の出力方式）
        private DS4WinWPF.ArgumentParser ResolveParser() => _startupArguments.Parser ?? new DS4WinWPF.ArgumentParser();

        public void Initialize()
        {
            string attemptVirtualkbmHandler = ResolveParser().VirtualkbmHandler;
            _kbmLifecycle.DetermineHandler(attemptVirtualkbmHandler);

            bool handlerConnected = false;
            try
            {
                handlerConnected = _virtualKBM.Connect();
            }
            catch { }

            if (!handlerConnected &&
                attemptVirtualkbmHandler != VirtualKBMFactory.GetFallbackHandlerIdentifier())
            {
                _kbmLifecycle.SwitchToFallbackHandler();
            }
            else
            {
                // Connection was made. Check if version number should get populated
                if (_virtualKBM.GetIdentifier() == FakerInputHandler.IDENTIFIER)
                {
                    _kbmLifecycle.ApplyFakerInputVersion();
                }
            }

            _kbmLifecycle.InitializeMapping(_virtualKBM.GetIdentifier());
            _profileSettings.OutputKBMMapping.PopulateConstants();
            _profileSettings.OutputKBMMapping.PopulateMappings();
        }
    }
}
