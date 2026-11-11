# Phase22 計画書: USB の Xbox 360 コントローラーを入力機種として足す

作成日: 2026-11-10
前提: `Xbox-Input-Research.md`（調査と実機の確認の結果）、`Phase18-Plan.md`（読み取りの骨組みと、機種を足す差し込み口の完成）、`Model-Diagram/02-Extension-Guide.md`（機種を足す手順）。
追加の調査は、各 Step の実施時に行う（本計画書では、調査済みの事実だけを前提にする）。

## 1. 目的と終わりの状態

- USB 接続の Xbox 360 コントローラー（Windows 上の名称「XBOX 360 Controller For Windows」。VID_045E、PID_028E）を、DS4Windows の入力機器として使えるようにする。仮想パッドへの出力と、マッピングは、既存の経路をそのまま使う。
- 機種は、単純な機種の最短経路（`IDeviceProtocol` ＋ `ProtocolDeviceCartridge`）で足す。ジャイロ・タッチパッドは持たない。
- ゲームに物理コントローラーを見せない（二重入力の防止）ために、HidHide で隠す。方式は §2 の決定 3 のとおり。
- 守るもの: 動作の完全維持（2.2）、ログの文言（2.3）、既存の機種（DS4・DualSense・Switch Pro）の動作。

## 2. 設計の決定

| 番号 | 項目 | 決定 | 理由 |
|---|---|---|---|
| 1 | 対象の範囲 | USB 接続の Xbox 360 系のみ。Xbox One／Series の USB と、Bluetooth の Xbox は、この Phase の対象外 | 実機で確かめられたのは Xbox 360（USB）だけ。One／Series の USB は通信方式（GIP）が別で、見え方も未確認（`Xbox-Input-Research.md` §3、§7） |
| 2 | 読み方（**2026-11-11 改定**） | **XInput を主経路にする**（`xinput1_4.dll`）。Xbox 系は、HID の子の有無にかかわらず、すべて XInput で読む。HID の経路（22-1 の `XboxReportParser`）は使わない | 標準の Xbox 360／One（USB）は、`xusb22` が受ける専用の通信で、HID の子を持たない（今回の実機は、PS4 と Xbox の両対応機のため、HID の子も出す特殊な機器）。HID の読み取りでは、標準の純正品を読めない。XInput なら、経路が 1 本で、機器による違いが出ず、Guide も取れる。旧案（HID を直接読む。実機で、隠した状態でも許可リストで読めると確認済み）は、標準の純正品が対象外になるため、取りやめた |
| 3 | 隠し方 | **HID の子は、利用者が HidHide の GUI（`HidHideClient.exe`）で隠す**（今の DS4 と同じ使い方。2026-11-11 確定）。**USB の親（`XnaComposite`）は、DS4Windows が、サービスの有効中に、その機器が接続されたとき、HID の子の親をたどって導出して隠し、サービスの停止時に、自分が足した分だけを解除する**。この機能は、メイン画面の Other タブのチェックボックス「USB の親を隠す」で、有効・無効を切り替える（アプリ全体の設定。既定はオフ） | GUI は HID の子しか隠せず、それでは XInput の経路に漏れる（実測）。USB の親のパスは、シリアルを含み、抜き差しで変わらなかった。既存の `HidHideService`／`HidHideAPIDevice` を拡張するだけで済む。HidHide の登録を勝手に変えないよう、利用者が入れたときだけ動かす |
| 4 | 隠す対象の限定 | USB の親を隠す対象は、**DS4Windows が入力機器として認識した Xbox 系の機器すべて**（HID の子が GUI で隠されているかには依存しない）。判定は、HID の子の親が `XnaComposite` であることを基本にし、**他社製の互換機（Razer など）も含める**（2026-11-11 確定）。認識していない機器は、隠さない。プレーンな Xbox として出したいときは、仮想 Xbox 360 の出力（プレーン）を使う | DS4Windows が認識した機器は、プロファイルが適用されて仮想パッドへ出るので、元の機器は隠す。入力をそのまま Xbox として出したい利用者は、プレーンな仮想 Xbox 360 の出力で足りる。一般の HID 機器で USB の親だけを隠すと、複合機器の全機能が消える（HidHide の公式の注意）ため、`XnaComposite` に限る |
| 5 | 解除の範囲と記録 | DS4Windows が足した USB のパスを**ファイルに記録**し、解除はその分だけ（すでに隠されていたパスは足さず、記録せず、解除しない）。サービスの停止・アプリの終了・セッション終了・スリープの前に解除し、強制終了のあとは、次の起動時に、記録にあって残っているものを解除する。リストの更新は、毎回読み直してから書く（GUI の編集との競合を避ける） | 利用者の設定との衝突を避ける。異常終了で隠したままになるのを防ぐ |
| 5a | HidHide の状態への対応 | HidHide の隠す機能の全体スイッチ（cloak）が切れていても、**DS4Windows は触らない**。隠れないことを、Other タブの表示とログで知らせる。HidHide が未導入、制御デバイスが開けない、反転許可リストのときも、隠さず、同様に知らせる（2026-11-11 確定） | 利用者の全体の状態を、勝手に変えない |
| 6 | 仮想パッドとの区別 | DS4Windows 自身が作る仮想の Xbox 360（ViGEm。同じ VID_045E／PID_028E）を、入力機器として拾わない。XInput の番号ごとに、機器のパス（XUSB のデバイス インターフェース）と親をたどり、既存の `DevicePathProbe.CheckIfVirtualDevice`（根の直下が `ROOT\SYSTEM*`／`ROOT\USB*`。DS4 の仮想の除外と同じ方法）で除外する。**Razer の Tartarus Pro が作る仮想の Xbox 360 は、除外せず、実機の Xbox コントローラーと同じに扱う**（2026-11-11 確定）。割り当てたプロファイルで、信号をそのまま仮想 Xbox として出力すれば、運用上は問題ないため。除外の規則（`MI_xx` の親など）は足さない | 仮想パッドを入力として拾うと、出力が自分に戻る。ViGEm の仮想パッドは本物と同じ VID/PID に見せるため、VID/PID では区別できない |
| 7 | XInput の番号と機器の対応（2026-11-11 追加） | XInput の番号（0〜3）と、物理の機器（親の `XnaComposite`、シリアル）の対応づけの方法は、**22-1b の実機の検証で決める**。候補: (a) XUSB のデバイス インターフェース（GUID `EC87F1E3-C13B-4100-B5F7-8B84D54260CB`）を列挙し、各インターフェースの LED 状態の IOCTL（プレイヤー番号）で番号を得る、(b) `XInputGetCapabilitiesEx`（序数 108）の VID/PID と、接続の順序の組み合わせ。**対応づけが信頼できないと分かった場合は、この Phase の範囲を決め直す**（利用者に相談する） | XInput の番号は、機器の識別子を持たない。公開資料では、この対応づけの方法が確認できていない |
| 8 | Guide の取得（2026-11-11 追加） | 文書化されていない `XInputGetStateEx`（序数 100）を使って Guide を読む。取れない環境では、文書化された `XInputGetState` へ自動で切り替える（Guide は無しになる） | 多くの互換ツールの実績がある方法。将来の Windows で動かなくなっても、他のボタンは影響を受けない |

