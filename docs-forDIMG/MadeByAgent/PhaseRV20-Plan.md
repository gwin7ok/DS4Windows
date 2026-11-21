# PhaseRV20 計画書: 残りの画面の出し分けの移行と、実機確認の消化

作成日: 2026-11-21
状態: **計画（決定済み: D1＝コード作業を先、D2＝設定パネル・導入ダイアログ＋Readings＋MainWindow・AutoProfiles、D3・D4＝推奨どおり）。コードは未着手**
出典: `Measure-CodeReview.py --only mvvm`（RV19 完了時点）、`PhaseRV19-Plan.md` の完了記録。
動き: **挙動・ログ・保存の形は変えない**。

## 現状（RV19 完了時点）
- MVVM 88.9%（A−。A は 90%）。直接代入: `Content` 61・`Visibility` 49・`SelectedIndex` 25・`IsEnabled` 22・`ItemsSource` 9。
- 実機確認は **RV3〜RV19（§9b〜§9m、RV15・RV17、RV18-0〜2）がすべて未実施**。単体テスト約 3,720 件は通っている。

## 残っている直接代入の集中先
| 場所 | 内容 | 判断 |
|---|---|---|
| `ControllerReadingsControl.Readings` `Content` 21、`.xaml.cs` `Visibility` 8 | 毎フレームの入力値の表示 | RV19 で見送り。割り当て・頻度を測ってから決める |
| `BindingWindow.xaml.cs` `Visibility` 9 | ホバーの強調など（見た目だけ）が中心 | 残してよい見込み（規約の「見た目だけ」） |
| `WelcomeDialog.xaml.cs` `IsEnabled` 7、`Installers` `Content` 4 | 導入の進行の表示 | `InstallProgress` を ViewModel の状態へ（RV17 の続き） |
| `LightbarSettingsPanel` `IsEnabled` 4、`LanguagePackControl` 3、`TouchpadSettingsPanel` `Content` 4 | 設定パネルの出し分け | VM の bool へ |
| `MainWindow`・`AutoProfiles` の `SelectedIndex`／`ItemsSource` 計 約 25 | 選択・一覧の同期 | 双方向バインディングへ。選択の副作用が多く、実機で見た目の確認が要る |

## 決定してほしい事項

### D1 順序
- **(A) 実機確認を先に行い、見つかった不具合を直す（推奨）**。RV3〜RV19 の変更が積み上がっており、これ以上画面を変える前に現状を確かめる。確認は 1 項目ずつ、選択肢（OK／NG／未確認）で答える。
- (B) コード作業を先に行い、確認は RV20 の後にまとめて。

### D2 コード作業の対象（D1 の確認のあとに行う分）
- **(A) 設定パネルと導入ダイアログ（`WelcomeDialog`・`LightbarSettingsPanel`・`LanguagePackControl`・`TouchpadSettingsPanel`）（推奨）**。直接代入が約 20 件減り、MVVM は 90%（A）に届く見込み。単体で固めやすい。
- (B) (A) ＋ `ControllerReadingsControl.Readings`（先に割り当て・頻度を測り、悪化するなら見送り）。
- (C) (A) ＋ `MainWindow`・`AutoProfiles` の選択・一覧の同期（見た目の確認が要る。リスクが大きい）。
- (D) やらない（実機確認のみ）。

### D3 進め方
- **(A) 画面ごとに、(1) 現在の出し分けを開いた実物の値で固める表のテスト、(2) ViewModel の状態と `{Binding}` へ置き換え（推奨。RV19 と同じ）**。
- (B) 先に置き換え、実機で確かめる。

### D4 ラチェット
- **(A) 直接代入・イベント属性の基準は、減った分だけ同じコミットで更新（推奨）**。

## Step（D1＝A、D2＝A、D3＝A の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV20-0 | 実機確認（利用者と 1 項目ずつ。結果を `PhaseRV20-DeviceCheck-Result.md` へ） | 記録のみ |
| RV20-1 | NG の原因調査と修正（1 件 1 コミット。特性化テストを足してから） | Debug ALL |
| RV20-2 | `WelcomeDialog` の導入の進行 → ViewModel | 表のテスト、Debug ALL |
| RV20-3 | `LightbarSettingsPanel`・`LanguagePackControl`・`TouchpadSettingsPanel` の出し分け → ViewModel | 同上 |
| RV20-4 | 再採点（`RV20.json`）、`02-Scorecard.md`、チェックリスト（§9n）、計画書の完了記録、マージ | `ModelDiagramGuardTests` |

ブランチは `PhaseRV20`。コミットは Step ごと、完了で `--no-ff` マージ（push なし）。

## 予想
- 直接代入 166→約 145。MVVM 88.9→約 90%（A に届く可能性がある。境界のため断言しない）。

## リスク
- 実機で NG が出たとき、原因の RV の特定に時間がかかる（チェックリストの「疑い先」を使う）。
- 設定パネルは、`DataContext` が複数あり、バインディングの経路を誤ると出し分けが効かない。表のテストは、窓を画面の外で 1 回表示して読む（RV19 の方式）。

## 次の Phase（見込み）
RV21: `MainWindow`・`ProfileEditor`・`App` の整理（500 行級の型）と、`Readings` の判断。

## 決定の記録（2026-11-21）
- D1＝(B) コード作業を先。実機確認は RV20 の後にまとめて 1 回（§9b〜§9n）。推奨は (A) だったが、利用者の選択。
- D2＝(A)＋(B)＋(C) の全部。**追加の Step**:
  - RV20-5: `ControllerReadingsControl.Readings`: 先に更新の頻度と割り当て（`MappingHotPathAllocationTests` 方式）を測り、悪化するなら、その部分を見送って理由を記録。
  - RV20-6: `MainWindow`・`AutoProfiles` の `SelectedIndex`／`ItemsSource`: 選択の副作用（プロファイルの切り替え）を特性化テストで固めてから、双方向バインディングへ。見た目の確認は実機（後でまとめて）。
- D3・D4＝(A)。Step の順は RV20-2 → 3 → 5 → 6 → 4（再採点は最後）。実機確認の Step（RV20-0・1）は、RV20 の後の別の作業として扱う。
