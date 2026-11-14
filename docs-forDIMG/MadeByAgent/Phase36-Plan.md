# Phase36 計画書: 配置・名前・文書の整理と、判断が要る小さな項目（コードレビュー 2026-11 の低の項目）

作成日: 2026-11-11
状態: **一部完了（2026-11-15）。36-A1・A4・A7・B1・B2・B3 を完了。36-A2（Phase38 の 38-0b で完了）、36-A3・A5・A6、C 部は未着手**
出典: `docs-forDIMG/Code-Review-2026-11/00-Summary.md` §4 の順 14〜23

## 目的
コードレビューで見つけた、重大度が低い項目をまとめて片づける。3 つに分ける。
- **A 部（機械的）**: 改名・移動・文書の更新。動作は変わらない。
- **B 部（小さな是正）**: 静的な差し替え口など、局所の修正。
- **C 部（判断から）**: 直すかどうかを、調査のあとに決める項目。決めた結果は、この計画書に記録する。

## A 部: 機械的な整理

### 現状（調査）
- ファイル名と型名の不一致（2-3）: `Input/DS4Devices.cs`（中身は `InputDeviceRegistry`）、`Processing/Services/Ds4DeviceRegistryAdapter.cs`（`InputDeviceRegistryAdapter`）、`Processing/Abstractions/IDs4DeviceRegistry.cs`（`IInputDeviceRegistry`）。`Core/Updater/UpdaterInstallTrace.cs`（コメントだけ）と `UI/Themes/_.cs`（名前空間の宣言だけ）は、型を持たないファイルで、不一致ではない。
- UDP の型が `Input/` にある（2-4）: `DualShockPadMeta.cs`、`PadDataRspPacket.cs`、`DsState.cs`、`DsBattery.cs`、`DsConnection.cs`、`DsModel.cs`。
- `Processing/Services` に約 70 ファイル、`Input/` の直下に 31 ファイル（2-5）。
- 文書が古い（5-4、3-1）: `02-Extension-Guide.md:3`・`:9`、`01-Structure.md:57`〜`62`・`:134`、`02-Extension-Guide.md:48`、`03-Lifecycle-and-DI.md:38`。
- UI に、使っていない `using DS4Windows.Input;` が約 10 ファイル。

### Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 36-A1 | ファイル名を型名に合わせる（`git mv` のみ。R7）。対象は 3 件: `Input/DS4Devices.cs` → `InputDeviceRegistry.cs`、`Processing/Services/Ds4DeviceRegistryAdapter.cs` → `InputDeviceRegistryAdapter.cs`、`Processing/Abstractions/IDs4DeviceRegistry.cs` → `IInputDeviceRegistry.cs`（追加調査で、3 件目の中身を確認した） | ファイル名＝型名の検査を、型の検査として足す（全 `.cs`。例外は `GlobalSuppressions.cs`、`AssemblyInfo.cs`、`*.xaml.cs`、`partial` の分割ファイル） | 未着手 |
| 36-A2 | （2026-11-13 統合）UDP の型 6 つの移動は、`Phase38-Plan.md` の 38-0b へ移した。Phase38 を行わない（その D1 が C）と決めたときだけ、ここで `Core/Model` へ移す（`git mv` と、名前空間の書き換えを別コミットに） | `Phase14NamespaceFolderGuardTests`、`LayerDependencyRuleTests` | 未着手 |
| 36-A3 | （Phase31〜35 のあとに行う。これらが `Processing/Services` へ型を足す・移すため）`Processing/Services` を機能ごとのフォルダーへ分ける（案: `Connection`［接続・切断・開始と停止］、`Conditioning`［補正の段］、`Devices`［機種のモジュール］、`Slots`［UI 向けの窓口］。残りは `Services`）。移動と名前空間の書き換えを別コミットに | 同上。`ModelDiagramGuardTests` | 未着手 |
| 36-A4 | UI の、使っていない `using DS4Windows.Input…;` を消す | ビルド | 未着手 |
| 36-A5 | 文書の更新: モデル図 3 文書（フォルダーの表、引数の基準 12、手順書の冒頭）と、規則 3.4 R1（36-A2・A3 の結果）。あわせて、規約・モデル図・`Code-Review-2026-11`・Phase29〜36 の計画書に残る、テストの旧名（`Phase14NamespaceFolderGuardTests` → `NamespaceFolderGuardTests`、`Phase13FuncRatchetGuardTests` → `FuncRatchetGuardTests` など。Phase15-0 で改名済み。対応表は `Phase15-0-Test-Rename-Map.md`）を新名へ直す（2026-11-12 追加） | `ModelDiagramGuardTests` | 未着手 |
| 36-A6 | （追加）`UI/IntToBoolConverter.cs` を `UI/Converters` へ移す（`git mv` と、名前空間・XAML の参照の書き換えを別コミットに）。コメントだけのファイル `Core/Updater/UpdaterInstallTrace.cs`（型なし、7 行）は、規則 2.4 の手順（`git log` で経緯を確かめる）のあと、ユーザーの承認を得て削除する。`UI/Themes/_.cs`（名前空間の宣言だけ。IDE のための置き石）は残す | `Phase14NamespaceFolderGuardTests`。画面の起動（XAML の参照） | 未着手 |

