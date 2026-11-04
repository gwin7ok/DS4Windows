# Phase14b 個別計画書: 依存の向きの是正（第1弾: 物差しと即効の整理）

作成日: 2026-11-05
状態: **承認済み（2026-11-05。D1〜D5 はすべて推奨案）→ 実施中**
ブランチ: `Phase14b`（master から作成。D1）
根拠: `Repository-Review-2026-11-05.md`（R-01・R-03・R-04）、規約 §1（依存の向き）、§2.3（ログ）、§3.1（ラチェットの一致検査）、§3.3 原則3（`using` は使う名前空間だけ）、§3.4 R2・R5・R7、モデル図 `Model-Diagram/02-Layer-Architecture-Diagram.md`

## 1. 目的

- **R-01**: 層の依存の向き（規約 §1、§3.4 R5）を、アセンブリの実際の型参照で検査するテストを作る。既存の違反は許可リスト（件数のラチェット）で凍結し、以後の是正（R-02 ほか）の物差しにする。
- **R-04**: Phase14 の `using` 自動生成で入った未使用の `using static DS4Windows.Processing.Mouse;` を消し、その原因であるネスト列挙型 `Mouse.TouchButtonActivationMode` を `Core/Settings` のトップレベルの型にする。
- **R-03**: `AppLogger.NotificationService` の暗黙のフォールバック（2 つ目の `BackingStore` を作る）を、何もしない実装に替える。同類の `BackingStore.ProfileSettings` のフォールバックは、本番で通るかを調べて記録する（直すかは別途判断）。
- 機能は変えない（規約 §2.2）。利用者に見えるログは変えない（§2.3）。

### 対象外（Phase14b の後続の Step として、別途この計画書へ追記して承認を得る）
- R-02 の本体（`SpecialAction`・`BezierCurve` などの共有型の `Core/` への移動、`IAppSettingsService`／`IDeviceLayerSettings` の置き場所、`Input` → UI の通知の是正）。
- R-05 以降（静的な状態、カートリッジ化など）。

## 2. 決定事項（承認が必要）

| # | 項目 | 推奨 | 代替案 |
|---|---|---|---|
| D1 | Phase 名とブランチ | **Phase14b**、master から `Phase14b` を作る。Phase15 の前に行う（名前空間を先に確定させるため。レビュー §4） | 既存の Phase15 の Step 0 として行う |
| D2 | R-01 の検査の方式 | **ArchUnitNET（`TngTech.ArchUnitNET.xUnit`）**。§3.3 の比較を参照 | (b) NetArchTest.eNhancedEdition、(c) `System.Reflection.Metadata` による自作 |
| D3 | `Core/` → `Input/`・`Output/` の参照（`DS4Controls`、`X360Controls`、`DS4Color` など）の扱い | **記録のみ**（件数をテストの出力と計画書に残すが、失敗させない）。是正は R-17（層間のデータ契約の `Core/` への移動）と一緒に判断する | 違反として許可リストで凍結する |
| D4 | `INotificationService`／`NotificationEventArgs` の置き場所 | **Step 3 で `Processing/Abstractions` → `Core/Abstractions` へ移す**（実装は UI、利用者は `Core` の `AppLogger`。R2「契約は使う側・実装側の層」に照らして、最下層の利用者に合わせる）。利用は本体 6 ファイル・テスト 6 ファイル | 移さない（R-02 の本体でまとめて行う） |
| D5 | `TouchButtonActivationMode` の置き場所 | **`Core/Settings/TouchButtonActivationMode.cs`**（プロファイルの設定値の型。R5） | `Core/Abstractions` |

### 2.1 追加の決定事項（2026-11-05、Step 1〜3 の後にまとめて確認。ユーザー承認済み）

| # | 項目 | 決定 |
|---|---|---|
| D6 | R-02 本体の範囲 | **4 項目すべて**: UI のファクトリーの契約、小さな共有型と契約、第1層 → UI の通知、`SpecialAction` の `Core` への移動 |
| D7 | UI のファクトリーの契約の置き場所 | **`UI/Abstractions` を新設**（名前空間 `DS4Windows.UI.Abstractions`。規約 §3.4 R1 の UI の一覧に追記する） |
| D8 | `BackingStore.ProfileSettings` のフォールバック | **撤去する**（テスト側で明示的に設定する） |
| D9 | 実機確認 | **Phase14b の最後に 1 回** |

