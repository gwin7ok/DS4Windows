# Phase10-Step6 計画書: 統合 Step（共通の割り当て定義と、builder の 1 本化）

作成日: 2026-10-21
状態: **承認済み（2026-10-21）。I1 完了。I2 以降は、Wave ごとに設計を説明し、承認を得てから実装する**
前提: Step4（W0〜W6）、Step4c、Step4d は完了。目標モデルは `Phase10-Integration-Target-Model.md`（§3 完成形、§5 骨子）。
位置づけ: 旧 Step6 の前倒し（2026-10-21 のユーザー決定）。この Step の後に、W7・W8 相当（Check Battery、Multi-action を Controls へ。Q5 を含む）→ Step5（SA 側のアナログ出力）の順に進める。

## 1. 目的

Controls タブと SpecialActions（SA）タブのどちらで設定したかに依存せず、**割り当ての解釈・出力の作り方・方針が 1 か所**にあり、暫定の構造（`forControls`、`ControlJudgement`、`BuiltSpecialTypes`、`ControlSpecialEditors`）がない状態にする。ユーザーから見える挙動は、SA の Extras（I5）を除き、変えない。

## 2. 決定事項（I0。2026-10-21、ユーザー確認済み）

| 項目 | 決定 |
|---|---|
| 元の出力の消し方 | 「トリガー数」ではなく**定義の由来（Controls／SA）**で決める（`BindingPolicy.ErasesOutputEveryFrame` として方針の表に残す） |
| Controls の複数ボタン | **許さない**。由来が Controls の定義は、トリガーが常に 1 つ。`Profile.xml` の保存形式は変えない |
| SA の Extras | **全種別に付けられる。その SA が成立している間だけ有効**。動作は Controls の Extras と同じ。同じボタンで重なったら SA が勝つ |
| 保存形式 | 2 つのまま（`Profile.xml`、`Actions.xml`）。SA の文字列の受け渡しは型のある項目へ替えるが、旧書式の読み込みは維持する |
| Multi-action の未設定マクロ、「No unload triggers」の表示 | Multi-action は W8 相当で決める。「No unload triggers」は I6 で表示ごと削除するか、そこで提案する |

## 3. 現状（規模）

- `ControlBindingBuilder`（533 行）と `ActionBindingBuilder`（126 行）が別。出力の作り方は `SpecialActionOutputFactory.Create(device, sa, index, forControls)`（154 行）が共有しているが、`forControls` で方針を分岐している。
- Controls 側は `BuiltSpecialTypes`（5 種別）に入る種別だけを新経路で作る。
- UI は `SpecialActionEditor`（692 行）が `ControlSpecialEditors`（23 行）・`ControlSettingsEditTarget`（420 行）を介して両方を扱う。旧 `BindingWindow`（976 行）に Full／Keyboard のモードが残る。
- 参照するファイルは、本体 14 本・テスト 13 本（`forControls`／`ControlJudgement`／`BuiltSpecialTypes`／`ControlSpecialEditors`）。

## 4. 進め方

守りにするテストは Phase8 の特性化テスト、Phase10 Step4 系・Step4c W0 の方針の表、永続化のゴールデン。**各 Wave の終わりで `Debug ALL` を 0 警告・0 エラー・全テスト合格にする**。新しい静的な窓口は作らない。1 ファイル 1 型。コミットはユーザーが行う。

| Wave | 内容 | 挙動の変化 |
|---|---|---|
| I1 | `AssignmentDefinition`（共通の割り当て定義: 種別、型のある項目、トリガー 1〜n、Extras、由来、ID）と `AssignmentOrigin`（Controls／SA）を作る。`ControlSpecialItem` を土台にする | なし（型の追加のみ） |
| I2 | **（2026-10-22 変更）** SA の型のある項目（Q7）。保存形式は変えず、`SpecialAction` ⇄ 型のある項目の変換部品（`SpecialActionItemMapper`）を作る。旧書式の文字列を同じ項目として読める | なし |
| I3 | 読み込みの部品 2 つ（`ControlsAssignmentReader`、`SpecialActionAssignmentReader`）。builder の入力を定義にする（出力は同じ束縛） | なし |
| I4 | builder を `AssignmentBindingBuilder` 1 本に統合。`forControls` → 由来、`ControlJudgement` → 出力の種類（デジタル／アナログ）の性質、方針の表を種別 × 由来で引く。立ち上がりのログをエンジンの 1 か所にまとめる（SA の出力アクション 9 クラスの重複を除く） | なし |
| I5 | SA の Extras を実装（保存・読み込み・UI・束縛）。重なりは SA が勝つ | **あり（新機能）**。実機確認 |
| I6 | 暫定の構造の撤去（`BuiltSpecialTypes`、`ControlSpecialEditors`、残りの分岐）。`SpecialActionEditor` → `AssignmentEditor` の改名。旧 `BindingWindow` の Full／Keyboard の撤去。ラチェット・ガードテストの更新 | なし（旧 BindingWindow の撤去対象は、入口を調べて承認を得る） |
| I7 | 文書（モデル図 `03`、`Phase10-Integration-Target-Model.md`、`Phase10-Status.md`）、台帳 §22 の回帰の整理、完了報告書 | — |

