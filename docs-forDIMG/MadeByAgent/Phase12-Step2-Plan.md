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

## 5c. 進捗

- 2a 完了（§5b）。2b-1 完了（`WindowTargetMatcher`。コミット 5dd6fd00）。
- 2b-2 完了（2026-11-03）: `IWindowFocuser`／`IWindowSystem`／`WindowFocusResult`／`WindowCandidate`（`DS4Windows/DI/`）、`WindowFocuser`／`Win32WindowSystem`（`DS4Control/Services/`）、`ServiceRegistration` に登録。`WindowTargetMatcher.IsEmpty` を追加。単体テスト 15 件（偽の `IWindowSystem` による手順の検査と、Win32 の列挙の煙テスト）。Debug は警告 0・エラー 0、全 2,854 件合格。
- 2c 完了（2026-11-03）: 新しい種別 `ActionTypeId.FocusWindow`（enum の末尾に追加）。モデルと保存・読込まで（実行と画面は 2d・2e）。
  - SA（Actions.xml）: `Type=FocusWindow`、`Details`＝プログラムのパス、新要素 `WindowTitle`＝ウィンドウタイトル（空なら書かない）、`Delay`。`SpecialAction` のコンストラクターでは、タイトルを「トリガー解除」のボタン名として読まない（Program 以外の種別の既存の解釈から除外）。`SaveActionNew` の mode 11。
  - Controls（プロファイル XML）: `ControlSpecialItem` に `WindowPath`／`WindowTitle`、`Type=FocusWindow`。
  - 古い版との互換: 古い版は、未知の種別として読み飛ばす（SA は一覧に入らず、Controls はそのボタンが未割り当てのまま）。**古い版で開いて保存し直すと、その割り当ては消える**（既存の読込の仕様。R-2 の `MacroExecuteMode` と違い、種別そのものが未知のため、劣化した形では残らない）。
  - 単体テスト 8 件。Debug は警告 0・エラー 0、全 2,862 件合格。
  - 未接続（2d・2e で接続）: `ControlsSpecialTypeTable`（Controls で実行時に束縛を作る種別の表）、`SpecialActionOutputFactory`（実行）、`ActionTypeCatalog`／`AssignmentEditor`／`AssignmentDescriber`／`MappedControl`（画面と表示名）。
- 2d 完了（2026-11-03）: 実行まで接続。
  - `FocusWindowAction`（`DS4Windows/Actions/`）: 立ち上がりの 1 回。対象は `sa.details`（パス）と `sa.extra`（タイトル）から `WindowTargetMatcher` を作る。前面化は `Task.Run` の別スレッドで行い、入力処理を止めない。前の前面化が終わっていないうちの再実行は捨てる（連打・押しっぱなしで並ばない）。例外は握りつぶして警告ログ。
  - `SpecialActionOutputFactory`: `IWindowFocuser`（省略可）を受け、`FocusWindow` の束縛を作る。実行方針は既定（Fresh-press ガードあり、実行中のブロックなし、離したときの Stop あり）。窓口が未接続なら束縛を作らない。SA と Controls の両方（Controls は、既存の Controls の方針）。
  - `ControlsSpecialTypeTable`: `FocusWindow` を追加。`ControlBindingBuilder.TryInterpretSpecial`: パスもタイトルも空の Controls の割り当ては、束縛を作らない（どのウィンドウにも一致してしまうため）。
  - 単体テスト 15 件。Debug は警告 0・エラー 0、全 2,877 件合格。
  - 未接続（2e）: `ActionTypeCatalog`／`AssignmentEditor`／`AssignmentDescriber`／`MappedControl`（画面と表示名）。
- 2e 完了（2026-11-03）: 編集画面まで接続。
  - 新規: `WindowTargetEditor`（Path・Title の依存関係プロパティ、参照ボタン。ツールチップは Auto-Profile の `AutoProfPathTip`／`AutoProfTitleTip` を再利用）、`FocusWindowViewModel`（モード 11 で保存。パスもタイトルも空は保存を止め、両方の欄に赤い枠）。
  - 変更: `ActionTypeCatalog` に添字 9 の FocusWindow を追加（Default は 10、Unbound は 11 に移る）、`AssignmentEditor`（XAML の項目とタブ、コードビハインド）、`AssignmentDescriber`（"Focus Window: パス / タイトル"）、`MappedControl`、翻訳（`SpecialActionEdit.FocusWindow`、`SpecialActionEdit.FocusWindowPath`。英語・日本語）。
  - 既存テストの添字の更新: 種別の一覧の件数と、Default／Unbound の添字（9→10、10→11）に依存していた 8 件。
  - 検証: Debug ALL（警告 0・エラー 0）、DS4WindowsTests 2,888 件（+11）と StandaloneTests 11 件がすべて合格。画面の目視は 2f の実機確認で行う。
