# コードレビュー（2026-11）観点 3: コンストラクターの引数が必要最低限か

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）
測り方: 本体の全 `.cs` で、ファイル名と同じ名前のコンストラクターの引数を数えた（スクリプトによる実測。`ConstructorArityBaseline` の 4 件と一致）。

## 1. 良い点

- 12 を超える型は 4 つだけで、すべて `ConstructorArityBaseline` に凍結されている。
- Phase28 で工場（`SpecialActionOutputFactory` 14 → 1、`ShellViewModelFactory` 14 → 12）を、束ねではなく分割で減らした。
- 依存を隠すための束ね型（何でも箱）は見つからなかった。`ProfileSlotSettings`（8 引数）は、領域の窓口（スティック・トリガー・ジャイロなど）の束で、依存を隠す箱ではない。2026-11-11 の追加調査で、名前（`*Context`、`*Dependencies`、`*Services`、`*Bundle` など）による全体の検索も行い、該当なしを確かめた（`04-Duplication-and-Abstraction.md` の 4-8）。

## 2. 引数が多い型の一覧（10 以上）

| 型 | 引数 | 場所 | 見立てと、減らす方向 |
|---|---|---|---|
| `MainWindow` | 15 | `UI/MainWindow.xaml.cs:113` | code-behind の残り（既知 B）。Phase28 で「削れる引数なし」と確認済み。画面の責務の整理に従属 |
| `Mapping` | 15 | `Processing/Mapping.cs:100` | 状態の入れ物 3、段 6、窓口 6。`IProfileActionProvider` は、特殊アクションの処理（`Mapping.cs:349`、`689`〜`707`）だけが使う。そこを `ITriggerDispatchEngine` の側へ寄せれば 1 減る。`IDeviceSlotState` は 4 か所（`265`、`341`、`456`、`495`）で、デバイスの経過時間などを読むために使っており、すぐには外せない |
| `InputReportProcessor` | 13 | `Processing/Services/InputReportProcessor.cs:51` | OSC の前後処理（`IOscService`、`IAppSettingsService` の 2 つの設定）と、DS4 型のトリガーの後処理（`IOutputDeviceTypeState`）を「出力の直前の段」へ出せば 10 前後。後者は `05-Extensibility.md` の 5-1 と同じ箇所 |
| `DeviceConnector` | 13 | `Processing/Services/DeviceConnector.cs:33` | 接続の手順の指揮役。`IProfileXmlStore`（`DeviceConnector.cs:73`）と `IAppSettingsService`（同 `124`）は 1 か所ずつしか使わない。その 2 つの処理を `IDeviceConnectionSetup` へ寄せれば 11 になる |
| `ServiceLifecycle` | 12 | `Processing/Services/ServiceLifecycle.cs:75` | `LightbarOverrides` と `ILightbarService` の両方を受ける。前者は、開始・停止のときに印（`shuttingdown`、`defaultLight`、`forcelight`、`forcedFlash`）を書き換えるために使う（`ServiceLifecycle.cs:56`、`125`、`203`〜`205`）。「開始のとき」「停止のとき」の操作として `ILightbarService` に持たせれば 1 減る |
| `ProfileApplicationService` | 12 | `Processing/Services/ProfileApplicationService.cs:34` | 未調査 |
| `ProfileEditor` | 12 | `UI/ProfileEditor.xaml.cs:172` | 既知 B |
| `ShellViewModelFactory` | 12 | `UI/Services/ShellViewModelFactory.cs:34` | Phase28 で整理済み |
| `AssignmentDefinition` | 12 | `Processing/Actions/AssignmentDefinition.cs:34` | 値の入れ物（依存ではない） |
| `DeviceDisconnectHandler` | 11 | `Processing/Services/DeviceDisconnectHandler.cs:32` | 未調査 |
| `RecordBox`、`AssignmentEditor` | 11 | `UI/RecordBox.xaml.cs:71`、`UI/AssignmentEditor.xaml.cs:94` | 既知 B |
| `BindingPolicy` | 11 | `Processing/Actions/BindingPolicy.cs:14` | 値の入れ物 |
| `ProfileSettingsViewModel`、`ProfileEditorFactory`、`ProfileEditViewModelFactory`、`IpcCommandProcessor`、`AutoProfileService`、`RepeatHelper` | 10 | — | 基準内 |

## 3. 問題点

### 3-1（低）基準の数字が、文書どうしで違う
- 場所: `.github/copilot-instructions.md` §3.1 は「12 以下」。`docs-forDIMG/Model-Diagram/01-Structure.md:134`、`02-Extension-Guide.md:48`、`03-Lifecycle-and-DI.md:38` は「15 以下」。
- 修正の方向: モデル図の 3 文書を 12 へ直す（`05-Extensibility.md` の 5-4 と同じ作業）。

### 3-2（低）引数の null 検査が、どの型にも手書きで並ぶ
- 事実: `?? throw new ArgumentNullException(nameof(x))` が、引数の数だけ並ぶ（例: `Mapping.cs:118`〜`133`）。コンテナは、解決できない依存を `null` で渡さない。
- なぜ問題か: 害はないが、コンストラクターを長くし、引数を足すときの手数になる。
- 修正の方向: 急がない。新しい型では書かない、と決めるだけでよい（決定が要る）。

引数の数だけを目的にした修正は勧めない。上の表の「減らす方向」は、原因（1 つの型が受け持つ仕事）の側の話で、Phase29 以降の計画書では、5-1（出力の種類の分岐）のように、別の理由で手を入れる箇所に重ねている。
