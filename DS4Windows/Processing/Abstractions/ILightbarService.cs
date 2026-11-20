using DS4Windows.Input.Abstractions;

﻿namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// ライトバーの色の計算。プロファイルの設定、強制表示の状態（<c>LightbarOverrides</c>）、電池・充電・無操作の状態から色と点滅を決めて、デバイスへ設定する。
    /// 入力 1 フレームごとに、スロットごとに呼ぶ。
    /// </summary>
    public interface ILightbarService
    {
        void Update(ILightbarDevice device, int deviceNum);
    }
}
