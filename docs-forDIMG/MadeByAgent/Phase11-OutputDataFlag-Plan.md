# Phase11 個別計画書: 「Enable Output data to DS4」を、出力の種類に関係なく、再適用でも効くようにする（W6c）

作成日: 2026-11-03
関連: `Phase11-Plan.md` §13.4 の K-W6-5（Xbox 360 のライトバー）・K-W6-12（本件）、§11（W4。DS4 への出力補正）
状態: **ユーザーの仕様の変更の指示を受けて作成。実装の前に、§4 の決定事項の承認が必要。**

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

- (D1) **S2 は、Q1（2026-11-02。DS4 補正の復元）の一部を変更する**。補正 (c)［本フラグ ON → DS4 へ強制］を廃止し、(a)［DS3 の擬似 → DS4］だけを残す。台帳の P11-3・P11-4（本フラグの補正の確認。○済み）の期待が変わるため、確認項目を差し替える。**この変更でよいか。**
- (D2) **ON → OFF にしたときの、物理パッドの扱い**。案 A: 何もしない［パッドは最後の色を保持する。現在の動き。S3 を満たすには、フラグの更新だけでよい］。案 B（推奨）: フラグを立てる直前に、**1 度だけ**「ライトバー消灯・振動ゼロ」を書く［書き込みに失敗しても例外にせず、続行する。もとから書けないパッドは、すでにフラグが立っているので書かない］。ユーザーの期待（OFF ならライトバーが点灯しない）に合うのは案 B。**どちらにするか。**
- (D3) 画面の説明文（ツールチップ）に、「OFF にすると、ライトバー・振動・トリガー効果がすべて、物理パッドへ送られない」を足す［`Strings.resx` と各言語の `EnableOutputDataToDS4Tip`。小さな改善。**足してよいか**］。

## 5. 作業（承認後。各 Step ごとに、単体テストと `Debug ALL`［警告 0・エラー 0］を確認し、1 コミット）

| Step | 内容 |
|---|---|
| C0 | 調査（I1〜I5）。実機ログが必要なものは、ユーザーに 4 通り（OFF／ON × DS4／Xbox 360）の確認を依頼する。結果を本書 §6 に記録 |
| C1 | 補正 (c) の廃止（S2）。`CorrectOutputTypeToDs4` の (c) と、`Ds4OutputCorrectionReason.EnableOutputDataToDS4`、`ProfileEditor.xaml.cs` の該当メッセージの扱い。W4 のテストの差し替え |
| C2 | 種類に依存した動きの是正（S1）。C0 で見つかった原因の修正 |
| C3 | 同じプロファイルの再適用でも、本フラグが反映される（S3）。D2 の決定に沿って、ON → OFF の扱い |
| C4 | （D3 を承認した場合）説明文の追記 |
| C5 | 実機確認（OFF／ON × DS4／Xbox 360、同じプロファイルの Apply／Save、別プロファイルへの切り替え）。台帳に新しい確認項目を追加 |

## 6. 調査結果（C0 で記録する）

（未実施）

## 7. 受入条件

- OFF のとき、Emulated Controller が DS4 でも Xbox 360 でも、物理パッドへ何も書かれない。ON のときは、どちらでも、これまでどおり書かれる。
- ON にしても、Emulated Controller は強制的に変わらない。
- 同じプロファイルの Apply／Save で、本フラグの変更が、プロファイルの切り替えなしで反映される。
- `Debug ALL` が警告 0・エラー 0、全テストがグリーン。既存のログ・ログレベルは維持する（2.3）。
