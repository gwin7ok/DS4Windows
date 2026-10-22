# Phase9-E 計画書: 9-E ControlService の責務分割

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-E、`Phase9-B-Plan.md`・`Phase9-C-Plan.md`（同じ進め方）  
状態: **承認済み（2026-10-22。§8）。E-1（OSC）は実装済み（ビルド・テストはユーザーの実行待ち）。E-2（HidHide）・E-3（UDP モーション）は、E-1 の結果を見て設計を追記する**

---

## 1. 目的と範囲

`ControlService.cs`（3,438 行。メンバー約 70）を、責務ごとの部品へ切り出す。挙動は変えない（ガードレール 6 項目を維持）。9-B・9-C と違い、**実機（コントローラー、ViGEm、Bluetooth）が絡む領域が多い**ため、純粋に近い部分から順に、1 領域ずつ切り出す。

### 1.1 再計測（2026-10-22）

| 領域 | 対象（行数） | 規模 | 実機依存 | 外部からの呼び出し |
|---|---|---|---|---|
| **OSC** | `MapMonitoringOscMessageToCommand`（28）、`CreateOSCCallback`（178）、`ChangeOSCListenerStatus`（16）、`ChangeOSCSenderStatus`（21）、`SendOsc`（6）、`OSCPostMappingStep`（25）、`OSCPreMappingStep`（33）、`CompareAndSendChangesToOSC`（117）、状態 `oscState[8]`／`oscCallback`／`oscListener`／`oscSender` | 約 430 | なし（UDP ソケットのみ） | `ChangeOSCListenerStatus`・`ChangeOSCSenderStatus`（`MainWindow`）、`oscSender`（外 3 箇所） |
| **HidHide** | `CheckHidHidePresence`（72）、`UpdateHidHideAttributes`（28）、`UpdateHidHiddenAttributes`（8）、`CheckAffected`（23）、`GetKnownExtraButtons`（32）、`ChangeExclusiveStatus`（8）、状態 `hidDeviceHiding*` | 約 190 | HidHide ドライバー、`DS4Devices` | `App`・`AutoProfilesViewModel` が `CheckHidHidePresence` |
| **UDP モーション** | `ChangeUDPStatus`（53）、`ChangeMotionEventStatus`（38）、`UseUDPPort`（46）、`PrepareDevUDPMotion`（37）、`ResetUdpSmoothingFilters`（12）、`ChangeUdpSmoothingAttrs`（15）、状態 `udpEuroPair*`／`udpOutBuffers`／`_udpServer` | 約 200 | UDP ソケット、`DS4Device` のイベント | `MainWindow` が `ChangeUDPStatus`・`ChangeMotionEventStatus` |
| **仮想パッドの出力** | `EstablishOutFeedback`（228）、`PluginOutDev`（158）、`UnplugOutDev`（36）、`RemoveOutFeedback`（23）、`AttachNewUnboundOutDev`／`AttachUnboundOutDev`／`DetachUnboundOutDev`（各 10）、`StartViGEm`／`StopViGEm`（35）、`OutputslotMan_ViGEmFailure` | 約 530 | ViGEm、振動・ライトバーの戻り | `BackingStore`・各画面が `PluginOutDev`／`UnplugOutDev`／`AttachUnboundOutDev`／`DetachUnboundOutDev` |
| **入力レポートの処理** | `On_Report`（229）、`LagFlashWarning`、`CheckForTouchToggle`、`GetActiveInputControl`（78）ほか | 約 450 | 毎秒 250〜1000 回の入力スレッド | イベント（`DS4Device.Report`） |
| **ライフサイクル** | `Start`（174）、`Stop`（120）、`HotPlug`（130）、`PrepareConnectedInputControllerSettingEvents`（162）、`On_DS4Removal`（109）、`CheckProfileOptions`ほか | 約 1,000 | 全部 | `MainWindow`・`App` ほか多数 |

- `ControlService` は、`IDeviceStateAccessor` の実装者であり、`Mapping`・各画面が `ControlService` を直接受け取る箇所が多い（`ctrl` 引数）。**切り出した後も、`ControlService` の公開メソッドは薄い委譲として残し、呼び出し側の変更は 9-F まで行わない**（9-B・9-C と同じ方針）。
- 既存のテスト: `ControlServiceOscSenderRaceTests`（OSC 送信側の競合）、`ControlServiceHotPathAllocationTests`（On_Report の割り当て 0）、`ControlService*DiWiringTests`・`*ShimEquivalenceTests`（配線）。送信内容・受信の解釈・HidHide・UDP を固定するテストは**ない**。テストプロジェクトは SharpOSC を参照していない。

## 2. 設計の方針

### 2.1 Wave の順序（リスクの小さい順。1 Wave ＝ 1 領域）

