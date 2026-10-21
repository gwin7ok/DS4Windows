# フェーズ10 進捗管理文書: 割り当て編集画面の統合（Controls タブと SpecialActions タブ）

最終更新日: 2026-10-22  
状態: **Phase10 の計画した Step は、すべて完了（2026-10-22）。Step1〜Step5、Step3b、Step4c、Step4d、統合 Step（Step6）、W7・W8 相当。残りは、先送りの実機確認（台帳 §27〜§30）。結果と差は `Phase10-Integration-Target-Model.md` §10〜§12、完了報告書は `Phase10-Step6-Completion-Report.md`**
計画書: `Phase10-Assignment-UI-Unification-Plan.md`（方針承認済み。§6 の決定事項 Q1〜Q7、§7 の優先順位と抑制の仕様 R1〜R10）  
Step1: `Phase10-Step1-Plan.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Step1 | マクロの再生オプションの共通部品（Execute Mode、Scan Code）と、Press Key の Scan Code の移動 | **完了**（2026-10-21。W1〜W4 ビルド・テスト成功、実機確認済み） | `Phase10-Step1-Plan.md` |
| Step2 | 共通の編集画面の骨格（ViewModel の分離、Controls／SA の 2 モード化、優先順位・抑制の仕様の特性化テスト） | **完了**（2026-10-21。W0〜W5。ビルド・テスト成功、実機確認済み） | `Phase10-Step2-Plan.md`（§9 に完了の記録、未接続のコードの一覧、Step3・4 への引き継ぎ） |
| Step3（繰り上げ） | **Controls タブを割り当てエディターへ接続する**（選択ダイアログ `BindingWindow`＝出力ピッカーの統一、6 か所の呼び出しを、左ペイン全体に重ねる割り当てエディターへ切り替え） | **完了（2026-10-21）**。完了の記録は `Phase10-Step3-Plan.md` §10。実機確認の台帳は `Phase6-Step11-RealDevice-Verification-Checklist.md` §15（P10-21 は未確認） | `Phase10-Step3-Plan.md` |
| Step3b（追加） | Touchpad タブとジャイロタブを左ペインへ移す。Axis Config の 4 つの入口を、Controls タブの一覧へ加える（レイアウトのみ） | **完了（2026-10-21）**。完了の記録は `Phase10-Step3b-Plan.md` §9。実機確認の台帳は `Phase6-Step11-RealDevice-Verification-Checklist.md` §16（P10-26 まで確認済み。P10-21 のみ未確認） | `Phase10-Step3b-Plan.md` |
| Step4 | 保存形式の拡張と、Controls での SA 専用の種別の実行（型のある項目の定義、`Profile.xml` への保存、出力アクションの作り方の共通化、画面の解除を、群ごとに縦通し） | **W6 で締め**（2026-10-21、ユーザー決定）。W0〜W6 の実装は完了（Macro の 3 項目、Disconnect BT、較正 2 種、Launch Program、Load Profile）。**W7（Check Battery）・W8（Multi-action。Q5 を含む）は、統合後へ繰り延べ**。実機確認は未確認のまま進む（P10-39、44、47〜51、52〜57。仕様変更・統合の後に回帰としてまとめて確認） | `Phase10-Step4-Plan.md`（§10「Step4 の締め」） |
| Step4c（仮称・新設） | **仕様変更**: Controls と SA の共通度を高める（押下判定の閾値、Fresh-press ガード、元の出力の扱い、ログ、プロファイルの固定 ID、Hold の共有、検証の揃え）。項目ごとに、ユーザーの承認を得て、採否を決める。実機の挙動が変わるため、構造の統合とは別の Step | 計画書 `Phase10-Step4c-Plan.md` を作成。採否は確定（項目 1・2・4・7・9 を採用、10［SA の Extras］は設計のみ採用で実装は統合。3・6 と E・D・G は見送り。5 は Step4d へ）。W0（特性化テスト）完了（2026-10-21）。W1（Fresh-press ガードの表）完了（2026-10-21。ビルド・テスト成功）。W2（全種別で立ち上がりと離したときのログ）完了（2026-10-21。`Debug ALL` 成功。実機確認 P10-58〜62 は先送り）。W3（必須の値がないエラーで保存を止める。赤い枠）完了（2026-10-21。`Debug ALL` 成功。実機確認 P10-63〜68 は先送り）。W4（デジタルな出力の押下判定を SA の閾値へ）完了（2026-10-21。`Debug ALL` 成功。実機確認 P10-69〜73 は先送り）。W5（入力の集合を同じにする。SA に LS／RS Outer、Controls に Touch Started／Ended）完了。最終 W（文書）完了。**Step4c は完了**（2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1902 件と 11 件が成功）。実機確認は、台帳 §27［統合版。P10-79〜89］を、ユーザー判断で先送り（統合 Step の後に回帰として確認）。次は Step4d（固定 ID）と統合 Step の計画書 | `Phase10-Step4c-Plan.md` |
| Step4d（新設） | **固定 ID**: プロファイルと SA の両方で、名前をキーにしているもの（`ProfileActions`、Load Profile の切り替え先［SA と Controls］、リンクプロファイル、自動プロファイルほか）を、固定 ID をキーにする。名前変更の追従（`ReplaceActionReference`、`ReplaceProfileSwitchTarget`、`ReplaceControlProfileSwitchTarget`）が不要になる。既存ファイルの移行、ID の重複、取り込みの扱いを設計する | 計画書を作成（2026-10-21）。決定事項 Q1〜Q6 は確定（Q5 は、内部の実行時の状態も ID をキーにする、に変更）。W0〜W7 と最終 W の構成。**W0（特性化テスト）完了**（2026-10-21。`Debug ALL` 成功）。**W1（プロファイルの ID）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-90 は先送り）。**W2（SA の ID）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-91 は先送り）。**W3（保存する参照を ID に）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-92 は先送り）。**W4（スロット・リンク・自動プロファイルを ID に）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-93 は先送り）。これで保存する参照はすべて ID が主になった。**W5a（登録簿のメモリ索引）完了**（2026-10-21。`Debug ALL` 成功。実機確認なし）。W5 は案 1（保管を ID にし、名前は ID から引く窓口）で進める。**W5b（スロットの保管を ID に）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-94 は先送り）。**W5c（切り替え・復帰の実行経路を ID に）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-95 は先送り）。**W5d（自動プロファイルとリンクプロファイルを ID に）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-96 は先送り）。これでプロファイル側の実行時の名前は、すべて ID の窓口になった（W5 完了）。**W6'（SA の実行時は名前のまま。理由を計画書に記録）決定**（2026-10-21）。**W7（追従のコードの撤去。案 A）完了**（2026-10-21。`Debug ALL` 成功。実機確認 P10-97 は先送り）。最終 W（文書）完了。**Step4d は完了**（2026-10-21。`Debug ALL` 成功: 警告 0・エラー 0、テスト 1998 件と 11 件が成功）。実機確認は台帳 §27［P10-90〜97］を先送り。次は統合 Step の計画書 | `Phase10-Step4d-Plan.md` |
| （完成形の文書） | 完成形のモデル図、差の一覧、統合 Step の骨子（案）。ユーザーの承認待ち | 2026-10-21 作成 | `Phase10-Integration-Target-Model.md` |
| Step6' （旧 Step6 を前倒し） | **統合**: SA の文字列の受け渡しを型のある項目へ（Q7）、`ActionBindingBuilder`／`ControlBindingBuilder` の統合、暫定の構造（`forControls`、`ControlJudgement`、`BuiltSpecialTypes`、`ControlSpecialEditors`）の整理、`SpecialActionEditor` → `AssignmentEditor` の改名、旧 `BindingWindow` の Full／Keyboard の撤去。最初に、Controls にも複数ボタンの組み合わせを許すか（8）を、ユーザーに確認する。SA にも Extras を付けられるようにする（Step4c の項目 10）も、この Step で実装する | **完了（2026-10-22）**。I1〜I7 の全 Wave が完了（`Debug ALL` 成功: 警告 0・エラー 0、テスト 2086 件と 11 件）。結果と差は `Phase10-Integration-Target-Model.md` §10、完了報告書は `Phase10-Step6-Completion-Report.md`。実機確認は先送り（台帳 §27、§28）。次は W7・W8 相当（Check Battery、Multi-action を Controls へ。Q5 を含む）→ Step5 |
| （W7・W8 相当） | Check Battery と Multi-action を Controls へ（統合後は、種別の追加が 1 か所で済む）。Q5 の判断を含む | **完了（2026-10-22）。`Debug ALL` 成功: 警告 0・エラー 0、テスト 2102 件と 11 件。結果は `Phase10-Integration-Target-Model.md` §11。実機確認は先送り（台帳 §29）** | |
| Step5 | SA 側のアナログ出力（旧 Step4b） | **完了（2026-10-22）。`Phase10-Step5-Plan.md`。`Debug ALL` 成功: テスト 2150 件と 11 件。実機確認は先送り（台帳 §30）** | |

## 2. Step1 の Wave

| Wave | 内容 | 状態 |
|---|---|---|
| W1 | `MacroPlaybackOptions`（再生オプションの ViewModel）と単体テスト | 完了 |
| W2 | `MacroPlaybackOptionsPanel` と、Controls の `RecordBox` への適用 | 完了（実機確認済み） |
| W3 | SA の Macro タブ（Execute Mode ドロップダウン）と、SA から開く記録画面のオプション非表示 | 完了（実機確認済み） |
| W4 | SA の Press Key に Scan Code、選択ダイアログ（`ForPressToggle`）の Toggle／Scan Code の非表示、ラベルを「Output Key/Mouse/Button」に変更 | 完了（実機確認済み） |
| W5 | 文書 | 完了 |

## 3. Step1 の実機確認の結果（2026-10-21）

- W2（Controls のマクロの Execute Mode／Scan Code の設定・保存・再読み込み）: 問題なし（Scan Code の不具合の是正後）。
- W3（SA の Macro タブの Execute Mode、記録画面にオプションが出ない）: 問題なし。
- W4（SA の Press Key の Scan Code。選択ダイアログに Toggle／Scan Code が出ない。ボタン選択時の無効化。Controls の選択ダイアログは従来どおり。ラベル）: A-1〜A-7、B、C のすべてで問題なし。

## 4. 実機確認で見つかった、以前からの不具合（Step1 で是正）

| # | 不具合 | 原因 | 是正 |
|---|---|---|---|
| 1 | Controls で、記録画面の Scan Code にチェックを入れてマクロを保存しても、開き直すと外れる | `BindingWindowViewModel.PrepareSaveMacro` が `hasScanCode` を渡していなかった | `PrepareSaveMacro` で渡す（W2。`Phase10Step1W2BindingWindowMacroScanCodeTests`） |
| 2 | SA の Press Key で、Scan Code を保存しても、開き直すと外れる | `ActionsDTO` が、コンストラクターで「Scan Code」を除かれた `details` だけを Actions.xml に書いていた。あわせて、保存後の実行時状態のリセットが「65 Scan Code」を読めず飛ばされていた | `ActionsDTO` で Details に「 Scan Code」を足して書く。リセットは先頭の語だけを読む（W4。`Phase10Step1W4KeyActionScanCodePersistenceTests`）。**暫定。Step3 で、型のある独立した項目に置き換える（Q7）** |

### 持ち越し C-5（マウスボタンの Toggle の制限）

- 調査済み（`Phase10-Assignment-UI-Unification-Plan.md` §6 Q4）。**仕様ではなく編集画面の欠落**（`OutBinding.WriteBind` が、Key の分岐でしか Toggle を書かない）。
- 対応: Step4 で、選択ダイアログの Toggle を削除し、共通画面の Switch Mode（Key と Button の両方に効く）に統一する。Step9 の B-11 の記録（「パッドのボタン A では Toggle が設定される」）との食い違いは未解決で、Step2 の往復変換テストで再現して確定する。

## 5. 残した整理の対象（Phase9-I の C-4 と、翻訳の整理）

- 使われなくなった言語リソース: `SpecialActionEdit.RepeatWhileHeld`（4 言語）。
- ラベル「Output Key/Mouse/Button」（`SpecialActionEdit.PressKey`）は、全言語で英語を表示するため、オランダ語・ロシア語・簡体字中国語の旧訳（Press/Toggle key）の項目を削除した（既定の英語へフォールバック）。翻訳を戻す場合は、Phase9-I の翻訳整理で行う。
- SA の「Execute Mode:」のラベルは、翻訳キーを持たない直書き（`Switch Mode:` と同様）。

## 5b. Step2 の結果（2026-10-21）

- W0: 優先順位・抑制の仕様（条件 1・2、R2〜R7、R9）は、現行の挙動と**一致**していた。R1・R8・R10 は、時間依存・切替・保存先の都合で、固定していない（`Phase10-Step2-Plan.md` §8）。
- W1〜W4: 共通の編集画面の骨格を作った。SA のモードは従来と同じ動作。Controls のモードは、骨格と、設定との変換まで作ったが、**呼び出し元がない**（Step4 で接続）。
- 実機確認で見つけた、以前からの不具合 1 件を是正（無効な入力で、保存を押すと、編集画面が空になる）。
- 持ち越し C-5（Button の Toggle）: 共通の編集画面は、Key と Button の両方に Toggle を書ける（W4）。従来の選択ダイアログの欠落は、Step4 で、ダイアログの Toggle を削除して解消する。

## 6. 次の作業

- Step4（保存形式の拡張と、Controls での SA 専用の種別の実行）の、実施の判断と個別計画書の作成。Controls のモードで暫定的に隠している項目（Macro タブの 3 つのチェックボックス）と、グレーアウトの種別を、順に出す。最終形（共通の割り当て定義と、束縛を作る builder の統合）の進め方も、あわせて決める。
- 未確認の台帳の項目（P10-21: 出力ピッカーでの Extras の設定と、割り当てエディターの Save／Cancel での反映・破棄、既存プロファイルの回帰）の、実機での確認。
