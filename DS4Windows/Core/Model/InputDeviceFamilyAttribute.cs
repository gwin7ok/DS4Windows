using System;

namespace DS4Windows.Core.Model
{
    /// <summary>
    /// <see cref="InputDeviceType"/> の値が属する系統を宣言する。種類と系統の対応は、この属性が唯一の持ち主で、
    /// <see cref="InputDeviceTypeInfo"/> が起動時に一度だけ読む（種類を足すときは、enum の値にこの属性を付けるだけでよい）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InputDeviceFamilyAttribute : Attribute
    {
        public InputDeviceFamilyAttribute(InputDeviceFamily family)
        {
            Family = family;
        }

        public InputDeviceFamily Family { get; }
    }
}
