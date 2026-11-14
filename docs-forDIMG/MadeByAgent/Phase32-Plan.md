# Phase32 計画書: プログラム起動の一本化と、出力の種類の分岐の整理（コードレビュー 2026-11 の 4-1・2-1・5-1）

作成日: 2026-11-11
状態: **32-A 完了（2026-11-14）。32-B 実施中**
出典: `docs-forDIMG/Code-Review-2026-11/02-Layers-and-Placement.md`、`04-Duplication-and-Abstraction.md`、`05-Extensibility.md`

## 目的
1. 「プロファイルに結んだプログラムを起動する」処理を 1 か所にし、永続化の層から副作用をなくす。
2. 仮想パッドの出力の種類（Xbox 360／DS4）による分岐を、カートリッジと `OutContTypeInfo` に集め、3 つ目の種類を、手順書どおりに足せる状態にする。

2 つは別の主題だが、どちらも「第 2 層・第 3 層の境目の整理」で、規模が中くらいなので 1 つの Phase にまとめた。A 部（32-A）と B 部（32-B）は独立に進められる。

## 現状（調査）
### A. プログラム起動
- `DeviceConnectionSetup.CheckLauchProfileOption`（`Processing/Services/DeviceConnectionSetup.cs:78`〜）: `startUp` が偽のとき、プロセスを列挙し、なければ 5 秒後に起動する。
- `ProfileMiscLoadSection`（`Core/Persistence/ProfileSections/ProfileMiscLoadSection.cs:59`〜`110`）: `context.LaunchProgram` が真のとき、同じことをする。こちらだけ、先に `IProcessInspector` を試す。
- 追加調査（2026-11-11）: 2 つは場面で分かれており、たどった範囲では二重に起動しない。
  - 接続のとき: 読み込みは「起動しない」で行い（`ProfileSlotApplier.cs:38` の引数仕様）、そのあと `DeviceConnector.cs:106` が `ApplyProfileOptions`（`startUp` は偽）を呼んで、第 2 層の側が起動する。
  - 切替のとき（特殊アクションのプロファイル切替 `ProfileApplicationService.cs:124`・`130`、自動プロファイルの一時プロファイル `AutoProfileService.cs:227`、IPC の `LoadTempProfile` `IpcCommandProcessor.cs:177`）: 読み込みを「起動する」で行い、永続化の側が起動する。読み込みの中からの `CheckProfileOptions` は `startUp` が真（`BackingStore.cs:1171`）で、第 2 層の側は起動しない。
  - 画面からの手動の切替、自動プロファイルの既定への復帰（`AutoProfileService.cs:279` は「起動しない」）は、どちらも起動しない見込み（32-A0 で表にして確かめる）。

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
| D1 | A と B を同じ Phase で進めるか | (A) 同じ Phase。A を先に／(B) B を Phase32、A を別の Phase に分ける |
| D2 | A で、場面ごとの起動する・しないの違いをどう扱うか（追加調査で、二重起動はない見込みと分かった。違いは「接続時」「切替時」で別の実装が動くこと） | (A) 今の場面ごとの動作を、表のとおりに固定して 1 つの部品へ移す（動作は不変）／(B) 場面ごとの違いを見直す（機能の変更。別に承認を得る） |
| D3 | B の `use360Mode`（二択の bool） | (A) 種類（`OutContType`）を持つ形に変え、画面の表示は種類から引く／(B) 触らない（3 つ目を足すときに直す） |
| D4 | B で、`InputReportProcessor` のトリガーの後処理をどこへ置くか | (A) DS4 の出力デバイス（`ConvertandSendReport` の中）／(B) カートリッジの「送出の直前の後処理」 |

## 決定の記録（2026-11-13。ユーザーの回答）
D1＝A（同じ Phase。A を先に）、D2＝**B（場面ごとの違いを見直す）**、D3＝A（`OutContType` を持つ形）、D4＝A（DS4 の出力デバイスの中）。
- D2＝B は機能の変更にあたる（規約 2.2）。32-A0 の表ができた時点で見直し案を提示し、**2026-11-14 に案 A を承認**: 「特殊アクションの復帰」「自動プロファイルの既定への復帰」「保存・名前変更のあとの再適用」以外の、プロファイルを選ぶ・切り替える・接続する場面は、すべて起動する。今との差は 1 点だけで、**メイン画面のプロファイル選択（コンボ）でも起動する**ようになる（今はトレイの選択だけ）。

