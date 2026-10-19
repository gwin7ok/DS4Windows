# Phase8-Step7-8d 計画書: Controls タブの「シフト」機能の廃止

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§2.2 の B、§3.3。7.8d を「シフトの新経路への移行」から「シフトの廃止」に変更）、`Phase8-Step7-Plan.md`（7.8）、`Phase10-Assignment-UI-Unification-Plan.md`  
状態: **計画書を承認済み（2026-10-20。ユーザー）。W0（調査）、W1（部品とテスト）完了。W2（読込・保存・実行への配線と、シフトの削除。画面の項目の削除を含む）完了［ビルド・テスト成功。2026-10-20］。W3（編集画面の警告ダイアログ。文言の追加）完了［ビルド・テスト成功。2026-10-20］。W4（文書・チェックリストの仕上げ）完了。7.8d はコード・テスト・文書とも完了（実機の画面確認は Step9 の U-43）。次は 7.8e（Extras）**

> 初版（シフトを Key／Macro で新経路へ移す計画）は、ユーザーの廃止の方針により破棄した。初版の調査（旧経路のシフトの動き、副ボタンの表、通常とシフトが同じ名前で状態を共有する癖）は、§2 に要点を残す。

---

## 0. 決定（ユーザー）

1. **シフト機能（`shiftTrigger`、シフトの割り当て、シフトの Extras）を廃止する**（規約 2.2 の機能廃止。ユーザーの明示的な承認。2026-10-20。この計画書が、廃止の記録を兼ねる）。理由: 同じことは、SpecialActions タブの組み合わせトリガーで代用できる（Key、X360 のボタン、Macro）。代用できない使い方［アナログの割り当て、Extras］は、ユーザーが承知のうえで失う。
2. **廃止後も、プロファイル設定ファイルにシフトの設定が残っている場合の扱い**: そのプロファイルの**編集画面を開いたときに警告を出し**、ユーザーに**「設定項目自体を削除する」か「残す」か**を選ばせる。**残す場合は、編集画面を開くたびに警告を出す**。
3. **廃止するタイミングは、担当（エージェント）に一任**（§3）。
4. **シフトの設定値を SA へ変換する移行処理は作らない**。

## 1. 廃止するもの

- Controls タブの割り当ての「シフト」: `DS4ControlSettings` の `shiftActionType`、`shiftAction`、`shiftTrigger`、`shiftExtras`、`shiftKeyType`、および画面の項目。
- 実行時の処理: `Mapping.ShiftTrigger`、副ボタンの表（`shiftTriggerMapping`、番号 26 のタッチ 1 本指）、`ProcessControlSettingAction` のシフトの枝（割り当ての選択、`shiftE`）。

**廃止しないもの**: SA タブの組み合わせトリガー、Controls タブの通常の割り当て、通常の Extras（`extras`）。

## 2. 現状の調査（要点）

- 参照の規模: シフトに関する識別子は、12 ファイルに約 138 か所（`DS4ControlSettings`、`BackingStore`、`OutBinding`、`BindingWindow`＋ViewModel、`MappedControl`、`RecordBoxViewModel`、`Mapping`、`ProfileButtonSaveSection`、`ProfileButtonShiftLoadSection`、`ControlBindingBuilder`、`ProfileSettingsService`、翻訳の文字列）。内蔵のプリセットは、シフトを使っていない。
- 保存: `Profile.xml` の `<ShiftControl>`（`Button`／`Macro`／`Key`／`KeyType`／`Extras` の子。各項目に `Trigger` の属性）。読込は `ProfileButtonShiftLoadSection`、保存は `ProfileButtonSaveSection`（通常とシフトを 1 つのループで集めて保存）。
- 旧経路の癖: 通常とシフトは同じ合成 SA の名前で状態を共有する（押したままの切り替えで、押下・解放のエッジが起きない可能性）。廃止により、この癖も消える。
- 廃止による単純化: 7.8d（シフトの新経路）が不要になる。エンジンの「1 ボタン ＝ 1 束縛」が保てる。7.8e（Extras）、7.8f（Button）の範囲から、シフトとの組み合わせが消える。

## 3. 時期（担当の判断）と順序

**7.8d として、7.8e・7.8f より前の、今、行う**（7.8c の次）。理由:

