# Phase36 計画書: `TouchpadMouse` の分割

作成日: 2026-11-12
状態: **未着手。承認待ち**
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 3、優先 1）、`02-Layers-and-Placement.md` §3、`06-Performance.md` の 6-10
前提: Phase29 の 29-8（ジャイロのトリガーの判定）と 29-9（動き・タッチのイベント引数）のあとに行う。同じファイルを 2 回触らないため。

## 目的
`Processing/Touchpad/TouchpadMouse.cs`（1,810 行）を、受け持つ仕事ごとの型に分け、それぞれを単体でテストできるようにする。動作と、毎フレームの割り当ての量は変えない。

## 現状（調査。2026-11-12 の実測）
- 1 つの型が、スロットごとに 1 つ作られ（`TouchpadMouseFactory`）、タッチパッドのイベント（`touchesBegan`／`touchesMoved`／`touchesEnded`／`touchUnchanged`、タッチボタン）と、**ジャイロのイベント**（`sixaxisMoved`）の両方を受ける。名前はタッチパッドだが、ジャイロの出力（マウス、マウススティック、Controls のトリガーの判定）も持つ。
- メソッド 30 個。長い順に、`TouchpadMouseStick` 297 行（タッチ → スティック）、`touchesEnded` 227 行（タップ・ダブルタップ・スワイプの判定）、`SixMouseStick` 209 行（ジャイロ → スティック）、`TouchButtonCheckProcess` 134 行、`touchUnchanged` 122 行、`touchesMoved` 110 行、`StartTapClickHold` 87 行、`ResetToggleGyroModes` 80 行。
- すでに別の型になっているもの: カーソルの移動（`MouseCursor` 445 行）、ホイール（`MouseWheel`）、トラックボールの慣性（`FakeTrackball`）。
- 実装するインターフェース: `ITouchpadBehaviour`（実装はこの型だけ）、`ITouchPadGyroToggles`。
- 設定の読み取りは、領域の窓口（`_profileSettings.ForSlot(deviceNum)`）を毎回たどる（55 か所）。
- テスト: `TouchpadMouse` に触れるテストのファイルは 14。
- 毎フレームの経路である（動きのフレームごと、触れている間のフレームごと）。未調査のまま残っている範囲（タッチの各出力の計算の中身）を含む。

## 方針（決定事項の案）
1. **特性化テストを先に足す**（36-0）。入力の列（タッチの始まり・移動・終わり、ジャイロの値、設定）に対して、出力（マウスの移動量、クリック、スティックの値、切り替えの状態）を固定する。
2. **仕事ごとに型を切り出す**。`TouchpadMouse` は、イベントの受け口と、出力の種類による振り分けだけを残す。切り出す先の案:
   - ジャイロの出力（`sixaxisMoved` の中身、`IsGyroTriggerActive`、`SixMouseStick`、`SixMouseReset`、切り替えの状態）→ `Processing/Gyro` の型。
   - タッチ → スティック（`TouchpadMouseStick`）→ `Processing/Touchpad` の型。
   - タップ・クリック・押し続けの判定（`touchesEnded` の判定の部分、`StartTapClickHold`）→ 同上。
   - タッチボタン（`TouchButtonCheckProcess`）→ 同上。
3. **設計の型（State／Strategy）を先に決めない**。まず長いメソッドを、状態を引数と戻り値で受け渡す部品へ出し、形が見えてから、出力の種類ごとの差し替え（`ITouchpadBehaviour` を出力の種類ごとの実装にする）にするかを決める（D2）。
4. 毎フレームの経路なので、割り当て 0 を保つ（Phase29 のあとの状態を基準にする）。切り出した型は、同じ層の中だけで使う計算の部品なら、インターフェースを作らず具象で持つ（`04-Duplication-and-Abstraction.md` の 4-5）。
5. 時間は `TimeProvider`（すでに受けている）を使い、テストは実時間で待たない。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 範囲 | (A) ジャイロの出力の切り出し（36-1）と、長いメソッド 3 つ（36-2〜36-4）まで／(B) 全部（36-5 を含む） |
| D2 | 出力の種類ごとの差し替え（`ITouchpadBehaviour` を、マウス・Controls・スティック・パススルーごとの実装にする） | (A) 36-1〜36-4 のあとに、形を見て決める／(B) 最初から行う／(C) 行わない（その場合、実装が 1 つの `ITouchpadBehaviour` を残すかどうかは、規約 2.4 の手順で判断する） |
| D3 | ジャイロの出力を切り出したあとの、型の名前 | (A) `TouchpadMouse` のまま（ジャイロは別の型へ出るので、名前が実体に合う）／(B) 改名する |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 36-0 | 特性化テスト: 出力の種類（マウス、Controls、マウススティック、パススルー）と、ジャイロの出力（Controls、マウス、マウススティック）ごとに、入力の列と出力を固定する。未調査だった計算の中身を読み、毎フレームの割り当てがあれば記録する | 新規（仕事ごと） | 未着手 |
| 36-1 | ジャイロの出力を別の型へ（`sixaxisMoved` の中身、トリガーの判定、`SixMouseStick`、切り替えの状態）。購読（`DeviceConnectionSetup.cs:199`）は、新しい型へ向ける | 36-0 が不変。割り当て 0 | 未着手 |
| 36-2 | タッチ → スティック（`TouchpadMouseStick` 297 行）を別の型へ。フィルター（`OneEuroFilterPair`）とトラックボールの状態の持ち主を決める | 同上 | 未着手 |
| 36-3 | タップ・ダブルタップ・押し続けの判定（`touchesEnded`、`StartTapClickHold`）を別の型へ。プロファイルのスワイプ切替の合図（`slideleft`／`slideright`）の出し方は変えない | 同上。タップの時間の境界（感度 × 1.5 ミリ秒）を固定する | 未着手 |
| 36-4 | タッチボタン（`TouchButtonCheckProcess` 134 行）を別の型へ | 同上 | 未着手 |
| 36-5 | （D1 が B のとき）`touchesMoved`・`touchUnchanged` の、出力の種類ごとの枝を整理する。D2 の差し替えを行う場合は、ここで行う | 同上 | 未着手 |
| 36-6 | 設定の読み取りを、フレームの先頭で 1 回、領域の窓口（`TouchpadSlotSettings`、`GyroSlotSettings`）を取ってから使う形にそろえる（`ForSlot(deviceNum)` の 55 か所） | 36-0 が不変 | 未着手 |
| 36-7 | 文書: `01-Structure.md`（型の表）、`Code-Review-2026-11/00-Summary.md` の未調査の行を更新 | `ModelDiagramGuardTests` | 未着手 |

## 確認の方法
- 各 Step のあと、特性化テスト（36-0）と割り当て 0 のテスト。性能を比べるときは Benchmark の印のテストを前後で流す。
- 実機（DS4）: タッチパッドのマウス移動、タップ、ダブルタップ、ドラッグ、スクロール、トラックボール、2 本指のスワイプでのプロファイル切替、タッチボタン（左・右・上・複数）、タッチ → スティック。ジャイロの Controls・マウス・マウススティックと、トリガー（なし・1 つ・複数・切り替え）。

## リスク
- 中。毎フレームの経路で、状態（前のフレームの値、タップの時刻、切り替え）が多い。型を分けるときに、状態の持ち主を取り違えると、タップが効かない・カーソルが飛ぶ形で現れる。特性化テストが前提。
- DualSense・Switch Pro のジャイロとタッチは、実機で確かめられない。

## 履歴
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。
