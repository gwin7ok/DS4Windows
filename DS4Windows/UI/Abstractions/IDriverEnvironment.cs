#nullable enable

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 初回の案内画面（<see cref="DS4Windows.UI.WelcomeDialog"/>）が、OS のバージョンと必要なドライバーの導入状況を知るための窓口。
    /// 画面が業務のサービス（<c>IEnvironmentService</c>）を直接参照しないための、画面の側の契約。
    /// </summary>
    public interface IDriverEnvironment
    {
        bool IsWin10OrGreater();
        bool IsWin8OrGreater();

        /// <summary>ViGEmBus の導入情報を調べ直す（導入の直後の確認の前に呼ぶ）。</summary>
        void RefreshViGEmBusInfo();
        bool IsViGEmBusInstalled();
        bool IsHidHideInstalled();
        bool IsFakerInputInstalled();
    }
}
