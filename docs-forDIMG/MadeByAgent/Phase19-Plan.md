# Phase19 計画書: 工場（Factory）を廃止し、DI で直接つくる（引数が多くなる根本原因の除去 その 1）

作成日: 2026-11-10
前提: 「引数 6 以上のコンストラクターを避ける根本的な対策」の検討（効果の大きい順に 1: 責務で分ける、2: 工場をやめる、3: 同じ契約の依存を列で受ける）。本 Phase は対策 2。実施順は、機械的で安全な 2 → 責務の分割（1。Phase20）→ 列で受ける（3。Phase21）。
規則: 3.1（Pure DI。Service Locator を持ち込まない。引数は 15 以下が努力目標、新規は 6 以下をめざす）、3.3、2.2（機能・ログの完全維持）。

## 1. 目的と終わりの状態

- 「窓や ViewModel を作るためだけに、依存をすべて受け取って、そのまま新しいオブジェクトへ流す」工場の引数の列を無くす。工場は、依存の一覧を二重に持つ原因になっている（例: `MainWindowFactory` 18、`ShellViewModelFactory` 14、`ProfileEditViewModelFactory` 10、`ProfileEditorFactory` 9）。
- 起動時に 1 回だけ作る窓・ViewModel は、DI コンテナに型を登録して直接つくる（`AddSingleton<MainWindow>()` や `ActivatorUtilities`）。実行中に何度も作るもの（編集画面、割り当て編集画面など）は、実行時の引数（スロット番号、対象のプロファイルなど）だけを受ける小さな工場に絞る（コンテナの依存は、工場が持たず、`ActivatorUtilities.CreateFactory` で補う）。
- 画面の見た目・操作・ログは変えない。

## 2. 現状（2026-11-10 の反射での測定）

| 型 | 引数 | 種類 | 作るもの |
|---|---|---|---|
| `MainWindowFactory` | 18 | 起動時 1 回 | `MainWindow` |
| `ShellViewModelFactory` | 14 | 起動時 1 回 | 画面全体の ViewModel 群 |
| `ProfileEditViewModelFactory` | 10 | 実行時に何度も（引数あり） | プロファイル編集の ViewModel |
| `ProfileEditorFactory` | 9 | 実行時に何度も | プロファイル編集の窓 |
| `AssignmentWindowFactory`／`EditorWindowFactory`／`MacroRecordingWindowFactory`／`ActionViewModelFactory`／`ViewModelFactory` | 調査 | 実行時 | 各種の窓・ViewModel |
| `SpecialActionOutputFactory`（Processing） | 14 | 実行時 | 特殊アクションの出力 |

（上は例。Step 19-0 で全工場を分類する。）

## 3. 設計の決定

| 番号 | 項目 | 決定 | 理由 |
|---|---|---|---|
| 1 | 分類 | 工場を 3 種に分ける: A. 起動時 1 回（コンテナで直接つくる）、B. 実行時に引数つきで何度も（`ActivatorUtilities.CreateFactory` で、実行時の引数だけを受ける）、C. 本当に判断を持つ工場（選ぶ・数える）。C は残す | 「依存をそのまま流すだけ」の工場だけを無くし、判断を持つものは消さない |
| 2 | Service Locator との違い | `IServiceProvider` を業務のクラスへ持ち込まない。`ActivatorUtilities` を使ってよいのは `Composition/` の中だけ。工場の契約（`IMainWindowFactory` など）は、外から見える形を変えず、実装を差し替える | 規則 3.1 の Pure DI を保つ。呼び出し側は変更しない |
| 3 | 窓の登録 | 窓（`MainWindow`）は WPF の `Window` で、UI スレッドで作る必要がある。登録は `AddSingleton<MainWindow>` ではなく、`Func`／工場の中で UI スレッドを守って作る（既存の起動順序を変えない） | 起動順序とスレッドの前提を守る |
| 4 | 順序 | 影響の小さい順に: 19-1 `ProfileEditorFactory`、19-2 `ProfileEditViewModelFactory`、19-3 `ShellViewModelFactory`、19-4 `MainWindowFactory`。1 つずつ、全テストを通す | 窓の工場は最後。他は Phase20 と干渉する |
| 5 | 引数の減り方の見込み | 工場そのものの引数が 0〜2（実行時の引数だけ）になる。窓・ViewModel の引数は変わらない（それは Phase20 で扱う） | 工場が持つ依存の「二重の一覧」だけを消す |

## 4. 段階

| 段階 | 内容 | 守り |
|---|---|---|
| 19-0 | 全工場の分類（A・B・C）。各工場について、作るもの・呼び出し元・実行時の引数・スレッドの前提を表にする。計画書を更新 | 計画書の更新 |
| 19-1〜19-4 | 決定 4 の順に、工場の実装を `Composition` の登録へ置き換える。契約（`I…Factory`）の形は不変。作るオブジェクトの生成の順序・初期化の呼び出しは、旧と同じにする | 既存の工場のテスト、`ServiceRegistration` の解決テスト、全テスト |
| 19-5 | `ConstructorArityBaseline` から、工場の分（`MainWindowFactory` 18 など）を除く。台帳・モデル図 01・ロードマップの更新 | `ConstructorArityRuleTests` |

## 5. リスク

- 窓を UI スレッド以外で作る・順序が変わると、起動時に例外や表示の乱れが出る。→ 決定 3 のとおり、生成の位置・スレッドを変えない。
- コンテナで作った窓が、後から破棄される寿命の違い（Singleton と一時）で、保持が残る。→ 工場 B は「毎回新しく作る（Transient 相当）」で、コンテナに窓を登録しない。
- 実機の確認（起動、プロファイルの編集画面を開閉、割り当て編集画面）は利用者に依頼する。

## 6. 進捗

- 2026-11-10: 計画書を作成。
- 2026-11-10 19-0 の分類と実施（結果）:
  - **`MainWindowFactory`（18）は廃止の対象（A. 起動時 1 回）**。`Composition/MainWindowFactory` へ移し（`git mv` のみ）、`ActivatorUtilities.CreateInstance<MainWindow>(provider, parser)` で窓をつくる形にした。引数は 18 → 1（`IServiceProvider`。組み立て専用の層だけ）。`ConstructorArityBaseline` から除いた。`MainWindowFactoryResolutionTests` が窓の全依存（起動引数を除く）がコンテナから解決できることを検査。全テスト 3,376 件緑。
  - **`ShellViewModelFactory`（14）、`ProfileEditViewModelFactory`（10）、`ActionViewModelFactory`、`ViewModelFactory`**: 1 つの工場が多数の ViewModel を作る「多品目の工場」で、品目ごとに依存の一部と実行時の値を渡し、`[DI] …Created` のログも出す。基準（15 以下）を満たしており、置き換えると品目ごとに登録が要るため、**廃止の対象外（C）**。引数の整理は Phase20 の責務の分割で扱う。
  - **`ProfileEditorFactory`（9）、`AssignmentWindowFactory`／`EditorWindowFactory`／`MacroRecordingWindowFactory`**: 実行時の引数つき（B）。基準を満たし、null 検査のテストが多く、効果が小さいため **今回は見送り**（引数が基準を超えた時点で再検討）。
  - よって Phase19 は **19-1（MainWindowFactory）のみで完了**。計画書の 19-1〜19-4 の順序案は、上の分類で置き換えた。
  - 残る確認: 実機で起動（最小化起動 `-m` を含む）と複製の名前入力欄。
