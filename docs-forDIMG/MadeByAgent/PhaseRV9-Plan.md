# PhaseRV9 計画書: Nullable の層ごとの全体有効化（と、品質の門の最後の 1 つ）

作成日: 2026-11-19
状態: **D1〜D4 は決定済み（2026-11-19。「決定事項」を参照）。着手は未承認（コードは未着手）**。着手には、Phase ごとの承認が要る
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の G5（`Nullable` の範囲。2026-11-19 に (B)＝層ごとに全体を有効化と決定）、`PhaseRV1-Plan.md` の D2、`PhaseRV1-Nullable-Trial.md`（試験の記録。本計画の根拠）
前提: PhaseRV1 の完了（試験の 2 層、`CodeHealthRatchetTests` の枠）。**進める順は D1＝(C)**: A 組（RV9-0〜6）を RV2 の前に、B 組（RV9-7〜12）を待つ Phase の完了後に、切り替え（RV9-13）以降を最後に（「順序」を参照）。
動き: **機能・保存の形・ログは変えない**。注釈（`?`）が中心。`null` の流れを変える修正（ガード）は、台帳に 1 件ずつ残す。

## 目的
1. `DS4Windows`（本体）と `DS4WindowsCli` の全ファイルで `null` 許容の注釈を有効にし、警告 0 にする。最後に、プロジェクトの設定 `<Nullable>enable</Nullable>` に切り替える。
2. 「`null` かもしれない値」を型で表し、`null` 参照の不具合（NRE を `catch` で握りつぶしていた箇所を含む）を、ビルドで見つけられるようにする。
3. 品質の門（採点基準の「警告をエラー化・アナライザー有効・スタイル診断を無効化していない」）の最後の 1 つ、`TreatWarningsAsErrors` を、D4＝(A) の決定に従って、本体と CLI だけ有効にする（今は 2/3）。

## 背景（実測。2026-11-19、RV1 完了後に `-p:Nullable=enable` で 1 回ビルドして測った）
- 残りの警告: **2,131 件・349 ファイル**（本体）。`DS4WindowsCli` は 5 件（`CommandLineClient` 3、`CliArguments` 2）。`#nullable enable` を持つのは 56 ファイル（`Core/Abstractions` 37、`Composition` 19。RV1-3）で、**残る 909 ファイル**に指令を足す。警告のあるのは 349 ファイルで、**残りの約 560 ファイルは、指令を足すだけ**で済む。
- 種類: `CS8600` 638（`null` かもしれない値を非 `null` へ）、`CS8625` 378（`null` リテラルを非 `null` へ）、`CS8602` 349（`null` かもしれない値の参照）、`CS8618` 256（非 `null` のフィールド・プロパティが未初期化）、`CS8622` 182（イベントハンドラーの `object sender`）、`CS8603` 147（`null` かもしれない値を返す）、`CS8604` 113（`null` かもしれない引数）、`CS8601` 41、`CS8612` 15（イベントの型）、その他 10。
- 偏り: `Core/Persistence` 677（XML の `SelectSingleNode(...).InnerText` など。`CS8600`・`CS8602` が大半）と `UI` 684 で、全体の 64%。上位のファイル: `AppSettingsXmlSerializer` 116、`ProfileStickAxialLoadSection` 73、`ProfileStickOutputLoadSection` 64、`ProfileEditor.xaml.cs` 59、`ProfileTriggerStickBasicLoadSection` 59、`MainWindow.xaml.cs` 57。
- 試験（RV1-3）の結果: 27 件を、型の `?`（9＋7＋6＋5 件）で直した。`!` は 1 か所（理由のコメントつき）。手を入れたのは 3 か所だけ。連鎖は 1 件（下の層から有効にすると少ない）。ファイル単位の `#nullable enable` で、層ごとに進められる。
- **他の Phase との重なり**: `Core/Persistence` は RV5 が宣言化で書き直す（`try { … SelectSingleNode(…).InnerText … } catch { }` の形の大半が、`NRE` を握りつぶす作りで、注釈を付けるとガードの追加＝動きの変更になる）。`UI` の画面（`MainWindow`、`ProfileEditor`、`BindingWindow`、`RecordBox`、`WelcomeDialog`）は RV3・RV4 が書き直す。`Processing/Actions` の `KeyButtonActionController` まわりは RV6、`Core/Settings` の `AppSettingsService`・`Gyro*Info` は RV5・RV6。書き直される前に注釈を付けると、作業が無駄になり、動きを変えるガードも無駄に入る。

