using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    public interface ISpecialActionRepository
    {
        string ActionsPath { get; }
        IReadOnlyList<SpecialAction> Actions { get; }
        List<SpecialAction> ActionList { get; }

        bool LoadActions();
        // Phase7-Step2 W7: SaveActions／GetAction／ActionExists は契約から外した（本体の呼び出しが 0 件。受け手が別の型だったため、
        // 名前の衝突で前回の走査から漏れていた）。保存は SaveAction／RemoveActionAndSave、取得は GetActionOrPlaceholder を使う。
        // 実装の GetAction／ActionExists は、RemoveActionAndSave の内部判定とテストの確認で使うため、クラスには残した。

        // Phase7-Step2 W5・W6: AddAction／ReplaceAction／RemoveAction／GetActionIndex は削除した。AddAction／ReplaceAction／RemoveAction は、
        // メモリ上のリストだけを変える並行実装で、本体の保存経路（SaveAction／RemoveActionAndSave＝Actions.xml への保存と読み直し）と別物だった。
        // GetActionIndex は、AddAction などの内部でだけ使われていた。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.4

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// Actions.xml を読み込めなかったときの復旧処理（<c>Global.CreateStdActions</c>）。
        /// 各プロファイルの ProfileActions に「Disconnect Controller」を追加し、アクションを読み込み直す。
        /// </summary>
        void CreateStandardActions();

        // ---- Phase6-Step8-1: ProfileEditor.xaml.cs の Global 直接参照解消（契約差の吸収）----
        /// <summary>
        /// 名前（前後の空白を無視、大文字小文字を無視）でアクションを取得する。見つからないときは
        /// <c>null</c> ではなく placeholder（<c>new SpecialAction("null", "null", "null", "null")</c>）を返す。
        /// <c>Global.GetAction</c> と同じ契約。<c>GetAction</c> は前後の空白を無視せず、見つからないと
        /// <c>null</c> を返すため、置換できない（Phase6-Step8 §0A.1-4）。
        /// </summary>
        SpecialAction GetActionOrPlaceholder(string actionName);

        // ---- Phase6-Step10-3-1: スペシャルアクション編集画面の Global 直接参照解消（Global への薄い委譲）----

        /// <summary>
        /// アクションを保存する（<c>Global.SaveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、<c>Actions.xml</c> への書き込み、読み直しと件数の検証、
        /// キー種別（<paramref name="mode"/>＝4）のランタイム状態のリセットまでを含む。
        /// </summary>
        /// <param name="mode">種別（1:マクロ、2:プログラム、3:プロファイル、4:キー、5:切断、6:バッテリー、7:複数アクション、8:ステアリング校正、9:ジャイロ校正、10:ボタン）</param>
        /// <param name="actionId">
        /// Phase10-Step4d W2: 保つ SA の固定 ID。名前の変更（古い名前を削除して作り直す経路）で、編集前の ID を引き継ぐために渡す。
        /// null なら、編集（<paramref name="edit"/>）は同じ名前の SA の ID を、新規は新しい ID を使う。
        /// </param>
        void SaveAction(string name, string controls, int mode, string details, bool edit,
            double delayTime = 0.0, string extras = "", string actionId = null);

        /// <summary>
        /// Phase10-Step6 I5a: 名前で探した SA に、Extras（<see cref="SpecialAction.outputExtras"/>。Controls の Extras と同じ書式）を設定して <c>Actions.xml</c> へ保存する。
        /// null／空白で付けない状態にする。<see cref="SaveAction"/> は SA を作り直すため、Extras を持たない。エディターは、SA の保存のあとにこれを呼ぶ。
        /// </summary>
        /// <returns>SA が見つかれば true</returns>
        bool SaveActionExtras(string actionName, string outputExtras);

        /// <summary>
        /// Phase10-Step6 後の Step5: 名前で探した Button の SA に、アナログな出力の強さ（0〜100%）を設定して <c>Actions.xml</c> へ保存する。
        /// <see cref="SaveAction"/> は SA を作り直すため、強さを持たない（既定の 100）。エディターは、SA の保存のあとにこれを呼ぶ。
        /// </summary>
        /// <returns>Button の SA が見つかれば true</returns>
        bool SaveActionAnalogStrength(string actionName, int strength);

        /// <summary>
        /// アクションを削除して <c>Actions.xml</c> へ保存する（<c>Global.RemoveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、ファイルへの保存、読み直しと件数の検証を含む。
        /// </summary>
        /// <returns>削除前に、その名前のアクションが存在していれば true</returns>
        bool RemoveActionAndSave(string actionName);

        // TODO(Phase8): 購読者が 0 件のまま維持している。実装は保存・削除のたびに正しく発火する。旧 ProfileActions.ActionsChanged も購読者がなかった（移行前から同じ）。
        // 画面の特殊アクション一覧の更新通知への接続は Phase8 で行う。Phase7-Step10e W4 で経緯を確認し、削除せず維持と判断した。
        event EventHandler ActionsChanged;
    }
}
