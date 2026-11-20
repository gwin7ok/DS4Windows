# PhaseRV10 計画書: BackingStore の公開フィールドを、設定の領域ごとの窓口へ移す

作成日: 2026-11-20
状態: **着手は未承認（コードは未着手）**。D1〜D4 の決定を待つ
出典: `docs-forDIMG/Code-Review-2026-11-19/02-Scorecard.md`（オブジェクト指向 B。`BackingStore` の公開フィールド 107 件が最大の減点）、`.github/copilot-instructions.md` §3.1「設定の領域の窓口」
動き: **機能・保存の形（XML）・ログは変えない**。フィールドの置き場所と、読み書きの経路だけを変える。

## 目的
1. `BackingStore` の public な可変フィールド（計測で 107 件。本計画の対象は 90 件）を、領域ごとの小さな型（窓口）へ移し、`BackingStore` からは、領域を返す読み取り専用のプロパティだけを公開する。
2. 設定を 1 つ足すときの触る場所を、「領域の型 1 つ（＋DTO の対応 1 か所）」に絞る。

## 現状の調査（2026-11-20）
- 対象の公開フィールドは 90 件。型は `int`・`bool`・`string`・`double`・`DateTime`・列挙型・配列・一覧。
- 本体（`DS4Windows`）の参照は 555 件。**集中している**: `AppSettingsDTO` 196、`AppSettingsService` 145、`AppSettingsXmlSerializer` 89、`SpecialActionsXmlSerializer` 22、`ProfilePresets` 14、`ProfileRepository` 13、`ProfileSettingsService` 13。ほかは 1〜8 件の小さなファイル（`App.xaml.cs` 6、`PathService` 4、`DeviceStateService` 3 など）。UI の ViewModel・画面は、`IAppSettingsService` 経由で、フィールドを直接触らない。
- テストの参照は 523 件（55 ファイル。`TestGlobals.Store.<名前>`）。機械的に書き換えられる。
- 領域の分け方（案）:

| 領域（案） | 主なフィールド | 件数 |
|---|---|---|
| 画面の配置（`LayoutSettings`） | `formWidth`・`formHeight`・`formLocationX/Y`、エディターの幅、各一覧の列の幅 | 約 33 |
| 更新の確認（`UpdateCheckSettings`） | `lastChecked`・`lastVersionChecked`・`checkEveryValue/Unit`・`checkUpdateStartup` | 6 |
| 通知・動作（`BehaviorSettings`） | `notifications`・`profileChangedNotification`・`quickCharge`・`closeMini`・`flashWhenLate(At)`・`startMinimized`・`minToTaskbar`・`processPriority` ほか | 約 18 |
| 外部連携（`NetworkSettings`） | OSC 6 件、UDP 7 件、Steam フォルダー 2 件 | 15 |
| 言語・見た目（`AppearanceState`） | `useLang`・`downloadLang`・`useIconChoice`・`useCurrentTheme`・`fakeExeFileName` | 5 |
| ログ | `logMaxArchiveFiles`・`logMinLevel`（`logTimeColWidth` などは配置） | 2 |
| パス・実行時の状態（`BackingStore` に残す） | `m_Profile` など 4 件、`profilePath`・`selectedProfile`・`linkedProfiles`・`actions`・`ds4controlSettings`・`profileActions` など | 約 24 |

## 決定してほしい事項

### D1 範囲
- **(A) 設定ファイル（`AppSettings.xml`）に保存される設定だけ（約 66 件）を領域へ移す（推奨）**。パス・実行時の状態（約 24 件）は、設定ではなく実行時の状態なので、別の扱いにする。
- (B) 90 件すべて。実行時の状態も領域へ移す。変更が大きく、スロットの配列（`profileActions` など）は読み書きの経路が多い。
- (C) 画面の配置（約 33 件）だけを先に。小さく始めて、効果を確かめる。

### D2 形
- **(A) 領域ごとの型に、同名のプロパティ（`{ get; set; }` と既定値）を持たせ、`BackingStore` は `public LayoutSettings Layout { get; }` のように領域を 1 つずつ持つ（推奨）**。`AppSettingsService` の各プロパティは `store.Layout.formWidth` のように 1 段増える。
- (B) `AppSettingsDTO` を、そのまま実行時の保持にも使う（`BackingStore` のフィールドをなくし、DTO を持つ）。型は減るが、保存の形と実行時の状態が結びつく。

### D3 名前
- **(A) 移すときは、名前をそのまま（小文字始まりのプロパティ）にし、名前の整理は別 Phase にする（推奨）**。移動と改名を混ぜない（`copilot-instructions.md` §3.4 R7 の考え方）。
- (B) 移すと同時に Pascal 形へ改名する。差分が大きくなり、テストの 523 件も改名される。

### D4 順序と確認
- **(A) 領域ごとに 1 コミット。各コミットで Debug ALL（警告 0・全テスト）。保存・読み込みの往復（ラウンドトリップ）のテストを、領域ごとに足してから移す（推奨）**。実機確認は、最後に「設定を変えて保存→再起動→読み込み」の 1 項目だけ。
- (B) 全領域をまとめて 1 コミット。

## Step（D1＝(A)、D2＝(A)、D3＝(A)、D4＝(A) の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV10-0 | 設定ファイルの保存・読み込みの黄金テスト（全項目を既定値以外にして往復）を足す。移す前に通ることを確かめる | 新テスト、Debug ALL |
| RV10-1 | `LayoutSettings`（画面の配置）を作り、`BackingStore`・`AppSettingsService`・DTO の対応・テストを移す | 同上 |
| RV10-2 | `UpdateCheckSettings`、`BehaviorSettings` | 同上 |
| RV10-3 | `NetworkSettings`、`AppearanceState`、ログ | 同上 |
| RV10-4 | 公開フィールドの残りを確認し、`BackingStore` の public な可変フィールドの許可リスト（ラチェット）を足す。規約 §3.1 の「設定の領域の窓口」へ追記（承認後） | `CodeHealthRatchetTests` |
| RV10-5 | 評価の再算定（`BackingStore` の公開フィールド 107→約 40、オブジェクト指向・機能追加の容易化の変化） | `Score-CodeReview.py` |

## 守るもの
- 設定ファイルの形、読み込みの既定値、保存の順序。
- `IAppSettingsService` の契約（UI・ViewModel は変えない）、`SettingChanged` の通知。
- ログの文言。

## 変更履歴
- 2026-11-20: 作成（D1〜D4 の決定待ち）。
