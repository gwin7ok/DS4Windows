# Phase11 以降のロードマップ（FurtherImprovements 01〜06 との統合）

作成日: 2026-11-02
状態: **案（ユーザーの承認待ち）。実装は、各 Step の個別計画書を作り、承認を得てから行う（copilot-instructions §4）**
ブランチ: `ArrangementRoadmap`
関連: `Phase9-Completion-Report.md` §7、`Phase9-Deferred-Ledger.md`、`../FurtherImprovements/01〜06`、`Release-Backlog.md`

---

## 1. 目的

- Phase11（名前空間・フォルダの再編）の内容を 1 か所に定義する。従来は `Phase9-Responsibility-Split-Plan.md`（Q4・Q10）、`Phase9-Completion-Report.md` §7、`copilot-instructions.md` §3.3 原則 3・§3.4 に散らばっていた。
- `FurtherImprovements/01〜06`（外部 AI が Phase10 到達時点で作成した診断・設計・手順書。2026-10-24 追加）と、既存の計画（Phase9 の先送り項目、Phase11）との関係を整理し、実施順を決める。

## 2. Phase11 の定義

**名前空間とフォルダ構成の再編。機能は変えない。**

- 名前空間をフォルダパスと一致させる（規約 §3.3 原則 3 の最終目標）。
- `DI/` を Composition Root 専用にする（規約 §3.4 の最終目標。インターフェースは層別のフォルダへ）。
- 移動は `git mv` のみで、同じコミットで内容を変えない（R7。`git diff -M100%` で検証）。名前空間の書き換えは、別のコミットにする。
- 現状（2026-11-02 実測）: `DS4Windows` 195 ファイル、`DS4Windows.Actions` 108、`DS4Windows.Services` 103、`DS4Windows.DI` 72、`DS4WinWPF.*` 約 200。`DI/` に 81 ファイル。

## 3. FurtherImprovements との対応

| 文書 | 内容 | 既存計画との関係 |
|---|---|---|
| 01 | 診断（`ControlService` の引数過多、`Func<T>`、`ServiceProviderHolder`、UI の重複、脆いテスト） | 現状と照合済み（§5） |
| 02 | 設計（Phase A〜D） | A〜D は §4 の P-A〜P-D |
| 03 | 実施手順（A〜C） | 同上。常時グリーン・Strangler Fig は本プロジェクトの規約と同じ |
| 04 | 入力パイプラインの高速化 | 性能の実測で問題が出たときだけ。Phase に属さない |
| 05 | プラグイン開発ガイド | P-D の後 |
| 06 | UI デザインシステム | P-B と一体 |

| FurtherImprovements | 既存の項目 |
|---|---|
| Phase A（テスト整備） | `3b7ca385`（ProfileEditor の配置テストを書式に依存しない判定に）で一部済み |
| Phase B（描画の共通化）、06 | F-4（`ProfileEditor.xaml.cs` の分割）、G-1（`ProfileSettingsViewModel` の分割）の前提となる UI の仕様固め |
| Phase C（`ControlService` の整理） | F-5c（`running`／`suspending` の転送）、F-1c、F-2c の延長 |
| Phase D（Layer 1 のカートリッジ化） | 9-H（第 1 層のデバイスクラスの分離）と同じ内容 |

## 4. 実施順（推奨）

| 順 | 名称 | 内容 | 開始の条件 |
|---|---|---|---|
| 0 | 前提の消化 | 先送りの実機確認（台帳 §27〜§30、P9M3-3 など）、K7b-1 の観察の締め | — |
| 1 | P-A | テスト整備（XAML のセマンティック検証ヘルパー）、`ControllerReadingsControl` の数値の見切れの修正 | 0 と並行可。小さく独立 |
| 2 | P-C | `ControlService` の整理（引数の集約、`Func<T>` の削減、`ServiceProviderHolder` の撤廃）。F-5c を含む | P-A の後。**現状との再照合が先**（§5） |
| 3 | **Phase11** | 名前空間・フォルダの再編 | P-C の後（構造変更が落ち着いてから、機械的な一括変更を 1 回で行う） |
| 4 | P-B | コントローラー描画の共通化、デザインシステム（06）。F-4・G-1 の判断 | Phase11 の後（新しい部品を最終の名前空間で作る） |
| 5 | P-D | Layer 1 のカートリッジ化（9-H と統合）、05 | K7b-1 の観察の終了後 |
| — | 04 | 入力パイプラインの高速化 | 実測で問題が出たとき |

順序の理由:
- Phase11 を P-C の前に行うと、P-C で新設・移動する型の名前空間を 2 回直すことになる。
- P-B・P-D が作る新しい型や XAML は、Phase11 の後なら最初から最終の名前空間に置ける。XAML の名前空間（`xmlns:local`）の一括変更も、UI を触り終えてからのほうが安全。
- P-A は他に依存せず、利用者に見える不具合（数値の見切れ）を直せる。

## 5. FurtherImprovements の記述と現状の差（採用前に再照合する点）

2026-11-02 の実測:
- `ControlService` のコンストラクタ引数: 文書は 34 個。実測は 30 台前半（数え方の確認が要る）。
- `Func<T>` の登録: `ServiceRegistration.cs` に 14 件。
- `ServiceProviderHolder` の使用: 6 ファイル・13 件（台帳 §17.3 の 17 件から減少）。
- 文書の記述: 対象ブランチが `DarkThemeArrange`、テスト 2,728 件など、作成時点の値。

採用の条件:
- 提案が `copilot-instructions.md` の規約（§2 品質・安全基準、§3.3 1 ファイル 1 型、§3.4 配置ルール R1〜R7）に反しないこと。
- 機能の削減をしないこと（§2.2）。ログの文言・レベルを維持すること（§2.3）。
- 個別計画書（Step ごと）を作り、承認を得てから実装すること（§4）。

## 6. 次の作業

1. 本書の承認（実施順、P-A〜P-D の採否）。
2. 前提（順 0）の消化の計画。
3. P-A の個別計画書の作成。
