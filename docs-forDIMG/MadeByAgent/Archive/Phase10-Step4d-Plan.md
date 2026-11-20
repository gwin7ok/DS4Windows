# Phase10-Step4d 個別計画書: 固定 ID（プロファイルと SA の参照を、名前から ID へ）

作成日: 2026-10-21
対象ブランチ: `For-DI-migration-work`
前提: `Phase10-Step4c-Plan.md`（完了。項目 5 を本 Step に分離）、`Phase10-Integration-Target-Model.md`、`Phase10-Status.md`

> 状態: **完了（2026-10-21。W0〜W7 と最終 W。W6 は W6' に見直し。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1998 件と 11 件が成功）。実機確認は、統合版の台帳 §27［P10-90〜97］を、ユーザー判断で先送り（統合 Step の後に、回帰としてまとめて確認）**。次は統合 Step の計画書。

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
7. **W6: 実行時の状態を ID に（SA）→ 見直し（W6'。2026-10-21、ユーザー決定）**。調査の結果、SA 側の実行時の状態（`profileActions[device]`、`ActionManager` の登録簿）は、**名前のまま残す**。理由は §8 の W6'。W7 で `ReplaceActionReference`（全プロファイルの XML の書き換え）を撤去する。
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

- **W0（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1913 件と 11 件が成功）**: 特性化テスト 2 ファイルを追加した（製品コードは変更なし。実機確認なし）。
  - `Phase10Step4dW0NameKeyTests`（11 件）: リンクプロファイルの名前変更（`ReplaceLinkedProfileName`。大文字小文字を無視、表と画面表示用の値、保存）、自動プロファイルの名前変更（`AutoProfileHolder.ReplaceProfileName`。`Auto Profiles.xml` へ保存）、SA の名前変更・削除（`ReplaceActionReference`／`RemoveActionReference`。全プロファイルの `<ProfileActions>`、Ordinal の比較、重複の除去、空・同名は無視）、`GetAppliedProfilesUsingAction`、保存形式に ID がないこと（プロファイル XML、`Actions.xml`）、`ActionManager` の登録簿が SA の名前で状態を引くこと、画面の経路（`MainWindow` の名前変更が 4 つの追従を順に呼ぶこと、`ProfilePath`／`OlderProfilePath` の書き換え、`ProfileEditor` の SA の名前変更が `ReplaceActionReference` を呼ぶこと）。
  - `Phase10Step4dW0RuntimeKeyRatchetTests`（1 件）: 名前をキーにする実行時の参照の件数を、ファイルごとに固定（`ProfilePath`／`OlderProfilePath` 71 件、`profileActions`／`ProfileActions` 62 件。28 ファイル）。実数と一致することを検査する。W5・W6 で ID に替えて件数が減ったら、基準値も同時に更新する。
  - 既存で固定済み: 保存形式の golden（`Phase7Step10cGoldenPersistenceTests`）、SA の Load Profile の追従（`ProfileRenamePropagationTests`）、Controls の Load Profile の追従（`Phase10Step4W6ControlProfileTests`）。