| 36-A7 | （2026-11-12 追加。外部レビューの照合から）自動生成コード（`Translations/Strings.Designer.cs` 4,239 行、`Properties/Resources.Designer.cs` 1,677 行）は、対応しない。ルートの `.editorconfig` で、静的解析の対象から外れているか（`generated_code = true` など）だけを確かめ、外れていなければ設定を足す | ビルドの警告が増えないこと | 未着手 |

### 決めること
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D-A1 | 36-A3 のフォルダーの分け方 | (A) 上の案／(B) 行わない（平置きのまま。規則 R1 の「`Services`」を保つ）／(C) 別の分け方 |

## 決定の記録（2026-11-13。ユーザーの回答）
D-A1＝A（案どおり分ける）、36-C1＝A（調査してから `Core/Abstractions` に最小の契約を置く）、36-C2＝A（直さない。確定）、36-C4＝A（先に実機で体感できるか確認）。36-C5〜C7 は、調査の結果を見て決める項目で、まだ決めない。
- 36-A3 は、Phase31〜35 のあとに行う（先に記したとおり）。

## B 部: 小さな是正

| Step | 内容（出典） | テスト | 状態 |
|---|---|---|---|
| 36-B1 | `RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride`（静的な差し替え口。1-5。`Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:21`）を、コンストラクターで受ける工場に替える。使っているテストを、その工場を渡す形に直す | `RemovedApiRuleTests` の表へ足す | 未着手 |
| 36-B2 | Xbox の `HideUsbParent`（5-3。`UI/ViewModels/SettingsViewModel.cs:55`）: 設定の定義（`WithUi`）で表せるかを確かめ、表せるなら寄せる。表せないなら、理由をコメントに書く | 設定画面の生成のテスト | 未着手 |
| 36-B3 | 規則への追記（4-5）: 「同じ層の中だけで使う、状態を持たない計算の部品は、インターフェースを作らず、具象のまま注入してよい。層をまたぐポートと、テストで偽物に差し替えるものは、インターフェースにする」（`.github/copilot-instructions.md` §3.1）。既存のインターフェースは撤去しない | — | 未着手 |

## C 部: 判断から始める項目

各項目は、まず調査して、直す／直さない（理由つき）を決め、この表に記録する。直す場合は、追補の Step を足す。

