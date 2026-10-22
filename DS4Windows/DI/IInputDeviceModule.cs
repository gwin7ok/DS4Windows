using System;
using System.Collections.Generic;
using DS4Windows.InputDevices;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6c（Phase9-E6-Plan.md §3.1）: デバイスの種類（DS4／DualSense／Switch Pro／Joy-Con／DS3）ごとの処理を持つ部品（カートリッジ）。
    /// 以前は <c>ControlService</c> の中に、<c>DeviceType</c> の分岐として散らばっていた
    /// （<c>CheckForSupportedDevice</c>、<c>GetKnownExtraButtons</c>、<c>PostDS4DeviceInit</c>、<c>PrepareDS4DeviceSettingHooks</c>、<c>CheckProfileOptions</c> の DualSense の枝）。
    /// 新しい種類の追加や差し替えは、この契約を実装して DI に登録するだけで、<c>ControlService</c> を触らずにできる。
    /// </summary>
    public interface IInputDeviceModule
    {
        /// <summary>この部品が扱う種類か。</summary>
        bool Handles(InputDeviceType type);

        /// <summary>この種類のデバイスを使う設定が有効か（旧 <c>CheckForSupportedDevice</c> の switch）。</summary>
        bool IsEnabled(ControlServiceDeviceOptions options);

        /// <summary>マッピングで追加に見るボタン（旧 <c>GetKnownExtraButtons</c> の switch）。なければ空のリスト。</summary>
        List<DS4Controls> GetKnownExtraButtons(DS4Device device);

        /// <summary>デバイスの初期化の直後に呼ぶ（旧 <c>PostDS4DeviceInit</c>。Joy-Con の結合の初期値）。</summary>
        void OnDeviceInit(DS4Device device, ControlServiceDeviceOptions options);

        /// <summary>接続時に、デバイス固有の設定の変更への反応を付ける（旧 <c>PrepareDS4DeviceSettingHooks</c>。DualSense の LED）。<paramref name="activeControllers"/> は発火のたびに読む。</summary>
        void HookSettings(DS4Device device, Func<int> activeControllers);

        /// <summary>プロファイルの設定のうち、この種類に固有のものをデバイスへ反映する（旧 <c>CheckProfileOptions</c> の DualSense の枝）。</summary>
        void ApplyProfileOptions(DS4Device device, int ind, IProfileSettingsService settings);
    }
}