- **W1（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1928 件と 11 件が成功。実機確認 P10-90 は先送り）**: プロファイルの ID。参照はまだ名前のまま（追加のみ）。
  - 保存形式: プロファイル XML のルート直下の先頭に `<ProfileId>`（GUID の `N` 形式）を保存する。**ID はファイルの中**にあるため、名前の変更（ファイル名の変更）では変わらない。保存時は、保存先のファイルがすでに ID を持つならそれを保ち（上書き保存）、なければ新しい ID を振る（別名で保存、新規作成）。このため、スロットごとの ID の状態（`BackingStore`）や、読み込みの節は不要になった（計画の当初案から簡素化）。
  - 登録簿: `IProfileIdentityService`（`DI/`）／`ProfileIdentityService`（`Services/`、Singleton）。`EnsureAllPersisted()`（ID のないファイルへ書く。重複を付け替える）、`GetId`、`GetName`、`EnsurePersisted`、`TryResolve`（ID を優先し、なければ名前）。ファイルの読み書きは `ProfileIdFile`（内部の部品。ID は `XmlReader` で先頭から読み、書くときは `XmlDocument` で、ほかの内容を変えずに先頭へ足す）。排他は `ProfileXmlStore.XmlIoLock`。
  - **決定の変更（ユーザー、2026-10-21）: Q4 の移行の時期は、「起動時に全ファイルへ一括で ID を書く」**（当初の推奨は、参照を保存する直前と通常の保存のとき）。`App` の起動で、メイン画面の生成（プロファイルのスロットへの読み込み）の前に `EnsureAllPersisted()` を呼ぶ。書けなかったファイル（読み取り専用、壊れたファイル）は、ログに警告を出し、参照を保存する直前（W3〜W4）の `EnsurePersisted` で改めて試す。
  - 重複（Q3・Q4 の b）: 同じ ID のファイルのうち、作成日時が古いほうが ID を保ち、ほかには新しい ID を書く（同じ作成日時なら名前順で先のもの）。
  - 当初案から外したもの: `ProfileEntity.Id`（画面の一覧への ID の持たせ方は、一覧が ID を必要とする W3〜W4 で決める。一覧の作成時に全ファイルを読むことを避ける）。
  - golden: 保存されたプロファイル XML の `<ProfileId>`（保存のたびに変わる）は、比較の正規化で除いた（`PersistenceGoldenContext.Normalize`）。golden のファイルは変更なし。ID の検査は新しいテストが持つ。
  - テスト: `Phase10Step4dW1ProfileIdTests`（15 件。保存で先頭に ID、上書きで保つ・別名で新しい ID、名前変更で変わらない、起動時の割り当て［内容を保つ、安定、コピーで付け替え、作成日時が同じなら名前順、書けない・壊れたファイルは報告、フォルダーなし］、`EnsurePersisted`、`TryResolve`、形式が不正な ID、DI の登録と起動の配線）。`Phase10Step4dW0NameKeyTests` の保存形式の検査を、新しい形（プロファイルに ID あり、SA への参照は名前）へ更新した。
  - 実機確認: 台帳 §27 の P10-90（統合版に追加）。先送り。
- **W2（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1940 件と 11 件が成功。実機確認 P10-91 は先送り）**: SA の ID。参照はまだ名前のまま（追加のみ）。
  - 保存形式: `Actions.xml` の `Action` に `Id` 属性（GUID の `N` 形式。`SpecialActionSerializer.Id`）。古いバージョンは、未知の属性として読み飛ばす。メモリでは `SpecialAction.id`（コンストラクターで新しい ID を割り当て、読み込みでファイルの ID に置き換える）。
  - 起動時の一括割り当て（W1 と同じ方針）: `SpecialActionsXmlSerializer.LoadActions` が、ID のない（または不正な）SA に新しい ID を割り当て、重複（ファイルの順で先のものが保ち、後のものに新しい ID）を付け替えて、`Actions.xml` を 1 回保存する。変更がなければ書き換えない。読み込みの `StreamReader` を閉じてから書く。
  - 編集・名前変更での引き継ぎ: `ISpecialActionRepository.SaveAction` に省略可能な `actionId` を追加。8 か所の保存の呼び出しが `actionId: action.id` を渡す。`ActionEditorViewModel.SetAction` が、保存済みの SA の ID を、保存用の `SpecialAction` へ写す。`SaveActionNew` は、編集では同じ名前の SA の ID を、名前の変更では渡された ID を保ち、新規は新しい ID にする。
  - golden: `Actions.xml` の `Id` 属性は、比較の正規化で除いた。状態のダンプ（`BackingStoreStateDumper`）から、`id` を除いた。**`actions.xml` の golden を更新した**: 旧形式の保存（`SpecialActionsXmlSerializer.SaveAction`。テストの準備だけが使う）は、最後に `LoadActions` を呼ぶため、ID の割り当てで `Actions.xml` が DTO の書式で保存し直される。その結果、コメントの後の空行が 1 つ増え、空の `<Arguements>` 要素がなくなった（意図した変更）。
  - テスト: `Phase10Step4dW2ActionIdTests`（12 件。保存で ID、往復、編集で保つ、名前変更で引き継ぐ［渡さなければ新しい ID、不正な ID は無視］、ID のない古いファイルへの割り当てと 1 回の保存、ID があれば書き換えない、重複・不正な ID の付け替え、新規インストールの既定の SA、古い読み方でも読める、編集画面が ID を引き継ぐ、8 か所の呼び出し）。`RecordingSpecialActionRepository` ほか、`ISpecialActionRepository` を実装するテスト用の偽物 3 つに、`actionId` を追加。`Phase10Step4dW0NameKeyTests` の「Actions.xml に ID がない」の検査を削除（W2 のテストへ置き換え）。
  - 実機確認: 台帳 §27 の P10-91（統合版に追加）。先送り。
