# Phase8-Step4b 監査メモ（4b-0）: `LoadProfile`／`SaveProfile` の節の区切りと依存

作成日: 2026-10-19  
対象: `DS4Windows/DS4Control/Services/LegacyProfileXmlSerializer.cs`（調査時点の行番号）  
上位: `Phase8-Step4b-Plan.md` §3.1  

---

## 1. `LoadProfile` の共有している局所変数と、節をまたぐ依存

`LoadProfile`（108〜2430 行）の局所変数のうち、宣言から最後の使用までが 60 行を超えるもの。これらが、節を分けるときに文脈（`ProfileLoadContext`）へ移す対象になる。

| 変数 | 宣言 | 最後の使用 | 備考 |
|---|---|---|---|
| `Loaded` | 111 | 2428 | 戻り値。ファイルの読込の失敗で `false`。後始末が参照 |
| `rootname` | 122 | 2431 | XPath の接頭辞（現行は常に `"DS4Windows"`） |
| `missingSetting` | 123 | 2392 | 全節が書く（`catch { missingSetting = true; }` が約 170 箇所）。後始末が参照 |
| `profilepath` | 124 | 2394 | 後始末（保存し直し）が参照 |
| `xinputPlug`／`xinputStatus` | 130・131 | 2424 | 出力コントローラーの節が書き、後始末（`PostLoadSnippet`）が参照 |
| `Item` | 135 | 2121 | 各 `try` の中で毎回代入する作業用変数。**節ごとに自前で宣言してよい**（節をまたぐ値を持たない） |
| `oldContType` | 163 | 2111 | 準備で取得し、出力コントローラーの節（2104〜）が参照 |
| `lightbarSettings`／`lightInfo` | 164・165 | 1003 | ライトバーの節が主。**`ChargingType`（1003 行、雑多の節）も `lightInfo` に書く** |
| `shiftM` | 1008 | 2307 | **`ShiftModifier`（雑多の節、1008〜1013）で読み、シフトのボタン割り当て（2307）で使う**。節をまたぐ値 |
| `tpForControlsPresent` | 1089 | 1448 | **`UseTPforControls`（雑多の節）の有無を、`TouchpadOutputMode` の読込（1446〜）が参照**。節をまたぐ値 |
| `customMap*` 5 個・`shiftCustomMap*` 5 個 | 112〜121 | 2244・2360 | ボタン割り当ての節の中で完結（通常とシフトの 2 つのループ）。他の節は使わない |
| `keyType`／`wvk` | 2148・2149 | 2360・2314 | ボタン割り当ての節の中（通常とシフトで共有） |

- 節をまたぐ値は、`shiftM`、`tpForControlsPresent`、`lightInfo`、`oldContType`、`xinputPlug`／`xinputStatus`、`missingSetting`、`Loaded`、`rootname`、`profilepath` の 9 個。W2 の文脈に入れる。
- 要素の順序: **読込の順序に意味がある箇所**は、上の依存（`ShiftModifier` → シフトの割り当て、`UseTPforControls` → `TouchpadOutputMode`、旧形式の `UseSAforMouse`／`GyroOutputMode` のフォールバック）。節は、ファイル順の連続した範囲で区切るので、順序は保たれる。

## 2. 節の区切り（案。W2〜W5 で確定）

行は調査時点。1 クラス 500 行以下を目安にする（Q1＝A）。

| # | 節（クラスの案: `Profile…LoadSection`） | 行 | 備考 |
|---|---|---|---|
| 0 | 準備（指揮役に残す）: ファイルを開く、移行の読み出し、各種のリセット | 108〜172 | `LoadProfile` に残す |
| 1 | 全般・ライトバー・振動・バッテリー | 174〜310 | `lightInfo` |
| 2 | タッチパッドの基本 | 311〜336 | |
| 3 | トリガー（L2／R2）の基本設定 | 332〜376 | 2 と一部が重なる行（332〜 は `LeftTriggerMiddle` から）。W2 で境界を確定 |
| 4 | スティックの回転・不感帯・最大域・外周 | 378〜590 | |
| 5 | スティックの軸別の不感帯・デルタ加速 | 591〜890 | 約 300 行（`lsAxialDeadElement`／`rsAxialDeadElement`／`*DeltaAccelElement`） |
| 6 | SX／SZ（モーションの不感帯）・感度 | 892〜1002 | |
| 7 | 雑多（`ChargingType`、`MouseAcceleration`、`ShiftModifier`、`LaunchProgram`、`DinputOnly`、`StartTouchpadOff`、`UseTPforControls`、`UseSAforMouse`） | 1003〜1116 | `shiftM`、`tpForControlsPresent` を書く |
| 8 | SA ステアリング・ジャイロの制御・ジャイロのマウススティック | 1117〜1372 | |
| 9 | ジャイロのスワイプ・タッチパッド出力モード・ジャイロ感度・ジャイロのマウス | 1373〜1578 | `tpForControlsPresent` を読む（1446〜） |
| 10 | BT のポーリング・スティックの出力（曲線、四角形、アンチスナップバック、出力モード）・出力設定 | 1579〜1742 | |
| 11 | DualSense・トリガーの二段階・曲線・SX／SZ の曲線 | 1743〜1870 | |
| 12 | タッチパッドのマウス（トラックボール、相対マウス）・タッチパッドのスティック | 1871〜2103 | `TouchpadMouseStick` 約 200 行 |
| 13 | 出力コントローラーの種別 | 2104〜2116 | `oldContType`、`xinputPlug`／`xinputStatus` |
| 14 | **特殊アクション（`ProfileActions`）** | 2117〜2151 | **Step5 が足す節。単独のクラスにする** |
| 15 | ボタン割り当て（通常） | 2152〜2250 | `customMap*`、`keyType`、`wvk` |
| 16 | ボタン割り当て（シフト） | 2250〜2390 | `shiftCustomMap*`、`shiftM` を読む |
| 17 | 後始末（指揮役に残す）: 欠けた設定の追記、`CacheProfileCustomsFlags`、`PostLoadSnippet` | 2391〜2430 | `LoadProfile` に残す |

