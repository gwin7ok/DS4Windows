#nullable enable

using System;
using System.Collections;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 画面（UI）のスレッドへ処理を渡す窓口。ViewModel が <c>Application.Current.Dispatcher</c> を直接使わず、
    /// WPF なしでテストできるようにするための契約（テストでは、その場で実行する偽物を渡す）。
    /// </summary>
    public interface IUiDispatcher
    {
        /// <summary>呼び出したスレッドが UI のスレッドなら true。</summary>
        bool CheckAccess();

        /// <summary>UI のスレッドで実行し、終わるまで待つ（UI のスレッドから呼べば、その場で実行する）。</summary>
        void Invoke(Action action);

        /// <summary>UI のスレッドでの実行を依頼し、待たずに戻る。</summary>
        void BeginInvoke(Action action);

        /// <summary>UI のスレッドが手すきになってから実行するよう依頼し、待たずに戻る（画面の更新が終わったあとに行いたい処理用）。</summary>
        void BeginInvokeIdle(Action action);

        /// <summary>別のスレッドからの変更を許すよう、コレクションを <paramref name="lockObject"/> のロックで同期する（画面のスレッドから呼ぶ）。</summary>
        void EnableCollectionSynchronization(IEnumerable collection, object lockObject);

        /// <summary>コレクションの読み書きを <paramref name="accessCallback"/> 経由で行わせる。引数は、実行する処理と、書き込みかどうか。</summary>
        void EnableCollectionSynchronization(IEnumerable collection, object context, Action<Action, bool> accessCallback);
    }
}
