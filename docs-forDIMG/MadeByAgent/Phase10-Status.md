# フェーズ10 進捗管理文書: 割り当て編集画面の統合（Controls タブと SpecialActions タブ）

最終更新日: 2026-10-21  
状態: **Step1・Step2 完了（2026-10-21）。Step3 以降は未着手（Step3 の個別計画書を作り、承認を得てから着手する）**  
計画書: `Phase10-Assignment-UI-Unification-Plan.md`（方針承認済み。§6 の決定事項 Q1〜Q7、§7 の優先順位と抑制の仕様 R1〜R10）  
Step1: `Phase10-Step1-Plan.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Step1 | マクロの再生オプションの共通部品（Execute Mode、Scan Code）と、Press Key の Scan Code の移動 | **完了**（2026-10-21。W1〜W4 ビルド・テスト成功、実機確認済み） | `Phase10-Step1-Plan.md` |
| Step2 | 共通の編集画面の骨格（ViewModel の分離、Controls／SA の 2 モード化、優先順位・抑制の仕様の特性化テスト） | **完了**（2026-10-21。W0〜W5。ビルド・テスト成功、実機確認済み） | `Phase10-Step2-Plan.md`（§9 に完了の記録、未接続のコードの一覧、Step3・4 への引き継ぎ） |
| Step3（繰り上げ） | **Controls タブを割り当てエディターへ接続する**（選択ダイアログ `BindingWindow`＝出力ピッカーの統一、6 か所の呼び出しを、左ペイン全体に重ねる割り当てエディターへ切り替え） | **完了（2026-10-21）**。完了の記録は `Phase10-Step3-Plan.md` §10。実機確認の台帳は `Phase6-Step11-RealDevice-Verification-Checklist.md` §15（P10-21 は未確認） | `Phase10-Step3-Plan.md` |
| Step3b（追加） | Touchpad タブとジャイロタブを左ペインへ移す。Axis Config の 4 つの入口を Controls タブへボタンとして追加（レイアウトのみ。ユーザーの方針、2026-10-21） | 計画書は確定（Q1〜Q3 回答済み）。ユーザーの承認待ち | `Phase10-Step3b-Plan.md` |
| Step4 | 保存形式の拡張と、Controls での SA 専用の種別の実行（旧 Step3＋旧 Step5）。Step3 のあとで、実施するか・後回しにするかを判断 | 未着手 | |
| Step5 | SA 側のアナログ出力（旧 Step4b） | 未着手 | |
| Step6 | SA の文字列の受け渡しを型のある項目へ（Q7）、旧画面の撤去・改名、モデル図・文書の更新、完了報告書 | 未着手 |  |

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

- Step3b（Touchpad タブとジャイロタブを左ペインへ移す。Axis Config の 4 つの入口を Controls タブへボタンとして追加）の個別計画書を作る。確認事項（レイアウトの幅、Axis Config のボタンの置き場所）をユーザーが決めてから着手する。
