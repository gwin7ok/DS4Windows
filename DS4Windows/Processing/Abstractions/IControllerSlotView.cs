using System;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// UI が見る、接続中のコントローラー 1 台の面。第 1 層の <c>DS4Device</c> を UI に直接持たせないための窓口（規約 1 章）。
    /// 実装は機器ごとに 1 つだけ作る（<see cref="IControllerSlotQuery"/> が返す）ため、同じ機器なら参照が同じで、一覧での同一判定に使える。
    /// 各イベントの <c>sender</c> は、この面自身（機器ではない）。購読の解除は、購読したときと同じデリゲートで行う。
    /// </summary>
    public interface IControllerSlotView
    {
        string DisplayName { get; }

        string MacAddress { get; }

        ConnectionType ConnectionType { get; }

        int Battery { get; }

        bool Charging { get; }

        /// <summary>入力の遅延（ミリ秒）。</summary>
        double Latency { get; }

        bool Synced { get; }

        /// <summary>シリアル（MAC）が、有効な値か（空の値ではないか）。</summary>
        bool HasValidSerial { get; }

        bool IsRemoving { get; }

        /// <summary>合体した機器（Joy-Con）の主側か。</summary>
        bool PrimaryDevice { get; }

        DeviceExclusiveStatus ExclusiveStatus { get; }

        /// <summary>この機器の設定値（機種ごとの設定）。</summary>
        ControllerOptionsStore OptionsStore { get; }

        event EventHandler BatteryChanged;

        event EventHandler ChargingChanged;

        event EventHandler MacAddressChanged;

        event EventHandler SyncChange;

        event EventHandler Removal;

        /// <summary>Bluetooth の接続を切る。入力スレッドを待たず、その場で切る（トレイのメニュー用）。</summary>
        bool DisconnectBluetooth();

        /// <summary>USB ドングルの接続を切る。</summary>
        bool DisconnectDongle();

        /// <summary>機器の入力スレッドで実行する処理を積む（接続を切る処理など）。</summary>
        void QueueEvent(Action action);

        /// <summary>入力の報告を止めた状態で処理を実行する。入力スレッドからは呼ばない。</summary>
        void HaltReportingRunAction(Action action);

        /// <summary>この機器の設定（コントローラーごとの設定ファイル）を保存する。</summary>
        bool SaveControllerConfigs();
    }
}
