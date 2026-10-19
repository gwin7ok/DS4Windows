using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（W2b で <c>DefaultMultiActionMacroPlayer</c> から改名）: <see cref="IMacroStringPlayer"/> の本番の実装。
    /// <c>Mapping</c> は後から作られるため、遅延（<c>Func&lt;IMapping&gt;</c>）で受け、<c>Mapping</c> が実装する <see cref="IMacroStringPlayer"/> へ委譲する。
    /// <c>Mapping</c> がなければ（テスト）、何もしない。
    /// </summary>
    public sealed class DefaultMacroStringPlayer : IMacroStringPlayer
    {
        private readonly Func<IMapping> _mapping;

        public DefaultMacroStringPlayer(Func<IMapping> mapping)
        {
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        public void PlayMacroString(int deviceIndex, string macro)
        {
            (_mapping() as IMacroStringPlayer)?.PlayMacroString(deviceIndex, macro);
        }
    }
}
