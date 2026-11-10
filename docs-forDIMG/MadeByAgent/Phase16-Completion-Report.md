# Phase16 完了報告書

作成日: 2026-11-10 / ブランチ: `Phase16`（DS4 実機確認済み。master への取り込みは未実施）

## 実施内容
| Step | 内容 |
|---|---|
| 16-6a〜6f | `IHidTransport`、`IInputReportParser` と DS4／DualSense／Switch Pro／DS3／Joy-Con（左・右）のパーサー。旧実装との差分テスト |
| 16-7 | ライトバーの色の計算を `ILightbarService` へ（`TimeProvider`） |
| 16-8 | `IOutputDeviceCartridge`、`OutContTypeInfo` |
| 16-9 | `ActionBindingBuilder` 16 → 5 引数 |
| 16-10 | `Mapping` から `ICurveAndDeadzoneStage`、`IDeviceInputCleanup` を切り出し 25 → 20 引数（目標約 16 には届かず。段 2 は見送り） |
| 16-11 | 文書の更新 |
| 16-12a〜d（追補） | カートリッジにシリアルの読み方、種類と系統の表 `InputDeviceTypeInfo`、機種ごとの設定を系統キーの入れ物に（型付きプロパティ廃止）、`*ControllerOptions` の重複の集約。保存形式は黄金テストで不変を確認 |
| 16-13a・b（追補） | `Mapping` 20 → 17（`IMouseConversionStage`）、`ViewModelFactory` 19 → 3（3 工場へ委譲） |

## 検証
- Debug ALL: 警告 0・エラー 0。Actions 3,395 件合格。層の違反（L1〜L10）許可リスト空。

## 未実施・制限
- 実機確認（DS4 のみ・1 回）は 2026-11-10 にユーザーが実施し、全項目問題なし。確認項目: ライトバー（全モード）、出力の種類の切り替えとプリセット、USB／Bluetooth 接続、切断時のキー解放。
- DualSense・Switch Pro・Joy-Con・DS3 は実機未確認（`Known-Issues-Ledger.md` §3）。
- `InputReportProcessor` は 15 引数のまま。レジストリを偽の列挙子でテストする検査は未実施。
- 追補後の実機確認（DS4 で 1 回: 設定画面の機種ごとの設定の保存・再読み込み、ライトバー、切断）は未実施。Release ビルドも未実施。
- 引数の目標（15 以下）に届かないもの: `Mapping`（17）、`MainWindow`（21）、`MainWindowFactory`（20）、`EditorWindowFactory`（18）。画面の分割が要るため、次の Phase の候補（計画書 §12「16-13b-2／13c」）。
- 見送り: 解析器の選択をカートリッジへ移すこと（K-e）、`MappingFrame`、設定画面の `*OptionsWrapper` の統合。
