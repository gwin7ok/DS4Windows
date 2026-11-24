---
name: ds4w-architecture-and-review
description: DS4Windows の最新リファクタ到達点（PhaseRV1〜45）・テスト方針・ラチェット基準・コードレビュー採点ルールを扱うときに参照する
---

# アーキテクチャ到達点・テスト・採点基準スキル

## 1. リポジトリの現在地（2026-11-23 時点の要点）
- Pure DI 完了済み（`Global`・`Program.rootHub`・`ServiceProviderHolder` は削除済み。`AppHost` 参照は `App` と `Composition` のみ）。
- 4層アーキテクチャの禁止依存違反は **0 件**（`LayerDependencyRuleTests`）。
- 入力層は `InputDeviceCore` + `InputLoopDevice` + `ProtocolDevice` + `[InputDeviceFamily]` ベースへ整理済み（DS3 と Joy-Con は廃止・削除済み、ターゲットは DS4 / DualSense / Switch Pro / Xbox）。
- プロジェクト全体 `<Nullable>enable</Nullable>` ＋ `TreatWarningsAsErrors` 有効（`DS4WinWPF`, `DS4WindowsCli`）。`!` 抑止には同じ行か直前行に `//` の不変条件コメントが必須（`CodeHealthRatchetTests`）。
- ステアリング機能・vJoy・Touchpad/Gyro の `MouseJoystick` モード・`StandaloneTests`（xUnit へ統合）は削除済み。
- **未実施・保留中の主な作業候補**:
  - PhaseRV2 の残り（S3 型限定・S4 診断ログ：安全弁破損リスクのため事前一覧承認が必須）
  - PhaseRV3 のルール文（V3）の `.github/copilot-instructions.md` への反映（ユーザー承認待ち）および RV3-6 残り
  - PhaseRV5 の残りロード節（`GyroMouse`, `General`, `Misc`, `TouchpadMouse/Basic`）の `ReadLegacy` 化
  - PhaseRV4 の一部残り（`ProfileEditor.xaml.cs` 分割等）

## 2. テスト作成とラチェットのルール
- **テスト命名**: 新規テストは Phase 名ではなく「守る機能・振る舞いの名前」にする。テスト実行順（クラス名順）に依存しないよう自分で状態を初期化・復元する。
- **文字検索ではなく振る舞いテストへ**: ソース文字列を読むテストの新設は避け、振る舞いテスト（`VirtualPad`、`ManualTimeProvider`、`RecordingProxy`、`WpfPanelTestRunner` 等）を書く。文字列調査やソース読みが必要な場合も自前ヘルパーを作らず **`SourceText`**（`Read` / `ReadCode` / `StripComments` 等）と `RegistrationSource.ReadAll` を使う。
- **ラチェットの一致検査**: `LayerDependencyBaseline`、`ConstructorArityBaseline`、`ViewModelWpfReferenceBaseline`、`ViewDirectServiceReferenceBaseline`、`ViewCommandBindingBaseline`、`BackingStorePublicFieldRatchetTests`、`CodeHealthRatchetTests` 等は「実数との一致」を検査するため、違反や件数を減らしたら **同じコミットで基準値も更新する**。
- **API 削除時**: 型やメンバーを削除した場合は `RemovedApiRuleTests` の表に行を追加する。

## 3. コードレビュー採点・計測ツール
- 採点スクリプト: `scripts/audit/Score-CodeReview.py`、`scripts/audit/Measure-CodeReview.py`（`python -I` 推奨）、`scripts/audit/Measure-Localization.py`、`scripts/audit/Find-HistoryComments.py` 等。
- ルーブリック正本: `docs-forDIMG/Code-Review-2026-11-19/03-Rubric.md`、`02-Scorecard.md`、`scripts/audit/Rubric-Inputs.json`。
- **アンカー凍結ルール**: 採点の基準アンカー（`RV0-baseline` 調整値）を Phase 途中で動かさない。変更時は `RUBRIC_VERSION` 更新＋ユーザー承認＋履歴再採点が必要。