| ID | 項目（出典） | 調べること | 判断 |
|---|---|---|---|
| 36-C1 | 第 3 層が第 1 層の `InputDeviceCore` を直接使う（2-2） | 第 3 層が `InputDeviceCore` の何を使っているか（メンバーの一覧）。最小の契約にすると何メンバーか | 未決（(A) `Core/Abstractions` に最小の契約を置く／(B) 図と規則を実態に合わせる） |
| 36-C2 | 機器の誤りが続く間の、毎フレームのログ（6-3。`Processing/Services/LatencyMonitor.cs:41`） | 追加調査で確認済み: `frameError` を立てるのは、タッチパッドの取り込みの `catch`（`DualShock4Device.cs:739`、`DualSenseDevice.cs:673`）だけ。続くのは、毎フレーム例外が出る異常なときに限る | **直さない案を推奨**（通常は起きず、異常のときは気づける方がよい。ログの文言・出力を変えない）。ユーザーの確認で確定する |
| 36-C3 | 20 ms のタイマーが、スワイプ切替が無効でも回り続ける（6-4 の残り。`UI/MainWindow.xaml.cs:243`、`593`） | — | **Phase29 の 29-10b へ統合した**（2026-11-13）。同じ 2 つのタイマーを触る 29-10 と、同じメソッドを 2 回触らないため。列挙の競合と、例外で止まる問題（6-12）も 29-10 にある |
| 36-C4 | Bluetooth の Xbox の、接続直後の最大 3 秒の待ち（6-5。`Input/Xbox/XInputTransport.cs:188`） | 待ちを、読み取りスレッドの開始前（`PrimeBatteryForOtherDevice`）へ移せるか。実機で、接続直後の入力の止まりが体感できるか | 未決 |
| 36-C5 | 30 秒ごとの第 0 世代の GC の強制（6-6。`App.xaml.cs:901`） | 経緯（`git log -S "GarbageTask"`）。Phase29 のあとの、1 秒あたりの割り当ての量（計測）。追加調査で、毎フレームの割り当ての主な元（6-1、6-2、6-10、6-11）が分かった。これらを直したあとなら、不要になる見込み | 未決。Phase29（29-9 を含む）のあとに、計測して判断する |
| 36-C6 | ロガーの静的イベントと、静的なキーボードの設定（1-6。`Core/Logging/AppLogger.cs:27`・`29`、`Core/Platform/KeyboardSettings.cs:11`） | 購読者・利用者の一覧。`Phase7-Residual-Ledger.md` §17.3 の目録での扱い | 未決 |
| 36-C7 | 使われていない契約の候補（4-6） | 規則 2.4 の手順（経緯の調査 → 使うべきかの判断）を、1 つずつ行う | 未決 |
| 36-C8 | コンストラクターの null 検査の手書き（3-2）、読み込みと保存の節が対でない（5-5） | — | 3-2 は**計画に入れない**（害がない。記録だけ残す）。5-5 は、2026-11-12 に `Phase37-Plan.md` の 37-4 へ移した |
| 36-C9 | （2026-11-12 追加。外部レビューの照合から）`Mapping.cs`（869 行）と `TriggerDispatchEngine.cs`（747 行）が大きい、という指摘 | 照合済み: `Mapping.cs` の最長のメソッドは `MapCustom` 124 行で、種別ごとの巨大な分岐は Phase8・9 で部品へ出してある。`TriggerDispatchEngine` は判定と実行のエンジンで、状態保持・シリアライズではなく、作りにも問題は見つからなかった | **対応しない**（`Code-Review-2026-11/07-External-Review-2026-11-12.md` §2）。`Mapping` の引数（15）は `03-Constructor-Arity.md` のとおり |

## 確認の方法
- A 部: 各コミットのあと、ビルドと、`Phase14NamespaceFolderGuardTests`・`LayerDependencyRuleTests`・`ModelDiagramGuardTests`。移動のコミットは `git diff -M100%` で、内容の変更がないことを確かめる。Step の終わりに Debug ALL。
- B 部・C 部: 直した項目ごとに、単体テストと、必要なら実機確認（`Known-Issues-Ledger.md` §3 へ足す）。

