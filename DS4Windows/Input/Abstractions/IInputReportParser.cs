using System;
using DS4Windows.Core.Model;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// 入力レポートのバイト列を、1 フレームの状態（<see cref="DS4State"/>）へ変換する解析の契約。
    /// OS・通信・スレッドに依存せず、バイト列と書き込み先の状態だけで決まる部分（ボタン、スティック、トリガー、方向キー、タッチの生値）だけを受け持つ。
    /// 電池・時刻・ジャイロ・タッチパッドの処理など、デバイスの状態を持つ部分は、デバイス側に残る。
    /// </summary>
    public interface IInputReportParser
    {
        /// <summary>入力レポート（レポート ID を含む。先頭が 0 番）を解析して、<paramref name="state"/> の該当する項目へ書く。</summary>
        void Parse(ReadOnlySpan<byte> report, DS4State state);
    }
}
