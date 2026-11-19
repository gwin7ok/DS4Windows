# PhaseRV2 計画書: 例外の握りつぶしと、非同期・待機の作法

作成日: 2026-11-19
状態: **承認済み（RV2-0 の分類の承認待ち）**。D1〜D4 は 2026-11-19 にユーザーが選択済み（「決定事項」を参照）
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-05〜RV-08（§3.6、§3.7）
前提: PhaseRV1 の完了（`CodeHealthRatchetTests` の枠、空の `catch` を 2 つに分けた計測）。
動き: 機能は変えない。**ログの追加・変更は、利用者に見えないファイルのログだけ、承認のうえで**（規則 2.3。D1）。

## 目的
空の `catch`（389 件。全 `catch` の 48%）を、意図で分類し、「理由が分かる形」へ揃える。ログ呼び出しを `try { } catch { }` で包む 41 行をなくす。`async void`・`.Result`・`.Wait()`・`Thread.Sleep` の作法を決める。**落ちない安全弁（最上位のループ・タイマー・読み取りスレッドの `catch`）は 100% 維持する**（規則 2.2）。

## 背景（実測。数字は `01-Metrics.md`）
- 空の `catch`: 389／811（本体が空か、コメントのみ）。層別は `Processing` 213、`Core` 106、`UI` 36、ルート 14、`Output` 10、`Composition` 7、`Input` 3。
- 上位のファイル: `KeyButtonActionController.cs` 59、`ProfileStickAxialLoadSection.cs` 29、`DefaultActionManager.cs` 21、`ProfileTouchpadMouseLoadSection.cs` 15、`App.xaml.cs` 14、`MacroPlayback.cs` 12、`RepeatHelper.cs` 11、`KeyButtonControllerRegistry.cs` 11。
- ログ呼び出しを `try { } catch { }` で包む行: 41。
- `async void` 14（`MainWindow` 5、`AutoProfilesViewModel` 4、`WelcomeDialog` 3、`UdpMotionService`・`Mapping`・`UpdaterWindow` 各 1）。`.Result`・`.Wait()` 21。`Thread.Sleep` 13。
- 例外のログの方針は `Log-Policy.md` にあるが、「捨てる例外の書き方」の規則はない。
- 既存の安全弁: `GuardedTimerTick`（`UI/Services`。Phase29 で、タイマーが例外 1 つで止まる問題を是正）、合成入力の確定のロックの `try`／`finally`、各読み取りスレッドの最上位の `catch`。

