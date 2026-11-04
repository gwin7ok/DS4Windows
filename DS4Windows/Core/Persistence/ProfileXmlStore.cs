using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Core.Persistence
{
    public class ProfileXmlStore : IProfileXmlStore
    {
        private readonly BackingStore _backingStore;
        // Phase7-Step10c W2: コントローラー固有設定の保存・読込（旧: BackingStore のメソッド）
        private readonly DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer _controllerConfigsXml;
        // Phase7-Step10c W3: アプリ設定（Profiles.xml）の保存・読込（旧: BackingStore.Load／Save）
        private readonly DS4Windows.Core.Persistence.AppSettingsXmlSerializer _appSettingsXml;
        // Phase7-Step10c W4: プロファイル（旧形式の XML）の読込（旧: BackingStore.LoadProfile）
        private readonly DS4Windows.Core.Persistence.LegacyProfileXmlSerializer _legacyProfileXml;

        // Phase 5 ガードレール: Profiles.xml に対する同一プロセス内の並行アクセス競合を防ぐための静的排他ロックオブジェクト
        // (Step6: AppSettingsService との同一ファイル排他ロック共有)
        public static readonly object XmlIoLock = new object();

        // Phase7-Step3 W1: 任意引数＋Global 経由のフォールバックを廃止し、BackingStore を必須にした（Pure DI）。
        public ProfileXmlStore(BackingStore backingStore, DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer controllerConfigsXml,
            DS4Windows.Core.Persistence.AppSettingsXmlSerializer appSettingsXml, DS4Windows.Core.Persistence.LegacyProfileXmlSerializer legacyProfileXml)
        {
            _legacyProfileXml = legacyProfileXml ?? throw new ArgumentNullException(nameof(legacyProfileXml));
            _appSettingsXml = appSettingsXml ?? throw new ArgumentNullException(nameof(appSettingsXml));
            _controllerConfigsXml = controllerConfigsXml ?? throw new ArgumentNullException(nameof(controllerConfigsXml));
            _backingStore = backingStore ?? throw new ArgumentNullException(nameof(backingStore));
        }

        public bool LoadProfileXml(int deviceIndex, bool launchProgram, IProfileLoadTarget control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true)
        {
            lock (XmlIoLock)
            {
                try
                {
                    return _legacyProfileXml.LoadProfile(deviceIndex, launchProgram, control,
                        overridePath, xinputChange, postLoad);
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Error, $"Failed to load profile XML: {ex.Message}", LogOptions.UserFacing);
                    return false;
                }
            }
        }

        public bool SaveProfileXml(int deviceIndex, string profileName)
        {
            lock (XmlIoLock)
            {
                try
                {
                    return _legacyProfileXml.SaveProfile(deviceIndex, profileName);
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Error, $"Failed to save profile XML: {ex.Message}", LogOptions.UserFacing);
                    return false;
                }
            }
        }

        public bool LoadAppSettingsXml()
        {
            lock (XmlIoLock)
            {
                try
                {
                    bool result = _appSettingsXml.Load();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.LoadAppSettingsXml: loaded={result}");
                    return result;
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Error, $"Failed to load application settings XML: {ex.Message}", LogOptions.UserFacing);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogDebug($"[DI] ProfileXmlStore.LoadAppSettingsXml failed: {ex}");
                    return false;
                }
            }
        }

        public bool SaveAppSettingsXml()
        {
            lock (XmlIoLock)
            {
                try
                {
                    bool result = _appSettingsXml.Save();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileXmlStore.SaveAppSettingsXml: saved={result}");
                    return result;
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Error, $"Failed to save application settings XML: {ex.Message}", LogOptions.UserFacing);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogDebug($"[DI] ProfileXmlStore.SaveAppSettingsXml failed: {ex}");
                    return false;
                }
            }
        }

        // ---- Phase6-Step2-2 (PR-2): 接続時のコントローラー固有設定の読込 ----
        public bool LoadControllerConfigsForDevice(DS4Device device)
            => _controllerConfigsXml.LoadControllerConfigsForDevice(device);

        // ---- Phase6-Step3-1: 切断時のコントローラー固有設定の保存 ----
        public bool SaveControllerConfigsForDevice(DS4Device device)
            => _controllerConfigsXml.SaveControllerConfigsForDevice(device);
    }
}