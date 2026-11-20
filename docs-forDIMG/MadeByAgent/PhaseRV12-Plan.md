# PhaseRV12 計画書: 型そのものの分割（責務ごとに別の型へ）

作成日: 2026-11-21
状態: **D1＝(A) 第 2 層と設定・永続化、D2＝(A) 同層の internal 具象、D3＝(B) 層ごとに 1 コミット、D4＝(A) 実機確認は後回し（§9i へ追加）で決定（2026-11-21）。着手前**
出典: `docs-forDIMG/Code-Review-2026-11-19/02-Scorecard.md` の RV11b（`partial` を束ねた計測では、500 行以上は 33 件のまま。RV11 の分割は責務を分けていない）。`PhaseRV11-Plan.md`（D2＝(A) からの逸脱の回収）。
動き: **機能・保存の形・ログは変えない**。`partial` に分けた責務を、別の型（コンストラクターで受ける協力者）へ切り出し、呼び出し側を直す。

## 目的
1. 計測（型の単位。`partial` を束ねる）で、500 行以上の型を 33 → 目標 15 前後へ減らす。
2. 1 つの型が持つ責務を減らし、テストで協力者を差し替えられるようにする（`partial` の分け目は、そのまま切り出しの候補）。

## 前提（調べた事実、2026-11-21）
- 500 行以上の型は 33 件（束ねた行数）。上位: `ProfileEditor` 2,036、`MainWindow` 1,837、`InputDeviceCore` 1,193、`AppSettingsDTO` 1,185（XML の順序のため対象外）、`DualSenseDevice` 1,137、`SwitchProDevice` 1,038、`RecordBox` 1,000、`BackingStore` 985、`App` 985、`Mapping` 938。
- RV11 の `partial` の分け目（`Class.Group.cs`）が、切り出しの単位の候補。ただし `partial` は同じ型のフィールドを共有するため、切り出しには「共有する状態を何で渡すか」の設計が要る。
- 規約: コンストラクターの引数は 12 以下（`ConstructorArityBaseline`）。同じ層で状態を持たない計算の部品は、インターフェースなしの具象でよい（§3.1）。1 ファイル＝1 型（§3.3）。

## 切り出しの型（パターン）
| 型 | 切り出し先 | 例 |
|---|---|---|
| P1 状態を持たない計算 | `static` でなく、注入する具象（または `internal` の純粋な関数の型） | `Mapping.Curves`（曲線の計算）、`ControlBindingBuilder.Interpret` |
| P2 状態を持つ協力者 | 状態ごと別の型へ移し、本体はコンストラクターで受ける | `TriggerDispatchEngine.Runtime`、`TouchpadMouse.Touches`、`BackingStore` の領域ごとの読み書き |
| P3 画面の見た目の処理 | 見た目の部品（`UserControl`）か、ViewModel（業務）へ寄せる | `ProfileEditor.Visualizer`、`MainWindow.Devices` |
| P4 機種の骨組み | `InputLoopDevice` の差し込み口への移動、`Parsers`／`Reports` | `InputDeviceCore.Output`、`DualSenseDevice.Output` |

## 決定してほしい事項

### D1 範囲
- **(A) 第 2 層と設定・永続化の 6〜8 型を先に（推奨）**。`Mapping`、`TriggerDispatchEngine`、`TouchpadMouse`、`BackingStore`、`AppSettingsService`、`ProfilePresets`、`GyroOutputProcessor`、`ControlBindingBuilder`。テストがあり、実機確認が少なく済む。
- (B) 画面（`ProfileEditor`、`MainWindow`、`RecordBox` など）も含める。行数は最も効くが、XAML・イベント・ViewModel の整理を伴い、差分が大きい。
- (C) 入力機種（`InputDeviceCore` など）も含める。実機確認の項目が増える。

### D2 切り出し先の置き方
- **(A) 同じ層・同じフォルダーの別の型（`internal`、注入する具象）（推奨）**。新しい公開の契約を増やさない。
- (B) 契約（インターフェース）も足す。テストの差し替えはしやすいが、§3.1 の基準では要らない型まで増える。

### D3 順序と確認
- **(A) 型ごとに 1 コミット。各コミットでビルド警告 0・全テスト（推奨）**。戻しやすい。
- (B) 層ごとにまとめて 1 コミット（RV11 と同じ）。

### D4 実機確認
- **(A) 第 2 層・設定は、既存のテスト（黄金テスト・往復テスト）で足りるとし、実機確認は §9i に項目を足して後回し（推奨）**。
- (B) 画面・入力機種に触れる Step の直後に、1 項目ずつ実機確認を挟む。

## Step（D1＝(A)、D2＝(A)、D3＝(A)、D4＝(A) の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV12-0 | 切り出し先の候補の一覧と、共有する状態の洗い出し（型ごとに、`partial` の分け目、共有フィールド、コンストラクターの引数の増え方）。足りない挙動の固定テストを足す | Debug ALL |
| RV12-1 | `Mapping`: 曲線・スペシャルアクションの計算を別の型へ（P1） | 同上 |
| RV12-2 | `TriggerDispatchEngine`、`TouchpadMouse`、`GyroOutputProcessor`（P2） | 同上 |
| RV12-3 | `ControlBindingBuilder`、`KeyButtonActionController`（P1） | 同上 |
| RV12-4 | `BackingStore`・`AppSettingsService`・`ProfilePresets`（領域ごとの協力者。`IAppSettingsService` の契約は変えない） | 同上＋往復テスト |
| RV12-5 | 評価の再算定（束ねた計測で、500 行以上 33→目標 25 前後） | `Score-CodeReview.py` |

## 守るもの
機能・保存の形（XML）・ログの文言・`IAppSettingsService` などの公開の契約・各ラチェット（`ConstructorArityBaseline` は増やさない）・1 ファイル＝1 型・名前空間＝フォルダー。

## リスク
- `partial` が共有しているフィールド（ロック、キャッシュ）の持ち主を移すと、スレッドの直列化（ガードレール）を壊しうる。RV12-0 で、共有する状態とロックの一覧を作ってから着手する。
- 引数の数が増える型は、まとめ方（束ねるだけの「何でも箱」は禁止）を RV12-0 で決める。

## 変更履歴
- 2026-11-21: 作成（D1〜D4 の決定待ち）。
- 2026-11-21: D1＝(A)、D2＝(A)、D3＝(B)、D4＝(A) を、ユーザーが選択。D3 は推奨の (A) と異なり、層ごとに 1 コミット（RV12-1〜3 を第 2 層、RV12-4 を設定・永続化で、それぞれ 1 コミット）。