## 方針・制約
- **まず分類、次に処置**。全件の分類表（RV2-0）を承認してから、手を動かす。
- 処置は 3 つのどれか: (a) 型を限定する、(b) 理由のコメントを書く、(c) 診断のログ（Debug／Trace）を残す。**何も書かない空の `catch` は 0 にする**（コメントのみのものは、理由の質を見る）。
- 最上位の安全弁（読み取りスレッド、毎フレームの経路、タイマーの本体）は、型を絞らない。絞ると、想定外の例外で全入力が止まる（Phase29 の 6-7・6-12 の教訓）。
- プロファイルの節（`Core/Persistence/ProfileSections`）の `try { } catch { }` は、RV5 で宣言化するときに「欠けた要素は既定値のまま」の 1 つの部品へ畳むので、この Phase では**触らない**（二重の作業を避ける。RV5 の部品が、ここで決める規則を満たすこと）。
- `KeyButtonActionController`（59 件）は、この Phase では分類と処置だけ。構造の整理（`PressActionController`、アダプター）は RV6。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV2-0 | 分類。空の `catch` 389 件を、`PhaseRV2-Catch-Inventory.md` に表にする（ファイル:行、捕まえる型、`try` の本体の主な呼び出し、分類、推奨の処置）。分類は次の 5 つ: **S1** 後始末・解放・切断済みの機器への書き込みなど、失敗してよい／**S2** 古い保存ファイルとの互換のため、既定値のまま続ける／**S3** 捕まえる型が広すぎる（`Exception`）のに、想定する例外が限られる／**S4** 失敗を診断のログへ残すべき／**S5** 防御が不要（呼び出し先が投げない）。あわせて、同種のアプリでの扱いを比較する（規約 §4。例: 本家 DS4Windows、ViGEm 系のクライアント、`Meziantou.Analyzer` の MA0xxx、`SonarAnalyzer` の S2486・S108 の考え方）。ユーザーの承認を得る | — | 叩き台を作成（2026-11-19。`scripts/audit/Inventory-EmptyCatch.py`）。承認待ち |
| RV2-1 | 規則の文書化: `Log-Policy.md` に「例外の扱い」の節（G2。D3）を足し、`.github/copilot-instructions.md` §2.3 にその節へのリンク（1 行。承認済み）を足す。内容は、(a)(b)(c) の使い分け、最上位の安全弁の書き方、`catch (Exception)` を使ってよい場所、`when` フィルターの使い方 | — | 未着手 |
| RV2-2 | ログ呼び出しを包む `try { } catch { }`（41 行）: `AppLogger` が呼び出し側へ例外を出さないことを確かめ（出す経路があれば `AppLogger` の入口で吸収し）、呼び出し側の `try` を消す | 新規: 例外を投げる出力先を渡しても、`AppLogger.LogXxx` が例外を出さない。既存の `AppLogger*Tests` | 未着手 |
| RV2-3 | `Processing`（213 件）: S1 はコメント、S3 は型を限定、S4 は診断のログ（D1）、S5 は `try` ごと削除。対象の上位: `KeyButtonActionController`、`DefaultActionManager`、`MacroPlayback`、`RepeatHelper`、`KeyButtonControllerRegistry`。毎フレームの経路は、型を絞らない（上の方針） | 既存の `Processing` のテスト一式（特性化テスト、`AntiSnapbackDifferentialTests` など）。型を絞った箇所には、その例外で動きが変わらないことを確かめる単体テスト | 未着手 |
| RV2-4 | `Core`（106 件。`ProfileSections` を除く）: 設定・永続化・プラットフォームの `catch` | `BackingStore*Tests`、黄金テスト（保存の形が変わらないこと） | 未着手 |
| RV2-5 | `UI`（36）、ルート（14。`App.xaml.cs`）、`Output`（10）、`Composition`（7）、`Input`（3） | 既存のテスト。`App` の起動・終了は `AppShutdownSequenceTests`、`AppHostStartupShutdownGuardTests` | 未着手 |
| RV2-6 | 非同期・待機の作法: (1) イベントハンドラー以外の `async void` を `async Task` へ（呼び出し側が `await` か、`_ = …` と理由の明記）。(2) `.Result`・`.Wait()` 21 件を分類し、UI スレッドで待つものは非同期化、専用スレッドの中のものは理由のコメント。(3) `Thread.Sleep` 13 件を分類し、読み取りスレッドの再接続待ちなど専用スレッドの中のものは残して理由を書き、UI スレッドのものは非同期の待ちへ | 既存のテスト。デッドロックが疑われる箇所は、単体テスト（タイムアウトつき）を足す | 未着手 |
| RV2-7 | 検査: `CodeHealthRatchetTests` に、「何も書かない空の `catch`＝0」「ログを包む `try`＝0」「イベントハンドラー以外の `async void`＝0」を足す。コメントのみの空の `catch` の件数は、基準として凍結して減らす（理由の質は検査できない） | `CodeHealthRatchetTests` | 未着手 |
| RV2-8 | 実機確認: 型を絞った（S3）箇所と、非同期化した箇所だけを、`Known-Issues-Ledger.md` §3 と `Device-Check-Checklist.md` に 1 件ずつ足す（機器の抜き差し、プロファイルの切替、終了） | 実機（最後にまとめて 1 件ずつ） | 未着手 |
| RV2-9 | 評価の再算定（採点基準 `Code-Review-2026-11-19/03-Rubric.md`）。(1) `scripts/audit/Rubric-Inputs.json` の確認項目と手入力の値を、根拠（実行したテスト・実測）を `evidence` に書いて更新する。(2) `python -I scripts/audit/Score-CodeReview.py --json docs-forDIMG/Code-Review-2026-11-19/scorecard/RV2.json --label "RV2 完了" --diff docs-forDIMG/Code-Review-2026-11-19/scorecard/RV1.json` で、採点して保存する。(3) `02-Scorecard.md` に列と理由（等級が変わった観点、点数が動いた指標、予想とのずれ）を足し、`01-Metrics.md` §13 の「現在」を `Measure-CodeReview.py` の実測で取り直す。(4) ユーザーへ、点数・等級の変化と、次の Phase で動かす指標を報告する。机上の予想（`03-Rubric.md` §5）で動く観点: 冗長処理 C→B。ソースは変更しない（記録のみ） | `Score-CodeReview.py`（記録のみ） | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | ログの追加（報告書の G3。規則 2.3 の例外） | (A) S4 に限り、**ファイルのログだけ**を Debug／Trace で足す（利用者に見える画面・トレイのログは変えない）。足した行は一覧で承認を得る（Phase13b の前例）／(B) ログは足さず、理由のコメントだけ | (A) 利点: 失敗の原因が追える。欠点: ログの量が増える（レベルが低いので通常は出ない）。(B) 利点: 規則 2.3 と衝突しない。欠点: 原因が見えないまま。**推奨は (A)** |
| D2 | S3（広すぎる型）の扱い | (A) 後始末・ファイル I/O は型を絞る。毎フレームの経路・最上位の安全弁・タイマーの本体は絞らず、理由のコメントを書く／(B) すべて絞る／(C) 絞らない | (A) 利点: 安全弁を守りつつ、想定外が表に出やすくなる。欠点: 分類の判断が要る。(B) 利点: 揃う。欠点: 全入力が止まる事故の危険（Phase29 の教訓）。(C) 利点: 動きが変わらない。欠点: 広い `catch` が残る。**推奨は (A)** |
| D3 | 規則の置き場所（G2） | (A) `Log-Policy.md` に節を足す／(B) `copilot-instructions.md` §2 に足す | (A) 利点: ログの方針と同じ場所。欠点: 規約本体から見つけにくい。(B) 利点: 規約として強い。欠点: 規約が長くなる。**推奨は (A)**（規約本体には、`Log-Policy.md` の節への 1 行の参照だけ足す。規約の変更は承認を得る） |
| D4 | 理由のないコメント（`// ignore` など）の扱い | (A) 検査は件数の凍結だけにし、分類表で質を見る／(B) コメントに決まった書き出し（例: `// 無視してよい: <理由>`）を義務づけ、検査で数える | (A) 利点: 軽い。欠点: 質は人が見る。(B) 利点: 機械で数えられる。欠点: 書き方が硬い。**推奨は (A)** |