### フォルダー別の件数と、待つ Phase
| 組 | フォルダー | 警告 | 待つ Phase（理由） |
|---|---|---|---|
| A | `Core/Model` 10、`Core/Logging` 18、`Core/Utilities` 23、`Core/Updater` 7、`Core/Profiles` 6 | 64 | なし |
| A | `Core/Platform` | 69 | なし |
| A | `Input`（`Hid`・`Xbox`・`Devices`・`Abstractions` を含む） | 84 | なし（RV7 の小文字メソッドの改名は、注釈と独立） |
| A | `Output`（`Services`・`Udp`・`Devices`・`Backend`・`Kbm` を含む） | 98 | なし |
| A | `Processing`（`Actions` を除く。`Services` 128、`Abstractions` 8、`Macro` 6、`Touchpad` 4、`Gyro` 1、直下 9） | 156 | なし |
| A | ルート（`App.xaml.cs` 45）と `DS4WindowsCli`（5） | 50 | なし |
| B | `Core/Settings` | 110 | RV5（`AppSettingsService` の畳み込み）、RV6（`Gyro*Info`） |
| B | `Core/Persistence` | 677 | RV5（プロファイルの節を宣言化して書き直す） |
| B | `Processing/Actions` | 144 | RV6（`KeyButtonActionController` まわりの整理） |
| B | `UI/Abstractions` 9、`UI/Converters` 9、`UI/Services` 71、`UI/Controls` 36 | 125 | RV3（トレイ・コマンドの共有） |
| B | `UI/ViewModels` | 252 | RV3・RV4（ViewModel の分割・コマンド化） |
| B | `UI` 直下の画面 | 307 | RV4（`ProfileEditor`・`BindingWindow`・`RecordBox`・`WelcomeDialog` の整理） |
| 計 | A 516（＋Cli 5）／B 1,615 | 2,131 | A は全体の 24%。**B が 76%** |

## 方針・制約
- **ファイル単位の `#nullable enable` で進める**（プロジェクトの設定は、最後に 1 回だけ変える。試験で確認済み）。1 つの組（フォルダー）ごとに、指令を足す→ビルド→警告 0→Debug ALL→コミット。コミットは機械的な指令の追加と、手を入れた修正を分ける（差分で見分けられるようにする）。
- **下の層から**（連鎖が少ない）。順序は Step 一覧のとおり。
- **値・初期状態・保存の形を変えない**。型の `?` と、直し方の型（試験の表）で済ませる。未初期化（`CS8618`）は、まず `?` にする。「非 `null` の既定値で初期化する」は、見えない動きの変更（XML の出力が空要素になる、など）になりうるため、黄金テストで確かめられるものに限り、別コミットにする。
- **`!`（`null` 許容の抑止）は、不変条件を説明するコメントがあるときだけ**（D2）。`#pragma warning disable CS86xx` は使わない。
- **ガードの追加は、動きを変えうる**。`NullReferenceException` を `catch` で捨てていた箇所に、ガードを足すと、以前は例外で途中から抜けていた流れが、別の流れになる。足したガードは、`PhaseRV9-Guards.md`（台帳）に「ファイル:行、以前の動き、新しい動き、根拠のテスト」を 1 件ずつ書く。毎フレームの経路・書き込みの経路は、動きが同じことをテストで確かめる。
- **テストは対象外**（`DS4WindowsTests`・`StandaloneTests`）。本体の注釈で、テストの側に警告は出ない（テストは `Nullable` を指定していない）。ただし、署名の文字列を読むテスト（RV1-3 の `AppHostNoImplicitHostGuardTests` の前例）は、注釈の追加で期待値を直す。
- 自動生成コード（`*.g.cs` など）は対象外（RV0 の測定で、警告は出ていない）。
- 新規ファイルは `#nullable enable` で書く（D2 で、規約へ 1 行足す）。RV2〜RV8 で新しく作るファイルも同じ。

