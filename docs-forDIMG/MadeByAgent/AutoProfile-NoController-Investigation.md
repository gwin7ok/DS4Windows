# 調査メモ: コントローラー未接続でも自動プロファイルが適用され、通知が出る件

作成日: 2026-10-19 ／ 種別: 調査のみ（コードは未変更）

## 1. 結論（要約）

| 論点 | 判定 |
|---|---|
| 未接続のスロットへ自動プロファイルを適用する | **元のコード（master の `AutoProfileChecker.Process`）から同じ挙動。退行ではない。** 元のコードは `device == null` の分岐を持ち、Halt せずに `Global.ApplyProfile` を呼んでいた。接続の有無は見ていない。 |
| 切替の通知が出る | **退行（設定の取りこぼし）。** 元は `AutoProfileSwitchNotifyChoice`（既定 `None`）で通知の可否を決めていた。DI 化の途中で、この判定が UI 層へ委譲されたが、UI 側は `AutoProfileSwitchNotifyChoice` を見ていない。 |
| `Global_SelectedProfileChanged: Controller item not found` | 無害。`LogDebug` のみ。UI の `controllerDict` に未接続スロットの項目がないだけで、元のコードでも同じ（イベントの購読側の仕様）。 |
| Phase8-Step7（7.1 Button）との関係 | なし。`PublishBindings`（適用の最後）が空のスロットへ束縛を差し替えるだけ。 |

## 2. 根拠

### 2.1 未接続でも適用する（元のコードと同じ）

- 現行: `AutoProfileService.CheckProfiles`（[AutoProfileService.cs:106](../../DS4Windows/DS4Control/Services/AutoProfileService.cs)）は、`ProfileNames[j]` が設定されたスロット j すべてに `_profileAppService.ApplyProfile(...)` を呼ぶ。接続の有無の検査はない。
- 元: `git show master:DS4Windows/AutoProfileChecker.cs` の `Process()` は、`Program.rootHub.DS4Controllers[j]` が `null` のときも `Global.ApplyProfile(j, tempname, true, true, ...)` を呼ぶ（Halt しないだけ）。現行の `ProfileApplicationService.ApplyProfile` の `device != null ? HaltReportingRunAction : applyAction()` は、この分岐の写し。
- 意図的な設計: 接続時（`ControlService.PrepareConnectedInputController`）は `useAutoProfile = GetUseTempProfile(index)` が真なら、プロファイルを適用し直さず、事前に適用済みの一時プロファイルをそのまま使う（`aee3ea54`「Have device hotplug routine enforce profile settings when using a temp auto profile」由来）。つまり「接続前に自動プロファイルを先に当てておく」ことが、接続時の挙動の前提になっている。

### 2.2 通知の退行

- 元: `AutoProfileChecker` は `display = (autoProfileSwitchNotifyChoice == Notification || LogAndNotification)` を算出し、`Global.ApplyProfile(..., display)` → `AppLogger.LogProfileChanged(..., displayNotification)` で、偽ならイベント（通知）自体を発火しなかった。既定値は `None` なので、既定では**通知は出なかった**。
- 現行: `978104ff`／`5a51cef5`／`6b7e2e46`（2026-09-16、「通知判定を UI 層へ委譲」）で `displayNotification` を削除。`AppLogger.LogProfileChanged`（[AppLogger.cs:122](../../DS4Windows/DS4Control/AppLogger.cs)）は常にイベントを発火し、`MainWindow.OnProfileChanged`（[MainWindow.xaml.cs:792](../../DS4Windows/DS4Forms/MainWindow.xaml.cs)）→ `ShowProfileSwitchNotification` は `ProfileChangedNotification`／`Notifications` だけを見る。
- `AutoProfileSwitchNotifyChoice` は、設定画面（`AutoProfilesViewModel.ProfileSwitchChoice`）の読み書きと XML の保存に残るだけで、**通知の判定には誰も読まない**。ユーザーが画面で「なし／ログ／通知／ログと通知」を選んでも効かない。
- 補足: その仕様変更は、手動切替・スペシャルアクション向けには意図したもの（`38f4a356`「通知部分は仕様通り動作」）。ただし `ProfileChangeSource.AutoProfile` だけは、別の設定（`AutoProfileSwitchNotifyChoice`）で制御されていたため、取りこぼした。
- 未接続かどうかとは無関係。接続中でも自動プロファイルの切替で同じ通知が出る。ユーザーが未接続のときに気づいたのは、他に表示がなかったため。

## 3. 是正案（承認後に実施。コードは未変更）

### 案A（推奨）: 通知の判定を復元する（退行の是正）
- `MainWindow.OnProfileChanged` で `e.Source == ProfileChangeSource.AutoProfile` のとき、`_autoProfileService.AutoProfileSwitchNotifyChoice` が `Notification`／`LogAndNotification` の場合だけ `ShowProfileSwitchNotification` を呼ぶ（`None`／`Log` は呼ばない）。
- ログ（`LogToGui(prolog)`）は適用側で常に 1 回出る（元と同じ）ので、2.3 のログ維持の方針に合う。`Log` を選んだときのログの扱いは、元のコードの `DisplayProfileChange` の `Log` 分岐（GUI のログのみ）と同等で、現行の `LogToGui` が担う。
- 影響範囲は UI 層 1 か所。未接続のスロットの扱いには触れない。
- テスト: `OnProfileChanged` は WPF のため、判定を純粋関数（例: `ShouldNotify(source, choice)`）へ切り出して単体テストする。

