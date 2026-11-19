# コードレビュー（2026-11-19）: リポジトリ全体の評価と、PhaseRV1〜RV8 の計画

作成日: 2026-11-19
対象: ブランチ `master`（コミット `35f12b0a`。PhaseX3-7 まで統合済み）。`DS4Windows`（本体）、`DS4WindowsTests`、`StandaloneTests`、`DS4WindowsCli`、`.github`、`scripts`。
方法: コードは変更せず、計測スクリプト（`scripts/audit/Measure-CodeReview.py`。何度でも同じ数字が出る）と、読んで確かめた事実だけを記録した。確かめきれなかったものは「要確認」と書いた。数字の一覧は `01-Metrics.md`。
依頼: 「リポジトリ全体のコードレビューを行って、PureDI 化、各層ごとの役割分担、MVVM 原則遵守の割合、オブジェクト指向、共有部品化、冗長処理、採用手段の統一、メンテナンス性の向上、機能追加の容易化、リファクタリングの余地などを念頭に、Phase? として計画化されている文書（実施、未実施の両方）も考慮した上で行い、報告書の形で文書化し、複数の PhaseRV? の形で実施計画を作る」
関連: `docs-forDIMG/Code-Review-2026-11/00-Summary.md`（前回のレビュー。Phase29〜36 で是正済み）、`docs-forDIMG/DI-App-Wide-Migration-Plan.md`（§6.14〜6.18、§9.3）、`Known-Issues-Ledger.md`、`Phase34`〜`Phase37`・`PhaseX3` の各計画書、実施計画 `docs-forDIMG/MadeByAgent/PhaseRV1-Plan.md`〜`PhaseRV8-Plan.md`

## 1. 要約

### 1.1 総評

前回のレビュー（2026-11）で挙げた弱点は、DI・層・引数の数・重複の面ではほぼ是正済みで、**構造の骨格は健全**である。Service Locator は `Composition` と `App` の外に 0 件、層の依存の違反は 0 件（`LayerDependencyRuleTests`）、View と ViewModel の型の規則（V1・V2）の違反も 0 件、12 を超える引数の型は 2 件、と数字が揃っている。テストは 613 ファイル・約 7.4 万行（本体の 72%）で、「直しても壊れない」土台は十分にある。

今回の新しい弱点は、構造の外側、つまり **「型の形は整ったが、中身の流れがまだ旧来のまま」** という部分にある。大きいものは 5 つ。

1. **MVVM が、型の規則だけを満たしている。** コマンドのバインディングが 0 件（`RelayCommand` を使うファイルも 0）で、`Click` 属性が 193、その他のイベント属性が 46、code-behind が部品へ直接代入する行が約 290、`+= メソッド名;` の手結びが 227 ある。V2 は「業務の型を直接参照しない」だけを見ているため、中身が委譲だけの窓口（`MainWindowServiceControl` など）を挟んで満たしている。
2. **例外の握りつぶしが多い。** `catch` 811 件のうち 389 件（48%）が本体の空の `catch`。`Processing` に 213、`Core` に 106。ログ呼び出しを `try { } catch { }` で包む行も 41 ある。落ちないことは守られるが、原因が見えない。
3. **設定の項目を 1 つ足す手数が多く、形が重複している。** 読み込みと保存が別のファイル・別の節にあり（例: `TouchpadClickPassthru` は読み込みが `ProfileTouchpadBasicLoadSection`、保存が `ProfileTriggerStickSaveSection`）、`AppSettingsService`（68%）・`ProfileStickAxialLoadSection`（91%）・`AppSettingsDTO`（44%）の構造が重複している。入力機種の設定は Phase17 で宣言型（`ControllerSetting`）になったが、アプリ・プロファイルの設定は旧来のまま。
4. **採用手段が揃っていない。** 変更通知（直接実装 13／`ObservableObject` 12）、タイマー（生成 13 か所が 4 種類。`System.Timers.Timer` 7［別名 `NonFormTimer`］／`System.Threading.Timer` 4／`DispatcherTimer` 1／`TimeProvider.CreateTimer` 1。数が少なく、効果は小さい）、小文字始まりの公開メソッド 92、テストの枠（xUnit と、`.sln` の外の MSTest）。
5. **経緯のコメント・文書・ガードテストが積もっている。** 全 965 ファイルのうち 487 ファイル（50%）に `Phase<数字>` を含む行があり（1,656 行）、`//` コメントの「旧」が 759 行。テストの XML コメントに Phase 名を持つファイルが 415。ソースの文字列を読むテストが 128 ファイル、`Guard`／`Ratchet`／`Rule`／`Baseline` を名前に持つテストが 114 ファイル。`docs-forDIMG/MadeByAgent` は 450 ファイル・約 6 万行。

あわせて、**品質の門が緩い**ことも、改善を続ける上で効いている（`Nullable` なし、`TreatWarningsAsErrors=false`、スタイル系の診断は `.editorconfig` で `none`、Core の 50 ファイルに `using System.Windows*;` の雛形の残り）。

### 1.2 観点別の評価

評価は A〜D の目安で、主観を含む。根拠は §3、数字は `01-Metrics.md`。

