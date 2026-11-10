# Phase17 個別計画書（草案）: 対応デバイスの追加しやすさと、構造・モデル図の簡素化

作成日: 2026-11-10
状態: **草案。決定 D1〜D5 は回答済み（§6）。計画書の承認待ち。承認までコードは変更しない**
ブランチ: 承認後に `Phase17` を master（2a30c0e6）から作成
根拠: `Model-Diagram/01〜04`、`Phase16-Plan.md` §12・§13、`Phase11-and-Beyond-Roadmap.md` の Phase17（P17-1〜P17-8）、`Repository-Review-2026-11-05.md` R-07・R-11
規約: `.github/copilot-instructions.md`（1 章の依存の向き、2.2 機能の完全維持、3.3 の 5 原則、3.4 のフォルダ規則）

---

## 1. 目的

1. **対応デバイスを足すときの作業とファイル数を減らす**（現状: 約 28 ファイル、約 95 か所。§2.1）。
2. **構造をより単純にする**: 機種ごとの情報が散らばる場所を 1 か所に集め、継承の深い巨大クラス（`DS4Device` 約 2,000 行 ＋ 派生 4 つで各 600〜1,900 行）への依存を減らす。
3. **モデル図そのものを簡素にし、実装と食い違わないようにする**（4 文書 2,279 行。実在しないノードが多い。§2.3）。

実機は DS4 だけのため、**DS4 以外の機種の動作を変える変更は最小にし、差分テストで守る**（§4）。

## 2. 現状調査（2026-11-10、master 2a30c0e6）

### 2.1 デバイスを 1 つ足すときに触る場所（Switch Pro を例に実測）

| 区分 | ファイル | 内容 |
|---|---|---|
| 第 1 層 | `Input/Devices/SwitchProDevice.cs`（1,380 行）、`SwitchProDeviceCartridge.cs`、`Input/Parsers/SwitchProReportParser.cs` | デバイス本体、VID/PID と生成、解析 |
| Core/Model | `InputDeviceType.cs`、`InputDeviceFamily.cs`、`InputDeviceTypeInfo.cs` | 種類、系統、対応表（同じ事実が 3 か所） |
| 第 2 層 | `Processing/Services/SwitchProDeviceModule.cs`、`ServiceRegistration.Processing.cs` | 接続フック、登録 |
| 設定 | `Core/Settings/SwitchProDeviceOptions.cs`、`SwitchProControllerOptions.cs`、`ControlServiceDeviceOptions.cs` | 有効・無効、機種ごとの設定、入れ物 |
| 保存 | `SwitchProSupportSettings.cs`、`SwitchProControllerOptsDTO.cs`、`InputDeviceOptions.cs`、`AppSettingsDTO.cs`、`AppSettingsXmlSerializer.cs` | XML の形 |
| UI | `SwitchProControllerOptionsWrapper.cs`、`ControllerRegDeviceOptsViewModel.cs`、`FirstLauchUtilViewModel.cs`、`SettingsViewModel.cs`、`ControllerRegisterOptionsWindow.xaml`、`FirstLaunchUtilWindow.xaml`、`MainWindow.xaml`、`Strings.*.resx` | 画面、文言 |

合計: 非テストのファイル **28 個**、名前の参照 **95 か所**。「機種を足す = 同じ形のクラスを約 10 種類コピーして書き換える」作業になっている。

### 2.2 デバイス本体の構造
- `DS4Device`（2,015 行）が、読み取りループ、出力スレッド、切断、生存確認、較正、状態の保持を持つ。派生の `DualSenseDevice`（1,466）、`JoyConDevice`（1,906）、`SwitchProDevice`（1,380）、`DS3Device`（621）は、その上に同じ形のコード（イベントの override、`StartUpdate`、`StopOutputUpdate`、`DisconnectBT`、`IsAlive` など）を重ねている。
- Phase16 で、通信（`IHidTransport`）、解析（`IInputReportParser`、約 40〜95 行ずつ）、ライトバー、シリアルの読み方は分離できた。**残る大きな塊は、読み取りループ・出力レポートの組み立て・切断の手順**で、機種ごとにほぼ別のコピーである。
- Vader 4 Pro の対応は Phase6-Step10 で削除済み。モデル図には名残の `Vader4ProReportParser` が目標像として残っている。

