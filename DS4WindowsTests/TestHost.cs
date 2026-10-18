using System;
using DS4WinWPF;

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
