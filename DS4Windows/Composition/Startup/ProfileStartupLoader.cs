#nullable enable
using DS4Windows.Core.Abstractions;

using System;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// 起動時の、プロファイルの固定 ID の確定と、スペシャルアクション（Actions.xml）の読み込み。
    /// 順序が意味を持つ: SA の「プロファイルの切り替え」は切り替え先を ID で解決するため、ID の割り当て（ファイルへの書き込みを含む）が先。
    /// どちらも、プロファイルをスロットへ読み込む前（メイン画面の生成の前）に行う。
    /// </summary>
    public sealed class ProfileStartupLoader
    {
        private readonly IProfileIdentityService _identity;
        private readonly ISpecialActionRepository _actions;

        public ProfileStartupLoader(IProfileIdentityService identity, ISpecialActionRepository actions)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(actions);
            _identity = identity;
            _actions = actions;
        }

        public void Run()
        {
            // 書けなかったファイル（読み取り専用など）は、参照を保存する直前に改めて試す
            _identity.EnsureAllPersisted();

            // Actions.xml がなければ既定のアクションを作る
            if (!_actions.LoadActions())
            {
                _actions.CreateStandardActions();
            }
        }
    }
}
