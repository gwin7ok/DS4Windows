using System.Collections.Generic;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// HID の列挙に載らない通信路（例: HID を持たない Xbox の XInput）の機器の供給元。
    /// 見つけた機器は、HID の機器と同じ後続の処理（有効設定の確認、仮想パッドの除外、シリアルの読み取り、カートリッジによる生成）へ流れる。
    /// 機器の種類は、返す <see cref="VidPidInfo"/> が決める。
    /// </summary>
    public interface IInputTransportSource
    {
        /// <summary>いま接続されている機器を列挙する（開いていない通信路を返す）。</summary>
        IReadOnlyList<InputTransportCandidate> Enumerate();
    }
}
