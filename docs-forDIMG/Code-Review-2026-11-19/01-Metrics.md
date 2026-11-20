# コードレビュー（2026-11-19）: 計測値と、PhaseRV ごとの目標値

作成日: 2026-11-19
対象: ブランチ `master`（コミット `35f12b0a`）
計測: `python -I scripts/audit/Measure-CodeReview.py`（全項目）。項目を選ぶときは `--only size,mvvm,catch,comments,iface,unref,clone,cloneexact,fields,naming,tests,di`。
採点: `python -I scripts/audit/Score-CodeReview.py`（基準は `03-Rubric.md`、記録は `02-Scorecard.md`。2026-11-19 追記）。9 観点の A〜D を、同じ物差しで算定する。
注意: 数字は正規表現による概算（コンパイラーの解析ではない）。Phase の完了の基準に使うときは、**同じスクリプトを同じ引数で**再実行して比べる。スクリプトを直したら、基準値も同じコミットで取り直す。

## 0. RV9 完了時の実測（2026-11-20。`Measure-CodeReview.py`）

基準点（以下の §1 以降は、RV0 時点の記録のまま残す）。全指標の最新値は `scorecard/RV9.json` の `metrics`。

| 指標 | RV0 | RV9 |
|---|---|---|
| ファイル／行（本体） | 965／103,549 | 987／101,407 |
| 500 行以上のファイル | 32 | 32 |
| XAML の `Command` バインディング | 0 | 260 |
| XAML の `Click` 属性 | 193 | 29 |
| code-behind の行（`.xaml.cs`） | 10,779 | 9,679 |
| イベントハンドラー型のメソッド | 278 | 95 |
| ViewModels の行 | 14,558 | 15,222 |
| `+= メソッド名;`（手結び） | 227 | 75 |
| `INotifyPropertyChanged` 直接実装 | 13 | 0 |
| `Nullable` を有効にした層／品質の門 | 1／2/3 | 6／3/3 |

## 1. 規模（`size`）

| 層（最上位のフォルダー） | ファイル | 行 |
|---|---|---|
| Input | 75 | 10,135 |
| Processing | 314 | 24,478 |
| Output | 59 | 7,185 |
| UI | 221 | 30,147 |
| Composition | 19 | 1,302 |
| Core | 274 | 29,350 |
| （ルート） | 3 | 952 |
| **合計** | **965** | **103,549** |

500 行以上のファイルは 32 件。上位 12: `UI/MainWindow.xaml.cs` 1,763／`UI/ProfileEditor.xaml.cs` 1,634／`Core/Persistence/Dto/AppSettingsDTO.cs` 1,179／`Input/InputDeviceCore.cs` 1,133／`Input/Devices/DualSenseDevice.cs` 1,089／`Core/Settings/BackingStore.cs` 1,027／`Input/Devices/SwitchProDevice.cs` 999／`UI/BindingWindow.xaml.cs` 938／`App.xaml.cs` 928／`Processing/Mapping.cs` 895／`UI/RecordBox.xaml.cs` 873／`Input/Devices/DualShock4Device.cs` 802。

## 2. 画面の結びつけ（`mvvm`）

| 指標 | 値 |
|---|---|
| XAML の `Command` バインディング | 0 |
| XAML の `Click` 属性 | 193 |
| XAML のその他のイベント属性 | 46 |
| XAML の `{Binding}` | 771 |
| XAML の `x:Name` | 686 |
| code-behind（`.xaml.cs`） | 39 ファイル／10,779 行／イベントハンドラー型のメソッド 278 |
| ViewModels | 14,558 行 |
| `RelayCommand` を使うファイル（定義を除く） | 0 |
| code-behind が部品へ直接代入 | `IsEnabled` 61／`Visibility` 83／`Content` 106／`ItemsSource` 15／`SelectedIndex` 25（計 290） |
| code-behind の `+= メソッド名;`（手結び） | 227 |
| 変更通知 | `INotifyPropertyChanged` 直接実装 13／`ObservableObject` 継承 12 |

## 3. 握りつぶしと防御コード（`catch`）

