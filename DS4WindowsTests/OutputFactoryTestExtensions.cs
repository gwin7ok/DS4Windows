using System;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I4c: テスト用の補助。本体から削除した互換の入口 <c>Create(device, sa, index, forControls)</c> の代わりに、SA を読み込みの部品で定義にして、
    /// 定義を受ける <see cref="ISpecialActionOutputFactory.Create(int, AssignmentDefinition, SpecialAction, int)"/> を呼ぶ（<paramref name="forControls"/> は由来に置き換わった）。
    /// 定義にならない SA（種別が不明、トリガーなし）は null。
    /// </summary>
    internal static class OutputFactoryTestExtensions
    {
        public static SpecialActionOutputs Create(this ISpecialActionOutputFactory factory, int device, SpecialAction sa, int index, bool forControls = false)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (sa == null) throw new ArgumentNullException(nameof(sa));

            AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa, forControls ? AssignmentOrigin.Controls : AssignmentOrigin.SpecialAction);
            return definition == null ? null : factory.Create(device, definition, sa, index);
        }
    }
}