| 観点 | 評価 | 一言 | 主な根拠 | 計画 |
|---|---|---|---|---|
| Pure DI 化 | A− | 骨格は完成。残りは起動・終了の手順と、過剰な抽象 | Service Locator 0 件（`Composition`・`App` の外）、登録 187 件。`App.xaml.cs` 928 行（`ResolvePostHostService` 23、`new 型名` 27）。インターフェース 187 のうち、実装 1・偽物なしが 77（41%） | RV1（一部）、RV6 |
| 各層の役割分担 | B+ | 実際の参照は 0 件。`using` 行と置き場所に残りがある | `LayerDependencyRuleTests` の違反 0 件。Core に、未使用と見られる上位層の `using` が 25 ファイル、`using System.Windows*;` が 50 ファイル | RV1 |
| MVVM 原則遵守の割合 | C+ | 型の規則 100%、操作の流れ 0% | §5 の算定。コマンド 0／239、`{Binding}` 771 に対し `x:Name` 686、code-behind 10,779 行（画面全体の 42.5%） | RV3、RV4 |
| オブジェクト指向 | B− | 骨組み（`InputLoopDevice`、カートリッジ）は良い。状態の袋と、大きな基底が残る | `BackingStore` の公開フィールド 107、`InputDeviceCore` 1,133 行（小文字始まりの公開メソッド 32）、公開の可変フィールド合計 700 | RV5、RV7 |
| 共有部品化 | B | 入力機種・出力の種類・設定画面の部品化は進んだ。画面と取得の流れが未着手 | `WelcomeDialog` の取得・導入の同型の流れ 3 本、`BindingWindow` の手結び 149、`GyroMouseInfo`／`GyroMouseStickInfo` の二重 | RV4、RV6 |
| 冗長処理 | C | コードの複写（2.1%）は少ない。冗長なのは、防御・経緯・検査 | 空の `catch` 389、ログを包む `try/catch` 41、`?? throw new ArgumentNullException` 666、経緯のコメント 1,656 行、ガード系テスト 114 ファイル | RV2、RV8 |
| 採用手段の統一 | C+ | 方針はあるが、徹底されていない | 変更通知 13／12、タイマー 4 種、小文字始まり 92、`async void` 14、`.Result`・`.Wait()` 21、`Thread.Sleep` 13、テストの枠 2 つ | RV7 |
| メンテナンス性 | B− | テストの安全網は厚い。読む量と、門の緩さが重い | テスト 613 ファイル、500 行以上のファイル 32、文書 474 ファイル、`Nullable` なし | RV1、RV8 |
| 機能追加の容易化 | B | 機種・出力の種類は道がある。設定の項目・画面の項目は手数が多い | 機種は 1 行登録（`AddInputDevice`）。プロファイルの設定の項目は 4〜6 ファイル、読み込みと保存が別の場所 | RV5、RV4 |
| リファクタリングの余地 | — | §3.10 に一覧。効果が大きい順に RV2→RV3→RV5 | 同上 | RV1〜RV8 |

### 1.3 主な発見（効果の大きい順）

| # | 内容 | 効果 | リスク | 計画 |
|---|---|---|---|---|
| 1 | 空の `catch` 389 件（48%）と、ログ呼び出しを包む `try/catch` 41 行。原因の見えない失敗の温床 | 高 | 中（ログ出力の規則 2.3 との調整が要る） | RV2 |
| 2 | MVVM の操作の流れ（コマンド 0、手結び 227、直接代入約 290）と、委譲だけの窓口 | 高 | 中〜高（画面の動きが変わり得る） | RV3、RV4 |
| 3 | 設定の項目の宣言化（アプリ・プロファイル）。読み込みと保存を 1 か所へ、`AppSettingsService`・DTO・節の重複を畳む | 高 | 高（保存の形に触れる。黄金テストが守る） | RV5 |
| 4 | 品質の門（アナライザー、`Nullable` の段階導入、未使用の `using`、計測の継続） | 中 | 低 | RV1 |
| 5 | 共有部品化と、冗長な実装の整理（`Gyro*Info`、`KeyButtonActionController` まわり、同一の重複、真に未参照の型。取得・導入の流れ 3 本は RV4） | 中 | 低〜中 | RV6（取得・導入は RV4） |
| 6 | 採用手段の統一（変更通知、タイマー、命名、`async void` など） | 中 | 低〜中（命名の一括変更は広い） | RV7 |
| 7 | 経緯のコメント・文書・テストの整理 | 中（読む量が減る） | 低（ただし、他の Phase と衝突しやすいので最後） | RV8 |

### 1.4 PhaseRV の一覧

| Phase | 主題 | 主な指摘 | 規模 | リスク |
|---|---|---|---|---|
| PhaseRV1 | 品質の門と土台の整理（アナライザー、未使用の `using`、計測の常設） | RV-01〜RV-04 | 中 | 低 |
| PhaseRV2 | 例外の握りつぶしと、非同期・待機の作法 | RV-05〜RV-08 | 大 | 中 |
| PhaseRV3 | MainWindow の MVVM 化（コマンド化と、委譲だけの窓口の解消） | RV-09〜RV-12 | 大 | 中〜高 |
| PhaseRV4 | ProfileEditor・BindingWindow・WelcomeDialog・RecordBox の MVVM 化と表駆動化 | RV-13〜RV-16 | 大 | 中〜高 |
| PhaseRV5 | 設定の宣言化（アプリ・プロファイルの項目を 1 か所の定義へ） | RV-17〜RV-20 | 大 | 高 |
| PhaseRV6 | 共有部品化と、冗長な実装の整理 | RV-21〜RV-25 | 中 | 低〜中 |
| PhaseRV7 | 採用手段の統一（変更通知、タイマー、命名、テストの枠） | RV-26〜RV-28 | 中 | 低〜中 |
| PhaseRV8 | 経緯のコメント・文書・ガードテストの整理 | RV-29〜RV-31 | 中 | 低 |

各 Phase の計画書は、同じ書式で `PhaseRV1-Plan.md`〜`PhaseRV8-Plan.md` に置いた（状態はすべて「未承認」）。進める順は §7。

## 2. 前回のレビュー（2026-11）の是正の確認

前回の指摘（`Code-Review-2026-11/00-Summary.md` §4）は、`DI-App-Wide-Migration-Plan.md` §9.3 のとおり、実施するものはすべて完了している。今回、次の数字で再確認した。

