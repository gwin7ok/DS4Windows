# Phase10-Step4 計画書: Controls で SA 専用の種別を保存・実行する

作成日: 2026-10-21  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase10-Assignment-UI-Unification-Plan.md`（§5 Step4、§6 Q1・Q7、§7 優先順位・抑制の仕様 R1〜R10）、`Phase10-Step3-Plan.md`（§3.5 暫定の隠し・グレーアウト）、`Phase8-Unified-Trigger-Dispatch-Plan.md`（D2＝B1）、`Phase10-Status.md`  
状態: **案（Q1〜Q4 は確定。Q5 は、最後の群で、実現可否を確認してから決める。ユーザーの承認後、Wave 0 から着手する）**

---

## 1. 目的と範囲

Controls のボタン 1 つの割り当てに、**SA 専用の種別**を、保存でき、実行できるようにする（ユーザーの方針: 最終的に、Controls も SA と同じ状態にする。マスター計画書 §6 Q1）。割り当てエディターで、暫定的に隠している項目・グレーアウトしている種別を、順に出す。

対象の種別と項目（Controls のモードで、いま選べない・隠れているもの）:

| 対象 | 内容 |
|---|---|
| Macro の SA 専用の項目 | Run on trigger release（`pressRelease`）、Synchronized Run（`synchronized`）、Keep key state（`keepKeyState`） |
| Launch Program | `details`＝実行ファイルのパス、`delayTime`＝押し続ける秒数、`extra` |
| Load Profile | `details`＝プロファイル名、`extra`（自動復帰 `AutomaticUntrigger`。解除トリガー `uTrigger` は廃止済み） |
| Disconnect from Bluetooth | `delayTime`＝押し続ける秒数 |
| Check Battery | `details`＝色の指定（「|」区切りを「,」へ。電池の空・満の色） |
| Multi-action Button | `details`＝タップ・長押し・ダブルタップのマクロ（「,」で 3 つ）、`delayTime` |
| Calibration of sixaxis wheel／Calibrate gyro | 固有の項目なし（トリガーだけ） |

範囲外: SA 側の Extras・アナログ出力（Step5）。SA の保存形式の変更（Step6。ただし、型のある項目の定義は、Step4 で作り、Step6 で SA が採用する。§3.1）。

## 2. 現状の調査（2026-10-21）

### 2.1 保存形式
- **Controls**（`Profile.xml` の `<Control>`。`ProfileButtonSaveSection`）: ボタンごとに、割り当ての種類別の節（`<Button>`、`<Macro>`、`<Key>`）と、`<Extras>`、`<KeyType>`（`Unbound`、`RepeatMacro`／`Macro`、`Toggle`、`ScanCode` の文字列）がある。1 ボタンにつき、`DS4ControlSettings`（`actionType`［Default／Key／Button／Macro］、`action`［`actionKey`、`actionBtn`、`actionMacro`］、`keyType`、`extras`）。SA 専用の項目を持つ場所は、ない。
- **SA**（`Actions.xml`。`SpecialAction`）: 名前、トリガー（`controls`）、種別（`type`）、`details`（種別ごとの文字列）、`delayTime`、`extra`（種別ごとの文字列。`Macro` は、`Scan Code／RunOnRelease／Sync／KeepKeyState／Repeat` を含む文字列。`Profile` は、`AutomaticUntrigger`）。種別ごとの意味が、**文字列**の中にある（マスター計画書 §6 Q7）。

### 2.2 実行
- SA と Controls は、同じエンジン（`TriggerDispatchEngine`）で動く（D2＝B1）。エンジンは、**入力の型**で、SA（`ComboTriggerInputAction`、複数ボタン）と Controls（`ControlButtonInputAction`、単体ボタン）を見分ける（優先順位・抑制の規則 R1〜R10 が、この区別で働く）。
- 束縛を作る部分が別: SA は `ActionBindingBuilder`（`Create` が、種別ごとの出力アクション［`LaunchProcessAction`、`ProfileSwitchAction`、`DisconnectBTAction`、`BatteryCheckAction`、`MacroStringAction` ほか］と、入力・方針を作る）、Controls は `ControlBindingBuilder`（Key・Macro・Button・Extras・素通しだけ）。
- **すでにある前例**: `ControlBindingBuilder.AddMacro` は、Controls のマクロのために、合成の `SpecialAction`（`Synthetic_Macro_{device}_{control}`）を作り、SA と同じ出力アクション（`MacroAction`）を、`ControlButtonInputAction` の入力と、Controls の方針（毎フレーム通常の出力を消す）で、束縛している。SA 専用の種別も、同じやり方で束縛できる（出力アクションは、すべて `SpecialAction` を受け取る）。
- 出力アクションの作り方が、`ActionBindingBuilder.Create` の中にあり、`ControlBindingBuilder` からは使えない（依存する「窓口」［`IProfileSwitcher`、`IProcessLauncher`、`IDisconnectPort`、`IBatteryCheckPort` ほか］を、`ActionBindingBuilder` だけが持つ）。

## 3. 設計

### 3.1 型のある項目の定義（保存の土台）
- Controls の割り当てに、SA 専用の種別を持たせるための、**型のある項目**（種別の ID、種別ごとのプロパティ）を定義する（案: `ControlSpecialItem`。種別＝`ActionTypeId`、プログラムのパス、プロファイル名、色、マクロの 3 つ、`delayTime`、フラグ［`RunOnRelease`、`Sync`、`KeepKeyState`、`AutomaticUntrigger`］）。文字列の `details`／`extra` へ詰めない（マスター計画書 §6 Q7）。
- `DS4ControlSettings` に、この項目を足す（`actionType` に `Special` を足し、型のある項目は別のプロパティで持つ。§9 Q2＝確定）。
- SA 側（`Actions.xml`）は、Step4 では変えない。Step6 で、同じ型のある項目を採用する（共通の割り当て定義への道。builder の統合の土台）。

### 3.2 `Profile.xml` への保存
- `<Control>` に、SA 専用の割り当ての節を足す（案: `<Special>`。ボタンごとの要素に、種別と項目を、属性・子要素で持つ）。既存の節（`<Button>`／`<Macro>`／`<Key>`／`<Extras>`／`<KeyType>`）は、変えない。
- **互換**: 読み込みは、新しい節がなければ、従来どおり。新しい節は、古いバージョンのアプリが、読み飛ばす（保存し直すと、失われる。下位互換の喪失は、ユーザーが許容済み［マスター計画書 Q5］）。
- 保存・読み込みは、往復のテスト（全種別、境界値、不正値）を先に作る。

### 3.3 実行: 出力アクションの作り方の共通化（builder 統合の第一歩）
- `ActionBindingBuilder.Create` の、種別ごとの**出力アクションの作成**を、共通の部品（案: `SpecialActionOutputFactory`）へ取り出し、`ActionBindingBuilder` と `ControlBindingBuilder` の両方が使う（依存する窓口は、この部品が持つ）。入力と方針は、それぞれの builder が決める（SA: 複数ボタンの組み合わせ、Controls: 単体ボタン＋毎フレームの出力の消去）。
- `ControlBindingBuilder` は、SA 専用の割り当てを、合成の `SpecialAction`（`Synthetic_Special_{device}_{control}`）で表し、共通の部品から出力アクションを得て、`ControlButtonInputAction` と Controls の方針で束縛する（`AddMacro` と同じ形）。優先順位・抑制の規則（R1〜R10。特に R10）は、入力の型で、そのまま働く。
- 種別ごとの方針（`BindingPolicy`）は、SA のものを基本にする（例: Program・Profile は `blocksWhileExecuting`、Profile は `untrigger`）。Controls 側の差は、`erasesOutputEveryFrame: true`（割り当てたボタンの通常の出力を、毎フレーム消す。§9 Q4＝確定）。判定の閾値は、SA のものを使う（§9 Q3＝確定）。

### 3.4 画面（割り当てエディター）
- 種別ごとの入力（`LaunchProgramViewModel`、`LoadProfileViewModel`、`CheckBatteryViewModel`、`MultiActButtonViewModel` ほか）は、いま、SA の保存（`ISpecialActionRepository.SaveAction`）へ書く。Controls のモードでは、型のある項目へ読み書きする経路を足す（`IActionTypeEditor` に、`SpecialAction` とは別の、型のある項目への変換を足す）。
- `ActionTypeDescriptor.SavableIn` を、種別ごとに、Controls へ広げる（順次）。グレーアウト・隠しの解除は、保存・実行・読み込みが動いたものから。

## 4. 変えないもの
- SA の保存形式（`Actions.xml`）、SA の実行・優先順位・抑制の規則、ログ。
- Controls の既存の割り当て（Key・Button・Macro・Extras・Default・Unbound）の保存形式と実行。

## 5. 作業の順序（Wave 案）
各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する。画面・動作を変える Wave には、実機確認を付ける。**種別の群ごとに、保存・実行・画面を縦に通して、1 群ずつ完了させる**（§9 Q1）。

1. **W0: 特性化テスト**（製品コードは変えない）: `ActionBindingBuilder.Create` の種別ごとの束縛（出力アクションの型、方針）、`ControlBindingBuilder.AddMacro` の束縛、優先順位・抑制の規則（R10 を含む。Step2 W0 の特性化テストの拡張）、`Profile.xml` の `<Control>` の保存・読み込み（現在の形式）。
2. **W1: 型のある項目と保存・読み込み**（画面なし・実行なし）: §3.1・§3.2。往復のテストを先に作る。
3. **W2: 出力アクションの作り方の共通化**（動作は変えない）: §3.3 の取り出し。SA の束縛が、変わらないこと（W0 のテストで固定）。
4. **W3: 第 1 群の縦通し**（案: Macro の SA 専用の項目）: 保存・読み込み・実行・画面（3 つのチェックボックスの表示）。実機確認。
5. **W4 以降: 群ごとに、同じ縦通し**（案: Disconnect BT、Calibrate gyro／sixaxis wheel → Launch Program → Load Profile → Check Battery → Multi-action）。簡単で、危険の少ないものから。各群で、実機確認と、優先順位・抑制の規則の確認。
6. **最終 W: 文書**。

## 6. テスト
- 往復（保存→読み込み）: 全種別、境界値（空の文字列、長いパス、0 秒、大きな秒数）、不正値（壊れた XML の節は、読み込みで無視し、ほかの割り当てを壊さない）。
- 束縛: 種別ごとに、Controls の単体ボタンから、SA と同じ出力アクションが動くこと（偽の窓口で）。方針の差（毎フレームの消去、Fresh-press ガード）の固定。
- 優先順位・抑制の規則（R1〜R10）が、SA 専用の割り当てにも、そのまま成り立つこと。
- 既存のプロファイル（SA 専用の割り当てなし）の読み込み・保存が、変わらないこと。

## 7. 実機（画面）の確認項目（群ごと）
- 割り当てエディターで、その種別を選んで、項目を入力し、保存、再読み込みで保たれる。
- 割り当てたボタンを押して、SA と同じ動作をする（プログラムが起動する、プロファイルが切り替わる、切断する、電池の色が出る、較正が走る、タップ・長押し・ダブルタップのマクロが出る）。
- 同じボタンに SA の割り当てがあるときの優先順位（SA が優先）と、組み合わせの先に押したボタンの扱い（マスター計画書 §7）。
- プロファイルの保存・Apply・切り替えで、割り当てが保たれる。

## 8. 想定されるリスク
- **押下の判定の違い**: Controls は、ボタンごとに判定の基準（`ControlJudgement`）があり、アナログ入力（スティック、トリガー）の閾値が、SA と異なる。SA 専用の種別を、Controls のスティック・トリガーに割り当てたときの扱い（§9 Q3）。
- **Multi-action**: 入力が、独立した入力アクション（`MultiTapInputAction`。タップ・長押し・ダブルタップの判定）で、Controls 側は `ControlButtonInputAction`。単体ボタンで、`MultiTapInputAction` を使えるか（§9 Q5）。
- **Profile の切り替え**: 切り替え元・復帰（一時プロファイル）の状態を持つ。Controls に割り当てたときの、復帰の扱い。
- **実行の中心部の変更**: W2 の取り出しは、SA の実行経路に触れる。W0 の特性化テストと、実機の回帰（SA の全種別）が、守りになる。

## 9. 確認事項（ユーザーの判断が要る）
- **Q1＝簡単なものから、群ごとに縦通し（確定、2026-10-21）**: Macro の SA 専用の項目 → Disconnect BT／較正 2 種 → Launch Program → Load Profile → Check Battery → Multi-action。
- **Q2＝`DS4ControlSettings.actionType` に `Special` を足し、型のある項目を別のプロパティで持つ（確定、2026-10-21）**。既存の 4 種別（Default／Key／Button／Macro）の処理は、変えない。
- **Q3＝アナログ入力（スティック、L2／R2）に割り当てたときの判定は、SA の閾値を使う（確定、2026-10-21）**。SA と同じ動作にする。
- **Q4＝SA 専用の種別でも、割り当てたボタンの通常の出力は、毎フレーム消す（確定、2026-10-21）**。Controls の割り当ては、すべて、そのボタンの既定の出力を置き換える（R10）。
- **Q5: Multi-action の扱い**: 単体ボタンの Multi-action（`MultiTapInputAction`）を、最後の群で、実現可否を確認してから決める（推奨）。

## 10. 進捗の記録

- **W0（完了 2026-10-21。ビルド・テスト成功）**: 特性化テスト 2 ファイルを追加した（製品コードは変更なし）。`Phase10Step4W0SaBindingTableTests`（`ActionBindingBuilder` の種別ごとの束縛の一覧表: 入力の型・出力アクションの型・方針、窓口がないときに束縛を作らない種別、トリガーのない SA）。`Phase10Step4W0ControlProfileXmlFormatTests`（`Profile.xml` の `<Control>` の読み込み・保存の形式、要素の順序［Button、Macro、Key、Extras、KeyType］、空の `<Control />`、未知の節は読み飛ばし・保存で失われるという互換の事実）。
  - 優先順位・抑制の規則 R10（Controls に割り当てた SA 専用の種別）は、現在は実装がなく再現できない（Step2 W0 の記録のとおり）。群ごとの縦通し（W3 以降）で、動くようになってから、テストを足す。
- **W1（完了 2026-10-21。ビルド・テスト成功）**: 型のある項目と、`Profile.xml` への保存・読み込みを追加した（画面・実行への影響なし）。
  - 新規: `ControlSpecialItem`（種別、秒数、Macro の 3 項目、Program、Profile、BatteryCheck、MultiAction の項目。`Clone`）、`ControlSpecialItemXml`（`DS4Control/Services/` 直下。書き出し・寛容な読み込み）。
  - 変更: `DS4ControlSettings`（`ActionType.Special`、`special`）、`ProfileButtonSaveSection`／`ProfileButtonControlLoadSection`（`<Special>` を `<KeyType>` の後に書く／最後に読む。Special が同じボタンの Button／Macro／Key に優先）、`MappedControl`（一覧の表示）、`ControlSettingsEditTarget`（複製・クリア）。
  - テスト: `Phase10Step4W1ControlSpecialItemTests`（14 件）。W0 の `AnUnknownSection_…` は、未知の節の例を `<Special>` から `<Future>` に変えた（`<Special>` を読むようになったため）。
  - 配置の判断: `ControlSpecialItemXml` は節ではなく補助部品なので、`ProfileSections/` ではなく `Services/` 直下に置く（`Phase8Step4bW11LengthGuardTests` が、`ProfileSections/` 内を `internal sealed class` 1 つに限っているため）。
  - 次: W2（`ActionBindingBuilder.Create` から、SA の出力アクションを作る共通部品を取り出す。挙動は変えず、W0 の表のテストで守る）。指示があるまで始めない。
