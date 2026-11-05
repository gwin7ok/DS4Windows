using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの <see cref="KeyButtonActionController"/> の登録簿（生成・再利用・破棄）。
    /// 旧: <c>Mapping</c> の静的フィールド <c>keyButtonControllers</c>／<c>keyButtonControllerLock</c> と関連メソッド。
    /// <c>Mapping</c>、<c>ProfileApplicationService</c>、<c>ActionManager</c>（静的ファサード）が使う。
    /// 実装は <c>DS4Windows.Processing.Services.KeyButtonControllerRegistry</c>。
    /// </summary>
    public interface IKeyButtonControllerRegistry
    {
        /// <summary>モード指定でコントローラーを得る（無ければ生成。アクション名が null のときは "&lt;mapping&gt;"）。</summary>
        KeyButtonActionController GetOrCreate(int device, KeyButtonActionController.Mode mode, string actionName = null);

        /// <summary>SpecialAction 指定でコントローラーを得る（無ければ生成）。</summary>
        KeyButtonActionController GetOrCreate(int device, SpecialAction sa);

        /// <summary>コントローラーを得て、<c>IActionController</c> のアダプターで包んで返す（旧: <c>Mapping.GetOrCreateKeyButtonControllerForAction</c>）。</summary>
        DS4Windows.Processing.Actions.IActionController GetOrCreateForAction(int device, SpecialAction sa);

        /// <summary>指定デバイスのコントローラーをすべて破棄して登録簿から外す（旧: <c>Mapping.ClearKeyButtonControllersForDevice</c>）。</summary>
        void ClearForDevice(int device);

        /// <summary>Phase14c-Step3（R-06）: キーの操作の部品（<c>KeyButtonActionController</c>、<c>KeyAction</c>）が使う合成キーの送出（旧: 静的な橋渡し <c>SyntheticDispatcher</c>）。</summary>
        ISyntheticDispatcher Dispatcher { get; }
    }
}
