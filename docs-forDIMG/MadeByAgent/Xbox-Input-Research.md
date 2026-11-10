# 調査: USB の Xbox コントローラーを入力に追加するときの課題

作成日: 2026-11-10
目的: Xbox コントローラー（USB）を入力機器として足す場合に、(1) 物理コントローラーをゲームから隠す方法、(2) 読み取りの方法、を比べる。コードは変えない。

## 1. 実機で確かめたこと（ユーザーの環境）

- USB の「XBOX 360 For Windows」を HidHide の GUI でチェックすると、Windows の「ゲーム コントローラー」（DirectInput）の一覧からは消える。
- しかし GAMEPAD TESTER（ブラウザー。XInput 経由）には、そのコントローラーの操作が出続ける。GUI のチェックだけでは XInput の経路を隠せない。

## 2. 隠す方法（HidHide）

出典: [HidHide FAQ](https://docs.nefarius.at/projects/HidHide/FAQ/)、[HidHide CLI-Usage](https://docs.nefarius.at/projects/HidHide/CLI-Usage/)

- GUI での Xbox（XInput）の隠しは「既知の制限で、確実ではない」。DirectInput は `HID\…` のノードを開き、XInput は `USB\…` の親（ベースコンテナ）を開くことがあるため、**両方の機器パスを CLI で隠す**のが公式の回避策。
- 手順（CLI）: `HidHideCLI.exe --dev-gaming` で機器を列挙し、`deviceInstancePath`（`HID\…`）と `baseContainerDeviceInstancePath`（`USB\…`）、空でなければ `xusbDeviceInstancePath` を、`--dev-hide` で隠して `--cloak-on`。接続し直して `joy.cpl` と XInput のゲームで確かめる。
- 注意: 一般の HID 機器で USB の親だけを隠すと、複合機器の全機能が消える。Raw Input を使うアプリ（オーバーレイ、音声マクロなど）には効かない。機器パスは接続のたびに変わりうるため、VID/PID のワイルドカードは使えない。USB と Bluetooth の両方で使うなら、両方のパスが必要。
- DS4Windows が読むには、`--app-reg` で DS4Windows 本体の実行ファイルを許可リストに登録する。

→ DS4Windows が隠しを自動化するなら、HidHide の API（または CLI）で、`HID\` と `USB\` の両方のパスを隠す処理が要る。現行の隠しの処理が HID のパスだけを扱うなら、拡張が必要（未調査）。

## 3. 読み取りの方法

| 方法 | 内容 | 長所 | 短所 |
|---|---|---|---|
| A. XInput の API | `XInputGetState` で読む（4 スロットまで） | 実装が簡単。USB・Bluetooth・360／One／Series すべてに通用 | 隠す側（HidHide）が、`USB\` のパスも隠す必要がある。XInput の仕様でスロットは 4 つ。仮想の Xbox 360 パッドもスロットを使う |
| B. HID のパスで読む | HidHide の許可リストに入れた上で、HID の機器として読む | 現行の `IHidTransport` の延長 | Xbox 360 系の HID 側が、どの形式のレポートを返すかは未確認。Xbox One／Series の USB は、HID としては見えない可能性が高い |
| C. WinUSB に置き換えて生の USB | ドライバーを WinUSB にして USB の通信を直接読む | XInput から完全に消える（隠し不要）。Xbox 360 の USB は 20 バイトのレポートで単純 | 他のアプリでは普通に使えなくなる。One／Series は GIP で複雑 |

出典（C の参考）:
- [SharpXusb](https://github.com/TheNathannator/SharpXusb)（XUSB の入力レポートを C# で読む。標準では使われない末尾 6 バイトも読める）
- [xboxdrv の PROTOCOL](https://sources.debian.org/src/xboxdrv/0.8.4-1/PROTOCOL)（360 の USB の通信の解説。読み取りは bulk ではなく interrupt を使うよう勧めている）
- [xone](https://github.com/medusalix/xone)（Linux の Xbox One／Series の GIP のドライバー）。Windows の WinUSB の実装例は、検索では見つからなかった。
- GIP（Xbox One／Series の USB 通信）は、2024-09-16 から Microsoft が仕様を公開している、という記述がある（[TheNathannator の gist](https://gist.github.com/TheNathannator/c5d3b41a12db739b7ffc3d8d1a87c60a)。未確認）。

## 4. 推奨の方針（案）

1. **まず A（XInput の API）で読む**。必要な契約は、`IHidTransport` とは別の「入力の通信の入口」を足すこと（XInput のスロットを機器として検出する）。機種の追加の手順（カートリッジ＋モジュール）は、そのまま使える見込み。
2. **隠しは、HidHide の CLI／API で `HID\` と `USB\` の両方を隠す**。DS4Windows が自動で行う設計を、別の計画書で決める。
3. **C は、A と隠しの組み合わせで足りない場合の予備**にする。

## 5. 未確認（実機・実装で確かめる）

- CLI で両方のパスを隠したとき、GAMEPAD TESTER から消えるか（ユーザーの環境で実測できる）。
- 現行の隠しの処理が、`USB\` のパスを扱えるか。
- 仮想の Xbox 360 パッドが XInput のスロットを使うことで、物理コントローラーを XInput で読む処理とぶつからないか。
- Xbox One／Series の USB が、XInput と HID のどちらでも読めるか。

## 6. 実機での確認結果（2026-11-10。USB の XBOX 360 Controller。VID_045E、PID_028E）

- GUI で HID の子だけを隠す → DirectInput（`joy.cpl`）からは消えるが、XInput（ブラウザーのテスター）には出る。
- CLI で `HID\…` と `USB\…`（親）の両方を隠す → **両方から消えた。抜き差しのあとも消えたまま**。両方のパスを隠す方法が、公式の資料どおりに効く。
- 抜き差しのあと、HID の子のパスが `IG_01\…` から `IG_02\…` に変わった（`IG_nn` は XInput のプレイヤー番号に連動しているように見える。未確認）。古いパスだけを隠していると、新しい子が見えてしまう。同じポートで差し直した場合、`IG_02\9&7D27A3F&0&0000` は変わらなかった。
- `--dev-gaming` は、説明文に日本語（「HID 準拠ゲーム コントローラー」）を含む機器の位置で出力が途切れ、それ以降の機器を列挙できなかった。DS4Windows が自動化するときは、CLI の出力の解析に頼らず、HidHide の API か、Windows の機器列挙（`Get-PnpDevice` 相当）から、機器パスを得る。
- 同じ VID/PID の `XnaComposite` が 2 件（`USB\VID_045E&PID_028E\A02014100201` と `USB\VID_045E&PID_028E&REV_0110`）出ていた。後者の正体は未確認（実機の構成による）。

### 設計への影響

- 隠すべきもの: (a) HID の子のパス、(b) USB の親（`XnaComposite`）のパス。
- 接続のたびにパスが変わりうるので、DS4Windows が、接続時に現在のパスを列挙して隠す（切断時に戻す）設計が要る。
- 隠したあとも DS4Windows 自身が読めること（許可リストへの登録）は、まだ確かめていない。

## 7. 実機での確認結果その2: 隠した状態で、許可リストの登録の有無による読み取り（2026-11-10）

調査用プログラム（XInput の 4 スロットと、HID の機器の生のレポートを読む）を、HID の子と USB の親の両方を隠した状態で実行した。

| 状態 | HID の機器 | XInput（この Xbox のスロット） |
|---|---|---|
| 許可リストに未登録 | 開けない（Win32 エラー 5。アクセス拒否） | 接続なし |
| 許可リストに登録 | 開けた。15 バイトのレポートが読める | 接続あり。ボタンが読める |

- 結論: **両方のパスを隠しても、許可リストに登録した DS4Windows は、XInput でも HID でも読める**。隠す（ゲームに見せない）ことと、読む（DS4Windows だけが読める）ことは、両立する。
- 読み取りの実測: HID のレポートは 15 バイト（`00-00-80-FF-7F-00-80-FF-7F-00-80-xx-00-00-00`）。Back ボタンの押し下げで 11 バイト目が `02` になった（XInput の `0x2000`）。形式は XInput の標準の並びではなく、Windows の XInput 用の HID の層（xinputhid）の形式と見られる。全ボタンの対応は、別途デコードが必要。
- 注意（運用）: 隠しの設定を変えたあとは、コントローラーを抜き差ししないと、スタックに反映されない場合がある（GUI の案内にも「RE-CONNECT」とある）。
- 未解決: XInput のスロット番号は、機器の VID/PID を含まない。複数の XInput 機器（今回は Razer の Xbox 360 とこの Xbox）があると、どのスロットがどの機器かが決められない。HID の機器（パスに VID/PID を含む）から直接読む方式なら、この対応づけが要らない。

### 方針への影響

- 読み方は、**HID の機器から直接読む方式（案 B）が有望**: スロットの曖昧さがなく、現行の `IHidTransport` と `IInputReportParser` の仕組みにそのまま載る可能性が高い。レポート形式のデコードが必要。
- 案 A（XInput の API）は、スロットの対応づけの問題がある。単独の接続なら使える。
- 隠しは、接続時に現在の `HID\` と `USB\` のパスを列挙して隠す（§6）。