| 前回の指摘の主題 | 今回の確認 |
|---|---|
| Service Locator・静的な窓口（1-x） | `Composition`・`App` の外での呼び出し 0 件。静的な可変の状態 85 件のうち、状態を持つ主なものは `AppLogger`・`PathService`・`EnvironmentService`（`Phase7-Residual-Ledger.md` の目録どおり） |
| 層の依存（2-x） | `LayerDependencyRuleTests` の基準 0 件。型の参照の検査は ArchUnitNET が行う |
| コンストラクターの引数（3-x） | 12 を超える型は `ConstructorArityBaseline` の 2 件 |
| 重複・過剰な抽象（4-x） | 同一の重複は 2.1%（§3.6）。実装 1・偽物なしのインターフェースは 77 件あるが、規則 3.1（Phase36-B3）で「既存は撤去しない」と決めてある |
| 機種・出力の種類の追加（5-x） | `AddInputDevice<TCartridge, TModule>()` の 1 行、`IOutputDeviceCartridge`。`02-Extension-Guide.md` のとおり |
| 毎フレームの割り当て・安全弁（6-x） | Phase29 で是正済み |
| 外部レビュー（`07-External-Review-2026-11-12.md`）の MVVM の形骸化 | Phase34 で V1・V2 の基準値は 0 になった。ただし、**操作の流れ（コマンド・手結び）は対象外のまま**だった。今回の RV-09 以降がその続き |

## 3. 観点別の評価

### 3.1 Pure DI 化（A−）

- **良い点**: ホストは `App` の起動だけが作る。登録は層ごとの `partial`（`ServiceRegistration.Core／Processing／Output／Ui／Wiring`）に集約され、後付けの配線は `Wiring` だけ。`Composition`・`App` の外での Service Locator の呼び出しは 0 件。
- **残り**:
  - `App.xaml.cs` が 928 行で、`ResolvePostHostService` が 23 件、`new 型名` が 27 件、`AppHost.CreateHost` が 3 件（通常の起動、`-driverinstall` の分岐など）。Phase35 で整理したあとの残りで、起動前に必要なものは過渡期の理由がある。**行数を目的に削らない**（前回の決定と同じ）。
  - インターフェース 187 のうち、実装 1・偽物なしが 77（`Processing/Abstractions` に 53）。規則 3.1 の基準（同じ層の中だけで使う、状態を持たない部品は具象のまま）に当たるものが多い見込みだが、**既存の撤去は規則で見送り済み**。新規に増やさない運用だけ守る。
  - 起動前に必要な静的な状態（`AppLogger`、`PathService`、`EnvironmentService`）は、台帳のとおり維持。
- **判断**: 構造としての変更は不要。RV6 で、中身のない `PressActionController` など、規則 2.4 の手順で整理できるものだけ扱う。

### 3.2 各層ごとの役割分担（B+）

- **良い点**: 層ごとの規模は Input 10,135 行／Processing 24,478／Output 7,185／UI 30,147／Composition 1,302／Core 29,350（`01-Metrics.md`）。依存の向きは機械で守られている（ArchUnitNET の L1〜L10、基準 0 件）。`Output` から第 1 層の具象（`InputDeviceCore`）への参照は、型としては 0 件で、コメントに名前が出るだけ（`Output/Services/UdpMotionService.cs:216`）。
- **残り**:
  - **`using` 行の残り**: `Core` の 25 ファイルに、`DS4Windows.Processing`・`DS4Windows.Input`・`DS4Windows.Output` などの `using` がある。たとえば `Core/Abstractions/IStartupArguments.cs` の `using DS4Windows.Processing;` は、本文が使う `ArgumentParser`（`Core.Utilities`）には要らない。ArchUnitNET は実際の型の参照を見るので違反は 0 件で、これらは未使用の `using` と見られる（要確認。RV1 で IDE0005 によって確かめる）。同様に、`Core` の 50 ファイルに `using System.Windows*;` があり、雛形の残りが多い。
  - 本体の `UI` が 30,147 行と、層で最大。うち ViewModels が 14,558 行、code-behind が 10,779 行（§5）。
- **判断**: 規則（L1〜L10）の追加は不要。RV1 で `using` の整理を行い、RV3・RV4 で UI の中身の置き場所を揃える。

### 3.3 MVVM 原則遵守の割合（C+）

算定は §5。要点だけ書く。

- 型の規則（V1・V2）は、基準 0 件で **100%**。
- 操作の流れは **0%**。XAML の `Command` バインディングが 0、`RelayCommand` を使うファイル（定義を除く）が 0。`Click` 属性 193、その他のイベント属性 46（`SelectionChanged`・`TextChanged` など）。
- code-behind の `.xaml.cs` は 39 ファイル・10,779 行、イベントハンドラー型のメソッドが 278。部品への直接代入は `IsEnabled` 61、`Visibility` 83、`Content` 106、`ItemsSource` 15、`SelectedIndex` 25。`+= メソッド名;` の手結びが 227（`BindingWindow` が 149 を占める。`x.Click += OutputButtonBtn_Click;` の並び）。
- 窓口の型が、V2 を満たすための形になっている。`UI/Services/MainWindowServiceControl.cs`・`MainWindowSettings.cs` は、本体が相手への委譲だけ。ViewModel に載せるべき判断（開始・停止の中の分岐、`Task.Run` と `IsEnabled = false` の組み合わせ）が、`MainWindow.xaml.cs` の `StartStopBtn_Click`・`HideDS4ContCk_Click` などに残っている。
- **判断**: 規則 V1・V2 に、**V3（操作は `ICommand` でバインドする）**を足し、基準値を凍結して減らす方式（Phase34 と同じ）で進める（RV3・RV4）。

### 3.4 オブジェクト指向（B−）