### 2.3 モデル図の現状
- `01`（336 行、依存図）、`02`（224 行、層の図）、`03`（1,361 行、クラス図）、`04`（358 行、ライフサイクルの表）。
- 実在しないノード（`InputLoopCoordinator`、`MappingPipelineContext`、`IInputMappingPipeline` と各 Processor、`IMouseEngine`、`IDeviceHotplugMonitor`、`Vader4ProReportParser` など）が多く、実装の図と目標の図が混ざっている。履歴の `%%` コメントも積み上がり、現状をつかみにくい。
- 図と実装の一致を確かめる検査がない（層の規則は `LayerDependencyRuleTests` で検査済み）。

## 3. 提案する構造

### 3.1 中心の考え方: 「機種 1 系統 = 記述子 1 つ ＋ プロトコル 1 つ」

```text
InputDeviceDescriptor（機種の系統ごとに 1 つ。DI に 1 行で登録）
 ├ 識別       : 系統 ID、表示名、VID/PID の表、機能フラグ、接続方式の判定
 ├ プロトコル : IDeviceProtocol の工場（解析器、シリアルの読み方、出力レポートの組み立て、初期化・較正、切断の手順）
 ├ 設定の定義 : DeviceSettingDescriptor の一覧（キー、型、既定値、XML の節名・要素名、表示名のリソースキー）
 └ 接続フック : 接続時・プロファイル適用時の機種固有の処理（いまの IInputDeviceModule）

ControllerSession（全機種で共通。読み取りループ、出力スレッド、切断、生存確認）
 └ IDeviceProtocol を使う（継承でなく、組み合わせ）
```

- 機種の追加が「プロトコル 1 つ ＋ 記述子 1 つ ＋ 登録 1 行（＋任意で設定の定義）」になる。種類・系統・対応表の 3 か所の事実は、記述子 1 つに集まる。
- 設定の定義から、保存（XML）と画面（チェックボックス・選択肢）を作る（XML の形は従来のまま。機種ごとの Wrapper・DTO・XAML・ViewModel の追加は不要）。
- 既存のデバイス（`DS4Device` 系）は**最初は変えない**。記述子のプロトコルの工場が、既存のデバイスを返す（ストラングラー）。新しい `ControllerSession` ＋ `IDeviceProtocol` の経路は、新しい機種とテスト用の偽の機種から使う。既存の機種の移行は、実機のある DS4 から、必要なときに行う。

### 3.2 手順（案）

| Step | 内容 | 効果・検証 |
|---|---|---|
| **17-0** モデル図の整理（§3.3） | 4 文書を、現状の構造・拡張の手順・ライフサイクルの表の 3 文書に整理。目標像と実在を分け、`ModelDiagramGuardTests`（図の「実在」の型が存在するか）を追加 | 文書と検査。読みやすさ、更新の確実さ |
| **17-1** 記述子の導入（P17-7・P17-4） | `InputDeviceDescriptor` を作り、いまのカートリッジ・接続フック・系統の表・種類の表を 1 つにまとめる。DI は `AddInputDeviceDescriptor<T>()` の 1 行。解析器の選択（P17-4）もここで渡す | 同じ事実の 3 か所が 1 か所に。既存のカートリッジ・モジュールのテストを記述子のテストへ移す |
| **17-2** 設定の定義から保存・画面を作る（P17-5） | `DeviceSettingDescriptor` で、有効・無効と機種固有の設定（HomeLED、CopyCat、DualSense の LED／ミュートなど）を宣言。保存は定義のループ（XML の形は不変）、設定画面とはじめの起動画面は定義から項目を作る。`*OptionsWrapper`、`*SupportSettings` の DTO を定義へ置き換える | 機種追加時の保存・画面のファイルが 0 に。保存の黄金テストが不変であること、画面の項目の一覧のテスト |
| **17-3** `ControllerSession` と `IDeviceProtocol`（新しい経路） | 読み取りループ・出力スレッド・切断・生存確認を `ControllerSession` に、機種固有を `IDeviceProtocol` に。**既存のデバイスは移さず**、テスト用の偽の機種で、記述子 ＋ プロトコル ＋ 登録の **3 項目だけで新機種が動く**ことを自動テストで示す | 「機種の追加の最短経路」をテストで保証。偽の通信路でループと切断を単体テスト |
| **17-4** 追加の手順書と雛形 | 機種・出力の種類・アクションの追加の手順を、実在のコードとテストに対応づけて書く | 文書 |
| **17-5** 全機種の移行（D2＝全機種） | DS4 → DS3 → DualSense → Switch Pro → Joy-Con の順に、`DS4Device` 系を `ControllerSession` ＋ 各 `*Protocol` へ移し、最後に `DS4Device` の継承を廃止する。機種ごとに別コミット | 実機は DS4 のみ。他の機種は「旧実装と新実装に同じ台本の入力を与え、出力レポート・状態・切断の順序が一致する」差分テスト（`FakeHidTransport` 利用）で守る。A/B 計測 |
| **17-6** 画面・Mapping の整理（P17-1〜3。D1＝含める） | `MainWindow` の責務分割、`EditorWindowFactory`、`Mapping` の段 2（引数 15 以下）。ホットパスの Mapping は A/B 計測と割り当て 0 のテスト | 引数の基準 `ConstructorArityBaseline` が空になる |
| **17-7** ライトバーの設定画面の改良（P17-8） | 17-2 の画面生成と合わせて、項目の並び・名前・ヘルプ文を見直す | 画面。実機確認 |
| 完了作業 | モデル図・規約・台帳の更新、Release ビルド、DS4 の実機確認（1 回） | |

