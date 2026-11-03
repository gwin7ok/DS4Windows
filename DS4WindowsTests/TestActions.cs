using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step4: 静的 <c>ActionManager</c>（廃止）の代わりに、共有のテスト用ホスト（<see cref="TestHost"/>）の管理役と登録簿を返す窓口。
    /// 旧: テストは <c>ActionManager.GetStateFor</c> などの静的メソッドを直接呼んでいた（本番と同じ <c>IManagedActionManager</c> に委譲されていた）。
    /// </summary>
    public static class TestActions
    {
        public static IManagedActionManager Manager => TestHost.GetService<IManagedActionManager>();

        public static IKeyButtonControllerRegistry Controllers => TestHost.GetService<IKeyButtonControllerRegistry>();

        /// <summary>
        /// 旧 <c>ActionManager.PreallocateForProfileApply</c>（本番では <c>ProfileApplicationService</c> の private メソッドへ移した）と同じ手順で、
        /// プロファイルの特殊アクションの状態とコントローラーを先に作る。
        /// </summary>
        public static void PreallocateForProfileApply(int device)
        {
            var provider = TestHost.GetService<IProfileActionProvider>();
            var names = provider?.GetProfileActionNames(device);
            if (names == null) return;

            foreach (var actionName in names)
            {
                var sa = provider.GetProfileAction(device, actionName);
                if (sa == null) continue;
                try { Manager.GetStateFor(sa, device); } catch { }
                try { Controllers.GetOrCreateForAction(device, sa); } catch { }
            }
        }
    }
}
