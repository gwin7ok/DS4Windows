# Phase9 完了報告書: 巨大ファイルの責務分割

作成日: 2026-10-23
計画書: `Phase9-Responsibility-Split-Plan.md`、各領域の計画書（`Phase9-A1`／`A2`／`B`／`C`／`E`／`E5`／`E6`／`F`／`MapCustom`）、先送り台帳: `Phase9-Deferred-Ledger.md`、実機確認: `Phase6-Step11-RealDevice-Verification-Checklist.md` §44〜§49
状態: **9-A〜9-F（および MapCustom の分割）は完了。実機確認は合格**（未確認の項目は §5）。9-G は先送り、9-H は未着手（条件待ち）。

---

## 1. 達成したこと

| 領域 | 内容 | 結果 |
|---|---|---|
| 9-A | スティック・トリガー・ジャイロの補正を、状態なしの部品へ（`StickInputConditioner`／`StickDeadZoneProcessor`／`TriggerDeadZoneProcessor`／`OutputCurveProcessor`／`GyroControlsProcessor`／`FlickStickProcessor`） | 完了 |
| 9-B | マウスの移動量の計算・整数化、ジャイロのマウススティックの合成、コントロールが押されているかの判定（`ButtonMouseMovementCalculator`／`MouseMotionQuantizer`／`GyroMouseStickBlender`／`ControlActivityReader`） | 完了 |
| 9-C | マクロの再生と 1 値の実行（`MacroPlayback`／`MacroStepExecutor`） | 完了 |
| 9-D | 廃止して、9-B・9-C・MapCustom に吸収 | — |
| MapCustom | `MapCustom` を、合成入力の確定（M1: `SyntheticInputCommitter`）、2 段階トリガー（M2: `TwoStageTriggerProcessor`）、スティック・トリガーの整理（M3）に分割 | 完了。M4（`ControlService` 依存の除去）は任意で未着手 |
| 9-E | `ControlService` の分割: OSC（E-1）、HidHide（E-2）、UDP モーション（E-3）、仮想パッド（E-4）、遅延監視・タッチパッド切替・押下探索（E-5）、接続・切断・Joy-Con 結合・デバイス種類ごとの部品・開始停止（E-6a〜f） | 完了 |
| 9-F | 起動処理の分割（F-1a・F-1b）、WndProc／IPC の分割（F-2a・F-2b）、プロセス優先度の表の一本化（F-5a）、`ControlService` の未使用の転送メンバーの削除（F-5b） | 完了。残りは先送り |
| 9-I | モデル図 01・02・04 の更新（実在／未実装の区別、追加サービスの一覧）、規約 §3.2 の行数、本報告書 | 完了 |

### 規模

| ファイル | 着手時 | 現在 |
|---|---|---|
| `Mapping.cs` | 7,611 行（Phase8 後は 4,626 行） | 約 1,100 行 |
| `ControlService.cs` | 3,436 行 | 約 1,075 行 |
| `App.xaml.cs` の `Application_Startup` | 393 行 | 手順ごとのメソッドに分割（呼び出しの順序はテストで固定） |
| `MainWindow.xaml.cs` | 2,269 行 | 2,042 行（IPC を `MainWindow.Ipc.cs` 327 行へ） |

DI の登録は 116 件。全テスト成功、ビルド警告 0、複数型ファイル 0、ラチェットの一致検査成功を、ユーザーのビルドで確認した。

## 2. 分割の途中で見つけて直した不具合

| 内容 | 原因と対応 |
|---|---|
| Exclusive Buttons の深い引きが、浅い引きの割り当ても出力する | 2 段階トリガーの `ExclusiveButtons` で、深い引きのときに浅い引きの割り当てを取り除いていなかった。`TwoStageTriggerProcessor` に分岐を追加 |
| Hip Fire で、キーが押しっぱなしのまま固定される | `Reset()` が前フレームの押下状態を消していた。`Reset` の前に保存するように修正（`ProcessTriggerWithTwoStage`） |
| 他に、スティックの扱いの順序（LS → RS → L2 → R2 → 標準ボタン）を、元の順に保つ修正 | MapCustom の M3 |
| IPC の `query` の結果が常に空 | クライアント側が、結果の領域ではなく別の領域（クラス名）を読んでいた（Phase10-Step4d W3 の dd10cbb で混入）。84194cd で修正、ガードテスト追加 |
| 2 つ目のインスタンスの起動で、動作中の 1 つ目のログがローテーションされる | 元からの順序の問題。ログのローテーションを、単一起動の確認の後へ移した（ユーザー指示。挙動の改善） |

