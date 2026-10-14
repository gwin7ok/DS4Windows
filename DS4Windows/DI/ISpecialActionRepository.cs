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
        bool SaveActions();

        SpecialAction GetAction(string actionName);
        int GetActionIndex(string actionName);
        bool ActionExists(string actionName);

        bool AddAction(SpecialAction action);
        bool RemoveAction(string actionName);
        bool ReplaceAction(string oldActionName, SpecialAction newAction);

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
        /// <c>Global.GetAction</c> と同じ契約。<see cref="GetAction"/> は前後の空白を無視せず、見つからないと
        /// <c>null</c> を返すため、置換できない（Phase6-Step8 §0A.1-4）。
        /// </summary>
        SpecialAction GetActionOrPlaceholder(string actionName);

        // ---- Phase6-Step10-3-1: スペシャルアクション編集画面の Global 直接参照解消（Global への薄い委譲）----

        /// <summary>
        /// アクションを保存する（<c>Global.SaveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、<c>Actions.xml</c> への書き込み、読み直しと件数の検証、
        /// キー種別（<paramref name="mode"/>＝4）のランタイム状態のリセットまでを含む。
        /// メモリ上のリストを変えるだけの <see cref="AddAction"/>／<see cref="ReplaceAction"/> とは別物。
        /// </summary>
        /// <param name="mode">種別（1:マクロ、2:プログラム、3:プロファイル、4:キー、5:切断、6:バッテリー、7:複数アクション、8:ステアリング校正、9:ジャイロ校正、10:ボタン）</param>
        void SaveAction(string name, string controls, int mode, string details, bool edit,
            double delayTime = 0.0, string extras = "");

        /// <summary>
        /// アクションを削除して <c>Actions.xml</c> へ保存する（<c>Global.RemoveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、ファイルへの保存、読み直しと件数の検証を含む。
        /// メモリ上のリストから外すだけの <see cref="RemoveAction"/> とは別物。
        /// </summary>
        /// <returns>削除前に、その名前のアクションが存在していれば true</returns>
        bool RemoveActionAndSave(string actionName);

        event EventHandler ActionsChanged;
    }
}
