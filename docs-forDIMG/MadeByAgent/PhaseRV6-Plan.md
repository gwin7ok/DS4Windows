# PhaseRV6 計画書: 共有部品化と、冗長な実装の整理

作成日: 2026-11-19
状態: **未承認（コードは未着手）**。D1〜D5 は 2026-11-19 にユーザーが選択済み（「決定事項」を参照）。`PressActionController` の削除は承認済み（実施は RV6-1）
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-21〜RV-25（§3.5、§3.6）
前提: PhaseRV1（未参照の型の調査の記録）、PhaseRV2（空の `catch` の分類）、PhaseRV4（`WelcomeDialog` の部品化は RV4 で済む）、PhaseRV5（`Gyro*Info` が使う設定の形）。
動き: 機能は変えない。未参照の型の削除は、規則 2.4 に従い、**1 件ずつ根拠を示して承認を得てから**。

## 目的
共有できる部品を 1 か所に集め、同じ形の重複と、役目を終えた置き物を整理する。範囲は、`Core/Settings` の `Gyro*Info` の二重、`KeyButtonActionController` まわり、同一の重複が目立つ数ファイル、そして、ほかから参照されない型（規則 2.4）。

## 背景（実測。数字は `01-Metrics.md`）
- `Core/Settings/GyroMouseInfo.cs`（163 行。同一の重複 22%）と `GyroMouseStickInfo.cs`（204 行。同一の重複 16%）に、平滑化のフィールドと入れ子の列挙型が二重にある。
- `Processing/Actions/` の `KeyButtonActionController.cs`（495 行、空の `catch` 59）、`KeyButtonActionControllerAdapter.cs`（103 行）、`RepeatHelperToIRepeaterAdapter.cs`（87 行）、`PressActionController.cs`（17 行）:
  - `PressActionController` は、本体が空の静的メソッドが 6 つだけの `[Obsolete]` の置き物で、ほかのどこからも使われない（名前の検索で、`.cs`・`.xaml`・テストに 0 件。「`KeyButtonActionController` へ置き換えた。移行を楽にするための無害な置き物として残す」という趣旨のコメントがある）。
  - `RepeatHelperToIRepeaterAdapter` は、`IRepeater.Start` に渡された `tickAction` を**無視する**（`RepeatHelper` が自前の送信を持つため）。契約が守られない包みで、ログ呼び出しを包む入れ子の `try { } catch { }` が複数ある。
- 同一の重複（10 行の窓）の上位: `FakerInputHandler.cs` 68／178（38%）、`AutoProfilesViewModel.cs` 61／261（23%）、`LinkedProfilesXmlSerializer.cs` 48／78（61%）、`HidHideAPIDevice.cs` 46／181（25%）、`Crc32Algorithm.cs` 32／252（12%）、`SendInputHandler.cs` 30／220（13%）、`GyroOutputProcessor.cs` 26／280（9%）。`ProfilePresets.cs`（71%）は表で、直さない。
- 構造の重複: `PresetMenuHelper.cs` 160／306（52%）、`ControllerReadingsControl.xaml.cs` 125／364（34%）。
- ほかから参照されない型は 55 件（誤検出を除くと、class・enum で約 28 件）。確認済みの未参照: `PressActionController`、`TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo`。そのほかの候補: `DualSenseDevice.cs` の `InputReportDataBytes`・`InputReportDataBytesUSB`・`InputReportDataBytesBT`・`HapticPowerLevelFriendlyName`・`GyroMouseSensDualSense`、`SwitchProDevice.cs` の `SwitchProSubCmd`、`InputDeviceCore.cs` の `BTOutputReportMethod`、`Input/Hid/HidDevices.cs` の `DeviceInfo`、`SwitchProRumbleEncoder.cs` の `RumbleTableData`、`UdpServer.cs` の `MessageType`、`DsuClientRegistry.cs` の `ClientRequestTimes`、`GyroOutputProcessor.cs` の `ParsedTriggerList`、`ControllerReadingsControl.xaml.cs` の `LatencyWarnMode`、`ProfileEditor.xaml.cs` の `RumbleType`、`PresetMenuHelper.cs` の `ControlSelection`。**誤検出の型**（拡張メソッドの静的クラス、XAML のマークアップ拡張、入れ子の型）は除く。
- インターフェース 187 のうち、実装 1・偽物なしが 77。規則 3.1（Phase36-B3）により、既存は撤去しない。

