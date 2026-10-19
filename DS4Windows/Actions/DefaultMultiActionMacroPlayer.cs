using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b: <see cref="IMultiActionMacroPlayer"/> の本番の実装。<c>Mapping</c>（<c>IMapping</c> の実体）が実装する同名のメソッドへ委譲する。
    /// <c>Mapping</c> は後から作られる（ホスト構築時に作らない。Phase5-Plan §5.4）ため、遅延（<c>Func</c>）で受ける。
    /// </summary>
    public sealed class DefaultMultiActionMacroPlayer : IMultiActionMacroPlayer
    {
        private readonly Func<IMapping> _mapping;

        public DefaultMultiActionMacroPlayer(Func<IMapping> mapping)
        {
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        public void PlayMacroString(int deviceIndex, string macro)
        {
            (_mapping() as IMultiActionMacroPlayer)?.PlayMacroString(deviceIndex, macro);
        }
    }
}
