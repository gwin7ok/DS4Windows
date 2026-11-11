# Phase34 計画書: 配置・名前・文書の整理と、判断が要る小さな項目（コードレビュー 2026-11 の低の項目）

作成日: 2026-11-11
状態: **未着手。承認待ち**（A 部は機械的で、ほかの Phase と独立に、いつでも行える）
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
| 34-A1 | ファイル名を型名に合わせる（`git mv` のみ。R7）。対象は 3 件: `Input/DS4Devices.cs` → `InputDeviceRegistry.cs`、`Processing/Services/Ds4DeviceRegistryAdapter.cs` → `InputDeviceRegistryAdapter.cs`、`Processing/Abstractions/IDs4DeviceRegistry.cs` → `IInputDeviceRegistry.cs`（追加調査で、3 件目の中身を確認した） | ファイル名＝型名の検査を、型の検査として足す（全 `.cs`。例外は `GlobalSuppressions.cs`、`AssemblyInfo.cs`、`*.xaml.cs`、`partial` の分割ファイル） | 未着手 |
| 34-A2 | UDP の型 6 つを `Core/Model` へ移す（`git mv` と、名前空間の書き換えを別コミットに） | `Phase14NamespaceFolderGuardTests`、`LayerDependencyRuleTests` | 未着手 |
| 34-A3 | `Processing/Services` を機能ごとのフォルダーへ分ける（案: `Connection`［接続・切断・開始と停止］、`Conditioning`［補正の段］、`Devices`［機種のモジュール］、`Slots`［UI 向けの窓口］。残りは `Services`）。移動と名前空間の書き換えを別コミットに | 同上。`ModelDiagramGuardTests` | 未着手 |
| 34-A4 | UI の、使っていない `using DS4Windows.Input…;` を消す | ビルド | 未着手 |
| 34-A5 | 文書の更新: モデル図 3 文書（フォルダーの表、引数の基準 12、手順書の冒頭）と、規則 3.4 R1（34-A2・A3 の結果） | `ModelDiagramGuardTests` | 未着手 |
| 34-A6 | （追加）`UI/IntToBoolConverter.cs` を `UI/Converters` へ移す（`git mv` と、名前空間・XAML の参照の書き換えを別コミットに）。コメントだけのファイル `Core/Updater/UpdaterInstallTrace.cs`（型なし、7 行）は、規則 2.4 の手順（`git log` で経緯を確かめる）のあと、ユーザーの承認を得て削除する。`UI/Themes/_.cs`（名前空間の宣言だけ。IDE のための置き石）は残す | `Phase14NamespaceFolderGuardTests`。画面の起動（XAML の参照） | 未着手 |

### 決めること
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D-A1 | 34-A3 のフォルダーの分け方 | (A) 上の案／(B) 行わない（平置きのまま。規則 R1 の「`Services`」を保つ）／(C) 別の分け方 |

## B 部: 小さな是正

| Step | 内容（出典） | テスト | 状態 |
|---|---|---|---|
| 34-B1 | `RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride`（静的な差し替え口。1-5。`Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:21`）を、コンストラクターで受ける工場に替える。使っているテストを、その工場を渡す形に直す | `RemovedApiRuleTests` の表へ足す | 未着手 |
| 34-B2 | Xbox の `HideUsbParent`（5-3。`UI/ViewModels/SettingsViewModel.cs:55`）: 設定の定義（`WithUi`）で表せるかを確かめ、表せるなら寄せる。表せないなら、理由をコメントに書く | 設定画面の生成のテスト | 未着手 |
| 34-B3 | 規則への追記（4-5）: 「同じ層の中だけで使う、状態を持たない計算の部品は、インターフェースを作らず、具象のまま注入してよい。層をまたぐポートと、テストで偽物に差し替えるものは、インターフェースにする」（`.github/copilot-instructions.md` §3.1）。既存のインターフェースは撤去しない | — | 未着手 |

## C 部: 判断から始める項目

各項目は、まず調査して、直す／直さない（理由つき）を決め、この表に記録する。直す場合は、追補の Step を足す。

