# PhaseRV1: アナライザーの調査と導入の記録（RV1-0・RV1-2）

作成日: 2026-11-19
状態: 調査（RV1-0）と導入（RV1-2）は完了。入れるパッケージと規則は、2026-11-19 にユーザーが承認した（AsyncFixer＋Threading＋Sonar を絞って。Meziantou は入れない）。

## 1. 目的と方法

- 目的: ビルドで見張れる品質の門を、費用（ビルド時間・ノイズ・依存の追加）に見合う範囲で作る。見たいのは、空の `catch`、`async void`、ブロッキング待ち、観測されない `Task`、未使用の `using`。
- 方法: リポジトリには手を入れず、一時の `Directory.Build.targets`（`PackageReference` を足す）と、全規則を警告にする globalconfig（`dotnet_analyzer_diagnostic.severity = warning`）を使って、`DS4Windows/DS4WinWPF.csproj` を `-c Debug /p:platform=x64 --no-incremental` でビルドした。ログは、位置（ファイル・行・桁・ID）で重複を除いた（最初の WPF のビルドは、一時プロジェクト `_wpftmp` で警告が二重に出るため）。一時ファイルは、調査後に削除した（コミットしていない）。
- 環境: .NET SDK 8.0.428。ベースライン（追加なし）は警告 0、ビルド 39 秒。

## 2. 調査で分かった前提（RV1-2 の設計に効く）

| # | 事実 | 影響 |
|---|---|---|
| 1 | 調査の時点で、`DS4WinWPF.csproj` は `EnableNETAnalyzers=false`。標準の CA 規則は、`EnableNETAnalyzers=true` と `AnalysisMode` を足したときだけ動く（RV1-2 で `false` を削除した） | 標準のアナライザーを使うなら、csproj の設定が要る |
| 2 | リポジトリの `.editorconfig` に `dotnet_analyzer_diagnostic.category-Style.severity = none` がある（IDE 系の規則をすべて無効にしている）。globalconfig より `.editorconfig` が優先されるため、IDE0005 などは、個別の ID の指定を `.editorconfig` に足さないと出ない | 未使用の `using` を測るには、一時的に `dotnet_diagnostic.IDE0005.severity = warning` を足す（RV1-1 で実施。恒久化は D3＝(A) のため、しない） |
| 3 | IDE 系の規則をビルドで出すには、`EnforceCodeStyleInBuild=true` と、IDE0005 は `GenerateDocumentationFile=true` も要る。ドキュメント生成を有効にすると、XML コメントの不整合（CS1573、CS1574 など）が約 140 件出る | D3＝(B)（ビルドで IDE0005 を常時警告）は、`GenerateDocumentationFile` の副作用のため、採らない（D3＝(A) を維持） |
| 4 | Meziantou.Analyzer **3.x は Roslyn 5.0 以上専用**で、この SDK（Roslyn 4.x）では読み込まれず、**警告 0 件になる**（エラーも出ない。気づきにくい）。2.0.302（最後の 2.x）では動く | Meziantou を使うなら 2.0.302 に固定する。SDK を上げるときに、3.x へ移す |
| 5 | Meziantou 2.0.302 は、MA0037（空の文）など一部の規則が、既定でエラーになる（今は 23 件。`;;` の余分な `;`） | 導入時に、規則ごとに `.editorconfig` で重大度を指定する |

## 3. 候補の比較

| 候補 | バージョン | 診断の件数（重複を除く） | 規則の種類 | ビルド時間（ベースライン 39 秒） | ノイズ | 本件に効く規則（件数） |
|---|---|---|---|---|---|---|
| .NET 標準（組み込み。`AnalysisMode=All`＋スタイル） | SDK 8.0.428 | 4,089 | 93 | 58 秒（＋49%） | 非常に多い（設計・命名・国際化の指摘が大半） | CA1031 一般の例外の捕捉 749、CA1510 `ThrowIfNull` 70 |
| SonarAnalyzer.CSharp | 10.36.0.15337 | 3,097 | 101 | 66 秒（＋69%） | 多い（S125 コメントアウトされたコード 461、S1104 公開フィールド 468、S1135 TODO 43 など） | S2486 握りつぶした例外 359（計測の「理由なしの空 catch」370 に近い）、S108 空のブロック 386、S3168 `async void` 8、S1481 未使用のローカル 36 |
| Roslynator.Analyzers | 5.0.1 | 29 | 6 | 42 秒（＋8%） | ほぼなし | RCS1075 空の `catch`（`Exception` を捕捉するもの）17 |
| Microsoft.VisualStudio.Threading.Analyzers | 18.12.1 | 120 | 5 | 47 秒（＋21%） | VSTHRD001 は JoinableTaskFactory 向けで 45 件がノイズ | VSTHRD100 `async void` 14（ハンドラーを含む）、VSTHRD002 ブロッキング待ち 11、VSTHRD110 観測されない `Task` 40 |
| AsyncFixer | 2.1.0 | 11 | 3 | 41 秒（＋5%） | ほぼなし | AsyncFixer03 `async void` 9（ハンドラー以外の数と**一致**）、AsyncFixer02 ブロッキング 1 |
| Meziantou.Analyzer | 2.0.302（3.x は不可。前提 4） | 1,285（うちエラー 23） | 31 | 計測できず（エラーで 19 秒で止まった） | 多い（MA0011 カルチャー 475、MA0006 文字列比較 207、MA0051 メソッドの長さ 162 など） | MA0134 観測されない `Task` 40、MA0037 空の文 23 |

