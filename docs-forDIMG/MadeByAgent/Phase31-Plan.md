# Phase31 計画書: プログラム起動の一本化と、出力の種類の分岐の整理（コードレビュー 2026-11 の 4-1・2-1・5-1）

作成日: 2026-11-11
状態: **未着手。承認待ち**
出典: `docs-forDIMG/Code-Review-2026-11/02-Layers-and-Placement.md`、`04-Duplication-and-Abstraction.md`、`05-Extensibility.md`

## 目的
1. 「プロファイルに結んだプログラムを起動する」処理を 1 か所にし、永続化の層から副作用をなくす。
2. 仮想パッドの出力の種類（Xbox 360／DS4）による分岐を、カートリッジと `OutContTypeInfo` に集め、3 つ目の種類を、手順書どおりに足せる状態にする。

2 つは別の主題だが、どちらも「第 2 層・第 3 層の境目の整理」で、規模が中くらいなので 1 つの Phase にまとめた。A 部（31-A）と B 部（31-B）は独立に進められる。

## 現状（調査）
### A. プログラム起動
- `DeviceConnectionSetup.CheckLauchProfileOption`（`Processing/Services/DeviceConnectionSetup.cs:78`〜）: `startUp` が偽のとき、プロセスを列挙し、なければ 5 秒後に起動する。
- `ProfileMiscLoadSection`（`Core/Persistence/ProfileSections/ProfileMiscLoadSection.cs:59`〜`110`）: `context.LaunchProgram` が真のとき、同じことをする。こちらだけ、先に `IProcessInspector` を試す。
- 要確認: 2 つが同じ場面で両方動くか（二重起動の有無）。

### B. 出力の種類の分岐（カートリッジと `OutContTypeInfo` の外）
`Output/OutputSlotManager.cs:158`、`Output/Services/OutputSlotView.cs:50`・`55`、`Output/Services/VirtualPadService.cs:135`〜`154`、`Processing/Services/InputReportProcessor.cs:170`、`Processing/Services/IpcCommandProcessor.cs:207`・`209`、`Processing/Macro/MacroParser.cs:101`、`Core/Model/X360ControlTables.cs:170`・`174`、`UI/ViewModels/BindingWindowViewModel.cs:84`（`use360Mode`）、`UI/ViewModels/SlotDeviceEntry.cs:128`、`UI/ViewModels/OutputSettingsViewModel.cs:35`・`56`。

## 方針（決定事項の案）
1. A: 読み込みの節は、値（`launchProgram`）を入れるだけにする。起動の判断と実行は、第 2 層の 1 つの部品（仮称 `ProfileProgramLauncher`。`IProcessInspector` と `IProcessLauncher` を受ける）が行う。5 秒の待ちと、起動の条件は変えない（No Feature Drop）。
2. B: 種類ごとの「値」の違い（保存キー、IPC の名前、ボタン名の表、既定かどうか）は `OutContTypeInfo` の項目に、種類ごとの「処理」の違い（送出の直前の後処理、表示用の番号）は `IOutputDeviceCartridge` の操作にする。
3. B: 3 つ目の種類は足さない。足せる形にするだけ。保存する値（種類の名前・番号）は変えない。
4. 毎フレームの経路（`InputReportProcessor.cs:170`）は、カートリッジの呼び出しに変えても、割り当て 0 を保つ（テストで固定）。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | A と B を同じ Phase で進めるか | (A) 同じ Phase。A を先に／(B) B を Phase31、A を別の Phase に分ける |
| D2 | A で二重起動が見つかった場合 | (A) 1 回に直す（不具合の修正として記録し、CHANGELOG に書く）／(B) 今の動作を固定する |
| D3 | B の `use360Mode`（二択の bool） | (A) 種類（`OutContType`）を持つ形に変え、画面の表示は種類から引く／(B) 触らない（3 つ目を足すときに直す） |
| D4 | B で、`InputReportProcessor` のトリガーの後処理をどこへ置くか | (A) DS4 の出力デバイス（`ConvertandSendReport` の中）／(B) カートリッジの「送出の直前の後処理」 |