| ID | 項目（出典） | 調べること | 判断 |
|---|---|---|---|
| 34-C1 | 第 3 層が第 1 層の `InputDeviceCore` を直接使う（2-2） | 第 3 層が `InputDeviceCore` の何を使っているか（メンバーの一覧）。最小の契約にすると何メンバーか | 未決（(A) `Core/Abstractions` に最小の契約を置く／(B) 図と規則を実態に合わせる） |
| 34-C2 | 機器の誤りが続く間の、毎フレームのログ（6-3。`Processing/Services/LatencyMonitor.cs:41`） | 追加調査で確認済み: `frameError` を立てるのは、タッチパッドの取り込みの `catch`（`DualShock4Device.cs:739`、`DualSenseDevice.cs:673`）だけ。続くのは、毎フレーム例外が出る異常なときに限る | **直さない案を推奨**（通常は起きず、異常のときは気づける方がよい。ログの文言・出力を変えない）。ユーザーの確認で確定する |
| 34-C3 | 20 ms のタイマーが、スワイプ切替が無効でも回り続ける（6-4 の残り。`UI/MainWindow.xaml.cs:243`、`593`） | タイマーを始める・止める場所（同 `583`〜`589`）。設定を画面で切り替えたときに、始め直せるか | 未決。なお、列挙の競合と、例外で止まる問題（6-12）は、Phase29 の 29-10 へ移した |
| 34-C4 | Bluetooth の Xbox の、接続直後の最大 3 秒の待ち（6-5。`Input/Xbox/XInputTransport.cs:188`） | 待ちを、読み取りスレッドの開始前（`PrimeBatteryForOtherDevice`）へ移せるか。実機で、接続直後の入力の止まりが体感できるか | 未決 |
| 34-C5 | 30 秒ごとの第 0 世代の GC の強制（6-6。`App.xaml.cs:901`） | 経緯（`git log -S "GarbageTask"`）。Phase29 のあとの、1 秒あたりの割り当ての量（計測）。追加調査で、毎フレームの割り当ての主な元（6-1、6-2、6-10、6-11）が分かった。これらを直したあとなら、不要になる見込み | 未決。Phase29（29-9 を含む）のあとに、計測して判断する |
| 34-C6 | ロガーの静的イベントと、静的なキーボードの設定（1-6。`Core/Logging/AppLogger.cs:27`・`29`、`Core/Platform/KeyboardSettings.cs:11`） | 購読者・利用者の一覧。`Phase7-Residual-Ledger.md` §17.3 の目録での扱い | 未決 |
| 34-C7 | 使われていない契約の候補（4-6） | 規則 2.4 の手順（経緯の調査 → 使うべきかの判断）を、1 つずつ行う | 未決 |
| 34-C8 | コンストラクターの null 検査の手書き（3-2）、読み込みと保存の節が対でない（5-5） | — | **計画に入れない**（3-2 は害がなく、5-5 は設定のオブジェクト化の続きで揃える。記録だけ残す） |

## 確認の方法
- A 部: 各コミットのあと、ビルドと、`Phase14NamespaceFolderGuardTests`・`LayerDependencyRuleTests`・`ModelDiagramGuardTests`。移動のコミットは `git diff -M100%` で、内容の変更がないことを確かめる。Step の終わりに Debug ALL。
- B 部・C 部: 直した項目ごとに、単体テストと、必要なら実機確認（`Known-Issues-Ledger.md` §3 へ足す）。

## リスク
- A 部: 低。機械的。ただし、ファイルの移動は、ほかの Phase の作業中のブランチと衝突するため、ほかの Phase の合間に行う。
- B 部: 低。
- C 部: 項目ごと。34-C2（ログ）と 34-C4（接続時の表示）は、利用者に見える動きが変わり得る。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-11: 追加調査の結果を反映。34-A1 の対象を 3 件に確定し、34-A6（`IntToBoolConverter` の移動、コメントだけのファイルの扱い）を足した。34-C2 は調査済み（直さない案を推奨）、34-C3 は範囲を縮めた（競合と停止は Phase29 の 29-10 へ）、34-C5 に割り当ての元を書き足した。