- **W3（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1956 件と 11 件が成功。実機確認 P10-92 は先送り）**: 保存する参照を ID に（ID を主、名前を併記）。実行時の内部は、まだ名前のまま（W5〜W6）。
  - **`<ProfileActions>`**（プロファイルが使う SA）: 名前の一覧は残し（古いバージョンのため）、並びが同じ SA の固定 ID の一覧 `<ProfileActionIds>`（`/` 区切り）を足す。見つからない SA の ID は空。1 つも ID がなければ、要素を書かない（golden は変更なし）。読み込みは、ID を主にして、現在の SA の名前に引き直す（SA の名前変更に追従）。ない・個数が合わない・SA が見つからないときは、名前のまま。同じ SA に解決された名前は 1 つにまとめる。`ProfileActionsSaveSection`／`ProfileActionsLoadSection`。
  - **SA の Load Profile の切り替え先**: `Actions.xml` の `Action`（種別 Profile）に `ProfileId` 要素（切り替え先のプロファイルの ID）を足す（`Details` は名前のまま）。`SpecialActionsXmlSerializer` は、省略可能な `IProfileIdentityService` を受ける。保存の前に、名前から ID を引く（`EnsurePersisted`）。読み込みでは、ID が見つかれば `details` を現在の名前に置き換える（ID が勝つ）。ID のない古いファイルは、名前から引いた ID を割り当てて、1 回保存する。見つからなければ名前のまま。`SpecialAction.profileId`、`SpecialActionSerializer.ProfileId`。
  - **Controls の Load Profile の切り替え先**: `<Control><Special>` の項目（種別 Profile）に `ProfileId` を足す（`ProfileName` は残す）。`ControlSpecialItem.ProfileId`、`ControlSpecialItemXml`。`LegacyProfileXmlSerializer` は、省略可能な `IProfileIdentityService` を受け、保存の前に名前から ID を引き、読み込みの後に、ID から現在の名前に置き換える（ID が勝つ）。
  - 規則（ユーザー承認）: a＝名前の一覧は残し、ID の一覧を別の要素で足す。b＝ID と名前が食い違うときは ID が勝つ（名前は次の保存で直る）。c＝切り替え先を保存するとき、先に `EnsurePersisted` で ID を確定する。
  - 起動の順序: `EnsureAllPersisted()` を、`LoadActions()` の前（Load Profile の切り替え先を ID で解決するため）へ移した。
  - 追従のコード（`Replace*`／`RemoveActionReference`）は、W7 まで残す（二重の安全策）。
  - テスト: `Phase10Step4dW3ReferenceIdTests`（16 件）。`TestRepositoryFactory` に登録簿の引数を追加。`BackingStoreStateDumper` の除外に `profileId` を追加。`Phase10Step4dW0RuntimeKeyRatchetTests` の基準値を更新（`ProfileActionsSaveSection` の `profileActions` 2 → 3）。
  - 実機確認: 台帳 §27 の P10-92（統合版に追加）。先送り。