エージェントの判断（既定値で進めたもの。§8 に記録）:
- `IDeviceLayerSettings`（第1層が読む設定の契約。実装は第2層）は、使う側の **`Input/Abstractions`** を新設して置く（§3.4 R2 と同じ考え方: 契約は使う側。`Core` に置くと「第1層の設定」という意味が薄れる）。
- `IProfileLoadTarget` の `Mouse GetTouchPad(int)` は、`Core` が使う 5 つの操作（`CursorGyroDead`、`ToggleGyroControls`、`ToggleGyroMouse`、`ToggleGyroStick`、`ResetToggleGyroModes`）だけを持つ契約 `ITouchPadGyroToggles`（`Core/Abstractions`）を返す形にし、`Mouse` がそれを実装する（同じインスタンスを返すので挙動は同じ）。

## 3. 現状（2026-11-05 実測）

### 3.1 R-04
- `using static DS4Windows.Processing.Mouse;` を持つファイル: 本体 54（`Core` 43、`Input` 4、`Processing` 4、`Output` 2、`UI` 1）。テスト・StandaloneTests は 0。追加したのは Phase14-Step7-12（29256a75）。
- `Mouse` の静的に参照できる公開メンバーは、ネスト列挙型 `TouchButtonActivationMode`（`Processing/Mouse.cs:94`、`ushort`、`Click`／`Touch`／`Release`）だけ。
- 使っているファイルは 6 つ: `Core/Abstractions/IProfileSettingsService.cs:101`、`Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:268,273`、`Core/Settings/BackingStore.cs:694-697,1413`、`Core/Settings/ProfileSettingsService.cs:274`、`Processing/Mouse.cs`（内部）、`UI/ViewModels/TouchButtonUserControlViewModel.cs:40-48`。XAML からの参照はない。
- 保存: `ProfileOutputCurveSaveSection.cs:139` が `ToString()`（値の名前）を書き、読込は `Enum.TryParse`。型の場所が変わっても XML は変わらない。

### 3.2 R-03
- `Core/Logging/AppLogger.cs:103-107`: `_notificationService ?? (_notificationService = new AppNotificationService(new BackingStore(new EnvironmentService())))`。
- `App.xaml.cs:732`（`InitializePostHostServices`）が DI の Singleton を設定する。それより前に `AppLogger.LogToTray` が呼ばれるとフォールバックが作られる。
- フォールバックの `AppNotificationService` の `NotificationTriggered` には購読者がいない（`MainWindow` が購読するのは DI の Singleton）。したがって、フォールバックが通知を表示することはなく、**「何もしない実装」に替えても、利用者に見える挙動は変わらない**。違いは、使われない `BackingStore`（全プロファイル分の設定の配列）を作る費用がなくなること。
- `LogToTray` の呼び出し元: 本体 17 か所（`DS4Devices`、`UdpMotionService`、`DefaultBatteryCheckPort`、`ControlService`、`DeviceDisconnectHandler`、`ServiceLifecycle`、`TouchpadToggleService`、`MainWindow`、`NotificationService`、`UpdaterWindow`）。
- テスト: `LogToTrayNotificationBridgeTests` が getter で元の値を退避して戻す（退避の時点でフォールバックが作られている）。`Phase7Step5ServiceLocatorGuardTests.AppLogger_UsesItsOwnNotificationService_SetByTheApp` が、`AppLogger.cs` のプロパティの宣言を正規表現 `DS4Windows\.Processing\.Abstractions\.INotificationService` で検査している（D4 を採る場合は更新が要る）。
- 同類: `Core/Settings/BackingStore.cs:752-778` の `ProfileSettings` は、登録がないまま使われると `new ProfileSettingsService(this, _environmentService, new DeviceSlotState())` を作る（Debug ログ「[Legacy] BackingStore.ProfileSettings: Fallback instance used」）。`BackingStore` 自身が 925・1169・1428 行で使う。テスト（`Phase7Step10dW5GlobalRemovedGuardTests`、`TestGlobals`）もこの経路に依存している。**ブートストラップの `BackingStore`（ホスト構築より前）でこの経路を通ると、空の `DeviceSlotState` を持つ `ProfileSettingsService` が先に結ばれる（「最初の 1 つが有効」）可能性がある**。本番で通るかは未確認。