### 3.3 モデル図の整理案（Step 17-0）

| 現状 | 提案 |
|---|---|
| 4 文書、2,279 行。目標像と実在が混在 | **3 文書**: ① `01-Structure.md`（層と依存の規則、実在する主要な部品、拡張点。実在のノードだけで、1 画面ずつ読める量）、② `02-Extension-Guide.md`（機種・出力の種類・アクションの追加の手順。ファイルとテストの対応つき）、③ `03-Lifecycle-and-DI.md`（Singleton／Transient の表。現在の `04` を引き継ぐ） |
| クラス図 `03`（1,361 行）に実在しない型が多い | 手書きは「主要な 15 型」までに絞る。目標像は `Ideas.md` に分離し、実装済みになった時点で `01` へ移す |
| 履歴の `%%` コメントが積み上がっている | 履歴は各 Phase の完了報告書に集約し、図の本文には書かない |
| 図と実装の一致の検査がない | `ModelDiagramGuardTests`: 図の「実在」の表の型名がすべて存在すること、図の規則表と `LayerDependencyRuleTests` の規則 ID が一致すること |
| 層の呼び方が分かりにくい | 規則は変えず、説明を「核（処理と設定）／アダプター（入力機器・出力先・OS）／殻（画面と組み立て）」で書き添える（D5）。フォルダ名・名前空間は変えない（移動の手間を避ける） |

## 4. 方針と制約

- 既存機能の削減なし（規約 2.2）。ログの文言とレベルは変えない（規約 2.3）。XML の保存形式は変えない。
- DS4 以外の機種の移行（17-5）は実機がないため、旧実装との差分テスト（同じ台本の入力に対する出力レポート・状態・切断の順序の一致）を移行の前に先に書き、それを満たしたうえで移す。実機確認は DS4 のみ。
- 新しい経路（17-3）は偽の機種で単体テストする。
- ホットパス（毎フレーム）の割り当て 0 を保つ。`ControllerSession` の導入では A/B 計測と割り当て 0 のテストを行う。
- 規約 3.3 の 5 原則、`LayerDependencyRuleTests` はそのまま守る。記述子は `Input/Abstractions`、設定の定義は `Core/Settings`、画面の生成は `UI` に置く。

## 5. リスク

| リスク | 影響 | 対策 |
|---|---|---|
| 設定の定義からの保存で XML の形が変わる | 利用者の設定が読めない | 保存の黄金テスト（不変）、旧 XML の読み込みテスト、要素名・節名は定義に明記 |
| 設定画面の自動生成で見た目や並びが変わる | 利用者に見える | 項目の順序・名前を定義に固定、実機確認（設定画面） |
| `ControllerSession` が遅延・スレッドの不具合を生む | 入力の遅延、切断の不具合 | 新経路は偽の機種でループ・切断・生存確認を単体テスト。機種ごとに別コミットで、戻せるようにする。DS4 は A/B 計測と実機確認 |
| DS3・DualSense・Switch Pro・Joy-Con は実機がなく、移行後の不具合を見つけにくい（D2＝全機種） | 利用者の環境で不具合 | 差分テストを先に書く。移行は機種ごとに独立のコミット。完了報告書に「実機未確認」を明記し、Phase 完了後も実機の確認を台帳に残す |
| 範囲が大きく終わらない | 計画の遅れ | 17-0〜17-2 を最小の完了とし、17-3 以降は Step ごとに止めても整合が取れるようにする |