## 3. 段階

| 段階 | 内容 | 守り |
|---|---|---|
| 22-0 | 計画書の確認。レポートのデコード（全ボタン・スティック・トリガー・方向キーの対応）を、実機のレポートの記録から決める（調査用プログラムで、操作ごとの 15 バイトを記録する）。仮想パッドの区別（決定 6）の既存の判定を読む | 記録をテストの台本にする |
| 22-1 | **（完了。経路の改定により、現在は未使用）** `XboxReportParser`（`Input/Parsers`）。15 バイトの HID レポートの解析。デコード表は実機の記録から決めた | 解析器の単体テスト（36 件）。経路の改定で使わなくなったため、`XboxReportParser` に、経緯と扱い（XInput の経路の確認後に、温存か削除かを決める。規則 2.4）の TODO を付ける |
| 22-1b | **実機の検証（調査用スクリプト。PowerShell の `Add-Type`）**: (a) `XInputGetStateEx`（序数 100）で、ボタン・スティック・トリガー・Guide を読む、(b) XInput の番号と機器（親の `XnaComposite`、シリアル）の対応づけ（決定 7 の候補）、(c) 仮想パッド（ViGEm、Razer の仮想）が、番号とパスのどちらで見えるか、(d) USB の親を HidHide で隠し、DS4Windows 相当のアプリを許可リストに入れたとき、XInput で読めるか。隠しの反映のタイミング、管理者権限の要否、(e) `XInputSetState` による振動。**標準の Xbox 360（HID の子を持たない）の実機でも確かめる**（利用者に依頼） | 結果を計画書に記録し、(b) が成立しなければ範囲を決め直す |
| 22-2 | XInput の経路の設計と実装。発見（接続の検出。今は HID の列挙が前提）、読み取り（`InputLoopDevice` の差し込み口で、XInput を一定周期で読む）、`XInputStateMapper`（`XINPUT_GAMEPAD` → `ControllerState`。純粋関数）、`IXInputApi`（偽の窓口）、`InputDeviceType`・`[InputDeviceFamily]`・カートリッジ／モジュールの登録（`AddInputDevice<…>()`）、仮想パッドの除外（決定 6）。**最初に、発見と読み取りの設計を、この計画書へ追記して承認を得る** | `XInputStateMapper` の単体テスト、偽の窓口でのテスト、層の検査 |
| 22-3 | 設定の定義（`ControllerSetting`）と、設定画面・はじめの起動画面（定義から自動生成）。最小の項目から始める | 保存の黄金テストが不変 |
| 22-4 | HidHide の拡張（22-1b の結果に従う）: (a) 機器の親（`XnaComposite`）を導出する部品、(b) `HidHideAPIDevice` を使った、隠す機器の追加・解除、(c) サービスの有効中の接続時に隠す／停止時に、自分が足した分だけを解除、(d) 足した分の記録と、異常終了のあとの解除、(e) Other タブのチェックボックス（アプリ全体の設定。既定オフ）と、隠せないときの表示。偽の窓口（`IHidHideApi`）でのテスト | 既存の `HidHideService` のテストが不変 |
| 22-5 | 振動の出力（`XInputSetState`）。仮想パッドの振動の戻りを、物理コントローラーへ送る | 偽の窓口でのテスト |
| 22-6 | 文書（モデル図 01・02、拡張の手順書、規則）、実機の確認の台帳 | |

