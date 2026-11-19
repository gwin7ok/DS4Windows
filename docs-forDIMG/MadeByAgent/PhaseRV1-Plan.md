# PhaseRV1 計画書: 品質の門と土台の整理（アナライザー、未使用の `using`、検査の枠、未参照の型の調査）

作成日: 2026-11-19
状態: **承認済み（2026-11-19）。実施済み**（ブランチ `PhaseRV1`。実績は「実施の結果」）。D1〜D3 は 2026-11-19 にユーザーが選択済み（「決定事項」を参照）。追加パッケージは、RV1-0 の調査のあと、2026-11-19 にユーザーが承認した（AsyncFixer、Threading、Sonar を絞って。Meziantou は入れない）
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-01〜RV-04（§3.2、§3.8、§3.6）
前提: なし。PhaseRV2 以降より先に行う（以降の Phase が、警告 0 を保ったまま検査の網を広げるための土台）。
動き: **コードの動きは変えない**。ログも変えない。

## 目的
1. 未使用の `using`（`Core` の上位層への 25 ファイルと `System.Windows*` の 50 ファイル、`NonFormTimer` の別名 11 行など）を整理し、層の見かけの依存をなくす。
2. 警告 0 を保ったまま、アナライザーと `Nullable` を段階的に導入する足がかりを作る。
3. 数字で守る検査（ラチェット）の枠 `CodeHealthRatchetTests` を作り、RV2 以降が項目を足せるようにする。
4. 未参照の型の候補を、規則 2.4 の手順で調べて記録する（削除の実施は RV6、承認後）。

## 背景（実測。数字は `01-Metrics.md`）
- `Directory.Build.props` は `TreatWarningsAsErrors=false`。`Nullable`・`AnalysisLevel`・`AnalysisMode` の指定はない。`.editorconfig` は `dotnet_analyzer_diagnostic.category-Style.severity = none`。
- `Core` の 25 ファイルに `using DS4Windows.Processing／Input／Output／UI…;`（`Processing` 12 行、`Input` 12 行、`Input.Devices` 5 行など）があるが、型の参照としては 0 件（`LayerDependencyRuleTests`）。例: `Core/Abstractions/IStartupArguments.cs` の `using DS4Windows.Processing;` は、本文が使う `ArgumentParser`（`Core.Utilities`）には要らない。
- `Core` の 50 ファイルに `using System.Windows*;`（雛形の残りが多い）。
- `using NonFormTimer = System.Timers.Timer;` の別名が 11 ファイルにあるが、`new NonFormTimer` は 5 ファイル 7 か所のみ。使っていない別名が多い見込み。
- `scripts/audit/Measure-CodeReview.py` は、手で実行する計測。数字の悪化を、Phase の外で検知する仕組みがない。
- 名前の検索で、ほかにどこからも使われないと確認できた型: `PressActionController`（RV6）、`TriggerType`（`Core/Settings`）、`MappingDoneEventArgs`（`Processing`）、`ViGEmBusInfo`（`Core/Platform/EnvironmentService.cs` 内の宣言のみ）。
- `Nullable` を全体で有効にした場合の警告（2026-11-19 の実測。`-p:Nullable=enable` を付けた 1 回のビルドで、ファイルは変更していない）: **2,153 件（359 ファイル）**。層別は Core 920、UI 684、Processing 300、Output 98、Input 84、ルート 38、Composition 24、Cli 5。種類は、`null` の渡し・代入 CS8600／CS8625 が計 1,027、`null` かもしれない値の使用 CS8602 が 349、非 null のプロパティの未初期化 CS8618 が 263、そのほか CS8622 182、CS8603 153、CS8604 113。Core の大半は設定・保存の型（`BackingStore`、DTO）と見ている（テストのプロジェクトは数えていない）。
- 既存の「数字を凍結し、実数と一致を検査する」仕組み: `LayerDependencyBaseline`、`ConstructorArityBaseline`、`Phase10Step4dW0RuntimeKeyRatchetTests` など（`SourceFileLocator`・`SourceText` を使うもの）。