## Step 一覧
| Step | 内容 | テスト | 前提 | 状態 |
|---|---|---|---|---|
| RV9-0 | 準備。(1) 道具: `scripts/audit/Measure-Nullable.py`（ビルドの記録からフォルダー別の件数表を出す。再実行できる）、`scripts/audit/Add-NullableDirective.py`（指令の追加。BOM・改行・ライセンスのコメントの後・最初の `using` の前を保つ。何度流しても同じ）、機械的な修正（`CS8622` の `object sender`→`object? sender`、`CS8612` のイベントの `?`）を警告の位置から行う `Fix-NullableMechanical.py`。(2) 検査: `CodeHealthRatchetTests` に「`#nullable enable` を持つファイルの数」（下回らない。実数と一致）と「理由コメントのない `!`」「`#pragma warning disable CS86`」の 0 を足す（`CodeHealthRatchet` に「下回らない」の評価を足す）。(3) 台帳 `PhaseRV9-Warning-Ledger.md`（組・フォルダー・件数・待つ Phase・状態）と `PhaseRV9-Guards.md`（空）を作る。(4) 規約: `copilot-instructions.md` §3.3 に「新規ファイルは `#nullable enable`、`!` は理由コメントつきだけ」の 2 行（D2 の承認の範囲） | 新規のテスト自身（偽の入力で、下回る・不一致が失敗すること）。スクリプトは偽のログで確かめる | RV1 | 未着手 |
| RV9-1 | **A 組** `Core/Model`・`Core/Logging`・`Core/Utilities`・`Core/Updater`・`Core/Profiles`（64 件）。`ProfileList`・`AutoProfileHolder` は、WPF のコレクションの同期のため、`?` の付け方に注意 | Debug ALL | RV9-0 | 未着手 |
| RV9-2 | A 組 `Core/Platform`（69 件。P/Invoke の構造体の `string`・配列は、マーシャリングが変わらないこと［注釈は型だけ］を、型の検査で確かめる） | Debug ALL | RV9-1 | 完了（2026-11-19） |
| RV9-3 | A 組 `Input`（84 件。`InputDeviceCore`・`DualSenseDevice`・`SwitchProDevice`。毎フレームの経路は、ガードを足さず、`?` と不変条件の `!` で済ませる。済ませられないときは台帳） | Debug ALL、`Ds4DeviceBehaviorTests` など機種の特性化テスト | RV9-2 | 完了（2026-11-19） |
| RV9-4 | A 組 `Output`（98 件。`UdpServer`、`Kbm`、`Backend`。ViGEm の型を参照する範囲は `Output/Backend` の中だけ［規約］のまま） | Debug ALL | RV9-3 | 完了（2026-11-19） |
| RV9-5 | A 組 `Processing`（`Actions` を除く 156 件。`Services`・`Macro`・`Touchpad`・`Gyro`・直下） | Debug ALL | RV9-4 | 完了（2026-11-19） |
| RV9-6 | A 組 ルート（`App.xaml.cs`。起動・終了の流れはガードを足さない）と `DS4WindowsCli`（5 件） | Debug ALL、`AppShutdownSequenceTests`、`AppHostStartupShutdownGuardTests` | RV9-5 | 完了（2026-11-19） |
| RV9-7 | **B 組** `Core/Settings`（110 件） | Debug ALL、`BackingStore*Tests`、黄金テスト | RV5・RV6 の完了 | 完了（2026-11-20。警告 0。`!` は理由コメントつき 11 件。動きを変えたガードの追加はなし） |
| RV9-8 | B 組 `Core/Persistence`（677 件。RV5 の宣言化のあとに残る分。新しい宣言型の部品は、RV5 のなかで `#nullable enable` で書いてある前提） | Debug ALL、黄金テスト（保存の形が変わらないこと） | RV5 の完了 | 完了（2026-11-20。警告 0。`!` は理由コメントつき。XML の読み込みの「ない要素は NullReferenceException」は、`Required()` で明示（動きは同じ）。`e.InnerException` は null のときメッセージを使う 4 か所だけ動きが変わる。スロットの名前リストは `GetRequiredProfileActions` に集約） |
| RV9-9 | B 組 `Processing/Actions`（144 件） | Debug ALL、`TriggerDispatchEngine`・アクションのテスト | RV6 の完了 | 完了（2026-11-20。警告 0。`!` は理由コメントつき。契約の `?` 化: `IActionBinding.Source`／`Input`、`IOutputContext.Frame`／`OutputHandler`、`ISpecialActionOutputFactory.Create`、`IManagedActionManager.GetStateFor`。エンジンの実行時の表は最初の Prepare で作る前提の `null!`。動きが変わるのは、`DefaultProcessLauncher` の作業フォルダー（ルート直下で NRE → null）だけ） |
| RV9-10 | B 組 `UI/Abstractions`・`UI/Converters`・`UI/Services`・`UI/Controls`（125 件） | Debug ALL | RV3 の完了 | 完了（2026-11-20。警告 0。契約の `?` 化: `IDialogService.PickOpenFile`/`PickFolder`/`PickSaveFile`（キャンセルで null）、`IIpcCommandHost.GetProfileName`/`GetControllerDevice`、`AssignmentEditorSession`/`CreateAssignmentEditor` の `SpecialAction`、`HitboxRegion.Tag`。`!` は理由コメントつき） |
| RV9-11 | B 組 `UI/ViewModels`（252 件。`CS8622`［ハンドラーの `object? sender`］、`CS8612`［`PropertyChanged` の `?`］は機械的に） | Debug ALL、ViewModel のテスト | RV3・RV4 の完了 | 完了（2026-11-20。警告 0。契約の `?` 化: `IActionEditTarget.Name`／`SavedAction`、`IActionTriggerSource.TriggersText`／`UnloadTriggersText`／`Validate` の `out string?`、`ProgramItem.Title`／`MatchedAutoProfile`、`SelectedItem`、`CompositeDeviceModel.LinkedProfileName`、`UpdateSettings` の `string? exts` ほか。`!` は理由コメントつき（ダイアログ窓口の 3 か所は ctor で 1 回だけ）） |
| RV9-12 | B 組 `UI` 直下の画面（307 件。code-behind。`FindName`・`DataContext` の取り扱いは、`?` と不変条件の `!`） | Debug ALL、`XamlTestHelper`・画面の読み込みテスト | RV4 の完了 | 未着手 |
| RV9-13 | 切り替え。(1) `DS4WinWPF.csproj` に `<Nullable>enable</Nullable>`（`DS4WindowsCli` も）。(2) ファイルごとの `#nullable enable` を機械的に消す（D3。1 コミット。差分が「指令の行の削除だけ」であることを、スクリプトで検証する）。(3) `DS4WinWPF.csproj` と `DS4WindowsCli` に `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`（D4＝(A)。ルートの `Directory.Build.props` は `false` のまま。テスト・`externals` を巻き込まない）。(4) `CodeHealthRatchetTests` の「指令を持つファイルの数」は不要になるので外し、「`!`（理由なし）＝0」だけ残す。(5) `Rubric-Inputs.json` の `nullable_layers_enabled` は、設定から自動で 6 になる（`Score-CodeReview.py`） | Debug ALL、Release ビルド（警告 0）、StandaloneTests | RV9-1〜12 のすべて、RV2〜RV8 の完了（D1＝(C)。切り替えは最後） | 未着手 |
| RV9-14 | 実機確認。台帳 `PhaseRV9-Guards.md` の、動きを変えうる箇所（機器の抜き差し、プロファイルの切替、保存・読み込み、起動・終了）だけを、`Known-Issues-Ledger.md` §3 と `Device-Check-Checklist.md` に 1 件ずつ足す | 実機（最後にまとめて） | RV9-13 | 未着手 |
| RV9-15 | 評価の再算定（採点基準 `Code-Review-2026-11-19/03-Rubric.md`）。(1) `scripts/audit/Rubric-Inputs.json` の確認項目と手入力の値を、根拠（実行したテスト・実測）を `evidence` に書いて更新する。(2) `python -I scripts/audit/Score-CodeReview.py --json docs-forDIMG/Code-Review-2026-11-19/scorecard/RV9.json --label "RV9 完了" --diff <直前の保存>` で、採点して保存する。(3) `02-Scorecard.md` に列と理由（等級が変わった観点、点数が動いた指標、予想とのずれ）を足し、`01-Metrics.md` の基準を `Measure-CodeReview.py` の実測で取り直す。(4) ユーザーへ、点数・等級の変化を報告する。机上の予想（`03-Rubric.md` §5）で動く観点: メンテナンス性（`Nullable` を有効にした層 6 層［今は 1］、品質の門 3/3［今は 2/3］）。ほかの観点は動かない見込み。ソースは変更しない（記録のみ） | `Score-CodeReview.py`（記録のみ） | RV9-14 | 未着手 |