### 3.1 22-2 の設計（2026-11-11 承認。実装済み）

**方針: 既存の HID 前提の仕組み（`IHidTransport`、`ProtocolDevice`、`InputDeviceRegistry`）を壊さず、XInput を「HID の通信路のふりをする部品」として差し込む。** 機種側（`ProtocolDevice`、カートリッジ、モジュール）は、ほぼ既存のまま使える。

| 部品 | 場所 | 役割 |
|---|---|---|
| `IXInputApi`／`XInputNativeApi` | `Input/Abstractions`／`Input/Xbox` | `XInputGetStateEx`（序数 100。取れなければ `XInputGetState`）と `XInputSetState` の窓口。テストは偽物に替える |
| `IXusbInterfaceSource`／`XusbInterfaceSource` | 同上 | XUSB のデバイス インターフェース（GUID `EC87F1E3-…`）を列挙し、各機器の VID/PID（`IOCTL_XUSB_GET_INFORMATION`）、LED（`IOCTL_XUSB_GET_LED_STATE`。番号＝LED−6）、インスタンス ID を返す |
| `XInputTransport : IHidTransport` | `Input/Xbox` | 1 台の XUSB 機器の通信路。`ReadFile` は、一定周期（4 ms＝250 Hz。DS4 と同じ。ストップウォッチで周期をそろえる）で XInput を読み、**合成レポート**（13 バイト: ID 0、ボタン 2、LT、RT、LX・LY・RX・RY 各 2。Guide は 0x0400）を作って返す。変化がなくても周期でフレームを返す（時間で動く機能が、フレームに頼っているため）。接続が切れたら `NotConnected`。番号が変わったら、LED から引き直す。`WriteOutputReportViaInterrupt` は、振動（22-5）で `XInputSetState` へ。`GenerateFakeHwSerial` は、インスタンス ID から、安定した 12 桁の疑似シリアルを作る |
| `XInputReportParser : IInputReportParser` | `Input/Parsers` | 合成レポート → `ControllerState`。純粋関数。スティックは符号付き 16 ビット → 8 ビット（Y は上が 0 になるよう反転、中立 128）。ボタン名は位置が対応する DS4 の名前（A＝Cross、B＝Circle、X＝Square、Y＝Triangle、LB／RB＝L1／R1、Back＝Share、Start＝Options、押し込み＝L3／R3、**Guide＝PS**）。`L2Btn`／`R2Btn` は書かない |
| `XboxProtocol : IDeviceProtocol`、`XboxDeviceCartridge : ProtocolDeviceCartridge`、`XboxDeviceModule` | `Input/Devices`、`Processing/Services`（既存の `*DeviceModule` と同じ場所） | `InputDeviceType.Xbox360` と `[InputDeviceFamily]` を足し、`AddInputDevice<…>()` を 1 行。接続方式は常に USB、電池なし（99%）、出力は 22-5 まで無し。VID/PID の表は持たない（発見は下の窓口が、VID/PID を問わず Xbox 系を返す） |
| `IInputTransportSource` | `Input/Abstractions` | 「HID 以外の通信路を列挙する窓口」。`Enumerate()` が、通信路と、その機種の `VidPidInfo` の組を返す。`XInputTransportSource` が、`XusbInterfaceSource` から作る |
| `InputDeviceRegistry` の変更 | `Input/DS4Devices.cs` | コンストラクターに `IEnumerable<IInputTransportSource>` を足す。`findControllers` は、HID の列挙の結果と、窓口の結果を、同じ流れ（モジュールの有効判定、`IsRealDS4`［仮想の除外。ViGEm は `ROOT\SYSTEM` で除外される］、開く、シリアル、生成）に載せる。いま 3 か所ある `knownDevices.Single(VID/PID)` は、組で持ち回る `VidPidInfo` に替える（HID の動作は不変） |

