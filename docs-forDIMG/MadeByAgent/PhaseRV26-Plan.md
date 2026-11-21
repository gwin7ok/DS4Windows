# PhaseRV26 計画書（接続時のプロファイル選択を挙動へ）

作成日: 2026-11-22。承認: 「次の Phase へ」（残りの文字列テストを挙動へ、の継続）。

## 実施（完了）
- `ConnectionProfileChoiceTests`（9 件）: 初回接続は OlderProfilePath、再接続は記憶（なければ SelectedProfile）、別のコントローラーは初回、Linked の優先と UI、一時プロファイルは記憶しない（`TestDs4Device.SetMac` を追加）。
- `RuntimeSwitchTests`: コンテナの配線を、本番の登録で作った入れ物の挙動で確認。
- 撤去: `ReconnectProfilePerControllerTests` と `DisconnectCharacterizationTests` の文字列走査各 1 件、`RuntimeSwitchTests` の 2 件（うち `ApplyFromAction` の 1 件は代替なし）。
- 負の確認 済み。Debug ALL OK（3757 件）。再採点: `scorecard/RV26.json`。

## 残り
文字列走査 106 ファイル。上位の候補: `ControlDefaultAndExtrasTests`、`ActionAndProfileReferenceIdTests`、`DefaultUnboundTypeTests`、`ProfileEditorLayoutTests`。実機確認の追加はなし（挙動は変えていない）。
