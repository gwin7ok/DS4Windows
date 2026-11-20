using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>更新の確認・言語・アイコン・テーマ・ログの設定の窓口。</summary>
    public partial class AppSettingsService
    {
        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い公開アクセサへの正規シム。
        // Notifications 等と同一パターン（独立フィールドを持たない）。
        public string LogMinLevel
        {
            get => _config.Log.logMinLevel;
            set => SetIfChanged(_config.Log.logMinLevel, v => _config.Log.logMinLevel = v, value);
        }

        public bool CheckUpdateStartupEnabled
        {
            get => _config.UpdateCheck.checkUpdateStartup;
            set => SetIfChanged(_config.UpdateCheck.checkUpdateStartup, v => _config.UpdateCheck.checkUpdateStartup = v, value);
        }

        public int CheckEveryValue
        {
            get => _config.UpdateCheck.checkEveryValue;
            set => SetIfChanged(_config.UpdateCheck.checkEveryValue, v => _config.UpdateCheck.checkEveryValue = v, value);
        }

        public int CheckEveryUnit
        {
            get => _config.UpdateCheck.checkEveryUnit;
            set => SetIfChanged(_config.UpdateCheck.checkEveryUnit, v => _config.UpdateCheck.checkEveryUnit = v, value);
        }

        public DateTime LastChecked
        {
            get => _config.UpdateCheck.lastChecked;
            set => SetIfChanged(_config.UpdateCheck.lastChecked, v => _config.UpdateCheck.lastChecked = v, value);
        }

        // _config.UpdateCheck.lastVersionCheckedNum は読み取り専用（LastVersionChecked代入時に自動算出）のため、
        // 本シムも読み取り専用として公開する。
        public ulong LastVersionCheckedNum => _config.UpdateCheck.lastVersionCheckedNum;

        // 代入時に番号（LastVersionCheckedNum）を算出する処理を、Global のプロパティからここへ移した。
        public string LastVersionChecked
        {
            get => _config.UpdateCheck.lastVersionChecked;
            set
            {
                if (_config.UpdateCheck.lastVersionChecked != value)
                {
                    _config.UpdateCheck.lastVersionChecked = value;
                    _config.UpdateCheck.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(value);
                    NotifyChanged(nameof(LastVersionChecked));
                    NotifyChanged(nameof(LastVersionCheckedNum));
                }
            }
        }

        // ---- App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // 他のプロパティと同じく Global(m_Config/BackingStore) への正規シム（独立フィールドを持たない）。
        public string UseLang
        {
            get => _config.Appearance.useLang;
            set => SetIfChanged(_config.Appearance.useLang, v => _config.Appearance.useLang = v, value);
        }

        // ファイル名に使えない文字を含む値は黙って無視する（従来の Global.FakeExeName の setter の動作。Phase7-Step3 W4 でここへ移した）。
        // 値が実際に変わったときだけ通知する。
        public string FakeExeName
        {
            get => _config.Appearance.fakeExeFileName;
            set
            {
                if (_config.Appearance.fakeExeFileName != value)
                {
                    if (!(value.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0))
                    {
                        _config.Appearance.fakeExeFileName = value;
                    }

                    if (_config.Appearance.fakeExeFileName == value)
                    {
                        NotifyChanged(nameof(FakeExeName));
                    }
                }
            }
        }

        public int LogMaxArchiveFiles
        {
            get => _config.Log.logMaxArchiveFiles;
            set => SetIfChanged(_config.Log.logMaxArchiveFiles, v => _config.Log.logMaxArchiveFiles = v, value);
        }
    }
}
