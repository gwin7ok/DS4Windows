# Phase9-E 計画書: 9-E ControlService の責務分割

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-E、`Phase9-B-Plan.md`・`Phase9-C-Plan.md`（同じ進め方）  
状態: **承認済み（2026-10-22。§8）。E-1（OSC）はビルド・テスト合格、E-2（HidHide）もビルド・テスト合格。E-3（UDP モーション）もビルド・テスト合格。E-4（仮想パッドの出力）は実装済み（ビルド・テストと実機確認はユーザーの実行待ち）。E-5（入力レポートの処理）・E-6（ライフサイクル）は別途設計**

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
| E1 | **〔是正済み。ユーザー承認、2026-10-22〕** OSC の受信で、コントローラー番号（`command[2]`）が範囲外（負、8 以上）だと、`oscState[stateInd]` で `IndexOutOfRangeException` になる（`int.TryParse` が成功し、範囲を確かめていない）。`UDPListener` のスレッド内で発生し、握りつぶされるか、アプリの異常終了になりうる | 範囲外（負、スロット数以上）のコントローラー番号は、`Received malformed OSC address` のログを出して捨てる（`IndexOutOfRangeException` にならない） |
| E2 | **〔是正済み。ユーザー承認、2026-10-22〕** 受信した値の変換（`Convert.ToByte(Convert.ToSingle(...))`）は、範囲外（256 以上、負）で `OverflowException` になる。引数の数が足りないと `ArgumentOutOfRangeException` | 値は 0〜255 に丸める（範囲内は旧実装と同じ結果）。引数の不足・型の不一致・NaN・アドレスの要素不足などの不正なメッセージは、`Received malformed OSC message: …` のログを出して捨てる（例外を外へ出さない）。2 引数のスティック（`left`／`right`）は、2 つとも変換してから代入する（片方だけ更新されない） |
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

## 10. E1・E2 の是正（2026-10-22。ユーザー承認。ビルド・テストはユーザーの実行待ち）

**何が起きていたか**: OSC は、外部のツール（VRChat など）から、UDP で「コントローラー 0 の cross を押した」のようなメッセージを受ける機能。受信は専用のスレッドで動く。旧実装は、次のような**外部から届く不正なメッセージ**で、例外が出ていた（例外は受信スレッドの中で起きるため、握りつぶされるか、最悪はアプリの異常終了になる）:

| 届くメッセージ | 旧実装 | 新しい実装 |
|---|---|---|
| コントローラー番号が範囲外（`/ds4windows/99/press/cross`、`-2`） | `IndexOutOfRangeException` | ログを出して捨てる |
| 値が範囲外（スティック `300`、負、トリガー `400`） | `OverflowException` | 0〜255 に丸める |
| 引数がない・数値でない・NaN | `ArgumentOutOfRangeException`／`FormatException`／`OverflowException` | ログを出して捨てる |
| アドレスの要素が足りない（`/ds4windows/0`） | `IndexOutOfRangeException` | ログを出して捨てる |

**挙動の変更**: 正しいメッセージの結果は変わらない（差分テストで確認）。不正なメッセージが「例外」から「無視（ログあり）」または「端に丸める」に変わる。

## 11. E-2（HidHide）の結果（2026-10-22。この環境に `dotnet` がないため、ビルド・テストは未実行）

**設計の修正**: §1.1 の表で HidHide の領域に入れた `GetKnownExtraButtons`（32 行）は、HidHide と関係がなく、デバイスの種類から「追加で見るボタン」を返す純粋な関数だった。**ControlService に残した**（デバイスの準備＝E-6［ライフサイクル］で扱う）。

