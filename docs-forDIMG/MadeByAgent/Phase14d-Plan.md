# Phase14d 個別計画書: テストの整理

作成日: 2026-11-05
状態: **承認済み（2026-11-05。F1〜F4 はユーザーがすべて推奨案を選択）→ 14d-0〜14d-3 実施済み（§9）**。F5・F6 は未確認のため、14d-6 の取り込みの前に確認する
ブランチ: `Phase14d`（master 4a0f83b3 から作成）
根拠: `Phase11-and-Beyond-Roadmap.md`（14d の行）、`Repository-Review-2026-11-05.md`（R-16）、`Phase14c-Plan.md`（14c-0 の Slow の印）、規約 `.github/copilot-instructions.md` §2.2・§2.3・§4

## 1. 目的

テストが増えて、実行の時間と、ファイルの移動・名前の変更のたびに直す手間（保守の費用）が大きくなった。次の 2 つを、**守っている内容を減らさずに**下げる。

1. 実行の時間: 全件（Debug ALL のテスト）と、Step の途中の確認（`Category!=Slow`）。
2. 保守の費用: ソースを文字列として読む検査（ファイルのパスや、コードの断片を文字列で持つ）の数と、その重複。

Phase15（UI 共通化）で UI のファイルを大きく動かす前に、パスを文字列で持つ検査を減らしておく（ロードマップの 14d の理由）。

## 2. 実測（2026-11-05、master 4a0f83b3）

### 2.1 時間

| 区分 | 件数 | 合計時間 | 備考 |
|---|---|---|---|
| 全件 | 3,264 | 65.6 秒（実行全体は約 70 秒） | 並列実行は無効（`xunit.runner.json` の `parallelizeTestCollections: false`） |
| 0.3 秒以上（`Category=Slow`） | 56 | **48.9 秒（75%）** | 実時間で待つもの（マクロの再生・繰り返し、マルチアクションのタップ間隔、ジャイロ較正の保持秒）、差分テスト、WPF の描画、ベンチマーク |
| 0.01〜0.3 秒 | 224 | 13.6 秒 | ホストの構築、リポジトリ全体の `.cs` を読む検査（ラチェット・名前空間）、ファイルの読み書き |
| 0.01 秒未満 | 2,984 | 3.2 秒 | ソースを読む検査の大半はここ（時間はほぼ使わない） |

上位: `Phase9MapCustomCommitterDifferentialTests`（1 件 5.8 秒。旧 `Mapping.Commit` の逐語コピーとの差分）、`AppLoggerNotificationFallbackTests`（2.7 秒）、`R2W4MacroToggleRepeatTests`（4.2 秒）、`Phase8Step78cW1ControlsMacroCharacterizationTests`（5.4 秒）、`Phase8Step77bW1MultiActionDualPathTests`（5.2 秒）、`ControllerReadingsNoClippingTests`（2.7 秒）、`Phase8Step4MapCustomBenchmarkTests`（1.6 秒）。

→ **時間を下げる手段は、遅い 56 件（と、並列実行）**。ソースを読む検査を消しても、時間はほとんど減らない（保守の費用が減る）。

### 2.2 テストの構成

| 区分 | ファイル | テストメソッド |
|---|---|---|
| テストを持つファイル | 438 | 2,426（`InlineData` を含めて約 3,190 件） |
| Phase 名・Step 名のファイル | 309 | 1,877 |
| 機能名のファイル | 129 | 549 |
| `*Guard*` | 110 | 447 |
| ホスト（`TestHost`）を使う | 148 | 967 |
| `[Collection("ServiceProviderHolder")]`（Phase13 で撤廃した型の名前のまま） | 140 | — |

### 2.3 ソースを文字列として読む検査

| 区分 | 件数 |
|---|---|
| ソースを読むテストメソッド | **391**（約 537 件、160 ファイル） |
| うち「ないこと」だけを検査（`DoesNotContain`／`DoesNotMatch`） | 31 |
| うち「ないこと」と「あること」の両方 | 123 |
| うち「あること」だけ（コードの断片、XAML の構造、ログの文言、登録の行など） | 237 |
| 読んでいるソースのファイル（重複なし） | 242（上位: `ControlService.cs` 39、`BackingStore.cs` 36、`ProfileEditor.xaml.cs` 34、`Mapping.cs` 29） |
| **もう存在しないファイルを読み、何も検査せずに合格しているメソッド**（空振り） | **9**（旧 `DS4Control/Global.cs` 8、旧 `Actions/ActionManager.cs` 1） |
| 「ないこと」の検査で多い文字列 | `AppHost` 32、`ServiceProviderHolder` 18、`Program.rootHub` 11、`*.Shared` 8、`Global.*` 多数 |
| 各ファイルが自前で持つ読み取りの補助メソッド | 62 ファイル |
| コメントを除く処理（`StripComments`）の複製 | 88 ファイル |