## 5. リスクと対策

- **挙動のずれ**: Wave ごとに特性化テストで押さえ、方針の表（種別 × 由来）を I4 の前に全件のテストへ落とす。
- **Actions.xml の互換**（I2）: 旧書式は読める。新書式で保存した後に旧版へ戻すと、型のある項目は読み飛ばされる（下位互換の喪失）。I2 の設計で、旧書式を併記して保存するかを選んでもらう。
- **巨大ファイル**: `Mapping.cs` などには触れない見込み。触れる場合はピンポイントで置換する。
- **ラチェット**: `Step10FourGlobalRatchetGuardTests` と、`Phase10Step4dW0RuntimeKeyRatchetTests` の件数は、型の追加・分割の際にファイル別に引き継ぐ。

## 6. 実機確認

台帳 §27（先送り分 P10-79〜P10-97）は、I7 の後に回帰として行う。I5 の新機能（SA の Extras）は、I5 の完了時に 1 項目ずつ説明して確認する（先送りも可）。

## 7. 進捗

- [x] **I1 完了（2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 2005 件と 11 件が成功）**: 新規 `Actions/AssignmentOrigin.cs`、`Actions/AssignmentDefinition.cs`（Controls 固有の種別は定義に入れない設計。ユーザー決定）、テスト `Phase10Step6I1AssignmentDefinitionTests`（7 件）。定義は I3 まで未接続（TODO コメント付き）。
- [x] **I2 完了（2026-10-22。`Debug ALL` 成功: 警告 0・エラー 0、テスト 2022 件と 11 件が成功）**: 設計変更（ユーザー決定）＝Actions.xml は種別ごとの要素で保存済みのため、**ファイル形式と `SpecialAction` は変えず**、変換部品を作った。新規 `Actions/SpecialActionItemMapper.cs`（`ToItem`＝SA → 項目、`ToSpecialAction`＝項目 → 合成の SA。Program の `$hidden`、Battery の「,」「|」、MultiAction の 3 マクロ、Profile の ID、Macro の 3 項目、遅延のみの 3 種）。`ControlSpecialItem` に `BatteryNotification`/`BatteryLightbar` を追加（プロファイル XML には書かない。Check Battery を Controls へ移す作業［W7 相当］で保存を足す。TODO 付き）。`AssignmentDefinition` に `Macro`（手順）と `KeyType` を追加。テスト `Phase10Step6I2SpecialActionItemMapperTests`（17 件）と I1 のテスト 1 件。変換部品は I3 まで未接続（TODO 付き）。
- [x] **I3 完了（2026-10-22。`Debug ALL` 成功: 警告 0・エラー 0、テスト 2031 件と 11 件が成功）**: 新規 `Actions/SpecialActionAssignmentReader.cs`（SA → 定義。Key/Button の解釈済みの値、Macro の手順・keyType、型のある項目）、`DS4Control/Services/ControlsAssignmentReader.cs`（Controls の SA 専用の種別 → 定義と、合成の SA）。`ISpecialActionOutputFactory` に定義を受ける `Create(device, definition, source, index)` を追加（方針の分岐は `forControls` ではなく定義の由来）。旧 `Create(device, sa, index, forControls)` は、リーダー経由で新しい `Create` を呼ぶ互換の入口にした（I4 で削除。TODO 付き）。`ActionBindingBuilder.Create` と `ControlBindingBuilder` の Special の経路を新しい `Create` へ替え、`CreateSpecialIdentity` を削除（`SpecialActionItemMapper.ToSpecialAction` が代わり）。MultiAction のマクロは項目の 3 つを「/」でつないで渡す（正常なデータでは旧と同じ。壊れた数値のマクロは、旧は再生タスクが失敗して何も再生されず、新は空で何も再生されない）。テスト `Phase10Step6I3AssignmentReaderTests`（9 件）。既存テストは変更なしで合格。出力アクション 9 クラスは、引き続き実行時の状態の入れ物（`SpecialAction`）を受け取る。
- [ ] **I4 進行中（設計承認 2026-10-22。I4a→I4b→I4c の 3 段階）**:
  - **設計（承認済み）**: 骨子の「builder を 1 本に」は、そのままでは成り立たない。Controls 側は、ボタンごとに Extras を通常の割り当ての前に並べ、一部でも扱えなければ dcs 全体の束縛を作らない、という順序と門番の規則を持つ（Phase8 の特性化テストが守る）。そこで、①束縛の組み立て（出力・方針・入力・`TriggerBinding` への包み）を新規 `AssignmentBindingBuilder` 1 つにし、②一覧を歩く部分は 2 つのまま（`ActionBindingBuilder`＝SA のリスト、`ControlBindingBuilder`＝Controls の dcs。共通の種別は `AssignmentBindingBuilder` に任せる）、③Controls 固有の部品（Pad、Mouse、AnalogMouse、Extras、素通し）は `ControlBindingBuilder` に残す。
  - **I4a**: `ControlJudgement`（Assignment／Trigger）を、出力の種類から決まる性質 `ControlOutputNature`（Digital／Analog）に置き換え、決める箇所を `ControlOutputNatureTable` の 1 か所にする。判定の中身は変えない。
  - **I4b**: Controls の Key と Macro も `ControlsAssignmentReader` と factory の経由にする。方針は種別 × 由来の 1 つの表から引き、`forControls` を完全に撤去する。Key の別名キーとログの抑制は定義の値で運ぶ。Controls 由来の Key／Macro は、`actionRepository` を渡さない現状の動作を保つ。
  - **I4c**: `AssignmentBindingBuilder` を作り、Special／Key／Macro の `TriggerBinding` の組み立てを集めて 2 つの builder から呼ぶ。互換の旧 `Create(device, sa, index, forControls)` を削除し、テストの呼び出し約 34 か所を機械的に置き換える。`ControlBindingBuilder.TryInterpretSpecial` の判定は、I6 の `BuiltSpecialTypes` 撤去まで残す。
  - **骨子から外した項目（承認済み）**: 「立ち上がりのログをエンジンの 1 か所にまとめる」（SA の出力アクション 9 クラスの重複する 4 行の整理）は I4 から外し、別の整理として後に回す。理由: 挙動の利得がなく、ログの文言とレベルを 9 クラスと関連テストで 1 つずつ守る必要があり、ログ維持の規約（2.3）に触れるリスクのほうが大きい。
  - [x] **I4a 実装済み（2026-10-22。ビルド・テストは未確認）**: `ControlJudgement.cs` → `ControlOutputNature.cs`（`git mv`）、新規 `ControlOutputNatureTable.cs`（`ForKind`、`ForPad`）。`ControlButtonInputAction.Judgement` → `OutputNature`、`ControlBindingBuilder` は表から引く。既存テスト 6 ファイルは名前の置き換えのみ。テスト `Phase10Step6I4aOutputNatureTableTests`（11 件）。
  - [x] **I4b 実装済み（2026-10-22。ビルド・テストは未確認）**: `FreshPressGuardTable.For(origin, kind, toggle)`（種別 × 由来の表）を追加。`SpecialActionOutputFactory` は、方針の引数を `forControls` から由来（`AssignmentOrigin`）に替え、Controls 由来の Key／Macro は `actionRepository` を渡さない（従来の Controls の束縛と同じ。ログの index と ActionEntries は SA 由来のみ）。窓口が未接続の種別（Button、Macro、Program、Profile）は null を返す（コンストラクターの必須の検査は公開版に残し、`ForControlsKeyAndMacro` が最小の部品を作る）。`ControlsAssignmentReader` に `TryReadKey`／`TryReadMacro` を追加（旧 `AddKey`／`AddMacro` の合成の名前・項目と同じ）。`ControlBindingBuilder` の Key／Macro は、読み込みの部品と出力の部品を通す（旧 `AddKey`／`AddMacro` を削除。出力の部品が渡されないときは、最小の部品を使う）。互換の入口 `Create(device, sa, index, bool forControls)` の `forControls` 引数は、テストの呼び出しが残るため I4c まで残す。テスト `Phase10Step6I4bControlsKeyMacroDefinitionTests`（10 件）。
- [ ] I4c・I5〜I7（未着手。I4c 以降は指示があるまで始めない）