## 方針・制約
- 警告 0・エラー 0（Debug ALL。`-c Debug /p:platform=x64`）を毎 Step 保つ。診断を足すときは、先に `suggestion` で件数を数え、0 にしてから `warning` へ上げる。
- `DS4WinWPF.csproj` の `NoWarn`（CS0103 ほか）は、WPF の 1 回目のビルドの誤りを避ける既存の措置なので触らない。
- 自動生成コード（`*.Designer.cs`、`*.g.cs`）は対象外。
- 追加するパッケージは、調査（RV1-0）と承認（D1）を経てから。
- 未参照の型は、この Phase では調べて記録するだけ。削除は RV6 で、承認を得てから。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV1-0 | 準備。(1) `Measure-CodeReview.py` に「本体が完全に空の `catch`」と「コメントのみの `catch`」を分けて数える項目を足し、基準を取り直す（RV2 の完了の定義に使う。2026-11-19 の採点基準の整備で、先行して追加済み: 「理由のコメントがない空の `catch`」370 件、「イベントハンドラーの形以外の `async void`」9 件。RV1-0 では、この 2 項目の基準を `01-Metrics.md` へ記録する）。(2) アナライザーの比較調査（規約 §4「慣れない機能は、まず GitHub のオープンソースを検索して比較」）: .NET 標準の `CAxxxx`／`IDExxxx`、Meziantou.Analyzer、SonarAnalyzer.CSharp、Roslynator。見るのは、何を検出できるか（空の `catch`、`async void`、未使用の `using`）、ノイズ、凍結の仕組み。結果は `PhaseRV1-Analyzer-Survey.md` に記録 | スクリプトの再実行で、同じ数字が再現すること | **完了**（調査書 `PhaseRV1-Analyzer-Survey.md`。6ca1a346） |
| RV1-1 | 未使用の `using` の整理。`dotnet format style --diagnostics IDE0005` を 1 度走らせ、差分を目で確認する（`#if` の中だけで使う `using` を消していないか）。対象は `Core` の 75 ファイル（上位層 25＋`System.Windows*` 50）を最初に、続けて全層。`NonFormTimer` の未使用の別名も消す。コミットは層ごとに分ける | ビルド、全テスト、`LayerDependencyRuleTests`、`ViewModelWpfReferenceRatchetTests` | **完了**（層ごとの 6 コミット: 126b4c04 Core／e58faf70 Input／bf03a1fd Output／4718009e Processing／830f4e04 UI／b0d63477 Composition・ルート。約 2,400 行の未使用の `using` を 444 ファイルから除いた） |
| RV1-2 | アナライザーの段階導入（D1＝(B)。標準のアナライザーに、追加パッケージを足す。入れるパッケージと規則は、RV1-0 の調査の結果を一覧にしてユーザーの承認を得てから）。`.editorconfig` で `suggestion` にして件数を測り、件数の少ない規則から 0 にして `warning` へ上げる。上げた規則は `PhaseRV1-Analyzer-Survey.md` の表に記録 | Debug ALL（警告 0） | **完了**（154a181b。AsyncFixer・Threading・Sonar を絞って導入。Meziantou は入れない。規則は `Analyzers.globalconfig`。実行のたびに止まる・落ちるテストの安定化は a4c4f1b6） |
| RV1-3 | `Nullable` の試験導入（D2。報告書の G5）。試験の範囲は `Core/Abstractions` と `Composition`（契約と組み立て。値の入れ物より警告が少ない見込み）。ファイル単位の `#nullable enable` で、警告を 0 にし、手間（件数・直し方の型・1 件あたりの時間）を記録する。D2＝(B) のため、層ごとの全体の有効化は別 Phase（PhaseRV9。予定）で行う。ここの記録が、その計画書の根拠になる | Debug ALL（警告 0） | **完了**（82d2d81e、記録 04011125 `PhaseRV1-Nullable-Trial.md`。2 層 56 ファイル、27 件を 0 に） |
| RV1-4 | 検査の枠 `CodeHealthRatchetTests`（基準値は `CodeHealthBaseline`）。「基準を超えない」と「実数と一致する」の両方を検査する（既存のラチェットと同じ形。数字を減らしたら、同じコミットで基準も更新）。最初の項目: `Core` の上位層の `using`（0）、`using System.Windows*` を持つ `Core` のファイル（0）、未使用の `NonFormTimer` の別名（0）。RV2 以降が、空の `catch`、`async void`、`Command` バインディングなどを足す | 新テスト自身（偽の入力で、超過・不一致が失敗することを確かめる） | **完了**（f29584ba。基準は `CodeHealthBaseline`） |
| RV1-5 | 未参照の型の調査（規則 2.4 の手順 1・2・4）。`TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo` について、`git log -S` と計画書（`docs-forDIMG/`）で作った経緯を調べ、「使うべき（TODO を付ける）」か「使うべきでない（削除候補）」かの判断を、根拠つきで `PhaseRV1-Unreferenced-Types.md` に記録する。`PressActionController` は RV6 で扱う。ほかの 51 件（誤検出を除く）は、RV6 の最初の Step で調べる | — | **完了**（547f2de0。`PhaseRV1-Unreferenced-Types.md`） |
| RV1-6 | 文書: `Code-Review-2026-11-19/01-Metrics.md` の基準の取り直し、`DI-App-Wide-Migration-Plan.md` §6.18 の表の更新 | `ModelDiagramGuardTests`（モデル図に触れたときだけ） | **完了**（本コミット） |
| RV1-7 | 評価の再算定（採点基準 `Code-Review-2026-11-19/03-Rubric.md`）。(1) `scripts/audit/Rubric-Inputs.json` の確認項目と手入力の値を、根拠（実行したテスト・実測）を `evidence` に書いて更新する。(2) `python -I scripts/audit/Score-CodeReview.py --json docs-forDIMG/Code-Review-2026-11-19/scorecard/RV1.json --label "RV1 完了" --diff docs-forDIMG/Code-Review-2026-11-19/scorecard/RV0-baseline.json` で、採点して保存する。(3) `02-Scorecard.md` に列と理由（等級が変わった観点、点数が動いた指標、予想とのずれ）を足し、`01-Metrics.md` §13 の「現在」を `Measure-CodeReview.py` の実測で取り直す。(4) ユーザーへ、点数・等級の変化と、次の Phase で動かす指標を報告する。机上の予想（`03-Rubric.md` §5）で動く観点: 各層の役割分担 B+→A、採用手段の統一 C+→B−、メンテナンス性 B−→B。ソースは変更しない（記録のみ） | `Score-CodeReview.py`（記録のみ） | **完了**（`scorecard/RV1.json`、`02-Scorecard.md` §3）。各層の役割分担 B+→A、メンテナンス性 B−→B。採用手段の統一は 59.9%（C+。予想 B− に約 3 点届かず） |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | アナライザーの導入方式 | (A) .NET 標準のアナライザー（`AnalysisLevel`／`AnalysisMode`＋`.editorconfig`）だけ／(B) (A)に、Meziantou.Analyzer などの追加パッケージ／(C) 導入しない | (A) 利点: 追加の依存なし、ビルドの時間が増えにくい。欠点: 空の `catch` などは検出できない（RV1-4 の検査で補う）。(B) 利点: 検出が広い。欠点: パッケージの追加、ノイズ、`TreatWarningsAsErrors` なしでも警告 0 を保つ手間。(C) 利点: 手間なし。欠点: 品質の門が緩いまま。**推奨は (A)**。(B) は、調査（RV1-0）の結果、費用に見合う規則があるときだけ |
| D2 | `Nullable`（G5） | (A) 新規・触るファイルだけ `#nullable enable`。試験は `Core/Abstractions`・`Composition`／(B) 層ごとに段階的に全体を有効化／(C) 導入しない | (A) 利点: 手間が小さく、警告の洪水がない。欠点: 全体では混在が長く続く。(B) 利点: 最終的に揃う。欠点: 約 10 万行で、警告の件数が大きく、保存の形・毎フレームの経路に触れる。(C) 利点: 手間なし。欠点: null の不具合を型で防げない。**推奨は (A)**（試験の結果を見て (B) へ進むかは RV1 の完了時に再判断） |
| D3 | 未使用の `using` の維持の仕方 | (A) `dotnet format` を、Phase の終わりに走らせる手順にする（検査は `CodeHealthRatchetTests`）／(B) ビルドで IDE0005 を警告にする（`GenerateDocumentationFile` が要る可能性。要確認） | (A) 利点: 設定が単純。欠点: 手順を忘れると増える（検査が見張る）。(B) 利点: 自動。欠点: ドキュメント生成の警告（CS1591）が大量に出るおそれ。**推奨は (A)**（(B) は RV1-0 の調査で成立すると分かれば再検討） |

