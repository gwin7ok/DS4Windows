using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DS4Windows.Composition
{
    /// <summary>
    /// <see cref="IMainWindowFactory"/> の実装。メイン画面は、コンテナが依存を補って直接つくる（<see cref="ActivatorUtilities"/>）。
    /// 起動引数（<see cref="ArgumentParser"/>）だけが実行時の値で、窓の依存の一覧を工場が二重に持たないようにするための置き場所。
    /// <see cref="IServiceProvider"/> を受けてよいのは組み立て専用のこの層だけで、窓や業務のクラスには渡さない（Pure DI の維持）。
    /// 窓の依存（<c>IAutoProfileService</c> を含む）は、生成のたびに解決される。自動プロファイルの設定コレクションは
    /// 設定の場所が決まった後でなければ生成できないため、呼び出し側（<c>App</c>）がホストの構築後に呼ぶ順序を保つこと。
    /// </summary>
    public sealed class MainWindowFactory : IMainWindowFactory
    {
        private readonly IServiceProvider _services;

        public MainWindowFactory(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public MainWindow Create(ArgumentParser parser)
        {
            var window = ActivatorUtilities.CreateInstance<MainWindow>(_services, parser);
            // 複製の名前入力欄が使うサービスは、窓の引数を増やさないよう、ここで渡す
            window.dupBox.Initialize(
                _services.GetRequiredService<IPathService>(),
                _services.GetRequiredService<IProfileIdentityService>());
            return window;
        }
    }
}