| Wave | 領域 | 理由 |
|---|---|---|
| **E-1** | OSC | 実機に依存せず、状態も小さい。受信の解釈と送信の差分検出を、通信なしでテストできる（下の分離）。最初の Wave として、進め方（部品の置き方、委譲、テストの足し方）を確定する |
| **E-2** | HidHide | 外部ドライバーとの境界だが、判定（`CheckAffected`、`GetKnownExtraButtons`）は純粋 |
| **E-3** | UDP モーション | ソケットとデバイスのイベント。スムージングのフィルターの状態あり |
| E-4 以降 | 仮想パッドの出力、入力レポートの処理、ライフサイクル | **E-1〜E-3 の後に、別の計画書で設計する**（実機が必須。ガードレールの「ドライバ破棄順序」「切断時クリーンアップ」「スレッド直列化」に直結） |

本計画書の承認の対象は **E-1〜E-3** とする。

### 2.2 E-1（OSC）の設計

| 部品 | 内容 |
|---|---|
| `IOscService`／`OscService`（DI の Singleton。契約は `DI/`、実装は `DS4Control/Services/`） | `oscState[8]`、受信のコールバック、リスナー／送信側のライフサイクル、送信（`Send`）、入力への重ね合わせ（旧 `OSCPreMappingStep`／`OSCPostMappingStep`）、変化の送信（旧 `CompareAndSendChangesToOSC`）、プラグの通知（`SendPlug(int index, bool plugged)`）、デバイスごとの状態の初期化（`ResetDeviceState(int index)`。旧 `oscState[i] = new DS4State()`） |
| 通信の分離（テスト用） | 送信の窓口を小さな契約 `IOscMessageSender`（`Send(string address, int value)`。実装は `UDPSender` を包む）にし、サービスはそれを工場（`Func<string, int, IOscMessageSender>`）から作る。これにより、送信内容を、SharpOSC なしで、記録して比べられる。受信の解釈は、`OscPacket` の取り出しと、解釈（`HandleMessage(string address, IReadOnlyList<object> arguments)`）に分け、後者をテストする |
| `ControlService` | 旧 OSC 系のメンバーを削除し、`IOscService` を受ける（コンストラクターの引数 +1）。`ChangeOSCListenerStatus`／`ChangeOSCSenderStatus` は、薄い委譲として残す。`oscSender` の公開フィールドは、外 3 箇所の参照を調べて、委譲のプロパティに置き換える |
| 先に足すテスト | 旧実装の逐語的な写しとの差分テスト: (1) 受信（アドレス × 引数）→ `oscState` の変化（ボタン、スティック 1 値・2 値、トリガー、バッテリー、監視形式 `monitor`）、(2) `PreMappingStep`／`PostMappingStep` の重ね合わせ（全フィールド）、(3) `CompareAndSend` の送信の列（全フィールドの変化）、(4) 競合（送信側が未生成・破棄済み）。旧実装の写しは、`ControlService` から切り出す前の `HEAD` から機械的に写す（9-B・9-C と同じ） |

E-2・E-3 は、E-1 の結果を見てから、**この計画書に設計を追記**して着手する（承認は都度）。

## 3. 調査で見つけた、現行の癖（挙動を変えない前提で、記録する）

| # | 内容 | 扱い |
|---|---|---|
| E1 | OSC の受信で、コントローラー番号（`command[2]`）が範囲外（負、8 以上）だと、`oscState[stateInd]` で `IndexOutOfRangeException` になる（`int.TryParse` が成功し、範囲を確かめていない）。`UDPListener` のスレッド内で発生し、握りつぶされるか、アプリの異常終了になりうる | そのまま移す。差分テストで「同じ例外」を固定。是正は Q3 で判断 |
| E2 | 受信した値の変換（`Convert.ToByte(Convert.ToSingle(...))`）は、範囲外（256 以上、負）で `OverflowException` になる。引数の数が足りないと `ArgumentOutOfRangeException` | 同上 |
| E3 | `ChangeOSCListenerStatus(false)` は、リスナーが null だと `NullReferenceException`（呼び出し側が `oscListener != null` を確かめてから呼ぶ前提） | そのまま移す（呼び出し側の条件も移す） |
| E4 | `OSCPreMappingStep` は、引数の入力状態 `cState` を直接書き換える（重ね合わせ） | そのまま移す |
| E5 | OSC の送信は、`UseOscSender` が true のときだけ呼ばれる（`ControlService` 内の条件）。条件の置き場が複数ある（`On_Report`、`On_DS4Removal`、`HotPlug`） | 条件の置き場は変えず、呼び出しだけを `IOscService` へ替える |

## 4. 質問（推奨は A）

