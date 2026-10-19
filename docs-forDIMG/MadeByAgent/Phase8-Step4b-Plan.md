# Phase8-Step4b 計画書: プロファイルの読込・保存の節ごとの分解、機械的な整理 2 件

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（2026-10-19 追記「実行順序の改訂」の Step4b）、`Phase8-Step4-Completion-Report.md` §5  
状態: **実施中（Q1〜Q8 を推奨どおり承認、2026-10-19）。W1 完了［ゴールデン確定、977 件成功、2026-10-19］。W2 完了［基盤＋2 節。成功］。W3 完了［2 節。成功］。W4 完了［4 節。成功］。W5 完了［読込の分解を完了。成功］。W6 完了［保存側 7 節を一括で移動（計画の W6〜W8 を 1 回で実施）。成功］。W9 完了［名前引き 2 関数の移動。成功］。W10 実装済み［ホバーの 3 つの Populate 関数と逆引きを ProfileEditor.HoverTables.cs へ移動。ビルド・テスト待ち］。次は W11**

---

## 1. 目的と位置づけ

- `LegacyProfileXmlSerializer`（3,093 行）の `LoadProfile`（約 2,320 行の 1 メソッド）と `SaveProfile`（約 560 行の 1 メソッド）を、**節ごとのクラスに分ける**。Step5（読込時の `TriggerBinding` の構築）が、「特殊アクション」の節に足すためのもの。
- あわせて、機械的な整理 2 件（4b-3）を行う: `BackingStore` の名前の変換関数、`ProfileEditor` のホバー画像・位置の表。
- **製品の挙動・保存形式を変えない**。`Mapping.cs`・`Actions/` には触れない。Phase8 本体（統一トリガーディスパッチ）の作業ではなく、Step5 の準備。
- 安全網: ゴールデンテスト（`Phase7Step10cGoldenPersistenceTests`: 既定の保存、変化させた状態の保存、変化させた XML の読込）、`ProfileLegacyXmlRoundTripTests`、`ProfileLegacyXmlCompletenessTests`。分解の各 Wave で、ゴールデンの文面が**一切変わらない**ことを条件にする。
- 規約: 1 ファイル 1 型、ファイル名＝型名（§3.3）、配置は R2（`DS4Windows/DS4Control/Services/`）、名前空間は R6（`DS4Windows.Services`）、巨大ファイルは対象メソッドのみ置換（§3.2）、ラチェット（`Global.*` ほか）の基準値は、新しいファイルへ引き継ぐ（合計を増やさない）。

## 2. 現状の調査（この計画書のための事前確認）

### 2.1 `LoadProfile` の形
- 構造: 準備（ファイルを開く、移行の読み出し、`ResetProfile` ほかのリセット）→ **約 180 個の要素を、それぞれ `try { … } catch { missingSetting = true; }` で読む**（約 2,200 行）→ 後始末（欠けた設定の追記、`postLoad`、XInput の抜き差し）。
- 共有している局所変数: `Item`（`XmlNode`）、`rootname`、`missingSetting`、`Loaded`、`profilepath`、`oldContType`、`lightbarSettings`／`lightInfo`、`xinputPlug`／`xinputStatus`、ボタン割り当て用の辞書 10 個（`customMap*`／`shiftCustomMap*`）、引数（`device`、`control`、`launchprogram`、`xinputChange`、`postLoad`）。
- 要素の並び（行の範囲。1 つのクラスにできる、連続した節の案）:

