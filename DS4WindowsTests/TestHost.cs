using System;
using DS4Windows.Composition;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W3: テストが、共有のホスト（<see cref="AppHost"/> の静的なホスト）からサービスを取得する入口。
    /// <see cref="AppHost.GetService{T}"/> は、ホストが未構築のとき暗黙に作らず null を返すようになったため、
    /// テストは、この入口で（未構築なら）ホストを明示的に作ってから取得する。
    /// ホストは全テストが共有し、<see cref="AppHost.Dispose"/> の後に取得すると、新しいホストが作られる。
    /// </summary>
    internal static class TestHost
    {
        /// <summary>共有のホストを返す。未構築のときは、明示的に作る。</summary>
        public static Microsoft.Extensions.Hosting.IHost Ensure()
        {
            return AppHost.Host ?? AppHost.CreateHost();
        }

        /// <summary>
        /// 本番の起動と同じ取り付け（<c>IRuntimeBootstrapper.Initialize</c>）を行う。何度呼んでも、ホストごとに 1 回だけ行われる。
        /// 取り付けを必要とするテスト（適用プロファイルの記憶、トリガーの 2 段階のリセットなど）だけが呼ぶ。
        /// </summary>
        public static void InitializeRuntime()
        {
            Ensure();
            AppHost.GetService<DS4Windows.Processing.Abstractions.IRuntimeBootstrapper>().Initialize();
        }

        public static T GetService<T>() where T : class
        {
            Ensure();
            return AppHost.GetService<T>();
        }

        public static object GetService(Type serviceType)
        {
            Ensure();
            return AppHost.GetService(serviceType);
        }
    }
}