| 区分 | 内容 |
|---|---|
| 新規（契約） | `DI/IHidHideService.cs`、`IHidHideApi.cs`（ドライバーの制御デバイスの窓口。ホワイトリストの確認をドライバーなしでテストするための境界）、`IHidHideApiFactory.cs` |
| 新規（実装） | `DS4Control/Services/HidHideService.cs`（旧 `CheckHidHidePresence`／`UpdateHidHideAttributes`／`CheckAffected`／`ChangeExclusiveStatus` と状態 `hidDeviceHiding*` を、そのまま移した。ログは `DebugMessage` イベント）、`HidHideApiAdapter.cs`（`HidHideAPIDevice` を包む）、`HidHideApiFactory.cs` |
| `ControlService.cs` | 3,028 → **2,914 行**（114 行減）。`CheckHidHidePresence`／`UpdateHidHiddenAttributes` は薄い委譲。`UpdateHidHideAttributes`（外部の呼び出し 0 件。`UpdateHidHiddenAttributes` の内側の重複）は削除。`BeginPrepareConnectedInputController` の判定は `IsAffected`／`MarkAffected` に。HidHide のログは、`DebugMessage` を購読して `LogDebug`（Debug イベント）へ流す。コンストラクターの引数 +1（`IHidHideService`） |
| DI | `IHidHideService`（`HidHideService`）、`IHidHideApiFactory`（`HidHideApiFactory`）を Singleton で登録。手書きの `ControlService` 生成にも追加 |
| テスト（新規） | `Phase9EHidHideServiceDifferentialTests`（旧実装との差分: 導入／制御デバイスの開閉／インバース／既にある／追加・削除／空のパス の全組み合わせの書き込み・ログ・開閉回数、有効状態と非表示の一覧の読み直し）、`Phase9EHidHideGuardTests`、スタブ `FakeHidHideApi` |
| 実機でなければ確かめられないもの | `IsAffected` の本体（`DS4Device` と HidHide が必要）。「無効のとき false」だけをテスト |

**実機確認（依頼。実装後）**: 台帳 §34（P9E-4〜5）。

## 12. E-3（UDP モーション）の結果（2026-10-22。この環境に `dotnet` がないため、ビルド・テストは未実行）

**設計**: UDP のモーションの処理は、`ControlService` の状態（`CurrentState`／`TempState`、イベントスレッド、パッドの情報）に絡むため、`IUdpMotionHost`（実装は `ControlService`）を介し、部品は `Func<IUdpMotionHost>` で遅延して受ける（`ControlService` が部品を受けるため、循環を避ける。マクロの振動で使った `IDeviceStateAccessor` と同じ方法）。サーバー（`UdpServer`、internal）は `IUdpMotionServer`／`IUdpMotionServerFactory` の背後に置き、ソケットなしでテストできるようにした。

| 区分 | 内容 |
|---|---|
| 新規（契約） | `DI/IUdpMotionService.cs`、`IUdpMotionHost.cs`、`IUdpMotionServer.cs`、`IUdpMotionServerFactory.cs`、`UdpPadDetailProvider.cs`（デリゲート） |
| 新規（実装） | `DS4Control/Services/UdpMotionService.cs`（旧 `ChangeUDPStatus`／`ChangeMotionEventStatus`／`UseUDPPort`／`PrepareDevUDPMotion`／`ResetUdpSmoothingFilters`／`ChangeUdpSmoothingAttrs` と、フィルター・出力バッファー・`_udpServer`・`udpChangeStatus` を、そのまま移した）、`UdpMotionServerAdapter.cs`、`UdpMotionServerFactory.cs` |
| `ControlService.cs` | 2,914 → **2,707 行**（207 行減）。`ChangeUDPStatus`／`ChangeMotionEventStatus` は薄い委譲。`Start`／`Stop`／デバイスの準備の呼び出しは `IUdpMotionService` に替えた。`IUdpMotionHost` を実装（`GetCurrentState`／`GetTempState`／`QueueOnEventThread`［旧 `TestQueueBus`］）。スムージング設定の変更の購読は、部品のコンストラクターへ移した。コンストラクターの引数 +1 |
| DI | 部品、工場、`Func<IUdpMotionHost>`、`IUdpMotionHost`（`ControlService`）を登録。手書きの `ControlService` 生成にも追加 |
| テスト（新規） | `Phase9EUdpMotionServiceTests`（作成・開始・停止・失敗のログ、ハンドラーのスムージングの有無・タイムスタンプ 0 の読み飛ばし・バッファーの再利用、フィルターの作り直し）、`Phase9EUdpMotionGuardTests`、スタブ `FakeUdpMotion` |