- **良い点**: 読み取りのループの骨組み（`InputLoopDevice` と差し込み口）、機種のカートリッジ、`IOutputDeviceCartridge`、`ControllerSetting` の宣言型は、継承より構成・差し込みで足す設計になっていて、追加の道が短い。
- **残り**:
  - `BackingStore`（1,027 行）が、公開フィールド 107 を持つ状態の袋。設定の領域の窓口（`ForSlot`、Phase15-6Z）で隠したが、実体は袋のまま（RV5 の対象）。
  - `InputDeviceCore`（1,133 行）は、小文字始まりの公開メソッドが 32 件（`ControllerStateExposed` 9、`TouchpadMouse` 9、`Mapping` 5、`DS4SixAxis` 4 など、全体で 92 件）。命名の不一致と、基底に責務が残っていることの両方を示す（RV7 の命名、`Phase40` の契約の続き）。
  - 公開の可変フィールドは合計 700 件。多くは、UDP の通信の構造（`PadDataRspPacket` 47）、ドライバーの構造（`DS4_REPORT_EX` 21）、毎フレームの経路の値の入れ物（`ControllerState` 31、`TriggerDispatchEngine` 20）で、**意図がある**。`SpecialAction`（30）・`TouchMouseStickInfo`（17）・`GyroMouseStickInfo`（16）のような、設定の値の入れ物は、RV5・RV6 で扱う。
- **判断**: 大きな型の分割は、すでに決定済みの対象（`Mapping`・`TriggerDispatchEngine` は分けない。`Code-Review-2026-11/07` §2）を再び持ち出さない。

### 3.5 共有部品化（B）

- **良い点**: 機種の差し込み（`IHidTransport`・`IInputReportParser`・`IDeviceProtocol`）、出力の種類（`OutContTypeInfo`）、設定の領域の窓口、`ProfileAreaSection`（読み込みと保存の節の対）、スペシャルアクションの種類の一覧は、共有の部品になっている。
- **残り**:
  - `WelcomeDialog.xaml.cs`（242 行の意味のある行のうち 58% が構造の重複）に、ViGEm・HidHide・FakerInput の「取得して、導入して、タイマーで待つ」流れが 3 本、同じ形で並ぶ。`ExternalComponentInstaller` のような部品 1 つで足りる形（RV4。画面と部品を一緒に整理する）。
  - `BindingWindow.xaml.cs`（675 行の意味のある行のうち 69%）の `Click +=` 149 件と、`ProfileEditor.HoverTables.cs`（79%）の `hoverIndexes[crossConBtn] = 0;` の表。これらは、表と `ItemsControl`／`DataTemplate`／添付ビヘイビアで共有化できる（RV4）。
  - `Core/Settings/GyroMouseInfo.cs`・`GyroMouseStickInfo.cs` の、平滑化のフィールドと入れ子の列挙型の二重（RV6）。
  - 画面側の確認・ファイル選択・タイマーを、機種・出力の種類のような 1 本の道（`IDialogService`、`IUiDispatcher`）で通す方針は整っているが、**変更通知の基底**が 2 通りある（§3.7）。

### 3.6 冗長処理（C）

- **コードの複写は少ない**: 10 行以上の同一の窓に入る意味のある行は 2.1%（947／44,362）。上位は表（`ProfilePresets` 71%）、`FakerInputHandler` 38%、`AutoProfilesViewModel` 23%、`LinkedProfilesXmlSerializer` 61%、`HidHideAPIDevice` 25%、`Crc32Algorithm` 12%。識別子・数値・文字列を正規化して数えると 13.1%（5,922／45,333）に増えるが、大半は表・宣言の並び（`DS4ControlNames`、`X360ControlTables`、`ProfilePresets`、`ProfileEditor.HoverTables`）で、直す対象は限られる。
- **冗長なのは「防御・経緯・検査」**:
  - 空の `catch` が 389 件（48%）。層別は `Processing` 213、`Core` 106、`UI` 36、ルート 14、`Output` 10、`Composition` 7、`Input` 3。上位ファイルは `KeyButtonActionController` 59、`ProfileStickAxialLoadSection` 29、`DefaultActionManager` 21、`ProfileTouchpadMouseLoadSection` 15、`App.xaml.cs` 14、`MacroPlayback` 12、`RepeatHelper` 11、`KeyButtonControllerRegistry` 11。
  - ログ呼び出しを `try { } catch { }` で包む行が 41。`AppLogger` が投げない保証があれば不要になる。
  - `?? throw new ArgumentNullException(nameof(...))` が 666 件（害はない。前回の 3-2 と同じ判断。RV7 の D で再確認）。
  - 経緯のコメント（§3.8）、ガード系のテスト 114 ファイル、ソースの文字列を読むテスト 128 ファイル。
  - 構造の冗長: 中身が委譲だけの窓口（`MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment`）、`PressActionController`（空の `[Obsolete]` の置き物）、漏れのある `RepeatHelperToIRepeaterAdapter`、`KeyButtonActionControllerAdapter`。
- **未参照の型**: 計測では 1,041 型のうち、自分のファイル以外から参照されない型が 55 件（P/Invoke の構造体などを除くと class・enum が 28 件）。ただし、**拡張メソッドの静的クラス**（`InputDeviceCartridgeServiceCollectionExtensions`）と、**XAML のマークアップ拡張**（`SystemIconExtension`）は、名前でなく構文で使われるため、誤検出（確認済み）。入れ子の型（`Entry`、`Candidate` など）も、自分のファイルの中だけで使うのが正しい。名前を検索して、ほかのどこにもない（テストにもない）と確認できたのは、`PressActionController`、`TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo`（`EnvironmentService.cs` 内の宣言のみ）。これらは規則 2.4 の手順（経緯の調査 → 使うべきかの判断 → 承認）に従って扱う（RV6）。

### 3.7 採用手段の統一（C+）