## 32-A0 の結果（2026-11-14。調査）
起動は、読み込みの引数 `launchProgram`（真なら `ProfileMiscLoadSection` が起動）と、接続時の `ApplyProfileOptions(startUp: false)`（`DeviceConnectionSetup` が起動）の 2 か所。読み込みの中から呼ばれる `CheckProfileOptions` は常に `startUp: true`（`BackingStore.cs:1171`）で、起動しない。

| 場面 | 入口 | `launchProgram` | 起動する側 | 起動するか |
|---|---|---|---|---|
| 接続（通常のプロファイル） | `ConnectionProfileSelector` → `ApplyToSlot` | 偽 | `DeviceConnector.cs:106` の `ApplyProfileOptions`（`DeviceConnectionSetup`） | する（1 回） |
| 接続（一時プロファイル／自動プロファイルが残っている） | `PublishSlot`（読み込まない） | — | 同上 | する |
| メイン画面のプロファイル選択（コンボ） | `ProfileSelectionCoordinator` → `ApplyToSlot` | 偽 | なし | **しない** |
| 保存・名前変更のあとの再適用 | 同上 `ApplyToSlot` | 偽 | なし | しない |
| トレイのメニューで選択 | `ApplyFromTray` → `ApplyFromUi` | 真 | `ProfileMiscLoadSection` | **する** |
| 特殊アクションの切替 | `ApplyFromAction` | 真 | 同上 | する |
| 特殊アクションの復帰 | `RestoreFromAction` | 偽 | なし | しない |
| IPC `LoadProfile`／`LoadTempProfile` | `IpcCommandProcessor` | 真 | 同上 | する |
| 自動プロファイルの一時プロファイル | `AutoProfileService.cs:227` | 真 | 同上 | する |
| 自動プロファイルの既定への復帰 | `AutoProfileService.cs:279` | 偽 | なし | しない |

- 二重起動: 同じ操作で両方の経路が動く場面はない（接続は偽→`DeviceConnectionSetup` のみ、切替は真→読み込みの節のみ）。ただし、接続の直後に自動プロファイルが一時プロファイルへ切り替えると、5 秒の待ちの間は両方が「起動していない」と見るため、まれに 2 回起動しうる（今も同じ）。
- 追加調査で見つけた差: (1) メイン画面のコンボは起動しないが、トレイの選択は起動する（計画書の初稿の「手動はどちらも起動しない」は誤り）。(2) 稼働の確認は、読み込みの節は `IProcessInspector`、`DeviceConnectionSetup` は自前の列挙（中身は同じ）。(3) 読み込みの節は、コントローラーがないスロットでも起動する。

## Step 一覧
### 32-A プログラム起動
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 32-A0 | 調査: 2 つの経路が動く条件（`startUp`、`context.LaunchProgram`）を、呼び出し元までたどって表にする。二重起動の有無を確かめる | — | 完了 |
| 32-A1 | 起動の部品を第 2 層に作る（プロセスの確認は `IProcessInspector`、起動は `IProcessLauncher`、待ちは `TimeProvider`）。`DeviceConnectionSetup` はそれを呼ぶ | 偽のプロセスの確認・起動で、「動いていれば起動しない」「5 秒後に 1 回だけ起動する」 | 完了 |
| 32-A2 | `ProfileMiscLoadSection` から、プロセスの列挙と起動を取り除く。読み込みの呼び出し側（`context.LaunchProgram` を真にしていた場所）が、読み込みのあとに 32-A1 の部品を呼ぶ | 読み込みのテストが、プロセスに触れないこと。保存の黄金テストが不変 | 完了 |
| 32-A3 | `BackingStore.ProcessInspector`（プロパティでの後付けの配線）が不要になるかを確かめ、不要なら `ServiceRegistration.Wiring.cs` から外す（一覧のコメントも更新） | 配線のテスト | 完了 |

