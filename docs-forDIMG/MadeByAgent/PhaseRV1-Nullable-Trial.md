# PhaseRV1: Nullable の試験導入の記録（RV1-3）

作成日: 2026-11-19
目的: 層ごとの全体の有効化（PhaseRV9。予定）の手間を、小さな範囲で実測し、計画書の根拠にする。D2＝(B)（層ごとに進める）。

## 1. 範囲と方法

- 範囲: `Core/Abstractions`（37 ファイル）と `Composition`（19 ファイル）の、全 56 ファイル。契約と組み立てで、値の入れ物より警告が少ない見込みの層を選んだ（RV0 で、プロジェクト全体を `Nullable=enable` にして測った結果は、`Core/Abstractions` 2 件、`Composition` 24 件）。
- 方法: プロジェクトの設定は変えず、各ファイルの先頭（ライセンスのコメントの後、最初の `using` の前）へ `#nullable enable` を足した（BOM と改行は保つ）。ビルドして、`CS86xx`・`CS87xx` の警告を 0 にした。
- 検査: Debug ALL（警告 0・エラー 0・全テスト）。

## 2. 結果

| 項目 | 値 |
|---|---|
| 最初の警告（重複を除く） | 26 件（`Composition` 24 件・`Core/Abstractions` 2 件）。8 ファイル |
| 直したあとに新しく出た警告 | 1 件（`AppHost` の引数を `?` にしたら、呼び先の `AddWiredServices` の引数が警告になった。連鎖） |
| 直した合計 | 27 件・9 ファイル（`AppHost`、`ServiceRegistration`、`ServiceRegistration.Wiring`、`BuildStampReader`、`CompleteInstallArguments`、`SettingsLocationFallback`、`SingleInstanceGuard`、`IProfileRepository`、`ISpecialActionRepository`） |
| ビルドの回数 | 3 回（注釈を足す前に 1 回、直した後に 2 回。1 回 約 45 秒） |
| 動作の変更 | なし（注釈のみ。例外は、下の「手を入れた」3 か所） |

### 直し方の型

| 型 | 件数 | 直し方 | 例 |
|---|---|---|---|
| 既定値の `null` を持つ引数・`out` 引数 | 9 | 型に `?` を足す（呼び先の引数が連鎖して警告になったもの 1 件を含む） | `BackingStore? existingStore = null`、`out string? source` |
| `null` を返すメソッド | 7 | 戻り値の型に `?` を足す | `string? FromAssembly(...)`、`T? GetService<T>()` |
| あとから代入するフィールド・プロパティ | 6 | 型に `?` を足す | `IHost? _host`、`string? CreationNote`、`Thread? _listenerThread` |
| 標準ライブラリが `null` を返しうる値 | 5 | 受ける変数に `?` を足す／`!`（理由のコメントつき。1 か所で 2 件） | `string? timestamp`、`out EventWaitHandle? tempComEvent`、`Path.GetDirectoryName(...)!` |

手を入れた（注釈だけではない）3 か所:
- `SingleInstanceGuard.ListenerWork`: フィールドを `?` にした結果として、フィールドを手元の変数へ写して、`null` ならスレッドを終える（`StartListening` が設定してから始まるため、実際には起きない）。
- `SingleInstanceGuard.Stop`: `_listenerThread.IsAlive` を `_listenerThread?.IsAlive == true` にした（`_comEvent != null` のときは必ず設定済みで、同じ動き）。
- `SettingsLocationFallback.OfferCopyToAppData`: `Path.GetDirectoryName(...)!`。理由はコメントに書いた（実行ファイルのフルパスで、親フォルダーは `null` にならない）。

## 3. 分かったこと（PhaseRV9 の計画の根拠）

1. **ファイル単位の `#nullable enable` で、層ごとに進められる**。プロジェクトの設定（`<Nullable>`）は、最後の層が終わってから 1 回だけ変える。途中の層の呼び出し側（まだ `Nullable` を有効にしていないファイル）は、警告が出ないので、順序の制約が少ない。
2. **下の層から有効にすると、連鎖が少ない**。`AppHost` の引数を `?` にしたら、呼び先の引数（同じ層）に 1 件の連鎖が出た。上の層から有効にすると、下の層の `?` のない戻り値に、多くの警告が出る。
3. **大半は、`?` を足すだけで済む**。`!` は 1 か所（2 件分）だけで、理由をコメントに書いた。`!` を警告の抑止に使わない（RV9 の規則にする）。
4. **件数は、層ごとに大きく違う**。RV0 の測定（プロジェクト全体）: `Core/Abstractions` 2、`Core/Profiles` 6、`Core/Updater` 7、`Core/Model` 10、`Core/Logging` 18、`Core/Utilities` 23、`Composition` 24、ルート 38、`Core/Platform` 69、`Input` 84、`Output` 98、`Core/Settings` 110、`Processing` 300、`Core/Persistence` 675、`UI` 684（合計 2,153 件）。`Core/Persistence`（XML の読み書き。`SelectSingleNode` の戻り値が多い）と `UI` が、全体の 6 割を占める。
5. **試験の範囲では、1 件あたりの手間は小さい**（注釈 1 つ＋ビルド）。`Core/Persistence` のような、同じ型の警告が大量に出る層は、共通の読み取り部品（拡張メソッド）に `?` を付ければ、まとめて減る見込み（RV9 の最初に確かめる）。

## 4. PhaseRV9 への推奨

- 順序: `Core/Abstractions` ✔（本書）→ `Composition` ✔（本書）→ `Core/Model`・`Core/Logging`・`Core/Utilities`・`Core/Updater`・`Core/Profiles`（件数が少ない）→ `Core/Platform` → `Input` → `Output` → `Core/Settings` → `Processing` → `Core/Persistence` → `UI` → 最後に `<Nullable>enable</Nullable>`。
- 層ごとにコミット（1 層 1 Step）。各 Step は、`#nullable enable` の追加→警告 0→Debug ALL。
- 検査: `CodeHealthRatchetTests` に「`#nullable enable` を持つファイルの数」を足し、減らさない（増える方向のラチェット）。最後に、プロジェクトの設定で全体を有効にしたら、この項目は不要になる。
- 評価（`Score-CodeReview.py`）の指標 `nullable_layers_enabled`（`Rubric-Inputs.json`）は、本書の 2 層を数えて 2 とする。
