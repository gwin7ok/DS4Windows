# Phase29 計画書: 毎フレームの経路の安全弁と割り当ての是正（コードレビュー 2026-11 の 6-7・6-8・6-2・6-1・6-9・1-1、追加の 6-10・6-11・6-12）

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

追加調査（2026-11-11）で分かったこと:
- `TouchpadMouse.IsGyroTriggerActive`（`Processing/Touchpad/TouchpadMouse.cs:284`〜`340`）: 動きのフレームのたびに、トリガーの設定の文字列を `Split(',')` し、`int.TryParse` する。ジャイロの出力の既定は Controls（`Core/Settings/BackingStore.cs:121`）なので、ジャイロを持つ機種では、既定の設定で毎フレーム起きる。
- 動き・タッチのイベント引数（`Input/DS4SixAxis.cs:198`、`Input/Devices/SwitchProDevice.cs:525`、`Input/DS4Touchpad.cs:105`〜`240`、`Input/TouchpadEventArgs.cs:42`・`48`）: 毎フレーム、クラスのオブジェクト（と配列）を作る。
- `MainWindow` の 2 つのタイマー（`UI/MainWindow.xaml.cs:593`〜`615`、`754`〜`764`）: 冒頭で止め、末尾で始め直すが、`try`／`finally` がない。例外が 1 回出ると、止まったままになる。`HotkeysTimer_Elapsed` は、`ControllerCol` をロックなしで列挙する（同 `599`。コレクションは `ControllerListViewModel` の `_colListLocker` の下で変わる）。
- `TriggerDispatchEngine` は、機器ごとのロックと、不変の配列の差し替えで作られており、問題がなかった。29-4 の D2 の (A) は、これと同じ形である。

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
| D4 | 29-9（イベント引数の使い回し）を行うか | (A) 29-9a の調査の結果を見てから決める（保持している箇所が少なければ行う）／(B) 行う／(C) 見送る（上流から続く形で、動作への影響は GC の頻度だけ） |
| D5 | 29-10 で足す Error のログを、利用者向け（画面・トレイ）にも出すか | (A) ファイルだけ（開発者向け）／(B) 利用者向けにも出す（`AppLogger.LogUser` とリソースのキーを足す） |

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
| 29-7 | 文書: `01-Structure.md` §4 の「割り当て 0」の記述に、検査しているテストの名前を足す。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ（29-8〜29-10 のあとに行う） | — | 未着手 |
| 29-8 | （追加。6-10）ジャイロのトリガーの判定（`TouchpadMouse.IsGyroTriggerActive`）の、毎フレームの `Split`／`TryParse` をやめる。設定の入れ物（`GyroControlsInfo.triggers`、`Gyro.MouseTriggers`、`Gyro.MouseStickTriggers`）が、値を入れたときに数の配列へ直して持ち、判定は配列を読む。保存する文字列・判定の結果は変えない（空文字・数でない要素の扱いも今のまま） | 文字列の全パターン（`"-1"`、`"4"`、`"4,5"`、空、数でない要素）で、変更の前後の判定が同じ。割り当て 0 | 未着手 |
| 29-9a | （追加。6-11）調査: 動き・タッチのイベント（`SixAccelMoved`、`TouchesBegan`／`TouchesMoved`／`TouchesEnded`、`TouchButtonDown`／`Up` など）の購読者をすべて挙げ、引数や `Touch` を、ハンドラーの外まで保持している箇所を洗い出す（例: `TouchpadMouse` の `firstTouch`）。D4 の方式を確定する | — | 未着手 |
| 29-9 | （追加。6-11）動き・タッチのイベント引数を、機器ごとに 1 つ持って使い回す（`DS4SixAxis.cs:198`、`SwitchProDevice.cs:525`、`DS4Touchpad.cs:105`〜`240`、`TouchpadEventArgs.cs:42`・`48`）。保持していた側は、値を写して持つ形に直す | タッチ・ジャイロの既存のテストが不変。動きのフレームと、触れている間のフレームで割り当て 0 | 未着手 |
| 29-10 | （追加。6-12）`MainWindow` の 2 つのタイマー（`HotkeysTimer_Elapsed`、`AutoProfilesTimer_Elapsed`）を `try`／`finally` で囲み、例外のあとも始め直す。例外は Error のログに出す（新しいログ。開発者向けの診断文として英語で書く）。`HotkeysTimer_Elapsed` の `ControllerCol` の列挙は、読み取りロックの下で取ったスナップショット（スロット番号と行の組）に対して行う。判断の部分は、WPF なしでテストできる部品へ出す（Phase26 の進め方） | 偽の処理が例外を投げたあとも、次の回が呼ばれること。列挙と行の追加・削除を並行に回しても例外が出ないこと | 未着手 |

## 確認の方法
- 各 Step: `dotnet test … -c Debug /p:platform=x64 --filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（警告 0・エラー 0）。
- 実機: DS4 で、キーの割り当て・トグル・マウスのクリック・特殊アクション（プロファイル切替を含む）・アンチスナップバックが従来どおり動くこと。未確認の項目は `Known-Issues-Ledger.md` §3 へ足す。
- 性能を比べるときだけ、Benchmark の印のテストを前後で流す。

## リスク
- 29-1: 低。`finally` で必ず解放されるようになるだけ。例外がこれまで握りつぶされていた場合、上へ伝わる先が変わらないかを確かめる（`InputReportProcessor.OnReport` の呼び出し元）。
- 29-4: 中。競合の守り方が変わる。29-4a の調査の結果しだいで、方式を変えるか、見送る。
- 29-5: 低。生成の順序だけが変わる。`ExposedState` が同じ参照を見ることをテストで固定する。
- 29-8: 低〜中。文字列の解釈（空、数でない要素）を、今と 1 つも変えないことをテストで固定する。設定の入れ物は UI スレッドが書き、入力スレッドが読むため、配列は差し替え（不変）にする。
- 29-9: 中。使い回す引数を、購読者が保持していると、あとのフレームの値に化ける。29-9a の洗い出しが前提。Switch Pro は実機がない。
- 29-10: 低。始め直しが必ず行われるようになるだけ。終了のとき（`MainWindow.xaml.cs:1038` の `Stop`）に、`finally` が始め直してしまわないよう、止める印を見る。

## 実機確認の項目（追加）
- ジャイロ: 出力が Controls・Mouse・MouseJoystick のそれぞれで、トリガーなし・1 つ・複数（AND／OR）・切り替え（トグル）の動作が従来どおり。
- タッチパッド: マウス移動、タップ、ダブルタップ、スクロール、スワイプでのプロファイル切替（2 本指）、タッチボタン。
- コントローラーを抜き差ししながら、スワイプ切替と自動プロファイルが動き続けること。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-11: 追加調査の結果を反映。Step 29-8（ジャイロのトリガーの判定）、29-9a・29-9（イベント引数）、29-10（タイマーの安全弁）と、決めること D4・D5 を足した。進める順は、29-1 → 29-10 → 29-2 → 29-3 → 29-8 → 29-5 → 29-4a・29-4 → 29-9a・29-9 → 29-6 → 29-7（安全弁を先に、リスクの低いものから）。