### 3.3 R-01（方式の比較。規約 §4「既存のオープンソースのアプローチを比較してから」）

| 方式 | 長所 | 短所 |
|---|---|---|
| **(a) ArchUnitNET**（TNG、Apache-2.0、xUnit 拡張あり。保守が継続中） | メソッド本体の参照（呼び出し、`new`、フィールドアクセス）まで見る。違反した型の一覧が取れ、許可リストとの照合が書きやすい | API の概念が多い。テストの依存パッケージが 1 つ増える（配布物には入らない） |
| (b) NetArchTest.eNhancedEdition（元の `NetArchTest.Rules` は更新停止。保守版のフォーク） | API が簡潔（`ResideInNamespace(...).ShouldNot().HaveDependencyOnAny(...)`） | フォークの継続性の確認が要る。違反の詳細（何を参照したか）が少ない |
| (c) `System.Reflection.Metadata` で自作 | パッケージが増えない | メソッド本体の IL の解読（`call`／`newobj`／`ldsfld`／`ldtoken`）、ジェネリック引数、属性を自前で扱う必要があり、検査そのものの誤りが入りやすい |
| （参考）ソースの文字列検索（既存の 166 ファイルの方式） | 既存と同じ書き方 | `using` やコメントに反応し、完全修飾名や `var` を見落とす。リファクタリングに弱い（レビュー R-16） |

- 推奨は (a)。版は、導入時に NuGet で確認し、**公開から 2 週間以上たった版を固定**する（2026-11-05 時点の最新は 0.13.5［2026-10-26 更新］。導入日がそれより 2 週間以上後でなければ、その前の版を使う）。
- 共通の限界（どの方式でも）: `const` の値と列挙型の定数は IL に埋め込まれるため、`const` の参照だけでは依存として出ない。`nameof` も出ない。コメントと `using` は出ない（そのため R-04 は R-01 とは別に必要）。

## 4. Step 構成

順序は、物差し（R-01）を先に入れ、R-04・R-03 で件数が減ることをラチェットの一致検査で確かめる形にする。各 Step の終わりに Debug ALL（Remove bin → Build Debug → Build Test → Execute Test）で、警告 0・エラー 0・テストが全部合格になるまで直す。コミットはユーザーが行う。

### Step 14b-1（R-01）層の依存のアーキテクチャテスト

| # | 作業 | 備考 |
|---|---|---|
| 1-1 | テスト用プロジェクト `DS4WindowsTests` に `TngTech.ArchUnitNET.xUnit` を追加（版は §3.3 の条件で固定） | D2 |
| 1-2 | 名前空間 → 層の対応表 `LayerMap`（テスト側の 1 型 1 ファイル）を作る | 下の表 |
| 1-3 | 規則のテスト `LayerDependencyRuleTests` を作る（Phase 名ではなく機能名で命名。レビュー R-16） | 下の規則 |
| 1-4 | 許可リスト `LayerDependencyBaseline`（規則ごとに「違反している型［トップレベルの完全名］ → 参照している禁止側の型の集合」）を作り、**実数と一致すること**を検査する（多くても少なくても失敗。規約 §3.1 と同じ運用）。失敗時のメッセージに、増えたもの・減ったものと、基準値を更新するための C# の初期化子をそのまま出す | ネスト型・コンパイラー生成の型（`<>c`、`+` 付き）はトップレベルの型にまとめる |
| 1-5 | 検査そのものが働くことのテスト: (1) `LayerMap` の分類（純粋関数）の単体テスト、(2) 既知の違反（`Input/DS4Devices` → `UI.Services.NotificationService`。メソッド本体の静的呼び出し）が検出されていることの確認 | メソッド本体の参照を見ていることの証拠 |
| 1-6 | 実測の件数を本書の §8 と `Repository-Review-2026-11-05.md` に記録。モデル図 02 の注記「依存の向きは、実コードと一致している」を、実測に合わせて書き直す | 規約冒頭（モデル図への反映） |

