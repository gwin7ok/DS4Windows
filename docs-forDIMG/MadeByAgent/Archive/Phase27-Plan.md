# Phase27 計画書: 実行時の管理の整理（Structure-Review の E）

作成日: 2026-11-11

## 調査の結果（Phase9・13 の分割のあとの実態）
Structure-Review では「実行時の管理が複数の型に分散し、責務が重なって見える」としていたが、調べると、`ControlService` は Phase9（9-E）・Phase13 で中身を部品へ移し終えた **殻**だった。

- 残っていた役割: (1) CRC の表の初期化、(2) 前回・一時の状態の初期化、(3) 2 段トリガーの切替での状態のリセットの配線、(4) 登録簿のフック・適用プロファイルの記憶・仮想パッドの失敗の処理の取り付け（`Attach()`。生成の副作用の順序のためだけに受けていた引数）、(5) `ShutDown()`（仮想パッドの停止、出力スロットの保存、イベントスレッドの停止）。
- 外から使われていたのは、定数 2 つ（`MAX_DS4_CONTROLLER_COUNT`、`EXPANDED_CONTROLLER_COUNT`。値は同じ 8）と `ShutDown()` だけ。
- 責務の重なりは、`ServiceLifecycle`（開始・停止の流れ）、`DeviceConnector`（接続）、`DeviceDisconnectHandler`（切断）、`ControllerSlotManager`（スロットの割り当て）、`IDeviceSlotState`（状態の持ち主）で、それぞれ「接続の一生」の段階ごとに分かれており、重複ではなかった。残る整理は、`ControlService` の殻の解消である。

## 方針（決定事項）
1. 殻を消す方向で、小さな Step に分ける。名前の付け替えや大きな移動は、実機の確認が要るため、Step ごとに最小にする。
2. 定数は `ControllerLimits.MaxControllerCount` へ一本化する（旧 `EXPANDED_CONTROLLER_COUNT` は同じ値の別名だった）。

## Step 一覧
| Step | 内容 | 状態 |
|---|---|---|
| 27-1 | 定数 2 つを `ControllerLimits` へ置き換え、`ControlService` の使っていない配列を削除 | 完了（2026-11-11） |
| 27-2 | 使っていないコンストラクターの引数（`IProfileRepository`、`IMapping`）、内部アクセサ、使っていないフィールドを削除（12 → 10 引数） | 完了（2026-11-11） |
| 27-3 | 取り付けの副作用（CRC の表、2 段トリガーの配線、`Attach()` 3 件）を、名前のついた起動の部品 `RuntimeBootstrapper`（`Processing/Services`、契約は `IRuntimeBootstrapper`）の明示の `Initialize()`（何度呼んでも 1 回だけ）へ移す。前回・一時の状態の初期化は、持ち主の `DeviceSlotState` のコンストラクターへ移す | 完了（2026-11-11） |
| 27-4 | `ShutDown()` を `RuntimeBootstrapper.Shutdown()` へ移し、`ControlService` を削除する（当初は `IServiceLifecycle` 側へ移す案だったが、`ServiceLifecycle` の依存を増やさないよう、取り付けと同じ部品に置いた） | 完了（2026-11-11） |

## 履歴
- 2026-11-11: 27-3・27-4 を完了。`ControlService` を削除し、`App` は `IRuntimeBootstrapper` を解決して `Initialize()`（起動）・`Shutdown()`（終了）を呼ぶ。テストは `TestHost.InitializeRuntime()` で同じ取り付けを行う。`ControlService.cs` のソースを読んでいたガードテスト（移動済みの内容が残っていないことの検査）は、型ごとなくなったため撤去し、他のファイルを読む肯定の検査は `TheOtherFiles_…` として残した。`ControlService` の生成だけを検査していた旧 DI の配線テスト 9 ファイルも撤去した。全 3,291 テスト合格（テスト数の減少は撤去分）。
- 2026-11-11: 調査と 27-1・27-2 を完了。全 3,408 テスト合格。旧 `ControlServiceStep3Step3DiWiringTests`（削除した内部アクセサだけを検査していた）を撤去し、引数の数を見る 2 つのテストを 10 に更新した。
