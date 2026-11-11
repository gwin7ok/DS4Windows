using System;
using System.Linq;
using System.Reflection;
using DS4Windows.Core.Model;

namespace DS4WindowsTests
{
    /// <summary>
    /// 解析の差分テスト用。<see cref="ControllerState"/> の公開フィールド（入れ子の構造体を含む）を名前順に並べた文字列にして、2 つの状態を比べられるようにする。
    /// </summary>
    internal static class DS4StateSnapshot
    {
        public static string Of(ControllerState state)
        {
            return string.Join(";", Dump(state));
        }

        private static string[] Dump(object o)
        {
            return o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(f =>
                {
                    object v = f.GetValue(o);
                    return v != null && v.GetType().IsValueType && !v.GetType().IsPrimitive && !v.GetType().IsEnum
                        ? f.Name + "{" + string.Join(",", Dump(v)) + "}"
                        : f.Name + "=" + v;
                }).ToArray();
        }
    }
}