- 実機でのフォーカスの確認は、2d（実行）と 2e（画面）が揃ったため、2f で行う。
- 2f 完了（2026-11-03、DS4 実機）: 編集画面（種別の追加、入力欄、両方空で保存停止）、SA での前面化（パス指定・ウインドウ名指定）、最小化からの復元、対象なしで落ちず警告ログ、Controls での割り当て・実行・再起動後の保持・説明文の表示。すべて想定どおり。
- 2h 実装済み（2026-11-03。実機確認の前）: `WindowTitleMatchMode`（列挙）、`WindowTargetText`（印と一致方法の相互変換、実行ファイル名のみへの変換）、`FocusWindowViewModel`（`TitleText`／`TitleMatchMode` を追加し、`WindowTitle` は組み立てた保存用の文字列）、`WindowTargetEditor`（一覧とボタン）、翻訳 5 件。検証: Debug ALL 警告 0・エラー 0、DS4WindowsTests 2,930 件（+42）と StandaloneTests 11 件が合格。
- 2h 完了（2026-11-03、DS4 実機で確認し、コミット済み）。
- 2i 実装済み・DS4 実機で確認済み（2026-11-03。マークの見た目、ドロップでの取得、自分自身・背景の除外、取得した値での前面化、保存後の再表示。すべて想定どおり）: `IWindowSystem.GetWindowUnderCursor()`（`GetCursorPos`→`WindowFromPoint`→`GetAncestor(GA_ROOT)`。戻りは `CapturedWindow`＝元の大文字小文字のフルパス・タイトル・プロセス ID）、`WindowTargetPicker`（自分自身のウィンドウは除外）、`WindowTargetEditor` にターゲットマーク（ウインドウ名のラベルの右。マウスキャプチャ＋カーソル変更）。`AssignmentEditor`／`EditorWindowFactory` は、`IWindowSystem` を必須引数で受ける。ドロップ後は一致方法を完全一致に戻し、フルパスとタイトル全文を入れる。既知の制限: タイトルが `^` で始まる・`$` で終わる・`*` で始まる場合、入力欄の「印は取り除く」規則（2h）で先頭／末尾の 1 文字が落ちる。検証: Debug ALL 警告 0・エラー 0、DS4WindowsTests 2,938 件（+8）と StandaloneTests 11 件が合格。

## 6. 完了条件

- 仕様が承認されている。
- Controls と SA の両方で、保存・読込・実行・編集ができる。
- 古い版との互換を確認している。
- `Debug ALL` が警告 0・エラー 0、`dotnet test` がグリーン。
- 実機確認（DS4 のみ）が済んでいる。

## 7. 追加要望（2026-11-03。2f の実機確認のあと。2h 一致方法の一覧、2i ドラッグ＆ドロップ、2j 中央へマウス移動）