- **既定は無効（2026-11-11 承認）**: Xbox 系を入力として使うかは、**Other タブの「デバイス オプション」にチェックボックスを足し、そのチェックで切り替える。初回の起動時の初期値はオフ**。オンにするまでは、今までどおり、Xbox コントローラーを DS4Windows は掴まない（既存の利用者の挙動を変えないため）。実装は、既存の機種と同じ「系統」の仕組み（`InputDeviceFamily.Xbox` と `XboxDeviceOptions`［既定オフ、保存キー `XboxSupportSettings`］）に載せる。系統を足すと、保存・読み込み・Other タブの項目・はじめの起動画面が、定義から作られる。文言のリソースと、保存の黄金テストの更新は、22-2 の後半（系統の追加）か 22-3 で行う。
- **接続の検出**: いまの検出（`HotPlug`）が、XUSB のインターフェースの抜き差しで呼ばれるかを、実装時に確かめる。呼ばれなければ、XUSB の GUID でも機器の変化の通知を登録する。
- **22-1 の `XboxReportParser`（HID の 15 バイト用）は削除する**（使い道がなく、規則 2.4 の「使うべきでないもの」に当たるため）。デコード表は、この計画書に残る。
- 同時に使えるのは XInput の 4 台まで。Razer の仮想（実機と同じ扱い）も 1 台として数える。
- テスト: `XInputReportParserTests`（純粋関数）、`XInputTransportTests`（偽の `IXInputApi`: 周期、切断、番号の引き直し）、`ProtocolDevice` と偽の通信路での一通りの流れ、`InputDeviceRegistry` と偽の窓口（組の持ち回り、無効時は拾わない）。層の検査（`LayerDependencyRuleTests`）と、既存の HID の機種のテストが不変。

### 3.2 22-4 の設計（2026-11-11 提案。承認待ち）

決定 3・4・5a に従う。**隠す対象の機器は、導出しなくてよい**: 22-2 の `XusbInterfaceInfo.InstanceId`（例: `USB\VID_045E&PID_028E\A02014100201`）が、XUSB のデバイス インターフェースの持ち主の機器そのもので、これが HidHide に足すべき「USB の親」である（22-1b で確認）。XUSB のインターフェースを持つ機器に限られるため、複合機器の他の機能を巻き込まない（決定 4 の `XnaComposite` の限定は、この性質で満たされる）。

| 部品 | 役割 |
|---|---|
| `IHidHideApi.SetBlacklist`（追加） | `HidHideAPIDevice` に既にある `SetBlacklist` を窓口へ出す（`HidHideApiAdapter` と偽物 `FakeHidHideApi` も） |
| `IHidHideService.SetXboxParentHidden(instanceId, hide)`（追加）／`RestoreHidden()` | 隠す・戻すの本体。**足した分だけ**を記録し、戻すのも足した分だけ（利用者が入れた登録は触らない）。結果は、隠した／すでに隠れていた／隠せない（未導入、制御デバイスが開けない、反転許可リスト、cloak 無効）の別で返し、ログに出す（決定 5a。cloak は切り替えない） |
| 記録ファイル（`AppDataPath` の `HidHideAdded.txt`） | 足した ID を書く。異常終了のあと、次のサービスの開始時（`UpdateHidHide`）に、記録の ID を外して消す（足したまま残る事故を防ぐ） |
| 呼び出し | `DeviceConnector.BeginPrepare`: 機種が Xbox で、設定がオンなら隠す（オフなら、足してあれば戻す）。`OutputServicesLifecycle` のサービスの停止: `RestoreHidden()` |
| 設定 | `XboxDeviceOptions.HideUsbParent`（既定オフ。保存は系統の節の `ControllerSetting`［`Bool`］。DTO は増やさない。保存の黄金テストは、節の項目が 1 つ増えるだけ） |
| 画面 | Other タブの「デバイス オプション」（`ControllerRegisterOptionsWindow`）に、「USB の親を隠す」のチェックボックスと説明を足す（`ControllerRegDeviceOptsViewModel` の 1 プロパティ。文言は `Strings.resx`／`.ja.resx`） |
| ログ | `Log_HidHide_XboxHidden`／`…NotHidden*`（`AppLogger.LogUser`。理由別: 未導入、権限、反転許可リスト、cloak 無効） |

