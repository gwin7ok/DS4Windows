#nullable enable

using System;
using System.Xml;

namespace DS4Windows.Core.Persistence
{
    /// <summary>
    /// 読み込みの途中で「この要素がなければ、その設定の読み込みを諦める」ことを、明示する補助。
    /// 要素がないとき、<see cref="NullReferenceException"/> を投げる。外側の <c>catch</c> が、これを「設定がない」として受ける
    /// （例外の種類を変えないので、<c>catch (NullReferenceException)</c> でも、すべてを受ける <c>catch</c> でも、同じに動く）。
    /// </summary>
    internal static class XmlNodeRequired
    {
        public static XmlNode Required(this XmlNode? node)
        {
            return node ?? throw new NullReferenceException("XML の要素がない");
        }
    }
}