ラチェット（基準値の表）:

| テスト | 状態 |
|---|---|
| `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`Program.rootHub` のファイル別の件数） | 表は 70 行、0 でないのは 4 行だけ（`AppHost` が `App`・`AppHost` 自身、`rootHub` がログの文言の 2 件）。`Global` 型は Phase7 で削除済み |
| `Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`） | 表は空（型は Phase13 で撤廃済み） |
| `Phase7Step10eW5RatchetEqualityGuardTests` | 上の 2 つの「基準値＝実数」を検査 |
| `Phase13FuncRatchetGuardTests`、`Phase10Step4dW0RuntimeKeyRatchetTests`、`LayerDependencyRuleTests` | 今も意味がある（残す） |

### 2.4 問題の整理

1. **空振り**: `SourceFileLocator.Find` は、ファイルが見つからないと `null` を返し、テストは何もせずに合格する（ソースなしで実行する環境のための設計）。ファイルを移す・消すと、検査が黙って消える（9 件が該当。Phase14 のフォルダの再編では、パスの書き換えの漏れも同じ形で隠れうる）。
2. **撤去済みの API の検査の散らばり**: 型そのものがもう存在しない（`Global`、`Program`、`ServiceProviderHolder`、`SyntheticDispatcher`、`ActionRegistry` など）のに、「その名前が文字列で現れない」ことを、多くのファイルが個別に検査している。型の検査（ArchUnitNET。`LayerDependencyRuleTests` と同じ仕組み）なら 1 か所で書ける。
3. **重複**: 読み取りの補助（62）と、コメントを除く処理（88）の複製。
4. **遅いテスト**: 実時間で待つテストと、移行時の差分テスト（旧実装の逐語コピーと比べる）が時間の大半を使う。
5. **名前**: Phase 名のファイルは、どの機能のテストかを探しにくい（R-16）。共有のコレクション名が、撤廃した型の名前のまま。

## 3. 守るもの（削除してはならない検査）

次の検査は、残すか、同じ内容を別の形（型の検査など）で守ってから移す。**削除・置き換えは、§6 の 14d-0 の棚卸し表で 1 件ずつ「何を守っていたか」と「置き換え先」を記録してから行う**（規約 §2.2）。

| 種類 | 理由 |
|---|---|
| 安全弁（Fresh-press ガード、実行中の再実行防止、一時プロファイルの復帰スタック、排他制御、ガードレール 6 つ） | 規約 §2.2 |
| ログの文言・レベル | 規約 §2.3（文言を文字列で検査するのは正しい使い方） |
| XAML の構造・見た目（`XamlTestHelper`） | 型の検査では表せない |
| 振る舞いのテスト（特性化・ゴールデン・差分） | 時間を下げるときも、入力の列・期待値の意味は変えない |
| 今も意味のあるラチェット（`Func<I…>`、層の依存、実行時キー） | 増加を止める仕組み |

## 4. 進め方の原則

- **本体のコード（`DS4Windows/`）は変えない**（F2 で B を選んだ場合だけ、本体の時間の扱いを変える）。
- テストの件数は減ってよいが、減った分は棚卸し表で説明がつくこと（「置き換え先」か「空振り・重複のため削除」）。
- 1 つの Step の中で、置き換え先のテストを先に足し、合格を確かめてから古い検査を消す（同じコミットでよい）。
- 各 Step の終わりに Debug ALL（全件）。途中の確認は `Category!=Slow`。

## 5. 決定事項（2026-11-05。F1〜F4 承認、F5・F6 は 14d-6 の前に確認）

| # | 項目 | 決定 |
|---|---|---|
| F1 | 整理の範囲 | **A**: 空振りの削除、撤去済み API の検査を型の検査 1 か所へ、登録・コンストラクター引数の文字列検査をコンテナ・リフレクションの検査へ（XAML・ログの文言・安全弁は残す） |
| F2 | 遅いテストの短縮 | **A**: テストの側だけで短縮（待ち時間の短いテストデータ、差分テストの入力の列の数、ベンチマークは `Category=Benchmark` にして通常の実行から外す） |
| F3 | テストの改名（R-16） | **B**: この Phase で中身を触るテストだけ機能名へ。共有のコレクション名も改名 |
| F4 | 並列実行 | **A**: 計測してから決める（共有の状態に触るクラスは直列のコレクションに残す。全件が 5 回続けて合格し、時間が 3 割以上減るときだけ有効にする） |
| F5 | 実機確認 | 推奨（未確認）: 本体のコードを変えない（F2＝A）ので行わない |
| F6 | 取り込み | 推奨（未確認）: Debug ALL の後に、master へ fast-forward で取り込み push（Phase14c の E6 と同じ） |

## 6. Step 構成

| Step | 内容 |
|---|---|
| 14d-0 | **棚卸し表** `Phase14d-Test-Inventory.md`: ソースを読む 391 メソッドと遅い 56 件を、K（残す）・R（型・コンテナの検査へ置き換え）・F（撤去済み API の検査へ畳む）・D（空振り・重複のため削除）・S（短縮）に分け、それぞれ「何を守っているか」を書く。補助スクリプトで下書きし、D・F・R は 1 件ずつ読んで確かめる |
| 14d-1 | **共通の読み取り部品** `SourceText`（探す・読む・コメントを除く）。リポジトリがあるのにファイルがないときは**失敗**にする（ソースそのものがない環境だけ省略）。62 の自前の補助と 88 の複製を置き換え、表面に出た空振り 9 件を棚卸し表のとおり処理する |
| 14d-2 | **撤去済み API の検査を 1 か所へ**: `RemovedApiRuleTests`（型の検査。`Global`・`Program`・`ServiceProviderHolder`・`SyntheticDispatcher`・`ActionRegistry` などの型がないこと、`*.Shared` などのメンバーがないこと、`AppHost` を使うのは `App` と `Composition` だけ）。置き換えた文字列検査と、役目を終えたラチェット（`Step10FourGlobalRatchetGuardTests` の 0 の行、`Phase7Step1ServiceLocatorRatchetGuardTests`、その一致検査）を整理する（ログの文言の 2 件は残す） |
| 14d-3 | **登録・引数の文字列検査の置き換え**: 「`services.AddSingleton<…>` の行がある」→ コンテナから解決して型と同一性を確かめる。「コンストラクターに `IXxx xxx` がある」→ リフレクションで引数の型を確かめる |
| 14d-4 | **遅いテストの短縮**: マクロ・マルチアクションのテストデータの待ち時間を短く（判定の境界は変えない）、差分テストの入力の列を代表的なものに絞る（網羅の観点は棚卸し表に記録）、ベンチマークを `Category=Benchmark` に。規約 §4 に Benchmark の扱いを追記 |
| 14d-5 | **コレクション名の改名と並列実行の計測**: `ServiceProviderHolder` → 共有の状態を表す名前（例: `SharedHostState`）。並列実行を試し、F4 の条件を満たすときだけ有効にする |
| 14d-6 | 規約・ロードマップ・記録（件数と時間の前後）、Debug ALL、F5・F6 の確認、取り込みと push |

目標（計測して記録する。保証ではない）: 全件の時間 65.6 秒 → 40 秒以下、途中の確認 約 18 秒 → 12 秒以下、ソースを読むテストメソッド 467 → 約 312（棚卸し表の数え方。F・D の 126 件を消し、R の 29 件を型・コンテナの検査へ）、自前の読み取り補助 62 → 0。

## 7. 対象外

- 振る舞いのテストの中身の書き直し（期待値・入力の意味は変えない）。
- 本体の時間の扱いの変更（`TimeProvider` の導入など）は、F2 で B を選んだ場合だけ。選ばない場合は、マクロの再生・繰り返しのコードを触る Phase（Phase16 など）の候補として記録する。
- Phase 名のテストの一括改名（F3 で A を選んだ場合だけ）。過去の計画書・完了報告がテストのクラス名を根拠として引いているため、一括改名すると、その記録をたどりにくくなる。

## 8. リスク

| リスク | 対策 |
|---|---|
| 検査を消して、守っていた内容が抜ける | 14d-0 の棚卸し表で 1 件ずつ記録し、置き換え先を先に足してから消す。§3 の種類は消さない |
| 「ファイルがないと失敗」にすると、ソースなしの環境（配布物だけでのテスト）で失敗する | リポジトリのルート（`DS4Windows/DS4WinWPF.csproj`）が見つからないときだけ省略する（今と同じ） |
| 待ち時間を短くして、時間に依存するテストが不安定になる | 判定の境界（タップ間隔・保持秒・Fresh-press の 80ms）は変えず、待ちの余裕だけを詰める。全件を 5 回続けて流して確かめる |
| 並列実行で、静的な状態（`AppLogger`、`PathService` の起動前の状態など）を共有するテストが干渉する | 共有の状態に触るクラスは直列のコレクションに残す。条件を満たさなければ直列のまま（F4） |

## 9. 実施の記録

| Step | コミット | 結果 |
|---|---|---|
| 14d-0 | 785ef416 | 棚卸し表 `Phase14d-Test-Inventory.md`。ソースを読むメソッド 467（補助メソッド・パスの一覧のフィールド経由を含めて数え直した。§2.3 の 391 は Assert の文字列で数えた概数）を、F 100・D 26・R 29・K 312（うち 16 件は撤去済みの名前の Assert だけを外す）に分けた。F・D で全メソッドが消えるファイルは 10。遅いテストは 60 件・50.7 秒（メソッド単位）。F の大半は、Phase7-Step10d で削除した `Global` 型の参照の検査（型がないため、参照はコンパイルで止まる） |
| 14d-1 | dd4598da | `SourceFileLocator.Find` を厳しくした（リポジトリがあるのにファイルがなければ失敗。ないことの確認は新設の `Exists`）。共通の読み取り部品 `SourceText`（`Read`・`ReadCode`・`ReadNormalized`・`StripComments`・`ProductionFiles`・`RelativePathOf`）を追加し、自前の補助 80 個（コメント除去の複製 20、読み取り 60）を置き換えた。残る 23 個は、メソッドの範囲の切り出しなどテスト固有の処理を持つもの（厳しくした `Find` を通る）。空振り 9 件と、F の 1 件（`Step10FourTwoGlobalReferenceGuardTests` の Global の検査。旧パスを含み失敗するようになったため前倒し）を削除。**厳しくしたことで、Phase14 のフォルダーの再編のときにパスの書き換えが漏れ、黙って省略されていた検査が 3 ファイル・4 件見つかった**（`Phase9E6ConnectionGuardTests` の 2 件［旧 `DS4Control/Services/`］、`Phase7Step2RemovedContractMembersGuardTests` の 2 件［旧 `DI/`］）。ファイル名から引く形に直し、いずれも合格。Debug ALL: 警告 0・エラー 0、3,254 件＋11 件 |
| 14d-2 | 371dae57 | `RemovedApiRuleTests`（撤去した型 35、撤去・移動したメンバーの表 20 型、`AppHost` を使うのは `App` と `DS4Windows.Composition` だけ［`LayerDependencyScanner` の辺を使い、App → AppHost の辺があることも確かめて空振りを防ぐ］）を追加。棚卸し表の F 71・D 16（14d-1 の 10 件を除く）を削除し、9 ファイルはテストがなくなったため削除（`Step10FourGlobalRatchetGuardTests`・`Phase7Step1ServiceLocatorRatchetGuardTests`・`Phase7Step10eW5RatchetEqualityGuardTests`、Global の参照の検査 6 ファイル）。15 メソッドで撤去済みの名前の Assert だけを外し、使われなくなった補助・一覧 42 個を削除。判定の見直し: リフレクションの検査 16 件と Phase9 の責務の検査を K に（棚卸し表の冒頭）。規約 §3.1・§3.3 原則1 の、撤去したラチェットの記述を置き換えの説明に更新。Debug ALL: 警告 0・エラー 0、3,151 件（103 件減）＋11 件。途中の確認 3,086 件・15 秒 |
| 14d-3 | （本コミット） | R の 27 件を置き換えた: 引数・フィールド・修飾子（volatile）・多重定義はリフレクション、登録の行はコンテナの解決（同じインスタンス、配線の結果、購読者が ActionsLoadedRuntimeReset であること）、外部・本体の型への依存（SharpOSC、HidHideAPIDevice、ControlService）は `LayerDependencyScanner.AllTargetsOf`（新設。外部の型も含めて、メソッド本体の参照まで見る。空振りを防ぐため、使う側から参照が見えることも確かめる）。`Func<ControlService>` は `Phase13FuncRatchetGuardTests` の対象に加え、`Phase13Step5FuncRemovalGuardTests` を削除。循環の制約（BackingStore の登録から IOutputSlotService を解決しない。実行するとスタックオーバーフロー）と、WPF の画面の配線（ProfileEditor・MainWindow）は、実行では確かめられないため、ソースの検査を残した。Debug ALL: 警告 0・エラー 0、3,150 件＋11 件 |

- 不安定なテスト: `ProfileRepositoryTests.LinkedProfile_ChangeAndRemove_ShouldNotThrowAndShouldPersist` が、途中の確認で時々失敗する（単独では合格。テストは直列で動くため、並行ではなく、前のテストが残した状態［アプリデータのフォルダー］に左右されると見られる）。14d-5 で共有の状態として調べる。
