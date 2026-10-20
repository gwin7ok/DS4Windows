# Phase10-Step2 計画書: 共通の編集画面の骨格（ViewModel の分離、Controls／SA の 2 モード化、優先順位・抑制の特性化テスト）

作成日: 2026-10-21  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase10-Assignment-UI-Unification-Plan.md`（§5 Step2、§6 決定事項、§7 優先順位と抑制の仕様）、`Phase10-Status.md`  
状態: **方針承認済み（§7）。承認をもらい次第、Wave 0 から着手する**

---

## 1. 目的と範囲

SA の編集画面（`SpecialActionEditor`）を土台にして、**Controls のボタン 1 つの割り当ても編集できる共通の編集画面**の骨格を作る。Step2 では、SA のモードは**従来と同じ動作**を保ち、Controls のモードは、骨格と変換（データの出し入れ）まで作る。

- **範囲内**:
  1. 優先順位・抑制の仕様（`Phase10-Assignment-UI-Unification-Plan.md` §7）の、特性化テスト（現行の挙動を固定する。最初に行う）。
  2. 編集画面の ViewModel を、「トリガーの供給元」「保存先」「種別の一覧」「各種別の入力」に分離する。
  3. トリガーの画面部品（約 50 個の名前つきチェックボックスと、そのコードビハインド）を、データ駆動の部品に置き換える。Controls のモードでは、対象のボタン 1 つだけを表示し、Name 欄は編集不可にして、ボタン名を表示する。
  4. Controls のモードの、`DS4ControlSettings` との変換（Key／Button／Macro／Default）と、編集用のコピー方式（Cancel で元に戻る）。往復変換のテストで、持ち越し C-5 と B-11 の食い違いを再現・確定する。
- **範囲外**:
  - 保存形式の拡張（Step3）、Controls の SA 専用の種別（Step3・Step5）、選択ダイアログ `BindingWindow` の統一と、Controls タブからの呼び出しの切り替え（Step4）、SA 側のアナログ出力（Step4b）、旧画面の撤去（Step6）。
  - 保存形式、実行、ログは、Step2 では変えない。

## 2. 現状の調査（2026-10-21）

### 2.1 SA の編集画面

- `SpecialActionEditor`（UserControl。コードビハインド 710 行、XAML 約 550 行）は、`ProfileEditor` の Special Actions タブの中で、一覧の代わりに表示される（`NewActionBtn_Click`／`EditActionBtn_Click`。`Saved`／`Cancel` イベントで戻る）。
- トリガー: 名前つきのチェックボックス 46 個（`crossTrigCk` ほか）と、解除トリガー用の同数（`unload*`。実際は `Visibility=Collapsed` で隠れている）。コードビハインドが、2 つのリスト（`triggerBoxes`、`unloadTriggerBoxes`）に手で並べ、`ControlTriggerCheckBox_Click` で `specialActVM.ControlTriggerList` に反映する。
- ViewModel: `SpecialActEditorViewModel`（Name、トリガー、種別の添字、検証）と、種別ごとの ViewModel（`MacroViewModel`、`PressKeyViewModel`、`LaunchProgramViewModel`、`LoadProfileViewModel`、`CheckBatteryViewModel`、`MultiActButtonViewModel` ほか）。保存は、`SaveBtn_Click` の `switch` が、種別ごとの ViewModel の `SaveAction` を呼ぶ（`ISpecialActionRepository.SaveAction`。Actions.xml）。
- 検証（`IsValid`）: 名前の必須と重複、トリガーが 1 つ以上、種別が選ばれていること。**これらは SA だけの規則**。

### 2.2 Controls の編集

- `ProfileEditor` のボタンの割り当てハンドラー（`HoverConBtn_Click` ほか 6 か所）が、`BindingWindow`（Full）を開く。`mpControl.Setting`（`DS4ControlSettings`）を**直接**編集し、ウィンドウを閉じるとき（`Window_Closing`）に `WriteBinds` で書き込む。Cancel はない（閉じると保存される）。
- Controls の保存先は、プロファイルの `DS4ControlSettings`（`actionType`、`action`、`keyType`、`extras`）。種別は `Default／Key／Button／Macro` の 4 つ。

### 2.3 優先順位・抑制の、現行のコードの順序（`Mapping.MapCustom`、確認済み）

1. `MapCustomAction` が、SA の束縛をエンジンに処理させる（`_engine.ProcessOwned`）。成立した SA は、トリガーのボタンを `Suppressed` に入れる。
2. そのあと、Controls の各ボタンを `ProcessControlSettingAction` で処理する。**最初に** `CheckForSpecialActionSuppression`（`Suppressed` を見る）。抑制されたボタンは、通常の出力を既定値に戻して、処理を終える。

したがって、**同じフレームで SA が成立すれば、最後に押されたボタンの Controls の処理は、そのフレームから実行されない**（§7.1 条件 2 の後半を、コード上は満たす見込み）。これは特性化テストで確定する（W0）。

## 3. 設計

### 3.1 ViewModel の分離（W1）

| 部品 | 責務 |
|---|---|
| `IActionTriggerSource` | トリガーの供給元。SA 用（`SpecialActionTriggerSource`: 複数のボタンの集合。追加・削除・検証）と、Controls 用（`ControlTriggerSource`: 対象の 1 つのボタンで固定。変更不可） |
| `IActionEditTarget` | 読み込みと保存の窓口。SA 用（`SpecialActionEditTarget`: 名前、`Actions.xml`、`ISpecialActionRepository`）と、Controls 用（`ControlSettingsEditTarget`: 編集用のコピーの `DS4ControlSettings`。保存で元へ反映） |
| `ActionTypeDescriptor`（一覧） | 種別の一覧。表示名、種別の ID、使えるモード（SA／Controls／両方）、現在保存できるか（Step3 までは、Controls で保存できない種別は、一覧に出して無効にする） |
| `ActionEditorViewModel` | 上の部品と、種別ごとの既存 ViewModel をまとめる。名前の検証と重複検査は、SA のモードだけ |

- 既存の種別ごとの ViewModel（`MacroViewModel`、`PressKeyViewModel` ほか）は、そのまま使う（Step1 で整理した部品）。
- SA のモードは、保存の呼び出し（`ISpecialActionRepository.SaveAction` の引数）を、**従来とまったく同じ**にする（W1 の特性化テストで、種別ごとに固定する）。

### 3.2 トリガーの画面部品（W2）

- `TriggerSelectionPanel`（新規 UserControl）: `IActionTriggerSource` の項目（ボタンの一覧。`DS4Controls` の名前と、表示名）をデータ駆動で並べる（`ItemsControl`＋チェックボックスのテンプレート）。SA のモード: チェックボックスの一覧（現行と同じ並び・同じ項目）。Controls のモード: 対象のボタンだけを表示（チェックボックスなし。読み取り専用のラベル）。
- `SpecialActionEditor.xaml` の 46＋46 個のチェックボックスと、`triggerBoxes`／`unloadTriggerBoxes`、`ControlTriggerCheckBox_Click`／`ControlUnloadTriggerCheckBox_Click` を撤去する（コードビハインドの縮小）。
  - **解除トリガー（`unload*`）**: 実際は非表示（`Visibility=Collapsed`）で、Profile 切替の解除トリガーのボタンによる復帰は Phase8-8b で廃止済み。データ（`ucontrols`）は、保持して書き戻す（現状と同じ）。画面部品は作らない。
- 項目の並びと、`Tag` の値（`Cross`、`L2 Full Pull`、`Function Left` など）は、現行の XAML と完全に一致させる（既存の `Actions.xml` を読めるように。W2 のテストで、並びと名前を固定する）。

### 3.3 Controls のモード（W3・W4）

- **Name 欄**: Controls のモードでは、編集不可にして、対象のボタン名（`DS4ControlNames.ds4inputNames`）を表示する。
- **種別の一覧**（右ペインの最上部）: §6 Q1＝全種別を両方で選べる、の決定に従い、一覧は両モードで同じ。Controls で現在保存できる種別（Default、Output Key/Mouse/Button［Key・Button］、Macro）だけを有効にし、ほかは無効（Step3 で有効にする）。
- **編集用のコピー**: Controls のモードは、`DS4ControlSettings` の**コピー**を編集し、Save で元へ反映、Cancel で破棄する（従来の「閉じると保存」とは違う。共通の編集画面は、Save／Cancel を持つため）。
- **変換**: `ControlSettingsEditTarget`が、`DS4ControlSettings`（`actionType`、`action`、`keyType`、`extras`）と、各種別の ViewModel の間を変換する。`extras` は、Step2 では**そのまま保持して書き戻す**（編集しない。Extras の UI は Step4。§6 Q6）。
- **Toggle／Scan Code**: Press Key タブの Switch Mode／Scan Code が、Key と Button の両方に効くようにする（持ち越し C-5 の解消の土台。`OutBinding.WriteBind` の「Key でしか Toggle を書かない」欠落を、この変換で補う）。
- **呼び出しの切り替えは、Step4**: Step2 では、`ProfileEditor` の Controls タブの呼び出しを、変更しない（従来の `BindingWindow` のまま）。Controls のモードの画面は、Step2 では作るが、ユーザーの操作からは開かない（§7 Q2）。未接続のコードには、規約 3.3 原則 4 の TODO コメント（経緯、未接続の理由、接続予定の Step4）を付ける。

## 4. 変えないもの

- 保存形式（`Profile.xml`、`Actions.xml`）、実行、ログ。
- SA の編集画面の見た目と動作（トリガーの並び、種別、保存の内容、検証のメッセージ）。ユーザーから見て、SA の編集は、Step2 の前後で変わらない。
- Controls の選択ダイアログ（`BindingWindow`）と、`ProfileEditor` の Controls の呼び出し。

## 5. 作業の順序（Wave）

各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する。画面を変える Wave は、実機（画面）の確認を付ける。

1. **W0: 優先順位・抑制の特性化テストと、C-5／B-11 の再現**（製品コードは変えない）
   - 既存のテストの足場（`SpecialActionTestScope`、`ControlsKeyTestScope`、`RunMapCustom`）で、フレーム列（押す・離すの順序）を入力し、`Phase10-Assignment-UI-Unification-Plan.md` §7 の条件 1・2 と R1〜R10 を固定する。
     - 条件 1: 同じボタンに Controls と SA があるとき、SA が成立したら、Controls の出力が出ない。
     - 条件 2: 複数ボタンのトリガーで、成立の手前までに押したボタンの Controls の出力は出る。最後のボタン（同じフレーム）は出ない。同時押し（同じフレーム）は、全ボタンが抑制される。
     - R3（取り消さない）、R4（SA どうしの重なり）、R5（素通し）、R6（アナログ）、R7（再押し）、R9（無効な SA）ほか、再現できるものを固定する。再現できないもの（実機が要る、など）は、仕様台帳に「確認不能」と記す。
   - C-5／B-11 の再現: `OutBinding.WriteBind` の Button の分岐が、Toggle を書かない（現行の挙動）ことを、テストで固定する。Step9 の B-11 の記録（「パッドのボタン A では Toggle が設定される」）との食い違いの原因（Toggle が、既存の Key の設定から残ったか、など）を、再現して確定する。
   - 現行のコードが、仕様（§7）と**食い違う**場合は、不具合として扱い、是正の承認を得る（実装の前に、結果を報告する）。
2. **W1: ViewModel の分離**（`IActionTriggerSource`、`IActionEditTarget`、`ActionTypeDescriptor`、`ActionEditorViewModel`。SA のモード）
   - テスト: 種別ごと（10 種）に、SA のモードの保存が、従来と同じ `SaveAction` の引数になること（`ISpecialActionRepository` の記録用の偽物で固定）。名前・トリガー・種別の検証が、従来と同じこと。
3. **W2: トリガーの画面部品のデータ駆動化**（`TriggerSelectionPanel`。46＋46 個のチェックボックスの撤去）
   - テスト: 項目の並びと名前が、従来の XAML と一致すること。読み込み・選択・解除の動作。
   - 実機（画面）の確認: SA の新規作成・編集で、トリガーの選択・保存・再読み込みが従来どおり。既存の SA のトリガーが、すべて正しく復元される。
4. **W3: Controls のモードの画面**（Name 欄の読み取り専用、トリガーの 1 つ固定、種別の一覧の無効化、Save／Cancel）。呼び出しは接続しない。
   - テスト: Controls のモードで、Name が対象のボタン名で編集不可、トリガーが 1 つで変更不可、保存できない種別が無効、など。
5. **W4: Controls のモードの変換と、編集用のコピー**（`ControlSettingsEditTarget`）
   - テスト: `DS4ControlSettings` の往復（Default／Key／Button／Macro、Toggle／Scan Code／Repeat、`extras` の保持）。Cancel で元が変わらないこと。Button の Toggle が保たれること（C-5 の土台）。
6. **W5: 文書**: `Phase10-Status.md`、本書の完了の記録、実機確認の台帳の追加、モデル図（必要なら）の更新。

## 6. リスク

- **画面（XAML）の大きな変更**（W2）: 46＋46 個のチェックボックスの撤去。単体テストで検査できない部分が多く、実機（画面）の確認が必須。並びと `Tag` の値の取り違えが、既存の `Actions.xml` の読み込みを壊し得るため、W2 のテストで固定する。
- **優先順位・抑制の食い違い**（W0）: 現行のコードが §7 と食い違う場合、是正の承認が要り、Step2 の後続に影響する。W0 で先に確定する。
- **Controls の保存の方式の変更**（閉じると保存 → Save／Cancel）: 呼び出しの切り替え（Step4）まで接続しないため、Step2 では、ユーザーに影響しない。
- **未接続のコード**: Controls のモードの画面は、Step4 まで使われない。規約 3.3 原則 4 の TODO コメントを付ける。

## 7. 決定事項（ユーザー承認、2026-10-21。すべて推奨どおり）

- **Q1＝Controls タブの中に埋め込む**: SA タブと同じように、Controls タブの内容を編集画面に切り替える（同じ画面部品をそのまま使う）。
- **Q2＝接続しない**: Step2 では、Controls のモードの画面を作るだけで、ユーザーの操作からは開かない。Controls タブは従来の選択ダイアログのまま。Step4 で切り替える。
- **Q3＝クラス名は Step6 まで変えない**: `SpecialActionEditor` のまま。Step6 の旧画面の整理で `AssignmentEditor` へ改名する（TODO コメントで明記）。
- **Q4＝W0 は、再現できるものすべて**: 条件 1・2 と R1〜R10 のうち、テストで再現できるものすべてを固定する。再現できないものは、仕様台帳に記す。
- **Q5＝W0 で食い違いが見つかったら、報告して決める**: 実装の前に結果を報告し、「コードを仕様に合わせて是正する」か「仕様を現行に合わせる」かを、ユーザーが選ぶ。動作を勝手に変えない。