名前空間と層の対応（`LayerMap`）:

| 名前空間（前方一致） | 層 |
|---|---|
| `DS4Windows.Input` | 第1層 |
| `DS4Windows.Processing` | 第2層（`DS4Windows.Processing.Abstractions` は「第2層の契約」として区別する） |
| `DS4Windows.Output` | 第3層 |
| `DS4Windows.UI` | 4-a |
| `DS4Windows.Composition`、ルート `DS4Windows` の `App`・`StartupMethods` | 4-b（起点） |
| `DS4Windows.Core` | 横断基盤 |
| `DS4Windows.Properties`、`DS4Windows.Translations` | 共有の資源（どこからでも可） |
| `Sensorit.Base`、外部のアセンブリ | 対象外 |

規則（「禁止」は違反を許可リストで凍結。「目標」は今回は記録のみ）:

| # | 規則 | 区分 | 根拠 |
|---|---|---|---|
| L1 | `Core` は 第2層・4-a・4-b を参照しない | 禁止 | §3.4 R5 |
| L2 | `Core` は 第1層・第3層を参照しない | D3 による（推奨: 記録のみ） | §3.4 R5、レビュー R-17 |
| L3 | 第1層は 第2層・第3層・4-a・4-b を参照しない | 禁止 | §1 |
| L4 | 第3層は 4-a・4-b を参照しない。第2層は `Processing.Abstractions` だけを参照してよい | 禁止 | §1、§3.4 R2 |
| L5 | 第2層は 4-a・4-b を参照しない | 禁止 | §1 |
| L6 | 4-a は 4-b を参照しない | 禁止 | §3.4 R3 |
| L7 | 4-a は 第1層・第3層を参照しない | 目標（記録のみ。数が多く、レビュー R-09 で扱う） | §1 |

完了の条件: Debug ALL が緑。許可リストが実数と一致。§8 に件数を記録。

### Step 14b-2（R-04）`TouchButtonActivationMode` の切り出しと `using static` の削除

| # | 作業 | 備考 |
|---|---|---|
| 2-1 | `Core/Settings/TouchButtonActivationMode.cs` を新規作成（`namespace DS4Windows.Core.Settings`、`public enum TouchButtonActivationMode : ushort { Click, Touch, Release }`。値と順序は変えない）。`Processing/Mouse.cs` のネスト型を削除 | D5。新しい型のファイルなので `git mv` の対象ではない（R7 は移動のみ） |
| 2-2 | 54 ファイルから `using static DS4Windows.Processing.Mouse;` の行だけを削除（他の `using` とコメントは触らない。規約 §3.3 原則4） | 機械的な置換。スクリプトは scratchpad に置く |
| 2-3 | 列挙型を使う 6 ファイルのうち、`using DS4Windows.Core.Settings;` がないものに追加 | 使う名前空間だけを明示（§3.3 原則3） |
| 2-4 | テストの追加: (1) 型がトップレベルで `DS4Windows.Core.Settings` にあり、基底型が `ushort`、値が `Click=0`／`Touch=1`／`Release=2` であること、(2) プロファイルの保存 → 読込で 3 つの値が往復し、XML の `TouchpadButtonMode` の文字列が値の名前（`Click` など）のままであること | 既存の往復テストの仕組み（`PersistenceHarness`）を使う |
| 2-5 | Step 14b-1 の許可リストを更新（`TouchButtonActivationMode` 由来の `Core` → 第2層の違反が減る）。一致検査で減少を確認 | 件数を §8 に記録 |
| 2-6 | （任意・記録のみ）IDE0005（未使用の `using`）を一時的に有効にしたビルドで、ほかの未使用の `using` の件数を測り、§8 に記録する。直すかは別途判断 | コードは変えない |

