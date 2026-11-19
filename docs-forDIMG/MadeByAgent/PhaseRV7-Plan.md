# PhaseRV7 計画書: 採用手段の統一（変更通知、命名、タイマーの書き方、テストの枠）

作成日: 2026-11-19
状態: **未承認（コードは未着手）**
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-26〜RV-28（§3.7）
前提: PhaseRV1〜RV6 の完了（触るファイルが広いため、機能の Phase が済んだあとに 1 回だけ行う）。PhaseRV3 の D1（コマンドの基盤）の結果。**RV3 の D1 は (B) CommunityToolkit.Mvvm に決定済み（2026-11-19）。そのため、本 Phase の D1（変更通知の基底）は、ツールキットの `ObservableObject` への移行（(C)）を前提に、確認の場で再提案する。**
動き: **機能・保存の形・ログは変えない**。名前の変更と、基底の付け替えが中心。

## 目的
同じことを 2 通り以上の書き方で書いている箇所を、決めた 1 つに揃える。対象は、(1) 変更通知（独自 `ObservableObject` と、`INotifyPropertyChanged` の直接実装）、(2) Java 風の小文字始まりのメソッド名（`getBattery()`、`isCharging()`、`setIdleTimeout()` など）、(3) タイマーの書き方の規則、(4) テストの枠（xUnit と MSTest）、(5) `?? throw new ArgumentNullException` の書き方。

## 背景（実測。数字は `01-Metrics.md`）
- **変更通知**: 独自の `UI/ViewModels/ObservableObject`（31 行。`SetProperty`・`OnPropertyChanged`）を継承する型が 12。`INotifyPropertyChanged` を直接実装するファイルは 13（`ObservableObject` 自身を除く）: `Core/Profiles/ProfileEntity.cs`、`Core/Settings/ProfileSubSettingBase.cs`、`UI/ProfileEditor.xaml.cs`、`UI/ViewModels/` の `AboutViewModel`・`CompositeDeviceModel`・`DeviceFamilyPanelModel`・`MacroPlaybackOptions`・`SpecialActionItem`・`SpecialActions/FocusWindowViewModel`・`MacroViewModel`・`PressKeyViewModel`・`SpecialActionsListViewModel`・`TriggerOptionViewModel`。
  - 制約: `Core` の 2 型（`ProfileEntity`、`ProfileSubSettingBase`）は、`UI` の `ObservableObject` を使えない（層の依存の規則。`Core` から `UI` へは参照できない）。`ProfileEditor` は `UserControl` の派生で、別の基底を持てない。したがって、統一には `ObservableObject` を `Core` へ移す必要がある（`git mv` と名前空間の書き換えを別コミットに。規則 3.4 R6・R7）。
- **命名**: 小文字始まりの公開・保護・内部メソッドが 92 件。`InputDeviceCore.cs` 32（`getWarnInterval()`、`isHidExclusive()`、`isExclusive()`、`isDisconnectingStatus()`、`getMacAddress()`、`getConnectionType()`、`getIdleTimeout()`、`setIdleTimeout(int)`、`getBattery()`、`isCharging()`、`getLastTimeElapsed()` など。元の DS4Windows の書き方）、`ControllerStateExposed.cs` 9（`getGyroYaw()` など）、`TouchpadMouse.cs` 9、`Mapping.cs` 5、`DS4SixAxis.cs` 4。
- **タイマー**: 生成は 13 か所で 4 種類。`System.Timers.Timer` 7（UI の 5 ファイル。`NonFormTimer` は、`using NonFormTimer = System.Timers.Timer;` の別名で、別種ではない）、`System.Threading.Timer` 4（`App`、`BatteryStatusLogger`、`CompositeDeviceModel`）、`DispatcherTimer` 1（`ProfileEditor`）、`TimeProvider.CreateTimer` 1（`RepeatHelper`）。例外で止まらない UI のタイマーの本体は `GuardedTimerTick`（Phase29）。数が少なく、効果は小さい。
- **テストの枠**: `DS4WindowsTests`（xUnit。`[Fact]` 2,495、`[Theory]` 246、613 ファイル）と、`StandaloneTests`（MSTest。8 ファイル。`.sln` の外だが、`.github/workflows/debug-all.yml` が別に build と test を実行する）。
- **`?? throw new ArgumentNullException(nameof(x))`**: 666 件。害はない（前回の 3-2 と同じ判断。`ArgumentNullException.ThrowIfNull` へ寄せるのは見た目の統一のみ）。

