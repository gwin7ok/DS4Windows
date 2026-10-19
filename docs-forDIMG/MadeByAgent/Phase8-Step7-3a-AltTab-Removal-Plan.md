# Phase8-Step7-3a 計画書: Alt-Tab マクロの特別扱いと「Cycle Programs」プリセットの削除（機能廃止）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-3-Plan.md`（7.3 Macro。本計画の結論で Q1 が不要になる）、`.github/copilot-instructions.md` 2.2（機能廃止はユーザーの明示的な承認が要る）、2.4  
状態: **Q1〜Q4 を推奨どおり承認（2026-10-19）。W1 実装済み（ビルド・テストの結果待ち）**。廃止の要望: ユーザー（2026-10-19。「Alt+Tab の特別扱い自体と、プリセットの CycleProgram を削除したい」）

---

## 1. 目的

マクロの手順が Alt-Tab の並びで始まるときだけ働く特別扱い（`AltTabSwapping`／`AltTabSwappingRelease`）と、その特別扱いを使うために作られた「Cycle Programs」プリセットを削除する。マクロの再生は、すべて通常の経路（手順どおりにキーを押して離す）になる。

- これは**機能の廃止**である（規約 2.2 の例外。ユーザーの明示的な承認による）。下記の「§3 挙動の変化」を、承認の前提として示す。
- 7.3 Macro（Phase8-Step7-3-Plan.md）の前に行う。削除により、7.3 の Q1（押している間の `EndMacro`）が不要になり、`IMacroPlayer.WhileHeld`／`IMapping.EndMacroDirect` の変更をしなくて済む。

## 2. 調査: 削除の対象

### 2.1 特別扱い（`DS4Windows/DS4Control/Mapping.cs`）

| 対象 | 場所 | 内容 |
|---|---|---|
| フィールド | 793〜795 行 | `altTabDone`、`altTabNow`、`oldAltTabNow` |
| 再生の分岐 | `PlayMacroTask` の 5551〜5570 行 | マクロが `164/9/9/164` か `18/9/9/18` で始まるとき、手順を再生せず `AltTabSwapping(wait, device)` を呼ぶ。`wait` は、手順の末尾の数値（300 以下か 65535 超なら 1000、それ以外は −300） |
| メソッド | `AltTabSwapping`（5948 行〜）、`AltTabSwappingRelease`（5972 行〜） | 1 回目は Tab を押す。2 回目以降は、`wait` ミリ秒ごとに Tab を押して離す。離す側は、Tab と左 Alt を離す（最後の送出から 10 ミリ秒以上たったとき） |
| 離す判定 | `EndMacro` の 3 つのオーバーロード（5921〜5946 行）の先頭 | Alt-Tab の並びで、`altTabDone` が偽なら `AltTabSwappingRelease` を呼ぶ |
| 毎フレームの呼び出し | `MapCustomAction` の Macro の分岐の 4733 行 | トリガーを押している間、`RepeatMacro` でなければ `EndMacro(device, macroControl, action.macro, None)`。Alt-Tab の判定を消すと、何もしない呼び出しになる |

`EndMacro` の残りの処理（`control` が `None` でないとき `macrodone` を偽に戻す）は、Controls タブのマクロが使うため残す。

### 2.2 「Cycle Programs」プリセット

マクロの記録画面（`RecordBox`）の「Preset」メニューにある項目。選ぶと、Alt 押す → Tab 押す → Tab 離す → Alt 離す → 1000 ミリ秒待つ（`18/9/9/18/1300`）の手順を書き込む。この並びが、上の特別扱いを起動する。特別扱いのためだけにある。

| 対象 | 場所 |
|---|---|
| メニュー項目 | `DS4Windows/DS4Forms/RecordBox.xaml` 52 行（`cycleProgPresetMenuItem`、表示名は直書きの「Cycle Programs」） |
| クリックの処理 | `DS4Windows/DS4Forms/RecordBox.xaml.cs` の `CycleProgPresetMenuItem_Click`（495〜504 行） |
| 手順の書き込み | `DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs` の `WriteCycleProgramsPreset`（275〜297 行） |

「LoadPreset」の他の項目（ファイルから読み込む、保存する）は変更しない。

### 2.3 テスト・ドキュメントへの影響

- `MappingMacroPathGlobalReferenceGuardTests`（`DS4WindowsTests`）の検査対象のメソッド名に、`AltTabSwapping`／`AltTabSwappingRelease` が含まれる。この 2 つを外す。
- ソース以外に、`164/9/9/164` を含むものは、`Phase8-Step2-Design.md`、`Phase8-Step7-3-Plan.md` だけ（保存データ・プリセットのファイルには、リポジトリ内に含まれない）。
- 過去の記録（`Phase6-Step3-Plan.md` ほか。履歴として残す）に、Alt-Tab の言及がある。
- `Phase9-Responsibility-Split-Plan.md` の対象に `AltTabSwapping*` がある。削除後は、対象から外す。

### 2.4 これまでの動作（実機の記録）

2026-09-24（Phase6-Step3-6c の実機確認）の観察: Alt-Tab のマクロは、**押している間、Tab が繰り返し送られ、2 つのウィンドウ間でフォーカスが切り替わり続け、離した時点でフォーカスされていたウィンドウで確定した**。ユーザーは、環境依存の見え方と判断した。

## 3. 挙動の変化（承認の前提）

