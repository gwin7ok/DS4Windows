# Phase14 個別計画書: 名前空間・フォルダの再編

作成日: 2026-11-04
状態: **承認済み（2026-11-04）→ 完了（実機確認済み）。結果は Phase14-Completion-Report.md**
ブランチ: `Phase14`（Phase13b を master に取り込んだ後、master から作成。決定済み）
根拠: `Phase11-and-Beyond-Roadmap.md` §3 Phase14、規約 §3.3 原則3・§3.4（R1〜R7）、`Phase6-Step13-Plan.md`、モデル図 `Model-Diagram/02-Layer-Architecture-Diagram.md`

## 1. 目的

- 名前空間をフォルダパスと一致させる（規約 §3.3 原則3 の最終目標）。
- `DI/` を Composition Root 専用にする（インターフェースは層別のフォルダへ）。
- フォルダ構成を、全体 4 層モデル（モデル図 02）に揃える。
- 機能は変えない（規約 §2.2）。ログは変えない。

## 2. 決定事項（2026-11-04、ユーザー承認済み）

| # | 項目 | 決定 |
|---|---|---|
| D1 | ブランチ | Phase13b を master に取り込んだ後、master から `Phase14` を切る |
| D2 | フォルダの目標 | **層別フォルダへ再編**（約 730 ファイルを振り分け） |
| D3 | ルート名前空間 | **`DS4Windows` に統一**（`RootNamespace` も `DS4Windows`。`DS4WinWPF.*` は廃止） |

## 3. 現状（2026-11-04 実測）

- `.cs` 約 730 ファイル、XAML 32 ファイル、テスト 467 ファイル。
- 名前空間とフォルダの不一致: `DS4Control/` 134 ファイルが `DS4Windows`、`DS4Control/Services/` 87 が `DS4Windows.Services`（14 が `DS4Windows`）、`DI/` 78 が `DS4Windows.DI`（11 が `DS4Windows.Services`）、`Actions/` 109 が `DS4Windows.Actions`、`DS4Library/` 28 が `DS4Windows`、`DS4Forms/` 系は `DS4WinWPF.DS4Forms.*`（約 125）、`Core/`・`Updater/`・`Startup/` などは `DS4WinWPF` 系。
- XAML: `clr-namespace:DS4WinWPF.*` 約 35 件、`x:Class` 30 件（すべて `DS4WinWPF.DS4Forms`／`DS4WinWPF`）。
- 文字列で型を引く箇所: `AppNotificationRegistration` の `Type.GetType(fullName)` 1 件。テストでは `BackingStoreStateDumper`／`BackingStoreStateFiller`（`Namespace == "DS4Windows.DI"`、`StartsWith("DS4Windows")`）ほか 13 ファイルが名前空間を直接参照。
- リソース: `DS4WinWPF.Properties.Resources`（`Properties/Resources*.resx`）、`DS4WinWPF.Translations.Strings`。名前空間は `RootNamespace` とフォルダから決まるため、書き換えが必要。

## 4. 層別フォルダの案（Step 14-1 で全ファイルの移行表にして承認を得る）

最上位は次の 7 つ（`DS4Windows/` 直下。名前空間は `DS4Windows.<フォルダ>`）。`Mapping` はクラス名と衝突するため、第 2 層のフォルダは `Processing` とする。

| フォルダ | 層 | 主な内容（現在の場所） |
|---|---|---|
| `Input/` | 第 1 層 | `DS4Library/`（`DS4Device`、`InputDevices/*`）、`HidLibrary/` |
| `Processing/` | 第 2 層 | `Mapping`、`Actions/`（サブフォルダ `Abstractions`／`Controllers`／`Implementations`）、`DS4Control/Services/` の入力処理・プロファイル・スロット系、`SyntheticDispatcher` 系 |
| `Output/` | 第 3 層 | `DS4Control/DS4OutDevices/`、`OutputKBM/`、`OutputSlotManager`、`OutSlotDevice` |
| `UI/` | 4-a | `DS4Forms/`（View、ViewModel、Converters、Themes）、`Notifications/`、`Translations/` ほか UI 付随 |
| `Composition/` | 4-b | `DI/` の配線（`AppHost`、`ServiceRegistration`、`ServiceProviderHolder`）、`Startup/`、`StartupMethods`、`App.xaml(.cs)` |
| `Core/` | 横断基盤・永続化 | `Core/Logging`、`Core/Utilities`、`DS4Control/DTOXml`、`Profiles`、`BackingStore` 系、`ApiDTO`、`Updater/` |
| `Contracts`（置かない） | — | インターフェースは、実装と同じ層のフォルダ（`Abstractions/`）へ移す。第 2 層が第 1・3 層を使うポートは、**使う側（第 2 層）の `Abstractions/`** に置く（依存の向きを保つ） |