組 B の Step は、待つ Phase が済むまで着手しない。

## 順序（D1＝(C) の決定）
```
RV1 ✔ → RV9-0〜6（A 組。RV2 の前）→ RV2 → RV3 → RV4 → RV5 → RV6 → RV9-7〜12（B 組。待つ Phase の完了ごと）→ RV7 → RV8 → RV9-13〜15（切り替え・実機確認・再算定）
```
- B 組の Step ごとの待ち先: RV9-10 は RV3、RV9-11・12 は RV4、RV9-7・8 は RV5・RV6、RV9-9 は RV6。待ち先が済めば、RV7・RV8 を待たずに着手してよい。
- RV2〜RV8 で新しく作るファイルは、最初から `#nullable enable`（D2）。RV2〜RV8 が、A 組のファイルに手を入れるときは、警告 0 を保つ（A 組は指令が入っているため、`null` の警告が出れば、Debug ALL の「警告 0」で見つかる。`TreatWarningsAsErrors` は、切り替えの RV9-13 まで `false`）。
- 切り替え（RV9-13）を最後にするのは、ファイルごとの指令の一括削除（約 960 ファイル）が、RV2〜RV8 の差分と衝突しないようにするため。

## 決定事項（2026-11-19。ユーザーの選択）
| # | 決めたこと | 選択 |
|---|---|---|
| D1 | 進める順 | **(C) 組で分ける**。A 組（RV9-0〜6）を RV2 の前に、B 組（RV9-7〜12）を、待つ Phase の完了後に、切り替え（RV9-13）以降を最後に |
| D2 | `!`・`#pragma` と新規ファイルの規則 | **(A) 理由コメントつきの `!` だけ許可**。`#pragma warning disable CS86xx` は禁止。理由なしの `!` を 0 に凍結（`CodeHealthRatchetTests`）。規約 §3.3 へ 2 行（「新規ファイルは `#nullable enable`」「`!` は理由つきだけ」）。**規約本体の変更は RV9-0 の中で行う**（D2 の承認の範囲。文面は、そのときに確認する） |
| D3 | 切り替えのあとの、ファイルごとの指令 | **(A) 設定 `<Nullable>enable</Nullable>` に切り替え、指令を機械的に消す**（1 コミット。差分が「指令の行の削除だけ」であることを、スクリプトで検証する） |
| D4 | `TreatWarningsAsErrors` | **(A) `DS4WinWPF.csproj` と `DS4WindowsCli` だけ `true`**（RV9-13）。品質の門は 2/3→3/3（採点のメンテナンス性が約 +3.3 点）。パッケージの更新で新しい警告が出てビルドが止まるときは、`Analyzers.globalconfig` の 1 行で、その警告だけを一時的に外す。設定全体を戻す必要はない |