## 方針・制約
- 規則 2.4 の手順を、**1 件ずつ**踏む: (1) `git log -S` と計画書で、作った経緯を調べる、(2) 使うべきか（本プロジェクトの目的に照らして）を判断する、(3) 使うべきなら TODO コメント（原則4）と、接続の計画を残す。使うべきでないなら削除候補、(4) 判断と根拠を記録し、ユーザーの承認を得てから実装する。
- 共有部品は、同じ層の中だけで使う状態を持たない計算なら、インターフェースを作らず具象のまま（規則 3.1）。
- 保存の形・ログの文言・スレッドの動きを変えない。`Crc32Algorithm`・`FakerInputHandler`・`SendInputHandler` は、毎フレームの経路や P/Invoke に近い。畳むときは、性能（割り当て 0）を `Benchmark`・Assert の検査で守る。
- `KeyButtonActionController` の空の `catch` は RV2 で分類・処置済みの前提。この Phase は構造だけ。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV6-0 | 未参照の型の調査（規則 2.4 の手順 1・2・4）。上の候補約 28 件を、1 件ずつ、`git log -S` と計画書で経緯を調べ、「使うべき／使うべきでない／誤検出」に分けて、`PhaseRV6-Unreferenced-Types.md` に記録する（RV1-5 の 3 件を引き継ぐ）。ユーザーの承認を得る | — | 未着手 |
| RV6-1 | `KeyButtonActionController` まわり: (1) `PressActionController` の削除（承認後。`RemovedApiRuleTests` の表へ）。(2) D2＝(B): `IRepeater` の契約を実態に合わせる。調査（2026-11-19）で、本番の呼び出し 4 か所（`KeyButtonActionController`）はいつも `tickAction = null`、アダプターは `tickAction`・`initialDelay`・`interval` の 3 つをすべて無視する（間隔は `RepeatHelper` が自前で持つ）と分かった。`IRepeater.Start` を引数なしの `Start()` にし、アダプターの無視する引数と、ログ呼び出しを包む入れ子の `try { } catch { }` を消す（RV2 の規則に従う）。テストの偽物（`FakeDeterministicRepeater`・`MockRepeater`・`TestActionController`）を新しい契約へ直す。連打の動きは変えない。(3) `KeyButtonActionControllerAdapter` の責務を確かめ、不要な層なら畳む | `ActionControllerTests` の一式、`RepeatHelper` まわりの既存のテスト、`RemovedApiRuleTests` | 未着手 |
| RV6-2 | `GyroMouseInfo`・`GyroMouseStickInfo` の共有化: 平滑化の設定（フィールド・入れ子の列挙型）を、共通の型 `GyroSmoothingSettings`（仮）へ。保存の要素名・既定値は変えない（RV5 の読み込みの補助に従う）。呼び出し元の変更範囲を、先に数える | `GyroControlsGoldenTests`、`GyroOutputProcessor` のテスト、黄金一式 | 未着手 |
| RV6-3 | 同一の重複の整理: `FakerInputHandler`・`SendInputHandler`（キー送出の共通の定型）、`HidHideAPIDevice`、`LinkedProfilesXmlSerializer`、`AutoProfilesViewModel`、`Crc32Algorithm`。1 ファイルずつ、重複の中身を確かめ、**畳めるものだけ**畳む（無理に統合しない）。毎フレームの経路は、割り当て 0 の検査を保つ | 既存のテスト、割り当ての検査（`Category!=Benchmark` の Assert の検査） | 未着手 |
| RV6-4 | 構造の重複の調査: `PresetMenuHelper`（52%）、`ControllerReadingsControl`（34%）。D4＝(B) により、構造の重複も整理の対象にする。ただし RV6-3 と同じく、畳めるものだけ。表・宣言の並びは畳まない（`ProfilePresets` は対象外のまま）。`ControllerReadingsControl` の `Click`・タイマーは RV4-6 で整理済みの前提で、ここは構造の重複だけを扱う。畳み方は 1 件ずつ決め、承認を得る | `PresetMenuHelper` まわりの既存のテスト | 未着手 |
| RV6-5 | 未参照の型の整理（RV6-0 で承認されたものだけ）: 削除は `git rm`（1 型 1 コミット、`RemovedApiRuleTests` の表へ）。「使うべき」と判断したものは、原則4 の TODO（経緯、未接続の理由、接続予定）を付け、接続の作業を適切な計画書へ追加する | `RemovedApiRuleTests`、全テスト | 未着手 |
| RV6-6 | 公開の可変フィールドの調査（記録のみ）: `SpecialAction`（30）、`TouchMouseStickInfo`（17）、`GyroMouseStickInfo`（16）が、`XmlSerializer`・リフレクション・毎フレームの経路から直接触られているかを調べ、プロパティ化できる範囲を `PhaseRV6-Public-Fields.md` に記録（実施は、効果を見て別に承認） | — | 未着手 |
| RV6-7 | 検査: `CodeHealthRatchetTests` に、「真に未参照の型（承認済みの削除対象）＝0」に当たる項目は作らず（誤検出が多い）、代わりに `RemovedApiRuleTests` の表を更新する。`Measure-CodeReview.py --only clone,cloneexact` の数字を、`01-Metrics.md` に追記する | — | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | `PressActionController` | (A) 削除する（廃止済みの置き物。規則 2.4 の「使うべきでないもの」）／(B) 残す | (A) 利点: 誤用の入口がなくなる。欠点: なし（呼び出し 0）。(B) 利点: なし。欠点: 置き物が残る。**推奨は (A)**。承認を得てから実施 |
| D2 | `RepeatHelperToIRepeaterAdapter` の契約 | (A) `RepeatHelper` が `tickAction` を受ける形へ寄せる（契約に従う）／(B) `IRepeater` 側の契約を、`tickAction` を要らない形に変える／(C) 触らない | (A) 利点: 契約が守られる。欠点: `RepeatHelper` の変更（連打の動きに関わる）。(B) 利点: 変更が小さい。欠点: 契約の変更。(C) 利点: 動きが変わらない。欠点: 契約の違反が残る。**推奨は (B)**（調査で、呼び出し側が `tickAction` を使っていないと分かれば。使っているなら (A)） |
| D3 | `Gyro*Info` の共通化 | (A) 共通の型へ寄せる（保存の形は不変）／(B) 継承で共通の基底を作る／(C) 触らない | (A) 利点: 構成で共有でき、規約の方針に合う。欠点: 呼び出し元の変更。(B) 利点: 変更が小さい。欠点: 継承の結びつき。(C) 利点: 変更なし。欠点: 二重のまま。**推奨は (A)** |
| D4 | 重複を畳む基準 | (A) 同一の重複のうち、意味のある部分（表・P/Invoke の宣言を除く）だけ／(B) 構造の重複も含める | (A) 利点: 効果が高く、安全。欠点: 件数は限られる。(B) 利点: 徹底。欠点: 表を無理に畳んで読みにくくなる。**推奨は (A)** |
| D5 | 未参照の型の削除の進め方 | (A) RV6-0 の表を見て承認し、承認した分だけを RV6-5 で行う／(B) 1 件ずつその場で確認 | (A) 利点: 一覧で判断できる（記録も残る）。欠点: 表の確認に手間。(B) 利点: 細かく確認できる。欠点: 往復が増える。**推奨は (A)** |