- 反映のタイミング: 接続のたびに（サービスの有効中に）隠す。ブラックリストに足したままなら、抜き差ししても、HidHide が自動で隠す。**接続中に足したときに、すぐ効くかは実機で確かめる**（効かなければ、利用者への案内は「USB を差し直す」。22-2 の切り替えと同じ運用）。
- 設定をオフにしたとき: 次の接続（差し直し）で戻す。サービスの停止でも戻す。
- 権限: HidHide の制御デバイスを開けない場合（通常の権限の CLI は拒否された）は、隠さずにログで知らせる。DS4Windows が足せるかは、実機で確かめる。
- Other タブへの状態の表示は、今回は行わない（ログだけ）。必要なら後で足す。
- テスト: 偽の `IHidHideApi` で、足す／戻すが足した分だけ、既存の登録を保つ、隠せない 4 条件では何もしない、記録ファイルでの復旧。既存の `HidHideService` のテストが不変。

## 4. 追加の調査（実施時に行う）

- 22-0: 全ボタンのデコード、Xbox 360 以外の同系統（Razer の Xbox 360 など、VID が違う機器）の扱い。
- 22-1b: XInput の番号と機器の対応づけ（決定 7）、仮想パッド（ViGEm、Razer）の識別（決定 6）、HidHide の許可リストでの XInput の読み取り。公開資料では確認できなかったため、実機で確かめる。
- 22-2: 仮想の Xbox 360 パッドの除外の方法（22-1b の結果に従う）。XInput の接続の検出の方法（定期的な走査か、デバイスの変化の通知か）。
- 22-4: **隠しの反映のタイミング**（接続中に USB の親を足したとき、すぐ効くのか、抜き差しが要るのか）。要るなら、DS4Windows が機器の構成を作り直す方法（管理者権限が要る）を採るか、利用者への案内にするかを決める。HID の子のパスが抜き差しで変わる（`IG_nn`）点は、GUI で隠し直す運用になる（この計画書の範囲では自動化しない）。
- 22-5: 振動は `XInputSetState`（0〜65535 の 2 モーター）。仮想パッドの振動の戻りとの対応を確認する。

## 5. リスク

- 実機の確認は、USB の Xbox 360 コントローラー 1 台だけ。Xbox One／Series、Bluetooth は対象外で、未確認のまま。
- HID の子のパスは、接続のたびに変わりうる。利用者が GUI でチェックし直す手間が残る（将来の改善候補: HID の子も DS4Windows が登録する）。
- 隠しの設定を変えたあと、反映に抜き差しが要る場合がある。
- 仮想の Xbox 360 パッドと、物理の Xbox 360 が同じ VID/PID。取り違えると、入力が自分に戻る。
- **XInput の番号と機器の対応づけが、実機で成立しない場合**がある（決定 7）。その場合は、範囲を決め直す。
- XInput は同時に 4 台まで。接続の検出が、HID より弱い（機器の変化の通知がない）。
- HidHide で親を隠した状態で、許可リストのアプリが XInput で読めるかは、未確認（22-1b）。
- 今回の実機（HID の子も出す機器）は、標準の Xbox と見え方が異なる。標準の Xbox 360 の実機での確認が、まだない。

## 6. 進捗

