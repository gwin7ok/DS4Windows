# Phase9-B 計画書: 9-B マウス変換と入力の解決の切り出し

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-B、`Phase9-A1-Plan.md`・`Phase9-A2-Plan.md`（同じ進め方）  
状態: **設計案（未承認）。承認を得るまで、コードは変えない**

---

## 1. 目的と範囲

`Mapping.cs`（2,568 行）に残る、マウスへの変換と入力の解決を、責務ごとのクラスへ切り出す。挙動は変えない（入力 → 出力が一致）。

### 1.1 再計測（2026-10-22）で分かったこと

- 親計画書にある `GetMouseWheelMapping`、`GetByteMapping`、`GetXYAxisMapping`、`getBool*Mapping` 群の大半は、**Phase8 で削除済み**。9-B の実際の対象は次の表のとおり（約 600 行）。
- `getMouseMapping` の呼び出し元は `SampleMouseMovement` の 1 箇所だけ（新経路の `ControlMouseMoveOutputAction` が `TriggerInput` 経由で呼ぶ）。`eState` と `ctrl` の引数は、`ctrl` から設定とデバイスを読むためだけに使っている（`eState` は未使用）。

| 対象 | 行 | 規模 | 性質 |
|---|---|---|---|
| `getMouseMapping` | 2013 | 約 318 | 毎フレーム（ホットパス）。8 方向のスティック分岐が、ほぼ同じコードの繰り返し |
| `calculateFinalMouseMovement`、`remainderCutoff` | 2331、2368 | 約 40 | 毎フレーム。端数の持ち越し（状態あり） |
| `TempMouseJoystick` | 1190 | 約 77 | ジャイロのスティック出力と通常のスティックの合成。`ControlService` が呼ぶ |
| `GetBoolMappingExternal`、`IsUsingGyroForControls` | 2401、2385 | 約 120 | マクロ記録画面（`RecordBoxViewModel`）だけが使う。状態なし |
| `getBoolButtonMapping` ほか 4 つ（static） | 2521〜 | 各 3 行 | 純粋関数。`ControlService`・テストが使う |

## 2. 設計

### 2.1 新しい部品（3 つ。1 ファイル 1 型。契約は `DI/`、実装は `DS4Control/Services/`）

| 部品 | 引き受ける処理 | 状態 | 備考 |
|---|---|---|---|
| `IButtonMouseMovementCalculator` | `getMouseMapping` の計算（ボタン・スティック・トリガー・ジャイロ方向 → マウスの生の移動量） | マウス加速の状態（`mcounter`／`mouseaccel`／`prevmouseaccel`）を持つ（Q2） | 引数を明示する: 方向番号、コントロール、`cState`、`DS4StateFieldMapping`、`ButtonMouseInfo`、左右のデッドゾーン補正値、`DeltaSettingsProcessorGroup`、経過時間（ms）。`ctrl`・`eState` は渡さない |
| `IMouseMotionQuantizer` | `calculateFinalMouseMovement`（端数の持ち越しと整数化） | 端数（`horizontalRemainder`／`verticalRemainder`）を持つ（Q2） | 時刻に依存しない |
| `IGyroMouseStickBlender` | `TempMouseJoystick`（ジャイロ由来のスティック値と、通常のスティック値のうち、中心から遠い方を採る） | なし（`StickMotionState` の配列を引数で受ける） | |

`GetBoolMappingExternal` は、**状態を持たない 1 つの部品 `IControlActivityReader`（仮称）に移す**（Q3）。`IMapping.GetBoolMappingExternal` は、窓口を変えないため、薄い委譲として残す（Standalone のテストが `IMapping` を実装しているため）。

### 2.2 変えないもの

- `IMapping` のメンバー、`getBool*Mapping`（純粋関数。`ControlService` が使う）、`remainderCutoff`（`MouseCursor` が使う）、`MOUSESPEEDFACTOR`（`ProfileSettingsViewModel` が使う）は `Mapping` の static のまま（親計画書 Q2＝A の方針）。
- 新しい static の窓口は作らない。部品は Singleton で登録し、`Mapping` のコンストラクターで受ける（Pure DI。18 → 21 引数）。

### 2.3 Wave の分け方（1 Wave ＝ 1 部品。各 Wave の前にテストを足す）

| Wave | 内容 | 先に足すテスト |
|---|---|---|
| **B-1** | `calculateFinalMouseMovement` → `MouseMotionQuantizer` | 入力の列（正負、端数、符号が反転する場面）→ 出力の整数と端数のゴールデン。既存の `AnalogMouseTestHarness` は端数を `Mapping` のフィールド名で初期化しているため、新しい持ち主に追従する |
| **B-2** | `getMouseMapping` → `ButtonMouseMovementCalculator`。まず**そのまま移す**（コードの形を変えない）。その後の**別のコミット**で、8 方向の繰り返しを 1 つの補助に畳み、200 行以下にする | ボタン／スティック（デッドゾーン 0 と非 0、デルタ加速の有無）／トリガー／ジャイロ方向 × 方向番号 4 × 感度・縦倍率・加速の格子のゴールデン。加速は呼び出しの列（連続して値が出る／途切れる）で固定 |
| **B-3** | `TempMouseJoystick` → `GyroMouseStickBlender`、`GetBoolMappingExternal` → `ControlActivityReader` | ブレンダー: ジャイロ値と通常値の大小・128 の場面・出力スティック 3 種（None／左／右）・水平垂直の組。リーダー: 全コントロール × 状態の表（しきい値 55／100／67 の境界、タッチパッド null、ジャイロ有効／無効） |

