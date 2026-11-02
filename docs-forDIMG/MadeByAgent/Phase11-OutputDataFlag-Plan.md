# Phase11 個別計画書: 「Enable Output data to DS4」を、出力の種類に関係なく、再適用でも効くようにする（W6c）

作成日: 2026-11-03
関連: `Phase11-Plan.md` §13.4 の K-W6-5（Xbox 360 のライトバー）・K-W6-12（本件）、§11（W4。DS4 への出力補正）
状態: ユーザーの仕様の変更の指示を受けて作成。**D1・D2（案 B）は 2026-11-03 に承認済み。D3 も 2026-11-03 に承認済み。D4（表示名の変更）も承認済み［英語 "Send output data to physical controller"、日本語「物理コントローラーへ出力データを送る」］**。

## 1. 背景（実機で分かったこと。2026-11-03 ユーザー報告）

「Enable Output data to DS4」（プロファイルの `outputDataToDS4`。以下「本フラグ」）は、OFF のとき、物理パッドへ何も書かない（ライトバーの色・振動・トリガー効果・触覚）。もとは、書き込みを受け付けない互換パッド（Hori PS4 Mini など）のための設定である（コミット `eabab611`。`VidPidFeatureSet.NoOutputData`）。

実機で、次の 3 つの不整合が見つかった。

1. **ON にすると Emulated Controller が強制的に DS4 になる**（W4 で復元した補正。`ProfileSettingsViewModel.CorrectOutputTypeToDs4()` の (c)）。本フラグは「物理パッドへ書くか」の設定で、仮想パッドの種類とは無関係のはずである。
2. **OFF でも、Emulated Controller が Xbox 360 だとライトバーが点灯する**（書き込みが続いているように見える）。
3. **Emulated Controller が DS4 で、ON → OFF に変えて Apply／Save（同じプロファイルの再適用）しても、ライトバーが点灯し続ける**。別のプロファイルへ切り替えてから戻すと、点灯しなくなる。

## 2. 変更後の仕様（ユーザー指示 2026-11-03）

- (S1) 本フラグは、**Emulated Controller の選択（DS4／Xbox 360）にかかわらず**、同じように機能する。
- (S2) 本フラグを ON にしても、Emulated Controller を DS4 へ**強制しない**（W4 の補正 (c) を廃止する。(a) の `UseDs3PitchRollSim` による補正は、そのまま残す）。
- (S3) **プロファイルを切り替えなくても、同じプロファイルの再適用（Apply／Save）で、本フラグの状態が反映される**。

## 3. 調査項目（実装の前に、実機ログとコードで確かめる）

- (I1) Xbox 360 のとき、OFF でも点灯する原因。本フラグ（`NoOutputData`）は種類に依存せず、`DeviceConnectionSetup.ApplyProfileOptions` が接続時・適用時に設定する。Xbox 360 のときに別の経路［ViGEm の戻り（`Xbox360FeedbackReceived` → `SetDevRumble`）、バッテリーの色、接続直後の出力など］が書いていないか、`DS4Device.sendOutputReport` 以外の書き込みの経路［`SendOutputReport`／`setRumble`／`PrepareOutputReport` の呼び元］を洗い出す。
- (I2) 同じプロファイルの再適用で `ApplyProfileOptions`（`ControlService.cs:691`、`DeviceConnector.cs:154`）が呼ばれているか。呼ばれていない、または `ProfileApplicationService` が同名のときに省略していないか。
- (I3) ON → OFF にしたとき、フラグが立った後は書かれないが、**パッドは最後の色を保持する**（点灯し続ける）。別のプロファイルへ切り替えると点灯しなくなる理由［切り替えで 1 度書かれるのか、出力スレッドが止まるのか］。
- (I4) OFF → ON にしたとき、再度書き始め、色・振動が復帰するか。
- (I5) `ControlService.cs` のほか、本フラグを読む箇所［`VirtualPadService.EstablishOutFeedbackIfEnabled`（ON のときだけ、ゲームからの振動・ライトバーの戻りを中継）、`ProfileEditor.xaml.cs:1035` の補正メッセージ、`ProfileSettingsViewModel`］の、種類への依存。とくに `EstablishOutFeedbackIfEnabled` は、Xbox 360 でも OFF なら中継しない、という現在の動きが S1 に合うか。