| 指標 | 値 |
|---|---|
| `catch` 全体 | 811 |
| 本体が空（コメントのみを含む） | **389（48%）** |
| うち、理由のコメントがない（生のソースで中括弧の中が空白だけ） | **370**（2026-11-19 の採点基準の整備で、項目を追加。RV2 の完了の定義の「何も書かない空の `catch`」の基準） |
| 空の `catch` の層別 | Processing 213／Core 106／UI 36／ルート 14／Output 10／Composition 7／Input 3 |
| 上位のファイル | `KeyButtonActionController.cs` 59／`ProfileStickAxialLoadSection.cs` 29／`DefaultActionManager.cs` 21／`ProfileTouchpadMouseLoadSection.cs` 15／`App.xaml.cs` 14／`MacroPlayback.cs` 12／`RepeatHelper.cs` 11／`KeyButtonControllerRegistry.cs` 11 |
| ログ呼び出しを `try { } catch { }` で包む行 | 41 |
| `async void` | 14（`MainWindow` 5、`AutoProfilesViewModel` 4、`WelcomeDialog` 3、`UdpMotionService`・`Mapping`・`UpdaterWindow` 各 1）。うち、イベントハンドラーの形（`object`, 〜）以外 **9**（2026-11-19 に項目を追加。RV2 の基準） |
| `.Result`・`.Wait()` | 21 |
| `Thread.Sleep` | 13 |
| `?? throw new ArgumentNullException` | 666 |

## 4. コメントに残る経緯（`comments`）

| 指標 | 値 |
|---|---|
| `Phase<数字>` を含む行 | 1,656（含むファイル 487／全 965 の 50%） |
| `//` コメントに「旧」を含む行 | 759 |
| TODO／FIXME／HACK | 44（原則4 の TODO。**消さない**） |
| XML コメントに Phase 名を持つテストのファイル | 415 |

**RV8 後（2026-11-20）**: `Phase<数字>` を含む行 225（RV8-5 の後。RV8-4 の直後は 258）、`//` コメントに「旧」を含む行 468、TODO／FIXME／HACK 44（変化なし）。残る `Phase<数字>` の行の多くは、実在するテストのクラス名（例: `Phase9EOscServiceDifferentialTests`）を指す参照で、RV8-5 のテストの改名で減らす。`CodeHealthRatchetTests.PhaseReferencesInCommentsDoNotGrow` が 258 で凍結している。

## 5. インターフェースの実装数（`iface`）

全 187。実装 2 以上 23／実装 1 で偽物あり 68／実装 1 で偽物なし 77／実装 0（継承のみ・工場の契約など）19。実装 1・偽物なしの置き場所: `Processing/Abstractions` 53／`Core/Abstractions` 9／`Processing/Actions` 8／`UI/Abstractions` 6／`Input/Abstractions` 1。規則 3.1（Phase36-B3）により、既存は撤去しない。

## 6. 参照されない型（`unref`）

型 1,041 件のうち、自分のファイル以外から参照されない型は 55 件（P/Invoke の構造体などを除く class・enum は 28 件）、テストからだけ参照される型は 5 件。
**誤検出に注意**: 拡張メソッドの静的クラス（`InputDeviceCartridgeServiceCollectionExtensions`）、XAML のマークアップ拡張（`SystemIconExtension`。XAML では `SystemIcon`）、入れ子の型（`Entry`、`Candidate`、`DeviceRuntime`、`EngineContext` など。自分のファイルの中で使うのが正しい）は、未参照ではない。名前の検索（`.cs`・`.xaml`・テスト）で、ほかに 1 件もないと確認できたもの: `PressActionController`、`TriggerType`、`MappingDoneEventArgs`。`ViGEmBusInfo` は、同じファイル（`EnvironmentService.cs`）の中だけで使う private の入れ子型で、未参照ではない（計測の誤検出。PhaseRV1-5 で確認。**private の入れ子型**は、自分のファイルの中で使うのが正しい）。そのほかは、規則 2.4 の手順で 1 件ずつ調べる（RV6。同じ誤検出が混じっているおそれがあるため、1 件ずつ確かめる）。

## 7. 重複（`clone`、`cloneexact`）

| 計測 | 意味のある行 | 重複の窓に入る行 | 割合 |
|---|---|---|---|
| 構造の重複（識別子・数値・文字列を正規化。14 行の窓） | 45,333 | 5,922 | **13.1%** |
| 同一の重複（正規化なし。10 行の窓） | 44,362 | 947 | **2.1%** |