### 本件に効く規則の位置（再現用）

- `async void`（ハンドラー以外。計測の 9 件）: `Output/Services/UdpMotionService.cs:180`、`UI/MainWindow.xaml.cs:806`、`UI/ViewModels/AutoProfilesViewModel.cs:194・206・218・230`、`UI/WelcomeDialog.xaml.cs:182・299・404`。VSTHRD100 の 14 件は、これに加えて `MainWindow.xaml.cs:1203・1223・1252・1291` と `UpdaterWindow.xaml.cs:79`（イベントハンドラーの形の `async void`）を含む。
- ブロッキング待ち（VSTHRD002 の 11 件）: `AppShutdownSequence.cs:58`、`Changelog.cs:87〜94`、`BluetoothBatteryReader.cs:53`、`UdpMotionService.cs:97`、`MacroPlayback.cs:175・223`、`DeviceDisconnectHandler.cs:90`。
- 空の `catch`（RCS1075 の 17 件）: `App.xaml.cs`、`IpcMemoryMap.cs`、`UdpServer.cs`、`XboxParentHider.cs` ほか。計測の 370 件（理由のコメントがない空の `catch`）のうち、`Exception` を捕捉するものだけを拾う。個別の例外型の空の `catch` は、Sonar の S2486 が拾う（359 件）。

## 4. 評価と推奨（要承認）

| 目的 | 最も合う規則 | 理由 |
|---|---|---|
| `async void` を増やさない | AsyncFixer03（9 件） | 計測の「ハンドラー以外の `async void`」と一致する。ノイズなし。VSTHRD100 はハンドラーも拾うので、`.editorconfig` で例外にする手間が出る |
| ブロッキング待ちを増やさない | VSTHRD002（11 件） | 計測の `.Result`・`.Wait()` より狭いが、デッドロックの危険がある形に絞れる。AsyncFixer02（1 件）は包含される |
| 観測されない `Task` を増やさない | VSTHRD110（40 件）または MA0134（40 件） | どちらも同じ件数。VSTHRD110 を推す（同じパッケージで VSTHRD002 も得られる） |
| 理由のない空の `catch` を増やさない | Sonar の S2486（359 件）、または Roslynator の RCS1075（17 件） | RCS1075 は小さく始められるが、計測の 370 件の大半を見逃す。S2486 は計測に近いが、Sonar 全体はノイズが大きい（規則を 2〜3 個に絞れば問題ない） |
| 未使用の `using` を増やさない | IDE0005（RV1-1 で測定。D3＝(A)） | 前提 3 のとおり、ビルドの常時警告にはしない。`dotnet format` と `CodeHealthRatchetTests` で守る |

**推奨**: AsyncFixer（AsyncFixer03）＋ Microsoft.VisualStudio.Threading.Analyzers（VSTHRD002・VSTHRD110 のみ）。空の `catch` は、Sonar を入れず、RV2 の `CodeHealthRatchetTests`（`catch` の数の凍結）で守る。理由: ビルド時間の増加が小さく（＋5〜21%）、ノイズが少なく、すべての規則を 0 件から始められる（既存の 9・11・40 件は、RV2〜RV3 の是正の対象で、`suggestion` から段階的に `warning` へ上げる）。Sonar の追加（ビルド＋69%）は、S2486 だけが目的なら、費用に見合わない。

**決定（2026-11-19。ユーザー）**: AsyncFixer、Threading（VSTHRD002・VSTHRD110）、Sonar（S2486・S1481 に絞る）を入れる。Meziantou は、SDK が上がるまで入れない（3.x が動かず、2.x の固定が要るため）。推奨案に、Sonar の 2 規則を足した形。

## 5. 導入の記録（RV1-2）

### 設定の変更

