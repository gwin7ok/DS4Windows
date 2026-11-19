# PhaseRV5 計画書: 設定の宣言化（アプリ・プロファイルの項目を、1 か所の定義へ）

作成日: 2026-11-19
状態: **未承認（コードは未着手）**。D1〜D4 は 2026-11-19 にユーザーが選択済み（「決定事項」を参照）。規模が大きいため、承認の際に、アプリ設定の部分（RV5-5b）を別 Phase へ切り出すかを再確認する
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-17〜RV-20（§3.4、§3.6、§3.9）
前提: PhaseRV2 の完了（空の `catch` の規則。`ProfileSections` の `try { } catch { }` を、この Phase で畳む）。PhaseRV1 の検査の枠。
動き: **保存するファイルの形（要素名・順序・既定値・範囲・値の書式）を変えない**。黄金テストで守る。読み込みの結果は、壊れた値（解析に失敗する要素）のときだけ、D2＝(B) で変わる（構造の変更とは別のコミットで行う）。

## 目的
設定の項目を 1 つ足す手数を減らし、読み込みと保存の食い違いを構造的になくす。具体的には、(1) `AppSettingsService` の「取得・設定・通知」の定型を補助へ畳み、(2) プロファイルの読み込みの節の「要素ごとの `try { } catch { }`」を、要素がないときの扱いを明示した補助へ置き換え、(3) 項目の定義（要素名・既定値・範囲・読み書き）を 1 か所に持つ宣言型へ段階的に移す。入力機種の設定が Phase17 で `ControllerSetting` の宣言型になったのと同じ考え方を、アプリ・プロファイルの設定へ広げる。

## 背景（実測。数字は `01-Metrics.md`）
- `AppSettingsService`（340 行。構造の重複 68%）: 71 の `NotifyChanged` 呼び出しがあり、各プロパティが 10 行前後の同じ形（`get => _config.x; set { if (_config.x != value) { _config.x = value; NotifyChanged(nameof(P)); } }`）。
- `AppSettingsDTO`（1,179 行。構造の重複 44%）: `[XmlElement("…")]` とプロパティの並び。Phase37 の 37-6 で、`partial` による分割は**見送り**（`XmlSerializer` が要素の順序を宣言の順で決めるため）。
- `BackingStore`（1,027 行）: 公開フィールド 107。設定の領域の窓口（`ForSlot`。Phase15-6Z）で画面からは隠したが、実体は袋のまま。
- プロファイルの読み込みの節（`Core/Persistence/ProfileSections`）: 読み込みの節 15、保存の節 7（Phase37-4 で `ProfileAreaSection` により領域ごとに対にした。節の本体の中身は動かしていない）。`ProfileStickAxialLoadSection` は構造の重複 91%（144／158 行）で、空の `catch` が 29、`ProfileTouchpadMouseLoadSection` は 15。
- 現状の読み込みの動き（**黄金テストで守る対象**。`ProfileStickAxialLoadSection` の例）: 要素が**ない**ときは、`SelectSingleNode` が `null` を返し、`Item.InnerText` で例外になって `catch { }` に入り、**何も代入しない**。要素が**あって解析に失敗**したときは、`int.TryParse` が `temp = 0` を返し、範囲へ丸めて、**代入する**。この 2 つの違いを、補助が正確に再現する必要がある。
- 項目 1 つを足す手数の例（`01-Metrics.md` §12、報告書 §3.9）: `RumbleBoost` は 6 ファイル（`ProfileGeneralLoadSection`、`ProfileGeneralSaveSection`、`OutputSlotSettings`、`OutputSettingsPanel.xaml`、`OutputSettingsViewModel`、`RumbleTestController`）。`TouchpadClickPassthru` は、読み込みが `ProfileTouchpadBasicLoadSection`、保存が `ProfileTriggerStickSaveSection` と、別々の節にある。
- 既存の宣言型: `Core/Settings/ControllerSetting.cs`（`Bool`／`Enum`。`WithUi`）と `ControllerOptionsStore`。保存と読み込みは定義の並びを順に処理し、真偽値の既定値・解析失敗の扱いを、従来の保存の読み込みに合わせてある。
- 黄金テスト: `GoldenPersistenceTests`、`ProfileGoldenGapTests`、`ProfileResetGoldenTests`、`LegacyFunctionGoldenTests`、`FlickStickGoldenTests`、`GyroControlsGoldenTests`、`AppSettingsSerializerGuardTests`。