構造の重複の上位（重複行／行、%）: `BindingWindow.xaml.cs` 467/675（69%）、`ProfilePresets.cs` 285/357（79%）、`AppSettingsDTO.cs` 244/554（44%）、`AppSettingsService.cs` 232/340（68%）、`ProfileEditor.HoverTables.cs` 211/266（79%）、`X360ControlTables.cs` 208/231（90%）、`DS4ControlNames.cs` 196/204（96%）、`PresetMenuHelper.cs` 160/306（52%）、`ProfileStickAxialLoadSection.cs` 144/158（91%）、`WelcomeDialog.xaml.cs` 142/242（58%）、`ControllerState.cs` 128/231（55%）、`ControllerReadingsControl.xaml.cs` 125/364（34%）、`ControllerStateFieldMapping.cs` 107/168（63%）、`HidHideAPIDevice.cs` 104/182（57%）。表・宣言の並び（`ProfilePresets`、`X360ControlTables`、`DS4ControlNames`、`ControllerState`）は、表のままでよい。

同一の重複の上位: `ProfilePresets.cs` 253/356（71%）、`FakerInputHandler.cs` 68/178（38%）、`AutoProfilesViewModel.cs` 61/261（23%）、`X360ControlTables.cs` 48/230（20%）、`LinkedProfilesXmlSerializer.cs` 48/78（61%）、`HidHideAPIDevice.cs` 46/181（25%）、`RecordBox.xaml.cs` 40/457（8%）、`ProfileMigration.cs` 33/258（12%）、`Crc32Algorithm.cs` 32/252（12%）、`SendInputHandler.cs` 30/220（13%）、`GyroOutputProcessor.cs` 26/280（9%）、`GyroMouseStickInfo.cs` 19/117（16%）、`GyroMouseInfo.cs` 19/83（22%）。

## 8. 公開の可変フィールド（`fields`）

合計 700。15 件以上のファイル: `BackingStore.cs` 107／`PadDataRspPacket.cs` 47（UDP の通信の構造）／`ControllerState.cs` 31（毎フレームの値）／`SpecialAction.cs` 30／`DS4_REPORT_EX.cs` 21（ドライバーの構造）／`TriggerDispatchEngine.cs` 20／`SendInputHandler.cs` 19（P/Invoke）／`TouchMouseStickInfo.cs` 17／`GyroMouseStickInfo.cs` 16。

## 9. 命名・採用手段（`naming`）

| 指標 | 値 |
|---|---|
| 小文字始まりの公開・保護・内部メソッド | 92（`InputDeviceCore.cs` 32、`ControllerStateExposed.cs` 9、`TouchpadMouse.cs` 9、`Mapping.cs` 5、`DS4SixAxis.cs` 4） |
| タイマーの生成（`new` の数） | `System.Timers.Timer`（別名 `NonFormTimer` を含む）7／`System.Threading.Timer` 4／`DispatcherTimer` 1／`TimeProvider.CreateTimer` 1。別名 `using NonFormTimer = System.Timers.Timer;` の行は 11（`NonFormTimer` は別種のタイマーではない）。**RV1-1 後（2026-11-19）: 5**（すべて使用中） |

## 10. テストの構成（`tests`）

| 指標 | 値 |
|---|---|
| テストのファイル | 613 |
| `[Fact]` | 2,495 |
| `[Theory]` | 246 |
| テストの行数 | 74,334（本体 103,549 行の 72%） |
| ソースの文字列を読むテスト | 128 ファイル |
| 名前に `Guard`・`Ratchet`・`Rule`・`Baseline` を持つテスト | 114 ファイル |
| ArchUnitNET を使うテスト | 2 ファイル |

## 11. 注入・静的な状態（`di`）

| 指標 | 値 |
|---|---|
| `Composition`・`App` 以外での Service Locator 呼び出し | 0 |
| `App.xaml.cs` | 928 行／`ResolvePostHostService` 23／`AppHost.CreateHost` 3／`new 型名` 27 |
| static の可変フィールド・自動プロパティ | 85（多くは表・キャッシュ。状態を持つ主なものは `AppLogger`・`PathService`・`EnvironmentService`） |
| UI 以外で `using System.Windows*;` を持つファイル | Core 50／Input 1／Output 3／Processing 4。**RV1-1 後（2026-11-19）: Core 3／Output 1／Processing 2／Input 0**（残りは実際に使う型。`Core` の 3 件は `CodeHealthBaseline` に凍結） |

## 12. そのほか（手で確認した数字）