| 節の案 | 行（目安） | 中身 |
|---|---|---|
| 準備 | 108〜172 | ファイルを開く、リセット |
| 全般・ライトバー・振動・バッテリー | 174〜310 | `touchToggle`、`LightbarMode`、`Color`、`LowColor`、`ChargingColor`、`FlashColor` ほか |
| タッチパッドの基本 | 311〜336 | `touchSensitivity`、`tapSensitivity`、`TouchpadInvert` ほか |
| トリガー（L2／R2） | 332〜376 | 不感帯、最大域、出力 |
| スティック（回転・不感帯・軸別・外周ほか） | 378〜890 | `LSRotation`〜`RSAxialDeadOptions`（軸別の不感帯だけで約 300 行） |
| モーション（SX／SZ、感度、SA ステアリング、ジャイロの各設定） | 892〜1580 | `SXDeadZone`〜`GyroMouseToggle`、`GyroSwipeSettings` |
| 雑多（`ChargingType`、`MouseAcceleration`、`LaunchProgram`、`DinputOnly`、`StartTouchpadOff`、`UseTPforControls` ほか） | 1003〜1120 | `ShiftModifier` の読込を含む |
| スティックの出力（曲線、四角形、アンチスナップバック、出力モード、デルタ加速）・トリガーの二段階・DualSense | 1587〜1870 | |
| タッチパッドのマウス・トラックボール・タッチパッドのスティック | 1871〜2103 | `TouchpadMouseStick` だけで約 200 行 |
| 出力コントローラーの種別 | 2104〜2116 | `OutputContDevice` |
| **特殊アクション（`ProfileActions`）** | 2117〜2151 | **Step5 が足す節** |
| ボタン割り当て（`Control`／`ShiftControl`） | 2152〜2390 | 割り当て用の辞書 10 個を埋める |
| 後始末 | 2391〜2430 | 欠けた設定の追記ほか |

  並びは、**ファイルの順序のまま連続した範囲で区切る**（読込の順序と相互依存を、そのまま保つため）。要素の読込に、節をまたぐ依存（先に読んだ値を後で使う）が見つかった場合は、4b-0 で一覧にする。
- 読込にだけ存在する旧形式の要素（移行用）: `GyroSmoothing`／`GyroSmoothingWeight`、`UseSAforMouse`、`UseTPforControls`、`ShiftModifier` ほか。

### 2.2 `SaveProfile` の形
- XML の文書を組み立てて（`m_Xdoc`）、約 250 個の要素を `AppendChild` していく約 560 行。**保存の要素の順序は、読込の順序とは異なる**（例: 保存では `ButtonMouseVerticalScale` が `MouseAcceleration` の直後）。ゴールデン XML が順序を固定している。
- したがって、節の区切りも**保存側は保存の順序で連続した範囲**で決める（読込の節と 1 対 1 にはならない）。

### 2.3 ゴールデンテストの網羅（4b-0 の初期結果）
- 変化させた状態（`BackingStoreStateFiller`）で保存した XML（310 行）には、読込側が読む上位の要素 166 個のうち、152 個が含まれる。ない 14 個は、入れ子の子要素（`Red`／`Green`／`Blue` ほか）と、読込専用の旧形式の要素（`GyroSmoothing`、`GyroSmoothingWeight`、`UseSAforMouse`、`UseTPforControls`、`ShiftModifier`）。
- **既定の保存と変化させた保存で値が同じ要素**（変化が付いていない可能性があるもの）: 曲線のモード（`LSOutputCurveMode`、`RSOutputCurveMode`、`L2／R2／SX／SZOutputCurveMode`）、`LightbarMode`、`ProfileActions`（**特殊アクションが 0 件のまま**）、`SASteeringWheelUseSmoothing`、`TouchDisInvTriggers`、`LSOuterBindDead`／`RSOuterBindDead`、ドリフトの補正ほか 34 個。→ これらの節は、分解の前に**非既定値のゴールデンを足す**（4b-0）。特に `ProfileActions` は Step5 の入力になるため必須。

### 2.4 4b-3 の対象
- `BackingStore.getDS4ControlsByName`／`getX360ControlsByName`（約 200 行。呼び出し元は本体内ほか、他のファイル・テストで 25 箇所）。移し先は、既存の `DS4ControlNames`／`X360ControlTables`。
- `ProfileEditor.PopulateHoverImages`（約 160 行）／`PopulateHoverLocations`（約 190 行）。WPF の画像を作る処理を含む（第 4 層）。

## 3. 作業内容

### 3.1 4b-0 監査とゴールデンの追加（W1）
- §2.3 の一覧に基づき、足りない節の**非既定値のゴールデン**を足す（`BackingStoreStateFiller` に値を足す、または旧形式の XML を直接読み込ませる入力を足す）。**製品のコードは変えない**。既存のゴールデンの文面は変わらない（足した節は、新しいゴールデンの追加として別ファイル、または末尾の追記）。
- 節をまたぐ依存（先に読んだ値を後で使う箇所）と、`missingSetting` の伝播、ログ出力、例外の握りつぶしの一覧を作る。
- 読込の前後で、同じ入力から同じ状態になることを確かめる「差分比較」の仕組み（分解の前に取った状態の記録と、分解後の状態の比較。ゴールデンとは別に、全要素を含む広い入力で）を用意する。

