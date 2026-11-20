#nullable enable

using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// <see cref="IMacroStringPlayer"/> の本番の実装。
    /// <c>Mapping</c> は後から作られるため、遅延（<c>Func&lt;IMapping&gt;</c>）で受け、<c>Mapping</c> が実装する <see cref="IMacroStringPlayer"/> へ委譲する。
    /// <c>Mapping</c> がなければ（テスト）、何もしない。
    /// </summary>
    public sealed class DefaultMacroStringPlayer : IMacroStringPlayer
    {
        private readonly IMapping _mapping;   // 旧 遅延取得デリゲート（循環なし）

        public DefaultMacroStringPlayer(IMapping mapping)
        {
            ArgumentNullException.ThrowIfNull(mapping);
            _mapping = mapping;
        }

        public void PlayMacroString(int deviceIndex, string macro)
        {
            (_mapping as IMacroStringPlayer)?.PlayMacroString(deviceIndex, macro);
        }
    }
}