**規約 2.4（未参照コード）の判断の記録**:
- `ControlService.UseUDPPort`（外部の呼び出し 0 件）: `UdpMotionService.UsePort` に移し、TODO を付けた（9-F で、使う予定がなければ削除をユーザーへ提案）。`ControlService` の委譲は残していない（呼び出し 0 件）。
- **`IUdpServerService`／`UdpServerService`（Phase5-Step10 で作った、UDP サーバーの安全な起動・停止の契約。DI に登録済みだが、呼び出しが 0 件）**: 契約のコメントにある「接続予定: ControlService の UDP の起動・停止を本サービス経由にする」が、E-3 の `UdpMotionService` で実現された。形が合わない（`Start(ControlService, …)` が起動と待ち受けを一度に行い、作成と待ち受けを分けられない）ため、**使うべきでない「正規の経路と重複する並行実装」と判断し、削除を提案する**（ユーザーの承認を得てから。承認までは TODO を付けて残す）。
  - **〔削除済み。ユーザー承認、2026-10-22〕** `DI/IUdpServerService.cs`、`DS4Control/Services/UdpServerService.cs`、`DS4WindowsTests/UdpServerServiceTests.cs`（3 件）を削除。DI の登録を削除。`Phase7Step2RemovedContractMembersGuardTests` の「残した契約」から外し、「削除したファイル」に追加。モデル図 01・02・04 の記載を `IUdpMotionService` に更新。

---

## 13. E-4（仮想パッドの出力）の設計案（2026-10-22。未承認。承認を得るまで、コードは変えない）

**前提の更新**: ユーザーの環境には、コントローラー（USB／Bluetooth）、ViGEmBus、HidHide がある。**E-4 以降の実機確認は、すべて実際に行える**（先送りにした台帳 §31〜§35 も、まとめて確認できる）。

### 13.1 対象と、つながり（2026-10-22 の再計測）

| 対象（`ControlService.cs` 2,707 行のうち） | 行 | 内容 |
|---|---|---|
| `StartViGEm`／`StopViGEm`、`vigemTestClient` | 約 35 | ViGEm のクライアントの生成・破棄（`Start` から。生成は別スレッドで待つ） |
| `EstablishOutDevice`、`AssignInitialDevices`、`OutputslotMan_ViGEmFailure` | 約 70 | 仮想パッドの作成、恒久スロットの割り当て、ViGEm の失敗の通知 |
| `EstablishOutFeedback`（約 228）、`RemoveOutFeedback`（約 23） | 約 250 | ViGEm からの振動・ライトバーの要求を、`DS4Device` へ戻す（Xbox 360 は `FeedbackReceived`、DS4 は出力バッファーの解析） |
| `PluginOutDev`（約 158）、`UnplugOutDev`（約 36） | 約 195 | デバイスに仮想パッドを割り当てる／外す（種類は X360／DS4、結合デバイス、恒久スロット、排他） |
| `AttachNewUnboundOutDev`／`AttachUnboundOutDev`／`DetachUnboundOutDev` | 約 32 | 入力に紐づかない仮想パッドの追加・削除（画面の「出力スロット」） |
| 状態 | | `outputslotMan`（`OutputSlotManager`）、公開配列 `outputDevices[8]`、公開プロパティ `OutputslotMan` |

- **外部からの参照**: `outputDevices`（`BackingStore`、`LegacyProfileXmlSerializer`）、`OutputslotMan`（`MainWindow` の画面、`ServiceRegistration` の `IOutputSlotService` の生成、テストの工場）、`PluginOutDev`／`UnplugOutDev`（`BackingStore`、`LegacyProfileXmlSerializer`、`Start`／`Stop`／`HotPlug`／`On_DS4Removal`）、`AttachUnboundOutDev`／`DetachUnboundOutDev`（`OutputSlotService`。**過渡期の逆依存**。`OutputSlotService` → `ControlService`）。
- **`ControlService` に頼るもの**: `DS4Controllers`（結合デバイスの相手の取得）、`SetDevRumble`（振動のブースト込み）、`LogDebug`／トレイ通知、`ActiveOutDevType`（`IOutputSlotService` の配列。遅延取得）、`_profileSettings`（出力の種類、`UseDInputOnlyArray`、`EnableOutputDataToDS4`）。

### 13.2 設計

