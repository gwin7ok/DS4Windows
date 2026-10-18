using System.Collections.Generic;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step2/Step5〜6): 使うべき型。実装クラスが存在せず（静的な ActionRegistry は SpecialAction の一覧を持つ別物）、Register／GetBindingsForDevice の呼び出し元は 0 件。
    // Step2 で実装の形（DI 登録するか）を設計し、Step5（プロファイル読込時の TriggerBinding の登録）と Step6（コントローラー接続時の紐付け）で接続する。
    /// <summary>
    /// Registry for bindings and controllers per device.
    /// </summary>
    public interface IActionRegistry
    {
        void Register(IActionBinding binding, int device);
        void Unregister(IActionBinding binding, int device);
        IEnumerable<IActionBinding> GetBindingsForDevice(int device);
    }
}