## 決定事項（2026-11-19、ユーザーが選択）
| # | 決定 | 計画への反映 |
|---|---|---|
| D1 | (A) ファイルのログだけ足す | S4 に限り、`AppLogger.LogDebug`／`LogTrace`（ログファイルだけ。画面・トレイには出ない）で足す。足した行は、RV2-0 の分類表と同じ一覧で承認を得る（Phase13b の前例）。報告書 G3 は (A) |
| D2 | (A) 場所で使い分け | 後始末・ファイル I/O は型を絞る。毎フレームの経路・最上位の安全弁・タイマーの本体は絞らず、理由のコメントを書く。個々の箇所の分類は RV2-0 の表で承認を得る |
| D3 | (A) `Log-Policy.md` に節を足す。**加えて、規約本体（`.github/copilot-instructions.md`）にその節へのリンクを書く** | RV2-1 で、`Log-Policy.md` に「例外の扱い」の節を足し、規約 §2.3 に、その節を指す 1 行（リンク）を足す。規約本体の変更はこの 1 行だけで、ユーザーの承認済み。報告書 G2 は (A)＋リンク |
| D4 | (A) 件数の凍結＋分類表 | 検査は「何も書かない空の `catch`＝0」とコメントのみの件数の凍結。理由の質は分類表（RV2-0）で人が見る |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 型を絞った箇所は、例外の種類を差し替えた偽物で、動きが変わらないことを確かめる。
- 実機確認は RV2-8（最後にまとめて）。

## リスク
- 中。型を絞ると、これまで黙って捨てていた想定外の例外が、上へ伝わって止まる。最上位の安全弁を絞らない方針（D2）と、分類の承認（RV2-0）で抑える。
- ログを足すと、診断の量が増える（D1 でレベルを Debug／Trace に限る）。
- `async void` を `async Task` にすると、例外の伝わり方が変わる（呼び出し側が `await` するか、明示的に捨てる）。

## 完了の定義
- 何も書かない空の `catch` が 0、ログを包む `try` が 0、イベントハンドラー以外の `async void` が 0（`CodeHealthRatchetTests`）。
- 残る空の `catch`（コメントのみ）は、すべて `PhaseRV2-Catch-Inventory.md` に分類されていて、全件に理由のコメントがある（`ProfileSections` の分は RV5 の完了で同じ状態にする）。
- `.Result`・`.Wait()`・`Thread.Sleep` の全件に、残す理由か、非同期化の記録がある。
- `Log-Policy.md` に「例外の扱い」の節がある。
- Debug ALL が警告 0・エラー 0。実機確認の項目が台帳に足されている。
- 評価の再算定（最終 Step）が済み、`02-Scorecard.md` に記録され、ユーザーへ報告されている（予想とのずれの理由つき）。

## 関連計画
- `PhaseRV1-Plan.md`（検査の枠）、`PhaseRV5-Plan.md`（プロファイルの節の `try { } catch { }`）、`PhaseRV6-Plan.md`（`KeyButtonActionController` の構造）。
- 過去: `Phase29-Plan.md`（安全弁 6-7・6-12）、`Phase12`・`Phase13b`（ログの整理と、規則 2.3 の例外）。
- 規約: §2.2（機能の維持）、§2.3（ログ）。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
- 2026-11-19: D1＝(A)、D2＝(A)、D3＝(A)＋規約本体へのリンク 1 行、D4＝(A) をユーザーが選択。
- 2026-11-19: 最終 Step「評価の再算定」を追加（採点基準 `Code-Review-2026-11-19/03-Rubric.md`、`scripts/audit/Score-CodeReview.py`）。基準 v1 は同日ユーザーが確定。
- 2026-11-19: RV2 を承認。ブランチ `PhaseRV2`。RV2-0 の叩き台（388 件）を作成。分類の承認待ち。