1. 新経路の所有の条件（「シフトなし」）と、7.8e・7.8f の設計が、単純になる（シフトとの組み合わせを考えなくてよい）。
2. シフトの新経路を作ってから消す無駄を避ける。7.8b・7.8c は、すでに「シフトなし」だけを新経路にしている。
3. Step8（旧コードの削除）の前に、シフトの扱いを決める必要がある（旧経路のシフトのコードは、Step8 で消すため）。
4. 機能の廃止は、データの読込・保存・画面に及ぶため、ほかの変更と混ぜない（独立した PR にする）。

## 4. 仕様

### 4.1 読込・保存・実行

- **読込**: `Profile.xml` に `<ShiftControl>` があり、子の項目が 1 つでもあるとき、その**ノードの内容をそのまま（加工せずに）保持**する（`RemovedShiftSettings`。プロファイルごと。中身は解釈しない）。設定値としては解釈せず、実行にも使わない。
- **実行**: シフトは存在しない。割り当ては、通常の割り当てだけが使われる。シフトの副ボタンを押しても、何も変わらない。
- **保存**: 「残す」状態（保持した内容がある）なら、`<ShiftControl>` を、保持した内容のまま書き出す（ほかの設定を編集して保存しても、シフトの内容は失われない）。「削除」を選んだ（保持した内容を捨てた）なら、`<ShiftControl>` は書き出さない（空の要素にする。従来、シフトがないときも空の `<ShiftControl>` が書き出されるため、形式は変わらない）。
- **ログ**: 読込のとき、シフトの設定を保持したら、警告のログを 1 行出す（新しいログ。旧経路にはない）。

### 4.2 編集画面の警告

- **いつ**: プロファイルの編集画面を開いて、そのプロファイルを読み込んだとき（`ProfileEditor` の `Reload` のあと）。保持した内容があるときだけ。
- **内容**: 「このプロファイルには、廃止された『シフト』機能の設定が含まれています。この設定は動作しません。SpecialActions タブの組み合わせトリガーで代用してください。」
- **選択**: 「**設定を削除する**」／「**残す**」。
  - 削除: 保持した内容を捨てる。**ファイルは、ユーザーが保存したときに更新される**（キャンセルなら、ファイルは変わらず、次に開くとまた警告が出る）。
  - 残す: 何も変えない。**編集画面を開くたびに、同じ警告を出す**（選択を記憶しない）。
- 警告は、編集画面を開くたびに 1 回。編集中は出さない。
- 警告の文言は、翻訳の仕組み（`Strings.resx` と `{lex:Loc}`）に追加する（英語と日本語）。

### 4.3 画面

- 割り当ての画面（`BindingWindow`）のシフトの項目（副ボタンの選択、シフトの割り当ての欄）を削除する。割り当ての一覧（`MappedControl`）の、シフトの表示も削除する。

## 5. 作業（Wave）

- **W0（調査。コードは変えない）**: シフトの参照 138 か所の一覧と、削除の範囲の確定。`Profile.xml` の書き込み・読み込みで、ほかに `ShiftControl` を触る経路（プロファイルの名前変更・SA の参照の更新で、XML を直接書き換える処理など）の有無。画面の警告の出し方（既存のダイアログの方式）。結果をこの計画書に追記する。
- **W1（仕様のテスト。先に書く）**: (a) 読込: `<ShiftControl>` に内容があれば保持、なければ保持しない。(b) 保存: 保持していれば、そのまま書き出す（ラウンドトリップ）。削除したら書き出さない。(c) 実行: シフトの副ボタンを押しても、割り当てが変わらない。(d) 警告の判定（保持の有無、削除、残す）は、画面から独立したクラスにして、テストする。
- **W2（実装）**: データモデルと実行の削除（`DS4ControlSettings` のシフトの欄、`Mapping` のシフトの処理、`ControlBindingBuilder` の条件）、読込・保存（`ProfileButtonShiftLoadSection` を、保持だけの処理に置き換え）、`RemovedShiftSettings`（保持）、警告の判定のクラス。
- **W3（画面）**: `BindingWindow` ほかのシフトの項目の削除、`ProfileEditor` の警告（ダイアログ。文字列の追加）。
- **W4（テストと文書）**: `Phase8Step1ShiftTriggerCharacterizationTests`（旧経路のシフトのテスト）の置き換え（「シフトは無視される」へ）、ゴールデンファイル（シフトの欄を含むもの）の更新、ラチェットの基準値の更新（件数が変わる場合）、完了報告書、`Phase8-Status.md`、チェックリスト（U-43: 編集画面の警告、削除と残すの動き）。

### 5.1 W0 の結果（2026-10-20。コードは変えていない）