| 場合 | 削除前 | 削除後 |
|---|---|---|
| 手順が `164/9/9/164` か `18/9/9/18` で始まるマクロ（「Cycle Programs」で作ったものを含む）を実行 | 押している間、一定間隔で Tab が繰り返され、離すと確定 | **通常のマクロとして再生**: Alt 押す → Tab 押す → Tab 離す → Alt 離す → 末尾の待ち。1 回の Alt+Tab になる。「押している間繰り返す」（`RepeatMacro`）を設定すれば、繰り返し再生される |
| それ以外のマクロ | 通常の再生 | 変わらない |
| 保存済みのマクロの手順・プロファイル | － | 変更しない（読み込みも、そのまま。警告なし） |
| 「Cycle Programs」メニュー | あり | なくなる（他のプリセットの項目は残る） |

## 4. 作業（各 Wave の後に、ユーザーがビルド・テストを実行）

- **W1: コードの削除（`Mapping.cs`）【実装済み。結果待ち】**: フィールド 3 つ、`PlayMacroTask` の分岐、`AltTabSwapping`、`AltTabSwappingRelease`、`EndMacro` の 3 つのオーバーロードの先頭の判定、`MapCustomAction` の 4733 行の呼び出し（Q4）を削除。`MappingMacroPathGlobalReferenceGuardTests` の対象から 2 つの名前を外す。
- **W2: 画面の削除**: 「Cycle Programs」のメニュー項目、`CycleProgPresetMenuItem_Click`、`WriteCycleProgramsPreset` を削除。
- **W3: 検査とドキュメント**:
  - 新規のガード（`Phase8Step7AltTabRemovalGuardTests`）: ソースに、`AltTabSwapping`、`altTabDone`、`altTabNow`、`oldAltTabNow`、`WriteCycleProgramsPreset`、`CycleProgPresetMenuItem`、`164/9/9/164`、`18/9/9/18` が残らない。`RecordBox.xaml` に「Cycle Programs」が残らない。
  - 挙動のテスト: `18/9/9/18` で始まるマクロを `PlayMacroDirect` で再生すると、通常の再生になり、キーの列（Alt 押す、Tab 押す、Tab 離す、Alt 離す）が記録用の KBM に記録される（記録用の KBM に、押下の記録を足す）。通常のマクロの再生が変わらないこと（既存のテスト）。
  - ドキュメント: `Phase8-Step2-Intent-Decisions.md` に廃止の決定を記録、`Phase8-Step2-Design.md` の該当の記述（§14.3、§14.5、7.3 の行）に追記、`Phase9-Responsibility-Split-Plan.md` の対象から `AltTabSwapping*` を外す、`Phase8-Step7-3-Plan.md` の Q1・W2 を改訂、`Phase8-Status.md` を更新。実機確認の項目を、チェックリストに追加（U-29）。

## 5. 質問（推奨は A）

- **Q1（削除後の、既存の Alt-Tab の並びのマクロの扱い）**: A（推奨）: そのまま読み込み、通常のマクロとして再生する（§3）。保存データは変更せず、警告も出さない。B: 読み込み時に、ログに警告を出す（「Alt-Tab の特別扱いは廃止された」）。C: 読み込み時に、手順を自動的に書き換える（利用者のデータを変えるため、推奨しない）。
- **Q2（「Cycle Programs」の削除範囲）**: A（推奨）: メニュー項目、クリックの処理、`WriteCycleProgramsPreset` の 3 か所だけを削除する（他のプリセットは変更しない）。B: 「Preset」メニュー全体を見直す（範囲外）。
- **Q3（順序）**: A（推奨）: 7.3 Macro の前に削除する。7.3 の Q1（押している間の `EndMacro`）が不要になり、`IMacroPlayer` と `IMapping` を変更しなくて済む。B: 7.3 の後に削除する（7.3 で、いったん `WhileHeld` を実装してから消すことになり、手戻りになる）。
- **Q4（`EndMacro` の呼び出し）**: A（推奨）: 3 つのオーバーロードは、`macrodone` を戻す処理のため残し、Alt-Tab の判定だけを消す。`MapCustomAction` の 4733 行の毎フレームの呼び出しは、何もしなくなるため削除する。B: 呼び出しも残す（何もしない呼び出しが残る）。

## 6. リスクと対応

- **既存の利用者のマクロ**: Alt-Tab の並びのマクロの動作が変わる（§3）。実機で確認する（U-29）。変更前の動作に戻したい場合は、`Cycle Programs` を使っていた利用者が、「押している間繰り返す」の設定を使うことで、近い動作にできる。
- **取り戻し方**: 削除はコミット単位（W1〜W3）で行うため、`git revert` で戻せる。
- **Controls タブのマクロ**: `EndMacro` の `macrodone` の処理を残すため、影響しない（W3 の既存テストと、実機確認で確かめる）。

## 7. 完了条件

- [ ] W1〜W3 のビルド・テストが成功（ユーザーが実行）。全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。
- [ ] ソースに Alt-Tab の特別扱い・「Cycle Programs」が残らない（ガードで確認）。通常のマクロの再生が変わらない。
- [ ] 実機確認 U-29: (1) 通常のマクロ（キー入力、マウスクリックを含む）が従来どおり (2) Alt+Tab の手順を記録したマクロが、1 回の切り替えを行う (3) Controls タブのマクロが従来どおり (4) 記録画面の「Preset」メニューから「Cycle Programs」がなくなり、他の項目が使える。
- [ ] ドキュメント（§4 W3）の更新。

## 8. 次

承認後、W1 を実装する。その後、7.3 Macro（Q1 を削除したうえで、計画書を改訂）に進む。
