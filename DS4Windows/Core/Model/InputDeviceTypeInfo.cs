using System;

namespace DS4Windows.Core.Model
{
    /// <summary>
    /// 入力デバイスの種類（<see cref="InputDeviceType"/>）と系統（<see cref="InputDeviceFamily"/>）の対応の表。
    /// 第 1 層のカートリッジと第 2 層の接続フックが、種類ごとに <c>==</c> を並べずに同じ表を引く。値は純粋な定数で、状態を持たない。
    /// 種類を足すときは、<see cref="InputDeviceType"/> の値に <see cref="InputDeviceFamilyAttribute"/> を付ける（ここは変えない）。
    /// </summary>
    public static class InputDeviceTypeInfo
    {
        // InputDeviceType の値の順（要素 i が、値 i の系統）。対応は enum の値に付けた [InputDeviceFamily] が持ち、ここは起動時に一度だけ読む
        private static readonly InputDeviceFamily[] FamilyByType = BuildFamilyTable();

        private static InputDeviceFamily[] BuildFamilyTable()
        {
            InputDeviceType[] values = (InputDeviceType[])Enum.GetValues(typeof(InputDeviceType));
            InputDeviceFamily[] table = new InputDeviceFamily[values.Length];
            foreach (InputDeviceType value in values)
            {
                InputDeviceFamilyAttribute attr = (InputDeviceFamilyAttribute)Attribute.GetCustomAttribute(
                    typeof(InputDeviceType).GetField(value.ToString()), typeof(InputDeviceFamilyAttribute));
                if (attr == null)
                {
                    throw new InvalidOperationException("InputDeviceType." + value + " に [InputDeviceFamily] がない");
                }

                table[(int)value] = attr.Family;
            }

            return table;
        }

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
    }
}