## 3. 承認済みの UI の仕様変更・機能追加（規約 2.2）

- Controls タブ: Default の編集画面（Output Key 等の表示、Switch Mode を Press に）、Toggle の追加（パッドのボタン、マウスのボタン、マウス移動。SA の `ButtonOutputAction` と同じ部品を使う）、「すべて Default に戻す」ボタン、割り当て一覧を 2 列の表にし、SA の一覧と書式を統一（`AssignmentDescriber`）、表示順を共通化（`InputControlCatalog`）。
- SA タブ: 行のダブルクリックで編集画面を開く。
- 機能の廃止: ステアリング設定（`Phase9-SteeringWheel-Removal-Plan.md`、ユーザー承認）。

## 4. 先送り（`Phase9-Deferred-Ledger.md`）

| ID | 内容 | 再開の条件 |
|---|---|---|
| F-1c | 起動手順を `AppStartupSequence` へ移す | 起動処理を頻繁に変更するとき |
| F-2c | IPC のコマンド実行を `IpcCommandProcessor` へ | IPC の機能を増やすとき |
| F-4 | `ProfileEditor.xaml.cs` の分割 | UI の仕様が固まったとき |
| F-5c | `ControlService` の `running`／`suspending` の転送を `IServiceLifecycle` 経由へ | `ControlService` の他のメンバーの切り離しと同時 |
| G-1 | `ProfileSettingsViewModel` のサブ ViewModel 化（9-G） | UI の仕様が固まったとき。F-4 と同時に判断 |
| 9-H | 第 1 層のデバイスクラスの分離 | K7b-1（異常終了）の切り分けが済んだ後 |
| M4 | `MapCustom` の `ControlService` 依存の除去（任意） | 必要になったとき |

## 5. 未確認・未解決

- 実機で未確認: P9M3-3（右スティックのフリックで円を描く）、P9F1-4（初回起動の流れ。設定のない環境が要る）、P9M2-2〜4。ハードウェアが要る確認（ステアリング較正、USB・ドングルでの切断など）は、Phase8 から持ち越し。
- **意図しない終了（K7b-1。0xc0000005）**【2026-10-23 更新】: 原因を特定して修正した。`HidDevice` の非同期読み書きで、保留中にバッファーが GC で移動し、カーネルが古いアドレスへ書いてヒープを壊していた（`7033089`。調査は `K7b1-Investigation.md`）。Gyro 校正ボタンの null 参照による終了も修正した（`5dadac9`）。ダンプ 57596（`performDs4Input` の NRE）は特定できず、再発したら再調査する。修正を入れたビルドを約 3 日使い、新しいダンプが出なければ閉じる（9-H の着手条件）。
- **リリース前の追加（Release-Backlog.md）**【2026-10-23】: R-1（SA の複製）、R-5（押下判定のデジタル化）、R-6（ホイール）を実装し、実機確認は合格（チェックリスト §50）。R-2（マクロの Repeat (Toggle)）は 4.2.0 向けに実装済み・実機確認合格（§51。計画書は `R2-MacroToggleRepeat-Plan.md`）。R-3・R-4 は次の版。公開済みの最新は 4.1.0（CHANGELOG 参照）。

## 6. 完了条件の確認

- 巨大ファイルの行数が着手時の半分以下: **達成**（`Mapping.cs` 約 14％、`ControlService.cs` 約 31％）。
- 全テスト成功、ビルド警告 0、複数型ファイル 0: **達成**。
- 挙動の維持: 各領域で値の特性化テスト（切り出しの前後で入力 → 出力が一致）を作り、実機で確認した。ログの文言・順序（規約 2.3）は維持。
- モデル図 01〜04 の更新: **達成**（実在／未実装を区別）。
- 巨大メソッドが 200 行以下: 9-E 時点で `MapCustom` が未達だったが、MapCustom の分割（M1〜M3）で解消した（Mapping.cs 全体が約 1,100 行）。

## 7. 次の候補

1. 未確認の実機確認（§5）の消化。
2. Phase11: 名前空間・フォルダの再編（規約 §3.3 原則 3・§3.4 の最終目標。`DI/` を Composition Root 専用にする）。
3. K7b-1 の観察（修正済み。約 3 日、新しいダンプなし）→ 閉じる → 9-H。
4. UI の仕様が固まった後の F-4・G-1。