| 項目 | 現状 | 目標 |
|---|---|---|
| 変更通知 | `INotifyPropertyChanged` を直接実装 13／`ObservableObject` 継承 12 | 1 つの基底へ（RV7。保存や UI の動きを変えない） |
| タイマー | 生成 13 か所が 4 種類: `System.Timers.Timer` 7（UI の 5 ファイル。`NonFormTimer` は、その別名を 11 ファイルの `using` で宣言したもの。未使用の別名が多い見込み）／`System.Threading.Timer` 4（`App`、`BatteryStatusLogger`、`CompositeDeviceModel`）／`DispatcherTimer` 1（`ProfileEditor`）／`TimeProvider.CreateTimer` 1（`RepeatHelper`） | 別名の整理（RV1 の `using` の整理）と、新規の書き方の規則だけ。生成 13 か所を 1 種へ寄せる必要はない（RV7 の D） |
| 命名 | 小文字始まりの公開・保護・内部メソッド 92 件 | PascalCase。呼び出し元ごと機械的に直す（RV7） |
| 非同期・待機 | `async void` 14、`.Result`・`.Wait()` 21、`Thread.Sleep` 13 | イベントハンドラー以外の `async void` をなくす。ブロッキングの待ちは理由つきで残す（RV2） |
| 空の `catch` | 389 件 | 規則を決めて分類（RV2） |
| テストの枠 | xUnit（`DS4WindowsTests`）と MSTest（`StandaloneTests`。`.sln` の外） | 方針を決める（RV7 の D） |
| 検査の書き方 | ソースの文字列を読むテスト 128 ファイル／ArchUnitNET を使うテスト 2 ファイル | 型の検査へ寄せる（RV8。規則 §4「型の検査で書く」の徹底） |

### 3.8 メンテナンス性の向上（B−）

- **良い点**: テスト 613 ファイル・`[Fact]` 2,495・`[Theory]` 246・約 7.4 万行（本体の 72%）。黄金テスト（保存の形）、ラチェット（基準値の凍結）、Debug ALL（警告 0・エラー 0）の手順が固まっている。
- **重い点**:
  - **経緯のコメント**: `Phase<数字>` を含む行が 1,656、含むファイルが 487（全 965 の 50%）。`//` の「旧」を含む行が 759。規則 §3.3 原則5 は、新しく書くコメントについて「Phase 名・Step 名と旧実装の説明をコードに書き足さない」と決めたが、**既存分は、触る Step の計画書で範囲を決めたときだけ縮めてよい**としている（ついでの一括削除はしない）。RV8 がその範囲を決める計画書になる。
  - **文書**: `docs-forDIMG` に 474 ファイル（`MadeByAgent` に 450 ファイル・約 6 万行。計画書 215）。完了した Phase の資料が、現行の規約・図と同じ場所に並ぶ。
  - **大きなファイル**: 500 行以上が 32 ファイル。上位は `MainWindow.xaml.cs` 1,763、`ProfileEditor.xaml.cs` 1,634、`AppSettingsDTO.cs` 1,179（保存の形の制約で分けない。Phase37 の決定）、`InputDeviceCore.cs` 1,133、`DualSenseDevice.cs` 1,089、`BackingStore.cs` 1,027、`SwitchProDevice.cs` 999、`BindingWindow.xaml.cs` 938、`App.xaml.cs` 928、`Mapping.cs` 895、`RecordBox.xaml.cs` 873。
  - **品質の門**: `Directory.Build.props` は `TreatWarningsAsErrors=false`、`Nullable` の指定なし、`.editorconfig` でスタイル系の診断はカテゴリごと `none`。`DS4WinWPF.csproj` の `NoWarn` に CS0103・CS0122・CS1061・CS0246・CS0234 があるのは、WPF の 1 回目のビルドの一時プロジェクトの誤りを避けるための既存の措置で（`scripts/Build-Debug.ps1` の再試行と対）、今回は触らない。

### 3.9 機能追加の容易化（B）

| 追加するもの | 手数 | 評価 |
|---|---|---|
| 入力機種 | `[InputDeviceFamily]` と `AddInputDevice<TCartridge, TModule>()` の 1 行。単純な機種は `IDeviceProtocol` ＋ `ProtocolDevice` | 良い（`02-Extension-Guide.md`） |
| 出力の種類 | `IOutputDeviceCartridge` と `OutContTypeInfo` の表 | 良い |
| 機種ごとの設定 | `ControllerSetting` の宣言 1 つ。保存・読み込み・設定画面・はじめの起動画面が、その定義から出る | 良い |
| **プロファイルの設定の項目** | 例 `RumbleBoost` は 6 ファイル（`ProfileGeneralLoadSection`、`ProfileGeneralSaveSection`、`OutputSlotSettings`、`OutputSettingsPanel.xaml`、`OutputSettingsViewModel`、`RumbleTestController`）。`TouchpadClickPassthru` は読み込みと保存が別々の節（`ProfileTouchpadBasicLoadSection` と `ProfileTriggerStickSaveSection`）。要素名・既定値・範囲が、読み込み・保存・画面の 3 か所に散る | 弱い（RV5） |
| **アプリの設定の項目** | `BackingStore` のフィールド、`AppSettingsService` の「取得・設定・通知」の定型、`AppSettingsDTO` の要素、読み込み、保存、設定画面 | 弱い（RV5） |
| **画面の項目（ボタン・リスト）** | XAML の要素、`x:Name`、`Click` のハンドラー、`+=`、code-behind の更新 | 弱い（RV3・RV4） |

### 3.10 リファクタリングの余地（効果の大きい順）

1. 例外の握りつぶしの分類と是正（RV2）。効果は大きく、機能は変えない（ログ文言の追加は承認制）。
2. 設定の宣言化（RV5）。追加の手数と、重複した定型が一気に減る。リスクが最も高い（保存の形）。
3. MainWindow・ProfileEditor・BindingWindow のコマンド化（RV3・RV4）。
4. 共有部品化（取得・導入の流れ、`Gyro*Info`）と、規則 2.4 に従う未参照の整理（RV6）。
5. 品質の門の段階導入（RV1）と、採用手段の統一（RV7）。
6. 経緯のコメントと文書の整理（RV8）。

## 4. 指摘の一覧

重大度は、高（放置すると不具合の見逃しや事故につながる）／中（設計の質・将来の手数に効く）／低（整理）。

