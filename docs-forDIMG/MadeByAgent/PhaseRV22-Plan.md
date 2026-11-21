# PhaseRV22 計画書: MVVM を A（90%）へ

作成日: 2026-11-22。決定: 利用者が「MVVM を A へ」を選択（RV21 の次の Phase）。
内容: code-behind の部品への直接代入のうち、状態の判断を持たない 9 件を、XAML の初期値・`DataTrigger`・ViewModel のプロパティへ移す（RV20 と同じ方針）。挙動は変えない。

## 完了記録（2026-11-22）
- 直接代入 136→127。MVVM 89.8%→90.1%（A）。再採点 `RV22.json`、`02-Scorecard.md` に記録。
- Debug ALL OK（3,728 件）。実機確認は、見た目が変わっていないこと（`Device-Check-Checklist.md` §9o）。
