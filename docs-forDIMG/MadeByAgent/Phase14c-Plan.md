# Phase14c 個別計画書（案）: 共有の値の型の確定と、UI に触れない静的な状態の撤廃

作成日: 2026-11-05
状態: **承認済み（2026-11-05。E1〜E4 はユーザーがすべて推奨案を選択、E5・E6 は Phase14b-2 の D15・D16 と同じ扱い）→ 未着手**
ブランチ: `Phase14c`（master 341d202c から作成）
根拠: `Repository-Review-2026-11-05.md`（R-05・R-06・R-09・R-13・R-15〜R-18）、`Phase14b-Plan.md`（§1 の対象外、§10）、`Phase11-and-Beyond-Roadmap.md`（Phase15・16）

## 1. 何を決めるための計画か

Phase14b の後に残った次の項目を、**Phase14c（Phase15 の前）で行うか、Phase15 以降へ回すか**を、他の作業との重なりで振り分ける。

- 「記録のみ」の違反: L2（Core → 第1層・第3層）75 件、L7（UI → 第1層・第3層）80 件
- `MainWindow` のコンストラクターの引数 27 個
- レビューの R-05 以降（静的な状態の撤廃、カートリッジ化ほか）

## 2. 振り分けの判断の軸

| 軸 | 考え方 |
|---|---|
| A. 名前空間を先に確定させる | 型の移動は名前空間を変える。Phase15 で UI の部品・XAML を作り直す前に済ませれば、新しい UI を最終の名前空間で書ける（Phase14・14b と同じ理由）。 |
| B. 同じファイルを 2 度触らない | UI の画面・ViewModel を大きく触る作業（UI 向けの窓口、引数の整理、変更通知の基底）は、Phase15 の画面の作り直しと同時に行う。 |
| C. 第1層の作り直しと一緒にする | 第1層のデバイスクラスの分離（9-H、Phase16）に深く関わるもの（`DS4Device`、`DS4Devices`、`DS4State`）は、Phase16 で行う。今触ると、Phase16 で作り直しになる。 |
| D. 独立して小さく済むものは早く | 他の作業と重ならず、利用者に見える変化がないものは、Phase14c でまとめて片付ける。 |

## 3. 実測（2026-11-05）

### 3.1 「記録のみ」の 155 件の内訳（参照先の型）

| 種類 | 型 | L2 | L7 | 性質 |
|---|---|---|---|---|
| 値の型（列挙型・構造体・名前の表） | `DS4Controls`、`X360Controls`、`OutContType`、`DS4Color`、`InputDeviceType`、`TriggerEffects`、`DS4ControlNames`、`X360ControlTables` | **45** | **44** | 振る舞いなし。XAML からの参照は 0。移動は機械的（参照: `DS4Controls` 189 ファイル、`X360Controls` 82、`OutContType` 57、`DS4Color` 45、`InputDeviceType` 31 ほか） |
| 振る舞いを持つ型 | `DS4Device`（L2 8、L7 11）、`DualSenseDevice`、`OutputSlotManager`、`OutputDevice`、`OutSlotDevice`、`DS4LightBar`（L7 5）、`DS4State`、`DS4SixAxis` ほか | 30 | 36 | 窓口（契約）の設計が要る。UI 側は R-09、Core 側は `IProfileLoadTarget` などのポートと第1層の作り直し |

L7 の参照元の上位: `RecordBoxViewModel` 7、`BindingWindow` 5、`ControllerReadingsControl` 5、`ProfileEditor` 5、`ProfileSettingsViewModel` 5、`SlotDeviceEntry` 4、`MappedControl` 4（いずれも Phase15 で触る画面）。

### 3.2 静的な状態（レビュー R-05・R-06）

| 対象 | 規模 | 重なり |
|---|---|---|
| `SyntheticDispatcher` の静的な橋渡し（R-06） | `KeyButtonActionController` ほか 4 種類のメソッド・計 26 か所 | 第2層の内側だけ。UI・第1層と重ならない |
| `DS4LightBar` の静的な配列（`forcelight`／`forcedColor`／`forcedFlash`。R-05） | 15 ファイル（UI 5） | 状態の持ち主の置き換えは機械的。色の計算の移動（`ILightbarService`）は第1層の作り直しと重なる |
| `DS4Devices` の静的な状態 | `Ds4DeviceRegistryAdapter` が包む | 第1層の作り直し（9-H）の中心 |
| 静的な `ActionRegistry` | **本番では `Initialize` が呼ばれていない**（呼ぶのはテスト 1 件だけ）。`DefaultActionManager` が回すが常に空 | 規約 §2.4（未参照のコード）の扱い |
| 登録ラムダの後付けの配線（R-13）、`ServiceRegistration` の 300 行の 1 メソッド（R-18） | Composition だけ | カートリッジ化（Phase16）の受け皿になる |

## 4. 振り分けの案（推奨）