| 指標 | 値 |
|---|---|
| `docs-forDIMG` の Markdown | 474 ファイル（`MadeByAgent` 450 ファイル・約 59,600 行。うち名前に `Plan` を含む 215） |
| `Core` で `using DS4Windows.(Processing|UI|Output|Composition|Input)…;` を持つファイル | 25（`Processing` 12 行、`Input` 12 行、`Input.Devices` 5 行など。実際の参照は ArchUnitNET が 0 件と確認）。**RV1-1 後（2026-11-19）: 1**（`ProfileGyroMouseLoadSection` が `Processing.Touchpad` の定数 `MouseCursor.GYRO_MOUSE_DEADZONE` を使うため。`CodeHealthBaseline` に凍結） |
| `Directory.Build.props` | `TreatWarningsAsErrors=false`、`WarningsAsErrors` は空 |
| `Nullable`・`AnalysisLevel`・`AnalysisMode` | `DS4WinWPF.csproj` に指定なし。**RV1 後（2026-11-19）**: プロジェクト全体の指定は引き続きなし。ファイル単位の `#nullable enable` を `Core/Abstractions`・`Composition` に付けた（試験の記録は `PhaseRV1-Nullable-Trial.md`）。残りの警告は 2,131 件／349 ファイル（RV1 完了後に再計測。フォルダー別は `PhaseRV9-Plan.md`）。`EnableNETAnalyzers=false` は RV1-2 で外した |
| `Nullable` を全体で有効にした場合の警告（2026-11-19 の実測。`-p:Nullable=enable` を付けた 1 回のビルド。ファイルは変更なし。テストのプロジェクトは除く） | 2,153 件／359 ファイル（ビルドの表示は重複を含む 2,454 件）。層別: Core 920、UI 684、Processing 300、Output 98、Input 84、ルート 38、Composition 24、Cli 5。種類: CS8600 640、CS8625 387、CS8602 349、CS8618 263、CS8622 182、CS8603 153、CS8604 113、CS8601 41 |
| `.editorconfig` | `dotnet_analyzer_diagnostic.category-Style.severity = none`、`IDE0090`・`IDE0028`・`IDE0300` も `none`。**RV1-2 後（2026-11-19）**: `category-Style` の `none` を外した（`IDE0090`・`IDE0028`・`IDE0300` は `none` のまま）。追加パッケージの規則は `Analyzers.globalconfig` |

## 13. PhaseRV ごとの目標値（完了の定義の基準）

各計画書の「完了の定義」と同じ値。承認のとき、各計画書の D 項目で調整する。

| 指標 | 現在 | RV1 | RV2 | RV3 | RV4 | RV5 | RV6 | RV7 | RV8 |
|---|---|---|---|---|---|---|---|---|---|
| 空の `catch`（本体が空で、理由のコメントもない） | 370（本体が空の 389 のうち、コメントなしのもの） | — | 0 | — | — | — | — | — | — |
| ログを包む `try/catch` | 41 | — | 0 | — | — | — | — | — | — |
| `async void`（イベントハンドラー以外） | 9 | — | 0 | — | — | — | — | — | — |
| XAML の `Command` バインディング | 0 | — | — | 増加 | **コマンド率 70% 以上** | — | — | — | — |
| XAML の `Click` 属性 | 193 | — | — | 減少 | 100 以下 | — | — | — | — |
| code-behind の `+= メソッド名;` | 227 | — | — | 減少 | 100 以下 | — | — | — | — |
| 委譲だけの窓口 | 3 | — | — | 0 | — | — | — | — | — |
| `BackingStore` の公開フィールド | 107 | — | — | — | — | 減少（目標は RV5 の D） | — | — | — |
| `AppSettingsService` の構造の重複 | 68% | — | — | — | — | 49%（目標 20% 以下は未達） | — | — | — |
| 真に未参照の型（規則 2.4 の承認済みのもの） | 4 以上 | 調査済み（3 件を記録。`ViGEmBusInfo` は誤検出） | — | — | — | — | 0 | — | — |
| 変更通知の実装の種類 | 2 | — | — | — | — | — | — | 1 | — |
| 別名 `using NonFormTimer = …` の行 | 11 | 0（未使用の別名を除く）→ **達成**（未使用 0。残る 5 行は使用中） | — | — | — | — | — | — | — |
| タイマーの生成の種類 | 4（13 か所） | — | — | — | — | — | — | 規則を決める（寄せるかは D） | — |
| 小文字始まりの公開メソッド | 92 | — | — | — | — | — | — | 0 | — |
| `Phase<数字>` を含む行 | 1,656 | — | — | — | — | — | — | — | 縮小（目標は RV8 の D） |
| `Core` の未使用の `using`（上位層・`System.Windows*`） | 75 ファイル | 0 → **達成**（未使用 0。残る 4 ファイルは実際に使う `using` で、基準 1＋3 に凍結） | — | — | — | — | — | — | — |

