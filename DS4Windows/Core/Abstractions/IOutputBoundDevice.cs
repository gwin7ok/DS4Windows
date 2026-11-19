#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 仮想パッドと結びつく入力機器（接続・取り外し・戻りの振動／ライトバー・HidHide の印）が必要とする操作だけの狭い契約。
    /// 第 3 層（<c>Output</c>）が第 1 層の具象（<c>InputDeviceCore</c>）を参照しないために置く。実装するのは <c>InputDeviceCore</c> だけ。
    /// </summary>
    public interface IOutputBoundDevice : IRumbleTarget
    {
        /// <summary>画面・ログに出す機器の名前。</summary>
        string DisplayName { get; }

        string MacAddress { get; }

        /// <summary>HID のデバイスパス（HidHide の対象の判定に使う）。</summary>
        string DevicePath { get; }

        /// <summary>HidHide などによる独占の状態。</summary>
        DeviceExclusiveStatus CurrentExclusiveStatus { get; set; }

        DS4Color LightBarColor { get; }

        ref DS4LightbarState GetLightbarStateRef();

        void SetLightbarState(ref DS4LightbarState lightState);
    }
}
