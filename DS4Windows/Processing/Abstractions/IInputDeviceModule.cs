using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// デバイスの種類（DS4／DualSense／Switch Pro／Joy-Con／DS3）ごとの処理を持つ部品（カートリッジ）。
    /// 以前は <c>ControlService</c> の中に、<c>DeviceType</c> の分岐として散らばっていた
    /// （<c>CheckForSupportedDevice</c>、<c>GetKnownExtraButtons</c>、<c>PostDS4DeviceInit</c>、<c>PrepareDS4DeviceSettingHooks</c>、<c>CheckProfileOptions</c> の DualSense の枝）。
    /// 新しい種類の追加や差し替えは、この契約を実装して DI に登録するだけで、<c>ControlService</c> を触らずにできる。
    /// </summary>
    public interface IInputDeviceModule
    {
        /// <summary>この部品が扱う種類か。</summary>
        bool Handles(InputDeviceType type);

        /// <summary>この種類のデバイスを使う設定が有効か。</summary>
        bool IsEnabled(ControlServiceDeviceOptions options);

        /// <summary>マッピングで追加に見るボタン。なければ空のリスト。</summary>
        List<DS4Controls> GetKnownExtraButtons(InputDeviceCore device);

        /// <summary>デバイスの初期化の直後に呼ぶ。</summary>
        void OnDeviceInit(InputDeviceCore device, ControlServiceDeviceOptions options);

        /// <summary>接続時に、デバイス固有の設定の変更への反応を付ける。<paramref name="activeControllers"/> は発火のたびに読む。</summary>
        void HookSettings(InputDeviceCore device, Func<int> activeControllers);

        /// <summary>プロファイルの設定のうち、この種類に固有のものをデバイスへ反映する。</summary>
        void ApplyProfileOptions(InputDeviceCore device, int ind, IProfileSettingsService settings);
    }
}