1. **XML の経路は 1 系統だけ**: `LegacyProfileXmlSerializer`（読込 `ProfileButtonShiftLoadSection`、保存 `ProfileButtonSaveSection`）。新形式の DTO（`DTOXml`）には、シフトの欄がない。`SaveProfile` の `xmlControls`／`xmlShiftControls` は、使われていない変数（削除してよい）。名前の変更・SA の参照の更新（`ReplaceActionReference`、`ReplaceProfileSwitchTarget`）は、XML を直接書き換える方式で、`<ShiftControl>` はそのまま残る（影響なし）。
2. **`<ShiftModifier>`（旧形式の読込専用。全体で 1 つの副ボタンの番号）も、シフトの一部**。`ProfileMiscLoadSection` が読み、`ProfileLoadContext.ShiftModifier` 経由でシフトの読込が使う。廃止に合わせて、読まない（`Trigger` の属性を持たない旧形式の項目は、保持した内容に、番号なしのまま残る。動作しないので問題なし）。
3. **画面**: `ProfileEditor.Reload`（`profileRepository.LoadProfile(deviceNum, profile.Name)` の直後）が警告の位置。既存のダイアログは `System.Windows.MessageBox` ＋ `Properties.Resources` の文字列。警告の文言は、`Properties.Resources`（と翻訳）に追加する。
4. **ゴールデン**: `profile-gap.xml`、`profile-gap.resaved.xml`、`profile-legacy.resaved.xml`、`profile-varied.xml` に `<ShiftControl>` の内容がある（W2 で、保存の結果は「保持した内容のまま」になるため、読み込んだ内容がそのまま出る。状態のゴールデンは、シフトの欄が消える）。`profile-default.xml`、`profile-minimal.rewritten.xml` の空の `<ShiftControl />` は、変わらない。
5. **参照**: シフトの識別子は 12 ファイル、約 138 か所（`DS4ControlSettings` 25、`OutBinding` 32、`BindingWindowViewModel` 16、`ProfileButtonSaveSection` 17、`Mapping` 10、`RecordBoxViewModel` 10、`BindingWindow.xaml.cs` 8、`MappedControl` 7、`BackingStore` 4、`ControlBindingBuilder` 4、`ProfileSettingsService` 3、翻訳 2）。
6. **保持の置き場**: `BackingStore` の、スロットごとの `RemovedShiftSettings`（`ds4settings` と同じ寸法）。読込のたびに取り込み直す（`ResetProfile` でも空にする）。

### 5.2 W1 の実装（2026-10-20。ビルド・テスト成功）

保持・警告の判定の部品（配線なし）と、その仕様のテスト。新設（1 ファイル 1 型）: `RemovedShiftSettings`（保持。`Capture`／`Clear`／`AppendTo`）、`RemovedShiftChoice`（Delete／Keep）、`RemovedShiftOutcome`（NoNotice／Deleted／Kept）、`IRemovedShiftPrompt`（警告の窓口。実装は W3 のダイアログ）、`RemovedShiftNotice`（警告の判定。選択は記憶しない）。テスト: `Phase8Step78dW1RemovedShiftSpecTests`（取り込み、置き換え、空、書き出し［属性を保つ、空のときは従来と同じ `<ShiftControl />`］、削除、残す［毎回警告］）。実行への影響（シフトの副ボタンを押しても何も変わらない）と、読込・保存のラウンドトリップは、配線をする W2 のテスト。

### 5.3 W2 の実装（2026-10-20。ビルド・テスト成功）