## Step 一覧
### 31-A プログラム起動
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 31-A0 | 調査: 2 つの経路が動く条件（`startUp`、`context.LaunchProgram`）を、呼び出し元までたどって表にする。二重起動の有無を確かめる | — | 未着手 |
| 31-A1 | 起動の部品を第 2 層に作る（プロセスの確認は `IProcessInspector`、起動は `IProcessLauncher`、待ちは `TimeProvider`）。`DeviceConnectionSetup` はそれを呼ぶ | 偽のプロセスの確認・起動で、「動いていれば起動しない」「5 秒後に 1 回だけ起動する」 | 未着手 |
| 31-A2 | `ProfileMiscLoadSection` から、プロセスの列挙と起動を取り除く。読み込みの呼び出し側（`context.LaunchProgram` を真にしていた場所）が、読み込みのあとに 31-A1 の部品を呼ぶ | 読み込みのテストが、プロセスに触れないこと。保存の黄金テストが不変 | 未着手 |
| 31-A3 | `BackingStore.ProcessInspector`（プロパティでの後付けの配線）が不要になるかを確かめ、不要なら `ServiceRegistration.Wiring.cs` から外す（一覧のコメントも更新） | 配線のテスト | 未着手 |

### 31-B 出力の種類
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 31-B0 | 調査: 上の各場所が、種類の何（値か、処理か）で分岐しているかを表にする。`OutContTypeInfo` の今の項目を確かめる | — | 未着手 |
| 31-B1 | 値の分岐を `OutContTypeInfo` へ: IPC の名前（`IpcCommandProcessor`）、ボタン名の表（`X360ControlTables`、`MacroParser`）、既定の種類（`OutputSettingsViewModel`、`BackingStore.DEFAULT_OUT_CONT_TYPE`） | 全種類に項目があることの型の検査。IPC・マクロの表示名のテストが不変 | 未着手 |
| 31-B2 | 処理の分岐をカートリッジへ: 送出の直前の後処理（D4）、表示用の XInput の番号（`OutputSlotView`）、スロットの差し込み（`OutputSlotManager`） | 仮想パッドの送出のテストが不変。割り当て 0 | 未着手 |
| 31-B3 | （D3 が A のとき）`BindingWindowViewModel.use360Mode`、`SlotDeviceEntry` を、種類から表示を引く形にする | ViewModel のテスト | 未着手 |
| 31-B4 | 検査: カートリッジ・`OutContTypeInfo`・既定値の定義のほかに、`OutContType.X360`／`.DS4` を名指しする場所がないことを、型の検査（ArchUnitNET）で守る。残す場所（`VirtualPadService` の起動時の一時的な差し込みなど）は、理由つきで許可リストに入れる | 新規 | 未着手 |
| 31-B5 | 文書: `02-Extension-Guide.md` §2 を、実際の手順に書き直す。規則 3.1 を更新。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ | — | 未着手 |

## 確認の方法
- 実機: (A) 「起動するプログラム」を設定したプロファイルを、接続時・手動の切替・自動プロファイルで適用して、起動が 1 回だけ起きること。すでに動いているときは起動しないこと。(B) 出力の種類の切替（Xbox 360 ⇄ DS4）、DS4 型でのトリガー、CLI の `-command` での差し込み、出力ピッカーの表示、マクロの表示名。
- 未確認の項目は `Known-Issues-Ledger.md` §3 へ足す。

## リスク
- A: 中。起動の時機が変わると、利用者に見える（起動しない／2 回起動する）。31-A0 の表を先に固める。
- B: 中。毎フレームの経路を 1 か所触る。DS4 型のトリガーの動作は実機で確かめる。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