- 2026-11-10: 計画書を作成。コードは未着手。
- 2026-11-11: 隠し方の決定（3・4・5・5a）を確定した。HID の子は利用者が GUI で隠す／USB の親は Other タブのチェックボックスで切り替える。対象は DS4Windows が認識した Xbox 系すべて（他社製の互換機を含む）。補足: 標準の Xbox コントローラーは親（XInput）だけを出力する。実機テストに使った機器は、子（DirectInput）も出力する特殊な機器なので、「子も GUI で隠す」案内は、チェックボックスの説明に書かない。
- 2026-11-11: **22-2 の実装（コード）が完了**（実機の確認は未）。HID の機種と同じカートリッジの仕組みに載せた（構造は壊していない）。
  - 通信路: `XInputTransport : IHidTransport`（4ms 周期で XInput を読み、13 バイトの合成レポート［`XInputReportLayout`］にして返す。番号が変わったら XUSB から引き直し、読めなければ `NotConnected`。出力［`[0]`=ID、`[1]`=左、`[2]`=右］は XInput の振動へ変える。シリアルは XUSB のインスタンス ID からの擬似値）。`HidDeviceAttributes`／`HidDeviceCapabilities` に、OS の構造体を介さない公開のコンストラクターを足した。
  - 発見: `IInputTransportSource`（HID の列挙に載らない通信路の供給元。実装 `XInputTransportSource`）。`InputDeviceRegistry` に供給元を足し、見つけた機器を HID の機器と同じ後続の流れ（有効設定の確認、仮想パッドの除外、シリアル、カートリッジによる生成）へ流す。機器ごとの情報（`VidPidInfo`）は供給元が持つ。**`XboxDeviceCartridge.KnownDevices` は空**にして、HID の列挙に Xbox の HID の子を載せない（決定 2: XInput のみ）。
  - 系統: `InputDeviceFamily.Xbox`、`InputDeviceType.Xbox`、`XboxDeviceOptions`（**既定オフ**、保存キー `XboxSupportSettings`）、`XboxProtocol`（電池なし。出力は 22-5）、`XboxDeviceCartridge`、`XboxDeviceModule`。登録は `AddInputDevice<XboxDeviceCartridge, XboxDeviceModule>()` の 1 行と、XInput の窓口 3 件。文言は `Strings.resx`／`Strings.ja.resx`（`ControllerSupportXbox`、`FirstLaunch.XboxEnableCkbLabel`／`XboxEnableText`）。Other タブの項目・保存・はじめの起動画面は、系統の定義から作られる。保存の黄金テスト 2 件は、`XboxSupportSettings` の節が増えた分だけ更新。
  - ホットプラグ: HID クラスの着脱通知だけでは、HID を持たない Xbox の着脱を拾えないため、`MainWindow` に **XUSB クラスの通知の登録**を足した（`IEnvironmentService.XusbClassGuid`）。
  - 削除: 22-1 の HID 用 `XboxReportParser` とそのテスト（`XInputReportParser` に置き換えた）。
  - テスト: `XInputTransportTests`（合成レポート、番号の引き直し、切断、振動、シリアル、供給元）、`XboxDeviceFlowTests`（共通のループ、登録簿での発見・無効時・二重生成なし）。既存の 7 件を更新。全件 3,331 件合格、警告 0・エラー 0。
  - 残り: 実機での Other タブの切り替えと接続・切断・ホットプラグの確認、22-5（振動）、22-4（HidHide の親の指定）。
- 2026-11-11: **22-2 の実機確認（USB 接続の Xbox 系 1 台）**。初期状態はオフで一覧に出ない／オンで認識される（仮想パッドは出ない）／ボタン・スティック・十字キー・トリガーは期待どおり／Guide が PS として反応／抜くと消え、差すと自動で戻って入力も届く、を確認。**チェックの切り替えだけでは一覧は変わらず、USB の差し直しで反映される**（利用者が、既存の機種と同じ仕様として許容）。再起動後の保存は未確認。二重出力（元の XInput が隠れていない）は、22-4（HidHide の親の指定）の対象で想定どおり。標準の Xbox 360（HID の子なし）の実機は未確認。
- 2026-11-11: **経路の改定**。標準の Xbox（USB）は HID の子を持たないため、HID の直接の読み取り（旧決定 2）では標準の純正品が読めないと判明した。利用者の承認で、XInput を主経路にし（決定 2、Guide は決定 8）、実機の検証（22-1b）を足した。22-2 以降は、22-1b の結果を受けて設計する。22-1 の `XboxReportParser` は未使用になった（TODO 付きで温存）。
- 2026-11-11: **22-1b の第 2 回（操作あり）と、番号と機器の対応づけの成立**（決定 7 の (a) が成立）。
  - 実機で A（0x1000）、十字キー（0x0001〜0x0008）、Guide（`btn=0x0400`）を操作し、**番号 1 だけが反応**、番号 0（Razer の仮想）は無反応。`XInputGetStateEx` で **Guide（0x0400）が読めた**（決定 8 は成立）。記録は `tools/xinput-probe.txt`。
  - **XUSB のデバイス インターフェースを開き、`DeviceIoControl` で 2 つを読めた**（読み取りのみ。管理者権限は不要だった）:
    - `IOCTL_XUSB_GET_LED_STATE`＝0x8000E008（入力 `00-00-00`、出力 3 バイト）→ `03-01-<LED>`。LED の値が **Razer の仮想＝6、実機＝7**。Xbox 360 の LED の模様（6＝1 番目が点灯、7＝2 番目、8＝3 番目、9＝4 番目）から、**XInput の番号＝LED − 6**。これが、**XInput の番号と、XUSB のインターフェース（＝機器のパス、親の連鎖、シリアル）を結ぶ手段**になる。
    - `IOCTL_XUSB_GET_INFORMATION`＝0x80006000（出力 12 バイト）→ 末尾 4 バイトが VID／PID（リトルエンディアン）。実機＝`5E-04 8E-02`（045E／028E）、Razer の仮想＝`6D-04 00-00`（046D／0000）で、`XInputGetCapabilitiesEx` の値と一致。
  - **(c) ViGEm の仮想パッドの識別（成立。記録 `tools/xinput-probe-vigem.txt`）**: DS4Windows の仮想パッドは、XUSB のインターフェース `USB\VID_045E&PID_028E\01` で、親の連鎖が **`ROOT\SYSTEM\0002` ← `HTREE\ROOT\0`**（XInput の番号 2、LED＝8）。実機（親が USB ハブ、シリアルあり）とは明確に違う。**既存の `DevicePathProbe.CheckIfVirtualDevice`（根の直下が `ROOT\SYSTEM*`）が、そのまま使える**。補助の手がかり: XInput の能力のバージョンが、実機（0x0110）と異なる（ViGEm は 0x0114、flags=0x000C）。
  - **(d) HidHide との関係（既存の調査で確認済み＋今回の再確認）**: `Xbox-Input-Research.md` §6・§7 で、**CLI で HID の子と USB の親の両方を隠し、許可リストにアプリを登録すると、そのアプリは XInput でも HID でも読める**（未登録は「接続なし」）と確認済み。今回（`tools/xinput-probe-hidden.txt`）、GUI で `Controller (Xbox360 For Windows)` と HID の子 3 件にチェックを入れ、実機を抜き差し（パケット番号が 124601 → 3812 に戻った）したあと、許可リスト未登録の PowerShell でも番号 1 が読めた。**GUI は XInput を隠せない**（既知の制限）ことの再確認で、親のパスを直接足す（CLI か、DS4Windows が `HidHideAPIDevice` で足す）必要がある。決定 3 は、そのまま成立。HidHide の制御用デバイスは、通常の権限の CLI では開けなかった（アクセス拒否）。DS4Windows が足すときの権限の要否は、22-4 で確認する。
  - 残る検証（旧）: (d) USB の親を HidHide で隠したときに、許可リストのアプリが XInput で読めるか（反映のタイミング、管理者権限の要否）、(e) 振動、(f) 標準の Xbox 360（HID の子を持たない）の実機。