- **W4（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1963 件と 11 件が成功。実機確認 P10-93 は先送り）**: 保存する参照を ID に（残り 3 つ）。ID を主、名前を併記。実行時の内部（`ProfilePath[]` ほか）は、まだ名前のまま（W5〜W6）。
  - **スロットの選択（`Profiles.xml`）**: 名前の要素（`Controller1`〜`8`）は残し、同じ並びのプロファイルの ID を `<ControllerProfileIds>`（`/` 区切り。ないスロットは空。1 つも ID がなければ書かない）で足す。読み込みは、ID が見つかれば `profilePath`／`olderProfilePath`／`selectedProfile` を現在の名前に置き換える（ID が勝つ）。保存は、名前から `EnsurePersisted` で ID を引く。`AppSettingsDTO`（`ProfileIdOf`／`ProfileNameOf`）、`AppSettingsXmlSerializer`（省略可能な登録簿）。
  - **リンクプロファイル（`LinkedProfiles.xml`）**: 各 `MAC…` 要素に属性 `Id`（要素の中身の名前は従来のまま。古いバージョンは中身だけを読む）。`LinkedProfileItem.ProfileId`、`LinkedProfilesDTO`（`ProfileIdOf`／`ProfileNameOf`、`ReadXml`／`WriteXml`）、`LinkedProfilesXmlSerializer`。
  - **自動プロファイル（`Auto Profiles.xml`）**: 各 `Program` に `<ControllerProfileIds>`（`Controller1`〜`8` と同じ並び。「(none)」と見つからないものは空）。`AutoProfileEntrySerializer.ControllerProfileIds`、`AutoProfilesDTO`、`AutoProfileHolder`（省略可能な登録簿。`Load` が使うため、コンストラクターで受ける）。
  - DI: 3 つとも具象クラスの Singleton のまま。省略可能な引数に、登録済みの `IProfileIdentityService` が渡る。登録簿がなければ（テスト）、従来どおり名前だけ。
  - 注意: `Profiles.xml` は起動の最初に読むため、プロファイルに ID が書かれる前の最初の起動では ID で解決できず、名前のまま。次の保存から ID が書かれ、以後は ID で解決される。
  - テスト: `Phase10Step4dW4ReferenceIdTests`（7 件。3 つの保存先の往復と名前変更への追従、登録簿なしでは従来の形式、ID のない古いファイルは名前で読む、DI の配線）。`Phase10Step4dW0RuntimeKeyRatchetTests` の基準値を更新（`AppSettingsDTO` の `ProfilePath` 系 16 → 18）。
  - 実機確認: 台帳 §27 の P10-93（統合版に追加）。先送り。
- **W5a（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1972 件と 11 件が成功。実機確認なし）**: 登録簿のメモリ索引。実行時の状態を ID にする（W5b〜W5c）前の土台。挙動は変えない。
  - **W5 の方針（ユーザー決定、2026-10-21）: 案 1**。スロットの保管（`ProfilePath`／`OlderProfilePath`）を ID にし、名前は ID から引く窓口（インデクサー）にする。呼び出し側（約 45 か所）は、名前の読み書きのまま。名前変更で配列を書き換える必要がなくなる。W5a＝登録簿の索引、W5b＝スロットの保管を ID に、W5c＝一時プロファイルの復帰・自動プロファイルの実行経路。
  - 実装: `ProfileIdentityService` が、ID → 名前、名前 → ID の索引を持つ。検索（`GetId`／`GetName`／`EnsurePersisted`／`TryResolve`）は索引から引く。鮮度は、プロファイルのフォルダーの最終更新時刻で判定する（追加・削除・名前変更・コピーは、次の検索で反映）。フォルダーのパスが変わったときも作り直す。フォルダーの時刻が 1 秒以内に新しいあいだは、ファイルシステムの時刻の粗さで直後の変更を見逃さないよう、毎回作り直す。アプリ自身の書き込み（`EnsurePersisted`、`EnsureAllPersisted`）は、その場で索引を更新する。排他は `XmlIoLock` → 索引のロックの順。
  - 制約: ファイルの中身だけを外から書き換えた場合（ID の手編集）は、次にフォルダーが変わるまで反映されない（`Refresh()` で明示的に作り直せる）。
  - インターフェース: `IProfileIdentityService` に `Refresh()` を追加。
  - テスト: `Phase10Step4dW5aIdentityIndexTests`（9 件。名前変更・追加・削除の反映、名前は大文字小文字無視・ID は区別、フォルダーが変わらないあいだは索引を使う、変わったら作り直す、速度［40 ファイル × 4000 回が 2 秒以内］、`EnsurePersisted`／`EnsureAllPersisted` の索引への反映、保存場所の変更）。W1 の登録簿のテストは変更なしで通る。