| 場所 | 変更 |
|---|---|
| `DS4Windows/DS4WinWPF.csproj` | `EnableNETAnalyzers=false` を削除（標準のアナライザーを有効にした。既定の重大度で出る警告は `CA2200` の 1 件だけで、直した）。`AsyncFixer` 2.1.0、`Microsoft.VisualStudio.Threading.Analyzers` 18.12.1、`SonarAnalyzer.CSharp` 10.36.0.15337 を `PrivateAssets=all`（ビルドだけで使い、配布物に入れない）で追加。`Analyzers.globalconfig` を `GlobalAnalyzerConfigFiles` で読み込む |
| `.editorconfig` | `dotnet_analyzer_diagnostic.category-Style.severity = none`（スタイル診断の一括無効）を削除。IDE 系の規則は、既定の重大度で使う（エディターの提案。`EnforceCodeStyleInBuild` は無効のままで、ビルドの警告にはならない）。`IDE0090`・`IDE0028`・`IDE0300` の none は残した |
| `Analyzers.globalconfig`（新規） | 追加のパッケージの規則の設定。段階導入の 5 規則は `suggestion`、今ある違反がある規則は `none` に凍結。違反が 0 件の規則は、既定の重大度（警告）のままで、新しい違反をビルドで止める。運用の規則はファイルの先頭に書いた |
| `Output/Udp/UdpServer.cs` | `CA2200`: `catch (SocketException ex) { ...; throw ex; }` を `catch (SocketException) { ...; throw; }` にした（同じ例外を投げ直す。スタックの情報が残る） |

ビルド時間: 54 秒（ベースライン 39 秒。`--no-incremental`）。警告 0・エラー 0（本体・CLI・テスト・StandaloneTests）。

### 規則の記録

| 規則 | パッケージ | 導入時の件数 | 今の重大度 | `warning` へ上げる予定 |
|---|---|---|---|---|
| CA2200 | 標準 | 1 | warning（既定。0 件にした） | 済み |
| AsyncFixer03（ハンドラー以外の `async void`） | AsyncFixer | 9 | suggestion | RV2（`async void` の整理）で 0 にして上げる |
| VSTHRD002（Task の同期的な待ち） | Threading | 11 | suggestion | RV2 で 0 にして上げる |
| VSTHRD110（観測されない Task） | Threading | 40 | suggestion | RV2 以降で 0 にして上げる |
| S2486（握りつぶした例外） | Sonar | 359 | suggestion | RV2（空の `catch` の整理）で 0 にして上げる |
| S1481（未使用のローカル変数） | Sonar | 36 | suggestion | 件数が小さい。RV2 のついでか、次の整理の Phase で 0 にして上げる |
| そのほかの Sonar 104 規則、VSTHRD001・100・105・200、AsyncFixer01・02 | 各パッケージ | 約 2,800 | none（凍結） | 使うと決めたときに、違反を 0 にしてから none の行を消す |

`Analyzers.globalconfig` の 2 つ目以降の none には、導入時の件数が多い規則（S1104 公開フィールド 468、S125 コメントアウトされたコード 461、S108 空のブロック 386、S2933 169 など）が含まれる。これらは、規則を使うかどうかの判断が要る（RV2 以降で、必要なものだけを選ぶ）。

## 6. 参考: 未使用の `using`（IDE0005）の測定

RV1-1 で、IDE0005 を使って、未使用の `using` を全層から消した（一時的に `.editorconfig` へ `dotnet_diagnostic.IDE0005.severity = warning` を足し、`EnforceCodeStyleInBuild=true`＋`GenerateDocumentationFile=true` でビルドして測り、消したあとで元に戻した。恒久化していない）。

| 項目 | 値 |
|---|---|
| 1 回目のビルドで出た未使用の `using` | 700 件・444 ファイル（Core 242、UI 203、Processing 141、Output 56、Input 48、ルート 5、Composition 5） |
| 消した `using` の合計（連鎖の分を含む） | 約 2,400 行・444 ファイル（Core 142、Processing 120、UI 105、Output 37、Input 35、Composition 4、ルート 1） |
| 差分の中身 | `using` の行・空行の削除と、先頭行の BOM の移動だけ（`#if` の中だけで使う `using` を消していないことも確認した） |

**IDE0005 は、1 回のビルドで出し切れない**。1 回目は 700 件だが、消すと、次のビルドで別の `using` が新しく出る（連鎖。ビルドごとの残りは 335 → 245 → 190 → 142 → 107 → 93 → 69 → 61 → 55 → 52 → 47 → 47 → 47 → 40）。観察では、一部のファイル（列挙型だけのファイルなど）で、1 回のビルドに 1 行ずつしか報告されなかった（原因は未調査）。`dotnet format style ./DS4Windows/DS4WinWPF.csproj --diagnostics IDE0005 --severity warn`（約 20 秒）は、連鎖の残りを 1 回で消した。したがって、**維持の手順は、`dotnet format` を 1 回走らせること**（D3＝(A)）で、ビルドの警告を数える方法は、件数が実態より少なく出るため、基準値の根拠にしない（検査は `CodeHealthRatchetTests` の `using` の数え方で行う）。

手順（次の Phase 以降）:

1. `.editorconfig` の `dotnet_analyzer_diagnostic.category-Style.severity = none` の次の行に `dotnet_diagnostic.IDE0005.severity = warning` を一時的に足す。
2. `dotnet format style ./DS4Windows/DS4WinWPF.csproj --diagnostics IDE0005 --severity warn` を走らせる。
3. `.editorconfig` を元に戻す（`git checkout .editorconfig`）。差分が `using` の行・空行だけであることを確かめ、Debug ALL を通す。