| ID | 観点 | 重大度 | 内容 | 根拠 | 計画 |
|---|---|---|---|---|---|
| RV-01 | メンテナンス性 | 中 | アナライザー・`Nullable` が未導入。`TreatWarningsAsErrors=false`、スタイル診断は `none` | `Directory.Build.props`、`.editorconfig` | RV1 |
| RV-02 | 層の役割 | 低 | `Core` に、未使用と見られる上位層の `using` が 25 ファイル。`using System.Windows*;` が 50 ファイル | `01-Metrics.md` di | RV1 |
| RV-03 | メンテナンス性 | 低 | 計測スクリプトが手動。数字の悪化を検知する仕組みがない | `scripts/audit/Measure-CodeReview.py` | RV1 |
| RV-04 | 冗長 | 低 | 真に未参照の型の候補（`TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo`）の扱いが未決（`PressActionController` は RV6） | §3.6 | RV1（調査）／RV6（実施） |
| RV-05 | 冗長・採用手段 | 高 | 空の `catch` 389 件（48%）。意図の有無が区別できない | §3.6 | RV2 |
| RV-06 | 冗長 | 中 | ログ呼び出しを `try { } catch { }` で包む行 41 | §3.6 | RV2 |
| RV-07 | 採用手段 | 中 | `async void` 14、`.Result`・`.Wait()` 21、`Thread.Sleep` 13 | §3.7 | RV2 |
| RV-08 | 採用手段 | 低 | 例外の扱いの作法（捨てる・ログ・再投げ）の規則が、文書にない | `Log-Policy.md` は、ログのレベルが主 | RV2 |
| RV-09 | MVVM | 高 | `Command` バインディング 0、`RelayCommand` 未使用 | §5 | RV3 |
| RV-10 | MVVM | 中 | `MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment` が委譲だけ | `UI/Services` | RV3 |
| RV-11 | MVVM | 中 | `MainWindow.xaml.cs` 1,763 行。`Task.Run` ＋ `IsEnabled = false` の定型が複数 | `StartStopBtn_Click`、`HideDS4ContCk_Click` | RV3 |
| RV-12 | MVVM | 低 | `MainWindow` のコンストラクターの引数が 12（上限ちょうど） | `UI/MainWindow.xaml.cs` | RV3 |
| RV-13 | MVVM | 中 | `BindingWindow` の `Click +=` が 149、938 行 | `UI/BindingWindow.xaml.cs` | RV4 |
| RV-14 | 共有部品 | 中 | `ProfileEditor.HoverTables.cs` の表（79% が重複）、`ProfileEditor.xaml.cs` 1,634 行 | §3.5 | RV4 |
| RV-15 | 共有部品 | 中 | `WelcomeDialog` の取得・導入の流れが 3 本、code-behind にある | §3.5 | RV4（画面と部品を一緒に） |
| RV-16 | MVVM | 低 | `RecordBox.xaml.cs` 873 行、部品への直接代入（合計約 290）、`ControllerReadingsControl`（重複 34%） | §5 | RV4 |
| RV-17 | 機能追加 | 高 | プロファイル・アプリの設定の項目が、読み込み・保存・画面に散る | §3.9 | RV5 |
| RV-18 | 冗長 | 中 | `AppSettingsService`（68%）の定型、`AppSettingsDTO`（44%）の重複 | §3.6 | RV5 |
| RV-19 | 冗長・防御 | 中 | `ProfileStickAxialLoadSection`（91%）と、要素ごとの `try { } catch { }`（29 件）。`ProfileTouchpadMouseLoadSection` にも 15 件 | §3.6 | RV5（RV2 と連携） |
| RV-20 | OOP | 中 | `BackingStore` の公開フィールド 107 | §3.4 | RV5 |
| RV-21 | 共有部品 | 中 | `GyroMouseInfo`・`GyroMouseStickInfo` の平滑化の二重、入れ子の列挙型の二重 | §3.5 | RV6 |
| RV-22 | 冗長 | 中 | `KeyButtonActionController` まわり（空 `catch` 59、`PressActionController` の置き物、`RepeatHelperToIRepeaterAdapter` の漏れ、`KeyButtonActionControllerAdapter`） | §3.6 | RV6 |
| RV-23 | 共有部品 | 低 | `FakerInputHandler`（38%）、`HidHideAPIDevice`（25%）、`SendInputHandler`（13%）、`AutoProfilesViewModel`（23%）、`LinkedProfilesXmlSerializer`（61%）の同一の重複 | §3.6 | RV6 |
| RV-24 | 冗長 | 低 | 真に未参照の型の整理（規則 2.4。承認制） | §3.6 | RV6 |
| RV-25 | 共有部品 | 低 | `Crc32Algorithm`（12%）、`DualSenseDevice`・`SwitchProDevice` の内部クラスの未参照（`InputReportDataBytes*`、`HapticPowerLevelFriendlyName`、`SwitchProSubCmd` など。規則 2.4） | `01-Metrics.md` unref | RV6 |
| RV-26 | 採用手段 | 中 | 変更通知の実装 13／12（独自の `ObservableObject` が 12、直接実装が 13）。タイマーは生成 13 か所が 4 種で、効果は小さい（低） | §3.7 | RV7 |
| RV-27 | 採用手段 | 中 | 小文字始まりの公開メソッド 92（`InputDeviceCore` 32） | §3.7 | RV7 |
| RV-28 | 採用手段 | 低 | テストの枠が 2 つ（xUnit と、`.sln` の外の MSTest `StandaloneTests`。後者は 8 ファイルで、`debug-all.yml` が別に実行する）。`?? throw new ArgumentNullException` 666 | §3.7 | RV7（判断） |
| RV-29 | メンテナンス性 | 中 | 経緯のコメント（`Phase<数字>` 1,656 行／487 ファイル、「旧」759 行） | §3.8 | RV8 |
| RV-30 | メンテナンス性 | 中 | テストの XML コメントの Phase 名（415 ファイル）、ソースの文字列を読むテスト 128、ガード系 114 | §3.8 | RV8 |
| RV-31 | メンテナンス性 | 低 | 文書 474 ファイル。完了した Phase の資料と現行の規約が同じ場所にある | §3.8 | RV8 |

