# Phase16-Step0 個別計画書: 物差しの整備（引数の検査と基準値）

作成日: 2026-11-10
状態: **草案（承認待ち）。承認を得るまで、コードは変更しない**
ブランチ: `Phase16`
親: `Phase16-Plan.md` §5 Step 16-0、§8、§9（決定 C7＝A、C9＝C）
規約: `.github/copilot-instructions.md` §3.1・§3.3・§4

## 1. 目的

以後の Step で「引数が減った」「依存が減った」を数値で示せるようにする。本体の挙動は変えない。

1. 引数の数をリフレクションで測るテストと、増える方向への歯止め（ラチェット）を入れる（C7＝A）。
2. 規約に努力目標を追記する（C7＝A）。
3. 基準値（引数、L2・L7、テストの組み立ての数、テストの時間）を記録する。
4. Step 16-6a の前提（`HidDevice` を偽物にできるか）を調べ、結果を記録する。

## 2. 事前の確認（2026-11-10）

- `HidDevice` は具象クラスで、`virtual` のメンバーがなく、`ReadFile`／`CancelIO` は非 `virtual`。**偽物にできない**。したがって差分テストの土台は、16-6a で `IHidTransport` を入れた後にしか作れない（親計画書 §5 の順序のとおり）。
- テストで `DS4Device` を直接作る箇所は 0 件（`new DS4Device(`・`performDs4Input` の参照なし）。`DS4Device` に触れるテストファイルは 32。
- 層の検査は `LayerDependencyScanner`（ArchUnitNET の `ArchLoader` で `typeof(AppHost).Assembly` を読む）が前例。本件はリフレクションで足りる。
- 手で組み立てる箇所（テスト）: `Mapping` 1／1 ファイル、`ServiceLifecycle` 2／1、`ActionBindingBuilder` 19／10、`SpecialActionOutputFactory` 18／10、`AutoProfileService` 11／2、`DeviceConnector`・`InputReportProcessor`・`ControlService` は 0（共有ホスト経由）。

## 3. 作業

### 3.1 テスト `ConstructorArityRuleTests`（`DS4WindowsTests/`、新規）と `ConstructorArityBaseline`（新規）

- 対象: `typeof(AppHost).Assembly` の、抽象でない `class`／`struct`／`record`。コンパイラー生成の型（`<>c`、ラムダの入れ物、`DisplayClass` など）と、ネスト型の生成物は除く。
- 数え方: 型ごとに、公開・内部・保護・非公開のインスタンス コンストラクターの引数の数の**最大**。主コンストラクターも同じ（リフレクション上は通常のコンストラクター）。
- 規則: 引数が **15 を超える型**は `ConstructorArityBaseline` の許可リストに「型の完全名|引数の数」で載せる。
  - 許可リストに載る型の数が実測と**一致**すること（増えても減っても失敗）。増えた型は是正する（リストへ足さない）。数が減ったら同じコミットでリストも更新する。規約 §3.1 のラチェットの一致検査と同じ運用。
  - 環境変数 `DS4W_ARITY_BASELINE_OUT` に書き出し先を入れて実行すると、実測から許可リストを作り直せる（`LayerDependencyBaseline` と同じ方式）。
- 初期の許可リスト（9 型、実測で確定する）: `Mapping` 28、`MainWindow` 21、`MainWindowFactory` 20、`ViewModelFactory` 19、`DeviceConnector` 18、`EditorWindowFactory` 18、`ControlService` 16、`ActionBindingBuilder` 16、`ServiceLifecycle` 16。
- 努力目標（15 以下）の対象外（値の入れ物・工場・View）は、**テストでは区別しない**。許可リストに載せて増加だけを止める。値の入れ物は現在すべて 15 未満のため、免除の仕組みは作らない（必要になったら決定として扱う）。
- テスト自身の確認: 既知の型（`Mapping`）が許可リストにあり、引数が 28 と数えられること。コンパイラー生成の型が数に入らないこと。
- 命名は機能名（規約 §4）。経緯は XML コメントに書く。1 ファイル 1 型。

### 3.2 規約の追記（`.github/copilot-instructions.md` §3.1）

追記する文案（承認で確定）:

