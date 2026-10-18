using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step6): GetControllersForDevice は本体の呼び出し元 0 件（他のメンバーは KeyButtonControllerRegistry などが利用中）。
    // 使うべきメンバー: コントローラー接続時の TriggerBinding の紐付け（Step6）で、デバイスごとのコントローラー一覧が必要になる見込み。不要と分かった場合は Step6 で削除する。
    public interface IControllerRegistry
    {
        void Register(string key, IDisposable controller);
        void Unregister(string key);
        IReadOnlyList<IDisposable> GetControllersForDevice(int device);
        void ClearControllersForDevice(int device);
    }
}