注: 「Services を 1 フォルダにまとめる」は廃止し、責務で分ける（規約 R1〜R7 は、再編後の最終形で置き換える。14-14 で規約を改定）。

## 5. Step 構成

| Step | 内容 | 承認 |
|---|---|---|
| 14-0 | Phase13b の master 取り込みの後、ブランチ作成。ガードテストの足場（名前空間＝フォルダの検査。許可リスト方式で、不一致の件数を減らしていく） | 不要 |
| 14-1 | **全ファイル（約 730）の移行表**を作成: 旧パス → 新パス → 新名前空間。複数型ファイルがないこと、1 ファイル 1 型の確認。曖昧なものは AskUserQuestion で確認 | **必須（表で承認）** |
| 14-2〜14-6 | 層ごとの移動（`git mv` のみ。内容の変更なし。csproj のパス参照は同じコミットで更新）: Input → Output → Core → UI → Processing（最大）→ Composition | 14-1 の範囲内 |
| 14-7〜14-11 | 層ごとの名前空間の書き換え（別コミット）: 各層の `namespace` 宣言、`using`、XAML の `clr-namespace`／`x:Class`、テスト。各コミットで Debug ALL | 同上 |
| 14-12 | `RootNamespace` を `DS4Windows` に変更、`Properties.Resources`／`Translations.Strings` の名前空間、`Type.GetType` の文字列、反射テストの更新 | 同上 |
| 14-13 | ガードテストを厳格化（許可リストを空に）。ラチェットの基準値（ファイル別の `Global`／`AppHost`／`Program.rootHub` の件数）をファイルの移動先へ引き継ぐ | 不要 |
| 14-14 | 規約 §3.3・§3.4 の改定（R1〜R7 を再編後の最終形へ）、モデル図 01〜04、台帳、ロードマップ、完了報告書 | 規約の改定案を提示 |
| 14-15 | Release ビルド 2 か所 → 実機確認（1 回。起動、UI の全画面、リソース／翻訳の表示、プロファイルの読み書き、更新チェックを含む） | 実機確認 |

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| XAML の `x:Class`／`clr-namespace` と code-behind の名前空間の不一致で、ビルド失敗や起動時の例外 | UI は 1 つのコミットで XAML と `.cs` を同時に書き換える。Debug ALL に加え、実機で全画面を開く |
| `Properties.Resources`／`Strings` の名前空間変更で、翻訳の表示が壊れる | `RootNamespace` の変更と同じ Step で、`DependentUpon`／`LastGenOutput` と参照を更新。表示言語の切り替えを実機で確認 |
| 文字列の型名（`Type.GetType`）、XmlSerializer の型名への影響 | 14-1 で全件を洗い出す。DTO は `XmlRoot`／`XmlElement` の明示名を確認し、プロファイルの黄金ファイルのテスト（既存）で守る |
| 名前空間の変更で、同名の型が衝突（`DS4Windows.Mapping` のクラスとフォルダなど） | 14-1 の移行表で衝突を検査（`Processing` を採用） |
| 差分が巨大で、レビューやバグの特定が難しい | 移動と書き換えを別コミットにする（R7）。層ごとに分け、各コミットで Debug ALL を緑にする |
| テスト 467 ファイルの `using` の変更 | 書き換えスクリプトで機械的に更新し、スクリプトは scratchpad に置く。反射テストは個別に更新 |

## 7. 規約の順守

- 1 ファイル 1 型、ファイル名＝型名（既に達成済み。移動中も維持）。
- 移動は `git mv` のみ（R7）。内容の変更を伴う csproj の更新は、ビルドを緑に保つため同じコミットで行うが、`.cs` の内容は変えない。
- 層をまたぐ依存の向きは、規約 §1 に従う（第 2 層が核。第 1・第 3 層から第 2 層への参照は禁止）。移行表の作成中に、この向きに反するファイルを見つけたら、移さずに TODO を残して台帳に記録する（機能変更を避ける）。
- 実機確認は Phase の最後に 1 回だけ。

## 8. 追記（2026-11-04。実施時の変更）

- 14-1 の「移行表の承認」は、ユーザーの指示で**自動分類を信頼して承認なし**に変更した。
- `using` は、**各ファイルに必要最小限の明示的な using を自動生成**する方針（`global using` は使わない）。
- 14-2〜14-6 は層ごとのコミット、14-7〜14-12 は全層をまとめた 1 コミット（`Phase14-Step7-12`）にした。移動のコミットでは内容を変えていない（R7）。
- `App.xaml(.cs)`／`StartupMethods.cs` はルート直下に残した。
