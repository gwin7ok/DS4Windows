# Phase6-Step9b 計画書: Copilot レビュー（PR #1）指摘 14 件の検証と是正

作成日: 2026-10-14  
状態: 計画書作成済み・承認待ち（実装未着手。次の作業は Step9b-0 の検証）  
対象ブランチ: `For-DI-migration-work`  
位置づけ: Step9 の後・Step10 の前（Step7b と同じく、実機確認・外部レビューで見つかった事項を扱う追加 Step）  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`（特に §2.1〜2.4、§3.1、§3.3、§4）  
参照: PR #1（`gwin7ok/DS4Windows-Vader4Pro`、`For-DI-migration-work` → `master`）への Copilot の自動レビュー（2026-10-13 22:54 JST、COMMENTED、指摘 14 件。依頼は 22:46 JST に gwin7ok アカウントから）

---

## 0. 背景と方針

### 0.1 経緯
- 2026-10-13、PR #1（作業ブランチ全体の PR。コミット 1013 件）に Copilot の自動コードレビューが付いた。判定は「Changes recommended」、指摘は 14 件（High 4、Medium 7、Low 3）。
- 2026-10-14 に、指摘を現在のブランチ（Step9 完了時点、HEAD `83845149`）で簡易確認した。指摘の該当箇所はほぼすべて残っている。ただし、自動レビューのため、実害の有無（稼働経路かどうか）は未検証。
- 指摘の一部は、`copilot-instructions.md` §2.2（現在の機能の完全維持。250ms デバウンスなどの安全弁は 100% 維持）に反する可能性がある。Step10 以降の移行でさらに積み上げる前に、検証と是正を行う。

### 0.2 方針
1. **まず検証する**（Step9b-0）。自動レビューの指摘を鵜呑みにせず、現行コードと、置換前の実装（`git log -S`／`git show <commit>^:<path>`）と比べて、次の 3 つに分類する。
   - 分類A: 稼働経路で実害がある（修正する）
   - 分類B: 未配線のコード（系統A など）で、現時点の実害はない（TODO コメントを付けて Phase8 へ。§3.3 原則4）
   - 分類C: 誤検出（何もしない。理由を記録する）
2. **配分**: 機能・安全に関わるもの（F01〜F07）と、テストの品質（F10、F11、F14）は本 Step。Pure DI 違反（F08、F09）は Step10。1ファイル1型違反（F12、F13）は Step10b（台帳に既に載っている）。
3. 修正は小さなマイクロステップに分け、ユーザーのビルド・テスト・実機確認を待つ（Step9 と同じ）。

---

## 1. 指摘の台帳（Copilot の指摘 14 件と配分）

重大度は Copilot の判定。「簡易確認」は 2026-10-14 時点の現行コードでの確認。

### F01（High）プロファイル切替の 250ms デバウンスが落ちている
- 指摘: `Actions/DefaultProfileSwitcher.SwitchProfile` が、デバイスごとの 250ms デバウンスなしで `ApplyFromAction` を呼ぶ。押しっぱなしのトリガーが、切替先のプロファイルでも成立し、連鎖（カスケード）する。
- 簡易確認: `DefaultProfileSwitcher.cs:21〜23` にガードが無い。`Phase2-Bugfix-ProfileSwitch-CascadeLoop.md` には、同ファイルにデバウンスを導入したと記録されている。Phase5-Step14 の「`SwitchProfile` 一本化」で落ちた可能性がある。一方、`Mapping.cs` 側の稼働経路（`ApplyProfileDirect`）や `ProfileApplicationService.ApplyFromAction` に別のガードがあるかは未確認。
- 配分: Step9b。検証後、分類A なら修正。
- 実機確認（案）: 同じトリガーを各プロファイルに割り当てた構成（5-3 テスト）で、1 回の短押しにつき 1 段階だけ切り替わること。ログ: `ApplyProfile CALLED: device=…, profile=…`（Debug）の回数。

### F02（High）`IManagedActionManager` が DI に登録されていない
- 指摘: `ServiceRegistration` に登録が無いのに、`ActionManager` のファサードと `AppHost.PreallocateActionEntries` が解決しようとする。`DefaultActionManager` が使われず、`ActionManager.SetToggledOn` が失敗し（コントローラーが握りつぶす）、トグル状態の通知が失われる。
- 簡易確認: `ServiceRegistration` に `IManagedActionManager` の登録が無い。`ActionManager.cs` に `sp.GetService(typeof(IManagedActionManager))` が 8 箇所以上ある。`DefaultActionManager.cs` は存在する。
- 配分: Step9b。登録するか、ファサード側の利用をやめるかは決定事項（Step9b-0）。

### F03（High）`MouseOutputAction` がマウスイベントを出さない
- 指摘: `Execute`／`Stop` が `IVirtualKBM` を取得するだけで呼ばない。テストは「例外が出ない」ことしか見ていない。
- 簡易確認: `Actions/MouseOutputAction.cs` に `PerformMouse…`／`MoveRelativeMouse` の呼び出しが無い。`Mapping.cs:1266〜1360` が、`MouseOutputAction` を DI から取得する経路を持つ（「new DI route + fallback preserved」）。DI 登録の有無と、この経路が稼働するかは未確認。
- 配分: Step9b。経路が稼働していれば、マウスボタン割り当てが効かなくなる重大な不具合のため、最優先で検証する。

### F04（High）`NLog.config` の `async="false"` で、ログ設定の変更が `NullReferenceException`
- 指摘: 非同期ラッパーを無効にすると、`logfile` が直接の `FileTarget` になる。`LoggerHolder.UpdateLogSettings`／`RestoreArchiveSetting` は `FindTargetByName<WrapperTargetBase>("logfile").WrappedTarget` を前提にしているため、設定変更で例外になる。
- 簡易確認: `NLog.config:6` は `async="false"`。`LoggerHolder.cs` の `UpdateLogSettings`（180 行付近）・`RestoreArchiveSetting` が同じ前提。Step9-3 の実機確認でログ設定を変えた際のログにも `Log settings updated:` の行が見当たらず、整合する。例外は WPF のバインディングが握りつぶすため、画面では気づきにくい。
- 配分: Step9b。直接の `FileTarget` とラッパーの両方に対応させる。
- 実機確認（案）: 設定タブでアーカイブ数と最小ログレベルを変えた直後に、`Log settings updated: MaxArchiveFiles=…, MinLevel=…`（Debug）が出ること、レベルがその場で反映されること。

### F05（Medium）`DefaultMacroPlayer` が 4 スロット固定
- 指摘: `bool[4]` で、デバイス 4〜7 のマクロが何もしない（8 スロット対応のはず）。
- 簡易確認: `DefaultMacroPlayer.cs:16` が `new bool[4]`。使用者は `MacroAction`／`MacroActionAdapter`（系統A の経路）。稼働しているかは未確認。
- 配分: Step9b。スロット数は `IEnvironmentService.ControllerSlotLimit`（または `Global.MAX_DS4_CONTROLLER_COUNT`）に合わせる。

### F06（Medium）`LaunchProcessAction` がすべての起動を非表示にする
- 指摘: 通常の GUI 実行ファイルも `UseShellExecute=false`、`hidden=true` で起動される。非表示は `.bat`／`.cmd` のラッパーだけにすべき。
- 簡易確認: `LaunchProcessAction.cs:38` が `_launcher.Launch(targetPath, arguments, false, true)`。使用者は `LaunchProcessActionAdapter`（`ActionFactory`／`DefaultActionFactory`）と `Mapping.cs:5047` のフォールバック。
- 配分: Step9b。置換前の `Mapping.cs` の起動方法（`git show` で確認）と比べて、同じ動作に戻す。

### F07（Medium）`ServiceProviderHolder.GetRequiredService` が null を返しうる
- 指摘: 名前に反し、`GetService` を呼んで null を `T` として返す。失敗が無関係な呼び出し先へ移る。
- 簡易確認: `ServiceProviderHolder.cs:17`。`ActionFactory`／`ActionManager` が `ServiceProviderHolder.Provider` を直接使う（多数）。
- 配分: Step9b。例外を投げるようにすると、null を許容していた呼び出し側が影響を受けるため、呼び出し側の確認が前提。

### F08（Medium）`ProfileActionChainService` が自分で依存を解決し、フォールバックで `new`
- 指摘: `_actionDispatcher = actionDispatcher ?? AppHost.GetService<IMappingActionDispatcher>() ?? new MappingActionDispatcher()`。Pure DI 違反。
- 簡易確認: `ProfileActionChainService.cs:15`。同じパターンが `ProfileApplicationService`（`_deviceState ?? AppHost.GetService…` など）にもある。
- 配分: **Step10**（サービス実装のコンストラクタのフォールバック解消）。Step10-0 の台帳に、同じパターンのサービス実装を洗い出して載せる。

### F09（Medium）`ControllersViewModel` のコンストラクタの依存が任意引数＋ Service Locator
- 簡易確認: `ControllersViewModel.cs:18〜23`。
- 配分: **Step10**（ViewModel の Pure DI 化。決定1＝S9-A と同じ方針）。

### F10（Medium）サブ設定が null だとテストが `return` で通ってしまう
- 簡易確認: `ProfileSettingsServiceSubSettingsTests.cs` の複数箇所（41、66、94 行など）に `if (service.LSModInfo == null …) return;`。
- 配分: Step9b。null なら `Assert.NotNull` で失敗させる。ただし、環境未初期化で null になりうるテストがあるため、初期化を明示してから検査する。

### F11（Medium）シングルトンのイベント購読のテストが、リフレクションでは検証になっていない
- 簡易確認: `SpecialActionsListViewModelTests.cs:28`（`GetFields` でデリゲート型のフィールドを探す）。購読は発行側（サービス）に保持されるため、検出できない。
- 配分: Step9b。偽のシングルトンのイベントで、購読と解除（`Dispose`）を動作で検証する。

### F12（Low）`NotificationEventArgs` と `INotificationService` が 1 ファイルに同居
- 簡易確認: `DI/INotificationService.cs`。
- 配分: **Step10b**（台帳に載っている。`Phase6-Step10b-Plan.md` 付録 B 付近の `INotificationService.cs`）。

### F13（Low）`Trigger.cs` に 4 つの型が同居（ファイル名がどの型名とも不一致）
- 簡易確認: `DS4Control/Trigger.cs`（`TriggerContext`、`TriggerType`、`ITrigger`、`SingleButtonTrigger`）。
- 配分: **Step10b**（台帳に載っている）。

### F14（Low）`StandaloneTests` の正規化テストが、同じ関数同士を比較していて常に通る
- 簡易確認: `StandaloneTests/TestSpecialActionUtils.cs:30〜35` が `Util.NormalizeActionName` 同士を比較。
- 配分: Step9b。比較先を `Global.NormalizeActionName`（`ScpUtil.cs` のシム）にするか、テストを削除・改名する。

---

## 2. 決定が必要な事項（Step9b-0 で、検証結果とともにユーザーへ提示する）
- **決定1**: 分類B（未配線で実害なし）の扱い。案: 稼働経路でなければ TODO コメント（§3.3 原則4）を付けて Phase8 へ送る（推奨）／本 Step で直す。
- **決定2**: F02 の方針。案: `DefaultActionManager` を登録する／`ActionManager` ファサード側の利用をやめる。
- **決定3**: F07 の方針。案: 例外を投げる実装に変え、呼び出し側を確認・修正する／名前を `GetService` 相当に変える。
- **決定4**: PR #1 のレビューコメントへの返信・解決済みマーク（GitHub への書き込み）は、ユーザーの明示的な許可を得て行うか、ユーザー自身が行うか。

## 3. マイクロステップ（案。Step9b-0 の結果と決定で確定）
- **Step9b-0（実装なし）**: F01〜F07、F10、F11、F14 の検証。稼働経路か（分類A／B／C）、置換前の実装との比較（`git log -S`、`git show`）、決定の提示。結果を本書 §1 に追記する。
- **Step9b-1**: F04（ログ設定の `NullReferenceException`）。テスト＋実機確認。独立していて確実なため先に行う。
- **Step9b-2**: F01（デバウンス）と F07（`GetRequiredService`）。F01 は §2.2 の安全弁のため、回帰テスト（連続発火の遮断）を必須とする。
- **Step9b-3**: F02、F03、F05、F06（Actions 系。分類A のもの）。分類B のものは TODO コメントのみ。
- **Step9b-4**: テストの品質（F10、F11、F14）。
- **Step9b-5**: 文書更新（Status、Plan、Phase8 計画書、Step10／Step10b への申し送り）、完了報告書 `Phase6-Step9b-Completion-Report.md`。

## 4. リスクと対策
- **自動レビューの誤検出**: 検証で分類C にする。理由を記録し、不要な変更をしない。
- **未配線のコードを直して、稼働経路に影響する**: 分類B は変更せず、TODO と Phase8 への申し送りのみ。
- **`GetRequiredService` の変更による新たな例外**: 呼び出し側（`ActionFactory`、`ActionManager`）を網羅して確認してから変更する。
- **ログ設定の変更**: 実機確認が必須（§2.3。ログ出力とレベルの維持）。

## 5. 完了判定チェックリスト（案）
- [ ] F01〜F07、F10、F11、F14 の各件が、検証の結果（A／B／C）と対応が記録されている
- [ ] 分類A の指摘が修正され、回帰テストで固定されている
- [ ] 分類B の指摘に TODO コメントがあり、Phase8 計画書へ申し送られている
- [ ] F08、F09 が Step10 の台帳に、F12、F13 が Step10b の台帳に載っている
- [ ] `dotnet build`／`dotnet test` が全件成功。実機確認（F01、F04 ほか）で問題なし
- [ ] Status／Plan／完了報告書を更新