完了の条件: Debug ALL が緑。`using static DS4Windows.Processing.Mouse` が 0 件。

### Step 14b-3（R-03）`AppLogger` のフォールバックの廃止

| # | 作業 | 備考 |
|---|---|---|
| 3-1 | （D4 を採る場合）`INotificationService.cs`・`NotificationEventArgs.cs` を `Processing/Abstractions` → `Core/Abstractions` へ `git mv`（内容は変えない） | R7: 移動だけのコミット |
| 3-2 | （D4 を採る場合）名前空間を `DS4Windows.Core.Abstractions` に書き換え、利用者（本体 6・テスト 6）の `using` を更新。`Phase7Step5ServiceLocatorGuardTests` の正規表現を新しい名前空間に更新 | R7: 書き換えは別コミット |
| 3-3 | `Core/Logging/NullNotificationService.cs` を新規作成（`SendNotification` は何もしない。`NotificationTriggered` は発火しない）。`AppLogger.NotificationService` の getter のフォールバックを `NullNotificationService` に替え、`new BackingStore(...)`・`new EnvironmentService()`・`AppNotificationService` の参照を `AppLogger` から消す | `AppLogger` から `UI.Services` への参照もなくなる（L1 の違反が減る） |
| 3-4 | フォールバックが初めて使われたとき、1 回だけ `AppLogger.Log(AppLogLevel.Debug, "[DI] AppLogger.NotificationService: not set yet; notification dropped")` を出す（新しいログ。利用者には見えない Debug。`Log-Policy.md` に沿う） | 規約 §2.3（新規のコードは Log-Policy に沿う）、§2.3 の `[DI]` 接頭辞 |
| 3-5 | テスト: (1) 未設定のとき `LogToTray` が例外を出さず、`BackingStore` を作らないこと（`NullNotificationService` が返ること）、(2) 設定後は設定したサービスへ委譲すること（既存の `LogToTrayNotificationBridgeTests` を維持）、(3) `AppLogger` が `AppNotificationService`／`BackingStore` を参照しないこと（Step 14b-1 の規則 L1 の許可リストから該当行が消えることで確認） | |
| 3-6 | `BackingStore.ProfileSettings` のフォールバックの調査（コードは変えない）: ブートストラップ（`App` のホスト構築より前）から 925・1169・1428 行の経路に入るかを読解で確かめ、結果を §8 に記録する。入る場合は、次の Release ビルドの実機確認（別の作業のついで）で Debug ログ「[Legacy] BackingStore.ProfileSettings: Fallback instance used」の有無を見る手順を書く。直し方（例外、`Null` 実装、登録順の固定）は結果を見て別途提案する | 機能に関わるため、ここでは直さない |
| 3-7 | Step 14b-1 の許可リストを更新（`AppLogger` → UI、D4 を採る場合は `INotificationService` 由来の `Core` → 第2層の違反が減る） | 件数を §8 に記録 |

完了の条件: Debug ALL が緑。`AppLogger.cs` に `new BackingStore` がない。3-6 の結果が §8 にある。

### Step 14b-4 文書の更新
- `Repository-Review-2026-11-05.md` に、R-01・R-03・R-04 の対応結果を追記。
- `Phase11-and-Beyond-Roadmap.md` に Phase14b を追加（Phase14 と Phase15 の間）。
- モデル図 02（Step 1-6 の注記の書き直し）、必要なら 04（`NullNotificationService`）。
- `Phase9-Deferred-Ledger.md` に、3-6 の結果が「別途判断」になった場合の行を追加。

### Step 14b-5〜9（R-02 本体と D8。2026-11-05 追加。D6〜D8）

各 Step は、移動（`git mv` のみ）と名前空間の書き換えを別コミットにし（R7）、書き換えのコミットで許可リストを実数に合わせる。各 Step の終わりに Debug ALL を緑にしてコミットする。

