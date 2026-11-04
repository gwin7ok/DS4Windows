
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// Phase6-Step7-4（決定7＝案H）: <see cref="IStartupArguments"/> の実装。起動引数を保持するだけで、解析はしない。
    /// </summary>
    public class StartupArguments : IStartupArguments
    {
        private volatile ArgumentParser _parser;

        public ArgumentParser Parser => _parser;

        public void SetParser(ArgumentParser parser)
        {
            _parser = parser;
        }
    }
}
