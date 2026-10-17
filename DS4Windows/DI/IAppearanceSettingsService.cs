using DS4Windows;

namespace DS4Windows.DI
{
    /// <summary>
    /// アプリテーマおよびトレイアイコン表示設定を提供するサービス。
    /// 全体移行計画書 §5.4 #9 で計画されていたが未実装のまま残っていたため、
    /// Phase5-Step14前クリーンアップの一環として新設する。
    /// </summary>
    public interface IAppearanceSettingsService
    {
        AppThemeChoice UseCurrentTheme { get; set; }
        TrayIconChoice UseIconChoice { get; set; }

        /// <summary>
        /// 指定したトレイアイコン種別に対応するリソースパスを取得します。
        /// </summary>
        string GetIconResourcePath(TrayIconChoice choice);

        // ---- Phase6-Step2-5 (PR-5): バッテリー残量の変化通知（このサービスのイベント）----
        void InvokeBatteryChanged(byte percentage);

        // ---- Phase6-Step9-1: バッテリー残量の変化の購読（このサービスのイベント）----
        /// <summary>
        /// バッテリー残量が変化したときの通知（<see cref="InvokeBatteryChanged"/> と対）。
        /// 購読者へ通知する。送信元はこのサービス（旧: <c>typeof(Global)</c>）、引数は従来と同じ。
        /// </summary>
        event System.EventHandler<byte> BatteryChanged;
    }
}