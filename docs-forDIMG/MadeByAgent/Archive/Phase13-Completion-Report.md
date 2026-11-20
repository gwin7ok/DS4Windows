# Phase13 完了報告書: `ControlService` の整理

作成日: 2026-11-04
対象ブランチ: `Phase13`（`master` から 12 Step、約 45 コミット）
計画書: `Phase13-Plan.md`。進捗: `Phase13-Status.md`。個別計画書: `Phase13-Step1〜11-Plan.md`

---

## 1. 結果の要約

| 指標 | Phase13 開始時 | 完了時 |
|---|---|---|
| `ControlService` のコンストラクター引数 | 34 | **17**（努力目標 20 以下を達成） |
| `ControlService.cs` の行数 | 約 1,075 | 約 527 |
| `Func<T>`（遅延取得） | 14 件 | **2 件**（本物の循環のみ。許可リスト検査で固定） |
| `ServiceProviderHolder`（クラス、`SetProvider`、静的 `ActionManager`） | 6 ファイル・13 件 | **0 件** |
| `ControlService` の `running`／`suspending` の転送（F-5c） | あり | **0 件**（`IServiceLifecycle` を直接注入） |
| Host 窓口（`IVirtualPadHost`／`IDeviceSlotHost`／`IServiceLifecycleHost`／`IUdpMotionHost`） | 4 本 | **0 本** |
| テスト | 3,043 件程度 | Actions 3,147 件＋Standalone 11 件（すべて合格）、警告 0・エラー 0 |

機能の削減・ログの文言とレベルの変更は**なし**（ログ整理は Phase13b）。

## 2. Step ごとの結果

| Step | 内容 | 主な成果 |
|---|---|---|
| 1 | トリガー系 4 クラスほかの未使用 `profileSettings` 引数の削除 | 未使用引数 0 |
| 2 | `ControlService` の未使用引数 6 個の削除 | 34 → 28 |
| 3 | 状態を持たないホストの分離 | `IEventThreadDispatcher`／`IDeviceRumbleService`／`IOutputKbmInitializer` 新設。Host 窓口 2 本を廃止 |
| 4 | `ServiceProviderHolder` の撤廃 | 静的 `ActionManager`・`ServiceProviderHolder`・`ButtonSpecialActionOutput` を削除し、`IManagedActionManager`／`IKeyButtonControllerRegistry` を注入 |
| 5／5b | スロット状態の持ち主の新設、`On_Report` の分離 | `IDeviceSlotState`／`IInputReportProcessor`。`Mapping` から `ControlService` への逆依存を解消。ホットパスの A/B 計測は割り当て 0・有意な悪化なし |
| 6 | `Func<ControlService>` の置き換え | `IProfileLoadTarget` ほか。本番ソースの `Func<ControlService>` が 0 件 |
| 7 | `Func<IOutputSlotService>` ほかの相互参照の解消 | `IOutputDeviceTypeState` |
| 8 | 残る `Func` の再判定 | 循環が消えたものを直接注入へ。残りは 2 件のみ |
| 9 | F-5c（`running`／`suspending` の転送の削除） | `IServiceLifecycle` の直接注入 |
| 10 | 転送メンバーの削除と引数の整理 | `IRuntimeFeatureSwitch`／`IServiceDebugRelay` 新設。26 → 17 引数 |
| 11 | （任意）F-2c の実施、F-1c の見送り | `IpcCommandProcessor`／`IIpcCommandHost`。UI なしでコマンドを単体テスト可能に |
| 12 | 完了作業 | モデル図 01〜04、台帳、本報告書、Release ビルド、実機確認 |

## 3. 完了条件（`Phase13-Plan.md` §11）の照合

| # | 条件 | 結果 |
|---|---|---|
| 1 | 未使用のコンストラクター引数が 0 件 | 達成（Step1・2・5b-e・10） |
| 2 | 循環を理由とする `Func<T>` が 0 件。起動順を理由とするものは台帳に記録 | **残る 2 件は本物の循環**（下記 §4）。許可リスト検査 `Phase13FuncRatchetGuardTests` で固定 |
| 3 | `ServiceProviderHolder` 0 件。ラチェットは禁止ガードとして維持 | 達成（Step4） |
| 4 | F-5c 完了 | 達成（Step9） |
| 5 | F-1c・F-2c: 実施、または見送りを台帳に記録 | F-2c 実施、F-1c 見送り（`Phase9-Deferred-Ledger.md`） |
| 6 | `ControlService` の引数が 20 以下 | 達成（17） |
| 7 | `Debug ALL` 警告 0・エラー 0・全合格、ラチェット 3 本がグリーン | 達成（最終実行は §6） |
| 8 | モデル図 01〜04 が §10 のとおり反映済み | 達成（`407f9445`） |
| 9 | Release ビルドを 2 か所に出し、実機確認が完了 | ビルドは §6。実機確認はユーザーの 1 回を待つ |
| 10 | 完了報告書、台帳、ロードマップの更新 | 達成（本報告書、`Phase9-Deferred-Ledger.md`、`Phase7-Residual-Ledger.md` §17.6、`Phase11-and-Beyond-Roadmap.md` §7） |

## 4. 残る `Func<T>` 2 件とその理由

| 場所 | 型 | 理由 | 再検討の条件 |
|---|---|---|---|
| `DefaultActionManager` | `Func<IMapping>` | `Mapping` → `ITriggerDispatchEngine` → `IManagedActionManager`（`DefaultActionManager`）→ `IMapping` の循環。生成時に解決すると循環する | `DefaultActionManager` が `IMapping` を使わなくなったとき |
| `ProfileApplicationService` | `Func<IActionBindingBuilder>` | `Mapping` → `IProfileApplicationService` → `IActionBindingBuilder` → … → `Mapping` の循環 | 同上（束縛の組み立てを別の経路で受けられるようになったとき） |