## 選択肢の記録（決める前に示したもの）
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | 進める順（RV2〜RV8 との前後） | (C) 組で分ける: A 組（RV9-0〜6。他の Phase に書き直されない 516 件）を RV2 の前に、B 組（RV9-7〜12）を、待つ Phase の完了後に、切り替え（RV9-13）以降を最後に／(A) 全部を RV8 のあとに 1 回／(B) 全部を今（RV2 の前）に | (C) 利点: 書き直される前の注釈の無駄がなく（B 組の 76%）、A 組の層（入力・出力・基盤）は RV2〜RV8 の大きな変更のあいだ、型の検査の恩恵を受ける。欠点: Phase が 2 か所に分かれ、切り替えは最後まで待つ。(A) 利点: 最終のコードに 1 回で済む。欠点: RV2〜RV8 の大規模な変更が、`null` の検査なしで進む。(B) 利点: 以降のすべてが型の検査つき。欠点: `Core/Persistence`（677 件）と `UI`（画面の約 4 割）の注釈が、RV5・RV3・RV4 で書き直されて無駄になり、動きを変えるガードも無駄に入る |
| D2 | `!`（`null` 許容の抑止）・`#pragma` と、新規ファイルの規則 | (A) `!` は、不変条件を説明するコメントがあるときだけ。`#pragma warning disable CS86xx` は禁止。理由なしの `!` を 0 に凍結（`CodeHealthRatchetTests`）。規約 §3.3 へ「新規ファイルは `#nullable enable`」と「`!` は理由つきだけ」の 2 行を足す／(B) `!` を制限しない（規約・検査なし）／(C) `!` は一切禁止 | (A) 利点: 試験（1 か所）と同じ運用で、抜け道を数字で見張れる。欠点: コメントの質は人が見る。規約本体の変更 2 行（承認の対象）。(B) 利点: 手間が最小。欠点: `!` で警告を消すだけになり、型で守る意味が薄れる。(C) 利点: 厳密。欠点: 不変条件が確かな箇所（例: `Path.GetDirectoryName` の結果）にも、不要な分岐を足すことになり、動きの変更を招く |
| D3 | 切り替えのあとの、ファイルごとの指令 | (A) プロジェクトの設定 `<Nullable>enable</Nullable>` にして、ファイルごとの `#nullable enable` を機械的に消す（1 コミット。差分が指令の行の削除だけであることを検証）／(B) 指令を残す（設定は変えず、検査で「全ファイルに指令がある」を守る）／(C) 設定にして、指令も残す | (A) 利点: 約 960 ファイルの先頭がきれいになり、新規ファイルは何も書かなくてよい。採点基準も設定で 6 層と数える。欠点: 960 ファイルを触る 1 コミット（機械的で、差分は検証できる）。(B) 利点: 触らない。欠点: 新規ファイルに指令を忘れると無効。採点が設定を見ない。(C) 利点: 安全側。欠点: 冗長な指令が残る（スタイルの診断で冗長と出る） |
| D4 | `TreatWarningsAsErrors`（品質の門の 3 つ目） | (A) RV9-13 で `DS4WinWPF.csproj`（と `DS4WindowsCli`）に `true`。`Directory.Build.props` には置かない（テスト・`externals` を巻き込まない）。警告が 0 の状態を保つ／(B) `WarningsAsErrors` に `nullable` だけを指定（`Nullable` の警告だけをエラーにする）／(C) 今のまま `false`（品質の門は 2/3 のまま） | (A) 利点: 新しい警告がビルドで止まる（品質の門が 3/3。採点が上がる）。欠点: パッケージの更新（アナライザー）で、新しい警告が出ると、ビルドが止まる（`Analyzers.globalconfig` の運用で、その場で扱える）。外すのは設定 1 行。(B) 利点: `Nullable` だけを厳格に。欠点: ほかの警告は素通り。品質の門は 3/3 にならない。(C) 利点: リスクなし。欠点: 警告 0 を保つのが、人の運用だけになる |

