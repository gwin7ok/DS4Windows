using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step7-8a-Plan.md W3）: <see cref="IControlBindingBuilder"/> の実装（DI の Singleton）。
    /// 移行済みの区分（<see cref="BindingMigrationOptions.MigratedControlKinds"/>）が空の間は、何も読まずに空のリストを返す
    /// （<c>ActionBindingBuilder</c> と同じ作法）。7.8a では、区分が有効になっても空を返す（組み立ては後続の PR）。
    /// </summary>
    // TODO(Phase8-Step7 7.8b〜f。規約 2.4): 割り当てごとの束縛の組み立ては、区分ごとの PR で実装する（7.8b: Key、7.8c: Macro、7.8e: Extras、7.8f: Button）。
    public sealed class ControlBindingBuilder : IControlBindingBuilder
    {
        private readonly BindingMigrationOptions _options;

        public ControlBindingBuilder(BindingMigrationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IReadOnlyList<IActionBinding> Build(int device)
        {
            if (_options.MigratedControlKinds.Count == 0) return Array.Empty<IActionBinding>();

            return Array.Empty<IActionBinding>();
        }
    }
}