| 項目 | 振り分け | 理由（§2 の軸） |
|---|---|---|
| 値の型 8 つの `Core` への移動（L2 45 件、L7 44 件） | **Phase14c** | A・D。名前空間を Phase15 の前に確定。機械的で、Phase14b の補助スクリプトと手順をそのまま使える |
| R-06 `SyntheticDispatcher` の静的な橋渡しの撤廃 | **Phase14c** | D。第2層の内側だけで閉じる |
| R-05 の前半: `DS4LightBar` の静的な配列を、状態の持ち主（例: `ILightbarOverrideState`）へ | **Phase14c** | A・D。UI の 5 ファイルも触るが、Phase15 の前に済ませれば UI は最初から契約で書ける |
| `ActionRegistry` の経緯の調査と、削除または TODO | **Phase14c** | D。規約 §2.4 の手順（削除なら承認） |
| R-13・R-18 `ServiceRegistration` の分割と後付けの配線の整理 | **Phase14c** | D。Phase16 のカートリッジの登録の受け皿 |
| 「記録のみ」の L2・L7 を件数のラチェット（増えたら失敗、減るのは可）に | **Phase14c** | D。Phase15・16 で是正するまでの間に、増えないようにする |
| R-15・R-16 の運用ルール（Phase 履歴のコメント、テストの命名） | **Phase14c**（規約への追記だけ。既存の書き換えはしない） | D |
| R-09 UI 向けの窓口（`ControlService` の公開状態の置き換え）、`MainWindow` の引数 27 個、L7 の振る舞いの 36 件 | **Phase15**（最初の Step） | B。同じ画面を Phase15 で作り直す |
| R-10 `IProfileSettingsService` の分割、R-12 変更通知の基底、R-14 ViewModel の生成 | **Phase15** | B。G-1（サブ ViewModel 化）・F-4 と同じ画面 |
| R-05 の後半: ライトバーの色の計算を `ILightbarService` へ | **Phase16** | C |
| `DS4Devices` のインスタンス化、L2 の振る舞いの 30 件、R-17（`DS4State` など層間の状態の型） | **Phase16** | C。9-H と同時 |
| R-07・R-08・R-11（入力・出力のカートリッジ化、機種の設定の共通化） | **Phase16** | C（既存のロードマップどおり。R-18 の分割が受け皿） |

見込み: Phase14c の後、禁止の規則は 0 件のまま、記録のみは 155 → 約 66 件（値の型の 89 件が消える）。

## 5. 決定事項（2026-11-05 承認。§6 の Step はこの内容が前提）

| # | 項目 | 決定 |
|---|---|---|
| E1 | Phase14c を作り、§4 の「Phase14c」の行を行う | 作る |
| E2 | 値の型の置き場所 | `Core/Model` を新設（層をまたぐ値の型の置き場所。規約 §3.4 R1 に追記） |
| E3 | `ActionRegistry`（本番で未初期化） | §2.4 の手順で経緯を調べ、使うべきでなければ削除 |
| E4 | 「記録のみ」の L2・L7 | 件数のラチェット（増えたら失敗） |
| E5 | 実機確認 | Phase14c の最後に 1 回 |
| E6 | 取り込み | 実機確認の後、master へ fast-forward で取り込み push |

## 6. Step 構成（E1〜E6 が推奨案のとき）

| Step | 内容 |
|---|---|
| 14c-1 | 「記録のみ」の L2・L7 の件数のラチェット（E4）。今の 155 件を基準値にする |
| 14c-2 | 値の型 8 つを `Core/Model` へ（移動のコミット → 名前空間の書き換えのコミット。E2） |
| 14c-3 | R-06: `SyntheticDispatcher` の静的な橋渡しを撤廃（`IKeyButtonActionControllerFactory` が `ISyntheticDispatcher` を渡す） |
| 14c-4 | R-05 の前半: ライトバーの強制色の状態の持ち主（Singleton）へ置き換え |
| 14c-5 | `ActionRegistry` の調査と処置（E3。削除の場合は承認済みとして進める） |
| 14c-6 | R-13・R-18: `ServiceRegistration` を層ごとの登録に分割し、後付けの配線を 1 か所へ |
| 14c-7 | 規約（R1 の `Core/Model`、R-15・R-16 の運用ルール）、モデル図、ロードマップ（Phase15・16 への追記）、記録 |
| 14c-8 | Debug ALL、Release ビルド 2 か所 → 実機確認（E5）→ 取り込みと push（E6） |

## 7. Phase15・16 への追記（ロードマップ。14c-7 で反映）

- Phase15 の最初の Step: R-09（UI 向けの窓口。`ControlService` の `DS4Controllers`／`slotManager`／`touchPad` などの置き換え）と、`MainWindow` の引数の整理。L7 の残り（振る舞いの型）をここで減らす。続けて R-10・R-12・R-14 を G-1・F-4 と同時に。
- Phase16: R-05 の後半（`ILightbarService`）、`DS4Devices` のインスタンス化、R-17、L2 の残り、R-07・R-08・R-11（`ServiceRegistration` の分割を受け皿に）。

## 8. リスク

| リスク | 対策 |
|---|---|
| `DS4Controls` は毎フレームの経路（Mapping）でも使う | 列挙型の名前空間が変わるだけで、IL の値は同じ。性能への影響はない。全テストと実機で確認 |
| 名前の表（`DS4ControlNames`、`X360ControlTables`）は public の static な（書き換え可能な）フィールド | 移動だけで中身は変えない。読み取り専用化は別途（今回は範囲外） |
| `SyntheticDispatcher` の撤廃でキーの送出の順序が変わる | 送出の実体（`SyntheticDispatcherService`）は同じインスタンス。既存の特性化テストで守る |