### 案B（任意・別途承認）: 未接続のスロットを飛ばす
- `CheckProfiles` で `Control.DS4Controllers[j] == null` のスロットを飛ばせば、未接続の通知はなくなる。ただし**機能が落ちる**（規約 2.2）。
  - 接続前に当てた一時プロファイルが失われ、接続時に `useAutoProfile` が偽になって、通常プロファイル（`OlderProfilePath`／Linked）が適用される。ゲーム起動中にコントローラーをつないでも、自動プロファイルが効かなくなる（つなぎ直しの直後から次の `CheckProfiles` までの 1 周期は特に）。
  - `_tempAutoProfile` が `matched` のまま保持され、「既に適用済み」の判定（`useTemp && tempname == currentTemp`）に頼る再試行が起きない。スキップするなら、接続イベントで `CheckProfiles` を再実行する仕組みが要る。
  - `OlderProfilePath` は一時適用（`isTemp: true`）では更新されない。通常へ戻す側（`isTemp: false`）は更新する。スキップしても `OlderProfilePath` は壊れないが、上記の機能低下が起きる。
  - Phase8-Step6 の `PublishSlot`／`ReleaseSlot` との関係: 未接続のスロットの `PublishBindings` は空のリストを登録するだけ。切断時に `ReleaseSlot` が束縛を消したあと、再接続時に `useAutoProfile` が真なら `PublishSlot` が組み立て直す。案B にすると、この経路が使われる機会が減る（接続前に当てた状態がないので、通常プロファイルの適用に倒れる）。
- 推奨しない。必要なら「未接続のスロットでは、適用はするが通知は出さない」が、機能を落とさない折衷案（案A に加えて、`e.DeviceIndex` の接続の有無を `OnProfileChanged` で見る。元のコードにはない新仕様なので、ユーザーの承認が要る）。

## 4. 確認してほしいこと（実機）
- 設定画面の「自動プロファイルの切替通知」の選択（既定は「なし」）と、今回通知が出たときの値。`None` でも出ていれば、2.2 のとおり。
- 元のビルド（`master` 系のタグ）で、同じ条件（未接続・自動プロファイル一致）で通知が出ないことの確認（既定値の場合）。

## 5. 参照
- `master:DS4Windows/AutoProfileChecker.cs`（元の `Process`）、`master:DS4Windows/DS4Control/ScpUtil.cs`（元の `ApplyProfile`）、`master:DS4Windows/DS4Control/Log.cs`（元の `LogProfileChanged`）
- コミット: `978104ff`、`5a51cef5`、`6b7e2e46`（displayNotification の削除）、`aee3ea54`（接続時の一時プロファイルの扱い）
- 規約: `.github/copilot-instructions.md` 2.2（機能維持）、2.3（ログ維持）

## 6. 実装の記録（2026-10-19、ユーザー承認済み: 案A＋案B［スロットごとに接続を確認］）

- 案A: `AutoProfileNotificationPolicy.ShouldNotify`（新設、純粋関数）。`MainWindow.OnProfileChanged` が呼ぶ。自動プロファイル由来は、`AutoProfileSwitchNotifyChoice` が `Notification`／`LogAndNotification` のときだけ通知。他の由来は従来どおり。
- 案B: `AutoProfileService.CheckProfiles` は、`DS4Controllers[j] == null` のスロットを飛ばす（`SlotConnectedProbe` で差し替え可。ControlService 未解決のときは接続とみなす）。
  - 接続時の再確認: `IAutoProfileService.CheckProfilesForConnectedController()`（新設）。前景が変わらないと `GetForegroundProcessInfo` が false を返すため、最後に取得した前景の情報を保持して照合し直す。`MainWindow` が `ControlService.HotplugController` で別スレッドから呼ぶ。
  - 通常プロファイルへの復帰（未知プロセス）は、未接続のスロットも従来どおり行う（古い一時プロファイルが残り、再接続時に適用されるのを防ぐ）。ただし未接続のスロットの通知は出さない（`item == null`）。
- 追加テスト: `AutoProfileServiceTests`（未接続で適用しない→接続後の再照合で適用／前景未取得で何もしない／通知の判定の Theory）。
- 未実行: ビルド・テスト（ユーザーが実行）。実機確認の観点: ①未接続で自動プロファイルが一致しても適用・通知なし ②接続した直後（前景は同じ）に自動プロファイルが適用される ③通知の設定の 4 値の効き方。
- 既知の注意: 接続直後の通知は、UI の `ControllerDict` に項目が入る前に適用が走ると、出ないことがある（競合）。

## 7. 検証結果（2026-10-19、ユーザー報告）
- ビルド、テストビルド、テスト実行: すべて成功。
- 実機確認: 問題なし。案A・案B とも完了。
