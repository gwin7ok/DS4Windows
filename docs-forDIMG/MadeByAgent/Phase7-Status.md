# フェーズ7 進捗管理文書: `Global`／`BackingStore` の解体、起動の近代化、`Mapping` の instance 化

最終更新日: 2026-10-17  
状態: **Step1・Step2 完了。Step3 の計画書（`Phase7-Step3-Plan.md`）を作成済み。Q1〜Q5 の承認待ち。**  
計画書: `Phase7-Plan.md`。調査: `Phase7-0-Survey.md`。残存台帳: `Phase7-Residual-Ledger.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Phase7-0 | 調査と Step 分割案 | 完了（D1〜D6 承認、2026-10-17） | `Phase7-0-Survey.md` |
| Step1 | 整理と台帳の確定 | **完了**（2026-10-17。W1〜W4 のビルド・テスト成功、U-10 確認済み） | `Phase7-Step1-Plan.md`、`Phase7-Step1-Completion-Report.md` |
| Step2 | DI インターフェースの 0 件メンバー（49）の個別判断 | **完了**（2026-10-17。W1〜W7 のビルド・テスト成功。U-11 の実機確認は未。完了報告書あり。判断: 削除 45／接続 3／温存 1。Phase8 へ 12） | `Phase7-Step2-Plan.md` |
| Step3 | `BackingStore` の DI 化と `Global` の転送の削除 | **実施中**（Q1〜Q5 承認済み。W1 完了、W2 実装済み・ビルド・テスト待ち） | `Phase7-Step3-Plan.md` |
| Step4 | 起動・終了シーケンスの近代化 | 未着手 | |
| Step5 | `Program.rootHub` と Service Locator の削除 | 未着手 | |
| Step6 | 出力スロットの所有権の移動 | 未着手 | |
| Step7 | `Mapping` の instance 化（1）特性化テストと状態の切り出し | 未着手 | |
| Step8 | `Mapping` の instance 化（2）instance 化と呼び出し元の切り替え | 未着手 | |
| Step9 | `Mapping` の instance 化（3）互換 `AppHost` の削除ほか | 未着手 | |
| Step10 | 仕上げ（`Global` の残り、永続化の切り出し、総合検証） | 未着手 | |

## 2. ラチェットの基準値（全域）

- 推移（`Global.*`／`AppHost`／`Program.rootHub`／`ServiceProviderHolder`）:
  - Phase6 完了時（2026-10-17）: 747／40／22／（未追跡）
  - Phase7-Step1 W1 後: 733／40／22／（未追跡）
  - Phase7-Step1 W4 後（現在）: **733／40／22／23**
- ガード: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`rootHub`）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）、`Phase7Step1DeadMethodGuardTests`（`BackingStore` の削除済みメソッド）。
- 目標値は、各 Step の計画書で確定し、`Phase7-Residual-Ledger.md` に反映する。

## 3. 各 Step の詳細進捗

### Phase7-Step1: 整理と台帳の確定【完了（2026-10-17）】
- W1: `BackingStore` の孤立メソッド 10 名（396 行）を削除。`Global.*` 747 → 733。
- W2: K2（`GetProfileActionIndexOf` が未検出で -1）と未使用変数の削除。
- W3: 不具合 B（アプリ設定の DTO 読込の後処理の欠落）の是正。
- W4: `ServiceProviderHolder` の追跡（23 件）。
- 実機確認 U-10: 偽の exe 名の保持は○。スキップしたバージョンは値の保持まで○（通知の抑制は U-7 とあわせて確認）。

### Phase7-Step2: DI インターフェースの 0 件メンバーの個別判断【計画書の承認待ち】
- 走査の結果、本体の呼び出しが 0 件のメンバーは 61。うち `Actions/` の未配線の抽象 11 は Phase8 へ移す。残り約 50 が対象。
- 調査中に、`IDeviceStateService` の `SetDevice` に本体の呼び出しが無く、サービス内の配列が空のまま、`ConnectedControllersCount` などが常に 0 を返す構造であることを確認した（使うのは `ControllersViewModel.HasControllers` だけで、そのプロパティ自体の使用箇所が無い。実害は出ていない）。判断の詳細は `Phase7-Step2-Plan.md`。

### Phase7-Step3〜Step10【未着手】
- 内容と依存関係は `Phase7-Plan.md` §2。

## 4. 実機確認の台帳（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11）

- U-1〜U-7: 機材・環境が揃うまで未確認（リスク受容）。
- U-8、U-9: 確認済み（Phase6）。
- U-10: 確認済み（偽の exe 名の保持。スキップしたバージョンの値の保持）。通知の抑制は、新バージョンの公開時に確認（U-7 とあわせる）。
- 今後の追加候補: Step4 の後（終了時のホスト破棄、`-driverinstall` の起動）、Step6 の後（スロットの付け外し）、Step7〜9 の後（通常入力・スティック・タッチパッド・ジャイロ・マクロ・スペシャルアクション）。

## 5. 持ち越し・保留事項

- K3（マクロの二重ガード）: `Mapping` の instance 化（Step8〜9）の中で、再設計の要否を判断。
- K7-1: 通常の起動では解消済み。`-driverinstall` 分岐の `Global.Load()` の経路は Step4。
- `AppHost.CreationNote` と `[DI] Host creation path` のログ: ユーザー決定により残す。Step4 の最後に、残すか判断する。
- 孤立定数 8 件（`BackingStore`）: Step10 の定数の再配置。
- Phase8 への申し送り: `Actions/` の `ServiceProviderHolder` 17 件、`Actions/` の未配線の抽象 11 メンバー（`IActionBinding`、`IKeyController`、`IActionRegistry`、`IControllerRegistry`、`IInputAction`、`IMacroPlayer`、`ITrigger`）。

## 6. 直近の次アクション

1. `Phase7-Step2-Plan.md` §3（Q1〜Q5）の承認。
2. 承認後、Step2 を W1 から実施。