## 5. MVVM 遵守率の算定

計測は `Measure-CodeReview.py --only mvvm`。XAML は `DS4Windows` 配下の全 `.xaml`、code-behind は全 `.xaml.cs`、ViewModel は `UI/ViewModels`。

| 指標 | 式 | 値 | 読み方 |
|---|---|---|---|
| ① 型の規則（V1・V2） | 基準 0 件のとき 100% | **100%** | `ViewModelWpfReferenceBaseline`＝0、`ViewDirectServiceReferenceBaseline`＝0。V2 は窓口の型を挟んで満たしている部分がある |
| ② コマンド率 | `Command` ÷（`Command` ＋ `Click` ＋ その他のイベント属性）＝ 0 ÷（0 ＋ 193 ＋ 46） | **0%** | 操作が `ICommand` を通っていない |
| ③ 結びつけ率（目安） | `{Binding}` ÷（`{Binding}` ＋ `x:Name`）＝ 771 ÷（771 ＋ 686） | **52.9%** | `x:Name` は code-behind から触る部品の目安。すべてが code-behind の参照とは限らない |
| ④ ViewModel の行の割合 | ViewModels ÷（ViewModels ＋ code-behind）＝ 14,558 ÷（14,558 ＋ 10,779） | **57.5%**（code-behind 42.5%） | code-behind のうち、ホバー・描画・フォーカス・ドラッグ（規則どおり View に置いてよい）の割合は、画面ごとに調べる必要がある（RV3・RV4 の最初の Step） |
| ⑤ 手結び | `+= メソッド名;` | 227 件（`BindingWindow` 149） | `ItemsControl`＋コマンドのパラメーターで消せる見込み |
| ⑥ 直接代入 | `IsEnabled` 61、`Visibility` 83、`Content` 106、`ItemsSource` 15、`SelectedIndex` 25 | 計 290 件 | 結びつけに置き換えられるもの、View の見た目だけのもの（ホバー・描画）が混ざる |

**総合**: ①は満点でも、②が 0% のため、「MVVM の原則を守っている割合」としては、**型の規則を満たしたうえで、操作と状態の流れは未着手**と読む。目標は、RV3・RV4 の完了時に、②を 70% 以上、⑤を 100 件以下、⑥を 150 件以下（残りは見た目だけの処理）に下げ、基準値を凍結して減らし続けること。目標値は承認のときに調整する（各計画書の D 項目）。

## 6. 既存計画との関係と、対象外にしたもの

### 6.1 既存計画との関係

| 既存の計画 | 状態 | 今回の扱い |
|---|---|---|
| Phase29〜36・38・40（前回レビューの是正） | 完了（一部は決定して見送り） | 重複させない。是正の確認だけ（§2） |
| Phase34（画面の MVVM 化。V1・V2 の基準値の削減） | 一区切り。基準値は 0 | その**続き**を RV3・RV4 にする。V3（コマンド）を新設。Phase34 の決定（ホバー・描画は View に置く）は踏襲 |
| Phase35（`App` と起動・終了の整理） | 完了 | `App.xaml.cs` の行数を目的にした整理は、しない |
| Phase37（設定・保存の整理） | 完了（37-6 は見送り） | 37-6（`AppSettingsDTO` の `partial`）の見送りの理由（`XmlSerializer` の要素の順序が宣言の順に依存）を、RV5 の制約として引き継ぐ。RV5 は、**保存の形を変えずに**宣言の置き場所を 1 つにする |
| Phase36（C 部の見送り） | 決定済み | C2・C4・C5・C6・C7 は再び持ち出さない |
| PhaseX3（日本語化。`{lex:Loc}`） | 進行中 | RV3・RV4 は XAML を大きく触るので、PhaseX3 の完了（または区切り）のあとに行う。XAML の文言の規則（`{lex:Loc キー}`）は、RV3・RV4 でも守る |
| PhaseX（KI-7。入力側の画像・ボタン名）、PhaseX4（KI-14） | 時期未定 | PhaseX は「一番最後」の決定のまま。RV の後にする。PhaseX4 は独立 |

### 6.2 対象外（前回までの決定を再び持ち出さない）

- `App.xaml.cs` の行数を減らすこと自体を目的にした整理（Phase35 の決定）。
- `Mapping.cs`・`TriggerDispatchEngine.cs` の分割（`Code-Review-2026-11/07` §2。最長のメソッドは `MapCustom` 124 行）。
- `AppSettingsDTO` の `partial` 分割（37-6 の見送り）。
- `AppLogger`・`PathService`・`EnvironmentService` の静的な状態の廃止（`Phase7-Residual-Ledger.md`）。
- 実装 1・偽物なしの既存のインターフェースの撤去（規則 3.1。新規に増やさないだけ）。
- `NoWarn` の CS0103 などの撤去（WPF の 1 回目のビルドの誤りを避ける既存の措置）。
- 自動生成コード（`Strings.Designer.cs` など）の整形。

### 6.3 守る制約

機能の維持（No Feature Drop）、**ログの文言とレベルの維持**（規則 2.3。ログを足す・変える Step は承認制、Phase13b の前例）、保存するファイルの形の不変（黄金テスト）、1 ファイル 1 型、移動は `git mv` のみ（別コミット）、新しく書くコメントは意図を書く（原則5）、既存の TODO（原則4）は残す、未参照の型は規則 2.4 の手順を踏む、コンストラクターの引数は 12 以下、テストの名前は機能名、用語は「スペシャルアクション」。

## 7. PhaseRV の順序と依存

