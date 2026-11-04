# Phase14 完了報告書: 名前空間・フォルダの再編

作成日: 2026-11-04
ブランチ: `Phase14`
計画書: `Phase14-Plan.md`（承認済み）

## 1. 結果の要約

- 本体の `.cs` 約 730 ファイルを、全体 4 層モデルに沿った層別フォルダへ `git mv` で移動した（745 件がリネーム。内容変更なしの移動を別コミットにした）。
- 名前空間を `DS4Windows.<フォルダ>` に統一した。`DS4WinWPF.*`、`DS4Windows.DI`、`DS4Windows.Services`、`DS4Windows.DS4Control` などの旧名前空間は廃止した。
- 各ファイルに、使う名前空間だけを明示する `using` を自動生成した（`global using` は使わない）。
- `RootNamespace`／`StartupObject` を `DS4Windows` に統一した。`Properties.Resources`（`DS4Windows.Properties`）と `Translations.Strings`（`DS4Windows.Translations`）のマニフェスト名を確認した。
- 機能、ログ、設定の保存形式は変えていない。

## 2. 新しいフォルダ構成（層との対応）

| フォルダ | 層 | 主な内容 |
|---|---|---|
| `Input/`（`Devices`、`Hid`） | 第1層 | `DS4Device`、各コントローラーのデバイスクラス、HID |
| `Processing/`（`Abstractions`、`Actions`、`Services`） | 第2層 | `Mapping`、`ControlService`、`SyntheticDispatcher`、Actions、入力処理サービス、第2層のポート（契約） |
| `Output/`（`Devices`、`Kbm`、`Services`） | 第3層 | `DS4OutDevice`／`Xbox360OutDevice`、OutputKBM、スロット、仮想パッド・HidHide・UDP・OSC |
| `UI/`（`ViewModels`、`Converters`、`Themes`、`Services`） | 4-a | 旧 `DS4Forms` 一式、通知、ファクトリ |
| `Composition/`（`Startup`） | 4-b | `AppHost`、`ServiceRegistration`、起動手順。`DI/` は廃止 |
| `Core/`（`Abstractions`、`Logging`、`Utilities`、`Platform`、`Settings`、`Persistence`、`Profiles`、`Updater`） | 横断基盤・永続化 | 設定値の型、XML、DTO、プロファイルの節、ログ、更新 |
| ルート直下 | — | `App.xaml(.cs)`、`StartupMethods.cs`、`GlobalSuppressions.cs` |

フォルダ名を `Mapping` ではなく `Processing` にしたのは、クラス `DS4Windows.Mapping` との名前衝突を避けるため。

## 3. 判断した点

- **自動分類**: 全ファイルの移行表は、ユーザー承認（2026-11-04）に従い自動分類で作り、個別の承認は求めなかった。分類の誤りは、`Processing` の既定に落ちた `Xbox360OutDevice`／`X360Controls`／`X360ControlTables` を `Output/Devices` へ移した 1 件だけ（Step5 の追加コミット）。
- **インターフェース**: 実装と同じ層の `Abstractions/` に置く。第2層が第1・3層・UI の実装を使うポートは、使う側の `Processing/Abstractions/` に置いた（依存の向きを保つため）。`DI/` にあった 78 ファイルは、この規則で振り分けた。
- **名前空間を持たなかった型**: DTO 9 ファイル（`AppSettingsDTO` など）は、これまでグローバル名前空間にあった。`DS4Windows.Core.Persistence.Dto` のファイルスコープ名前空間を付けた。
- **例外**: `Sensorit.Base`（`LowpassFilter`、`OneEuroFilter`）は第三者由来のため名前空間を変えない（ガードテストの許可リスト、2 件）。
- **曖昧な型名**: `FlickStickSettings` は設定（`Core.Settings`）と DTO（`Core.Persistence.Dto`）に同名がある。両方を使うファイルには、`Core.Settings` へのエイリアスを自動で付ける。
- **ルート直下**: `App.xaml(.cs)` と `StartupMethods.cs` は、計画書の `Composition/` ではなくルート直下に残した（規約の「アプリの起点」。WPF のアプリ定義ファイルの移動リスクを避けた）。

## 4. 検証

- Debug ALL（クリーンビルド、警告 0、エラー 0）: 本体＋テスト 3,154 件（既存 3,150 件＋ガード 4 件）と StandaloneTests 11 件が合格。
- 新設のガード `Phase14NamespaceFolderGuardTests`:
  - 全 `.cs` の名前空間がフォルダと一致すること（許可リストは `Sensorit.Base` の 2 件のみ）。
  - 名前空間を持たない型がないこと。
  - 全 XAML の `x:Class` が実在し、フォルダと一致すること。
  - XAML の `clr-namespace` が実在すること。
- ラチェット系のガード（`Global`／`AppHost`／`Program.rootHub` の件数、ファイル名キー）は、ファイル名が変わらないため基準値は変更なしで一致した。
- ソース文字列を検査するテスト（約 155 ファイル）の旧パス文字列を、新しい配置へ更新した。リポジトリのルートを求める処理は、深さに依存しない `SourceFileLocator.RootFromAnchor` へ集約した。
- Release ビルド 2 か所: `DS4Windows\bin\x64\Release\`（publish）、`DS4Windows\bin\x64\DS4Windows\`（build）。どちらも警告 0、エラー 0。

## 5. 規約の改定

`.github/copilot-instructions.md` の次の箇所を改定した（2026-11-04）。

- §3.3 原則3: 「過渡期の例外」を終了し、名前空間＝フォルダ（`DS4Windows.<フォルダ>`）を厳格順守とした。
- §3.4: R1〜R7 を、層別フォルダの規則へ全面改定した（R1 層別フォルダ、R2 インターフェースの置き場、R3 Composition の役割、R4 Actions、R5 設定・永続化と基盤、R6 名前空間、R7 `git mv` のみの移動）。

## 6. 未解決・持ち越し

- CLI の `ChangeLedColor`／`LoadProfile`（Phase13b から。`Phase9-Deferred-Ledger.md` の Phase13b-1）。
- テストの `[Collection("ServiceProviderHolder")]` の改名（Phase13b-3）。名前空間の再編とは独立。
- 実機確認の結果は §7 に記録する。

## 7. 実機確認

（ユーザーの確認結果を追記する）
