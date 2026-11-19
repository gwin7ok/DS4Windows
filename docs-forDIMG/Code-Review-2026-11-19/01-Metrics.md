# コードレビュー（2026-11-19）: 計測値と、PhaseRV ごとの目標値

作成日: 2026-11-19
対象: ブランチ `master`（コミット `35f12b0a`）
計測: `python -I scripts/audit/Measure-CodeReview.py`（全項目）。項目を選ぶときは `--only size,mvvm,catch,comments,iface,unref,clone,cloneexact,fields,naming,tests,di`。
注意: 数字は正規表現による概算（コンパイラーの解析ではない）。Phase の完了の基準に使うときは、**同じスクリプトを同じ引数で**再実行して比べる。スクリプトを直したら、基準値も同じコミットで取り直す。

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
| 空の `catch` の層別 | Processing 213／Core 106／UI 36／ルート 14／Output 10／Composition 7／Input 3 |
| 上位のファイル | `KeyButtonActionController.cs` 59／`ProfileStickAxialLoadSection.cs` 29／`DefaultActionManager.cs` 21／`ProfileTouchpadMouseLoadSection.cs` 15／`App.xaml.cs` 14／`MacroPlayback.cs` 12／`RepeatHelper.cs` 11／`KeyButtonControllerRegistry.cs` 11 |
| ログ呼び出しを `try { } catch { }` で包む行 | 41 |
| `async void` | 14（`MainWindow` 5、`AutoProfilesViewModel` 4、`WelcomeDialog` 3、`UdpMotionService`・`Mapping`・`UpdaterWindow` 各 1） |
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

## 5. インターフェースの実装数（`iface`）

全 187。実装 2 以上 23／実装 1 で偽物あり 68／実装 1 で偽物なし 77／実装 0（継承のみ・工場の契約など）19。実装 1・偽物なしの置き場所: `Processing/Abstractions` 53／`Core/Abstractions` 9／`Processing/Actions` 8／`UI/Abstractions` 6／`Input/Abstractions` 1。規則 3.1（Phase36-B3）により、既存は撤去しない。

## 6. 参照されない型（`unref`）

型 1,041 件のうち、自分のファイル以外から参照されない型は 55 件（P/Invoke の構造体などを除く class・enum は 28 件）、テストからだけ参照される型は 5 件。
**誤検出に注意**: 拡張メソッドの静的クラス（`InputDeviceCartridgeServiceCollectionExtensions`）、XAML のマークアップ拡張（`SystemIconExtension`。XAML では `SystemIcon`）、入れ子の型（`Entry`、`Candidate`、`DeviceRuntime`、`EngineContext` など。自分のファイルの中で使うのが正しい）は、未参照ではない。名前の検索（`.cs`・`.xaml`・テスト）で、ほかに 1 件もないと確認できたもの: `PressActionController`、`TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo`。そのほかは、規則 2.4 の手順で 1 件ずつ調べる（RV6）。

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
| タイマーの生成（`new` の数） | `System.Timers.Timer`（別名 `NonFormTimer` を含む）7／`System.Threading.Timer` 4／`DispatcherTimer` 1／`TimeProvider.CreateTimer` 1。別名 `using NonFormTimer = System.Timers.Timer;` の行は 11（`NonFormTimer` は別種のタイマーではない） |

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
| UI 以外で `using System.Windows*;` を持つファイル | Core 50／Input 1／Output 3／Processing 4 |

## 12. そのほか（手で確認した数字）

| 指標 | 値 |
|---|---|
| `docs-forDIMG` の Markdown | 474 ファイル（`MadeByAgent` 450 ファイル・約 59,600 行。うち名前に `Plan` を含む 215） |
| `Core` で `using DS4Windows.(Processing|UI|Output|Composition|Input)…;` を持つファイル | 25（`Processing` 12 行、`Input` 12 行、`Input.Devices` 5 行など。実際の参照は ArchUnitNET が 0 件と確認） |
| `Directory.Build.props` | `TreatWarningsAsErrors=false`、`WarningsAsErrors` は空 |
| `Nullable`・`AnalysisLevel`・`AnalysisMode` | `DS4WinWPF.csproj` に指定なし |
| `Nullable` を全体で有効にした場合の警告（2026-11-19 の実測。`-p:Nullable=enable` を付けた 1 回のビルド。ファイルは変更なし。テストのプロジェクトは除く） | 2,153 件／359 ファイル（ビルドの表示は重複を含む 2,454 件）。層別: Core 920、UI 684、Processing 300、Output 98、Input 84、ルート 38、Composition 24、Cli 5。種類: CS8600 640、CS8625 387、CS8602 349、CS8618 263、CS8622 182、CS8603 153、CS8604 113、CS8601 41 |
| `.editorconfig` | `dotnet_analyzer_diagnostic.category-Style.severity = none`、`IDE0090`・`IDE0028`・`IDE0300` も `none` |

## 13. PhaseRV ごとの目標値（完了の定義の基準）

各計画書の「完了の定義」と同じ値。承認のとき、各計画書の D 項目で調整する。

| 指標 | 現在 | RV1 | RV2 | RV3 | RV4 | RV5 | RV6 | RV7 | RV8 |
|---|---|---|---|---|---|---|---|---|---|
| 空の `catch`（本体が空で、理由のコメントもない） | 389 | — | 0 | — | — | — | — | — | — |
| ログを包む `try/catch` | 41 | — | 0 | — | — | — | — | — | — |
| `async void`（イベントハンドラー以外） | 要確認 | — | 0 | — | — | — | — | — | — |
| XAML の `Command` バインディング | 0 | — | — | 増加 | **コマンド率 70% 以上** | — | — | — | — |
| XAML の `Click` 属性 | 193 | — | — | 減少 | 100 以下 | — | — | — | — |
| code-behind の `+= メソッド名;` | 227 | — | — | 減少 | 100 以下 | — | — | — | — |
| 委譲だけの窓口 | 3 | — | — | 0 | — | — | — | — | — |
| `BackingStore` の公開フィールド | 107 | — | — | — | — | 減少（目標は RV5 の D） | — | — | — |
| `AppSettingsService` の構造の重複 | 68% | — | — | — | — | 20% 以下 | — | — | — |
| 真に未参照の型（規則 2.4 の承認済みのもの） | 4 以上 | 調査 | — | — | — | — | 0 | — | — |
| 変更通知の実装の種類 | 2 | — | — | — | — | — | — | 1 | — |
| 別名 `using NonFormTimer = …` の行 | 11 | 0（未使用の別名を除く） | — | — | — | — | — | — | — |
| タイマーの生成の種類 | 4（13 か所） | — | — | — | — | — | — | 規則を決める（寄せるかは D） | — |
| 小文字始まりの公開メソッド | 92 | — | — | — | — | — | — | 0 | — |
| `Phase<数字>` を含む行 | 1,656 | — | — | — | — | — | — | — | 縮小（目標は RV8 の D） |
| `Core` の未使用の `using`（上位層・`System.Windows*`） | 75 ファイル | 0 | — | — | — | — | — | — | — |