## 決定事項（2026-11-19、ユーザーが選択）
| # | 決定 | 計画への反映 |
|---|---|---|
| D1＋D5 | (A) `PressActionController` を削除。ほかの未参照の型は、RV6-0 の表を見て一括で承認 | `PressActionController` の削除は承認済み（RV6-1。`RemovedApiRuleTests` の表へ）。ほかの候補約 28 件は、RV6-0 で 1 件ずつ経緯を調べて表にし（使うべき／使うべきでない／誤検出）、一括で承認を得てから RV6-5 で実施する（規則 2.4） |
| D2 | (B) `IRepeater` の契約を実態に合わせる | 実測で、本番は `tickAction` をいつも `null` で渡し、アダプターは 3 つの引数をすべて無視していると分かったため、(B) の推奨を確認のうえ選択。`Start()` と `Stop()` だけの契約にする。連打の動きは変えない |
| D3 | (A) 共通の型へ寄せる | 平滑化の設定を共通の型にし、`GyroMouseInfo`・`GyroMouseStickInfo` が持つ（構成）。保存の形は不変。呼び出し元の変更範囲は先に数える |
| D4 | (B) 構造の重複も含める | `PresetMenuHelper`（52%）・`ControllerReadingsControl`（34%）も対象。表・宣言の並びは畳まない。畳み方は 1 件ずつ承認を得る |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 毎フレームの経路（`Crc32Algorithm`、`SendInputHandler` など）に触れたときは、割り当て 0 の Assert の検査と、`--filter "Category=Benchmark"` の前後比較を、記録に残す。
- 実機確認が必要なもの: `RepeatHelper` の連打（D2）、キー送出（`FakerInputHandler`・`SendInputHandler`）を触った場合のみ。`Known-Issues-Ledger.md` §3 に足す。

