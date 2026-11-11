# Phase29 計画書: 毎フレームの経路の安全弁と割り当ての是正（コードレビュー 2026-11 の 6-7・6-8・6-2・6-1・6-9・1-1）

作成日: 2026-11-11
状態: **未着手。承認待ち**（下の「決めること」を確認してから着手する）
出典: `docs-forDIMG/Code-Review-2026-11/06-Performance.md`、`01-DI.md`

## 目的
入力のたびに動く経路（機器 1 台あたり 1 秒に約 250 回）にある、安全弁の抜けと、割り当て（アロケーション）を直す。動作・ログの文言・保存の形は変えない。規模が小さく、リスクが低いものを最初の Phase にまとめた。

## 現状（調査）
- `SyntheticInputCommitter.Commit`（`Processing/Services/SyntheticInputCommitter.cs:43`〜`205`）: 全コントローラー共通の書き込みロックを取り、`try`／`finally` なしで約 160 行を実行して解放する。間にキーボード・マウスの送出がある。例外が出ると解放されず、全コントローラーの入力が止まる。ロックを取る場所は、本体でここだけ。
- `DefaultActionManager.SetToggledOn`（`Processing/Actions/DefaultActionManager.cs:222`〜`242`）: 値が変わるたびに、ログのレベルを見ずに `StackTrace` を取り、文字列にする。
- `StickInputConditioner.AntiSnapback`（`Processing/Services/StickInputConditioner.cs:93`）: `queue.Any(ラムダ)` で、毎フレーム、捕捉のオブジェクト・デリゲート・列挙子の箱詰めを割り当てる。
- `Mapping.MapCustomAction`（`Processing/Mapping.cs:692`）: 特殊アクションを持つプロファイルで、毎フレーム `ToArray()` する。
- `InputReportProcessor` のコンストラクター（`Processing/Services/InputReportProcessor.cs:92`〜`97`）が、`DeviceSlotState.CurrentStates` の中身を作る（生成順への隠れた依存）。
- Trace／Debug のログの文字列を、レベルの判定なしで組み立てる箇所（`SyntheticDispatcherService.cs:57`・`58`・`107`・`108`、`SyntheticInputCommitter.cs` 34 か所、`KeyButtonActionController.cs` 9 か所、`TouchpadMouse.cs:1114`〜`1167` 4 か所）。
- 割り当て 0 を検査する `ControlServiceHotPathAllocationTests` は、設定の getter だけを見ている。

## 方針（決定事項の案）
1. 1 Step ＝ 1 メソッド（または 1 ファイル）の、局所の置換にする（規則 3.2）。
2. ログの文言・レベル・出す条件は変えない。判定で囲むだけにする（規則 2.3）。
3. 各 Step で、直した経路に「割り当て 0」または「例外のあとも次のフレームが動く」テストを足す。テストの名前は、守る振る舞いの名前にする（規則 4 章）。
4. 29-4 だけは、UI スレッドとの競合の守り方を変えるため、先に調査（29-4a）を置く。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 29-1 で、ロックの範囲も狭めるか | (A) まず `try`／`finally` だけ（動作は不変）。範囲の見直しは 29-1b として調査から／(B) 同時に、共有の集計の更新だけに絞る |
| D2 | 29-4 の方式 | (A) 一覧の持ち主（`BackingStore.profileActions`）が、変更のたびに不変の配列を作り直し、読む側は参照を読むだけ／(B) スロットごとの使い回しのバッファーへ毎フレーム写す（割り当ては 0 だが、写しは残る） |
| D3 | 29-6（ログの判定）の範囲 | (A) 上の 4 ファイルだけ／(B) `Processing` 全体を走査して一括 |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 29-1 | `SyntheticInputCommitter.Commit` の、ロックの取得から解放までを `try`／`finally` で囲む（中の処理は 1 行も変えない） | 偽の `IVirtualKBM` が例外を投げたあと、別の機器の `Commit` が戻ること（時間切れつき。Slow の印は付けない長さにする） | 未着手 |
| 29-1b | （D1 が A のとき）ロックの範囲の調査。`GlobalState` を読む・書く場所を洗い出し、送出を排他の外へ出せるかを判断して、追補の Step を決める | — | 未着手 |
| 29-2 | `DefaultActionManager.SetToggledOn` の、スタックトレースの取得からログまでを `if (AppLogger.IsTraceEnabled)` で囲む | Trace が無効のとき、`SetToggledOn` の切り替えで割り当てが一定以下であること | 未着手 |
| 29-3 | `StickInputConditioner.AntiSnapback` の `queue.Any(…)` を `foreach` に書き直す（計算式は同じ） | 既存のアンチスナップバックのテストが不変。新たに割り当て 0 のテスト | 未着手 |
| 29-4a | 調査: `profileActions` を書き換える場所と、そのスレッドの洗い出し。D2 の方式を確定する | — | 未着手 |
| 29-4 | `Mapping.MapCustomAction` の `ToArray()` をやめる（D2 の方式） | 特殊アクションつきのプロファイルで `MapCustom` を回し、割り当て 0。UI スレッド相当の変更と並行に回しても例外が出ないこと | 未着手 |
| 29-5 | `DeviceSlotState` が、`CurrentStates` の中身を自分のコンストラクターで作る。`InputReportProcessor` は作らない（`ControllerStateExposed` の作成は、参照先ができたあとに行う順序を保つ） | `DeviceSlotState` だけを作っても、`CurrentStates[i]` が `null` でないこと | 未着手 |
| 29-6 | Trace／Debug のログの文字列の組み立てを、レベルの判定で囲む（D3 の範囲） | ログの文言の黄金テスト（あれば）が不変。なければ、代表の 1 か所に割り当てのテスト | 未着手 |
| 29-7 | 文書: `01-Structure.md` §4 の「割り当て 0」の記述に、検査しているテストの名前を足す。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ | — | 未着手 |

## 確認の方法
- 各 Step: `dotnet test … -c Debug /p:platform=x64 --filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（警告 0・エラー 0）。
- 実機: DS4 で、キーの割り当て・トグル・マウスのクリック・特殊アクション（プロファイル切替を含む）・アンチスナップバックが従来どおり動くこと。未確認の項目は `Known-Issues-Ledger.md` §3 へ足す。
- 性能を比べるときだけ、Benchmark の印のテストを前後で流す。

## リスク
- 29-1: 低。`finally` で必ず解放されるようになるだけ。例外がこれまで握りつぶされていた場合、上へ伝わる先が変わらないかを確かめる（`InputReportProcessor.OnReport` の呼び出し元）。
- 29-4: 中。競合の守り方が変わる。29-4a の調査の結果しだいで、方式を変えるか、見送る。
- 29-5: 低。生成の順序だけが変わる。`ExposedState` が同じ参照を見ることをテストで固定する。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
