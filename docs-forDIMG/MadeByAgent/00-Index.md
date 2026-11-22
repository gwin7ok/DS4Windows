# MadeByAgent 文書の索引

作成日: 2026-11-20

完了した Phase の計画書・完了報告・調査メモ（Phase 28 以前の 406 件）は `Archive/` にある（読むだけ。内容は変えない）。ここには、現行の規約・台帳・手順書と、未完了または近い Phase の文書を置く。

## 規約・台帳・手順
| 文書 | 内容 |
|---|---|
| `../../.github/copilot-instructions.md` | エージェントの作業規約。変更履歴は `Copilot-Instructions-Changelog.md` |
| `Known-Issues-Ledger.md` | 未解決の不具合の台帳 |
| `Release-Backlog.md` | リリース待ちの項目 |
| `Device-Check-Checklist.md` | 実機確認の一覧 |
| `Log-Policy.md` | ログの方針（レベル・文言・例外の扱い） |
| `Cli-Regression-Check-Commands.md` | CLI の回帰確認の手順 |
| `Phase7-Residual-Ledger.md` | 残る静的な状態の目録（規約 2.1） |
| `PhaseX3-Glossary.md` | 日本語の用語集（規約 2.3） |
| `Phase15-0-Test-Rename-Map.md` | テストの改名の対応表（規約 4 章） |
| `Phase39-54-Record.md` | Phase39〜54 の記録 |

## モデル図
`../Model-Diagram/01-Structure.md`（現状の構造）、`02-Extension-Guide.md`（機種・出力の種類を足す手順）、`03-Lifecycle-and-DI.md`（登録と寿命）、`Ideas.md`（未実装の構想）。旧文書は `../Model-Diagram/Archive`。

## 計画書（現行・近い Phase）
- 規約が参照する計画書: `Phase5-Plan.md`（ガードレール）、`Phase6-Status.md`、`Phase6-Step10-Plan.md`、`Phase6-Step10b-Plan.md`、`Phase14-Plan.md`、`Phase17-Plan.md`。
- Phase29〜40、Phase34 の V2 の 3 件、PhaseX／X2／X3／X4。
- コードレビュー（`../Code-Review-2026-11-19/`）に基づく PhaseRV1〜RV48 の計画書と調査メモ（`PhaseRV*`）。
- 仕様との突き合わせの記録 `PhaseRV38-Spec-Crosscheck.md`（RV38〜RV42 の追補を含む）。RV39〜RV48 は、実施のあとに記録として計画書を作成した（小さな Phase のため）。

## その他の調査メモ
`AutoProfile-NoController-Investigation.md`、`K7b1-Investigation.md`、`Notification-Legacy-Path-Cleanup-Report.md`、`R2-MacroToggleRepeat-Plan.md`、`Reconnect-Profile-PerController-Fix-Plan.md`、`Repository-Review-2026-11-05.md`、`Structure-Review-2026-11.md`、`Xbox-Input-Research.md`、`Xbox-Probe/`、`tools/`。

## 点検
文書の参照が実在するかは `python -I scripts/audit/Check-DocLinks.py`（切れが 0 件）。