### 32-B 出力の種類
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 32-B0 | 調査: 上の各場所が、種類の何（値か、処理か）で分岐しているかを表にする。`OutContTypeInfo` の今の項目を確かめる | — | 未着手 |
| 32-B1 | 値の分岐を `OutContTypeInfo` へ: IPC の名前（`IpcCommandProcessor`）、ボタン名の表（`X360ControlTables`、`MacroParser`）、既定の種類（`OutputSettingsViewModel`、`BackingStore.DEFAULT_OUT_CONT_TYPE`） | 全種類に項目があることの型の検査。IPC・マクロの表示名のテストが不変 | 未着手 |
| 32-B2 | 処理の分岐をカートリッジへ: 送出の直前の後処理（D4）、表示用の XInput の番号（`OutputSlotView`）、スロットの差し込み（`OutputSlotManager`） | 仮想パッドの送出のテストが不変。割り当て 0 | 未着手 |
| 32-B3 | （D3 が A のとき）`BindingWindowViewModel.use360Mode`、`SlotDeviceEntry` を、種類から表示を引く形にする | ViewModel のテスト | 未着手 |
| 32-B4 | 検査: カートリッジ・`OutContTypeInfo`・既定値の定義のほかに、`OutContType.X360`／`.DS4` を名指しする場所がないことを、型の検査（ArchUnitNET）で守る。残す場所（`VirtualPadService` の起動時の一時的な差し込みなど）は、理由つきで許可リストに入れる | 新規 | 未着手 |
| 32-B5 | 文書: `02-Extension-Guide.md` §2 を、実際の手順に書き直す。規則 3.1 を更新。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ | — | 未着手 |

### 32-A の実施の記録（2026-11-14）
- 新設: `IProfileProgramLauncher`（`Processing/Abstractions`）、`ProfileProgramLauncher`（`Processing/Services`。`IProcessInspector`・`IProcessLauncher`・`TimeProvider` を受ける。5 秒の待ちと「動いていれば起動しない」は従来どおり）。`IProcessLauncher` に既定実装つきの `LaunchWithWorkingDirectory` を足した（実行ファイルのあるフォルダーを作業フォルダーにする従来の動作。既存の差し替え部品を壊さないため既定実装にした）。
- 起動の呼び出し: (1) 接続時＝`DeviceConnectionSetup.ApplyProfileOptions(startUp: false)`（従来どおり）。(2) 適用の完了のあと＝`ProfileApplicationService.ApplyCore` が、起動の指定が真で成功したときに `IProfileLoadTarget.LaunchProfileProgram` を呼ぶ（コンストラクターの引数を 12 に保つため、`IProfileLoadTarget` 経由にした）。(3) IPC の `LoadTempProfile`＝読み込みの成功後に同じ窓口を呼ぶ。(4) メイン画面のコンボ＝`IProfileSlotApplier.ApplyToSlot(…, launchProgram: true)`（承認した変更点）。
- 取り除いたもの: `ProfileMiscLoadSection` のプロセスの列挙と起動、`ProfileLoadContext.LaunchProgram`、`LoadProfile`／`LoadProfileXml`／`LoadTempProfile` の `launchProgram` 引数、`DeviceConnectionSetup.CheckLauchProfileOption`、`BackingStore.ProcessInspector` とその後付けの配線（`ServiceRegistration.Wiring.cs` の一覧も更新）。
- 残した負債: プリセットの読み込み（`ProfilePresets`／`IProfilePresetLoader` の 10 メソッド）の未使用の `launchProgram` 引数は、今回の範囲外のため残した（以前から使われていない）。
- テスト: `ProfileProgramLauncherTests`（待ち・動いている・失敗の握りつぶし）、`ProfileApplicationLaunchTests`（起動の指定と成功のときだけ呼ぶ）を新設。

## 確認の方法
- 実機: (A) 「起動するプログラム」を設定したプロファイルを、接続時・手動の切替・自動プロファイルで適用して、起動が 1 回だけ起きること。すでに動いているときは起動しないこと。(B) 出力の種類の切替（Xbox 360 ⇄ DS4）、DS4 型でのトリガー、CLI の `-command` での差し込み、出力ピッカーの表示、マクロの表示名。
- 未確認の項目は `Known-Issues-Ledger.md` §3 へ足す。

## リスク
- A: 中。起動の時機が変わると、利用者に見える（起動しない／2 回起動する）。32-A0 の表を先に固める。
- B: 中。毎フレームの経路を 1 か所触る。DS4 型のトリガーの動作は実機で確かめる。

## 履歴
- 2026-11-13: 決めること（D 項目）のユーザーの回答を「決定の記録」に記録した。
- 2026-11-13: Phase 番号の付け替え（旧 Phase31 → Phase32。進める順に合わせた）。本文中の Step 番号も付け替えた。
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-11: 追加調査の結果を反映。プログラム起動の 2 つの経路は場面で分かれ、二重起動はない見込みと分かった（現状の A と、決めること D2 を書き直した）。32-A0 は、その表を確定する作業として残す。
