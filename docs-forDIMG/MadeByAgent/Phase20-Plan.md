# Phase20 計画書: MainWindow の責務を分け、引数 15 以下の基準を満たす

作成日: 2026-11-10
位置づけ: 「引数 6 以上を避ける根本的な対策」の 1 番目（責務で分ける）。Phase19（工場の廃止）のあとに行う。工場は Phase19 で引数が 0〜2 になるため、`MainWindowFactory` の 18 は本 Phase の対象から外れる。
前提: `Phase17-Plan.md` 17-6（部分達成）、ロードマップの 2026-11-10 の「構造の宿題」。`Mapping` は 15 になり、基準値の一覧（`ConstructorArityBaseline`）に残るのは `MainWindow` 18 と `MainWindowFactory` 18 だけ。
規則: 3.1（コンストラクターの引数は 15 以下を努力目標。「何でも箱」は作らない）、3.3（1 ファイル 1 型、名前空間＝フォルダ）、2.2（機能・ログの完全維持）。

## 1. 目的と終わりの状態

- `MainWindow`（約 2,060 行の code-behind）から、まとまりのある責務を別の部品へ移し、引数を 18 → 14 にする。`MainWindowFactory` は Phase19 で解消済みの前提。
- `ConstructorArityBaseline` が空になる（`ConstructorArityRuleTests` が一致を検査する）。
- 画面の見た目・操作・ログ・通知は変えない。コードは「書き直さず、移す」（メソッドの本体はそのまま。窓に触る部分は、小さなホストの契約を通す）。

## 2. 現状の分析（`MainWindow.xaml.cs` の依存の使われ方）

引数 18: `parser`、`appSettings`、`editorWindowFactory`、`pathService`、`environmentService`、`appearanceSettings`、`profileRepo`、`notificationService`、`lifecycle`、`runtimeFeatures`、`ipcProcessor`、`profileSlotApplier`、`mainWindowsViewModel`、`autoProfileService`、`viewModelFactory`、`themeLanguage`、`changelogService`、`slots`。

コンストラクターの後（260 行目以降）の依存ごとの使われ方（行番号は 2026-11-10 時点）:

| 依存 | 使われる場所 | 責務のまとまり |
|---|---|---|
| `notificationService` | 463（通知イベントの購読） | トレイ・通知 |
| `appearanceSettings` | 527、534（テーマの選択、トレイのアイコン） | トレイ・外観 |
| `themeLanguage` | 528（テーマの切り替え）。コンストラクターで画像のパスの資源へ渡す | トレイ・外観 |
| `editorWindowFactory` | 1753（プロファイル編集画面の生成）のみ | プロファイルの選択・編集 |
| `profileRepo` | 1116、1202、1770、1804〜1834 | プロファイルの選択・編集（一覧の同期） |
| `profileSlotApplier` | 400、1125、1856（プロファイルの適用） | プロファイルの選択・編集 |
| `changelogService` | 294、357、1481（更新の確認） | 更新の確認 |
| `runtimeFeatures` | 691、1324、1397〜1422（ホットキー、OSC、UDP、隠し） | 機能の切り替え |
| `pathService`、`environmentService`、`lifecycle`、`slots`、`autoProfileService` | 複数の責務にまたがる（`lifecycle` は 20 か所） | 窓に残す |

## 3. 設計の決定

| 番号 | 項目 | 決定 | 理由 |
|---|---|---|---|
| 1 | 分け方 | 依存が「ひとつの責務」にだけ使われているものを、その責務の部品へ移す。部品は DI の Singleton。窓は部品を 1 つずつ受ける | 引数の数だけを減らす「束ね」ではなく、使う場所ごと移すため、依存が見えなくならない |
| 2 | 窓との接点 | 部品は窓の部品に直接触れず、`IIpcCommandHost`（Phase13-Step11 の前例）と同じ形の小さなホストの契約を窓が実装して、呼び出しのたびに渡す | 部品が WPF の窓を保持しない。テストで偽のホストを使える |
| 3 | 第 1 弾（トレイ・外観・通知） | `TrayAppearanceController`（`UI/Services`）へ、`notificationService`、`appearanceSettings`、`themeLanguage` を移す（−3 ＋ 1 ＝ **−2**）。対象のメソッド: `ShowSystemNotification`（2 つ）、`ShowProfileSwitchNotification`、`ShowHotkeyNotification`、`SettingsWrapVM_AppChoiceIndexChanged`、`SettingsWrapVM_IconChoiceIndexChanged`、トレイ関連の `TrayIconVM_*` | 3 つの依存が、トレイ・通知・テーマの切り替えにだけ使われる |
| 4 | 第 2 弾（プロファイルの選択・編集） | `ProfileSelectionCoordinator`（`UI/Services`）へ、`editorWindowFactory`、`profileRepo`、`profileSlotApplier` を移す（−3 ＋ 1 ＝ **−2**）。対象: `SelectProfCombo_SelectionChanged`、`LinkedProfCombo_SelectionChanged`、`ShowProfileEditor`、`ProfileEditor_Closed`、`SyncProfileListAndControllers`、`IsSlotConnected`、`Editor_ProfileSaved`、`Editor_ProfilesUpdatedExternally`、`TrayIconVM_ProfileSelected` | 3 つの依存が、プロファイルの選択と編集の後始末にだけ使われる |
| 5 | 結果 | 窓: 18 − 2 − 2 ＝ **14**。工場: 窓の依存に追随して、`notification`、`appearance`、`themeLanguage`、`editorWindowFactory`、`profileRepo`、`profileSlotApplier` が部品 2 つに替わり、`profileIdentityService` を足して **約 14〜15** | 基準（15 以下）を満たす。工場が 16 以上になるときは、複製の名前入力欄（`dupBox`）の初期化を別の部品へ移す |
| 6 | 第 3 弾（任意） | 更新の確認（`changelogService`）、機能の切り替え（`runtimeFeatures`）は、14 になった後に、さらに分けるかを決める | 引数の数ではなく、窓の行数を減らす目的の整理。この Phase の範囲外 |
| 7 | 動作の確認 | メソッドの本体は書き直さず、移したあとのコードを `git diff --color-moved` で、移動だけであることを確認する。ログの文言・通知の文言・イベントの購読の順序は変えない | 規則 2.2、2.3 |