### 7.1 2h: ウインドウ名の一致方法を一覧で選ぶ、実行ファイル名のみで判定するボタン
- ウインドウ名の入力欄の**下**に、一致方法の一覧（ComboBox）を置く: 前方一致・後方一致・部分一致・完全一致。**初期値は完全一致**。
- 保存の形式は変えない。内部では従来どおり、文字列の印で表す（前方＝先頭に `^`、後方＝末尾に `$`、部分＝先頭に `*`、完全＝印なし。`WindowTargetMatcher` の規則）。画面では、入力欄に印を除いた文字列だけを出し、一覧に方法を出す。ViewModel が、一覧の選択と入力欄から保存用の文字列を組み立て、読み込み時は印を分解して一覧と入力欄へ戻す（既存の割り当ても、そのまま開ける）。
- 手で `^` などを入力欄に打った場合は、一覧の選択を優先する（印は、一覧の方法で付け替わる）。
- **「実行ファイル名のみで判定」ボタン**: 「Program path」のラベルの右あたりに置く。押すと、パス欄の値からフォルダ部分を取り除き、実行ファイル名だけで判定する形に変換する。
  - 変換: 先頭の `^`・`*`、末尾の `$` の印を外し、最後の `\` か `/` より後ろを取り出して、`\ファイル名$`（後方一致。例: `C:\Windows\notepad.exe` → `\notepad.exe$`）にする。先頭に `\` を付けるのは、`xnotepad.exe` のような別のファイルに一致しないため。
  - 既にファイル名だけ（フォルダ部分なし）のときは `\ファイル名$` にそろえる。パス欄が空のときは何もしない（ボタンは無効にする）。
  - 変換は欄の値を書き換えるだけ（保存の形式は変えない）。元に戻す操作は付けない（手で入力し直せる）。
  - 変換は静的メソッドにして単体テストする（フルパス、`/` 区切り、印つき、フォルダなし、空、末尾が `\` のパス）。
- 範囲: タイトルの一致方法の一覧と上のボタン。パス欄の一致方法の一覧は付けない（印を手で入力）。Auto-Profile 画面は変えない（2g で判断）。
- 追加する型: 一致方法の列挙 `WindowTitleMatchMode`（1 ファイル 1 型）。分解と組み立ては、静的メソッドで単体テストする（印あり・なし、空文字、印だけの文字列の扱い）。

### 7.2 2i: ターゲットマークのドラッグ＆ドロップで対象を取得する
- 割り当て編集画面の「ウインドウを前面に出す」のタブ（`WindowTargetEditor`）の空き場所に、正方形のターゲットマークを置く。
- マークをマウスでドラッグし、他のウィンドウの上で離すと、そのウィンドウの**実行ファイルのフルパス**と**ウィンドウタイトル全文**を、パス欄・ウインドウ名欄へ入れる（承認済み: フルパス＋タイトル全文）。タイトルの一致方法は完全一致に戻す（2h の一覧）。入れた後は手で編集でき、一覧で方法を変えられる。
- 取得の方法（Spy++・AutoHotkey Window Spy 方式）: マウスのキャプチャを取り、ドラッグ中はカーソルをターゲットの形にする。離した位置のウィンドウを `WindowFromPoint` → 最上位の親（`GetAncestor(GA_ROOT)`）→ パス（`QueryFullProcessImageName`）とタイトル（`GetWindowText`）。
- 部品化: `IWindowSystem` に `GetWindowAt(point)`（`WindowCandidate` を返す）を足し、`Win32WindowSystem` で実装。`WindowTargetEditor` は取得を `IWindowSystem` 越しに受ける（ViewModel ではなく、画面の部品が呼ぶため、`AssignmentEditor` から注入する）。
- DS4Windows 自身のウィンドウの上で離したときは、取得しない（自分を対象にしないため）。タイトルが空のウィンドウは、パスだけ入れる。
- テスト: `GetWindowAt` の結果から欄へ入れる処理（フルパス＋タイトル全文、自分のウィンドウは除外、タイトル空）を、偽の `IWindowSystem` で検査する。マウス操作そのものは実機で確認する。

### 7.3 2j: フォーカスのあと、ウィンドウの中央へマウスポインターを移動する（チェックボックス）
- 編集画面にチェックボックス「フォーカスしたあと、ウィンドウの中央へマウスポインターを移動する」を足す。既定は off（既存の割り当ての動作は変えない）。
- 動作: 前面化が成功（`Focused`）したときだけ、そのウィンドウの外枠の中央へ `SetCursorPos` で移動する（最小化から復元した場合は、復元後の位置）。見つからない・失敗のときは動かさない。
- 保存: SA は Actions.xml に `<CenterCursor>true</CenterCursor>`（true のときだけ書く）、Controls は項目の属性 `CenterCursor`（ControlSpecialItem に `CenterCursor`）。古い版は未知の要素を読み飛ばすため、フォーカスだけが動く（移動は無視される）。
- 実行: `IWindowFocuser.Focus` に移動の指定を渡す（`WindowFocusOptions` 相当）か、`IWindowSystem.MoveCursorToCenter(handle)` を足して `WindowFocuser` が呼ぶ。複数モニター・DPI は、画面座標（仮想スクリーン）のまま `GetWindowRect` の中央を使うため、追加の変換は要らない。
- 範囲外: ウィンドウのクライアント領域の中央（タイトルバーを除く）への移動、マウスのクリックは行わない。必要になれば別の要望とする。
- テスト: 偽の `IWindowSystem` で、成功時のみ移動すること・off では移動しないこと・保存と読込の往復（SA と Controls）・古い版が読み飛ばすことを検査する。

### 7.4 進め方
- 2h → 2i → 2j の順（2j は保存の形式を変えるため、最後に単独で）。それぞれ Debug ALL とテストをグリーンにし、実機確認（DS4）を行ってからコミットする。
- 規約: 新しい型は 1 ファイル 1 型、ログは `Log-Policy.md` に沿う（移動の実行は Debug ログ 1 件）。
