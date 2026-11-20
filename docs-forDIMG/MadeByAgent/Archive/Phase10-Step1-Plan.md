# Phase10-Step1 計画書: マクロの再生オプションの共通部品と、Press Key の Scan Code の移動

作成日: 2026-10-21  
改訂日: 2026-10-21（ユーザーの指示［Execute Mode の追加、記録画面からの削除、Scan Code の移動］を反映）  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase10-Assignment-UI-Unification-Plan.md`（§5 Step1、§6 決定事項）、`Phase8-Fix-MacroRepeatUnify-Plan.md`  
状態: **完了（2026-10-21。W1〜W5。ビルド・テスト成功、実機確認済み）。進捗は `Phase10-Status.md`**

---

## 1. 目的と範囲

Controls と SA で、**オプションの置き場所と表現を揃え**、記録の画面（`RecordBox`）や選択ダイアログ（`BindingWindow`）から、重複するオプションを減らす。後の Step2（共通の編集画面）が、そのまま使える部品にする。

- **範囲**:
  1. SA の Macro タブに「**Execute Mode**」ドロップダウン（Play Once／Repeat While Held）を追加する（Press Key の「Switch Mode」と同じ形）。SA の「Repeat While Held」チェックボックスは、これに置き換える。
  2. SA の Macro タブの「**Scan Code**」チェックボックスは、そのまま残す。SA から開く記録画面（`RecordBox`）からは、繰り返し（ドロップダウン）と Scan Code を**消す**。
  3. SA の Press Key タブに「**Scan Code**」チェックボックスを足す。SA から開く選択ダイアログ（`BindingWindow` の `ForPressToggle`）からは、**Scan Code と Toggle のチェックボックスを消す**（Toggle は、すでに Press Key の「Switch Mode」にある）。
- **範囲外**: Run on release、Sync、Keep key state を Controls に出すこと（保存形式の拡張が要るため **Step3**）。保存形式・実行・ログは変えない。
- **Controls 側は、Step1 では変えない**（§4）。

## 2. 現状の調査（2026-10-21）

| | Controls | SA |
|---|---|---|
| マクロの記録 | `BindingWindow`（Full）の「Record A Macro」→ `RecordBox` | Macro タブの「Record macro」→ `RecordBoxWindow` → `RecordBox` |
| マクロの繰り返し | `RecordBox` 内のドロップダウン | Macro タブのチェックボックス。**加えて**、開いた `RecordBox` にも同じドロップダウンが出る（二重。保存後に `SpecialActionEditor.xaml.cs` 448〜452 行で手で書き戻している） |
| マクロの Scan Code | `RecordBox` 内のチェックボックス | Macro タブのチェックボックス。**加えて**、開いた `RecordBox` にも出る（二重） |
| キー・ボタンの Toggle／Scan Code | `BindingWindow`（Full）のチェックボックス（Toggle、Scan Code）が**唯一の場所** | Press Key タブの「Switch Mode」（Press／Toggle）が Toggle を担う。Scan Code は、選択ダイアログ（`ForPressToggle`）のチェックボックスで設定する。**Toggle もダイアログに出る（二重）** |

Press Key の値の流れ: `PrepareSettings` → `BindingWindow`（`OutBinding.WriteBind` が、`keyType` の Toggle／ScanCode を、`OutBinding` の値から書く）→ `ReadSettings`。ダイアログでチェックボックスを触らなければ、値は、そのまま通過する。したがって、**チェックボックスを Press Key タブに移しても、`PrepareSettings` でタブの値を `settings.keyType` に入れ、`ReadSettings` で読み戻せば、ダイアログは値を通過させるだけ**になる。

## 3. 設計

### 3.1 部品（マクロ）

- **`MacroPlaybackOptions`**（新規。`DS4Forms/ViewModels/`。ViewModel）: `RepeatWhileHeld`（bool）、`UseScanCode`（bool）、`Repeatable`（bool。繰り返しを選べるか）、`PlayModeIndex`（0＝Play Once、1＝Repeat While Held。`RepeatWhileHeld` との相互変換）。`INotifyPropertyChanged`。
  - `FromKeyType(DS4KeyType, bool repeatable = true)`、`LoadFrom(DS4KeyType)`、`ApplyTo(DS4KeyType)`（値を返す。`ref` ではない）: `RepeatMacro`、`ScanCode` のビットだけを操作する（他のビットは触らない）。
  - `Repeatable=false` は、画面の有効・無効だけに使い、`RepeatWhileHeld` の値は**消さない**（従来の `RecordBox` は、ドロップダウンを無効にしても、読み込んだ `RepeatMacro` を保存時に書き戻していたため。規約 2.2。W1 で、当初の「偽に固定」から変更）。
- **`MacroPlaybackOptionsPanel`**（新規。`DS4Forms/`。UserControl）: 「Execute Mode:」ラベル＋ドロップダウン（`RecordBox.PlayOnce`／`RecordBox.RepeatWhileHeld`）と、「Scan Code」チェックボックス。`MacroPlaybackOptions` を `DataContext` に受ける。`Repeatable=false` のとき、ドロップダウンを無効にする。

### 3.2 適用先（マクロ）

- **SA の Macro タブ**: 既存の 2 つのチェックボックスを `MacroPlaybackOptionsPanel` に置き換える。`MacroViewModel` が `MacroPlaybackOptions` を持つ（`UseScanCode`／`RepeatHeld` の個別のプロパティは、`MacroPlaybackOptions` へ委譲または置き換え）。
- **SA から開く `RecordBox`**: `ShowPlaybackOptions=false` で、繰り返しと Scan Code を出さない。`RecordBoxWindow.Saved` のあとの手の書き戻し（448〜452 行）を撤去する。
- **Controls（`RecordBox`）**: 既存の `macroModeCombo` と `useScanCode` を、`MacroPlaybackOptionsPanel` に置き換える（見た目はほぼ同じ。ラベル「Execute Mode:」が付く）。`ShowPlaybackOptions` の既定は `true`。

### 3.3 Press Key の Scan Code

- `PressKeyViewModel` に `UseScanCode`（bool）を足す。`LoadAction`、`PrepareSettings`、`ReadSettings`、保存（`keyType` の ScanCode ビット）に反映する。
- Press Key タブに「Scan Code」チェックボックスを足す（Switch Mode の下）。
- `BindingWindow` の `ForPressToggle` では、Toggle と Scan Code のチェックボックス（`topOptsPanel`）を隠す。`Full`（Controls）では、Step1 では残す（§4）。

## 4. 変えないもの

- 保存形式（`Profile.xml` の `RepeatMacro`／`ScanCode`、`Actions.xml` の `Repeat`／`Scan Code`）、実行、ログ。
- **Controls の画面**: 選択ダイアログ（`Full`）の「Record A Macro」「Toggle」「Scan Code」は、Step1 では残す。Controls では、現状これらが唯一の設定場所で、共通の編集画面（Step2）ができる前に消すと、設定できなくなる。削除は Step4（`Phase10-Assignment-UI-Unification-Plan.md` §5）。
- `RecordBox` の記録・編集・プリセット・Extras（振動・ライトバー）・マウス 4／5 ボタンの機能。
- SA の他の項目（Run on release、Sync、Keep key state、Hold 秒数など）。

## 5. 作業の順序（Wave）

各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する。

1. **W1: `MacroPlaybackOptions` と単体テスト**（画面は変えない。**実装済み、ビルド・テスト待ち**。ファイル: `DS4Forms/ViewModels/MacroPlaybackOptions.cs`、`DS4WindowsTests/Phase10Step1W1MacroPlaybackOptionsTests.cs`）
   - テスト: `FromKeyType`／`ApplyTo` の往復（`RepeatMacro`、`ScanCode`。他のビット［`Macro`、`Toggle`、`Unbound` など］を保つ）、`PlayModeIndex`↔`RepeatWhileHeld`、`Repeatable=false` でも読み込んだ `RepeatWhileHeld` を保つ、`PropertyChanged` の通知。
2. **W2: `MacroPlaybackOptionsPanel` と、Controls（`RecordBox`）への適用**（**完了（ビルド・テスト成功、実機確認済み。2026-10-21）**。新規: `DS4Forms/MacroPlaybackOptionsPanel.cs`（XAML なし。コードだけ。§補足）、`DS4WindowsTests/Phase10Step1W2RecordBoxPlaybackOptionsTests.cs`。変更: `RecordBox.xaml(.cs)`、`RecordBoxViewModel.cs`。パネルは、`AllowModeEdit`［記録中の一時無効］と `ShowScanCode`［従来の showscan］を持つ）
   - `RecordBoxViewModel` の `MacroModeIndex`／`UseScanCode` を `MacroPlaybackOptions` に置き換える。
   - テスト: 読み込みで `keyType` の `RepeatMacro`／`ScanCode` を復元、`ExportMacro` が `RepeatMacro` だけを立てる、既存の `RecordBoxViewModelTargetDeviceTests`・`Phase8FixMacroRepeatUnifyPersistenceTests` が成功。
   - 実機（画面）の確認: Controls のマクロで、Execute Mode／Scan Code の設定・保存・再読み込みが従来どおり。
   - **実機確認で見つかった、以前からの不具合（2026-10-21）**: Controls で、記録画面の Scan Code にチェックを入れて保存しても、開き直すと外れていた。原因は、`BindingWindowViewModel.PrepareSaveMacro` が `macroType` だけを `OutBinding` に渡し、`WriteBind`（Macro）が見る `hasScanCode` を渡していなかったこと（繰り返しは `macroType` から書かれるため、保たれていた）。`PrepareSaveMacro` で `hasScanCode` も渡すように是正し、`Phase10Step1W2BindingWindowMacroScanCodeTests` で固定した（W2 の変更が原因ではない）。
   - **画面の配置は過渡的**: Controls の `RecordBox` に Execute Mode と Scan Code を置くのは、共通の編集画面（Step2）ができるまでの暫定。Step4 で、これらは共通の編集画面の Macro タブへ移り、`RecordBox` は記録専用になる（`BindingWindow` の「Record A Macro」も、このとき削除する）。
3. **W3: SA の Macro タブと、SA から開く `RecordBox`**（**完了（ビルド・テスト成功、実機確認済み。2026-10-21）**。新規テスト: `Phase10Step1W3MacroViewModelPlaybackOptionsTests`。変更: `SpecialActionEditor.xaml(.cs)`、`MacroViewModel.cs`、`RecordBox.xaml.cs`［`showPlaybackOptions`］、`RecordBoxWindow.xaml.cs`、`IEditorWindowFactory`／`EditorWindowFactory`［`showPlaybackOptions`］。パネルに `ScanCodeAlignment` を追加。SA から開く記録画面は 4 か所［Macro と、MultiAction の Tap／Hold／DoubleTap］すべてでオプションを出さない。MultiAction の記録画面の Scan Code は、保存に使われず［`actionMacro` だけを読む］効果がなかったため、消えても影響はない）
   - Macro タブに Execute Mode を追加（チェックボックスの置き換え）。`RecordBox` の `ShowPlaybackOptions=false`。手の書き戻しを撤去。
   - テスト: `MacroViewModel.LoadAction`／`PrepareSettings`／`SaveAction` の往復（`Repeat`、`Scan Code`、他の extras を保つ）。
   - 実機（画面）の確認: SA の Macro の新規作成・編集・保存・再読み込み。記録画面にオプションが出ない。Press Key／MultiAction のマクロは従来どおり。
4. **W4: SA の Press Key（Scan Code の移動）**（**完了（ビルド・テスト成功、実機確認済み。2026-10-21）**。新規テスト: `Phase10Step1W4PressKeyScanCodeTests`。変更: `PressKeyViewModel.cs`［`UseScanCode`、`IsScanCodeEnabled`］、`SpecialActionEditor.xaml`［チェックボックス］、`BindingWindow.xaml.cs`［`ForPressToggle` で `topOptsPanel` を隠す］。設計上の判断: Scan Code は `keyType` とは別のフィールドで持つ。ボタンを選んで `keyType` が 0 になっても、選択を失わず、キーに戻したときに使える。ボタンのときは、チェックボックスを無効にする）
   - **実機確認で見つかった、以前からの不具合（2026-10-21）**: SA の Press Key で、Scan Code にチェックを入れて保存しても、開き直すと外れていた。原因は、`ActionsDTO`（Actions.xml の書き出し）が、`SpecialAction` のコンストラクターで「Scan Code」を除かれた `details`（`"65"`）をそのまま Details に書き、`keyType` の `ScanCode` ビットを書いていなかったこと（以前は選択ダイアログのチェックボックスでも、同じ理由で保存されなかった）。是正: `ActionsDTO` の Key で、`ScanCode` のとき Details に「 Scan Code」を足して書く（読み込み側は、元の DS4Windows と同じ書式を既に解釈する）。あわせて、`SpecialActionRepository.SaveAction` のキー実行時状態のリセットが、「65 Scan Code」を数値として読めず飛ばされていた点も、先頭の語だけを読むよう是正（`Phase10Step1W4KeyActionScanCodePersistenceTests`）。**これは暫定の是正**: SA の文字列の受け渡し（`details` の「65 Scan Code」、`extras` の改行区切り・`/` 区切り）を、型のある独立した項目に置き換える作業は、Step3 で行う（`Phase10-Assignment-UI-Unification-Plan.md` §6 Q7）。
   - `PressKeyViewModel.UseScanCode`、Press Key タブのチェックボックス、`BindingWindow`（`ForPressToggle`）の Toggle／Scan Code の非表示。
   - テスト: `PressKeyViewModel` の `LoadAction`／`PrepareSettings`／`ReadSettings`／保存の往復で、`ScanCode` と Toggle が保たれる。ダイアログを通した後も、タブの値が保たれる。
   - 実機（画面）の確認: SA の Press Key（キー、パッドのボタン）で、Scan Code と Switch Mode の設定・保存・再読み込み。ダイアログに Toggle／Scan Code が出ない。Controls の選択ダイアログは従来どおり。
5. **W5: 文書**（**完了**）: `Phase10-Status.md`（新規）、本書の完了の記録、`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 への確認項目の追加、`Phase10-Assignment-UI-Unification-Plan.md` の Step1 の完了の記録。