## 決定事項（2026-11-19、ユーザーが選択）
| # | 決定 | 計画への反映 |
|---|---|---|
| D1 | (B) 標準のアナライザー＋追加パッケージ | RV1-0 で候補（Meziantou.Analyzer、SonarAnalyzer.CSharp、Roslynator）を比べ、入れるパッケージと規則を一覧にして承認を得る。入れる規則は、`suggestion` で件数を測り、0 にしてから `warning` へ上げる（警告 0 を保つ）。ビルド時間の増加を測って記録する |
| D2 | (B) 層ごとに全体を有効化 | RV1 では試験（RV1-3）まで行う。全体は **PhaseRV9（予定）** で、警告の少ない層から（Composition 24 → Input 84 → Output 98 → Processing 300 → UI 684 → Core 920）。Core の設定・保存の型（`BackingStore`、DTO）は最後にし、保存の形のゴールデンテストを先に確認する。他の Phase と同じファイルを触るため、順序（RV2〜RV8 の前か後か、触るファイルから先か）は PhaseRV9 の計画書で決める。計画書は、RV1-3 の試験の結果を見てから作る |
| D3 | (A) 手順＋検査 | `dotnet format` を各 Phase の終わりに走らせる手順にし、RV1-4 の `CodeHealthRatchetTests` が見張る。(B)（ビルドで IDE0005 を警告にする）は、RV1-0 の調査で成立すると分かれば再検討 |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- RV1-1 のコミットは、`using` の行の増減以外の差分がないことを `git diff` で確かめる。
- 実機確認は不要（動きが変わらない）。

