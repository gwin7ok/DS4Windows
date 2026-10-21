# Phase10-Step6 計画書: 統合 Step（共通の割り当て定義と、builder の 1 本化）

作成日: 2026-10-21
状態: **案（ユーザーの承認後、I1 から着手する。Wave ごとに設計を説明し、承認を得てから実装する）**
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
| I2 | SA の型のある項目（`SpecialActionItem` 相当）。`SpecialAction` ⇄ `Actions.xml` を型のある項目で往復。旧書式（`details`／`extra` 文字列）の読み込みを維持 | なし（旧ファイルを読めること、新しく保存したものを旧版が読み飛ばす点は計画書に明記し、承認を得る） |
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

- [ ] I1 〜 [ ] I7（未着手。承認待ち）
