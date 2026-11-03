# Phase13-Step2 個別計画書: `ControlService` の未使用コンストラクター引数の削除

作成日: 2026-11-04
状態: **承認待ち**
ブランチ: `Phase13`（Step1 は `d0729a8c` でコミット済み）
上位: `Phase13-Plan.md` §4（Step2）、§9（リスク R6）

## 1. 目的

`ControlService` が保持するだけで使っていないコンストラクター引数を削除する。**34 → 28。**

## 2. 調査結果（2026-11-04）

### 2.1 削除する 6 引数

| 引数 | `ControlService.cs` 内の使用 | 外部の利用 | 判定 |
|---|---|---|---|
| `Services.IProfileSlotApplier profileSlotApplier` | フィールドと代入のみ | — | 削除 |
| `SyntheticInputState syntheticInput` | 同上 | — | 削除 |
| `DI.IJoyConLinker joyConLinker` | 同上 | — | 削除 |
| `DI.IConnectionProfileSelector connectionProfileSelector` | 同上 | — | 削除 |
| `DI.IDeviceConnector deviceConnector` | 同上 | — | 削除 |
| `DI.IProfileXmlStore profileXmlStore` | internal アクセサ `ProfileXmlStore`（158 行）に公開するだけ | **アクセサの利用者は 0 件**（本番・テスト・Standalone を検索。`ProfileXmlStore.XmlIoLock` の静的参照は別物で、影響なし） | フィールドとアクセサごと削除 |

計画書 §2.1 の「5 個 + `profileXmlStore`」の確認結果。M6（アクセサの再確認）は「使われていない」で確定。

### 2.2 R6（生成のタイミング・購読の配線）の検査結果

削除する型が、`ControlService` の削除後も従来どおり生成されるかを確認した。

- `IDeviceConnector`、`IJoyConLinker`: `ServiceLifecycle` が受ける。`ServiceLifecycle` は `ControlService` が受けるので、同じ経路で生成される。
- `IConnectionProfileSelector`: `DeviceConnector` が受ける（上と同様）。
- `IProfileSlotApplier`: `ConnectionProfileSelector`、`DeviceDisconnectHandler`、`MainWindowFactory` が受ける。
- `SyntheticInputState`: `Mapping`、`Mouse`、`DeviceConnector` ほか 8 ファイルが受ける。
- `IProfileXmlStore`: `ProfileRepository` ほかが受ける。
- 上記の型のコンストラクターは、イベント購読（`+=`）を行わない（`DeviceConnector` の購読はメソッド内）。**生成のタイミングが変わっても、配線の抜けは起きない。**
- 生成順の保証（`ControlService` の生成時に、`ServiceLifecycle` を通じて接続系が全部作られる）は変わらない。

## 3. 変更内容

1. `ControlService.cs`: 6 引数、対応するフィールド（`_profileSlotApplier`、`_syntheticInput`、`_joyConLinker`、`_connectionProfileSelector`、`_deviceConnector`、`_profileXmlStore`）、代入、アクセサ `ProfileXmlStore` を削除する。各フィールドにつけたコメント（「Phase9 9-E E-6a」など）のうち、削除したフィールド専用のものも削除する。他のフィールドのコメントは残す。
2. `ServiceRegistration.cs` の `new ControlService(…)`: 対応する 6 つの `GetRequiredService` を削除する。
3. `Phase7Step8W1…` など、`ControlService` のコンストラクターの形を検査する既存テストがあれば更新する（検索で確認する。`new ControlService(` はレジストリ 1 か所のみ）。
4. 規約 §3.2 のとおり、ファイル全体は再生成せず、該当箇所だけを置換する。

## 4. 規約への適合

- 機能維持（§2.2）・ログ（§2.3）: 処理の変更なし。
- 1 ファイル 1 型、配置、名前空間: 新規ファイルなし（テストを 1 つ追加）。
- ラチェット: `Global`／`AppHost`／`ServiceProviderHolder` の件数に変化なし。

## 5. テスト計画

新規 `Phase13Step2ControlServiceConstructorGuardTests`（1 ファイル 1 型）:
- `ControlService` のコンストラクターの引数が 28 個で、削除した 6 つの型を含まない（リフレクションで固定。引数の増減を意図せず戻さないためのガード）。
- `TestHost` から `ControlService` を解決した後、削除した 6 つの型が解決でき、`ControlService` 以外の経路で生成される Singleton であること（`IDeviceConnector` などが `ServiceLifecycle` から参照できること）。
- 既存テスト全件が合格すること（接続・切断・プロファイル適用の特性化テストを含む）。

`Debug ALL`（Remove-bin → Build Debug → Build Test → Test）で警告 0・エラー 0。

## 6. 実機確認

不要（Phase 末の H1・H2 で担保）。

## 7. リスク

- 低。コンパイルエラーが取りこぼしを示す。唯一の注意点は生成のタイミングで、§2.2 のとおり配線の抜けは起きない。

## 8. 決めてほしいこと

なし。

## 9. 完了条件

`ControlService` の引数が 28 個。ガードテストを含む全テストが合格。`Phase13-Status.md` を更新。