## リスク
- A 部: 低。機械的。ただし、ファイルの移動は、ほかの Phase の作業中のブランチと衝突するため、ほかの Phase の合間に行う。
- B 部: 低。
- C 部: 項目ごと。36-C2（ログ）と 36-C4（接続時の表示）は、利用者に見える動きが変わり得る。

## 履歴
- 2026-11-13: 決めること（D 項目）のユーザーの回答を「決定の記録」に記録した。
- 2026-11-13: Phase 番号の付け替え（旧 Phase34 → Phase36。進める順に合わせた）。本文中の Step 番号も付け替えた。
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-11: 追加調査の結果を反映。36-A1 の対象を 3 件に確定し、36-A6（`IntToBoolConverter` の移動、コメントだけのファイルの扱い）を足した。36-C2 は調査済み（直さない案を推奨）、36-C3 は範囲を縮めた（競合と停止は Phase29 の 29-10 へ）、36-C5 に割り当ての元を書き足した。
- 2026-11-12: 外部レビューの照合の結果を統合。36-A7（自動生成コードの除外の確認）、36-C9（`Mapping`・`TriggerDispatchEngine` は対応しない）を足し、5-5 を Phase37 へ移した。UDP の型の移動（36-A2）は、`Phase38-Plan.md` の D3 と調整する（どちらか一方で行う）。
- 2026-11-13: Phase 番号の付け替えに伴い、36-A2 を Phase38 の 38-0b へ統合した（移動と、分けた部品の置き場所を、1 か所で決めるため）。36-C3 は Phase29 の 29-10b へ統合した。36-A3 は、Phase31〜35 が型を足す・移すあとに行う（下の A3 の注）。

## 実施の記録
### 2026-11-15
- 36-A1: 3 ファイルを `git mv` で改名（内容の変更なし。`git diff -M100%` で確認）。あわせて、ファイル名＝型名の検査を `FileNameMatchesTypeGuardTests` として足した（名前空間の直下に型が 1 つだけのファイルが対象。`partial`・`*.xaml.cs`・`*.Designer.cs` などは除く）。
- 36-A4: UI の使っていない `using DS4Windows.Input…;` 13 ファイルから削除（ビルドで、必要なものがないことを確認）。
- 36-A7: `*.Designer.cs` は `<auto-generated>` の見出しで解析の対象外だが、`.editorconfig` に `[*.Designer.cs] generated_code = true` を明示した。
- 36-B1: `RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride`（静的）を、コンストラクターで受ける工場（`DefaultKeyButtonActionControllerFactory` → `KeyButtonActionController` → `ToggleImpl`／`PressImpl` → アダプターへ、省略できる引数で渡す）に替えた。テストは、その工場を渡す形に直した。`RemovedApiRuleTests` の表へ足した。
- 36-B2: 調査の結果、`HideUsbParent` は機種の設定の定義に入っているが、画面は設定タブの 1 項目で、HidHide 未導入の案内と連動するため、現状の形を保つ（理由を `SettingsViewModel` のコメントに書いた）。
- 36-B3: 規約 3.1 に「インターフェースを作る基準」を追記した。
- 未着手: 36-A3（`Processing/Services` のフォルダー分け。決定 D-A1＝A。型の出入りが落ち着くまで見送り）、36-A5（文書の旧名の更新）、36-A6、C 部。
- 注: 全件のテストで、WPF のパネルを読み込む 3 件が 1 度だけ時間切れで失敗し、再実行と単独の実行では合格した（共有の WPF 状態を使うテスト同士の順序依存と見られる。既存の不安定さ）。

