# Phase6-Step10-3 計画書: グループ3【スペシャルアクション系】の View／ViewModel ペア Pure DI 化

作成日: 2026-10-14  
状態: 計画案。決定 G3-1〜G3-4 は、すべて推奨案で承認済み（2026-10-14）。実装未着手  
上位計画書: `Phase6-Step10-Plan.md`（§0A.4 が本 Step の台帳）、前の Step: `Phase6-Step10-2-Plan.md`（完了）  
準拠: `.github/copilot-instructions.md` §2.1〜2.4、§3.1、§3.3、§3.4

---

## 1. 目的
- スペシャルアクションの編集画面と一覧、各アクション種別の ViewModel について、`Global` 直接参照と、`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックを解消する。
- 決定3（View の依存は親から必須引数）、決定1（不変の静的値と純粋関数は温存）に従う。
- 動作は変えない。新しい契約は `Global` への薄い委譲にし、契約テストで同じ動作を固定する。

## 2. 対象（現行コード 2026-10-14 の再確認）

### 2.1 View
- `SpecialActionEditor.xaml.cs`:
  - `Global.RemoveAction`×1（名前を変えて保存するとき、旧名のアクションを削除する）。
  - `AppHost.GetService<IViewModelFactory>()`×1（`null` のとき `new SpecialActEditorViewModel` へ後退）。
  - 種別ごとの ViewModel を `new` で 9 個生成している（`MacroViewModel`、`LaunchProgramViewModel`、`LoadProfileViewModel`、`PressKeyViewModel`、`SpecialActionViewModel`×3、`CheckBatteryViewModel`、`MultiActButtonViewModel`）。

### 2.2 ViewModel
- `SpecialActEditorViewModel`: `Global.GetActions()`×1（名前の重複検査）。
- `SpecialActionsListViewModel`:
  - `?? AppHost.GetService…?? Global.*Instance` のフォールバック 5 件。引数はすべて任意（`= null`）。
  - `Global.GetActions()`×1、`Global.RemoveAction`×1（いずれも `specialActionRepo` が null のときだけ通る後退）。
  - `actionManager` フィールド: 宣言と代入のみで、読み取りが無い（未使用）。
- `SpecialActions/*`（種別ごとの保存）:
  - `Global.SaveAction`×9（`CheckBattery`、`LaunchProgram`、`LoadProfile`、`Macro`、`MultiActButton`、`PressKey`×2、`SpecialActionViewModel`）。
  - `Global.configFileDecimalCulture`×3（`CheckBattery`、`LaunchProgram`、`SpecialActionViewModel`）。
  - `PressKeyViewModel`: `Global.OutContType`×3、`Global.outDevTypeTemp`×1、`Global.getX360ControlString`×7。
- 生成箇所: `ProfileEditor.xaml.cs:329` が `new SpecialActionsListViewModel(deviceNum)`（引数 1 個。残りはフォールバックで解決）。

## 3. 現行コードの調査結果（設計に影響するもの）
- **`Global.SaveAction` は、`ISpecialActionRepository` の既存メソッドでは置き換えられない。**
  - `Global.SaveAction` は、`ActionManager.ClearAllEntries()` → `SaveActionNew`（`Actions.xml` へ書き込み）→ `Actions.xml` の読み直しと件数の検証 → キー種別（`mode == 4`）のとき `Mapping.ResetRuntimeStateForKey` を行う。
  - 既存の `AddAction`／`ReplaceAction` は、メモリ上のリストを変えて通知を出すだけで、ファイルへ保存しない。置き換えると、保存されなくなる。
- **`Global.RemoveAction` と `ISpecialActionRepository.RemoveAction` は、別の動作。**
  - `Global.RemoveAction`: `ActionManager.ClearAllEntries()` → `Actions.xml` から削除して保存 → 読み直しと検証。
  - `ISpecialActionRepository.RemoveAction`: メモリ上のリストから外して通知を出すだけ（ファイルは書かない）。
  - `SpecialActionsListViewModel.RemoveAction`（一覧の削除）は、すでにリポジトリ側（メモリのみ）を呼んでおり、`Global.RemoveAction` は後退の分岐にしか無い。編集画面の「名前変更時の旧名削除」だけが `Global.RemoveAction` を使っている。
- **`Global.GetActions()` と `ISpecialActionRepository.ActionList` は同じリスト**（`Global.store.actions`。`SpecialActionRepository` は `Global.store` を保持）。契約の追加は不要。
- **`configFileDecimalCulture` は、`new CultureInfo("en-US")` を代入したまま変更されない静的値**（不変データ）。台帳（§0A.4）で温存と決めている。
- **`getX360ControlString` は、ボタン名を返す純粋関数**（出力コントローラー種別の引数あり／なしの 2 通り）。温存（`MappingListViewModel` と同じ扱い）。
- **`IProfileSettingsService.OutContType`** と **`IOutputSlotService.OutDevTypeTemp`** は契約として存在する（`Global.OutContType`／`Global.outDevTypeTemp` と同じ配列）。契約の追加は不要。
- **`IViewModelFactory` に `SpecialActionsListViewModel` の生成口が無い。** `ViewModelFactory` は、一覧に必要なサービス（`_actionRepo`、`_profileRepo`、`_outputSlotService`、`_profileSettings`）をすでに保持している。
- **テストが `SpecialActionsListViewModel` の任意引数を使っている**: `ProfileActionsSyncTests`（`(0)`）、`ProfileEditorBackendViewModelTests`（`profileRepo:` 名前付き）、`Specialactionslistviewmodelsorttests`（`(0, null, null, null, null, null)`）、`SpecialActionsListViewModelTests`（`actionManager` を位置引数で渡している）。引数を必須化する場合、これらを同じ Step で更新する。

## 4. 決定が必要な事項

### G3-1: 種別ごとの保存（`Global.SaveAction` の置換先）
- 案A（推奨）= `ISpecialActionRepository` に `SaveAction(name, controls, mode, details, edit, delayTime = 0.0, extras = "")` を追加する。実装は `Global.SaveAction` への薄い委譲（上記の保存・読み直し・ランタイム状態のリセットをそのまま維持）。契約テストで、保存後の `ActionList` の内容が `Global.SaveAction` と同じになることを固定する。
- 案B = `AddAction`＋`SaveActions` の組み合わせで置き換える。契約の追加は不要だが、`ActionManager.ClearAllEntries`、読み直しと検証、`ResetRuntimeStateForKey` が失われ、動作が変わる（§2.2 の機能維持に反する）。非推奨。

### G3-2: 名前変更時の旧名アクションの削除（`Global.RemoveAction` の置換先）
- 案A（推奨）= `ISpecialActionRepository` に、ファイルへの保存と読み直しを含む削除を表す別メソッドを追加する（仮称 `RemoveActionAndSave(actionName)`）。実装は `Global.RemoveAction` への薄い委譲。既存の `RemoveAction`（メモリのみ）は変えない。契約のコメントで、両者の違いを明記する。
- 案B = 既存の `RemoveAction` ＋ `SaveActions` で置き換える。ただし `ActionManager.ClearAllEntries` と読み直しの検証が失われ、動作が変わる。非推奨。
- 備考: 一覧画面の削除（`SpecialActionsListViewModel.RemoveAction`）は、現行どおり既存の `RemoveAction`（メモリのみ）を呼び続ける。その後の保存の経路は変えない（置換前からの動作）。

### G3-3: `SpecialActionsListViewModel` の生成と、未使用の `actionManager`
- 案A（推奨）:
  - `IViewModelFactory` に `CreateSpecialActionsListViewModel(int deviceNum)` を追加する（`ViewModelFactory` が保持するサービスを渡す）。`ProfileEditor` は、すでに `IViewModelFactory` を `AppHost` 経由で使っているため、同じ取得箇所で生成する。
  - コンストラクタの 5 つの依存（`ISpecialActionRepository`、`IProfileRepository`、`IOutputSlotService`、`IProfileSettingsService` ＋ `deviceNum`）を必須引数にし、フォールバックを削除する。
  - 未使用の `actionManager`（フィールドと引数）は、`ControllersViewModel`（F09）と同じ扱いで削除する。§2.4 に従い経緯を確認済み（`6da199d7` で引数として追加されたが、一度も読み取られていない。「使うべきもの」の根拠も計画書に無い）。
- 案B = `actionManager` を温存し、TODO コメントを付けて Step12 へ送る。引数の数は変わらないが、未使用の依存が残る。

### G3-4: 種別ごとの ViewModel への依存の渡し方
- 案A（推奨）= `ISpecialActionRepository`（保存）を、各 ViewModel（`MacroViewModel`、`LaunchProgramViewModel`、`LoadProfileViewModel`、`CheckBatteryViewModel`、`MultiActButtonViewModel`、`SpecialActionViewModel`、`PressKeyViewModel`）のコンストラクタの必須引数にする。`PressKeyViewModel` は、さらに `IProfileSettingsService`（`OutContType`）と `IOutputSlotService`（`OutDevTypeTemp`）を受け取る。`SpecialActionEditor` が `new` で生成するときに渡し、`SpecialActionEditor` は `EditorWindowFactory` から必要なサービスを必須引数で受け取る（Step10-2-4a の窓口の拡張）。
- 案B = 種別ごとの ViewModel を `IViewModelFactory` で生成する。生成口が 9 個増え、契約が肥大する。非推奨。

## 5. 契約の追加（`Global` への薄い委譲。契約テストで固定）
- `ISpecialActionRepository.SaveAction(...)`（G3-1＝A の場合）。
- `ISpecialActionRepository.RemoveActionAndSave(actionName)`（仮称。G3-2＝A の場合）。
- `IViewModelFactory.CreateSpecialActionsListViewModel(int deviceNum)`（G3-3＝A の場合）。契約を実装するテスト用クラスの有無を確認し、あれば更新する。

## 6. マイクロステップ（案）
各ステップのあと、ビルド・テスト・実機確認の結果を待ってから次へ進む。

### Step10-3-1: 契約の追加と契約テスト
- 5 章の契約を追加し、`Global.SaveAction`／`Global.RemoveAction` と同じ動作であることを契約テストで固定する（一時ディレクトリの `Actions.xml` を使い、`Global.store` の保存先を退避・復元する）。

### Step10-3-2: 種別ごとの ViewModel と `SpecialActEditorViewModel`
- `SpecialActions/*` の 7 クラスと `SpecialActEditorViewModel` を、必須引数＋薄い契約へ置換する（`configFileDecimalCulture`、`getX360ControlString` は温存）。
- `SpecialActionEditor` の `new` を更新する（この時点では、サービスを `EditorWindowFactory` 経由で渡す変更は 3-4 と一体のため、3-2 と 3-4 の境界は実装時に調整する）。

### Step10-3-3: `SpecialActionsListViewModel`、`IViewModelFactory`、`ProfileEditor`
- 一覧 ViewModel の依存を必須引数へ。`actionManager` の削除。`ViewModelFactory`／`ProfileEditor` の更新。4 件のテストファイルを更新する。
- 実機確認: プロファイル編集画面のスペシャルアクション一覧の表示、並べ替え、有効／無効の切り替え、削除。

### Step10-3-4: `SpecialActionEditor` と `EditorWindowFactory`
- `SpecialActionEditor` のコンストラクタへ必要なサービスを必須引数で追加し、`AppHost` と `new SpecialActEditorViewModel` の後退を削除する。`Global.RemoveAction` を置換する。`EditorWindowFactory` が渡す。
- 実機確認: 新規作成と編集で、各種別（マクロ、プログラム起動、プロファイル切替、キー押下、切断、バッテリー確認、複数アクション、ステアリングホイール校正、ジャイロ校正）の保存。名前を変えて保存したとき、旧名が残らないこと。

### Step10-3-5: ガードテストと文書
- `Step10GroupThreeGlobalReferenceGuardTests`（新規）: 置換済みのファイルに、温存項目以外の `Global.`、`AppHost`、`Program.rootHub` が無いこと、必須引数の null 拒否。
- `Phase6-Status.md`、`Phase6-Step10-Plan.md`、モデル図（03 のクラス図、04 の §2）を更新する。

## 7. 実機確認（案。確認できない項目は `Phase6-Step11-Plan.md` §3.3 へ登録する）
- 上記の各ステップの項目に加え、全体の確認として、スペシャルアクションの新規作成 → プロファイルへの紐付け → 実機でのトリガー発火 → 名前変更 → 削除。
- 実機を持たない種別（該当なし。すべて DS4 で確認可能）。

## 8. リスク
- **`SaveAction` の副作用**: `ActionManager.ClearAllEntries` と `Mapping.ResetRuntimeStateForKey` を含む。薄い委譲にして、順序と内容を変えない。
- **テストの引数変更**: 任意引数の使用箇所が 4 ファイルある。同じ Step で更新する。
- **`SpecialActionEditor` のコンストラクタの引数の増加**: 窓口の `EditorWindowFactory` に集約して、呼び出し側（`ProfileEditor`、`SpecialActionEditor`）の変更を増やさない。
- **一覧の削除の保存経路**: 現行の `RemoveAction`（メモリのみ）の後の保存は、置換前から変わらない。実機確認で、削除後に保存して再読み込みしても削除されていることを確認する（確認の結果、置換前からの不具合が見つかった場合は、修正の可否を確認する）。

## 9. 完了条件
- 対象ファイルに、温存項目（定数、不変の静的値、純粋関数）を除く `Global.`、`AppHost.GetService`、`Program.rootHub` が無い。
- 追加した契約の契約テストとガードテストが通り、`dotnet test` が全件合格する。
- 実機確認の結果が記録され、確認できない項目が先送り台帳へ登録されている。

---

## 10. 決定の記録（2026-10-14）
- G3-1＝A、G3-2＝A、G3-3＝A、G3-4＝A（すべて推奨案で承認）。
- 追加の要望（名前変更・削除を全プロファイルへ反映し、接続中のコントローラーへ再適用する）は、**R3＝B** により、Step10-3 の完了後に別の機能追加として実施する。計画は `Phase6-Step10-3b-Plan.md`。Step10-3 では、G3-2 の `RemoveActionAndSave` を追加するだけで、参照の書き換えは行わない。

## 11. 進捗（2026-10-14）
- **Step10-3-1（契約の追加と契約テスト）**: 実装済み・ビルド、テスト、実機確認の結果待ち。
  - `ISpecialActionRepository`: `SaveAction`（`Global.SaveAction` への薄い委譲）、`RemoveActionAndSave`（`Global.RemoveAction` への薄い委譲。削除前に存在したかを返す）を追加。実装は `SpecialActionRepository`。契約のコメントに、メモリ上のリストだけを変える `AddAction`／`RemoveAction` との違いを明記した。
  - `IViewModelFactory.CreateSpecialActionsListViewModel` は、一覧 ViewModel のコンストラクタ変更と同時に 3-3 で追加する（契約だけ先に足すと、未使用の契約が残るため）。
  - テスト: `Phase6Step10GroupThreeContractTests`（保存、編集、`Global.SaveAction` との一致、保存付き削除、存在しない名前、メモリのみの `RemoveAction` との違い）。一時ディレクトリの `Actions.xml` を使い、`Global.store` の保存先とアクション一覧を退避・復元する。