## 14. RV1 完了時点の再計測（2026-11-19）

`Measure-CodeReview.py`（全項目）と `Score-CodeReview.py` の結果。RV0 の値は 1〜12 章、保存は `scorecard/RV0-baseline.json`・`scorecard/RV1.json`。

| 指標 | RV0 | RV1 完了 |
|---|---|---|
| 本体の行数（ファイル数は 965 のまま） | 103,549 | 101,214（未使用の `using` の行を除いたため） |
| 500 行以上のファイル | 32 | 31 |
| `Core` で上位層の `using` を持つファイル | 25 | 1（基準に凍結） |
| UI 以外で `using System.Windows*;` を持つファイル | 58（Core 50／Input 1／Output 3／Processing 4） | 6（Core 3／Output 1／Processing 2） |
| 別名 `using NonFormTimer = …` の行 | 11 | 5（すべて使用中） |
| テスト（`[Fact]`／`[Theory]`／行数） | 2,495／246／74,334 | 2,504／246／74,623（xUnit 3,626 件＋StandaloneTests 11 件） |
| ソースの文字列を読むテスト／名前に `Guard`・`Ratchet`・`Rule`・`Baseline` を持つテスト | 128／114 | 131／118 |
| `docs-forDIMG` の Markdown（Archive の外） | 482 | 485 |
| 品質の門の達成数（警告をエラー化・アナライザー有効・スタイル診断を無効化していない） | 0/3 | 2/3 |
| `Nullable` を有効にした層（全ファイルが有効な層だけを数える） | 0 | 1（`Composition`。`Core` は `Abstractions` のみ） |
| 追加のアナライザー | なし | AsyncFixer・Threading・Sonar（規則は `Analyzers.globalconfig`。段階導入の 5 規則は `suggestion`） |
| ビルドの警告・エラー | 0 | 0（Debug・Release） |

変わっていない主な値: 空の `catch`（理由のコメントなし）370、ログを包む `try/catch` 41、`async void`（形が違うもの）9、`Click` 193、手結び 227、`BackingStore` の公開フィールド 107、`Phase<数字>` を含む行 1,656。RV2 以降が動かす。

## 15. RV3 完了時点の再計測（2026-11-20）

`Measure-CodeReview.py` の実測。保存は `scorecard/RV3.json`。

| 指標 | RV2 | RV3 完了 |
|---|---|---|
| XAML の `Command` バインディング | 0 | 58 |
| XAML の `Click` 属性 | 193 | 136 |
| XAML のその他のイベント属性 | — | 41 |
| コマンド率 | 0.000 | 0.247 |
| code-behind の `+= メソッド名;` | 227 | 222 |
| code-behind の部品への直接代入 | 290 | 248 |
| 500 行以上のファイル | 31 | 33 |
| 委譲だけの窓口 | 3 | 0（`MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment` を削除） |
| テスト（xUnit／StandaloneTests） | 3,626／11 | 3,671／11 |

## 16. RV4 完了時点の再計測（2026-11-20。一部完了）

`Score-CodeReview.py` の実測。保存は `scorecard/RV4.json`。

| 指標 | RV3 完了 | RV4 |
|---|---|---|
| コマンド率 | 0.247 | 0.790 |
| code-behind の `+= メソッド名;` | 222 | 75 |
| 500 行以上のファイル | 33 | 32 |
| `BindingWindow.xaml.cs` の構造の重複 | 69% | 11% |
| テスト（xUnit） | 3,671 | 3,682 |

## RV10 の実測（2026-11-20）
- `BackingStore` の public な可変フィールド: 107（計測の定義による）→ **24**（反射の実測。`BackingStorePublicFieldRatchetTests` が固定）。再算定は RV10 の保存（`scorecard/RV10.json`）。計測スクリプトの数え方では 35。
