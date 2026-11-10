using DS4Windows.Output.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Composition
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Phase14c-Step6（R-18）: 第3層（<c>Output/</c>）の実装の登録。
        /// 出力種別の配列の持ち主 <c>IOutputDeviceTypeState</c> は、生成のときに <c>BackingStore</c> へ配列を渡すため <c>AddWiredServices</c>（ServiceRegistration.Wiring.cs）にある。
        /// </summary>
        private static void AddOutputServices(IServiceCollection services)
        {
            // Phase 5 Step 12: 出力スロット永続化・管理サービス（永続化の IOutputSlotStore は ServiceRegistration.Core.cs）
            // Phase6-Step10-4-3: スロット管理の実体は ControlService が所有する。循環を避けるため、遅延で取得する
            // Phase7-Step6 W1: 出力種別の配列は OutputSlotService が持ち、BackingStore へは本サービスの生成時に渡す。
            // BackingStore の登録側から IOutputSlotService を解決してはならない（IOutputSlotStore → IPathService → BackingStore と循環し、
            // 実行時にスタックオーバーフローになる）。配列を書き込むのは ControlService だけで、IOutputSlotService を通すため、書き込みの前に必ず接続済みになる。
            services.AddSingleton<IOutputSlotService>(sp =>
            {
                // Phase13-Step7 7-d: スロット管理の実体は VirtualPadService が持つ（Phase9 E-4）。旧: 遅延取得のデリゲート 2 本は直接注入にした
                return new OutputSlotService(
                    sp.GetRequiredService<IVirtualPadService>(),
                    sp.GetRequiredService<IOutputSlotStore>(),
                    sp.GetRequiredService<IOutputDeviceTypeState>());   // BackingStore への配線は IOutputDeviceTypeState の登録が行う
            });

            // Phase7-Step8 W3（Q6＝A）: 出力ハンドラの実体を持つ OutputKBMHandlerAdapter を 1 つ登録し、IVirtualKBM（送出）と IVirtualKBMLifecycle（決定・破棄）が共有する
            services.AddSingleton<OutputKBMHandlerAdapter>();
            services.AddSingleton<IVirtualKBM>(sp => sp.GetRequiredService<OutputKBMHandlerAdapter>());
            services.AddSingleton<IVirtualKBMLifecycle, OutputKBMHandlerLifecycle>();

            services.AddSingleton<IOscTransportFactory, SharpOscTransportFactory>();   // Phase9 9-E E-1: OSC の通信（SharpOSC）の生成
            services.AddSingleton<IOscService, OscService>();   // Phase9 9-E E-1: OSC（受信の解釈、送信、入力への重ね合わせ、変化の送信）
            services.AddSingleton<IHidHideApiFactory, HidHideApiFactory>();   // Phase9 9-E E-2: HidHide ドライバーの窓口の生成
            services.AddSingleton<IHidHideService, HidHideService>();   // Phase9 9-E E-2: HidHide（ホワイトリストの確認、影響を受けるデバイスの判定）
            services.AddSingleton<IOutputSlotStore, OutputSlotStore>();   // OutputSlots.xml の永続化。出力スロットの型を使うため第 3 層に置く（IPathService だけに依存する）
            services.AddSingleton<IVirtualPadBackend, ViGEmVirtualPadBackend>();   // 仮想パッドのドライバー（ViGEmBus）の窓口。ViGEm の型を参照するのは Output/Backend だけ
            services.AddSingleton<IVirtualPadService, VirtualPadService>();   // Phase9 9-E E-4: 仮想パッド（ViGEm）の出力。OutputSlotManager、割り当て、振動・ライトバーの戻り
            // Phase13-Step7 7-d: 遅延取得デリゲート 2 本の登録は撤廃した（VirtualPadService ⇄ OutputSlotService の循環は、出力種別の配列を IOutputDeviceTypeState へ移して解消）
            // Phase 5 Step 10: UDP サーバーサービス（Cemuhook モーションサーバー境界化）
            // Phase9 9-E E-3: 旧 IUdpServerService／UdpServerService（未接続の並行実装）は削除した（ユーザー承認）。UDP の窓口は IUdpMotionService
            services.AddSingleton<IUdpMotionServerFactory, UdpMotionServerFactory>();   // Phase9 9-E E-3: UDP サーバー（UdpServer）の生成
            services.AddSingleton<IUdpMotionService, UdpMotionService>();   // Phase9 9-E E-3: UDP モーションの送出（サーバーの作成・開始・停止、ハンドラー、スムージング）
            services.AddSingleton<IDeviceRumbleService, DeviceRumbleService>();   // Phase13-Step3: ブースト・反転つきの振動
            services.AddSingleton<IOutputKbmInitializer, OutputKbmInitializer>();   // Phase13-Step3: KBM 出力の初期化
        }
    }
}
