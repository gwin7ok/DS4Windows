using System;

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
    }
}
