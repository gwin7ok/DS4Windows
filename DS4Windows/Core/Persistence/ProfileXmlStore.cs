#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    public class ProfileXmlStore : IProfileXmlStore
    {
        private readonly BackingStore _backingStore;
        // コントローラー固有設定の保存・読込
        private readonly DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer _controllerConfigsXml;
        // アプリ設定（Profiles.xml）の保存・読込
        private readonly DS4Windows.Core.Persistence.AppSettingsXmlSerializer _appSettingsXml;
        // プロファイルの読込
        private readonly DS4Windows.Core.Persistence.LegacyProfileXmlSerializer _legacyProfileXml;

        // Profiles.xml に対する同一プロセス内の並行アクセス競合を防ぐための静的排他ロックオブジェクト
        // (Step6: AppSettingsService との同一ファイル排他ロック共有)
        public static readonly object XmlIoLock = new object();

        // 任意引数＋Global 経由のフォールバックを廃止し、BackingStore を必須にした（Pure DI）。
        public ProfileXmlStore(BackingStore backingStore, DS4Windows.Core.Persistence.ControllerConfigsXmlSerializer controllerConfigsXml,
            DS4Windows.Core.Persistence.AppSettingsXmlSerializer appSettingsXml, DS4Windows.Core.Persistence.LegacyProfileXmlSerializer legacyProfileXml)
        {
            ArgumentNullException.ThrowIfNull(legacyProfileXml);
            _legacyProfileXml = legacyProfileXml;
            ArgumentNullException.ThrowIfNull(appSettingsXml);
            _appSettingsXml = appSettingsXml;
            ArgumentNullException.ThrowIfNull(controllerConfigsXml);
            _controllerConfigsXml = controllerConfigsXml;
            ArgumentNullException.ThrowIfNull(backingStore);
            _backingStore = backingStore;
        }

        public bool LoadProfileXml(int deviceIndex, IProfileLoadTarget control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true)
        {
            lock (XmlIoLock)
            {
                try
                {
                    return _legacyProfileXml.LoadProfile(deviceIndex, control,
                        overridePath, xinputChange, postLoad);
                }
                catch (Exception ex)
                {
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Xml_ProfileLoadFailed", ex.Message);
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
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Xml_ProfileSaveFailed", ex.Message);
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
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Xml_SettingsLoadFailed", ex.Message);
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
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Xml_SettingsSaveFailed", ex.Message);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogDebug($"[DI] ProfileXmlStore.SaveAppSettingsXml failed: {ex}");
                    return false;
                }
            }
        }

        // ---- 接続時のコントローラー固有設定の読込 ----
        public bool LoadControllerConfigsForDevice(IConfigurableDevice device)
            => _controllerConfigsXml.LoadControllerConfigsForDevice(device);

        // ---- 切断時のコントローラー固有設定の保存 ----
        public bool SaveControllerConfigsForDevice(IConfigurableDevice device)
            => _controllerConfigsXml.SaveControllerConfigsForDevice(device);
    }
}