- **Q1（承認の範囲）**: A＝**E-1〜E-3 を承認する。E-1 から着手し、E-2・E-3 は E-1 の結果を見て設計を追記する**（推奨）／B＝E-1 だけを承認する。
- **Q2（Wave の粒度）**: A＝1 領域ごとに、ビルド・テストを確認する（推奨）／B＝まとめて 1 回。
- **Q3（E1・E2: 範囲外の OSC メッセージ）**: A＝**今回はそのまま移す**（推奨。挙動を変えない）。範囲外のコントローラー番号・値で例外になる癖は、別の是正として後で判断／B＝**範囲を確かめて、範囲外は無視する**（`Received malformed OSC address` のログと同じく、GUI ログに出して捨てる。例外で受信スレッドが落ちる可能性がなくなる。**挙動の変更**）。
- **Q4（`ControlService` の公開メソッド）**: A＝薄い委譲として残し、呼び出し側は 9-F まで変えない（推奨）／B＝呼び出し側（`MainWindow` ほか）も同時に `IOscService` を受けるようにする（差分が大きく、画面の実機確認が要る）。

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| 入力スレッド（`On_Report`）から呼ばれる OSC の処理で、割り当てが増える | `OscService` は新しいオブジェクトを作らない（送信の `OscMessage` は、旧実装も作っていた）。`ControlServiceHotPathAllocationTests` と、割り当ての計測で確かめる |
| `ControlService` のコンストラクターの引数が増え、テストの工場（`ControlServiceTestFactory`）や DI の配線テストが壊れる | Wave ごとに、工場と配線テストを直す。引数は +1 ずつ |
| 競合（K9-1 の回帰）を再発させる | `SendOsc` の「フィールドをローカルに取る」作りをそのまま移し、`ControlServiceOscSenderRaceTests` を新しい部品に合わせて直す |
| 静的な窓口を新設しない | 部品はコンストラクター注入（Pure DI）。ラチェット（`Global`／`AppHost`／`Program.rootHub`）の件数は変えない |
| ガードレール（同一 XML 排他、適用時 Halt、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ） | E-1〜E-3 は、OSC・HidHide・UDP で、いずれもこの 6 項目の対象外。ただし「切断時クリーンアップ」と「停止時の破棄順序」の中で呼ばれる箇所（`Stop`、`On_DS4Removal`）は、呼び出しの順序を変えない |

## 6. 実機確認（想定。実装後に依頼）

(1) OSC: 設定で OSC サーバー／送信を ON にして、外部ツールから受信したボタン・スティックが入力に反映され、コントローラーの入力が外部へ送信される（`/ds4windows/monitor/…`）。(2) OSC の ON／OFF を繰り返してもアプリが落ちない。(3) コントローラーの接続・切断で、プラグの通知が送られる。

## 7. 次

承認（Q1〜Q4）を得たら、E-1 から着手する。ビルド・テストが必要になった時点で、ユーザーへ依頼する。

---

## 8. 決定事項（2026-10-22、すべて推奨どおり承認）

- **Q1＝E-1〜E-3 を承認**: E-1 から着手し、E-2・E-3 は E-1 の結果を見て設計を追記する（承認は都度）。
- **Q2＝1 領域ごとに確認**: OSC、HidHide、UDP を 1 つずつ実装し、そのつどビルド・テストを確認する。
- **Q3＝今回はそのまま移す**: 範囲外のコントローラー番号・値で例外になる癖（E1・E2）は、そのまま。別の是正として後で判断する。
- **Q4＝薄い委譲として残す**: `ControlService.ChangeOSCListenerStatus`／`ChangeOSCSenderStatus` は残し、呼び出し側は 9-F まで変えない。

## 9. E-1（OSC）の結果（2026-10-22。この環境に `dotnet` がないため、ビルド・テストは未実行）

| 区分 | 内容 |
|---|---|
| 新規（契約） | `DI/IOscService.cs`、`IOscTransportFactory.cs`、`IOscMessageSender.cs`、`IOscListenerHandle.cs` |
| 新規（実装） | `DS4Control/Services/OscService.cs`（旧 OSC 系を、そのまま移した。受信の解釈は `HandleMessage(address, arguments)`、マッピング前後は `PreMappingStep`／`PostMappingStep`、変化の送信、`SendOsc` と K9-1 の競合対策、リスナー／送信側のライフサイクル）、`SharpOscTransportFactory.cs`・`SharpOscListenerHandle.cs`・`SharpOscMessageSender.cs`（SharpOSC の型は、ここだけに閉じた） |
| `ControlService.cs` | 3,438 → **3,028 行**（410 行減）。`oscState`／`oscCallback`／`oscListener`／`oscSender`、`MapMonitoringOscMessageToCommand`、`CreateOSCCallback`、`SendOsc`、`OSCPreMappingStep`／`OSCPostMappingStep`、`CompareAndSendChangesToOSC` を削除。`ChangeOSCListenerStatus`／`ChangeOSCSenderStatus` は `IOscService` への薄い委譲。`Start`／`Stop`／`HotPlug`／`On_DS4Removal`／`On_Report` の呼び出しを `IOscService` に替えた（条件 `UseOscSender`／`UseOscServer` の置き場は変えていない）。コンストラクターの引数 +1（`IOscService`） |
| DI | `IOscService`（`OscService`）、`IOscTransportFactory`（`SharpOscTransportFactory`）を Singleton で登録 |
| テスト（新規） | `Phase9EOscServiceDifferentialTests`（旧実装との差分: 受信の全メッセージ、範囲外の例外、null アドレス、マッピング前後の重ね合わせ 200 ラウンド、`SendPlug`、`ResetDeviceState`、ライフサイクル）、`Phase9EOscGuardTests`、スタブ `RecordingOscTransportFactory` |
| テスト（追従） | `ControlServiceOscSenderRaceTests`（検査の対象を `OscService` に） |

**実機確認（依頼。実装後）**: 台帳 §33（P9E-1〜3）。
