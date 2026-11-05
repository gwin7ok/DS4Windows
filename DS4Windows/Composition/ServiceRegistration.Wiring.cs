using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Settings;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4Windows.Composition
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Phase14c-Step6（R-13）: 生成のときに、別のオブジェクトへ自分（または自分の持ち物）を結ぶ登録。後付けの配線は、すべてこのメソッドに置く。
        /// 配線は、そのサービスが最初に解決されたときに行う（遅延。生成の時点は Phase14c より前と同じ）。
        /// <list type="number">
        /// <item><c>BackingStore</c>: <c>ProcessInspector</c>（プロパティ）と、<c>ActionsLoaded</c> の購読者（<c>ActionsLoadedRuntimeReset</c>）。
        /// ブートストラップの <c>BackingStore</c> はコンテナより前に作られるため、コンストラクターでは渡せない。</item>
        /// <item><c>IProfileIdentityService</c>: 登録簿を、<c>BackingStore</c>（スロットの名前の保管）と <c>ProfileSettingsService</c>（一時プロファイルの名前）へ結ぶ。
        /// <c>BackingStore</c> の登録から登録簿を解決すると <c>IPathService</c> → <c>BackingStore</c> と循環するため、登録簿の側から結ぶ。
        /// 本番では、App の起動（ホストの構築の後）が解決する。<c>IProfileSettingsService</c> が <c>ProfileSettingsService</c> でない（テストの偽物）ときは、そちらへは結ばない。</item>
        /// <item><c>IOutputDeviceTypeState</c>: 出力種別の配列を <c>BackingStore.ActiveOutDevType</c> へ渡す。</item>
        /// </list>
        /// 新しい後付けの配線を足すときは、このメソッドへ足し、上の一覧も更新する。
        /// </summary>
        private static void AddWiredServices(IServiceCollection services, BackingStore existingStore)
        {
            // Phase7-Step3 W1: BackingStore（設定の唯一の実体）を登録する。
            // Phase7-Step10d W5: 旧 Global.store（静的）をやめた。ブートストラップの BackingStore があればそれを、なければコンテナが作る。
            // IProcessInspector はプロパティで設定する（プロファイル読込の「起動するプログラム」の稼働確認で使う。BackingStore を
            // コンテナより前に作る場合があるため、コンストラクターでは渡せない）。
            services.AddSingleton<BackingStore>(sp =>
            {
                var store = existingStore ?? new BackingStore(sp.GetRequiredService<IEnvironmentService>());
                store.ProcessInspector = sp.GetRequiredService<IProcessInspector>();
                // Phase7-Step7 W2（Q4＝A）: Actions.xml の読み込み後の実行時状態のリセット。ブートストラップの BackingStore はコンテナより長く生きるため、
                // コンテナが作り直されても購読が 1 つになるよう、前回の購読者を外してから登録する（Phase7-Step8 W3: instance の購読者）
                // Phase14c-Step6（R-13）: 前回の購読者は BackingStore 自身が覚える（旧: このクラスの静的フィールド _subscribedReset）
                var reset = sp.GetRequiredService<ActionsLoadedRuntimeReset>();
                store.ReplaceActionsLoadedHandler(reset.Reset);
                return store;
            });

            // Phase10-Step4d W1: プロファイルの固定 ID と名前（ファイル名）を結ぶ登録簿
            // W5b: 登録簿を作るとき、BackingStore のスロットの保管（ProfileSlotNames）へ結ぶ（ID ↔ 名前を引く）
            services.AddSingleton<IProfileIdentityService>(sp =>
            {
                var service = new ProfileIdentityService(sp.GetRequiredService<IPathService>());
                sp.GetRequiredService<BackingStore>().AttachProfileIdentity(service);
                (sp.GetRequiredService<IProfileSettingsService>() as ProfileSettingsService)?.AttachProfileIdentity(service);   // W5c: 一時プロファイルの名前
                return service;
            });

            // Phase13-Step7 7-b: 出力種別の 2 本の配列の持ち主（依存なし）。BackingStore へは、本サービスの生成時に ActiveOutDevType の同じ配列を渡す
            // （旧: IOutputSlotService の登録ラムダで渡していた）。BackingStore の登録側から IOutputSlotService を解決してはならない
            // （IOutputSlotStore → IPathService → BackingStore と循環し、実行時にスタックオーバーフローになる）が、このホルダーは依存がなく、循環しない。
            services.AddSingleton<IOutputDeviceTypeState>(sp =>
            {
                var state = new OutputDeviceTypeState();
                sp.GetRequiredService<BackingStore>().ActiveOutDevType = state.ActiveOutDevType;
                return state;
            });
        }
    }
}
