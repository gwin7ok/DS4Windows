#nullable enable
using System;
using System.Reflection;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 型の定数（<c>const</c>）の値を、リフレクションで読む。ソースの文字列に値が書かれていることではなく、本体の定数の値そのものを確かめるために使う。
    /// 非公開の定数も読む。
    /// </summary>
    internal static class ConstantValueReader
    {
        public static T Get<T>(Type type, string name)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.True(field != null && field.IsLiteral, $"{type.Name}.{name} が定数として宣言されていない");
            return (T)field!.GetRawConstantValue()!;
        }
    }
}