## 6. 決定事項（2026-11-10 回答済み）

| ID | 内容 | 決定 |
|---|---|---|
| D1 | 画面・Mapping の整理（17-6） | **Phase17 に含める** |
| D2 | 既存デバイスの移行 | **全機種を移す**（17-5。差分テストで守る） |
| D3 | 設定の保存と画面 | **設定の定義から自動生成** |
| D4 | モデル図 | **3 文書に整理し、図の検査テストを足す** |
| D5 | 層の呼び方 | **「核／アダプター／殻」を書き添える**（フォルダは変えない） |

## 6a. 進捗

- 2026-11-10 **17-0 完了**: 旧 4 文書を `Model-Diagram/Archive/` へ移し、`01-Structure.md`・`02-Extension-Guide.md`・`03-Lifecycle-and-DI.md`・`Ideas.md` を作成。`ModelDiagramGuardTests`（図の型の実在）を追加。規約の参照先を更新。
- 2026-11-10 **17-1 完了**（計画からの変更あり）: 層の依存（L3: 第1層は第2層を参照しない）のため、第1層のカートリッジと第2層の接続フックを 1 つの型にはまとめられない。そこで「記述子」は新しい型を作らず、**カートリッジに `Family` を持たせ**、種類と系統の対応は `InputDeviceType` の値に付ける `[InputDeviceFamily]` 属性を唯一の持ち主にした（`InputDeviceTypeInfo` の表と `InputDeviceFamily` 対応表の二重管理を廃止）。登録は `AddInputDevice<カートリッジ, モジュール>()` の 1 行。解析器の選択（P17-4）は、デバイスが解析器を静的に持つため、17-3 の `IDeviceProtocol` へ吸収する。テスト 3,401 件合格。

- 2026-11-10 **17-2a・17-2b 完了**: 機種ごとのコントローラー設定（ControllerOptionsStore）と、系統ごとのアプリ設定（DeviceOptions 節）を、`ControllerSetting` の定義の並びで読み書きする形にした（DTO 10 ファイルを廃止。XML の形は従来のまま、保存の黄金テスト不変）。変更点（実害の小さい差）: 系統ごとのアプリ設定で、`JoinedGyroProvider` の要素がない場合の既定値が JoyConL から JoyConR（設定クラスの既定）になった。**17-2c（画面の自動生成）は未着手**: 設定画面・はじめの起動画面のチェックボックスの文言は `lex:Loc` の静的キーで、機種ごとの設定タブも XAML に固定されている。自動生成は見た目が変わりうるが、実機確認は DS4 のみのため、方針を確認してから行う。
- 2026-11-10 **17-3 完了**: `IDeviceProtocol`（解析・電池・出力レポートの組み立て）、共通の `ProtocolDevice`（読み取りループ・Report・出力・切断）、`ProtocolDeviceCartridge`（土台）を追加。偽の機種（プロトコル＋カートリッジだけ）で、Report・電池・出力の書き込み・切断が動くことをテストで確認。`VidPidInfo` のコンストラクターを public にした。**17-5（既存機種の移行）は未着手**: `DS4Device` 系の読み取りループは、ジャイロ・タッチパッド・遅延計測・アイドル切断・複数接続方式を含み（各 300〜1,500 行）、`ProtocolDevice` はそれらを持たない。移行にはそれらの共通化が先に必要で、実機のない機種は差分テストが前提。

## 7. 完了条件（案）

- 全機種が `ControllerSession` 上で動き、`DS4Device` の継承が廃止されている。`ConstructorArityBaseline` が空。
- テスト用の偽の機種を、記述子 ＋ プロトコル ＋ 登録の **3 項目で追加できる**ことを自動テストで示す（17-3）。
- 既存の機種相当を 1 つ追加するときに触るファイル数（現状 28）が 10 以下になる（記述子、プロトコル、機種固有の解析、リソース文字列、テスト）。
- 保存の黄金テストが不変。層の違反は 0 件。警告 0・エラー 0。
- モデル図は 3 文書になり、実在しないノードがない。`ModelDiagramGuardTests` が合格する。
- DS4 の実機確認（設定の保存・再読み込み、ライトバー、出力の種類、切断）が OK。