- 2026-11-11: 22-1b の第 1 回の観察（`tools/Probe-XInput.ps1`。読み取り専用。無操作の状態）。
  - XInput は番号 0 と 1 が接続中。`XInputGetStateEx`（序数 100）と `XInputGetCapabilitiesEx`（序数 108）は、ともに呼べた。
  - **番号 0＝VID_046D／PID_0000**（Razer が作る仮想の Xbox 360。HID の子 `VID_046D&PID_0000&IG_00` と一致）。**番号 1＝VID_045E／PID_028E**（今回の実機）。仮想パッドが、実機とは別の VID/PID で XInput に見えている。
  - XUSB のデバイス インターフェースは 2 件: (1) `USB\VID_045E&PID_028E&REV_0110\01`（親は Razer の Tartarus Pro `MI_01`。Razer の仮想）、(2) `USB\VID_045E&PID_028E\A02014100201`（親は USB ハブ。実機）。**番号とインターフェースの対応は、まだ結べていない**（番号 0 の VID/PID は 046D だが、対応するインターフェース (1) の VID は 045E で一致しない）。
  - 仮想パッドの識別の手がかり: 実機のインスタンス ID は、シリアルを持ち、親が USB ハブ。Razer の仮想は、`REV_0110\01` の形で、親が USB 機器の `MI_01`。ViGEm の仮想は、まだ記録がない（DS4Windows を動かして仮想パッドを作った状態の記録が要る）。