- **W5b（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1983 件と 11 件が成功。実機確認 P10-94 は先送り）**: スロットの保管を ID に（案 1）。
  - 新しい型 `ProfileSlotNames`（`DS4Control/`）: スロットごとの「ID と名前の控え」を持つ、名前を読み書きする窓口。`BackingStore` の `profilePath`（現在）、`olderProfilePath`（直前）、`selectedProfile`（選択）の型（以前は `string[]`）。書くと、名前の控えを保存し、登録簿から ID を引いて保存する（見つからない名前・空・「(none)」は、ID なし）。読むと、ID が引ければ現在の名前を返す（名前を変えても、保管を書き換えなくてよい）。引けなければ名前の控え（削除されたプロファイルは、最後の名前が残る。従来と同じ）。ID がない・無効になったときは、読むときに名前から ID を引き直す（あとで作られたプロファイル、同じ名前で作り直したプロファイルに結ぶ）。登録簿がなければ（テストの新しい `BackingStore`）、従来の配列と同じに動く。
  - 結び: `BackingStore.AttachProfileIdentity`。DI の登録が、登録簿を作るときに結ぶ（登録簿が `BackingStore` に依存しない向き）。
  - `IProfileRepository`／`ProfileRepository` の `ProfilePath`／`OlderProfilePath`／`SelectedProfile` の型を `ProfileSlotNames` に替えた。呼び出し側の本体は、ほとんど変更なし（インデクサーの読み書きのため。`App` の 2 行のみ）。
  - 登録簿の排他: 索引が有効なら、`XmlIoLock` を取らず、索引のロックだけで引く（実行時の読み取りが、長い XML の読み書きを待たない）。作り直すときだけ `XmlIoLock` → 索引のロックの順。
  - テスト: `Phase10Step4dW5bProfileSlotNamesTests`（11 件。登録簿なしは配列と同じ、書くと ID・名前変更に追従、未登録・空は名前のまま、削除で最後の名前、あとから作られた・作り直したプロファイルへの結び、結びの付け外し、`BackingStore` の 3 つの配列、DI の結び、XML の排他を待たない、並行アクセス）。`BackingStoreStateDumper` は `ProfileSlotNames` を配列として出力する（golden は変更なし）。`ControlServicePr4ShimEquivalenceTests`／`Phase6Step10GroupThreeBContractTests` の型を更新。ラチェットの基準値を更新（`BackingStore` 7 → 9）。
  - `MainWindow` の名前変更の再構築（`ProfilePath[i] = newProfile` など）は、まだ残す（W7 で撤去）。残しても、窓口の書き込みが同じ ID を引くため、結果は変わらない。
  - 実機確認: 台帳 §27 の P10-94（統合版に追加）。先送り。