## リスク
- 低。`IDE0005` の自動除去が、`#if`・XAML の `x:Class` の生成コード・拡張メソッドの `using` を誤って消すおそれ（ビルドで見つかる）。
- アナライザーの `warning` 化で、警告 0 の維持に手間がかかる（件数の少ない規則だけを上げる方針で抑える）。

## 完了の定義
- `Core` の未使用の `using`（上位層・`System.Windows*`）が 0。残る 4 ファイル（上位層 1、`System.Windows*` 3）は実際に使う型・定数のため、基準 1・3 として `CodeHealthBaseline` に凍結し、`CodeHealthRatchetTests` が検査する（0 にするには、型や定数を `Core` へ移す。手順は基準のコメント）。
- 未使用の `NonFormTimer` の別名が 0（残る 5 行は使用中）。
- D1 で決めたアナライザーの規則が `warning` として有効で、Debug ALL が警告 0・エラー 0。
- `Nullable` の試験の結果（件数・手間・推奨）が記録されている。
- `Measure-CodeReview.py` に、完全に空の `catch` の項目があり、基準が `01-Metrics.md` にある。
- `PhaseRV1-Analyzer-Survey.md`、`PhaseRV1-Unreferenced-Types.md` がある。
- 評価の再算定（最終 Step）が済み、`02-Scorecard.md` に記録され、ユーザーへ報告されている（予想とのずれの理由つき）。

