using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        // === デバイス検出・列挙契約 ===
        IEnumerable<DS4Device> FindControllers();
        IEnumerable<DS4Device> GetDS4Controllers();

        // === ライフサイクル・切断制御 ===
        void StopControllers();
        bool RemoveDevice(DS4Device device);
        void OnRemoval(object sender, EventArgs e);
        void UpdateSerial(object sender, EventArgs e);
        // TODO(Phase7-Step2 W5 の調査で「接続」と判断。削除しない): 2026-09-20 に Actions/ から DI/ へ移した契約。本来は、上位層（App）が
        // 第 1 層の静的な DS4Devices を直接呼ばず、この抽象を通すためのもの。呼び出し側の切り替えが未実施で、本体の呼び出しが 0 件。
        // 現状の差: App.xaml.cs:712 が DS4Devices.reEnableDevice を静的に直接呼ぶ（コンテナ構築前の Pre-Host の経路）。
        // 接続予定: Phase7-Step4（起動・終了シーケンスの近代化）。コンテナ構築後へ移せない場合は、Pre-Host の例外として判断し直す。
        void ReEnableDevice(string deviceInstanceId);

        // === 動作モード・ドライバ状態 ===
        bool IsExclusiveMode { get; set; }

        // === 初期化・昇格イベントおよびデリゲート ===
        event RequestElevationDelegate RequestElevation;
        PrepareInitDelegate PrepareDS4Init { get; set; }
        PrepareInitDelegate PostDS4Init { get; set; }
        CheckPendingDevice PreparePendingDevice { get; set; }
    }
}