## リスク
- 低〜中。連打（`RepeatHelper`）とキー送出は、利用者に見える。重複を無理に畳むと、微妙な差（待ち時間、順序）が消えるため、畳む前に差を特性化する。
- 未参照の型の削除は、リフレクションや XAML の文字列での参照を見落とす可能性（RV6-0 の調査で、`.xaml`・`.resx`・設定ファイルの文字列検索も行う）。

## 完了の定義
- 承認された未参照の型が削除され、残すものには TODO（原則4）がある。`RemovedApiRuleTests` の表が更新されている。
- `PressActionController` が削除され、`IRepeater.Start` が引数なしの `Start()` になり、`RepeatHelperToIRepeaterAdapter` が無視する引数を持たない（D2＝(B)）。
- `Gyro*Info` の二重が解消されている（D3 が (A) のとき）。
- 同一の重複が、`01-Metrics.md` の基準より減っている（目標は RV6-3 の調査のあとに、この計画書へ書く）。
- Debug ALL が警告 0・エラー 0。

## 関連計画
- `PhaseRV1-Plan.md`（RV1-5）、`PhaseRV2-Plan.md`、`PhaseRV4-Plan.md`、`PhaseRV5-Plan.md`。
- 過去: `Phase36-Plan.md`（C7: 使われていない契約の候補の調査）、`Phase39-54-Record.md`（`DsuClientRegistry`）、`Phase31-Plan.md`（機種間の読み取りの重複）。
- 規約: §2.4（未参照コード）、§3.1（インターフェースを作る基準）、§3.3 原則4。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
- 2026-11-19: D1＋D5＝(A)、D2＝(B)、D3＝(A)、D4＝(B) をユーザーが選択。D2 の判断のため、`IRepeater.Start` の呼び出しを調べた（本番は `tickAction` をいつも `null`、アダプターは 3 つの引数をすべて無視。使うのはテストの偽物だけ）。`PressActionController` の削除は承認済み。
