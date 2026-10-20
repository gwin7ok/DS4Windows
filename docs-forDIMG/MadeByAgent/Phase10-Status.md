# フェーズ10 進捗管理文書: 割り当て編集画面の統合（Controls タブと SpecialActions タブ）

最終更新日: 2026-10-21  
状態: **Step1 完了（2026-10-21）。Step2 以降は未着手（Step2 の個別計画書を作り、承認を得てから着手する）**  
計画書: `Phase10-Assignment-UI-Unification-Plan.md`（方針承認済み。§6 の決定事項 Q1〜Q7、§7 の優先順位と抑制の仕様 R1〜R10）  
Step1: `Phase10-Step1-Plan.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Step1 | マクロの再生オプションの共通部品（Execute Mode、Scan Code）と、Press Key の Scan Code の移動 | **完了**（2026-10-21。W1〜W4 ビルド・テスト成功、実機確認済み） | `Phase10-Step1-Plan.md` |
| Step2 | 共通の編集画面の骨格（ViewModel の分離、Controls／SA の 2 モード化、優先順位・抑制の仕様の特性化テスト） | 計画書を作成（2026-10-21。Q1〜Q5 承認済み）。W0 から着手待ち。C-5 の調査は完了（§4） | `Phase10-Step2-Plan.md` |
| Step3 | 保存形式の拡張（Controls に SA 専用の種別・項目。SA の文字列の受け渡しを、型のある独立した項目へ。Q7）と、往復変換のテスト | 未着手 | |
| Step4 | Controls 側の各種別の移植。選択ダイアログ `BindingWindow` の統一（Record A Macro、Toggle、Scan Code の削除）、Execute Mode／Scan Code を共通画面の Macro タブへ | 未着手 | |
| Step4b | SA 側のアナログ出力（固定値の強さ） | 未着手 | |
| Step5 | SA 専用の種別を Controls で実行する経路（`ControlBindingBuilder` の拡張） | 未着手 | |
| Step6 | 旧 `BindingWindow`／旧エディタの撤去、モデル図・文書の更新、完了報告書 | 未着手 | |

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

## 6. 次の作業

- Step2 の個別計画書（共通の編集画面の骨格。ViewModel の分離、2 モード化、§7 の優先順位・抑制の特性化テスト。条件 2 の後半［同じフレームで最後のボタンが抑制されるか］の確認から）を作る。承認後に着手する。