## 方針・制約
- 名前の変更は、IDE の「名前の変更」（Roslyn）で行い、**型 1 つにつき 1 コミット**。呼び出し元ごと変わるので、リフレクション・XAML のバインディング・文字列で名前を使う箇所（IPC のコマンド名、ログの文言、テストの文字列）は、先に検索して確かめる。
- 名前の変更で、公開の契約（`IAppSettingsService` など）・保存の形・ログの文言は変えない。
- 基底の付け替えで、`PropertyChanged` の通知の名前・タイミングを変えない。
- 新しく足す規則は、`copilot-instructions.md` の変更として、**承認を得てから**足す。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV7-0 | 調査と規則案。(1) 変更通知: 直接実装 13 型を、「`ObservableObject` へ載せ替えられる／別の基底を持つので載せ替えられない」に分け、`PhaseRV7-Survey.md` に表にする。(2) 命名: 小文字始まり 92 件を、外部から名前で触られるもの（リフレクション・XAML・IPC・文字列）と、そうでないものに分ける。`getXxx()`・`isXxx()`・`setXxx()` を、プロパティへ変える案の影響を数える。(3) タイマー 13 か所の、使い分けの現状（UI を触るか、周期、停止の保証）を整理し、規則案を作る。(4) `StandaloneTests` の 8 ファイルの内容を調べ、`DS4WindowsTests` へ移せるか（`ProjectReference`、別の理由）を確かめる。(5) 規約へ追記する文面の案。結果は同じ文書へ。ユーザーの承認を得る | — | 未着手 |
| RV7-1 | `ObservableObject` を `Core` へ移す（D1）。`git mv`（内容は変えない）→ 名前空間と `using` の書き換え（別コミット）。`Core` の `ProfileEntity`・`ProfileSubSettingBase` を載せ替える。`RemovedApiRuleTests` の表へ、旧名前空間の型を足す | `LayerDependencyRuleTests`、`NamespaceFolderGuardTests`、変更通知を検査する既存のテスト | 未着手 |
| RV7-2 | UI の直接実装 11 型（`ProfileEditor` を除く）を `ObservableObject` へ載せ替える。通知の名前・タイミングは同じ（`SetProperty` の等価の比較が、元の比較と同じ意味になること。値の入れ物・コレクションを持つ型は、1 型ずつ確かめる）。`ProfileEditor`（`UserControl`）は、理由をコメントに書いて残す | 各 ViewModel のテスト、`PropertyChanged` の通知を検査するテスト（足す） | 未着手 |
| RV7-3 | 命名（D2）: 小文字始まりの公開メソッドを PascalCase へ。型ごとに 1 コミット（`DS4SixAxis` → `ControllerStateExposed` → `TouchpadMouse` → `Mapping` → `InputDeviceCore` の順を目安に、小さい型から）。`getXxx()`・`isXxx()`・`setXxx()` をプロパティへ変えるかは D2 に従う | ビルド、全テスト（名前の変更は、コンパイルで漏れが分かる）。文字列で名前を使う箇所の検索 | 未着手 |
| RV7-4 | タイマーの規則（D3）: 規則の案を `copilot-instructions.md` §3.1 に 1〜2 行で足す（承認後）。既存の 13 か所は、規則に反するもの（例外で止まる、UI を別スレッドから触る）だけ直す。`NonFormTimer` の別名は、RV1 で未使用のものは消えている。残る 5 ファイルの別名は、`System.Timers.Timer` の直接の記述へ置き換える（別名の廃止） | `GuardedTimerTick` のテスト、既存のテスト | 未着手 |
| RV7-5 | テストの枠（D4）: 決定に従う。(A) のとき、`StandaloneTests` の 8 ファイルを、`DS4WindowsTests` へ移し（xUnit へ書き換え）、`StandaloneTests` を削除し、`debug-all.yml` の該当の行を更新する | 移したテストが緑。CI の設定は、`gh workflow` でなく、ファイルの差分を目で確かめる | 未着手 |
| RV7-6 | 検査: `CodeHealthRatchetTests` に、「小文字始まりの公開メソッド＝0」「`INotifyPropertyChanged` の直接実装＝許可リスト（`ProfileEditor` のみ）」「別名 `using NonFormTimer`＝0」を足す。許可リストは空に近く保つ（足さない） | 新テスト | 未着手 |
| RV7-7 | 文書: 規約（承認後）、`02-Extension-Guide.md`（新しい ViewModel の作り方の 1 行）、`01-Metrics.md` の基準 | `ModelDiagramGuardTests` | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | 変更通知の基底の置き場所 | (A) `ObservableObject` を `Core/Model`（または `Core/Utilities`）へ移し、層をまたいで 1 つにする／(B) `Core` の 2 型は、いまのまま直接実装を残し、UI の型だけ揃える／(C) CommunityToolkit.Mvvm へ移る（RV3 の D1 が (B) のときのみ） | (A) 利点: 1 つに揃う。欠点: 12 型の `using` が変わる（機械的）。(B) 利点: 移動なし。欠点: 2 通りが残る。(C) 利点: 標準の基底。欠点: 12 型の置き換え、パッケージの追加。**推奨は (A)**。RV3 の結果で (C) が選ばれていれば (C) |
| D2 | 命名の範囲 | (A) PascalCase へ改名するだけ（`GetBattery()`、`IsCharging()`）。プロパティ化は、別の機会／(B) `getXxx()`・`isXxx()`・`setXxx()` を、プロパティへ変える／(C) 触らない | (A) 利点: 機械的で、リスクが低い。欠点: Java 風の get/set メソッドの形は残る。(B) 利点: C# らしい。欠点: `InputDeviceCore` の呼び出し元が全層に広く、バインディング・リフレクションに影響し得る。(C) 利点: 変更なし。欠点: 不統一が残る。**推奨は (A)**（(B) は、改名のあとに、効果を見て別に判断） |
| D3 | タイマーの扱い | (A) 新規の書き方の規則だけ決める（UI を触る周期は `DispatcherTimer` か `IUiDispatcher` と `GuardedTimerTick`、バックグラウンドの周期は `System.Threading.Timer` か `TimeProvider.CreateTimer`）。既存の 13 か所は、規則に反するものだけ直す／(B) 13 か所を 1 種類へ揃える／(C) 触らない | (A) 利点: 変更が小さく、新規の不統一を止められる。欠点: 4 種類が残る。(B) 利点: 揃う。欠点: 13 か所の動き（スレッド、周期）に触れる割に効果が小さい。(C) 利点: 変更なし。欠点: 新規が増える。**推奨は (A)** |
| D4 | テストの枠 | (A) `StandaloneTests`（MSTest）を、xUnit へ移して 1 つにする／(B) 残す（別の理由があるときのみ。RV7-0 で確認）／(C) 残し、方針だけ書く | (A) 利点: 枠が 1 つ。CI の手順が短くなる。欠点: 8 ファイルの書き換え（小さい）。(B) 利点: 変更なし。欠点: 2 つの枠が残る。(C) 同上。**推奨は (A)**（RV7-0 で、別プロジェクトである理由が見つかれば (B)） |
| D5 | `?? throw new ArgumentNullException` の 666 件 | (A) 触らない（前回の 3-2 の判断を維持）。新規は `ArgumentNullException.ThrowIfNull` を許す／(B) すべて `ThrowIfNull` へ寄せる | (A) 利点: 変更なし。欠点: 2 通りの書き方が混ざる。(B) 利点: 揃う。欠点: 666 か所の機械的な変更。差分が大きく、レビューしにくい。**推奨は (A)** |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 名前の変更・基底の付け替えのコミットは、`git diff` で、名前以外の差分がないことを確かめる。
- 実機確認: 画面の起動と、プロパティの変更が画面へ反映されること（`ObservableObject` の付け替えで通知が落ちると、表示が更新されない）。`Device-Check-Checklist.md` に足す。