- **W5c（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1990 件と 11 件が成功。実機確認 P10-95 は先送り）**: 切り替え・復帰の実行経路を ID に（W5 の範囲 1・2。ユーザー承認）。実行時に名前を持つ残りの箇所のうち、自動プロファイルのルール（`AutoProfileEntity.ProfileNames`）とリンクプロファイル（`BackingStore.linkedProfiles`、`linkedProfileUI`）は **W5d** に分けた（一覧と画面に広がる別の性質の変更のため）。
  - 一時プロファイルの名前: `ProfileSettingsService` の `_tempProfileName` を `ProfileSlotNames`（保管は ID の窓口）にした。`GetTempProfileName`／`SetTempProfileName` の外向きの形は変えない。登録簿は、DI の登録（登録簿を作るとき）が `AttachProfileIdentity` で結ぶ（`BackingStore` と同じ）。
  - SA の Load Profile の実行時の解決: `ProfileApplicationService.ApplyFromAction` が、切り替えの直前に、`SpecialAction.profileId`（W3 で持たせた切り替え先の ID）から現在の名前を引く（`ResolveTarget`。引けたら `details` も直す）。引けない・ID がないときは、従来どおり `details`。コンストラクターに、省略可能な `IProfileIdentityService`（11 個目）を足した。
  - Controls の Load Profile: 合成 SA（`ControlBindingBuilder.CreateSpecialIdentity`）に `ControlSpecialItem.ProfileId` を `profileId` として渡す。実行の直前の解決は、SA と同じ経路。
  - これで、プロファイルの名前を変えても、SA の定義や束縛を作り直さなくても、押したときの切り替えが新しい名前で動く。追従のコード（`ReplaceProfileSwitchTarget`、`ReplaceControlProfileSwitchTarget` ほか）は、W7 まで残す（二重の安全策）。
  - テスト: `Phase10Step4dW5cRuntimeSwitchTests`（7 件。一時プロファイルの名前が名前変更に追従［登録簿あり］・登録簿なしは従来どおり・DI の結び、切り替え先の解決［名前変更後は現在の名前、ID なし・引けない・登録簿なしは名前］、Controls の合成 SA が ID を持つこと、`ApplyFromAction` が解決した名前を使うこと）。`Phase7Step10bW3ContractCleanupGuardTests` のコンストラクターの引数の数を更新（10 → 11）。
  - 実機確認: 台帳 §27 の P10-95（統合版に追加）。先送り。
- **W5d（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1998 件と 11 件が成功。実機確認 P10-96 は先送り）**: 自動プロファイルのルールとリンクプロファイルの、実行時の保持を ID に（W5 の範囲 3・4）。これで、**実行時にプロファイルの名前を持つ箇所は、すべて ID を保管する窓口になった**。
  - 自動プロファイル: `AutoProfileEntity.ProfileNames` を `ProfileSlotNames` にした（以前は `string[]`）。登録簿は、`AutoProfileHolder` が、ルールが入るとき（`CollectionChanged`。ファイルの読み込み、画面からの追加）に `AttachProfileIdentity` で結ぶ（`Load` より前にフックする）。ホルダーに入る前に設定した名前も、入るときに ID に結ばれる。
  - リンクプロファイル: `BackingStore.linkedProfiles` を新しい型 `LinkedProfileMap` にした（以前は `Dictionary<string, string>`）。使い方は辞書と同じ（インデクサー、`ContainsKey`、`Remove`、`TryGetValue`、`Count`、`Keys`［写し］、列挙）で、値は名前を読み書きする窓口（保管は ID）。`BackingStore.linkedProfileUI`（画面表示用）は `ProfileSlotNames`。`IProfileRepository.LinkedProfileUI` の型を替えた。`BackingStore.AttachProfileIdentity` が、5 つ（現在・直前・選択・リンク・リンクの表示用）をまとめて結ぶ。
  - 呼び出し側の本体は、ほとんど変更なし（インデクサーの読み書き。`AutoProfileHolder.ReplaceProfileName` と `ProfileRepository.ReplaceLinkedProfileName` の型の宣言のみ）。
  - テスト: `Phase10Step4dW5dTests`（8 件。`LinkedProfileMap` が辞書と同じに動く・名前変更に追従・削除で最後の名前・結びの付け直し、`BackingStore` のリンクと表示用の追従、`ProfileRepository.GetLinkedProfile` が `ReplaceLinkedProfileName` なしで現在の名前を返す、自動プロファイルのルールの追従［ホルダーに入った後、ファイルから読み込んだ後］、ホルダーの外・登録簿なしは名前のまま）。`BackingStoreStateDumper` は `LinkedProfileMap` を辞書として出力する（golden は変更なし）。
  - 追従のコード（`ReplaceLinkedProfileName`、`AutoProfileHolder.ReplaceProfileName`、`ReplaceProfileSwitchTarget` ほか）は、W7 で撤去するまで残す（二重の安全策）。
  - 実機確認: 台帳 §27 の P10-96（統合版に追加）。先送り。