## 確認の方法
- 各組の Step の中で、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0。`Build-Debug.ps1`、テストの建て、`Execute Test`）。
- 指令の追加だけのコミットは、`git diff` で「先頭の 1 行の追加」だけであることを、スクリプトで検証する。
- ガード（動きを変えうる修正）は、台帳に書き、その箇所のテスト（偽の `null` を渡す）を足す。保存の形に触れうるフォルダー（`Core/Settings`・`Core/Persistence`）は、黄金テストを先に流して基準にする。
- 実機確認は RV9-14（台帳の箇所だけ、最後にまとめて）。

## リスク
- 中。**注釈のつもりが、動きを変える**: `NullReferenceException` を `catch` で捨てていた箇所は、ガードを足すと流れが変わる。台帳（ガードの 1 件ずつ）と、偽の `null` を渡すテストで抑える。`Core/Persistence` を RV5 のあとにする（D1＝(C)）のは、この種の箇所が最も多いため。
- XML のシリアライズ（`XmlSerializer` の DTO）: `?` の追加で、出力が変わらないこと（黄金テスト）。
- 署名の文字列を読むテストの期待値の修正が必要（RV1-3 で 1 件あった）。
- 件数が大きい（2,131 件）。層ごとのコミットと、機械的な修正のスクリプトで、1 Step の大きさを抑える。ビルドは 1 回 約 55 秒。
- `TreatWarningsAsErrors`（D4＝(A)）で、アナライザーのパッケージ更新や、新しい SDK の警告が、ビルドを止める。設定 1 行で戻せる。
- 他の Phase との順序（D1）。