## リスク
- 低〜中。名前の変更は、コンパイルで漏れが分かるが、文字列での参照（`nameof` を使わないバインディング、IPC のコマンド名）は、コンパイルで分からない。事前の検索（RV7-0）と、実機の確認で守る。
- 基底の付け替えで、`SetProperty` の等価の比較が元の `!=` と異なると、通知が増減する。1 型ずつ確かめる。
- 他の Phase のブランチと衝突しやすい（触るファイルが広い）。機能の Phase が済んだあとに行う理由。

## 完了の定義
- 小文字始まりの公開メソッドが 0。`INotifyPropertyChanged` の直接実装が、許可リスト（`ProfileEditor` など、別の基底を持つもの）のみ。別名 `using NonFormTimer` が 0。
- タイマーの書き方の規則が、規約に（承認後）ある。
- D4 が (A) のとき、テストの枠が 1 つで、`debug-all.yml` が更新されている。
- `CodeHealthRatchetTests` に上記の項目がある。Debug ALL が警告 0・エラー 0。

## 関連計画
- `PhaseRV1-Plan.md`（別名の未使用の整理）、`PhaseRV3-Plan.md`（コマンドの基盤 D1）、`PhaseRV2-Plan.md`（`async void`）。
- 過去: `Phase29-Plan.md`（`GuardedTimerTick`）、`Phase36-Plan.md`（C8: null 検査の手書きは計画に入れない）、`Phase14`（ファイルの移動の規則 R6・R7）。
- 規約: §3.3 原則3（名前空間＝フォルダ）、§3.4 R6・R7。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