- 節 4〜5・8〜9 の境界は、W2〜W5 の各 Wave の着手時に、要素の依存を確認して確定する。
- 読込専用の旧形式の要素（`UseSAforMouse`、`UseTPforControls`、`GyroSmoothing`、`GyroSmoothingWeight`、`ShiftModifier`）は、7・8・9 の節にある。W1 で足したゴールデン（legacy）が、この経路を固定する。
- ログ: `LoadProfile` の読込の節には、ログ出力はほぼない（`catch` は黙って `missingSetting` を立てる）。W2 以降、節ごとに `AppLogger` の呼び出しの有無を確認し、移動で消えないようにする。

## 3. `SaveProfile` の区切り（保存の順序。案。W6〜W8 で確定）

保存は、`m_Xdoc` に要素を `AppendChild` していく約 560 行（2506〜3070）。要素の並びが読込と異なる（例: `ButtonMouseVerticalScale` が `MouseAcceleration` の直後）。XML の順序はゴールデンが固定している。区切りは、保存の順序で連続した範囲にする。

| # | 節（クラスの案: `Profile…SaveSection`） | 要素（保存の順序） |
|---|---|---|
| 0 | 準備（指揮役に残す）: パスの決定、文書の作成、既存のボタン割り当ての節の退避 | 〜2549 |
| 1 | 全般・ライトバー・タッチパッドの基本 | `touchToggle`〜`TouchpadClickPassthru` |
| 2 | トリガー・スティックの不感帯・最大域・軸別・デルタ加速 | `L2AntiDeadZone`〜`RSDeltaAccelSettings` |
| 3 | SX／SZ・感度・雑多・SA ステアリング | `SXDeadZone`〜`TouchDisInvTriggers` |
| 4 | ジャイロ（制御・マウス・マウススティック・スワイプ） | `GyroSensitivity`〜`GyroSwipeSettings` |
| 5 | スティックの出力・トリガーの二段階・DualSense・タッチパッドのマウス／スティック・曲線 | `LSOutputCurveMode` 前後〜`TouchpadMouseStick` ほか |
| 6 | **特殊アクション（`ProfileActions`）・BT のポーリング** | 2776〜2778 |
| 7 | 出力コントローラーの種別 | `OutputContDevice` |
| 8 | ボタン割り当て（通常・シフト） | `Control`／`ShiftControl` |
| 9 | 後始末（指揮役に残す）: `SaveLegacyExtraSettings`、書き出し | 3070〜 |

- 正確な境界は、W6 の着手時に、保存の要素の並び（ゴールデン XML）と照合して確定する。

## 4. ゴールデンの網羅（W1 で足したもの）

- 既存の `profile-varied.xml`（310 行）には、読込側が読む上位の要素 166 個のうち 152 個がある。ない 14 個は、入れ子の子要素と、読込専用の旧形式の要素。
- **既定の保存と変化させた保存で値が同じ要素 34 個**: 自動入力（`BackingStoreStateFiller`）が private なフィールドや設定用のメソッドの項目を変えられないため。→ `Phase8Step4bProfileGoldenGapTests` の gap で、曲線のモード 6 個、ライトバーのモード、`ProfileActions`（3 件）、ステアリングホイールの平滑化、`TouchDisInvTriggers`、外周の結合不感帯を明示的に変える。
- legacy: 旧形式の読込専用の要素と、新形式の出力モード要素がない場合のフォールバック。
- minimal: 要素がほとんどない XML の読込と、欠けた設定を追記して保存し直す動作（`LoadProfile` の最後）。
- ドリフトの補正（`LeftStickDriftXAxis` ほか、`SaveLegacyExtraSettings`／`LoadLegacyExtraSettings`）は、別の経路（追加設定）で、既存のゴールデンの範囲外。値が既定のままなので、W1 の gap に含められるか確認し、含められなければ W2 の前に足す（**未確認**）。