各 Wave の終わりに、`Debug ALL`（警告 0・エラー 0・全テスト合格）、割り当て 0（`MappingHotPathAllocationTests`）、性能の計測（`Phase9HotPathTimingBenchmark`。許容は 5% 以内の悪化）を行う。ゴールデンは、置き換えの前後で 1 バイトも変えない。

### 2.4 期待する結果

- `Mapping.cs`: 約 2,568 → 約 1,950 行（約 600 行減）。`getMouseMapping` 相当は 200 行以下。

## 3. 調査で見つけた、現行の癖（挙動を変えない前提で、記録する）

| # | 内容 | 扱い |
|---|---|---|
| K1 | マウス加速の状態（`mcounter`／`mouseaccel`／`prevmouseaccel`）と、移動の端数（`horizontalRemainder`／`verticalRemainder`）は、**コントローラー（device）ごとではなく、`Mapping` の 1 つを全コントローラーで共有**している。2 台以上を同時に使うと、加速の進み方や端数が互いに影響する | 親計画書の原則（挙動を変えない）に従い、**そのまま 1 つで共有**する。デバイスごとへの分離は別の是正としてユーザーの判断を仰ぐ（Q2） |
| K2 | `getMouseMapping` のジャイロ方向は、`value = (byte)(gyroX > 0 ? Math.Pow(...) : 0)` と **`byte` にキャスト**している。値が 255 を超えると桁あふれし、小数部も切り捨てられる | そのまま移し、ゴールデンで固定する。是正は別途（旧実装［上流］からの癖の可能性が高い） |
| K3 | `public int mcounter`／`mouseaccel`／`prevmouseaccel` は `public` だが、`Mapping` の外からの参照は 0 件 | 新しい持ち主へ移し、`public` をやめる |
| K4 | `public static bool compare(byte, byte)` は呼出元 0 件。`MOUSESTICKANTIOFFSET`／`MOUSESTICKMINVELOCITY` は、使用箇所がコメントの中だけ | 規約 2.4 の手順で調べた。`git log -S` は、巨大な一括コミット（dd10cbb）までしか遡れず、経緯は不明。上流（DS4Windows）由来の未使用コードで、**DI 化の契約ではない**ため、**「使うべきでないもの」として削除を提案**する（Q4）。コメントアウトされた旧コードは、規約 3.3 原則 4 に従い消さない |

## 4. 質問（推奨は A）

- **Q1（Wave の粒度）**: A＝B-1 → B-2 → B-3 の順（推奨。B-1 が最も小さく、状態の持ち方［Q2］の確認に向く）／B＝まとめて 1 回。
- **Q2（K1 の共有状態）**: A＝**今回は、全コントローラーで共有のまま移す**（推奨。挙動を変えない）。分離は、別の是正として後で判断／B＝デバイスごとに分離する（2 台同時の利用では挙動が変わる。実機確認が要る）。
- **Q3（`GetBoolMappingExternal` の扱い）**: A＝部品に移し、`IMapping` は薄い委譲で残す（推奨）／B＝`Mapping` に残す（B-3 から外す。約 120 行が残る）。
- **Q4（K4 の未使用コード）**: A＝`compare` と、使われていない 2 つの定数を削除する（推奨。ただし、コメントの旧コードは残す）／B＝残す（TODO を付ける）。
- **Q5（B-2 の畳み込み）**: A＝移した後の別コミットで、8 方向の繰り返しを畳む。ゴールデンが 1 バイトも変わらないことが条件（推奨）／B＝畳まない（`getMouseMapping` 相当は 300 行のまま）。

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| ホットパスの性能の悪化・割り当て | 部品は参照型のフィールドから呼ぶだけで、`new` を行わない。Wave ごとに計測（5% 以内） |
| 加速の状態の持ち方で、挙動が変わる | Q2＝A で、1 つの共有のまま。ゴールデンは呼び出しの列で固定 |
| 浮動小数点の演算順序が変わり、ゴールデンがずれる | まず、コードをそのまま移す。畳み込みは別コミットで、ずれたら戻す |
| 既存テストがフィールド名（`horizontalRemainder` など）を文字列で参照している | `AnalogMouseTestHarness` を追従する。リフレクションで読むガードは、`rg` で洗い出して更新する |
| ラチェット（`Global`／`AppHost` ほか）の基準値 | 新しい静的な窓口は作らない。件数は変わらない見込み。一致検査で確認する |

## 6. 実機確認（想定。実装後に依頼）

(1) スティック・ボタン・トリガーをマウス移動に割り当て、従来どおり動くこと（感度、加速）。(2) ジャイロのマウススティック出力が、通常のスティックと合成されること。(3) マクロ記録画面で、各ボタン・スティック・タッチパッドの押下が検出されること。

## 7. 次

承認（Q1〜Q5）を得たら、B-1 から着手する。ビルド・テストが必要になった時点で、ユーザーへ依頼する（B-1 の実装後）。
