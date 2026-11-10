using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>MultiAction（タップ・長押し・ダブルタップ）の出力。</summary>
    public sealed class MultiActionOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IMacroStringPlayer _macroStringPlayer;

        public MultiActionOutputBuilder(IMacroStringPlayer macroStringPlayer)
        {
            _macroStringPlayer = macroStringPlayer ?? throw new ArgumentNullException(nameof(macroStringPlayer));
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.MultiAction;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            ControlSpecialItem item = definition.Item;
            // 独立した入力アクション（タップ・長押し・ダブルタップを Gesture で返す）。Fresh-press ガードを見ない。出力の Stop は不要（旧経路になし）。
            // 「どのジェスチャーでどのマクロか」の対応は、ここ（変換層）で決める: details を「,」で分けた 3 つ（タップ、長押し、ダブルタップの順。足りなければ空）から、
            // ジェスチャーごとの出力を 3 つそろえて作る（添字を固定するため、空でも作る）。エンジンが Outputs[Gesture - 1] を実行する（BindingPolicy.RoutesOutputsByGesture）

            // 手順は、型のある項目の 3 つのマクロ（タップ、長押し、ダブルタップ）を「/」でつないだ文字列にして渡す（再生側が受け取る形）
            var macros = new[]
            {
                item == null ? string.Empty : string.Join("/", item.TapMacro),
                item == null ? string.Empty : string.Join("/", item.HoldMacro),
                item == null ? string.Empty : string.Join("/", item.DoubleTapMacro),
            };
            var gestureOutputs = new IOutputAction[3];
            for (int g = 0; g < gestureOutputs.Length; g++)
                gestureOutputs[g] = new MacroStringAction(sa, device, macros[g], _macroStringPlayer);

            return new SpecialActionOutputs(gestureOutputs,
                OutputBindingPolicy.For(origin, definition, stopOnRelease: false, routesOutputsByGesture: true),
                isMultiTap: true);
        }
    }
}