### 2026-11-15（A3・A6）
- A3（完了）: `Processing/Services` の 67 ファイルを、`Connection`（13）・`Conditioning`（19）・`Devices`（15）・`Slots`（19）の 4 フォルダーへ移した（`git mv` のコミットと、名前空間・`using`・テスト内のソースの場所の文字列の書き換えのコミットを分けた。R7）。基準値（`LayerDependencyBaseline` など、型の完全名をキーに持つ表）も、新しい名前空間へ更新した。全テスト合格、警告 0・エラー 0。
- A6（完了）: `UI/IntToBoolConverter.cs` を `UI/Converters` へ移した。コメントだけの `Core/Updater/UpdaterInstallTrace.cs` の削除は、承認が必要なため据え置き。
- A5（見送り）: 設計書の中の旧いテスト名の置き換え。履歴の記述（当時の名前）としても読めるため、対応表（`Phase15-0-Test-Rename-Map.md`）を残すだけにした。

### 2026-11-15（C1・C6・C7 の調査。実装はしない）
- **C1（第 3 層が `InputDeviceCore` を直接使う）**: `Output` が `InputDeviceCore` から使うメンバーは約 20 種（`MotionEvent`、`Report`、`queueEvent`、`setRumble`、`SetLightbarState`、`LightBarColor`、`GetLightbarStateRef`、`DisplayName`、`MacAddress`、`GetDeviceType`、`ResetState`、`RemoveFeedbacks`、`HidDevice`、`Disconnect`、`CurrentExclusiveStatus`、`TriggerModeProvider`、`isValidSerial`、`isSynced`、`isCharging`、`getBattery`、`getConnectionType` など）。使う場所は `UdpMotionService`、`VirtualPadService`、`DeviceRumbleService`、`HidHideService`、各カートリッジの `AttachFeedback`。最小の契約にすると約 20 メンバーで、用途（モーション、振動、ライトバー、識別、切断）が混ざるため、1 つの契約にすると広すぎる。分けるなら用途別に 4〜5 個（`IMotionSource`、`IRumbleTarget`、`ILightbarTarget` など）。**推奨は (B)**（図と規則を実態に合わせ、第 3 層→第 1 層の「使う側」を明記する）。(A) を採るなら、独立した Phase が必要（影響範囲が `Output` 全体）。
- **C6（ロガーの静的イベントと静的な設定）**: `AppLogger.GuiLog` の購読者は `LoggerHolder`、`MainWindow`、`LogViewModel` の 3 か所、`ProfileChanged` の購読者は `MainWindow` の 1 か所。`KeyboardSettings` の利用者は `App.xaml.cs` の起動時の読み込み 1 か所のみ（ほか、`KeyButtonActionController` は同名の別の語で無関係）。`Phase7-Residual-Ledger.md` §17.3 の目録では、「ロガーは最初に必要」「OS の設定の読み取り」として、起動前に必要なものとして残すと記録済み。**推奨は、直さない**（購読者が少なく、起動前に必要で、静的な窓口を新設するものでもない）。
- **C7（使われていない契約の候補 5 件。規則 2.4）**: 4 件は「使われている」と判断した。`IGatedOutputAction`（`ProfileSwitchAction` が実装し、`TriggerDispatchEngine` が `is` で判定）、`IResettableOutputAction`（`ControlExtrasOutputAction` が実装し、エンジンが `is` で判定。ガードテストあり）、`IPendingUntriggerQuery`（`EngineContext` が実装し、`ProfileSwitchAction` が `as` で読む）、`IToggleFrameOutput`（`FrameOutput` が実装し、`ControlToggleOutputAction` が `as` で使う。ガードテストあり）。いずれも、Phase8-Step7 で、「能力の印」として意図して作られたもの。削除しない。残る 1 件 `ITouchpadBehaviour` は、2015 年の初版からある旧い抽象で、実装は `TouchpadMouse` の 1 つだけ、参照は実装の宣言と、コメントアウトされた 1 行のみ。**「使うべきでないもの」の候補**だが、削除は承認が要るため、ユーザーへの提案とする。