## 5. 先送り・持ち越し

- **F-1c（起動手順の `AppStartupSequence` への分離）**: 見送り。UI（ウィンドウ表示、ダイアログ）への密着が強く、窓口が太くなる。順序は `Phase9F1StartupOrderGuardTests` で固定済み。再開は、起動処理を頻繁に変更する必要が出たとき。
- **`ControlService` のスロット状態の転送**（`DS4Controllers`／`touchPad`／`slotManager` ほか、約 50 利用者）: 転送を残した。利用者の書き換えが大きく、得るものが小さい。Phase14 または必要になったときに。
- **`MainWindow` のコンストラクター引数は 23**（`ArgumentParser` を含む。Step11 で `IIpcCommandProcessor` が増えた）。ViewModel の分割の候補として、Phase15（UI 共通化）で再検討。
- **テストの `[Collection("ServiceProviderHolder")]` の名前**: 実体の撤廃後も名前だけ残る。改名は別コミットの小作業。
- 第 1 層（`DS4Devices`／`DS4LightBar`／`HidDevices`）の静的な状態は、Phase16 の候補のまま。

## 6. 検証

- `Debug ALL`（Build Debug → Build Test → Execute Test）: 警告 0・エラー 0・Actions 3,147 件＋Standalone 11 件が合格（Step12 での最終実行）。
- ラチェット: `Step10FourGlobalRatchetGuardTests`、`Phase7Step1ServiceLocatorRatchetGuardTests`、`Phase7Step10eW5RatchetEqualityGuardTests`、`Phase13FuncRatchetGuardTests` がグリーン。
- Release ビルド 2 か所:
  1. `DS4Windows\bin\x64\Release\`
  2. `DS4Windows\bin\x64\DS4Windows\`（`CopyDS4WindowsToJK.bat` が取り込む実行物。最後の管理者権限でのコピーはユーザー）

## 7. 実機確認（1 回。DS4 実機）

`Phase13-Plan.md` §7 の H1〜H11 に加えて、各 Step の完了時に記録した項目:

- 接続・切断・スロット・再接続（Step3、5、7）、PS+↓ での切断（既存の別件 `pending-alttab-on-disconnect` は再現を待つ）
- サービスの開始・停止、トレイの Start／Stop 表示、PC のスリープ・復帰、終了時にハングしない（Step9）
- OSC／UDP の切り替え、HidHide の登録、タッチパッドのスライド表示、プロファイルエディターの入力取得と振動の停止、ログ表示（Step10）
- CLI の `start`／`stop`／`cycle`／`disconnect`／`changeledcolor`／`loadprofile`／`loadtempprofile`／`outputslot`／`query`（Step11）
- プロファイルの適用・切替、Fresh-press ガード、割り当て（Key のトグル、Macro、Button、Program、Profile）、自動プロファイル（Step4、6、8）
- 起動時のログの順序、`ProcessingDelayMs` の比較（Step5b）
- 引き継ぎ: Phase11 の S9-17（電池表示の文言）

## 9. 実機確認の結果（2026-11-04。DS4 実機、Release v4.3.0）

| # | 項目 | 結果 |
|---|---|---|
| 1 | 起動と接続（USB／Bluetooth、起動前後） | 正常 |
| 2 | 切断と再接続（PS+↓、ケーブル抜き、ログ） | 正常 |
| 3 | サービス操作（開始・停止、トレイ表示、終了時のハング） | 正常（**スリープ復帰は見送り**） |
| 4 | プロファイル（切替、トリガー、一時、Fresh-press、自動プロファイル） | 正常 |
| 5 | 割り当て（Key のトグル、Macro、Button、Program、Profile） | 正常 |
| 6 | 出力と機能の切替 | Plug/Unplug と HidHide は正常。**OSC／UDP、タッチパッドのスライド表示、振動、プロファイルエディターの入力取得と振動停止は見送り** |
| 7 | CLI（`-command`） | `stop`／`start`／`cycle`／`Disconnect.1`／`OutputSlot.1.Unplug|PlugX360`／`Query.1.AppRunning` は正常。**`ChangeLedColor.1.255.0.0` と `LoadProfile.1.<名前>` は反応せず（ログもなし）。** `LoadTempProfile` は未確認 |
| 8 | 表示とログ（電池表示、ログ、入力の遅れ） | 正常（S9-17 を含む） |

### 未解決: CLI の `ChangeLedColor`／`LoadProfile`
- Step11 の前後でロジックは同一（`git show 937a7c52^` の旧 `MainWindow.Ipc.cs` と `IpcCommandProcessor` を突き合わせて確認）。単体テストは緑。
- 両者に共通するのは、`conLvViewModel.ControllerCol[index]`（`UpdateCustomLightColor`、`Device`）を使う点。正常だった `Disconnect`（`ControllerDict`）、`OutputSlot`、`Query` とは経路が違う。
- Phase13 より前の版との比較は、ユーザーが見送り。**回帰かどうかは未確定**。再開の条件: 旧版（`master` のビルド）で同じコマンドを実行して比較する。
- 見送り分（スリープ復帰、OSC／UDP、タッチパッド、振動、エディターの入力取得、`LoadTempProfile`）は、Phase13 の完了条件ではなく、次の実機確認の機会に行う。

## 8. 次の作業

Phase13b（ログの一括整理）。その後 Phase14（名前空間・フォルダの再編）。詳細は `Phase11-and-Beyond-Roadmap.md` §7。
