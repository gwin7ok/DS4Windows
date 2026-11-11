using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>
    /// 実行方針。SA 用（由来が SA）は、種別ごとの値のまま（すべて既定なら共有の <see cref="BindingPolicy.Default"/>）。
    /// Controls 用（由来が Controls）は、Controls の既存の束縛（Key／Macro／Pad など）と同じく、トリガーのボタンを抑制せず・出力も消さず（<c>suppressesTriggerButtons: false</c>、
    /// <c>resetsTriggerOutput: false</c>）、代わりに割り当てたボタンの通常の出力を毎フレーム消す（<c>erasesOutputEveryFrame: true</c>）。
    /// </summary>
    internal static class OutputBindingPolicy
    {
        public static BindingPolicy For(AssignmentOrigin origin, AssignmentDefinition definition, bool blocksWhileExecuting = false,
            UntriggerPolicy untrigger = UntriggerPolicy.None, bool stopOnRelease = true, bool routesOutputsByGesture = false)
        {
            // Fresh-press ガードを見るかは、規則の表（FreshPressGuardTable）が決める。Button の Toggle だけ、SA の設定に依存する
            bool toggle = definition.Button?.Toggle ?? definition.Key?.Toggle ?? false;   // Button の Toggle（SA の KeyButtonSwitchMode）。Key では表が見ない
            bool respectsFreshPressGuard = FreshPressGuardTable.For(origin, definition.Kind, toggle);   // 種別 × 由来の表
            if (origin != AssignmentOrigin.Controls)
            {
                bool isDefault = respectsFreshPressGuard && !blocksWhileExecuting && untrigger == UntriggerPolicy.None && stopOnRelease && !routesOutputsByGesture;
                return isDefault
                    ? BindingPolicy.Default
                    : new BindingPolicy(respectsFreshPressGuard: respectsFreshPressGuard, blocksWhileExecuting: blocksWhileExecuting, untrigger: untrigger,
                        stopOnRelease: stopOnRelease, routesOutputsByGesture: routesOutputsByGesture);
            }

            return new BindingPolicy(respectsFreshPressGuard: respectsFreshPressGuard, blocksWhileExecuting: blocksWhileExecuting, untrigger: untrigger,
                suppressesTriggerButtons: false, resetsTriggerOutput: false, stopOnRelease: stopOnRelease,
                routesOutputsByGesture: routesOutputsByGesture, erasesOutputEveryFrame: true);
        }
    }
}