| Step | 内容 | 移動先 |
|---|---|---|
| 14b-5 | 第1層 → UI の通知: `DS4Devices` の `UI.Services.NotificationService.ShowToast` を `AppLogger.LogToTray` の直接呼び出しへ（`ShowToast` は中で `LogToTray` を呼ぶだけ。挙動は同じ）。検査の確認テストの対象を、別のメソッド本体だけの参照へ差し替え | — |
| 14b-6 | UI のファクトリーの契約: `IEditorWindowFactory`・`IMainWindowFactory`・`IViewModelFactory` | `UI/Abstractions`（D7） |
| 14b-7 | 小さな共有型と契約: `BezierCurve`（`Core/Utilities`）、`MacroExecuteMode`・`MacroExecuteModes`・`ProfileActions`・`ControlActionData`（`Core/Settings`）、`SelectedProfileChangedEventArgs`・`WindowCandidate`・`WindowFocusResult`・`CapturedWindow`・`IAppSettingsService`・`IProfileLoadTarget`（`Core/Abstractions`）、`WindowTargetMatcher`（`Core/Platform`）、`IDeviceLayerSettings`（`Input/Abstractions`）。`IProfileLoadTarget.GetTouchPad` は `ITouchPadGyroToggles` を返す形へ（§2.1） | 左記 |
| 14b-8 | `SpecialAction` | `Core/Settings`（依存は `Core.Settings` と `Input` だけ。データの型で、実行は Actions サブシステムが持つ） |
| 14b-9 | `BackingStore.ProfileSettings` のフォールバックの撤去（D8）。未設定のまま使われたら例外にし、テスト（`TestGlobals`、`PersistenceHarness`、`Phase7Step10dW5GlobalRemovedGuardTests`）は明示的に設定する | — |
| 14b-10 | 規約 §3.4 R1（`UI/Abstractions`・`Input/Abstractions` の追記）、モデル図 01〜04、ロードマップ、レビュー報告書、本書 §8 | — |
| 14b-11 | Release ビルド 2 か所 → 実機確認（D9。§5） | — |

## 5. 実機確認

- 利用者に見える変更はないため、Phase14b の第1弾としての実機確認は行わない。R-02 の本体まで進んだ時点で、Phase14b の最後に 1 回行う（起動、トレイ通知［コントローラーの接続・切断、電池の通知］、プロファイルの保存・読込でタッチパッドのボタンのモードが保たれること）。
- 3-6 で「本番で通る」と分かった場合は、その確認手順を実機確認に含める。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| ArchUnitNET が WPF の exe アセンブリ（`DS4WinWPF.csproj`）を読めない、または読み込みが遅い | 1-1 の直後に、型の一覧が取れることだけを確かめる最小のテストを書く。読めない場合は D2 の代替案 (b) に切り替え、本書に追記して再承認を得る。テストの実行時間を計り、§8 に記録する |
| 許可リストが大きく、レビューしにくい | 規則ごとにファイルを分けず 1 つの基準値の型にまとめるが、規則ごとの小計を失敗メッセージとテストの出力に出す |
| 列挙型の移動で、XML の値の名前や数値が変わる | 2-1 で値と基底型を変えない。2-4 の往復テストで守る |
| `using static` の一括削除で、ほかの行やコメントを消す | 行全体が完全一致するものだけを消すスクリプトにし、差分の行数（54 行の削除のみ）を確かめる |
| `NullNotificationService` で、起動直後の通知が出なくなる | 現状もフォールバックには購読者がおらず表示されない（§3.2）ため、挙動は同じ。3-4 の Debug ログで、落ちた通知の有無を後から確認できる |
| 3-1・3-2 の名前空間の変更で、XAML や反射が壊れる | `INotificationService`／`NotificationEventArgs` は XAML から参照されていない。`AppNotificationRegistration` の `Type.GetType` が文字列で引くのは WinRT の型（`Windows.UI.Notifications.*` など）だけで、影響しない（いずれも 2026-11-05 確認済み） |

## 7. 規約の順守

- 1 ファイル 1 型、ファイル名＝型名（新規: `TouchButtonActivationMode`、`NullNotificationService`、テストの `LayerMap`、`LayerDependencyRuleTests`、`LayerDependencyBaseline`）。
- 名前空間＝フォルダ（`Phase14NamespaceFolderGuardTests` が検査）。
- 移動は `git mv` のみ、書き換えは別コミット（R7。3-1・3-2）。
- 既存のコメント・TODO は消さない（§3.3 原則4）。
- ログ: 既存のログは変えない。新しいログは 3-4 の Debug 1 件のみ（`[DI]` 接頭辞）。
- ラチェットの基準値は、件数を減らした Step で同時に更新する（§3.1）。