## 4. 決定してもらう事項（承認待ち）

- (D1)【承認済み 2026-11-03】**S2 は、Q1（2026-11-02。DS4 補正の復元）の一部を変更する**。補正 (c)［本フラグ ON → DS4 へ強制］を廃止し、(a)［DS3 の擬似 → DS4］だけを残す。台帳の P11-3・P11-4（本フラグの補正の確認。○済み）の期待が変わるため、確認項目を差し替える。**この変更でよいか。**
- (D2)【承認済み 2026-11-03: 案 B】**ON → OFF にしたときの、物理パッドの扱い**。案 A: 何もしない［パッドは最後の色を保持する。現在の動き。S3 を満たすには、フラグの更新だけでよい］。案 B（推奨）: フラグを立てる直前に、**1 度だけ**「ライトバー消灯・振動ゼロ」を書く［書き込みに失敗しても例外にせず、続行する。もとから書けないパッドは、すでにフラグが立っているので書かない］。ユーザーの期待（OFF ならライトバーが点灯しない）に合うのは案 B。**どちらにするか。**
- (D3)【承認済み 2026-11-03】画面の説明文（ツールチップ）に、「OFF にすると、ライトバー・振動・トリガー効果がすべて、物理パッドへ送られない」を足す［`Strings.resx` と各言語の `EnableOutputDataToDS4Tip`。小さな改善。**足してよいか**］。

## 5. 作業（承認後。各 Step ごとに、単体テストと `Debug ALL`［警告 0・エラー 0］を確認し、1 コミット）

| Step | 内容 |
|---|---|
| C0 | 調査（I1〜I5）。実機ログが必要なものは、ユーザーに 4 通り（OFF／ON × DS4／Xbox 360）の確認を依頼する。結果を本書 §6 に記録 |
| C1 | 補正 (c) の廃止（S2）。`CorrectOutputTypeToDs4` の (c) と、`Ds4OutputCorrectionReason.EnableOutputDataToDS4`、`ProfileEditor.xaml.cs` の該当メッセージの扱い。W4 のテストの差し替え |
| C2 | 種類に依存した動きの是正（S1）。C0 で見つかった原因の修正 |
| C3 | 同じプロファイルの再適用でも、本フラグが反映される（S3）。D2 の決定に沿って、ON → OFF の扱い |
| C4 | 説明文の追記（D3）と、表示名の変更（D4。名称の決定後）。英語・日本語を更新 |
| C5 | 実機確認（OFF／ON × DS4／Xbox 360、同じプロファイルの Apply／Save、別プロファイルへの切り替え）。台帳に新しい確認項目を追加 |

## 6. 調査結果（C0 で記録する）