### 3.2 4b-1 `LoadProfile` の分解（W2〜W5）
- 共有する状態を、`ProfileLoadContext`（読込 1 回分の状態: `Device`、`Control`、XML 文書、`RootName`、`MissingSetting`、`Loaded`、ライトバーの設定ほか。クラス）にまとめ、節ごとのクラスが受け取る。
- 節ごとのクラス（例: `ProfileLightbarLoadSection`）は、`void Load(ProfileLoadContext context)` を持つ。共通の契約は小さなインターフェース（`IProfileLoadSection`）。
- `LoadProfile` は、準備 → 節を**順に**呼ぶ → 後始末、の指揮役（約 150 行）として残す（公開メソッドと引数は変えない）。
- 節の中身は、現行の `try { … } catch { missingSetting = true; }` の形を**そのまま移す**（書き換えない）。ログ、順序、例外の握りつぶしを維持する。

### 3.3 4b-2 `SaveProfile` の分解（W6〜W8）
- 4b-1 と同様（`ProfileSaveContext`、`IProfileSaveSection`）。保存の順序で連続した範囲に区切る。XML の要素の順序・内容がゴールデンと完全に一致すること。

### 3.4 4b-3 機械的な整理（W9〜W10）
- 名前の変換関数: 本体を `DS4ControlNames`／`X360ControlTables` へ移し、呼び出し元を付け替える（Q6）。
- ホバー画像・位置の表: データ用のクラスへ（詳細は W10 の前に、現行のコードを確認してから決める）。

### 3.5 まとめ（W11）
- 長さのガード（`LoadProfile`／`SaveProfile` が分解後の長さを超えて肥大化しないことを、ソースの走査で検査）、完了報告書、`Phase8-Status.md`、`Phase8-Unified-Trigger-Dispatch-Plan.md` の追記。

## 4. テスト

- ゴールデン 12 件（既存。文面は変更しない）、`ProfileLegacyXmlRoundTripTests`、`ProfileLegacyXmlCompletenessTests`、`Phase7Step10c*GuardTests` が、**各 Wave で成功**すること。
- 4b-0 で足すゴールデン: 非既定値の節（特殊アクション、曲線のモード、ライトバーのモード、旧形式の読込専用要素ほか）。足した後、分解の前の状態でゴールデンを確定する。
- 差分比較: 広い入力（全要素を含む XML）の読込結果が、分解の前後で完全に一致すること。保存結果（XML）も一致すること。
- 各節のクラスの単体テストは、原則として作らない（ゴールデンと差分比較で担保する）。ただし、`ProfileLoadContext` の初期状態と、節を呼ぶ順序（指揮役）は検査する。
- ラチェットの一致検査、1 ファイル 1 型のガード、全既存テストが成功すること。

## 5. Wave 構成（案。目安 11 Wave）

- W1: 4b-0（監査、ゴールデンの追加、差分比較の用意）。
- W2: 基盤（`ProfileLoadContext`、`IProfileLoadSection`、指揮役）と、最初の節（準備・後始末・全般）。
- W3〜W5: 残りの読込の節（3〜4 節ずつ。W5 に「ボタン割り当て」と「特殊アクション」）。
- W6〜W8: 保存の節（基盤と節を 3 つの Wave に）。
- W9: 4b-3a 名前の変換関数。
- W10: 4b-3b ホバー画像・位置の表。
- W11: 長さのガード、完了報告書。

各 Wave で、ユーザーのビルド・テストが成功してから次へ進む。

## 6. 決定してほしい点（質問と推奨）