## 8. 実施記録（実施時に追記）

### 8.1 Step 14b-1（R-01）— 完了（2026-11-05）
- パッケージ: `TngTech.ArchUnitNET` **0.13.4**（2026-08-20 公開。最新の 0.13.5 は 2026-10-26 公開で 2 週間に満たないため見送り）。計画の `TngTech.ArchUnitNET.xUnit` ではなく本体のパッケージだけにした（違反の照合を自前で行い、xUnit 拡張の `Check()` を使わないため。依存が少ない）。
- 追加したファイル（テスト、1 型 1 ファイル）: `ArchLayer.cs`、`LayerMap.cs`、`LayerDependencyScanner.cs`、`LayerDependencyBaseline.cs`、`LayerDependencyRuleTests.cs`、`LayerMapTests.cs`。
- 走査の時間: 約 3 秒（アセンブリの読み込みを含む。1 回だけ読み、テスト間で使い回す）。
- 検査が働くことの確認: `DS4Devices` → `UI.Services.NotificationService`（メソッド本体だけの静的呼び出し）と、`ServiceRegistration` の登録ラムダ（コンパイラー生成のネスト型）からの `ProfileIdentityService` が、それぞれ辺として出ること。
- 実測（Step 14b-1 時点）: 禁止の規則 **108 件**（L1: 81［参照元の型 49］、L3: 5、L4: 1、L5: 15、L6: 6）。記録のみ: L2 **70 件**（参照元 37）、L7 **80 件**（参照元 33）。
- 新しく分かったこと（レビューに未記載）:
  - **L5 の 15 件はすべて、UI のファクトリーの契約が `Processing/Abstractions` にあることによる**（`IEditorWindowFactory` → `AssignmentEditor`／`BindingWindow`／`ProfileEditor`／`RecordBox` ほか 9、`IMainWindowFactory` → `MainWindow`、`IViewModelFactory` → 5 つの ViewModel）。実装も利用者も UI なので、契約は `UI/` 側（例: `UI/Abstractions`）に置くのが §3.4 R2 に合う。R-02 の本体で扱う。
  - L4 の 1 件: `Output.OutputSlotManager` → `Processing.IInstanceIdentifiable`（汎用の小さな契約が第2層にある）。
  - L6 の 6 件: UI（`MainWindow`、`ProfileEditor`、`ImageLocationPaths`、`LanguagePackControl`、`WelcomeDialog`、`SettingsViewModel`）がアプリの起点（`App`、`StartupMethods`）を直接参照。
- Debug ALL: 緑。

### 8.2 Step 14b-2（R-04）— 完了（2026-11-05）
- `Core/Settings/TouchButtonActivationMode.cs` を新規作成し、`Processing/Mouse.cs` のネスト型を削除（経緯のコメントを残した）。
- `using static DS4Windows.Processing.Mouse;` を **54 ファイルから削除**（53 は行の削除、`UI/ViewModels/TouchButtonUserControlViewModel.cs` は `using DS4Windows.Core.Settings;` へ置き換え）。残り 0 件。差分は削除 54 行・置換 1 行のみ（改行コードは CRLF のまま）。`BackingStore`・`ProfileSettingsService` は同じ名前空間のため `using` の追加は不要だった。
- テスト: `TouchButtonActivationModeTests`（型の形、`Mouse` にネスト型がないこと、保存・読込で 3 つの値と XML の値の名前が往復すること）。
- ラチェット: 一致検査が「減った違反 4 件」（`IProfileSettingsService`／`ProfileSettingsService`／`ProfileOutputCurveSaveSection`／`ProfileTouchpadMouseLoadSection` → `Mouse`）を検出し、許可リストを **108 → 104 件**（L1: 81 → 77）に更新した。`BackingStore` → `Mouse` は、列挙型以外の参照があるため残る。
- 2-6（IDE0005 による未使用の `using` の計測）: 未実施（任意）。必要なら別途行う。
- Debug ALL: 緑（ビルド 警告 0・エラー 0、`DS4Windows.Actions.Tests` 3,224 件合格、`StandaloneTests` 11 件合格）。