- **データ・読込・保存**: `DS4ControlSettings` からシフトの欄（`shiftActionType`、`shiftAction`、`shiftTrigger`、`shiftExtras`、`shiftKeyType`、`IsShiftDefault`）を削除し、`UpdateSettings`（シフトの引数を削除）を単純化。`BackingStore` に `removedShiftSettings`（公開の読み取り専用の配列。スロットごと）を追加し、`UpdateDS4CSetting`／`UpdateDS4CExtra`／`UpdateDS4CKeyType`（シフトの引数を削除）、`ResetProfile`（保持を空に）を更新。`IProfileSettingsService.RefreshActionAlias` は 1 引数に（シフトの引数を削除）。読込は `ProfileButtonShiftLoadSection`（保持だけ）、保存は `ProfileButtonSaveSection`（シフトの組み立てを削除し、保持した内容を書き出す）。旧形式の `<ShiftModifier>` の読込（`ProfileMiscLoadSection`、`ProfileLoadContext`）を削除。`SaveProfile` の使われていない変数を削除。
- **実行**: `Mapping` から、`ShiftTrigger`、副ボタンの表（35 要素）、番号 26 の定数、`ProcessControlSettingAction` のシフトの選択と `shiftE` を削除。`ControlBindingBuilder` の「シフトなし」の条件を削除（シフトは存在しない）。
- **画面（W3 の警告ダイアログ以外）**: `BindingWindow`（シフトのラジオボタン、副ボタンの選択、関連のメソッド）、`BindingWindowViewModel`（シフト用の `OutBinding` と読み書き）、`OutBinding`（シフトの欄と書き込み）、`MappedControl`（シフトの表示）、`ProfileEditor`（ハイライトの表示）、`RecordBoxViewModel`／`RecordBox`／`EditorWindowFactory`／`ViewModelFactory`（シフトの引数）、`ProfilePresets`／`PresetMenuHelper`（呼び出し）。
- **テスト**: 新規 `Phase8Step78dW2RemovedShiftWiringTests`（読込で保持、保存でそのまま、削除したら空の要素、ほかの設定を編集しても保たれる、`ResetProfile`、シフトのメンバーがないこと、ソースのガード）。`Phase8Step1ShiftTriggerCharacterizationTests`（旧経路のシフトの特性化）を削除。ガード・テスト・シグネチャを更新（`MappingCommitAndCalibration…` の終端、`RefreshActionAlias`、`RecordBox` 系の呼び出し、7.8b・7.8c のテストのシフトの項目）。ゴールデン: `profile-gap.xml`、`profile-gap.resaved.xml`、`profile-legacy.resaved.xml`、`profile-varied.xml` の `<ShiftControl>` を空の要素に、`profile-*.state.txt` からシフトの行（各 150 行）を削除。

### 5.4 W3 の実装（2026-10-20。ビルド・テスト成功）

- 新設: `MessageBoxRemovedShiftPrompt`（`DS4Forms/`。`IRemovedShiftPrompt` の画面の実装。`MessageBox` の「はい」＝削除、「いいえ」［またはダイアログを閉じる］＝残す。選択は記憶しない）。
- 配線: `IProfileSettingsService.GetRemovedShiftSettings(device)`（`ProfileSettingsService` が `BackingStore.removedShiftSettings` を返す）、`ProfileEditor.Reload`（`profileRepository.LoadProfile` の直後に `RemovedShiftNotice.Apply(…)`。プロファイルがあるときだけ。新規作成では呼ばない）。
- 文言: `Properties/Resources.resx`（英語）と `Resources.ja.resx`（日本語）に `RemovedShiftWarning`（`{0}`＝プロファイル名）を追加し、`Resources.Designer.cs` に手で参照用のプロパティを追加した（ほかの言語は英語にフォールバック）。
- テスト: `Phase8Step78dW3RemovedShiftNoticeWiringTests`（サービスが保持の入れ物を返す、文言［英語・日本語］、`Reload` の読込の直後に警告を呼ぶ［1 か所］）。ダイアログそのものは、Step9 の実機（画面）の確認（U-43）。

## 6. リスクと対応

| リスク | 対応 |
|---|---|
| 利用者のシフトの設定が失われる | 編集画面で、削除か残すかを選ばせる。残せば、ファイルの内容は保たれる（動作はしない）。SA への変換は作らない（ユーザー指示） |
| 参照が多く、削除の漏れ・取りすぎ | W0 で一覧を作り、W4 のソースのガード（シフトの識別子が残らないこと）で確認する |
| ゴールデンテストの更新が広範囲 | 差分が、シフトの欄だけであることを確認してから更新する |
| 保持した内容（生の XML）が、ほかの保存経路で消える | W0 で、`ShiftControl` を書き出す経路を洗い出し、すべて保持した内容を使う |
| 画面の確認が自動化できない | 警告の判定は、画面から独立させてテストする。画面は Step9 の実機（画面）の確認（U-43） |

## 7. 完了条件

- [x] W0 の結果を追記済み（§5.1）。
- [x] ビルド・テストが成功（ユーザーが実行。W1〜W3 とも成功。2026-10-20）。W1 の仕様のテスト、全ガード（1 ファイル 1 型、ラチェットの一致検査）。
- [ ] 実機（画面）の確認は Step9（U-43。チェックリストに登録済み）。
- [x] `Phase8-Step7-Completion-Report.md`（機能廃止の記録）、`Phase8-Status.md`、`Phase8-Step2b-ControlsTab-Design.md` の更新。

## 8. 次

7.8e（Extras／Lightbar マクロ。シフトの Extras は廃止済み。通常の Extras だけ）。
