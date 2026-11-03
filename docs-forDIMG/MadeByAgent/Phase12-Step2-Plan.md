# Phase12-Step2 個別計画書: R-4（プログラムのフォーカス）

作成日: 2026-11-03
状態: **承認待ち（案）。2a の仕様の決定後に着手する。規約 §4**
関連: `Release-Backlog.md` R-4、[Issue #3](https://github.com/gwin7ok/DS4Windows/issues/3)（本文は空。タイトルのみ）、`Log-Policy.md`
ブランチ: `Phase12`

## 1. 目的

割り当て（SA、Controls）に、指定したプログラム（ウィンドウ）を前面へ出す（フォーカスする）機能を追加する。

## 2. ログの運用（Step1 の方針より）

- 新しいコードのログは `Log-Policy.md` に沿って書く（レベル、`[機能名]` 接頭辞、英語と `key=value`、文字列を組み立てるログはガード）。
- 汎用 `Log(level, ...)` は 13b まで存在しないため、呼び出しは旧 API（レベル別メソッド）で書く。13b で機械的に置き換える。

## 3. 現状の調査

- 既存の Program 種別（`LaunchProcessAction`）は起動専用。`IProcessLauncher` は `Process.Start` を呼ぶだけで、前面化の処理はない。
- `DefaultProcessInspector` は `GetForegroundWindow` で前面ウィンドウを取得するだけで、前面へ出す処理（`SetForegroundWindow`）はない。
- 新しい `ActionTypeId` を足すときの変更箇所（9 か所）: `SpecialAction.cs`（enum、文字列からの読込）、`ActionTypeCatalog`、`AssignmentEditor`（編集用 ViewModel の対応）、`SpecialActionItemMapper`、`SpecialActionOutputFactory`、`ControlsSpecialTypeTable`、`ActionsDTO`／`SpecialActionsXmlSerializer`、`ControlSpecialItemXml`、`AssignmentDescriber`／`MappedControl`。
- `ActionTypeId.Program` に触れるテストは 16 ファイル。網羅的に種別を検査するテストは更新が要る。
- SA の XML は `Type` 文字列に種別名を持つ。古い版が新しい種別を読むときの挙動を確認する（R-2 の `MacroExecuteMode` が前例）。

## 4. Step の構成

| Step | 内容 | コード変更 |
|---|---|---|
| 2a | 仕様の決定（§5。済み）と、OSS の実装の比較（規約 §4: AutoHotkey の `WinActivate`、`AttachThreadInput` や Alt キーの擬似入力による前面化の制限回避など） | なし |
| 2b-1 | 照合の部品 `WindowTargetMatcher` を `AutoProfileEntity` から取り出す。`AutoProfileEntity.IsMatch` は委譲する。旧実装との差分テスト | あり |
| 2b-2 | 前面化の部品。`IWindowFocuser` を `DS4Windows/DI/`、実装を `DS4Control/Services/` に置く（規約 R1・R2）。ウィンドウの列挙、照合、最小化の復元、前面化。単体テスト付き | あり |
| 2c | 種別の追加（enum、保存、読込）。Controls と SA の両方で保存できる。古い版との互換 | あり |
| 2d | 実行（`FocusWindowAction`） | あり |
| 2e | 編集画面。設定の部品 `WindowTargetEditor`（Path・Title・参照）と、新種別の ViewModel、XAML、翻訳リソース（日本語と英語） | あり |
| 2f | 実機確認の計画。前面化は Windows の制限があるため、確認が多い | なし |
| 2g（任意） | Auto-Profile 画面を `WindowTargetEditor` へ置き換えるか判断。置き換える場合は、見た目と動作が変わらないことを確認する | 判断次第 |

各 Step で `dotnet test` を常にグリーンにする。`Debug ALL` は警告 0・エラー 0 まで私が実行する。

## 5. 仕様の決定事項（2a）

決定済み（2026-11-03）:

1. **対象の指定方法 ＝ Auto-Profile 機能と同じ特定方法・設定方法を部品化して使う**（ユーザー指定）。Auto-Profile の現行の仕様は、次のとおり。
   - 指定は `Path`（実行ファイルのフルパス。小文字化・`/`→`\` 正規化）と `Title`（ウィンドウタイトル。小文字化）。どちらも省略可。両方指定なら AND。
   - 一致の方式は、完全一致、前方一致（先頭に `^`）、後方一致（末尾に `$`）、部分一致（先頭に `*`）。
   - 設定は、Path と Title のテキスト入力（ツールチップ付き）と、プログラムの参照（ファイル選択、フォルダから追加）。
   - 現状の実装は `AutoProfileEntity.IsMatch`（前面ウィンドウ 1 つとの照合）と `AutoProfiles.xaml` の入力欄に埋まっており、部品になっていない。
2. 対象のウィンドウが見つからない ＝ 警告ログのみ（Warn。何もしない）。
3. 最小化されている ＝ 復元して前面化する。
4. 複数のウィンドウが見つかった ＝ 見つかった最初の 1 つ。
5. 管理者権限のウィンドウ ＝ 非対応を許容（前面化できなければ警告ログのみ）。
6. **新しい種別（`ActionTypeId`）を足す**。既存の Program は変えない。

### 1 の部品化の設計（案）

- **`WindowTargetMatcher`（照合の部品）**: `AutoProfileEntity` の `Path`／`Title` の設定と `IsMatch` の規則を、そのまま取り出した型。`AutoProfileEntity.IsMatch` は、この部品へ委譲する薄い形にする。**既存の Auto-Profile の動作を変えない**（規約 §2.2）ことを、差分テスト（旧実装と新部品で、同じ入力に同じ結果）で確認する。
- **ウィンドウの探索**: Auto-Profile は前面ウィンドウ 1 つとの照合だが、フォーカスは全ウィンドウから探す必要がある。`IWindowFocuser` が、トップレベルのウィンドウを列挙し（`EnumWindows`、可視のもの）、各ウィンドウのパスとタイトルを `WindowTargetMatcher` で照合する。
- **設定画面の部品（`WindowTargetEditor`）**: Path・Title の入力欄、ツールチップ、プログラムの参照を持つ UserControl を新設する。割り当て編集画面（新種別）が使う。**Auto-Profile 画面を同じ部品へ置き換えるかは、別 Step（2g）として、リスクを見て判断**する（既存の画面を触るため）。

## 5b. 2a の結果: OSS の前面化の手法の比較（2026-11-03）

調べたもの: AutoHotkey の `SetForegroundWindowEx`（`source/window.cpp`。参照: [AutoHotkey/AutoHotkey](https://github.com/AutoHotkey/AutoHotkey/blob/alpha/source/window.cpp)）、CodeProject の ["How to Bring Window to Top with SetForegroundWindow"](https://www.codeproject.com/Tips/76427/How-to-Bring-Window-to-Top-with-SetForegroundWindo)、Stack Overflow の関連質問。

| 手法 | 内容 | 評価 |
|---|---|---|
| 素の `SetForegroundWindow` | Windows の制限（前面のプロセスである、最後の入力を受けた、など）に合わないと失敗する | 最初に 1 回試す |
| 最小化の復元 | 最小化なら `ShowWindow(SW_RESTORE)` を先に。復元しないと前面化できない | 必須（仕様 3 と一致） |
| `AttachThreadInput` | 自スレッドを前面スレッドと対象スレッドの入力へ結び、前面化の後に外す | AutoHotkey が採用。段階的な第 2 手 |
| Alt キーの擬似入力 | `keybd_event(VK_MENU)` の押下・解放。Alt を押した直後は前面化が許可される仕組みを利用 | AutoHotkey が採用。最後の手段 |
| `AllowSetForegroundWindow(ASFW_ANY)`＋前面化ロックの時間の変更 | CodeProject の例。全体の設定を一時的に変えるうえ、ハングの報告がある | **採用しない**（副作用が大きい） |

**AutoHotkey の算法（採用する形）**: ①対象が NULL、または別スレッドで応答なし（`IsHungAppWindow`）なら失敗 → ②最小化なら復元 → ③すでに前面なら成功 → ④素の前面化を 1 回（短い待ちの後、前面になったか確認）→ ⑤`AttachThreadInput` で結び、最大 5 回試す（初回だけ Alt-up を 1 回送る）→ ⑥それでも失敗なら Alt の押下・解放を 2 回送って再度試す → ⑦結びを外す → ⑧成功なら `BringWindowToTop`。成功の判定は、前面のウィンドウが対象、または対象が所有するウィンドウであること。

**採用する設計**:
- `IWindowFocuser` は、上記 ①〜⑧ を C# で実装する。各手の間に確認と短い待ちを入れ、失敗は警告ログ（Warn）にする。
- 待ちはブロックさせない設計にする（実行は専用のワーカー上で行い、入力ループを止めない）。
- ウィンドウの列挙は `EnumWindows` を使い、可視で、タイトルを持ち、cloaked（`DwmGetWindowAttribute`）でないトップレベルのウィンドウを対象にする。パスは、`DefaultProcessInspector` と同じ方法（プロセス ID → 実行ファイルのフルパス）で求める。
- 注意: Alt の擬似入力は、前面のアプリのメニューを一瞬アクティブにすることがある。最後の手段に限る。管理者権限のウィンドウは、UIPI により擬似入力も効かないため、失敗は警告ログのみで終える（仕様 5）。

## 6. 完了条件

- 仕様が承認されている。
- Controls と SA の両方で、保存・読込・実行・編集ができる。
- 古い版との互換を確認している。
- `Debug ALL` が警告 0・エラー 0、`dotnet test` がグリーン。
- 実機確認（DS4 のみ）が済んでいる。