- **W6'（決定 2026-10-21。コードの変更なし）**: SA 側の実行時の状態は、名前のまま残す（ユーザー決定。当初の W6［ID に替える］から見直し）。理由（調査の結果）:
  1. `ActionManager` の登録簿（`ActionInstanceState` の鍵）は、SA の一覧を読み込み直すたびに（SA の保存、名前変更、削除のたびに）全エントリーを破棄する（`ClearAllEntries`）。名前を変えると必ず読み込み直しになるため、古い名前の状態が残って困る場面がない。Controls の合成 SA（`Synthetic_*`）は保存する ID を持たず、束縛を作り直すたびに新しい `SpecialAction` ができるため、ID を鍵にすると、作り直しのたびに状態（トグルなど）が引き継がれなくなる危険がある（名前が鍵だから引き継がれている）。
  2. `profileActions[device]`（`List<string>`）は約 47 か所が `List<string>` の API で使い、`List<string>[]` を受け渡す契約・テストが約 10 か所ある。ID の窓口にするには `IList<string>` を実装する新しい型が要る。しかも、SA を編集すると（名前が同じでも）、実行中のスロットが持つ `profileActionDict`（名前 → 古い定義）は古いままなので、プロファイルの再適用が必要。これは ID にしても変わらず、再適用が不要になるわけではない。
  3. 保存する参照は、W3 で ID が主になっている（`<ProfileActionIds>`）。SA の名前を変えたあと、プロファイルを再適用すれば、ファイルの `<ProfileActions>` を書き換えなくても、新しい名前で読み込まれる。
  - 帰結: W7 で、SA の名前変更の追従のうち、全プロファイルの XML の書き換え（`ReplaceActionReference`）を撤去する（再適用は残す）。削除（`RemoveActionReference`）は、古い参照の片付けとして残す。
  - ラチェット（`profileActions` 系の件数）は、W6' では動かさない（基準値は現状のまま）。
- **W7（完了 2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1998 件と 11 件が成功。実機確認 P10-97 は先送り）**: 追従のコードの撤去（案 A。ユーザー決定）。実行時に不要になったものだけを撤去し、ファイルを書き換える 2 つは、ID のない古いプロファイルのファイルのために残した。
  - 調査（名前変更の経路 `MainWindow.SyncProfileListAndControllers`）: スロットの配列の書き換えは、名前変更の直前にファイルを移動するため、窓口が新しい名前を返し、何もしない処理になっていた。リンク・自動プロファイル・SA の切り替え先の実行時の追従は、W5c〜W5d で自動になった。ファイルの名前を更新する役目だけが残っていた。
  - 撤去: `MainWindow` の、スロットの配列（`ProfilePath`／`OlderProfilePath`）の書き換え。`IProfileRepository.ReplaceLinkedProfileName`（と実装）。`AutoProfileHolder.ReplaceProfileName`。
  - 替えたもの: 名前変更の直後に、`SaveLinkedProfiles()`、`AutoProfileHolder.Save()`（既定の保存先の新しいオーバーロード）、`appSettingsService.Save()` で、保存先のファイルの名前と ID を書き直す。`ReplaceProfileSwitchTarget` は `RefreshProfileSwitchTargets(oldName, newName)` に替えた（`SpecialActionsXmlSerializer.RefreshProfileTargets`。切り替え先の ID から現在の名前に引き直し、Actions.xml を保存する。ID のない SA は、旧名との一致で置き換える）。
  - 残したもの（TODO 付き）: `ReplaceControlProfileSwitchTarget`（各プロファイル XML の Controls の Load Profile の書き換え）、`ReplaceActionReference`（SA の名前変更での、各プロファイル XML の `<ProfileActions>` の書き換え）、`RemoveActionReference`（SA の削除の片付け）。理由: W3 より前に保存されて、まだ保存し直していないプロファイルのファイルには、ID（`ProfileId`、`<ProfileActionIds>`）がなく、撤去すると、名前変更でその参照が切れる。**撤去の条件**: 全プロファイルのファイルへ ID を書き足す移行（起動時の一括書き込み）を用意するか、保存し直されることを確認したとき。
  - テスト: `Phase10Step4dW7FollowUpTests`（6 件。撤去したメンバーがないこと・残したものがあること、名前変更後の保存でリンク・自動プロファイルが新しい名前と同じ ID を書くこと、`RefreshProfileTargets`［ID から引き直す・ID のない SA は旧名で置き換える］、`MainWindow` の名前変更の共通の経路）。撤去したメンバーの旧いテスト（`Phase10Step4dW0NameKeyTests`、`Step10FourTwoGlobalReferenceGuardTests` の 3 件）を削除。`ProfileRenamePropagationTests`、`Phase10Step4W6ControlProfileTests`、`Phase8Step6W1DisconnectCharacterizationTests`（`olderProfilePath` を参照するファイルの一覧から `MainWindow` を除く）、ラチェット（`MainWindow` 6 → 4）を更新。
  - 実機確認: 台帳 §27 の P10-97（統合版に追加）。先送り。