## 方針・制約
- 保存の形は**一切変えない**。要素の順序は、`XmlSerializer` の宣言順と、手書きの `XmlWriter` の書き出し順の両方に依存する（37-6 の見送りの理由）。宣言型へ移すときは、現状の順を保つ。
- 現状の読み込みの動き（上記の「要素がない」と「解析に失敗」の違い）は、まず**そのまま**再現して置き換える。構造の変更と動きの変更を、同じコミットに混ぜない（黄金で構造の変更を検証するため）。D2＝(B) により、そのあとの**別のコミット**（RV5-3b）で、補助の 1 か所だけを直し、変わる黄金を更新する。
- 領域ごとに段階的に行う（Phase37-4 の方式: まず 1 領域で形を確かめてから広げる）。各領域の Step で、黄金テストが緑のまま。
- 空の `catch` の規則（RV2）を満たす。補助が「要素がないときは何もしない」を明示するので、`try { } catch { }` は不要になる。
- 公開の契約 `IAppSettingsService`・`IProfileSettingsService` は変えない。
- 新規の部品は 1 ファイル 1 型、コンストラクターの引数は 12 以下（新しい型は 6 以下をめざす）。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV5-0 | 調査。(1) 代表の項目（アプリ設定の真偽値・数値・文字列、プロファイル設定の真偽値・数値・列挙型・範囲つき数値）を 5 つ選び、足すときに触るファイルを `PhaseRV5-Setting-Touchpoints.md` に一覧にする。(2) `BackingStore` の公開フィールド 107 の、参照元のファイル数を数える（D3 の判断材料）。(3) 宣言型の比較（規約 §4）: Phase17 の `ControllerSetting`、.NET の Options パターン、`System.Text.Json` のソース生成、CommunityToolkit.Mvvm の `[ObservableProperty]`、属性ベースの `XmlSerializer`、他の WPF アプリの設定の持ち方。あわせて、D4＝(B) のソースの生成方式（Roslyn のインクリメンタル ソース ジェネレーター、生成したコードをリポジトリへ置く方式［再生成の手順と、最新であることの検査つき］、T4）も比べる。保存の形を変えずに使えるかを見る。ユーザーの承認を得る | — | 未着手 |
| RV5-1 | 特性化: 読み込みの動き（要素がない／解析に失敗／範囲の外／空の要素）を、節ごとの黄金テストで固定する。既存の黄金テストの穴を、`ProfileGoldenGapTests` に足す。D2＝(B) のため、あわせて (1) 解析に失敗したときに結果が変わる要素の一覧（節・要素・旧の結果・新の結果）を作り、承認を得る、(2) 実機のプロファイル（利用者のデータ。旧い版のものを含む。**読み取りのみ**）を走査し、解析に失敗する要素が実際にあるかを数える（あれば、利用者に見える変化になるので、一覧に載せる）。**これが緑でなければ、以降へ進まない** | 新規の黄金（壊れた入力・欠けた入力）。既存の黄金一式 | 未着手 |
| RV5-2 | `AppSettingsService` の定型を畳む: `SetIfChanged<T>(ref T field, T value, [CallerMemberName] string name = null)` の補助を 1 つ作り、71 のプロパティを 1〜2 行へ。`BackingStore` の項目は公開フィールドなので、`ref` で渡せる。変更通知の名前・タイミング（値が変わったときだけ、設定の後）は同じ | `AppSettingsServiceTests`（既存）、新規: 通知の名前が全プロパティで `nameof` と一致することを、リフレクションで確かめる | 未着手 |
| RV5-3 | プロファイルの読み込みの補助: `ProfileLoadContext` に `ReadInt`・`ReadBool`・`ReadFloat`・`ReadEnum`・`ReadString`（パス、範囲、代入先）を足す。**要素がないときは何もしない**、**解析に失敗したときは現状と同じ値（0 や既定値）を丸めて代入**。まず `ProfileStickAxialLoadSection` で置き換え、黄金が緑であることを確かめてから、`ProfileTouchpadMouseLoadSection` ほかへ広げる。空の `catch` は、補助の中の 1 か所（理由つき）になる | RV5-1 の黄金。補助の単体テスト（要素なし／解析失敗／範囲の外／空） | 未着手 |
| RV5-3b | 読み込みの動きの修正（D2＝(B)）: 補助の 1 か所を、**解析に失敗したとき（空の要素を含む）は何もしない（既定値のまま）**へ直す。RV5-1 で承認された一覧に載る黄金だけを更新し、それ以外の黄金は変えない。解析に成功した値が範囲の外のときの丸めは、現状のまま。RV5-3 とは別のコミット | 更新した黄金（一覧のとおり）、補助の単体テスト（解析失敗・空の要素） | 未着手 |
| RV5-4 | 宣言型の試作（1 領域）: 項目の定義（要素名、既定値、範囲、読み書き）の並び `ProfileSetting`（`ControllerSetting` の延長）を作り、**1 領域**（候補: `GeneralSlotSettings` の一部。RV5-0 の結果で選ぶ）の読み込みと保存を、1 つの定義の並びから処理する。保存の順序を現状と一致させ、黄金で確かめる。形を確かめたら、承認を得て広げる | 黄金（その領域）。新規: 定義の並びから読み書きした結果が、旧実装の出力と一致する差分テスト | 未着手 |
| RV5-5 | 領域ごとに広げる（D1＝(B)。広げる前に、RV5-4 の試作の結果を見せてユーザーの承認を得る。再決定ではなく、確認の関門）: General、Gyro、Touchpad、Stick、Trigger、Lightbar、Output の順を目安に、**1 領域 1 コミット**。各領域で、読み込みの節と保存の節を、定義の並び 1 つに置き換える。`ProfileAreaSection` は、定義の並びを受ける形へ | 黄金（領域ごと）、`ProfileResetGoldenTests` | 未着手 |
| RV5-5b | アプリ設定の宣言化と `AppSettingsDTO` の生成（D4＝(B)）: アプリ設定の項目（要素名、型、既定値、順序）を定義の並びにし、`AppSettingsDTO` を、その定義から生成する。生成の方式は RV5-0 の比較で決める。`XmlSerializer` の要素の順序は、定義の並び＝宣言順で保つ（37-6 の見送りの理由）。RV5-2 で畳んだ `AppSettingsService` も、定義から作れるかを見る。**関門**: WPF の 1 回目のビルド（`_wpftmp`）と合わない、または `AppSettingsSerializerGuardTests`・`GoldenPersistenceTests` が緑にならない場合は、(A)（形を変えない）へ戻す提案をする | `AppSettingsSerializerGuardTests`、`GoldenPersistenceTests`、新規: 生成物が最新であることの検査（生成コードを置く方式の場合） | 未着手 |
| RV5-6 | 設定画面との結びつき: 宣言した項目に、画面での見せ方（`WithUi`）を持たせられる範囲を決める。機種の設定（`ControllerSetting`）は、すでに画面が定義から作られる。プロファイルの設定の画面は、この Phase では**触らない**（RV4 の画面の作業と競合するため）。足す手順の文書化まで | — | 未着手 |
| RV5-7 | `BackingStore` の公開フィールド（107）: RV5-0 の参照数を見て、D3 に従う（調査のみ／段階的に窓口へ） | `SettingsSlotWindowUsageTests`、`LayerDependencyRuleTests` | 未着手 |
| RV5-8 | 文書: `Model-Diagram/02-Extension-Guide.md` に「アプリ・プロファイルの設定の項目を足す手順」の節を足す。`ModelDiagramGuardTests` が見る型の一覧に変更があるときは、`01-Structure.md` も同じ Step で更新 | `ModelDiagramGuardTests` | 未着手 |
| RV5-9 | 実機確認: 既存のプロファイル（実機のユーザーデータ。旧い版のものを含む）を読み込んで保存し、前後の XML が同じこと。設定画面の保存・再読み込み | 実機（最後にまとめて 1 件ずつ） | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | 範囲 | (A) RV5-1〜RV5-3（特性化、`AppSettingsService` の畳み込み、読み込みの補助）まで。宣言型（RV5-4・5）は、効果を見てから再承認／(B) RV5-5 まで全領域／(C) RV5-2 だけ | (A) 利点: 低リスクで、重複の大半（`AppSettingsService` 68%、`ProfileStickAxialLoadSection` 91%、空の `catch` 約 45）が減る。欠点: 「読み込みと保存が別の場所」は残る。(B) 利点: 項目の追加が 1 か所になる。欠点: 保存の形に触れる範囲が広く、最もリスクが高い。(C) 利点: ほぼ無リスク。欠点: 効果が限定的。**推奨は (A)**。RV5-4 の試作の結果で、(B) へ進むかを再承認する |
| D2 | 読み込みの動きの差異（要素がない→何もしない、解析に失敗→0 を代入） | (A) 現状と完全に同じにする／(B) 解析に失敗したときも「何もしない（既定値のまま）」に直す | (A) 利点: 保存の形も読み込みの結果も不変。欠点: 不自然な動き（解析に失敗すると 0）が残る。(B) 利点: 自然。欠点: 壊れた値を持つ既存のプロファイルの読み込み結果が変わる（利用者に見える）。**推奨は (A)**（(B) は別の Phase で、黄金の更新とともに承認を得る） |
| D3 | `BackingStore` の公開フィールド（107） | (A) この Phase は調査のみ（参照数を数えて記録）。窓口へ寄せる実施は、結果を見て別に承認／(B) 段階的に窓口（`AppSettingsService`・`ForSlot`）へ寄せ、フィールドを `internal` にする | (A) 利点: 範囲が膨らまない。欠点: 袋が残る。(B) 利点: 状態の袋が解消する。欠点: 参照が全層に広がっている見込みで、大きな変更。**推奨は (A)** |
| D4 | `AppSettingsDTO` の扱い | (A) 形を変えない（37-6 の決定のとおり）／(B) 項目の定義からソース生成する | (A) 利点: 保存の形の保証が簡単。欠点: DTO への 1 項目の追加は残る。(B) 利点: 追加が 1 か所。欠点: ソース生成とビルドの複雑さ（WPF の 1 回目のビルドの誤りとの相性）。**推奨は (A)** |