## 完了の定義
- `DS4WinWPF.csproj` と `DS4WindowsCli` が `<Nullable>enable</Nullable>`（D3＝(A) の場合。ファイルごとの指令は消えている）で、警告 0・エラー 0（Debug・Release）。
- 理由なしの `!` が 0、`#pragma warning disable CS86xx` が 0（`CodeHealthRatchetTests`）。
- `TreatWarningsAsErrors` が D4 の決定どおり（(A) なら `true`）で、Debug ALL が通る。
- `PhaseRV9-Guards.md` の全件に、以前の動き・新しい動き・根拠のテストがある。台帳 `PhaseRV9-Warning-Ledger.md` の全フォルダーが完了。
- 規約に、新規ファイルの規則（D2）が書かれている。
- 評価の再算定（最終 Step）が済み、`02-Scorecard.md` に記録され、ユーザーへ報告されている（予想とのずれの理由つき）。

## 関連計画
- `PhaseRV1-Nullable-Trial.md`（試験の記録）、`PhaseRV1-Plan.md`（D2、`CodeHealthRatchetTests` の枠）。
- 順序に関わる Phase: `PhaseRV5-Plan.md`（`Core/Persistence`・`Core/Settings`）、`PhaseRV6-Plan.md`（`Processing/Actions`）、`PhaseRV3-Plan.md`・`PhaseRV4-Plan.md`（`UI`）。`PhaseRV7-Plan.md` の `?? throw new ArgumentNullException` → `ThrowIfNull` は、注釈と独立（どちらが先でもよい）。
- 規約: §2.2（機能の維持）、§3.3（原則 5。コメントには意図を書く。`!` の理由コメントに適用）、§4（検査は型で、`Slow`・`Benchmark` の印）。

## 履歴
- 2026-11-19: 計画書を作成（RV1-3 の試験の結果と、RV1 完了後の再計測 2,131 件／349 ファイルを根拠に）。コードは未着手。D1〜D4 はユーザーの選択待ち。
- 2026-11-19: D1〜D4 をユーザーが選択（D1＝(C)、D2＝(A)、D3＝(A)、D4＝(A)。D4 は、品質の門の意味［今は 2/3］を説明し直したうえでの選択）。「順序」「決定事項」の節を足し、RV9-13 の前提と内容を決定に合わせた。着手は、Phase ごとの承認待ち。
