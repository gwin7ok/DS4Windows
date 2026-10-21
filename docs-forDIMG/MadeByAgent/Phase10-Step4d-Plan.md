# Phase10-Step4d 個別計画書: 固定 ID（プロファイルと SA の参照を、名前から ID へ）

作成日: 2026-10-21
対象ブランチ: `For-DI-migration-work`
前提: `Phase10-Step4c-Plan.md`（完了。項目 5 を本 Step に分離）、`Phase10-Integration-Target-Model.md`、`Phase10-Status.md`

> 状態: **決定事項は確定（2026-10-21、ユーザー。§6。Q5 は「内部の実行時の状態も ID をキーにする」へ変更）。Wave の着手の承認待ち**。製品コードは、まだ変えていない。

---

## 1. 目的

プロファイルと SA（Special Action）は、いま**名前**で識別され、名前で参照されている。名前を変えるたびに、参照している場所を 1 つずつ書き換える追従のコードが要り、書き換え漏れ（切り替わらない、選択が空欄になる）が過去に何度も不具合になった。

この Step は、プロファイルと SA に**固定の ID** を持たせ、参照を ID にする。名前は、表示とファイル名のためだけに使う。名前変更の追従が、原則として不要になる。

ユーザー決定（2026-10-21）: プロファイルと SA の**両方**について、名前をキーにしているものを ID をキーにする。SA は、プロファイルの所有にはしない（共有のまま。`Actions.xml`）。

## 2. 現状（コードで確認した事実）

### 2.1 プロファイルの識別と参照

| 場所 | 現状 | 名前変更の追従 |
|---|---|---|
| プロファイルのファイル | `Profiles\<名前>.xml`。ファイル名＝名前 | リネーム |
| 各スロットの選択（`Profiles.xml` の `Controller1`…） | 名前（拡張子なし） | `MainWindow` の再構築（`activeProfiles`、`ProfilePath[]`、`OlderProfilePath[]`） |
| リンクプロファイル（`LinkedProfiles.xml`） | コントローラーの識別子 → 名前 | `ReplaceLinkedProfileName` |
| 自動プロファイル（`Auto Profiles.xml`） | プログラム → 名前 | `AutoProfileHolder.ReplaceProfileName` |
| SA の Load Profile（`Actions.xml` の `details`） | 名前 | `ReplaceProfileSwitchTarget` |
| Controls の Load Profile（各プロファイルの `<Control><Special>` の `ProfileName`） | 名前 | `ReplaceControlProfileSwitchTarget`（Phase10-Step4 W6） |
| 実行時の状態 | `ProfilePath[]`、`OlderProfilePath[]`（19 ファイル・約 80 か所が参照）、一時プロファイルの名前（約 24 か所）と復帰の状態 | 上の再構築 |
| 画面 | `ProfileEntity.Name`、プロファイルの一覧（`ProfileList`） | 一覧の再構築 |

### 2.2 SA の識別と参照

| 場所 | 現状 | 名前変更の追従 |
|---|---|---|
| SA の定義（`Actions.xml` の `Action Name=…`） | 名前 | — |
| 各プロファイルの割り当て（`<ProfileActions>`） | 名前を `/` で連結した文字列 | `ReplaceActionReference`／`RemoveActionReference`（全プロファイルの XML を書き換え） |
| 実行時の状態（`ActionInstanceState`） | `ActionManager` の登録簿が、`SpecialAction` の名前を鍵に引く | — |
| 各スロットの SA の一覧（実行時） | `profileActions[device]`（名前の一覧。約 45 か所）、`GetProfileAction(device, 名前)` | — |
| Controls の合成 SA（`Synthetic_Key_{device}_{control}` ほか） | 実行時にだけ生成。保存しない | 対象外 |

## 3. 設計案（要確認の点は §6）

### 3.1 ID の形

- 形式: GUID（`N` 形式の 32 文字）。表示には出さない。
- 保存先:
  - プロファイル: プロファイル XML の新しい要素（例: `<ProfileId>`）。
  - SA: `Actions.xml` の `Action` の新しい要素（または属性）。