| Wave | 部品 | 引き受けるもの | 依存 |
|---|---|---|---|
| **E-4a** | `IVirtualPadFeedback`（仮称）／`VirtualPadFeedback` | `EstablishOutFeedback`／`RemoveOutFeedback`（ViGEm から `DS4Device` への振動・ライトバーの戻り）。DS4 の出力バッファーの解析は、純粋な関数として取り出し、テストで固定する | `Func<IUdpMotionHost>` と同じ形の遅延ホスト（`SetDevRumble`）、`IProfileSettingsService` |
| **E-4b** | `IVirtualPadService`（仮称）／`VirtualPadService` | `StartViGEm`／`StopViGEm`、`EstablishOutDevice`、`PluginOutDev`／`UnplugOutDev`、`Attach/Detach*UnboundOutDev`、`AssignInitialDevices`、`outputslotMan`、`outputDevices`、`vigemTestClient`。`IOutputSlotService`／`OutputSlotService` の逆依存（`ControlService` を直接呼ぶ）を解消し、`OutputSlotService` が新しい部品を呼ぶようにする | `IVirtualPadFeedback`、`IEnvironmentService`、`IProfileSettingsService`、遅延ホスト（`DS4Controllers`、ログ） |

- `ControlService` の公開メンバー（`PluginOutDev`、`UnplugOutDev`、`Attach*`、`Detach*`、`outputDevices`、`OutputslotMan`）は、**薄い委譲（配列とプロパティは、同じ実体を返す窓口）として残す**。呼び出し側は 9-F まで変えない。
- **テスト**: ViGEm の本物は、テストでは扱えないため、`OutputSlotManager` と仮想パッドの作成を窓口（工場）の背後に置き、スタブで「どの順に何を呼ぶか」（割り当て、結合デバイス、恒久スロット、種類の切り替え、排他の判断、後始末の順序）を固定する。振動・ライトバーの変換（Xbox 360 の `LargeMotor`／`SmallMotor` → `DS4Device.setRumble`、DS4 の出力バッファー → 振動／ライトバー）は、値の表で固定する。**実機が必要な部分は、台帳に確認項目を足す**（今回は実際に確認できる）。
- **ガードレール**（`Phase5-Plan.md` §5）のうち、「ドライバ破棄順序」（`Stop` の中で、仮想パッド → `OutputSlotManager` の停止 → ViGEm の破棄）と「切断時クリーンアップ」（`UnplugOutDev`）に直結する。**呼び出しの順序は、そのまま保つ**。

### 13.3 調査で見つけた癖（挙動を変えない前提で記録する）

| # | 内容 | 扱い |
|---|---|---|
| V1 | `PluginOutDev` の X360 の枝と DS4 の枝は、約 80 行がほぼ同じ（作成の型と `EstablishOutFeedback` の引数だけが違う）。結合デバイス（`JointDeviceSlotNumber`）の相手にも同じ処理を繰り返す | まず、そのまま移す。実機確認のあと、別コミットで畳む案（Q3） |
| V2 | `EstablishOutFeedback` の DS4 の枝には、使われていない大きなコメントアウト（旧実装）が残っている。コメントは消さない | 移し先へ原文のまま移す |
| V3 | `StartViGEm` は、別スレッドで `ViGEmClient` を作り、`SpinWait` で待つ（UI スレッドを塞ぐ） | そのまま移す（今回の範囲外） |
| V4 | `outputDevices` は公開された配列で、外から要素が書き換えられる（`BackingStore` が読む） | 窓口として、同じ配列を返す（コピーしない） |

### 13.4 質問（推奨は A）

- **Q1（Wave の粒度）**: A＝E-4a（振動・ライトバーの戻り）→ E-4b（作成・割り当て・取り外し）の 2 回（推奨。E-4a は純粋な変換が多く、テストで固めやすい）／B＝まとめて 1 回。
- **Q2（実機確認の時期）**: A＝**各 Wave のビルド・テストが通ったあと、実機確認をしてから次の Wave へ進む**（推奨。ここから先は、実機でしか確認できない箇所が増えるため）／B＝実機確認は E-4 全体の終わりにまとめる。
- **Q3（V1 の畳み込み）**: A＝移した後に別コミットで畳む。**実機確認が済んでから**（推奨）／B＝畳まない。
- **Q4（`OutputSlotService` の逆依存の解消）**: A＝E-4b で、`OutputSlotService` が新しい部品を呼ぶようにする（推奨。過渡期の逆依存の解消）／B＝今回は触らず、`ControlService` の委譲を経由させたままにする。

### 13.5 決定事項（2026-10-22、ユーザー承認）

