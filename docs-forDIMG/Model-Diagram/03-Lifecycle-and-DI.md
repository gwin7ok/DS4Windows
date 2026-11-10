# サービスの寿命と DI 登録

最終更新: 2026-11-10（Phase17-0 で、旧 `04-Service-Lifecycle-Spec.md` を整理して作り直した。旧版は `Archive/`）。
サービスごとの一覧表は手で保守するとコードとずれるため持たない。**登録そのものが正**であり、この文書は方針と読み方だけを書く。

---

## 1. 登録の置き場所

`DS4Windows/Composition/ServiceRegistration.cs` が入口で、実装クラスの層ごとの partial に分ける。新しい登録は、実装クラスの層のファイルへ足す。

| ファイル | 登録するもの |
|---|---|
| `ServiceRegistration.Core.cs` | 設定・永続化・パス・環境・ログなど（Core） |
| `ServiceRegistration.Processing.cs` | 入力の処理・割り当て・機種ごとの接続フック（第2層） |
| `ServiceRegistration.Output.cs` | 仮想パッド・キーボード／マウス出力（第3層） |
| `ServiceRegistration.Ui.cs` | ViewModel・ウィンドウの工場（UI） |
| `ServiceRegistration.Wiring.cs` | 後付けの配線（コンストラクターで渡せないものだけ） |

- 同じ型を複数登録する `IInputDeviceModule` は、解決すると登録順の列になる。並びを変えない。
- 入力機種のカートリッジは `InputDeviceCartridgeServiceCollectionExtensions` で登録する。

## 2. 寿命の方針

| 寿命 | 使う場面 |
|---|---|
| **Singleton** | アプリの起動から終了まで常駐するもの: 設定・リポジトリ・デバイスの登録簿・`Mapping` と処理の段・仮想パッドの出力・工場。状態を持つものは、スレッドセーフにする |
| **Transient** | 開くたびに最新の設定を読み直す画面（ViewModel） |
| **工場で生成** | 引数（プロファイル名・スロット番号）を伴う複合 ViewModel。`IViewModelFactory` 経由で作る |

- ホットパスの処理の段（`CurveAndDeadzoneStage` など）は状態を持たない Singleton か、スロット別の状態を別の保持クラス（`SyntheticInputState`、`StickMotionState`）で受ける。
- プロファイル編集画面では、編集対象（プロファイル）と、プレビュー・校正に使う実機（`targetDevice`。-1 は実機なし）を分けて渡す。

## 3. 依存の渡し方

- コンストラクター注入のみ（Pure DI）。`AppHost` を使うのは `App` と `Composition` だけ（`RemovedApiRuleTests`）。
- 循環や起動順の都合でコンストラクターで渡せないものは `Func<T>` か、`ServiceRegistration.Wiring.cs` の後付けの配線で結ぶ。理由はコメントに書く。
- 引数の数は 15 以下を努力目標とする（`ConstructorArityBaseline`）。

## 4. ライフサイクル（起動と終了）

- 起動: `App` が `AppHost.CreateHost(config, parser)` でホストを作る → `IServiceLifecycle`（`ServiceLifecycle`）が各サービスを初期化 → デバイスの探索を開始。
- 終了: 出力サービス（`IOutputServicesLifecycle`）→ デバイスの切断 → 仮想パッドの破棄の順（ドライバーの破棄順序のガードレール。`Phase5-Plan.md` §5）。
- 切断時: `IDeviceDisconnectHandler` → `IDeviceInputCleanup` → 出力スロットの解放。

## 5. 調べ方

- 「この型はどこで登録されているか」: `ServiceRegistration.*.cs` を型名で検索する。
- 「この型は何を受け取るか」: コンストラクターを読む。
