#nullable enable

using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// アプリの起動引数（<see cref="ArgumentParser"/>）を、DI コンテナ経由で
    /// 利用側（現在は <see cref="ControlService"/> の生成）へ受け渡すための保持役。
    ///
    /// DI コンテナは一度構築すると登録を追加できない。通常の起動では <c>App.xaml.cs</c> の明示的な
    /// <c>CreateHost(config, parser)</c> がホストを作るが、それより前に <c>AppHost.GetService</c> が呼ばれると
    ///</c>。現在は分岐が明示的にホストを作る）、起動引数なしでホストが先に暗黙に構築される
    /// （<c>docs-forDIMG/MadeByAgent/Archive/Phase6-Step7-Plan.md</c> §0.4）。どちらの順序でも動くよう、起動引数は「登録」ではなく、
    /// 登録済みのこの保持役へ <c>AppHost.CreateHost(config, parser)</c> が値を設定する形で渡す。
    /// </summary>
    public interface IStartupArguments
    {
        /// <summary>設定済みの起動引数。未設定なら null（起動引数なしのホスト、テストなど）。</summary>
        ArgumentParser? Parser { get; }

        /// <summary>起動引数を設定する。<c>AppHost.CreateHost(config, parser)</c> から呼ぶ。</summary>
        void SetParser(ArgumentParser parser);
    }
}
