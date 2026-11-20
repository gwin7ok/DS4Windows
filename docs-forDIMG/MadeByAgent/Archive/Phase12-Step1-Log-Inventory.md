# Phase12-Step1a ログ出力の目録（現状の実測）

作成日: 2026-11-03
計画書: `Phase12-Step1-Plan.md`（1a）
方法: `DS4Windows` 配下の `.cs`（bin／obj を除く）を機械的に走査し、コメント行と定義行を除いてログ呼び出しを抽出した。スクリプトは scratchpad に置き、リポジトリには入れていない。同一行に収まらない呼び出し（複数行の引数）の接頭辞・言語は拾えないため、**接頭辞と言語の数値は下限に近い目安**。コードは変えていない。

## 1. 経路別の件数（合計 666 か所・101 ファイル）

| 経路 | 件数 | 備考 |
|---|---|---|
| `AppLogger.LogTrace` | 202 | ファイルのみ |
| `AppLogger.LogDebug` | 173 | ファイルのみ |
| `AppLogger.LogToGui` | 114 | `warning` が false 66／true 47／式 1 |
| `LogDebug`（各サービスの private ヘルパーと `ControlService`） | 42 | 名前は Debug だが GUI ログ（`Debug` イベント）に流れる |
| `AppLogger.LogInfo`／`LogError`／`LogWarn` | 25／23／9 | |
| `AppLogger.LogToTray` | 16 | トレイ通知 |
| NLog を直接使用（`logger.*`／`Logger.*`） | 約 39 | `AppLogger` を経由しない。ほとんどが `LoggerHolder`、`App.xaml.cs` など |
| `Console.WriteLine`／`Debug.WriteLine` | 21／3 | AppLogger の外。コメントアウトを除いた実数 |
| `AppLogger.LogProfileChanged` | 2 | イベント発火 |

## 2. 所見

1. **経路が 5 種類に分散**している: `AppLogger.*`、サービスごとの private `LogDebug`（GUI に出る）、NLog の直接呼び出し、`Console`／`Debug.WriteLine`、`LogToTray`。方針の最初の論点（出し先の分類）。
2. **Info／Warn／Error が少ない**: 合計 57 か所（AppLogger 経由 564 か所の約 10%）。利用者に見える出来事の大半は `LogToGui` が担い、その中で `warning` の真偽がファイルログの Info／Warn を決めている。
3. **接頭辞**: `AppLogger.*` 564 か所のうち、同一行で確認できる接頭辞付きは 168 か所（約 30%）。内訳は `[DI]` 86、`[UpdaterInstall]` 47、`[SortSpecialActionsList]` 9、`[Diag-Toast]` 7、`[ProfileEditor]` 6、`[TAP]` 4 ほか。**接頭辞が機能名・診断名・層名の混在**になっている。
4. **言語**: 同一行で確認できる範囲では、ほぼ英語（日本語は `LogDebug` 7 か所、NLog 直接 2 か所）。複数行の呼び出しと、リソース化された文言（4.2.3）は含まない。
5. **偏り**: 上位 15 ファイルで約 36% を占める。最多は `UpdaterInstaller.cs` 40、`App.xaml.cs` 37、`SyntheticInputCommitter.cs` 34、`ProfileApplicationService.cs` 31。

## 3. ファイル別の上位

| 件数 | ファイル |
|---|---|
| 40 | `Updater/UpdaterInstaller.cs` |
| 37 | `App.xaml.cs` |
| 34 | `DS4Control/Services/SyntheticInputCommitter.cs` |
| 31 | `DS4Control/Services/ProfileApplicationService.cs` |
| 25 | `DS4Forms/UpdaterWindow.xaml.cs` |
| 23 | `DS4Forms/MainWindow.xaml.cs` |
| 21 | `DS4Forms/ProfileEditor.xaml.cs` |
| 20 | `DS4Control/WindowPlacementHelper.cs` |
| 20 | `DS4Library/DS4Device.cs` |
| 16 | `DS4Forms/ViewModels/ControllerListViewModel.cs` |

## 4. ロードマップの「約 624 か所・96 ファイル」との差

今回の走査（666 か所・101 ファイル）は、ロードマップの数え方より広い（NLog の直接呼び出し、`Console`／`Debug.WriteLine`、`LogToTray` を含む）。`AppLogger` の 6 メソッドだけを数えると 546 か所（LogToGui 114＋Trace 202＋Debug 173＋Info 25＋Warn 9＋Error 23）。L2 の作業量は、**方針でどの経路を対象にするか**（項目 7）で変わる。

## 5. 次の Step

1b: 方針の項目を 1 項目ずつ決める（項目 1: ログの出し先の分類から）。