### 8.3 Step 14b-3（R-03）— 完了（2026-11-05）
- 3-1（D4）: `INotificationService.cs`・`NotificationEventArgs.cs` を `Processing/Abstractions` → `Core/Abstractions` へ `git mv`（移動だけのコミット。`git show -M100%` で 100% の名前の変更）。
- 3-2: 名前空間を `DS4Windows.Core.Abstractions` に書き換え。利用者の更新は `AppNotificationService`、`MainWindow`（完全修飾名）、`AppLogger`（完全修飾名）、テストの `MockNotificationService`・`NotificationServiceTests`（`using` の置き換え）、`Phase7Step5ServiceLocatorGuardTests`（正規表現）。`App`・`ServiceRegistration`・`MainWindowFactory` と 2 つのテストは両方の名前空間の `using` を既に持ち、`Processing.Abstractions` もほかの型で使い続けている（確認済み）。許可リスト 104 → 103 件。
- 3-3〜3-5: `Core/Logging/NullNotificationService.cs` を追加し、`AppLogger.NotificationService` のフォールバックを差し替えた。`AppLogger` から `using DS4Windows.Core.Platform／Core.Settings／UI.Services` が不要になり削除。落とした通知があれば、最初の 1 回だけ Debug のログ「[DI] AppLogger.NotificationService: not set yet; notification dropped」を出す。テスト `AppLoggerNotificationFallbackTests`（未設定時は `NullNotificationService`、`LogToTray` が例外を出さない、イベントが発火しない、`AppLogger` から `BackingStore`／`EnvironmentService`／`AppNotificationService` への辺がない）。許可リスト 103 → **102 件**（`AppLogger` → `UI.Services.AppNotificationService` が解消）。
- 3-6（`BackingStore.ProfileSettings` のフォールバックの調査。コードは変えていない）: 読解の結果、**通常の起動では通らない**と判断した。
  - ホスト構築より前（`RunPreHostBootstrap`／`RotateLogsAndLogStartupDiagnostics`／`CheckOptions` の言語・テーマ）は、ブートストラップの `BackingStore` の単純なフィールド（`useLang`、`logMaxArchiveFiles`、`logMinLevel`、`useCurrentTheme`）だけを読む。
  - フォールバックを通りうる 3 つの経路（`EmitMissingActionLogsForDevice`、`UpdateDS4CSetting`、`CheckOldDevicestatus`）は、プロファイルの読込・プリセット・入力の処理からだけ呼ばれ、いずれも `App.xaml.cs:234` の `BuildHostAndServices` の後（`LoadAppSettings` は 252 行目）。`BuildHostAndServices` の `CreateControlService` が `ControlService` → `IProfileSettingsService` を解決し、`ProfileSettingsService` のコンストラクターが `LinkProfileSettings` で先に結ばれる。
  - `-driverinstall` の分岐（`CheckOptions`）はアプリ設定（`AppSettingsXmlSerializer`）だけを読み、これらの経路を通らない。
  - テスト（`TestGlobals`、`Phase7Step10dW5GlobalRemovedGuardTests`）はこのフォールバックに依存している。扱いは §9 の確認事項とした。
- Debug ALL: 緑（ビルド 警告 0・エラー 0、`DS4Windows.Actions.Tests` 3,229 件合格、`StandaloneTests` 11 件合格）。

### 8.4 許可リストの件数の推移
| 時点 | 禁止の規則 | 内訳 |
|---|---|---|
| Step 14b-1 | 108 | L1 81、L3 5、L4 1、L5 15、L6 6 |
| Step 14b-2 | 104 | L1 77 |
| Step 14b-3（3-2） | 103 | L1 76 |
| Step 14b-3（3-3） | 102 | L1 75 |
記録のみ: L2 70、L7 80（変化なし）。
