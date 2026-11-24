# DS4Windows プロジェクト作業ルール（Antigravity 用）

## 0. 正本ドキュメントとスキル参照ガイド
- **アーキテクチャ・4層依存・配置・命名・品質基準の正本**:
  新規設計・クラス追加・構造変更・リファクタ時は、**必ず先に `.github/copilot-instructions.md` と `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（構造変更時は `docs-forDIMG/Model-Diagram/01〜03`）を読むこと**。
- **スキル参照（該当作業時のみ読み込むこと）**:
  - ビルド・テスト実行・WPF テストの注意点・Git コミット・実機用 Publish 手順:
    → `.agents/skills/ds4w-build-test-release/SKILL.md`
  - 現在の移行到達点（PhaseRV 以降）・ラチェット/検査テスト・コードレビュー基準:
    → `.agents/skills/ds4w-architecture-and-review/SKILL.md`
  - 画面呼称・用語（スペシャルアクション等）・既知不具合リスト・実機確認手順:
    → `.agents/skills/ds4w-domain-and-checks/SKILL.md`

## 1. 会話・判断・自律進行の基本ルール
- **言語**: 常に日本語で返答・報告・提案を行う。
- **自律進行（2026-11-20 再確認）**: 決定事項（D項目・承認必須の一覧・`.github/copilot-instructions.md` の規約変更・実機確認）がない限り、Step や Phase をまたいでも「次へ進みますか？」と止めずに自動で次へ進む。
- **決定事項の聞き方**: 決定や承認が必要な項目は、一括で本文に番号つきの選択式として提示する。各項目に「平易な説明」と「選択肢ごとのメリット・デメリット・推奨（先頭に推奨マーク）」を必ず書く（決定事項は計画書の決定表へ記録）。
  - *例外（実機確認）*: 実機確認だけは **1 件ずつ** 進め、確認場所・操作・見るものをすべてその質問文の中に完結させて選択式（OK / NG 等）で聞く。
- **既存不具合の扱い**: 既存コードからある不具合を直す場合は、移行前コードと比較し、ユーザーの承認を得てから修正する。
- **無限ループ防止**: 同じエラーやテスト失敗が 2 回連続したら推測で修正を続けず、ログと関連コード全体を再確認して原因調査またはユーザー確認に切り替える。

## 2. ビルド・テスト・編集の必須ルール
- **実行環境（Windows / PowerShell）**: ルートは `G:\Cursor_Folder\DS4Windows-Vader4Pro`。`dotnet` コマンドは Git Bash（`/p:` をパス変換してしまう）ではなく **PowerShell** から実行する。**`dotnet test` には必ず `-c Debug -p:Platform=x64` を付ける**（付けないと古い DLL が走り誤判定する）。同時に 2 つの `dotnet test` を流さない。
- **Step 終了時の全件確認**: `scripts/Run-DebugAll.ps1` で本体とテスト（`DS4WindowsTests/DS4Windows.Actions.Tests.csproj`）を両方ビルドし、警告 0・エラー 0・全テスト合格を確認する（`StandaloneTests` は xUnit に統合・削除済み）。
- **テストの安全ルール**:
  - `{lex:Loc}` を使う XAML 画面部品テストは自前の STA スレッドを作らず **`WpfPanelTestRunner.Run`** を使う（放置すると全件テストが 10 件ほど 60 秒ずつハングする）。
  - `.github/copilot-instructions.md` §3.3 の「1ファイル1型」「ファイル名＝型名」「名前空間＝フォルダパス」「`#nullable enable`（`TreatWarningsAsErrors` 有効）」を厳守する。
- **編集の罠**: Bash ヒアドキュメント内の正規表現・C# エスケープ（`\n`, `\x` 等）は壊れやすいため、ファイル編集ツールまたは Python スクリプト（`chr(92)` 等）で安全に編集する。

## 3. Git 運用・規約変更の禁止事項
- **自動コミット・push 禁止**: Step や論理単位の Debug ALL 合格ごとに、エージェントが自動で `git add -A`（一部パス限定禁止）→ UTF-8 メッセージファイル経由（`git commit -F <file>`）でコミットする（`type(PhaseX-StepY): …`）。**`git push` は指示されない限り絶対に行わない**。
- **ブランチとファイル移動**: Phase ごとにブランチを切り、`master` へ `--no-ff` で統合する運用（計画指示に従う）。ファイル移動は `git mv` のみで行い、内容変更・名前空間変更とコミットを分ける（R7）。
- **`.github/copilot-instructions.md` の保護**: 規約の追記・変更は、必ず変更文面の全文をユーザーに提示して承認を得てから行う。
- **秘密情報の保護**: トークン等の値を出力しない。GitHub 等への外部投稿は全文確認後のみ。