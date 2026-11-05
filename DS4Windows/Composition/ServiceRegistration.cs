using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Settings;

namespace DS4Windows.Composition
{
    /// <summary>
    /// Phase 3 / Phase 4 / Phase 5: アプリケーション全体のDIコンテナ初期登録を一元管理する。
    /// Phase14c-Step6（R-18）: 登録は、実装クラスの置き場所（層のフォルダ）ごとの partial ファイルに分けた。
    /// <list type="bullet">
    /// <item><c>ServiceRegistration.Wiring.cs</c>: 生成のときに別のオブジェクトへ結ぶ（後付けの配線をする）登録。後付けの配線はすべてここに置く（R-13）</item>
    /// <item><c>ServiceRegistration.Core.cs</c>: 横断基盤・永続化（<c>Core/</c>）の実装</item>
    /// <item><c>ServiceRegistration.Processing.cs</c>: 第2層（<c>Processing/</c>）の実装。入力機種の部品（<c>IInputDeviceModule</c>）もここ</item>
    /// <item><c>ServiceRegistration.Output.cs</c>: 第3層（<c>Output/</c>）の実装</item>
    /// <item><c>ServiceRegistration.Ui.cs</c>: 4-a（<c>UI/</c>）の実装と、App へ委譲するアダプター（<c>Composition/</c>）</item>
    /// </list>
    /// 登録の順序が結果を変えるのは、同じサービス型を複数登録する <c>IInputDeviceModule</c> の並び（解決すると登録順の列になる）だけ。
    /// その並びは <c>ServiceRegistration.Processing.cs</c> の中で、元の順序のまま保つ。
    /// </summary>
    public static partial class ServiceRegistration
    {
        // Phase7-Step10d W5（Q3＝A）: existingStore は、コンテナの構築より前に作った BackingStore（本体の起動のブートストラップ。起動直後の設定の読み取りが使う）。
        // 省略（null）のときは、コンテナが BackingStore を作る（テストなど）。どちらも、IProcessInspector の設定と ActionsLoaded の購読は、この登録が行う
        // （Phase14c-Step6: ServiceRegistration.Wiring.cs の AddWiredServices）。
        public static IServiceCollection RegisterServices(this IServiceCollection services, BackingStore existingStore = null)
        {
            AddWiredServices(services, existingStore);
            AddCoreServices(services);
            AddProcessingServices(services);
            AddOutputServices(services);
            AddUiServices(services);
            return services;
        }
    }
}
