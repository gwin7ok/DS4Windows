using System.Collections.Generic;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// XUSB のデバイス インターフェース（Xbox 系の機器）の列挙と、XInput の番号の問い合わせ。
    /// XInput の番号は機器の識別子を持たないため、機器ごとに、LED の状態（番号 0〜3 が 6〜9）から求める。
    /// 実装は <c>XusbInterfaceSource</c>。テストでは偽物に替えられる。
    /// </summary>
    public interface IXusbInterfaceSource
    {
        /// <summary>接続中の XUSB のインターフェースを列挙する。</summary>
        IReadOnlyList<XusbInterfaceInfo> Enumerate();

        /// <summary>インターフェースの XInput の番号（0〜3）を読む。機器が外れた・読めないときは -1。</summary>
        int ReadPlayerIndex(string devicePath);
    }
}