> **コンストラクターの引数の努力目標**（2026-11-10 追記。Phase16-Step0）: 新規・変更するクラスのコンストラクターの引数は 15 以下を努力目標とする。超えるときは、理由をクラスのコメントに書く。値の入れ物・工場・View（Window）も数は同じ基準で測るが、責務の分割ではなく、生成物側の整理に追随して減らす。3 つ以上の bool を引数に持つ型は、呼び出し側で名前付き引数を使う。`ConstructorArityRuleTests` が、15 を超える型を `ConstructorArityBaseline` に凍結し、実数との一致を検査する。減らしたら同じコミットで基準も更新する。引数の数だけを指標にせず、束ねるだけで依存が見えなくなる変更（何でも箱）はしない。

あわせて、冒頭の「最終更新日」の括弧に Phase16-Step0 を足す。

### 3.3 基準値の記録

`Phase16-Plan.md` §12 に、次を表で記す。

| 指標 | 測り方 |
|---|---|
| 引数の分布（5・8・10・12・15・20 以上の件数） | 3.1 のテストの出力（正規表現の計測との差も記す） |
| 許可リストの 9 型の引数 | 同上 |
| L2・L7 | `LayerDependencyRuleTests` の出力（30・5） |
| テストの組み立ての数 | 親計画書 §9 の「偽物の数」: 5 クラス＋`ActionBindingBuilder`・`SpecialActionOutputFactory` を手で作るテストの箇所数とファイル数（上記 2 の数値を再計測） |
| テストの件数と時間 | Debug ALL（Build Debug → Build Test → Execute Test、`--filter "Category!=Benchmark"`）の件数と所要時間。警告 0・エラー 0 の確認 |
| `Nefarius.ViGEm` を参照するファイル、`TimeProvider` の使用、`DS4Device.cs` の行数 | grep と `wc`（8、0、2,062） |

### 3.4 16-6a の前提の確認（調査のみ）

- 上記 2 の事実を `Phase16-Plan.md` §12 に記す。
- `DS4Device` の読み取りループ（`performDs4Input`）が、`HidDevice` の何を呼ぶかの一覧（`ReadFile`、`WriteOutputReportViaInterrupt`、`CancelIO`、`readFeatureData`、`ReadSerial`、`Capabilities`、`Attributes`、`SafeReadHandle` ほか）を作り、`IHidTransport` の契約案の下書きとして `Phase16-Plan.md` §12 に添える。コードは書かない。
- 分離前の出力を記録する方式（偽の transport に固定のバイト列を流し、`DS4State` を JSON などで保存して比較）が成り立つかの所見を書く。成り立たない点（スレッド、タイマー、静的な状態）があれば、16-6a の計画書で扱う項目として挙げる。

## 4. 変更するファイル

| ファイル | 内容 |
|---|---|
| `DS4WindowsTests/ConstructorArityRuleTests.cs`（新） | 3.1 |
| `DS4WindowsTests/ConstructorArityBaseline.cs`（新） | 3.1 |
| `.github/copilot-instructions.md` | 3.2 |
| `docs-forDIMG/MadeByAgent/Phase16-Plan.md` | §12 進捗に 3.3・3.4 の結果 |

本体（`DS4Windows/`）は変更しない。

## 5. テストと確認

- 新しいテストが合格し、許可リストの不一致で失敗することを一度確かめる（リストから 1 型を消して失敗し、戻して合格）。
- Debug ALL: 警告 0・エラー 0・全合格。
- 実機確認: 不要（本体の変更なし）。

## 6. コミット

1. テストと許可リスト（1 コミット）
2. 規約の追記（1 コミット）
3. 基準値と調査結果の記録（1 コミット）

各コミットの末尾に `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`。push はしない。

## 7. リスク

- リフレクションの数え方が正規表現の計測とずれる（過負荷、ネスト型）。→ 差を 3.3 に記録し、以後はテストの値を正とする。
- 許可リストの運用が増える。→ 親計画書 C7＝A で承認済み。書き出し用の環境変数で更新の手間を小さくする。

## 8. 決定してほしい点

| ID | 内容 | 推奨 |
|---|---|---|
| S0-1 | 閾値を「15 を超える」とする（15 は許可）。親計画書の「15 以下が目標」と一致 | そのとおり |
| S0-2 | 値の入れ物・工場・View を免除せず、許可リストで凍結する | 免除しない |
| S0-3 | 規約の文案（3.2）で確定してよいか | 確定 |
