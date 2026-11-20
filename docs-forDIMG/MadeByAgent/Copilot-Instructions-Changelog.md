# 規約（copilot-instructions.md）の変更履歴

作成日: 2026-11-20

`.github/copilot-instructions.md` の見出しにあった「最終更新日」の行を、そのまま移したもの（新しい順）。規約を変更したら、この表の先頭へ 1 行足す。

| 日付 | 内容 |
|---|---|
| 2026-11-20 | PhaseRV10: 3.1 に「アプリ設定の領域」［BackingStore の保存対象は領域の型へ。公開フィールドのラチェット］を追記 |
| 2026-11-20 | PhaseRV8: 見出しの変更履歴をこのファイルへ移した（規則の変更はなし） |
| 2026-11-20 | PhaseRV3: 3.1 に規則 V3［画面の操作は ICommand］を追記 |
| 2026-11-19 | PhaseRV1: 3.1 にアナライザーの運用、4 章に画面部品のテストの共有 STA スレッドを追記。既存の規則の変更はなし |
| 2026-11-15 | Phase29〜54 の実績の反映。計画の状態は `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.15・§6.17・§9.3 と `docs-forDIMG/MadeByAgent/Phase39-54-Record.md` を参照。規則の変更はなし |
| 2026-11-14 | Phase32: 3.1 に出力の種類の違いの置き場所［`OutContTypeInfo`、`IOutputDeviceCartridge`、`OutputDevice`］と、プログラム起動の入口［`IProfileProgramLauncher`］を追記 |
| 2026-11-11 | Phase22: 3.1 に HID を持たない機種［Xbox。XInput］の規則を追記 |
| 2026-11-10 | Phase18 完了: 3.1 に入力機種の読み取りの骨組み［`InputLoopDevice`］と基底の名前［`InputDeviceCore`］を追記。2 台結合の仕組みは削除済み。同日: Phase16 完了: 3.4 R1・R2 の第 1 層・第 3 層の構成［Input/Parsers、IHidTransport、IInputReportParser、IOutputDeviceCartridge、OutContTypeInfo］は、すでに記載の規則どおり。ガードの追加はテスト側。同日: 3.1 にコンストラクターの引数の努力目標を追記［Phase16-Step0］ |
| 2026-11-09 | 3.1 に設定の領域の窓口［ForSlot］を追記［Phase15-6Z］ |
| 2026-11-08 | 2.3 に利用者向けログの書き方［AppLogger.LogUser とリソースのキー］、4 章のテストの命名に Phase 名のテストの改名とテストの状態の初期化を追記［Phase15-0］ |
| 2026-11-05 | 3.1 に RemovedApiRuleTests への置き換え、3.3 原則1 の引き継ぎの注記、4 章に Benchmark の印と「OS の状態に触れない」を追記［Phase14d］。同日: 3.1 に DI の登録の層ごとの分割と後付けの配線の置き場所、3.3 に原則5［コメントには意図を書く。R-15］、3.4 R1・R2 に `Core/Model`、4 章にテストの命名［R-16］と Slow の印を追記［Phase14c-Step7］。同日: 3.4 R1 のルート直下の記述を更新、3.1 に違反 0 件を追記［Phase14b-2］。同日: 3.1 に層の依存の検査［LayerDependencyRuleTests］、3.4 R1 に Input・UI の Abstractions、R2 に Core・第1層が使う契約と共有の値の型の置き場所を追記［Phase14b］ |
| 2026-11-04 | : 3.3 原則3 を達成済みに、3.4 を層別フォルダの規則 R1〜R7 へ全面改定［Phase14］ |
| 2026-11-03 | : 2.3 ログ出力の維持に、Phase13b での方針に沿った変更の例外を追記［Phase12-Step1 の承認済み］ |
| 2026-10-27 | : 調査指示の追記を反映 |
| 2026-10-22 | : 3.2 の巨大ファイルの行数を Phase9 の責務分割［9-B・9-C・9-E・MapCustom の M1〜M3］の後に更新 |
| 2026-10-21 | : 3.2 の Mapping.cs の行数を Phase8-Fix-MacroRepeatUnify の削除後に更新 |
| 2026-10-20 | : 3.2 の Mapping.cs の行数を Phase8-Step8 の削減後に更新 |
| 2026-10-18 | : 2.1 のシム維持の原則を過渡期の記述に改め、3.1 に暗黙のホスト構築の禁止とラチェットの一致検査を追記［Phase7-Step10e の承認済み］ |
| 2026-10-15 | : 3.3 原則1の過渡期ルールを終了し、既存ファイルを含む厳格適用へ更新［Phase6-Step10b の完了］ |
| 2026-10-14 | : 2.2 のカスケードループ防止ガードの記述を現行の機構へ更新［Phase6-Step9b の決定6］ |
| 2026-09-24 | : 2.4 未参照コードの扱いを追記 |
| 2026-09-20 | : 3.3 原則1に過渡期ルールを追記、3.4 ファイル配置ルールを追記 |
| 2026-09-03 | : 3.3 ファイル構成・クラス・名前空間の原則と過渡期ルールを追記 |
