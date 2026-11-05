/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using NLog;

namespace DS4Windows.Core.Logging
{
    public class AppLogger
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static event EventHandler<DebugEventArgs> GuiLog;
        // 型付きプロファイル変更イベント
        public static event EventHandler<ProfileChangedEventArgs> ProfileChanged;

        // GUI表示用ログ（LoggerHolderがGuiLogイベントを受け取ってファイル出力）
        // Phase13b-Step1: 汎用のログ API（Log-Policy.md §1・§7）。レベル名付きの短縮形（LogTrace／LogDebug／LogInfo／LogWarn／LogError）は、
        // この API へ委譲する（決定: 2026-11-04。LogToGui とトレイ用の TrayIconLog イベントは Phase13b-Step9 で削除した）。
        //  - options が利用者向け（IsUserFacing）: GuiLog を発火する。ファイルへの出力は LoggerHolder が行う（一時表示は出さない）。画面の購読者は従来どおり GuiLog を受ける。
        //  - それ以外: ファイル（NLog）のみ。
        public static void Log(AppLogLevel level, string data, LogOptions options = null)
        {
            if (options != null && options.IsUserFacing)
            {
                GuiLog?.Invoke(null, new DebugEventArgs(data, level, options.IsTemporary));
                return;
            }

            Logger.Log(ToNLogLevel(level), data);
        }

        public static NLog.LogLevel ToNLogLevel(AppLogLevel level)
        {
            switch (level)
            {
                case AppLogLevel.Trace: return NLog.LogLevel.Trace;
                case AppLogLevel.Debug: return NLog.LogLevel.Debug;
                case AppLogLevel.Warn: return NLog.LogLevel.Warn;
                case AppLogLevel.Error: return NLog.LogLevel.Error;
                default: return NLog.LogLevel.Info;
            }
        }

        // Debugレベルログ専用メソッド（GUI表示なし、ログファイルのみ）
        public static void LogDebug(string data)
        {
            Log(AppLogLevel.Debug, data);
        }

        // Traceレベルログ専用メソッド（より詳細なデバッグ情報用）
        public static void LogTrace(string data)
        {
            Log(AppLogLevel.Trace, data);
        }

        // Expose trace-enabled flag to avoid expensive string formatting when trace is off
        public static bool IsTraceEnabled => Logger.IsTraceEnabled;

        // Phase10-Step4c W2: Debug の文字列の組み立て（割り当てを伴う）を、無効のときに省くため
        public static bool IsDebugEnabled => Logger.IsDebugEnabled;

        // Errorレベルログ専用メソッド（エラー情報用）
        // Warnレベルログ専用メソッド（警告情報用）
        public static void LogWarn(string data)
        {
            Log(AppLogLevel.Warn, data);
        }
        public static void LogError(string data)
        {
            Log(AppLogLevel.Error, data);
        }

        // Infoレベルログ専用メソッド（情報ログ用）
        public static void LogInfo(string data)
        {
            Log(AppLogLevel.Info, data);
        }

        // Phase7-Step5 W3（Q7＝A）: 旧 Global.NotificationServiceInstance。静的なロガーは注入できないため、App が起動時に
        // DI のシングルトンを設定する。設定前（ホスト構築より前、テスト）は、何もしない実装（NullNotificationService）を返す。
        // Phase14b-Step3（R-03）: 旧フォールバックは new AppNotificationService(new BackingStore(new EnvironmentService())) で、
        // 2 つ目の BackingStore を作っていた（購読者がいないため通知は表示されず、表示の挙動は同じ）。
        private static DS4Windows.Core.Abstractions.INotificationService _notificationService;

        public static DS4Windows.Core.Abstractions.INotificationService NotificationService
        {
            get => _notificationService ?? (_notificationService = new NullNotificationService());
            set => _notificationService = value;
        }

        public static void LogToTray(string data, bool warning = false, bool ignoreSettings = false)
        {
            // ignoreSettings: 現状も無効（無機能）のパラメータ。旧実装でも受信側(MainWindow)が
            // sender引数を一切参照していなかったため実質未配線だった。今回のリファクタでも
            // 意図的に配線しない（挙動を変えないため）。将来対応が必要になった場合は、
            // INotificationService.SendNotification 側にも同等の引数追加を検討すること。

            try
            {
                // title は意図的に空文字を渡す。表示本体(MainWindow.ShowSystemNotification)は
                // イベント側のタイトルを使わず常に TrayIconViewModel.ballonTitle を自前解決するため、
                // ここでUI層のクラスへ依存を持ち込む必要がない（下位層→上位層参照の禁止に抵触しないため）。
                // temporary は旧実装でも LogToTray からは常に false 固定だった（このメソッド自体に
                // temporary パラメータが存在しなかったため）。挙動を変えないためここでも false 固定とする。
                NotificationService.SendNotification(string.Empty, data, warning: warning, temporary: false);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Toast] LogToTray: SendNotification failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// プロファイル適用イベントを記録し、ProfileChanged イベントを発火する。
        /// </summary>
        /// <param name="displayNotification">
        /// このプロファイル変更をイベントとして通知対象とするか否か（サイレント内部処理を除外するためのフラグ。既定は true）。
        /// UIへの実際の表示可否（トースト／独自ウィンドウのどちらで出すか、あるいは出さないか）は、
        /// 本イベントの購読側（MainWindow.OnProfileChanged）が ProfileChangedNotification 設定を見て別途判定する。
        /// </param>
        public static void LogProfileChanged(int deviceIndex, string profileName, bool isTemp, ProfileChangeSource source = ProfileChangeSource.Unknown, string originalMessage = null, DateTime? timestamp = null)
        {
            Logger.Debug($"LogProfileChanged CALLED: device={deviceIndex}, profile={profileName}, isTemp={isTemp}, source={source}");

            try
            {
                // ★ displayNotification による不要なスキップを完全撤廃し、プロファイル適用イベントを常に発行する
                Logger.Debug("LogProfileChanged: Invoking ProfileChanged event");
                ProfileChanged?.Invoke(null, new ProfileChangedEventArgs(deviceIndex, profileName, isTemp, source, originalMessage, timestamp ?? DateTime.UtcNow));
            }
            catch { }
        }
    }
}