## 6. 決定事項（ユーザー承認、2026-10-21）

- **Q1＝ドロップダウンに統一**: 繰り返しの表現は、ドロップダウン（Play Once／Repeat While Held）。SA の Macro タブでは、ラベル「Execute Mode」で、Press Key の「Switch Mode」と同じ形にする。内部の値は、従来どおり `RepeatMacro` の有無。
- **Q2＝記録画面の繰り返し・Scan Code を消す**（SA から開く場合）: Scan Code は Macro タブのものを残す。記録画面は、SA から開くときは記録専用にする。Controls から開く `RecordBox` は、Step4 まで従来どおり。
- **Q3＝Phase9-I へ回す**: 使われなくなる言語リソース（`SpecialActionEdit.RepeatWhileHeld`。4 言語）は、Step1 では消さず、最終クリーンアップ（C-4）の候補に加える。
- **追加の指示**: Press Key の Scan Code を、選択ダイアログからタブへ移す（W4）。ダイアログの Toggle は削除する（Switch Mode がある）。

## 7. リスク

- `RecordBox`（`RecordBoxViewModel` 412 行＋ `RecordBox.xaml.cs` 632 行）は、Controls と SA の両方から使われる。W2 の変更が、W3 の前の SA にも影響する。`ShowPlaybackOptions` の既定を `true` にして、W3 で SA の側だけ `false` にする。
- Press Key は、保存の経路（`PrepareSettings`→ダイアログ→`ReadSettings`→保存）が長い。W4 の往復テストで、Scan Code・Toggle・既存のキーの値が、保たれることを固定する。
- 画面（XAML）の変更は単体テストで検査できない。各 Wave の後の実機（画面）の確認が必須。
- 4 言語の表示: 新しい文字列は、「Execute Mode」のラベル 1 つだけ（既存の `Switch Mode:` と同様に、直書きまたは新しいキー）。翻訳が要る場合は、既定の英語で出し、翻訳は後続にする。

## 8. 補足: 画面部品を XAML なしにした理由（2026-10-21）

`MacroPlaybackOptionsPanel` は、当初 XAML（`.xaml`＋`.xaml.cs`）で作ったが、**クリーンビルド**（`scripts\Remove-bin.bat` の直後）で、WPF の一時プロジェクト（`*_wpftmp.csproj`）が、この部品の生成コードや BAML を見つけられず失敗した（`InitializeComponent` が存在しない、`BG1002: ...MacroPlaybackOptionsPanel.baml が見つかりません`）。増分ビルドでは、成功していた。`scripts\Build-Debug.ps1` の自動の再試行でも直らなかったため、**XAML を持たない（コードだけで組み立てる）部品に作り直した**（見た目・動作・公開のプロパティ `AllowModeEdit`／`ShowScanCode`／`ScanCodeAlignment` は同じ）。原因（WPF のどの段階の問題か）は、特定していない。今後、同じプロジェクト内の別の XAML から参照される XAML の部品を新設するときは、クリーンビルドでの確認が要る。