### 3.2 参照の持ち方

- 参照は **ID を主**にし、**名前を併記**する（読みやすさと、古いバージョンで読めること）。読み込み時は、ID で引き、見つからなければ名前で引く（古いファイル、手で編集したファイルへの救済）。名前で見つかり、ID が違う（または無い）ときは、その場で ID を補う（自己修復）。
- 対象: スロットの選択、リンクプロファイル、自動プロファイル、SA の Load Profile、Controls の Load Profile、`<ProfileActions>`。
- 実行時の内部も、**ID をキーにする**（§6 Q5。ユーザー決定）。`ProfilePath[]`／`OlderProfilePath[]`／一時プロファイルの状態は、プロファイルの ID（またはそれを含む型）に、`profileActions[device]` と `ActionManager` の登録簿は、SA の ID に置き換える。ファイル名・表示名は、ID から引く（`IProfileRepository` の窓口）。Controls が実行時に作る合成の SA（`Synthetic_*`）は保存しないため、保存する ID を持たず、実行時の鍵として、従来どおりの文字列を使う（鍵の型を、SA の ID と合成の鍵の両方を受けられる 1 つの型にまとめる）。

### 3.3 既存のファイルの移行

- ID が無いプロファイル・SA は、読み込み時に ID を割り当てる。保存は、次の通常の保存の機会に行う（読み込みだけで、他のファイルを書き換えない）案（§6 Q4）。
- 古いバージョンで保存したファイル（ID なし）は、そのまま読める。新しいファイルを古いバージョンで読むと、ID の要素は無視され、名前で動く（Phase10 の Q5＝新しいファイルが旧バージョンで読めなくなることを許容、の範囲内）。

### 3.4 重複と取り込み

- ファイルのコピー（同じ ID を持つ 2 つのプロファイル）、外部で配布されたプロファイルの取り込み、ID の衝突は、プロファイルの一覧を作るときに検出し、後から読んだほうに新しい ID を振る（§6 Q3）。
- SA の ID の衝突（`Actions.xml` を手でコピーした場合）も同様。

### 3.5 名前変更の追従の扱い

- 参照が ID になれば、名前変更で書き換える必要があるのは、**併記した名前**（表示用）だけになる。次の保存の機会に更新すれば足りる。
- 追従のコード（`ReplaceLinkedProfileName`、`ReplaceProfileSwitchTarget`、`ReplaceControlProfileSwitchTarget`、`AutoProfileHolder.ReplaceProfileName`、`ReplaceActionReference`、`RemoveActionReference`）は、段階的に不要にし、最後の Wave で撤去する。削除（`RemoveActionReference`）は、ID が無効になる扱い（参照を外す）に置き換える。

## 4. Wave 案

各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する（2026-10-21 以降は、私が `Debug ALL` を実行する）。実機確認は、挙動が変わる Wave に付ける（先送りの実機確認を溜めない）。

1. **W0: 特性化テスト**（製品コードは変えない）。現行の、名前変更の追従（上の 6 つの経路）、削除、保存形式（`Profiles.xml`、`LinkedProfiles.xml`、`Auto Profiles.xml`、`Actions.xml`、プロファイル XML の `<ProfileActions>`、`<Control><Special>`）を固定する。
2. **W1: プロファイルの ID**。ID の割り当て・保存・読み込み、プロファイルの一覧での重複の検出、自己修復。参照はまだ名前のまま（追加のみ）。
3. **W2: SA の ID**。`Actions.xml` の ID。同上。
4. **W3: 参照を ID に（SA と Controls の Load Profile、`<ProfileActions>`）**。ID を主、名前を併記。ID→名前の解決は 1 か所の部品にまとめる。
5. **W4: 参照を ID に（スロットの選択、リンクプロファイル、自動プロファイル）**。
6. **W5: 実行時の状態を ID に（プロファイル）**。`ProfilePath[]`、`OlderProfilePath[]`、一時プロファイル（復帰の状態）、`IProfileSwitcher`、`ProfileApplicationService`／`ProfileSlotApplier`、画面（`ProfileEntity`、`ControllerListViewModel`、`TrayIconViewModel`、`CompositeDeviceModel`）を、プロファイルの ID をキーにする。ファイルのパス・表示名は、ID から引く。
7. **W6: 実行時の状態を ID に（SA）**。`profileActions[device]`、`GetProfileAction`、`ActionManager` の登録簿（`ActionInstanceState` の鍵）を、SA の ID をキーにする。合成の SA（`Synthetic_*`）の鍵は、1 つの鍵の型にまとめる。
8. **W7: 追従のコードの撤去**。不要になった `Replace*`／`RemoveActionReference` と、`MainWindow` の再構築の一部を撤去。名前変更・削除の挙動（切り替わる、選択が残る、参照が外れる）を、テストと実機で確認。
9. **最終 W: 文書**（完了の記録、モデル図、台帳、実機確認の統合）。

