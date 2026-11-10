using System;

namespace DS4Windows.Core.Model
{
    /// <summary>
    /// 入力デバイスの種類（<see cref="InputDeviceType"/>）と系統（<see cref="InputDeviceFamily"/>）の対応の表。
    /// 第 1 層のカートリッジと第 2 層の接続フックが、種類ごとに <c>==</c> を並べずに同じ表を引く。値は純粋な定数で、状態を持たない。
    /// 種類を足すときは、<see cref="InputDeviceType"/> と、ここの表へ 1 行足す。
    /// </summary>
    public static class InputDeviceTypeInfo
    {
        // InputDeviceType の値の順（enum の並びと一致させる。テストが全値を検査する）
        private static readonly InputDeviceFamily[] FamilyByType =
        {
            InputDeviceFamily.DS4,         // DS4
            InputDeviceFamily.SwitchPro,   // SwitchPro
            InputDeviceFamily.JoyCon,      // JoyConL
            InputDeviceFamily.JoyCon,      // JoyConR
            InputDeviceFamily.JoyCon,      // JoyConGrip
            InputDeviceFamily.DualSense,   // DualSense
            InputDeviceFamily.DS3,         // DS3
        };

        /// <summary>種類が属する系統。</summary>
        public static InputDeviceFamily FamilyOf(InputDeviceType type)
        {
            int i = (int)type;
            if (i < 0 || i >= FamilyByType.Length) throw new ArgumentOutOfRangeException(nameof(type));
            return FamilyByType[i];
        }

        /// <summary>種類が指定の系統か。表にない値（将来の種類や不正な値）は、どの系統でもない（false）。</summary>
        public static bool IsFamily(InputDeviceType type, InputDeviceFamily family)
        {
            int i = (int)type;
            return i >= 0 && i < FamilyByType.Length && FamilyByType[i] == family;
        }

        /// <summary>Joy-Con の片側（左または右）か。充電グリップは含まない（グリップは、どちらかの側に付いて初めて左右になる）。</summary>
        public static bool IsJoyConSide(InputDeviceType type)
        {
            return type == InputDeviceType.JoyConL || type == InputDeviceType.JoyConR;
        }
    }
}
