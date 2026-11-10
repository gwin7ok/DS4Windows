using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions.OutputBuilders;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// <see cref="ISpecialActionOutputFactory"/> の実装（DI の Singleton）。種別ごとの出力アクション・実行方針の組み立ては、
    /// 種別 1 つにつき 1 つの <see cref="ISpecialActionOutputBuilder"/> が持ち、ここは定義の種別で選んで呼ぶだけ（種別ごとの構成は Phase8-Step2-Design.md §6）。
    /// 種別に対応する部品が登録されていないとき（テスト）は、束縛を作らない（null）。
    /// </summary>
    public sealed class SpecialActionOutputFactory : ISpecialActionOutputFactory
    {
        private readonly Dictionary<SpecialAction.ActionTypeId, ISpecialActionOutputBuilder> _builders = new Dictionary<SpecialAction.ActionTypeId, ISpecialActionOutputBuilder>();

        public SpecialActionOutputFactory(IEnumerable<ISpecialActionOutputBuilder> builders)
        {
            if (builders == null) throw new ArgumentNullException(nameof(builders));
            foreach (ISpecialActionOutputBuilder builder in builders)
            {
                if (builder == null) throw new ArgumentException("builders に null がある。", nameof(builders));
                _builders.Add(builder.Kind, builder);   // 同じ種別を 2 つ登録すると例外（どちらを使うか曖昧になるため）
            }
        }

        /// <summary>
        /// Controls の Key と Macro だけを作る最小の部品（<see cref="ControlBindingBuilder"/> が、出力の部品を渡されなかったとき［テスト］に使う）。
        /// 他の種別（窓口が必要なもの）は、null を返す。
        /// </summary>
        public static SpecialActionOutputFactory ForControlsKeyAndMacro(IProfileSettingsService profileSettings, IManagedActionManager manager,
            IKeyButtonControllerRegistry controllers, IMacroPlayer macroPlayer)
        {
            if (profileSettings == null) throw new ArgumentNullException(nameof(profileSettings));
            var builders = new List<ISpecialActionOutputBuilder>();
            // 窓口が未接続の種別は、部品を足さない（束縛を作らない）
            if (manager != null && controllers != null) builders.Add(new KeyOutputBuilder(profileSettings, manager, controllers));
            if (manager != null && macroPlayer != null) builders.Add(new MacroOutputBuilder(macroPlayer, manager));
            return new SpecialActionOutputFactory(builders);
        }

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (source == null) throw new ArgumentNullException(nameof(source));

            // 設定の内容は定義から、実行時の状態の入れ物（出力アクションが受け取る SpecialAction）は source から。由来が Controls のときは、方針を Controls の形にする
            return _builders.TryGetValue(definition.Kind, out ISpecialActionOutputBuilder builder)
                ? builder.Create(device, definition, source, index)
                : null;
        }
    }
}