- **最終 W（完了 2026-10-21。文書のみ）**: 完了の記録（本節）、`Phase10-Status.md`、モデル図 `03-Class-Interface-Diagram.md`（Step4d の注釈とクラス）、`Phase10-Integration-Target-Model.md`（Step4d の結果と引き継ぎ。§9）、台帳（§27 の導入）を更新した。
  - **Step4d の結果（保存する参照と、実行時の保持は、固定 ID が主になった）**:
    - 固定 ID: プロファイル XML のルート直下の先頭に `<ProfileId>`（W1。起動時に全ファイルへ一括で書く）、`Actions.xml` の `Action` の `Id` 属性（W2。同じく起動時）。GUID の `N` 形式。名前を変えても変わらない。
    - 保存する参照（ID を主、名前を併記。ID が勝つ。見つからなければ名前。W3〜W4）: プロファイルが使う SA（`<ProfileActionIds>`）、SA の Load Profile（`ProfileId`）、Controls の Load Profile（`ProfileId`）、スロットの選択（`<ControllerProfileIds>`）、リンクプロファイル（`MAC…` の `Id` 属性）、自動プロファイル（`<ControllerProfileIds>`）。
    - 登録簿 `IProfileIdentityService`（W1、W5a）: ID ↔ 名前。メモリの索引（フォルダーの最終更新時刻で鮮度を判定）。
    - 実行時の保持（W5b〜W5d）: 保管は ID で、名前を読み書きする窓口。`ProfileSlotNames`（スロットの現在・直前・選択、一時プロファイル、リンクの表示用、自動プロファイルの各コントローラー）、`LinkedProfileMap`（リンク）。名前を変えても、読み出す名前が自動で現在の名前になる。Load Profile は、実行の直前に ID から現在の名前を引く（W5c）。
    - SA 側の実行時の状態は、名前のまま（W6' の決定。理由は §8）。
    - 追従のコード: 実行時に不要になったもの（スロットの配列の書き換え、`ReplaceLinkedProfileName`、`AutoProfileHolder.ReplaceProfileName`）を撤去し、保存先のファイルは、名前変更の直後に保存し直す（W7）。`ReplaceControlProfileSwitchTarget`、`ReplaceActionReference`、`RemoveActionReference` は、ID のない古いプロファイルのファイルのために残した（TODO 付き）。
  - **残る課題（TODO）**: 古いプロファイルのファイルへ ID を書き足す移行（起動時の一括書き込み）を用意できたら、`ReplaceControlProfileSwitchTarget`／`ReplaceActionReference` を撤去する。W2 以降の golden の正規化（`<ProfileId>`、`Id` 属性）は、そのまま。
  - 実機確認 P10-90〜97 は先送り（統合 Step の後に回帰として確認）。