- **Q1＝まとめて 1 回**: E-4a（振動・ライトバーの戻り）と E-4b（作成・割り当て・取り外し）を 1 回で実装する。
- **Q2＝E-4 の最後にまとめて実機確認**: ビルド・テストのあと、台帳 §36 を一括して確認する（ViGEmBus、コントローラー［USB／Bluetooth］、HidHide が揃っている）。先送りにした §31〜§35 も、同じ機会にまとめて確認できる。
- **Q3＝実機確認の後に別コミットで畳む**: `PluginOutDev` の X360／DS4 の重複の畳み込みは、実機確認が済んでから。
- **Q4＝E-4b で逆依存を解消**: `OutputSlotService` は `ControlService` ではなく `IVirtualPadService` を呼ぶ。

## 14. E-4 の結果（2026-10-22。この環境に `dotnet` がないため、ビルド・テストは未実行）

**設計の調整**: E-4a・E-4b を分ける案だったが、承認（Q1）により 1 つの部品 `VirtualPadService` にまとめた（振動・ライトバーの戻りは、割り当てと同じ状態を使うため、分けても依存は減らない）。

| 区分 | 内容 |
|---|---|
| 新規（契約） | `DI/IVirtualPadService.cs`、`DI/IVirtualPadHost.cs`（`SetDevRumble`、`QueueOnEventThread`。実装は `ControlService`） |
| 新規（実装） | `DS4Control/Services/VirtualPadService.cs`（旧 `StartViGEm`／`StopViGEm`／`AssignInitialDevices`／`EstablishOutDevice`／`EstablishOutFeedback`／`RemoveOutFeedback`／`AttachNewUnboundOutDev`／`AttachUnboundOutDev`／`DetachUnboundOutDev`／`PluginOutDev`／`UnplugOutDev` と、`outputslotMan`／`outputDevices`／`vigemTestClient` を、そのまま移した。`OutputSlotManager` はコンストラクターで作る。デバイスは `IDeviceStateAccessor`、ホストは `Func` で遅延取得） |
| `ControlService.cs` | 2,707 → **2,168 行**（539 行減。9-E の開始時 3,438 行から 1,270 行減）。`PluginOutDev`／`UnplugOutDev` は薄い委譲。`outputDevices`（公開フィールド → 同じ配列を返す読み取り専用プロパティ）、`OutputslotMan`、新しい `VirtualPadService` プロパティ。`Start`／`Stop`／`HotPlug` の呼び出しを `_virtualPad` に替えた（呼び出しの順序は不変）。`OutputslotMan_ViGEmFailure` は、ControlService の状態（`loopControllers`、`Stop`）を使うため残した。コンストラクターの引数 +1 |
| `OutputSlotService.cs` | **過渡期の逆依存を解消**: `Func<ControlService>` → `Func<IVirtualPadService>`。抜き差しは `virtualPad.QueueOnEventThread`（旧: `control.EventDispatcher.BeginInvoke`）と `AttachUnboundOutDev`／`DetachUnboundOutDev` を呼ぶ |
| DI | `IVirtualPadService`、`Func<IVirtualPadService>`、`Func<IVirtualPadHost>`、`IVirtualPadHost`（`ControlService`）を登録。`IOutputSlotService` の生成は `IVirtualPadService.OutputSlotManager` を使う。手書きの `ControlService` 生成にも追加 |
| テスト（新規） | `Phase9EVirtualPadServiceTests`（窓口、事前条件による何もしない経路、ホストへの委譲、`ControlService` の窓口が同じ実体を返すこと）、`Phase9EVirtualPadGuardTests`（移した／削除したメンバー、逆依存の解消、静的な窓口の不参照、登録） |
| テスト（追従） | `TestRepositoryFactory`、`Step10FourTwoGlobalReferenceGuardTests`（`OutputSlotService` の第 3 引数の型） |
| 規約 2.4 の判断の記録 | `AttachNewUnboundOutDev`（呼び出し元 0 件）は、`VirtualPadService` に移し、TODO を付けた（9-F で、使う予定がなければ削除をユーザーへ提案）。`ControlService` の委譲は残していない。**訂正（ビルドエラーで判明）**: 最初は `AssignInitialDevices` も呼び出し元 0 件と誤認していた。実際は `ControlService.Start` から呼ばれており（`_virtualPad.AssignInitialDevices()` に替えた）、未参照ではない。調査で、定義だけを数え、同じクラス内の呼び出しを見落としていた |

**実機でなければ確かめられないもの**: 仮想パッドの作成・割り当て・取り外し（ViGEmBus）、振動・ライトバーの戻り、結合デバイス（Joy-Con）、恒久スロット、`OutputSlotManager` の非同期キューの順序。**台帳 §36** で確認する。