**C0（コードの読みによる調査。2026-11-03。実機ログは未取得）**
- (I2) 同じプロファイルの再適用でも、`ProfileXmlStore` の読込 → `BackingStore.PostLoadSnippet` → `device.queueEvent(... ControlService.CheckProfileOptions ...)` → `DeviceConnectionSetup.ApplyProfileOptions` が呼ばれ、`NoOutputData` は更新される（`ProfileApplicationService` は同名で省略しない）。したがって、S3 の不整合は「フラグが更新されない」ためではなく、(I3) による。
- (I3) `DS4Device.sendOutputReport` は、`NoOutputData` が立つと何も書かずに戻る。ON → OFF にしても、パッドは最後の色を保持する。別のプロファイルへ切り替えると消える理由は、コードからは特定できていない（実機のログで確認する）。対策は D2＝案 B（C3）。
- (I4) OFF → ON は、フラグが外れ、次の出力サイクルから `PrepareOutputReportInner` が現在の色・振動を書く（`LightBarColor = GetMainColor` が `ApplyProfileOptions` で設定される）。
- (I5) フラグを読む箇所: `DeviceConnectionSetup`（接続時・適用時）、`VirtualPadService.EstablishOutFeedbackIfEnabled`（ON のときだけ、ゲームからの振動・ライトバーを物理パッドへ中継。Xbox 360 でも同じ。S1 に合う）、`ProfileSettingsViewModel`／`ProfileEditor`（補正 (c)。C1 で廃止）。フラグを参照するのは `DS4Device`（DS4 の種類）だけで、DualSense・Switch Pro・Joy-Con は参照しない（今回の範囲外。別の機会）。
- (I1) Xbox 360 のとき OFF でも点灯する原因は、コードの読みだけでは特定できていない。`sendOutputReport` 以外の書き込みの経路は、接続直後の初期化（`PostInit` の `sendOutputReport(true, true, false)`。フラグを立てる前に実行される）以外に見つからない。その初期化で点灯した色が残っている可能性がある（D2＝案 B で、フラグを立てる前に 1 度消灯を書くため、接続時の OFF では消灯する見込み）。**実機で、OFF／ON × DS4／Xbox 360 の 4 通りを確認し、残る場合は C2 として調べる。**

## 7. 受入条件

- OFF のとき、Emulated Controller が DS4 でも Xbox 360 でも、物理パッドへ何も書かれない。ON のときは、どちらでも、これまでどおり書かれる。
- ON にしても、Emulated Controller は強制的に変わらない。
- 同じプロファイルの Apply／Save で、本フラグの変更が、プロファイルの切り替えなしで反映される。
- `Debug ALL` が警告 0・エラー 0、全テストがグリーン。既存のログ・ログレベルは維持する（2.3）。

## 8. D4: 設定の表示名の変更（2026-11-03 ユーザー指示。**推奨案を承認済み**: 英語 "Send output data to physical controller"、日本語「物理コントローラーへ出力データを送る」）

- 目的: 「Enable Output data to DS4」の「DS4」は、接続されるコントローラーが DS4 だけではないこと（DualSense、Switch Pro、Joy-Con など）と、Emulated Controller の「DS4」［仮想パッドの種類］との混同を避けるため、物理コントローラーを指す名称にする。
- 対象: 表示名の `EnableOutputDataToDS4` と、説明文の `EnableOutputDataToDS4Tip`。`Strings.resx`（英語）と、`Translations/` の各言語（現在 25 ファイルにキーがある）。画面は `ProfileEditor.xaml` の 1 か所（`ProfileEditor.xaml:4407`）。
- **変えないもの**: リソースのキー名、プロファイル XML のタグ（`outputDataToDS4`）、コード上の名前（`EnableOutputDataToDS4`）。保存済みのプロファイルとの互換を保つため、表示名だけを変える。名前の改称（コード側）は、別の機会に回す。
- 英語の候補:
  1. **Send output data to physical controller**（推奨。動作を表し、仮想パッドと区別できる）
  2. Enable output data to physical controller
  3. Write lightbar/rumble to physical controller
- 日本語の候補:
  1. **物理コントローラーへ出力データを送る**（推奨）
  2. 物理コントローラーに出力データを書き込む
  3. 物理コントローラーへライトバー・振動を出力する
- 説明文（D3 の追記を含む）の案: 「ライトバー・振動・トリガー効果などの出力データを、物理コントローラーへ定期的に送ります。OFF にすると、これらがすべて物理コントローラーへ送られません。データを受け取れないコントローラーでは OFF にします。この設定は、Emulated Controller の種類に関係なく働きます。」［英語は同じ内容で作る。ほかの言語は、現状の文言の翻訳を維持し、英語・日本語のみ更新する。要確認］
- ガードテスト: 表示名・説明文の更新後も、`Strings.resx` と各言語のキーの対応が崩れないことを、既存のリソースのテストで確認する。