## 決定事項（2026-11-19、ユーザーが選択）
| # | 決定 | 計画への反映 |
|---|---|---|
| D1 | (B) 全領域を宣言型にする（RV5-5 まで） | 試作（RV5-4）の結果を見せたうえで、全領域へ広げる（確認の関門を残す）。1 領域 1 コミット。この Phase の最もリスクの高い部分 |
| D2 | (B) 解析に失敗したときも「何もしない」に直す | 構造の置き換え（RV5-3。現状の動きを再現）と、動きの修正（RV5-3b。補助の 1 か所）を別コミットにする。変わる要素の一覧と、実機のプロファイルの走査の結果を、RV5-1 で承認してから実施。保存の形は変わらない（読み込みの結果だけが、壊れた値のときに変わる） |
| D3 | (A) この Phase は調査のみ | `BackingStore` の公開フィールド 107。実測（2026-11-19、概算）: 型を参照するファイル 90＝Core 69、Processing 8、Composition 7、UI 4、Output 1、`App` 1。主な利用者は永続化の節の読み書き。D1＝(B) で節がフィールドを直接触らなくなったあとで、窓口へ寄せるかを別に承認 |
| D4 | (B) 項目の定義からソース生成する | RV5-5b に置く。生成の方式（Roslyn のソース ジェネレーター／生成コードの取り込み／T4）は RV5-0 で比較して決める。関門（`_wpftmp` との相性、黄金が緑）を通らなければ (A) へ戻す。規模が大きいため、承認の際に別 Phase へ切り出すかを再確認する |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 保存の形は、黄金テストと、実機のプロファイルの「読み込み→保存」の前後比較で確かめる。
- 環境変数・OS の状態に触れない（規約 §4）。テストは一時フォルダーを使う。