## 実施の結果（2026-11-19）
- **未使用の `using`（RV1-1）**: `IDE0005` を 1 度走らせ、差分を目で確かめて除いた（約 2,400 行・444 ファイル。`#if` の中だけで使う `using` はなかった）。`Core` の上位層への `using` は 25 ファイル→1、`System.Windows*` は 50→3 になった。残る 4 件は、削除すると `CS0103` になる（実際に使う）ため、完了の定義の 0 は満たさず、基準を 1・3 に凍結した。
- **アナライザー（RV1-2）**: `Analyzers.globalconfig`（新規）で、件数が 0 の規則は既定（警告）、違反がある規則は `none`、段階導入の 5 規則（AsyncFixer03・VSTHRD002・VSTHRD110・S2486・S1481）は `suggestion` で凍結した。`EnableNETAnalyzers=false` と `category-Style` の `none` は外した。標準の `CA2200`（`throw ex;`）は 1 件を是正した。ビルドは 39 秒→54 秒（`--no-incremental`）。未使用の `using`（D3）は、ビルドの警告にすると `dotnet format` の連鎖が必要になるため、決定どおり (A)（Phase の終わりに `dotnet format`＋`CodeHealthRatchetTests`）のままとした（測定は調査書 §6）。
- **`Nullable`（RV1-3）**: 2 層（`Core/Abstractions` 37、`Composition` 19）の 56 ファイルで警告 27 件を 0 にした。1 件あたりの手間と直し方の型は `PhaseRV1-Nullable-Trial.md`（PhaseRV9 の根拠）。
- **検査の枠（RV1-4）**: `CodeHealthRatchetTests`（3 項目。基準は `CodeHealthBaseline`）。
- **未参照の型（RV1-5）**: `TriggerType`・`MappingDoneEventArgs` は削除候補（RV6 で承認を得て）。`ViGEmBusInfo` は private のネスト型で、`EnvironmentService.cs` の中で使われているため誤検出。
- **ずれ**: 計画にない作業として、実行のたびに止まる・落ちるテストの安定化を行った。画面部品を作る 6 つのテストが自前の STA スレッドを作っていて、WPFLocalizeExtension が、終わったスレッドの部品の Dispatcher を待って、共有の STA スレッドが止まっていた（`ThePanel_LoadsWithTheTheme` など 10 件が 1 分ずつ失敗。GC の時機しだいで出る）。6 つを `WpfPanelTestRunner.Run` へ寄せ、`SharedStaThreadRuleTests` が自前の STA スレッドを作らないことを見張る。ドングルの特性化テスト（`Ds4DeviceBehaviorTests`）は、時間しだいで同じ書き込みが 1〜3 回になるため、同じ行の繰り返しを 1 回にまとめて比べる。
- **確認**: Debug ALL（`Build-Debug.ps1`、テストの建て、`Execute Test`）が、警告 0・エラー 0・テスト 3,626 件＋11 件の合格。全件を続けて 3 回流して、毎回合格した。

## 関連計画
- 報告書: `Code-Review-2026-11-19/00-Report.md`、`01-Metrics.md`。
- 以降: `PhaseRV2-Plan.md`（`CodeHealthRatchetTests` に項目を足す）、`PhaseRV6-Plan.md`（未参照の型の削除）、`PhaseRV9`（予定。`Nullable` の層ごとの全体有効化。RV1-3 の後に計画書を作る）。
- 規約: `.github/copilot-instructions.md` §2.4（未参照コード）、§3.1（層の検査）、§4（遅いテスト・性能計測の印）。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
- 2026-11-19: D1＝(B)、D2＝(B)、D3＝(A) をユーザーが選択。D2 の判断のため、`Nullable` を全体で有効にした警告数を実測して背景へ追記（2,153 件）。全体の有効化は PhaseRV9（予定）へ切り出す。
- 2026-11-19: 最終 Step「評価の再算定」を追加（採点基準 `Code-Review-2026-11-19/03-Rubric.md`、`scripts/audit/Score-CodeReview.py`）。基準 v1 は同日ユーザーが確定。
