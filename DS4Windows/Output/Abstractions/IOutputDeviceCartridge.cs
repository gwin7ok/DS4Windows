using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Devices;

namespace DS4Windows.Output.Abstractions
{
    /// <summary>
    /// 仮想パッドの出力の種類（<see cref="OutContType"/>）ごとの、生成、型の絞り込み、ゲームからの戻り（振動・ライトバー）の接続と解除。
    /// 種類ごとの分岐（if／switch）をここへ集め、種類を足すときはカートリッジを足す（DI に登録する）。
    /// </summary>
    public interface IOutputDeviceCartridge
    {
        OutContType Type { get; }

        /// <summary>仮想パッドを作る。<paramref name="busVersion"/> は ViGEmBus のバージョン（機能の有無の判定に使う）。</summary>
        OutputDevice Create(IVirtualPadBackend backend, Version busVersion);

        /// <summary>この種類の型に絞り込む（合わなければ null）。</summary>
        OutputDevice Narrow(OutputDevice device);

        /// <summary>ゲームからの振動・ライトバーの戻りを、入力デバイスへつなぐ。</summary>
        void AttachFeedback(int index, OutputDevice outDevice, InputDeviceCore device);

        /// <summary>つないだ戻りを外す。</summary>
        void RemoveFeedback(OutputDevice outDevice, int inputIndex);
    }
}