- 2026-11-11: 22-0 の途中。
  - **仮想パッドの区別（決定 6）の既存の判定**: `DevicePathProbe.CheckIfVirtualDevice(devicePath)`（`Core/Platform`）。HID のパスから、親を `HTREE\ROOT\0` の直前までたどり、(a) ハードウェア ID が `ROOT\HIDGAMEMAP`（reWASD）／`ROOT\VHUSB3HC`（VirtualHere）、または (b) 根の直下が `ROOT\SYSTEM*`／`ROOT\USB*` なら仮想とみなす。VID/PID ではなく親の系統で見る。ViGEm の仮想パッドは根の直下の機器なので、同じ判定で除外できる見込み（Xbox では実機で確認する）。`DS4Devices.IsRealDS4` が呼んでいる。`CheckVirtualInfo`／`CheckVirtualDelegate` は、判定の結果を運ぶ型（現状は判定本体の経路に使われていない）。
  - **デコード**: 公開資料（FreeBSD `xb360gp.c`、Linux `xpad.c`、xboxdevwiki の XID）は、有線の生のレポート（20 バイト）の形で、Windows の `xinputhid` が見せる 15 バイトの HID レポートとは別物。そのため、実機の記録から決める。記録用のスクリプト: `docs-forDIMG/MadeByAgent/tools/Record-XboxHidReport.ps1`（操作ごとに 1.5 秒のレポートを集め、無操作との差分バイトを出す）。実機の記録待ち。
  - **デコード表（2026-11-11、実機の記録 `tools/xbox-reports.txt` より。15 バイト。バイト 0 はレポート ID＝0x00）**
    - 軸は 16 ビットの符号なし（リトルエンディアン）。LX＝バイト 1〜2（中立 0x8000、左 0x0000、右 0xFFFF）、LY＝3〜4（中立 0x7FFF、上 0x0000、下 0xFFFF）、RX＝5〜6（中立 0x8000）、RY＝7〜8（中立 0x7FFF）。上が小さい値。
    - **トリガーは 1 本の軸に合成**: バイト 9〜10 の 16 ビット（中立 0x8000）。LT を押すと 0xFFFF 側へ、RT を押すと 0x0000 側へ動く。LT＝max(0, z−0x8000)、RT＝max(0, 0x8000−z)（各 0〜0x7FFF → 0〜255 へ換算）。両方同時に押すと打ち消し合い、区別できない（`xinputhid` の仕様。XInput の生の値は別経路）。
    - ボタン（16 ビット、バイト 11〜12）: バイト 11 の bit0〜7＝A(0x01)、B(0x02)、X(0x04)、Y(0x08)、LB(0x10)、RB(0x20)、Back(0x40)、Start(0x80)。バイト 12 の bit0＝LS 押し込み(0x01)、bit1＝RS 押し込み(0x02)。
    - 十字キーは、バイト 12 の bit2〜5 のハット値 `(b12 >> 2) & 0x0F`。0＝なし、1＝上、2＝右上、3＝右、4＝右下、5＝下、6＝左下、7＝左、8＝左上。
    - バイト 13〜14 は常に 0。**Guide は、この HID の子には出ない**（長押しはタスクビューが開く。短押しも、記録 `tools/xbox-guide.txt` で無操作のレポート 1 件のみ＝出ないと確認済み）。Guide の取得は、このフェーズの範囲外（XInput 側の取得になるため）。タスクビューが開かないよう設定を変えたあとにも、短押し・長押し（3 秒）を記録し直したが、どちらも無操作のレポート 1 件のみ（`tools/xbox-guide.txt`）。Guide は HID の子に出ないと確定。
    - 以前の調査メモ（Xbox-Input-Research.md）の「Back＝バイト 11 の 0x02」は、この記録では B であり、Back は 0x40。メモのほうが取り違えか、機器の違いかは未確認（この機器の記録を正とする）。
    - 接続のたびに HID のパスが変わる（`IG_01` → `IG_02`）。パスでの照合は不可。
  - **22-1 完了（2026-11-11）**: `Input/Parsers/XboxReportParser`（状態を持たない。15 バイト未満は読み捨て）。軸は四捨五入で 8 ビットへ（LX の中立 0x8000 と LY の中立 0x7FFF を、どちらも 128 にそろえる）。トリガーは合成軸から LT／RT へ分け（0〜255）、`L2Btn`／`R2Btn` は書かない（後段の `InputReportProcessor` が `L2 > 0` で決める既存の動き）。ボタン名は位置が対応する DS4 の名前（A＝Cross、B＝Circle、X＝Square、Y＝Triangle、Back＝Share、Start＝Options）。`XboxReportParserTests` 36 件（実機の記録の実測値）。Debug ALL: 0 警告・0 エラー、3,325 件合格。
  - **機器の特定（2026-11-11、新しい実機を接続）**: 実機は `USB\VID_045E&PID_028E\A02014100201`（`XnaComposite`、親は USB ハブ）で、HID の子は `HID\VID_045E&PID_028E&IG_01`（親 `USB\VID_045E&PID_028E&IG_01\…`）。一方、もう 1 つの `USB\VID_045E&PID_028E&REV_0110\01` は、**Razer Tartarus Pro（`VID_1532&PID_0244&MI_01`）が作る仮想の Xbox 360**で、HID の子は `VID_046D&PID_0000&IG_00`（ViGEm ではない）。Razer の機器は VID_045E の HID パスを持たない（IG_00 が VID_046D）ので、記録スクリプトの既定の絞り込み（`vid_045e&pid_028e`）で実機だけが残る。なお、このほかに親が根の直下の `SWD\SYNTHETICGAMEPADENUM`（XBOX Synthetic Gamepad Bus）がある。仮想パッドの判定は、Razer の仮想（親が USB 機器 `MI_01`）を ROOT 系で除外できないため、決定 6 の検証項目に「Razer の仮想の Xbox 360 を入力として拾うか」を加える。
