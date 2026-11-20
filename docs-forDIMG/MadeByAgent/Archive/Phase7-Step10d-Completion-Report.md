# Phase7-Step10d 完了報告書: `BackingStore` のコンストラクター注入と `Global` の廃止

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step10d-Plan.md`（Q1〜Q9 はすべて推奨どおり承認）。上位: `Phase7-Step10-Plan.md`（Q6＝A）  
状態: 実装完了。W1〜W5（W6 を含む）のビルド・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。ゴールデン 11 ファイルも一致）。W7 は文書のみ。実機確認 U-21 は未。

---

## 1. 結果の要約

- **`Global`（`Global.cs`、562 行・53 メンバー）を削除した。** `DS4Windows.Global` の型は存在しない。`using static DS4Windows.Global;`（`Mapping.cs`、`DS4LightBar.cs`）も削除した。
- **`BackingStore` はコンストラクターで `IEnvironmentService` を受けて生成する**（旧: `Global` の静的な初期化が `new BackingStore()`）。App のブートストラップが、コンテナの構築より前に `BackingStore` を作り、`CreateHost` へ渡して、同じインスタンスをコンテナへ登録する（`PathService`／`EnvironmentService` と同じ方式）。
- 数値: `Global.*` 69 → **2**（残りはログ文言の文字列 `"Global.UseLang …"` で、ログ内容の維持のため残した。コードの参照ではない）。`AppHost.GetService|Services` 4 → **3**、`Program.rootHub` 0、`ServiceProviderHolder` 17（Phase8）。Phase7 開始時（733）からの累計は −731（実質ゼロ）。
- 機能・ログ・起動順序は変えていない。ガードレール 5.1〜5.6 への影響なし。

## 2. Wave ごとの実績

- W1: テストの移行。`TestGlobals`（テスト用の入口）を新設し、`Global.store`／`appdatapath`／`exedirpath`／`ProfileSettingsServiceInstance` の参照 296 件（47 ファイル）を置き換えた。回帰ガード `Phase7Step10dW1TestGlobalsGuardTests`。再実行 0 回。
- W2: 環境・パスの窓口と定数。`AdminCheck`（管理者判定）、`DevicePathProbe`（デバイスパスの判定）、`ConfigFileCulture`（設定ファイルの小数の文化）を新設。`exeversion`／`exelocation` は `PathService` の状態を直接使い、定数の別名（`CONFIG_VERSION` ほか）は持ち主へ移した。再実行 0 回。
- W3: 第 1 層の設定の読み取り。`IDeviceLayerSettings`（`DeviceLayerSettings`）を新設し、`DS4Devices`／`DS4Device`／`DS4SixAxis`／`HidDevices` が `Global` を読まない形にした。出力デバイスは `OutputSlotManager` から関数で軸を受け取る。再ビルド 1 回（App の再有効化の経路が引数なしで `Ds4DeviceRegistryAdapter` を作っていた）。
- W4: アプリ層の静的な窓口。`BatteryChanged`／`DeviceSerialChange` をサービスのイベントに、`SetCulture` を `UiCulture` に、`CreateAutoProfiles` を `AutoProfileService` に、更新確認のスキップ判定を引数にした。再実行 1 回（テスト 3 件）。
- W5（W6 を含む）: `BackingStore` のコンストラクター注入、ブートストラップの `BackingStore`、`RefreshActionAlias`・`ProfileSettingsServiceInstance` の整理、`DS4LightBar` への設定の受け渡し、`Global.cs` の削除、ガード・ラチェットの整理。再実行 2 回（§3）。
- W7: 台帳 §16、モデル図、U-21、完了報告書。

## 3. 計画との差と、調査で判明した事項

- **W5 と W6 を一つにした。** `Global` の残りのメンバーが `m_Config`（`BackingStore` の静的な生成）を使うため、`BackingStore` のコンストラクターを変えると `Global` はコンパイルできない。中間状態を作らず、同じ Wave で削除した。
- **`ProfileSettingsServiceInstance` の置き換え（Q8）**: `ProfileSettingsService` が `BackingStore` を受ける（逆向きの依存）ため、`BackingStore` はコンストラクターで受けられない。`BackingStore.ProfileSettings` に、`ProfileSettingsService` が自分の生成時に登録する（**最初の 1 つが有効**。旧: 最初のコンテナの instance を静的に保持）。登録がないまま使われると、従来のフォールバックと同じく、コントローラーを持たない `ProfileSettingsService` を 1 度だけ作る（旧: 呼ぶたびにログを出していた）。`RefreshActionAlias` の本体は `ProfileSettingsService` へ移した。
- **コンストラクター注入にしなかった項目（理由つき）**: `IProcessInspector`（ブートストラップの `BackingStore` はコンテナより先に作るため、`ServiceRegistration` の登録が設定する）、`ActiveOutDevType`（配列を持つ `OutputSlotService` が後で作られ、`BackingStore` の登録から解決すると循環する）。いずれも `BackingStore` のコメントに理由を記録した。
- **`PathService` の `config` 引数**: 省略時の `Global.store` をやめた。省略したインスタンスは、保存場所の決定（`SetSaveLocation`／`FindConfigLocation`）だけが使えない（`InvalidOperationException`）。App のブートストラップは `BackingStore` を渡す。
- **`AppLogger.NotificationService` の既定**: 設定前（ホスト構築より前）の退避先は、`Global.store` から、既定の値の `BackingStore`（`new BackingStore(new EnvironmentService())`）に替えた。App が起動時に設定した後は使われない。
- **`DS4LightBar`**: `Global` の転送（`getLightbarSettingsInfo` ほか）に替えて、`updateLightBar(device, deviceNum, profileSettings)` の引数で受ける。`IProfileSettingsService` に `DistanceProfiles`（`BackingStore.distanceProfiles` の配列そのもの）を追加した（実装は 1 つ）。
- **挙動の差（いずれも意図的、または影響なし）**:
  - `AppearanceSettingsService.BatteryChanged` の送信元が `typeof(Global)` からサービスのインスタンスになった（購読者は送信元を使っていない）。
  - `DS4Device` のデバウンスは、第 1 層の設定が未設定のとき 0ms になる（旧: 設定が無いと例外）。本番では `Ds4DeviceRegistryAdapter` が設定する。
  - 第 1 層の `Moonlight`／詳細ログは、未設定のとき無効として扱う（テストのみ）。
- **テストの扱い**: 共有の `BackingStore` は、テストのコンテナ（`AppHost`）が持つ実体（`TestGlobals.Store`）。ゴールデンテストの単独の `BackingStore` は、共有のサービスに結び付ける（`PersistenceHarness.NewStore`）。`BackingStoreStateFiller` は `ProfileSettings`（別サービスへの参照）を書き換えない。ソース位置の起点は `Global.cs` から `BackingStore.cs` に替えた（15 ファイル）。
- **再実行の原因（いずれもテストの問題。本体の回帰ではない）**: 初期化子つき `new BackingStore { … }` のテスト 4 件、削除した型を参照するテスト 4 件、`BackingStoreStateFiller` が `ProfileSettings` まで変えたことでゴールデン 2 件、AppHost の登録順ガードの文字列。
- **残る静的な状態（記録）**: `PathService` の静的な状態（`AppDataPathState` ほか）と `EnvironmentService` の静的な状態、`ServiceRegistration._subscribedReset`（コンテナの作り直しで購読を重複させないための記憶）。`AppHost` 自体が静的（10e の判断対象）。
- **未接続・孤立コード**: 新しく見つけたものはない。

## 4. 起動順序・スレッド・ガードレールへの影響

- 起動順序: `EnvironmentService` → `BackingStore` → `PathService`（`FindConfigLocation`）→ ログのローテーション → 最小ログレベル → … の順（`Phase7Step10dW5GlobalRemovedGuardTests` が App のソースで固定）。読み取る値は、設定の読込前の既定値（DI 移行前からの動作）。`-driverinstall` 分岐も同じ `BackingStore` を使い、設定の読込後に言語・テーマを読む。
- 5.1（同一 XML の排他ロック）、5.2（適用時 Halt）、5.4（On-Demand パス評価）、5.5（ドライバ破棄順序）、5.6（切断時クリーンアップ）: 変更なし。ホットパスに新しい確保はない（第 1 層の設定は、静的に保持した読み取り専用のオブジェクトの呼び出し）。

## 5. 未完了・持ち越し

- **実機確認 U-21**（チェックリスト §11）: 起動直後のログ設定、言語・テーマ、トレイのバッテリー表示、Moonlight、デバウンス、ライトバー、`-driverinstall`。U-11〜U-20 も未（U-13 は先送り）。10e の総合検証でまとめて行う。
- 10e: `CURRENT_DS4_CONTROLLER_LIMIT`（画面・サービスの分）、`AppHost` の暗黙のホスト構築と `CreationNote`、`Mapping` の依存（11）の記録、`ColorUtil` の孤立コードの判断（承認が必要）、`MainWindow` の引数 18 の記録、総合検証（U-11〜U-21）と GO/NO-GO、Phase7 の完了報告、`copilot-instructions.md` の更新。

## 6. 次の Step

- **10e**: 調査と `Phase7-Step10e-Plan.md` の作成から着手する（ユーザーの指示を待つ）。