## 5. テスト

- 保存形式の往復（旧形式［ID なし］の読み込み、新形式の保存・読み込み、旧形式のファイルは書き換えない）。
- 名前変更、削除、複製（ID の重複）、手編集（ID の食い違い）の各場面。参照の解決順序（ID → 名前）。
- 既存のテスト（Phase6〜Phase10 の追従のテスト）は、W5 まで通ること。W5 で、撤去したコードのテストを、新しい仕組みのテストへ置き換える。

## 6. 決定してほしい事項

| # | 内容 | 推奨 |
|---|---|---|
| Q1 | ID の形式 | GUID（`N` 形式）。短い連番は、複数のマシンで作ったプロファイルの取り込みで衝突するため非推奨 |
| Q2 | 参照の持ち方 | **ID を主、名前を併記**（ID で引き、無ければ名前で引く）。古いバージョンでも読め、手編集の救済にもなる |
| Q3 | 複製・衝突 | 一覧を作るときに検出し、後から読んだほうに新しい ID を振って、そのファイルへ保存する |
| Q4 | 既存ファイルの移行の時期 | 読み込み時に ID を割り当て、保存は**次の通常の保存の機会**（読み込みだけでは、書き換えない） |
| Q5 | 実行時の内部の範囲 | **決定（ユーザー、2026-10-21）: 内部の実行時の状態も ID をキーにする**（`ProfilePath[]`、`OlderProfilePath[]`、一時プロファイル、`profileActions[device]`、`ActionManager` の登録簿）。当初の推奨は「境界で変換し、内部は最小」だったが、ユーザーの指示で全面的に ID へ置き換える。変更が大きいため、保存する参照（W3〜W4）と実行時の状態（W5〜W6）を、別の Wave に分ける |
| Q6 | 追従のコードの撤去 | 最後の Wave（W5）で撤去（それまでは残して、二重の安全策にする） |

## 7. 想定されるリスク

- **保存形式の変更が大きい**（6 つの保存先）。W0 の特性化テストと、旧形式の読み込みテストを守りにする。
- **実行時の状態の置き換えが広い**（Q5 の決定。`ProfilePath`／`OlderProfilePath` が 19 ファイル、`profileActions` が約 45 か所、一時プロファイルが約 24 か所、`ActionManager` の登録簿）。実行の中心部（プロファイルの切り替え、SA の状態）に触れるため、Phase7b／Phase8 の切り替え・復帰・SA のテストを守りにし、プロファイルと SA で Wave を分ける。
- **自己修復の書き込みが、意図しないファイルを書き換える**危険。読み込みだけでは書かない（Q4）。
- **古いバージョンとの行き来**: ID の要素は無視されるが、名前変更の追従は古いバージョンでは行われない（既存の挙動と同じ）。
- **ファイルのコピーによる重複**: 検出して付け替える（Q3）。付け替えを行う場所（一覧を作る処理）が、読み込みの副作用でファイルを書くことになる。書き込みの排他（`XmlIoLock`）と、失敗時の扱いに注意。
- **実機確認が増える**: 名前変更・削除・コピー・切り替えの各場面。台帳 §27 の先送り分と合わせ、統合 Step の後に回帰として確認する。

## 8. 進捗の記録

（未着手）