## リスク
- 高。保存の形に触れる（要素の順序、既定値、解析の失敗時の値）。黄金テストの穴が、そのまま事故になる。RV5-1 の特性化を最初に行い、1 領域 1 コミットで進める。
- 古い版のプロファイル（互換の移行 `ProfileMigration`）が、新しい読み込みの補助を通ると、順序や既定値の扱いが変わる可能性。移行の経路もテストに含める。
- D2＝(B) は、利用者に見える変化。壊れた値（解析に失敗する要素）を持つ既存のプロファイルは、読み込み後の値が変わる（0 → 既定値）。実機のプロファイルの走査（RV5-1）で、実際に該当するものがあるかを確かめ、一覧で承認を得る。
- D4＝(B) は、ビルドの仕組み（ソース生成と WPF の 1 回目のビルド）に触れる。関門で確かめ、通らなければ (A) へ戻す。
- `SetIfChanged` で、値の等価の比較（浮動小数点、文字列、配列）が、元の `!=` と同じ意味になること（型ごとに確かめる）。

## 完了の定義
- `AppSettingsService` の構造の重複が 20% 以下（`Measure-CodeReview.py --only clone`）。`ProfileStickAxialLoadSection`・`ProfileTouchpadMouseLoadSection` に、理由のない空の `catch` がない。
- 全領域の読み込みと保存が、定義の並び 1 つから処理される（D1＝(B)）。
- 解析に失敗した要素は何も代入しない（D2＝(B)）。変わった黄金は、承認された一覧のものだけ。
- `AppSettingsDTO` が定義から生成されている（D4＝(B)）。関門を通らず (A) へ戻した場合は、その理由が記録されている。
- 黄金テスト一式が緑。実機のプロファイルの読み込み→保存の前後で、XML が同じ。
- `02-Extension-Guide.md` に、設定の項目を足す手順がある。
- Debug ALL が警告 0・エラー 0。

## 関連計画
- `PhaseRV2-Plan.md`（空の `catch` の規則）、`PhaseRV4-Plan.md`（画面側）、`PhaseRV6-Plan.md`（`Gyro*Info`）。
- 過去: `Phase37-Plan.md`（保存の整理。37-4 の節の対、37-6 の見送りの理由）、`Phase17-Plan.md`（`ControllerSetting`）、`Phase15-6Z`（設定の領域の窓口）、`Phase8-Step4b`（節への分割）。
- 規約: §2.2（機能の維持）、§3.1（設定の領域の窓口）、§3.3 原則4（TODO を消さない）。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
- 2026-11-19: D1＝(B)、D2＝(B)、D3＝(A)、D4＝(B) をユーザーが選択（D1・D2・D4 は推奨 (A) と異なる）。D3 の判断のため、`BackingStore` を参照するファイルの層別の数を概算で測った。D2 の修正は構造の置き換えと別のコミット（RV5-3b）に、D4 は RV5-5b に置き、関門を付けた。
