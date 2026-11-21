using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;

namespace DS4Windows.Core.Settings
{
    // ★ IDE0290 根本解決: C# 12 プライマリコンストラクタ構文を適用
    public partial class AppSettingsService(BackingStore config, IProfileXmlStore? xmlStore = null, IPathService? pathService = null) : IAppSettingsService
    {
        // Global の転送プロパティ経由をやめ、注入された BackingStore（設定の唯一の実体）を直接使う。
        private readonly BackingStore _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IProfileXmlStore? _xmlStore = xmlStore;
        private readonly IPathService? _pathService = pathService;

        // StartMinimized等はGlobal(m_Config/BackingStore)への
        // 正規シムとし、UI層(MainWindow.xaml.cs等)からGlobal直参照した場合と完全に同一の実体を
        // 参照するようにする。従来はここに独立したprivateフィールドを持ち、Global側と非連動という
        // 「孤立した重複状態」バグがあった(Step13投入時に発覚)。
        // 非連動プロパティ CheckWhenを削除した。

        public event EventHandler<string>? SettingChanged;

        // (※旧コンストラクタ public AppSettingsService(...) { ... } は削除)
        public bool Save()
        {
            bool success = _xmlStore!.SaveAppSettingsXml();   // 保存・読み込みを使う生成（DI の登録）では、必ず渡される
            if (success)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppSettingsService.Save succeeded");
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Settings_SaveFailed");
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogDebug("[DI] AppSettingsService.Save failed");
            }
            return success;
        }

        public bool Load()
        {
            bool success = _xmlStore!.LoadAppSettingsXml();   // 同上
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppSettingsService.Load: success={success}");
            return success;
        }

        private void NotifyChanged(string settingName)
        {
            SettingChanged?.Invoke(this, settingName);
        }

        /// <summary>
        /// 値が変わったときだけ、設定を書き換えて <see cref="SettingChanged"/> を出す（設定の後。通知の名前は呼び出したプロパティの名前）。
        /// 比較は <see cref="EqualityComparer{T}.Default"/>（bool・int・string・列挙型は、元の <c>!=</c> と同じ結果になる）。
        /// </summary>
        /// <summary>設定の領域のプロパティ用（プロパティは ref で渡せないため、現在の値と代入の処理を受ける）。</summary>
        private void SetIfChanged<T>(T current, Action<T> assign, T value, [CallerMemberName] string? settingName = null)
        {
            if (EqualityComparer<T>.Default.Equals(current, value)) return;
            assign(value);
            NotifyChanged(settingName!);   // CallerMemberName が、プロパティの中からの呼び出しで名前を埋める
        }

        private void SetIfChanged<T>(ref T field, T value, [CallerMemberName] string? settingName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            NotifyChanged(settingName!);   // CallerMemberName が、プロパティの中からの呼び出しで名前を埋める
        }

        public void SetCulture(string culture) => UiCulture.Set(culture);

        public bool FirstRun
        {
            // 旧 Global.firstRun。実体は PathService（FindConfigLocation が設定する）
            get => PathService.FirstRunState;
            set { if (PathService.FirstRunState != value) { PathService.FirstRunState = value; NotifyChanged(nameof(FirstRun)); } }
        }

        // 旧 Global.runHotPlug（保存しない実行時のフラグ）。このサービス（DI のシングルトン）のインスタンスの状態にした
        private bool _runHotPlug;

        public bool RunHotPlug
        {
            get => _runHotPlug;
            set => SetIfChanged(ref _runHotPlug, value);
        }

        // ---- ControlService の Global 直接参照解消（Global への薄い委譲）----

        public ControlServiceDeviceOptions DeviceOptions => _config.deviceOptions;

        // 静的イベントだった UDPServerSmoothing*Changed を、このサービスのインスタンスイベントにした
        // （購読者は ControlService のみ。発火の条件と順序は従来と同一で、発火元だけが Global の静的イベントから this に変わった）。
        public event EventHandler? UDPServerSmoothingMincutoffChanged;

        public event EventHandler? UDPServerSmoothingBetaChanged;
    }
}