```
RV1（品質の門）──┬─→ RV2（例外）──→ RV5（設定の宣言化）──┐
                 │                                          ├─→ RV7（統一）──→ RV8（コメント・文書・テスト）
                 └─→ RV3（MainWindow）──→ RV4（その他の画面）─→ RV6（共有部品・冗長）┘
```

| 順 | Phase | 前提・理由 |
|---|---|---|
| 1 | RV1 | 以降の Step の「警告 0」を、新しい診断に対しても保つ土台。リスクが低く、効果がすぐ見える |
| 2 | RV2 | 空の `catch` の規則（分類）を先に決め、RV3・RV5 が新しく書くコードに適用する |
| 3 | RV3 | `MainWindow` で、V3 とコマンドの基盤（`AsyncRelayCommand`、忙しい状態の表示）を作る。PhaseX3 の XAML の作業と競合しないように、X3 の区切りのあと |
| 4 | RV4 | RV3 の基盤を、残りの画面へ広げる |
| 5 | RV5 | RV2 の規則（読み込みの節の `try { } catch { }` の扱い）を前提にする。RV3・RV4 とは触るファイルが重ならないので、**並行ではなく順に**行うが、順は入れ替えてもよい（承認のとき決める。D） |
| 6 | RV6 | RV4（`WelcomeDialog`）と RV5（`Gyro*Info` の設定の値）の後に、残りの共有部品化 |
| 7 | RV7 | 命名・変更通知・タイマーの統一は、触るファイルが広いため、機能の Phase のあとで 1 回だけ行う |
| 8 | RV8 | コメントと文書の整理は、すべての Phase が触ったあと（衝突を避けるため最後） |

進め方は、これまでのとおり（`Phase` ごとにブランチを切り、Step ごとにテストと Debug ALL、完了で `--no-ff` で `master` へ統合、push はしない）。実機の確認は、最後にまとめて 1 件ずつ行う。

## 8. 全体の決定事項

各計画書の D 項目のうち、Phase をまたぐものだけ集める。

| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| G1 | 規則 V3（操作は `ICommand`）を、`copilot-instructions.md` §3.1 に足すか | (A) RV3 の完了時に足し、基準値を凍結する／(B) 規則にはせず、RV3・RV4 の実施だけにする／(C) 足さない |
| G2 | 例外の握りつぶしの規則（RV2 の分類）を、`Log-Policy.md` に足すか、`copilot-instructions.md` に足すか | (A) `Log-Policy.md` に節を足す（ログの扱いなので）／(B) 規約本体の §2 に足す |
| G3 | ログの変更（空の `catch` へ新しくログを足す、利用者に見えないファイルのログだけ）を、規則 2.3 の例外として扱うか | (A) 利用者に見える画面・トレイのログは変えず、ファイルのログ（診断）だけ足す。一覧で承認を得る／(B) ログは一切足さない（空の `catch` に理由のコメントだけ付ける） |
| G4 | 進める順（§7）。RV5 を RV3・RV4 より先にするか | (A) §7 のとおり（見た目の改善と、利用者の目に触れる効果を先）／(B) RV5 を先（保存の形の整理を先に終えたい場合） |
| G5 | `Nullable` の導入の範囲（RV1 の D） | (A) 新規・触るファイルだけ `#nullable enable`／(B) 層ごとに段階的に有効化／(C) 導入しない。**決定（2026-11-19）: (B)**。全体で有効にした実測は 2,153 件（359 ファイル）。試験は RV1-3、全体は PhaseRV9（予定） |

## 9. 方法と限界

- **方法**: `scripts/audit/Measure-CodeReview.py` が、行数・正規表現・空の `catch`（コメントを除いて数える）・重複（14 行の窓、正規化あり／10 行の窓、正規化なし）・インターフェースの実装数・参照のない型・テストの構成を数える。`python -I scripts/audit/Measure-CodeReview.py [--only size,mvvm,catch,comments,iface,unref,clone,cloneexact,fields,naming,tests,di]` で再現でき、各 Phase の完了の基準値に使う（`01-Metrics.md`）。数字は、正規表現による概算で、コンパイラーの解析ではない。
- **読んで確かめたもの**: 前回のレビュー・計画書（Phase29〜37・40、PhaseX3、`DI-App-Wide-Migration-Plan.md`、`Known-Issues-Ledger.md`、`Ideas.md`、`02-Extension-Guide.md`）。ソースは、`MainWindow.xaml.cs`、`MainWindowServiceControl.cs`、`MainWindowSettings.cs`、`BindingWindow.xaml.cs`、`ProfileEditor.HoverTables.cs`、`WelcomeDialog.xaml.cs`、`App.xaml.cs`、`KeyButtonActionController.cs`、`PressActionController.cs`、`RepeatHelperToIRepeaterAdapter.cs`、`AppSettingsService.cs`、`ProfileStickAxialLoadSection.cs`、`GyroMouseInfo.cs`・`GyroMouseStickInfo.cs`、`UdpServer.cs`、`Crc32Algorithm.cs`、`InputDeviceCore.cs`、`Build-Debug.ps1`、`.github/workflows`。
- **確かめて撤回した仮説**: `UdpServer` の `_clientsListCache` は `[ThreadStatic]` で問題なし。`Crc32Algorithm` の `testLook` は使われている。日本語名の追跡ファイルは欠陥ではない。
- **限界・要確認**:
  - 実機で動かしていない（読み取りと静的な計測のみ）。
  - `Core` の上位層の `using` 25 ファイルは、未使用と見られるが、全件は確認していない（RV1 の Step で IDE0005 により確認する）。
  - 重複の計測は、表・宣言の並びを多く含む。直す対象は、表以外の部分に限る。
  - MVVM の「code-behind のうち View に置いてよい部分」の割合は、画面ごとの確認が要る（RV3・RV4 の最初の Step）。
  - 評価の A〜D は主観を含む。数字を優先して読むこと。