- **Q1（節の区切り）**: A（推奨）: §2.1 の表のとおり、**ファイル順の連続した範囲**で区切る（読込の順序・相互依存を保つため）。大きすぎる節（軸別の不感帯 約 300 行、`TouchpadMouseStick` 約 200 行）は、1 クラス 500 行以下を目安に、W1 の監査で細かく分ける。B: 内容のまとまり（スティック全体など）で、順序を並べ替えて区切る（順序の依存を再確認する必要があり、リスクが高い）。
- **Q2（読込と保存の関係）**: A（推奨）: 読込側と保存側は**別のクラス群**にする（`Profile*LoadSection`、`Profile*SaveSection`）。保存の順序が読込と異なるため。B: 内容ごとに 1 クラスにして、`Load`／`Save` の両方を持たせる（順序を保つには、保存側の呼び出しを細かく刻む必要がある）。
- **Q3（DI 登録とフォルダー）**: A（推奨）: 節のクラスは、`LegacyProfileXmlSerializer` の実装の詳細として、serializer のコンストラクターで作る（内部用、`internal sealed`、状態を持たない）。DI には登録しない。配置は `DS4Control/Services/ProfileSections/`（名前空間は `DS4Windows.Services`）。B: 節ごとに DI へ登録する（登録が 24 件ほど増え、利点が少ない）。
- **Q4（状態の受け渡し）**: A（推奨）: `ProfileLoadContext`／`ProfileSaveContext` のクラスに共有状態をまとめて渡す。B: 節ごとに引数を並べる（引数が 10 個を超える節が出る）。
- **Q5（ゴールデンの足し方）**: A（推奨）: W1 で、足りない節のゴールデンを**先に足し、分解の前の状態で確定**する。既存のゴールデンの文面は変えない。B: 既存のゴールデンだけで進める（特殊アクションなどの節が未検証のまま動く）。
- **Q6（4b-3 の名前の変換関数）**: A（推奨）: 本体を既存のクラスへ移し、**呼び出し元（25 箇所）を付け替えて、`BackingStore` の関数を削除**する（規約 2.1: 新しい薄いシムを残さない）。B: `BackingStore` に委譲する 1 行の関数を残す（差分は小さいが、公開面が残る）。
- **Q7（4b-3 のホバー画像・位置の表）**: A（推奨）: W10 の前に現行のコードを確認し、方針を再度相談する（WPF の画像生成を含む第 4 層の処理のため）。B: この計画書の承認で、データ用のクラスへ機械的に移す方針まで決める。
- **Q8（長さのガード）**: A（推奨）: W11 で、`LoadProfile`／`SaveProfile` の行数の上限（分解後の実測＋余裕）を、ソースの走査のテストで固定する。B: ガードは作らない。

## 7. リスクと対応

- **読込の順序・副作用の取りこぼし**: 節は連続した範囲で、本文を書き換えずに移す。差分比較（読込結果と保存結果）とゴールデンで検出。1 つの Wave で移す節を少なくし、失敗したときの切り分けを容易にする。
- **`missingSetting` の伝播・例外の握りつぶしの変化**: 文脈のフィールドに集約し、節の中の `catch { … }` を変えない。W1 で一覧にして、各 Wave で確認。
- **ゴールデンが足りない節の見落とし**: W1 で、未カバーの節を要素単位で洗い出す（上の 14 個、34 個を出発点にする）。足りない節は、先に足す（Q5）。
- **`LoadProfile` を使う他の経路**（`ProfilePresetLoader`、`ControlService` の接続処理ほか）への影響: 公開メソッドと引数は変えない。
- **Step5 との調整**: 「特殊アクション」の節（`ProfileActions`）を単独のクラスにする（Step5 が足す場所を明確にする）。

## 8. 完了条件

- [ ] W1〜W11 のビルド・テストが成功する（ユーザーが実行）。ゴールデンの既存の文面が変わっていない。ラチェットの一致検査、1 ファイル 1 型のガードも成功する。
- [ ] `LoadProfile`／`SaveProfile` が、指揮役（それぞれ 150 行前後）になっている。節のクラスが、1 ファイル 1 型で `DS4Control/Services/ProfileSections/` にある。
- [ ] 差分比較（読込結果と保存結果）が、分解の前後で一致している。
- [ ] 4b-3 の 2 件が完了している。
- [ ] 完了報告書、`Phase8-Status.md` の更新。

## 9. 次の Step

- Phase8-Step5（プロファイルの読込・適用の完了後の `TriggerBinding` の構築と、登録簿への `Replace`）。計画書は、Step4b の完了後に作成する。