## 4. 段階

| 段階 | 内容 | 守り |
|---|---|---|
| 20-0 | 調査: 移す対象のメソッドが触る窓の部品（`trayIconVM`、`settingsWrapVM`、`conLvViewModel`、リストなど）を洗い出し、ホストの契約（`ITrayAppearanceHost`、`IProfileSelectionHost`）の項目を決める。窓の状態（`startMinimized` など）への依存も確認する | 計画書の更新 |
| 20-1 | `TrayAppearanceController` ＋ホストの契約＋窓での実装。移したメソッドは、窓の同名のメソッドから部品へ委譲する形ではなく、イベントの購読を部品へ移す（購読先の順序を保つ）。DI 登録（`ServiceRegistration.Ui.cs`）、工場の更新 | 部品の単体テスト（偽のホスト）、全テスト |
| 20-2 | `ProfileSelectionCoordinator` ＋ホストの契約＋窓での実装。選択・編集・一覧の同期を移す | 部品の単体テスト、全テスト |
| 20-3 | `ConstructorArityBaseline` を空にする。`MainWindow`／`MainWindowFactory` の引数の検査（`ControlServiceForwardRemovalGuardTests`）の数を更新 | `ConstructorArityRuleTests` |
| 20-4 | 文書（モデル図 01、台帳、ロードマップ）の更新。UI の動作の確認（§5） | 実機の確認 |

### 4.1 拡張: 窓以外の 10 引数以上の型（Step 20-5 以降。窓の分割のあとに続ける）

窓の分割（20-0〜20-4）が済んだら、同じ方針（依存が「ひとつの責務」にだけ使われているものを、その責務の部品へ移す。束ねない）で、引数 10 以上の型を順に分ける。各型の前に、現状を固めるテストを書く。1 型 1 Step、全テストを通してから次へ。

| 段階 | 型（引数） | 分け方の見込み |
|---|---|---|
| 20-5 | `InputReportProcessor`（15） | 1 レポートの処理（遅延の計測、電池の通知、タッチパッドの切り替え、マッピング、仮想パッドへの送出）の段に分ける。ホットパスのため、割り当てを増やさない（`Phase13Step5bOnReportTimingBenchmark` で比べる） |
| 20-6 | `DeviceConnector`（13）、`DeviceDisconnectHandler`（11） | 接続・切断の手順を、機種ごとの設定の適用、仮想パッドの接続、通知に分ける |
| 20-7 | `ServiceLifecycle`（12）、`ControlService`（12） | サービスの開始・停止の段階（入力の開始、出力の準備、ホットキーなど）を部品へ |
| 20-8 | `ProfileApplicationService`（12） | 読み込み・適用・通知の段へ |
| 20-9 | `SpecialActionOutputFactory`（14）、`AssignmentEditor`／`ProfileEditor`／`RecordBox`（11）、`AutoProfileService`／`IpcCommandProcessor`／`ProfileSettingsViewModel`（10）、`ProfileRepository`（9） | 順に判断。工場は Phase19 の結果に従う |
| 20-10 | 値の入れ物（`AssignmentDefinition` 12、`BindingPolicy` 11、`RepeatHelper` 10、`ExtrasBindingValues` 9） | 分割ではなく、不変のレコード型（`record`）への置き換えか、名前付き引数の徹底で足りるかを判断する（3 つ以上の bool は名前付き引数） |
| 20-11 | `ConstructorArityBaseline` のしきい値の引き下げ（15 → 12 → 10 を段階的に）と、新規の型は 6 以下をめざす旨を規則 3.1 へ追記 | `ConstructorArityRuleTests` |

数字は 2026-11-10 の反射での測定。各 Step の最初に、実測と使われ方を調べて表を更新する。

## 5. 動作の確認（実機・利用者の確認が要る項目）

窓の挙動は単体テストで守りきれないため、移したあとに次を確かめる。

1. トレイ: 通知（プロファイルの切り替え、ホットキー、システム通知）の表示、アイコンの切り替え、テーマ（ライト／ダーク）の切り替え、トレイのメニュー（開く・終了・プロファイルの選択）。
2. プロファイル: 各コントローラーの行でのプロファイルの選択、リンクされたプロファイルの選択、新規・編集・保存・名前変更・削除、編集画面を閉じたあとの一覧の同期、トレイからのプロファイルの選択。
3. 起動・終了: 最小化での起動、トレイからの復帰、終了。

## 6. リスク

- 画面のコードは単体テストが薄く、移し間違いが目視でしか見つからない。→ 本体は書き直さず移すだけにし、`git diff --color-moved` で確認する。ホストの契約の項目は少なく保つ。
- イベントの購読の順序・タイミング（`SetupEvents`）が変わると、通知の表示順やプロファイルの同期の順序が変わりうる。→ 購読の順序を保ち、移す前後でログを比べる。
- 部品が窓を長く保持すると、窓の破棄後も参照が残る。→ ホストは呼び出しのたびに渡し、部品は窓を保持しない。

## 7. 進捗

- 2026-11-10: 計画書を作成。コードは未着手。同日、Phase19（工場の廃止）の新設に伴い位置づけを更新し、窓以外の型への拡張（20-5〜20-11）を追記した。
