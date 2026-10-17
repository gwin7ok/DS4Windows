# Phase6-Step12 計画書: 呼出元0件旧静的シムの安全防壁付き物理削除とPhase6最終完了判定

作成日: 2026-09-09  
改訂日: 2026-09-18（Step2〜Step10確定スコープ・Step11実機総合検証成果統合・選択肢2［安全防壁付き物理削除］採用・全面拡充改訂）  
状態: 決定承認済み・§0〜§6 改訂済み（2026-10-17）。Wave 1 着手  
**2026-10-14 注記（文書整合）**: 本書の「Phase7 引き継ぎ101箇所」「Pre-Host 11箇所」は 2026-09-18 時点の記載で、現行の正と異なる。(1) Step3 は 2026-09-21 に方針を転換し、`Mapping.cs` の実参照150件を全件解消した（2026-09-24 完了）。防壁2の対象は「101箇所」ではなく、`Mapping` の静的 `profileSettings` 束縛（方針P2、TODO 付き）と温存3件である（`Phase6-Step3-Plan.md` §3.8）。(2) Pre-Host 領域は 2026-09-25 の Step7-0 で12箇所に再集計した。(3) Step7 決定6＝N、Step5 の実行時状態配列、K7-1 などの登録は §4 を参照。着手時（Step12-0）に、現行コードで削除候補の台帳を作り直すこと（`Phase6-Status.md` §6.3）。  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.12  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（Core層・Mapping層 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md`（UI層・ViewModel層 監査記録）  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step10-Plan.md`（各ステップ確定実装計画書）  
  - `Phase6-Step11-Plan.md`（第1層自動テスト ＆ 第2層実機総合検証計画）  

---

## 0A. Step12-0 の結果: 台帳の作り直し・決定事項の提示（2026-10-17、実装なし・承認待ち）

HEAD は `e0bc8258`（作業ツリーはクリーン）。以降の §0〜§6 は 2026-09-18 版のままで、現行コードと食い違う箇所がある。食い違いの一覧と改訂案は §0A.5。本節の数値は、2026-10-17 時点の現行コードを走査して得た。

### 0A.1 集計の方法

- 走査の道具: Python のスクリプト（`scripts/audit/Step12/` に保存。`audit.py`＝コメント除去と宣言の抽出、`count.py`＝`Global`、`countb.py`＝`BackingStore`、`cascade.py`＝連鎖、`gen.py`＝Wave 割り当て）。`dotnet` は使っていない。
- 対象: `DS4Windows`、`DS4WindowsTests`、`StandaloneTests` の全 `.cs`（`obj`／`bin` を除く）。コメントは除いた。文字列リテラルの中は数える（ガードテストの文字列の誤検出は、削除の Wave ごとに目視で確認する）。
- 参照の数え方（`Global` の各メンバーごと）:
  - 修飾参照: `Global.名前`、`DS4Windows.Global.名前`。
  - 非修飾参照: `using static DS4Windows.Global;` を持つファイル（コードは `Mapping.cs`、`DS4LightBar.cs` の 2 件のみ。テストの該当行は文字列）の中の、単語としての名前。
  - 内部参照: `Global.cs` 内の、別メンバーからの参照。型名と同名のメンバー（`OutContType` など）は内部参照として多めに数える（安全側）。
  - 連鎖: 「外部の呼び出し元が 0 件で、内部の参照元がすべて同じく不要」なメンバーは、不要とみなす（固定点まで繰り返し）。
- 確認した「抜け道」:
  - XAML の `x:Static`／`Binding`: `Global` への参照は 0 件（`ControllerRegisterOptionsWindow.xaml` の "Global" は文字列「JoyCon Global Options」）。
  - リフレクション: `typeof(Global)` は、イベントの送信元（sender）の値としてだけ使われている（`Global.cs` 2、テスト 3）。`GetType("…Global")`、`nameof(Global.…)`、別名（`using X = Global`）、継承は 0 件。
  - `partial class Global` は無い（Global は `Global.cs` の 1 ファイル）。
- 精度の限界: `BackingStore` はインスタンスなので、名前だけで数える（`.名前` と初期化子の `名前 =`）。0 件の判定は確実だが、1 件以上の判定は偶然の同名で過大になる。

### 0A.2 `Global` の結果（487 名、宣言 494 件）

- 外部の呼び出し元が 0 件: 286 名。うち 33 名は、生きているメンバーの内部部品（フォールバック実体、`m_Config`、ViGEm の内部関数など）で、削除できない。
- **不要（削除候補）: 253 名、宣言 258 件。** `Global.cs` の非空行 836 行（全体 3,472 行の約 24%）。
- 生存: 234 名。内訳（外部の呼び出し元の分類。台帳 `Phase6-Step11-Global-Residual-Ledger.md` の A〜H と対応）:
  - D（サービス実装の薄いシム）のみが呼ぶ: 144 名。§2.1 の「薄いシム」の委譲先。サービスが `Global` の実体を呼ぶ構造のため、呼び出し元は 0 件にならない。Phase7（実体のサービス側への移管）まで温存。
  - G（第 1〜2 層・Actions・Phase7）を含む: 32 名。温存。
  - E（Composition Root・Pre-Host・MainWindow／ProfileEditor）を含む: 12 名。温存。
  - UI（定数・名前表のみ）: 7 名。F（DTO・永続化）: 4 名。複合（D＋F、B＋D＋F）: 2 名。温存。
  - 内部部品のみ（外部の呼び出し元 0、内部の生きたメンバーが使う）: 33 名。温存。

#### 削除禁止の 5 領域との突き合わせ

- 防壁 1（`BackingStore`）: 不要 258 件のうち `BackingStore` 自身のメンバーは 0 件（`Global` の転送アクセサだけ）。`BackingStore` 側は §0A.3。
- 防壁 2（Phase7 持ち越し）: 不要 258 件のうち、`Mapping.cs`・`DS4LightBar.cs` の非修飾参照、`SyntheticDispatcher`、第 1 層（`DS4Device` 系）が参照するものは 0 件。`Mapping.cs` の静的 `profileSettings` 束縛（方針 P2、TODO 付き）と温存 3 件は、`Global` のメンバーではないので影響を受けない。
- 防壁 3（Pre-Host）: 該当 0 件（`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`、`FindConfigLocation`、`multisavespots`、`SetCulture`、`UseLang` などはすべて生存）。
- 防壁 4（定数・純粋計算）: 該当 1 件（`DS4_STICK_AXIS_MIDPOINT`）。→ 要判断（下記）。
- 防壁 5（ViGEm クライアント依存）: 指定の 4 つ（`RefreshViGEmBusInfo`、`IsRunningSupportedViGEmBus`、`vigembusVersion`、`vigemInstalled`）は生存。隣接する `CheckForDevice` は不要だが、防壁の対象ではない（下記）。

#### 分類結果（不要 258 件）

- **削除可: 256 件**（Wave 割り当ては §0A.6、名簿は §0A.8）。§2.4 の判断は、すべて「使うべきでないもの」:
  - ProfileSettings 系の互換アクセサ 150 件: 呼び出し側の切り替えは Step2〜10 で完了済み。DI サービス（`IProfileSettingsService` など）という正規の経路があり、`Global` 側は重複する並行実装。
  - `*Instance` のフォールバック群 19 件: DI コンテナ経由が正規の経路。本体の呼び出し元は 0 件で、テストが「DI または代替」の取得口として使っているだけ。
  - 上流由来の孤立コード 25 件: 4 つのイベント（`ControllerStatusChange`、`BatteryStatusChange`、`ControllerRemoved`、`DeviceStatusChange`）と発火メソッドは、上流の 1.4.x 時代に作られ、購読側の記述が上流の `d02317b5`「Minor code clean up」で消えて以降（`git log -S` で確認）、購読者も発火元も無い（DI 化とは無関係）。引数型 `BatteryReportArgs`、`ControllerRemovedArgs`、`DeviceStatusChangeEventArgs` の 3 型も、これらだけが使うため連鎖して不要（各 1 ファイル、`git rm`）。`SerialChangeArgs`／`DeviceSerialChange` は `DeviceStateService` が使うので生存。ほかに旧フィールド（`m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`）、`DS4Mapping`、`DownloadLang`、`CheckForDevice`（ViGEm の確認は `CheckForSysDevice` 系に移っている。`ee5ff50e` ほか）、重複実装の `NormalizeActionName`（正本は `Util.NormalizeActionName`）、呼び出し元 0 の旧 getter（`getUseExclusiveMode`、`getQuickCharge`、`GetMinToTaskbar`、`getFlashWhenLate`、`getFlashWhenLateAt`、`IsUsingUDPServerSmoothing`）、`DebouncingMsHasChanged`。
  - `BackingStore` 直転送の薄いラッパー 62 件: `GetDS4Action`、`UpdateDS4CSetting`、`SaveProfile` など。呼び出し元は 0 件で、DI 側に正規の経路がある。
- **要判断: 2 件**
  - `DS4_STICK_AXIS_MIDPOINT`（定数 128、呼び出し元 0）: 防壁 4 の定数。推奨は**温存**（害が無く、削除の利益が無い）。
  - `getDefaultX360ControlBinding`（`defaultButtonMapping` の添字参照の 1 行）: 呼び出し元 0。推奨は**削除可**（表 `defaultButtonMapping` は生存するので、情報は失われない）。
- **温存: 0 件**（防壁にかかるものは、上の「要判断」の 1 件だけ）。

#### 注意が必要な個別事項

- `GetProfileActionIndexOf`（`Global` 側）: K2（未検出時に 0 を返す。`Phase6-Status.md` の持ち越し項目 K2）の対象の片方。`Global` 側を消しても K2 は解消しない（`ProfileActionProvider` 側に残る）。K2 は別途の判断のまま。
- `*Instance` のフォールバック群（Wave 3）: 静的初期化子（`new SpecialActionRepository(...)`、`new PathService()` など 5 件）が消えるため、`Global` の静的初期化の順序が変わる。K7-1（ホストの暗黙構築）には、`fallbackProfileRepository` が Step10-4-2b で遅延生成に変わった現在、直接の影響は無い見込み。ただし**実機で起動ログ（`Host initialized with runtime parser` が出ること）を確認する**。
- 計画書 §2 の代表例のうち、実際に不要なのは 7 件（`getUseExclusiveMode`、`getFlashWhenLate`、`getFlashWhenLateAt`、`getQuickCharge`、`IsUsingUDPServerSmoothing`、`containsCustomAction`、`containsCustomExtras`）だけ。`setXxx` 系 4 件と `UseAbsoluteEDID` は、そもそも存在しない。残りの約 23 件（`isUsingOSCServer` ほか）は、サービスの委譲先として生存している。

### 0A.3 `BackingStore` とその他の静的シム

- `BackingStore`（6,869 行、348 名）:
  - 防壁 1 のとおり、フィールド・配列・プロパティは削除しない。サービス（`ProfileSettingsService`、`AppSettingsService` ほか）が内部で直接使っているため、`Global` の不要分（上の 258 件）を消しても、新たに 0 件になるのは 14 名だけ（連鎖の計算結果）。
  - その 14 名の内訳: 定数 4（`DEFAULT_CHECK_EVERY_UNIT`、`DEFAULT_CHECK_EVERY_VALUE`、`DEFAULT_SA_OUTPUT_CURVE_ID`、`DEFAULT_STICK_OUTPUT_CURVE_ID`）→ 防壁 1・4 のため**温存**。メソッド 10: `GetDS4Action`、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`（`Global` の不要ラッパー経由でのみ到達）、`GetGyroOutModeString`、`GetGyroOutModeType`（private。内部でも未使用）、`SaveOld`、`PostProcessLoad`、`UsedSavedProfileString`、`RebuildProfileActionDicts`。
  - 判断: **要判断。推奨は温存して Phase7 へ**（`SaveOld`／`PostProcessLoad`／`RebuildProfileActionDicts` は、2025-11-11 の `576426b8`「3.9.9以前のprofile.xmlと互換性のある保存読込メソッドを正として残す」に絡む、旧形式の保存・読込の設計判断で、DI 移行の作業物ではない。ユーザーが別途に方針を決める項目）。
- `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`、互換ラッパー）: 呼び出し元は `Mapping.cs:86` の `AppHost.GetService<IVirtualKBM>()` の 1 件だけ（`Mapping.cs` は Phase7 の持ち越しなので、この呼び出しは変えない）。`Host`、`CreateHost`、`GetService(Type)`、`Dispose`、private フィールド `_loggedResolvedTypes` は、呼び出し元 0 件 → **削除可**（Wave 5）。
- `DS4WinWPF.AppHost.Dispose()`: 呼び出し元は互換ラッパーだけ（アプリ終了時にホストを破棄する呼び出しは、元から無い）。移行前からの状態のため**今回は触らない**（要判断・Phase7 の起動シーケンス整理で扱う）。
- `PathService.AppDataPath`／`IPathService.AppDataPath` の**セッター**（Step7 決定 3＝P1 で登録済みの削除候補）: 本体の呼び出し元は 0 件。テストは 1 件（`Phase6Step10GroupOneContractTests.cs:100`、セッターの挙動を確認）。**削除可**（Wave 5。該当テストも削除する）。コンストラクタ引数 `PathService(string appDataPath)` と `_customAppDataPath` は残す。
- `ServiceProviderHolder`、`Program.rootHub`: 呼び出し元がある（`rootHub` は 25 件）。Phase7 の対象で、今回は触らない。
- **契約側（DI インターフェース）の 0 件メンバー: 52 件**（`IProfileSettingsService` 22、`IDeviceStateService` 7、`IOutputSlotService` 5 ほか。名前の一致で数えた概算）。`Global` の不要分を消した後の件数で、§2.4 の「契約だけ作って切り替え未実施」の可能性があるため、**一括削除しない**。推奨は Step12 の範囲外とし、Phase7 の台帳へ登録（個別の経緯調査が必要）。

### 0A.4 テストへの影響

- 不要メンバーを参照するテストは 15 ファイル（`StandaloneTests` 1 を含む）。
  - 等価性テスト（旧シムと新サービスが同じ値を返すことの確認）: `ControlServicePr4ShimEquivalenceTests`（1）、`ControlServicePr5ShimEquivalenceTests`（11）。旧シム専用の部分は削除、生存するプロパティで読めるものは書き換える（例: `getFlashWhenLateAt()` → `FlashWhenLateAt`）。
  - `*Instance` を使うテスト: `DeviceStateServiceTests`、`EnvironmentServiceTests`、`OutputSlotServiceTests`、`PathServiceTests`、`ProfileRepositoryTests`、`SpecialActionRepositoryTests`、`ProfileSettingsViewModelTestFactory`、`ProfileSettingsViewModelTests`。コンストラクタで直接生成するか、`AppHost.GetService` に書き換える。
  - 契約テスト: `ProfileSettingsServiceTests`（16 名）、`Phase6Step3ContractExtensionTests`（`GetControlSettingsGroup`、`GetProfileActionIndexOf`）、`Phase6Step10GroupThreeContractTests`・`SpecialActionsListViewModelMissingActionTests`（`GetActions`）、`MappingGyroControlsHelperTests`（`IsUsingSAForControls`）、`AppSettingsServiceTests`（`Save`）。
  - `StandaloneTests/TestSpecialActionUtils`: `Util` と `Global` の `NormalizeActionName` の一致を確認するテスト。`Global` 側が消えるので、そのテストだけ削除（`Util` 側のテストは残す）。
- ラチェット（`Step10FourGlobalRatchetGuardTests`）の走査対象は `DS4Windows` 配下のみ。テストの書き換えでは基準値は動かない。

### 0A.5 計画書の失効箇所と改訂案

- 全体: 「`ScpUtil.cs`」は存在しない（Step10b W5 で `Global.cs` 3,472 行／`BackingStore.cs` 6,869 行／`Changelog.cs` に分割）。→ 対象を `DS4Windows/DS4Control/Global.cs` に改める。
- §0.1: 「実機は Flydigi Vader 4 Pro／DS4／DualSense」→ Vader 4 Pro は削除済み。実機は DualShock 4 v.2（Bluetooth）。確認不能の項目は `Phase6-Step11-RealDevice-Verification-Checklist.md` §11。件数は、ラチェット基準値（`Global.*` 780、`AppHost` 48、`Program.rootHub` 25）に合わせる。
- §1 防壁 1: `ScpUtil.cs` → `BackingStore.cs`。「全フィールド・配列・プロパティ」と書かれており、メソッドの扱いが未定義。→ §0A.3 のとおり、メソッドは要判断（推奨は温存）と明記する。
- §1 防壁 2: 「101 箇所」「`SetCurveAndDeadzone`（34）」ほか → 失効。Step3 は 2026-09-21 に方針を転換し、`Mapping.cs` の実参照 150 件を解消済み。現行の `Mapping.cs` は `Global.*` 53 件（うち定数 50）、`AppHost` 6、`rootHub` 1。防壁 2 の中身は「`Mapping` の静的 `profileSettings` 束縛（方針 P2）、温存 3 件、`SyntheticDispatcher`、第 1 層」（台帳の分類 G）に置き換える。
- §1 防壁 3: 「11 箇所」→ 12 箇所（Step7-0 の再集計）に統一。§3、§4、§6 の「11 件」も同様。
- §1 防壁 4・5: 内容は有効。`Clamp`、`getTransitionedColor` は生存。
- §2 代表例: 約 35 名のうち 7 名だけが 0 件、5 名は存在しない、残りは生存。→ 代表例の一覧を削除し、§0A.8 の名簿に置き換える。「不要となった旧イベントシム」は、DI の旧イベントシムではなく上流由来のイベント 4 種（§0A.2）。
- §2「Step7 からの登録」: `AppDataPath` のセッターは有効（テスト 1 件の削除を追記）。`ResetConnectionFlags`、`LoadActions`、`CreateStdActions` は温存のまま（有効）。
- §3:
  - Step12-0 は本節で実施済み（検証の記録は §0A.9）。
  - Step12-1 の成果物 `Phase6-Step12-Deletion-Candidates-Report.md` → 本節 §0A.2・§0A.8 が代わりになる（別ファイルは作らない）。
  - Step12-2 の「1 PR で全削除」→ Wave に分割（§0A.6）。
  - Step12-3 の「`DS4Windows.Actions.Tests`」→ `DS4WindowsTests`（csproj 名は `DS4Windows.Actions.Tests.csproj`）、582 件＋`StandaloneTests` 13 件。ビルド・テストはユーザーが実行する。
  - 型を消す（イベント引数 3 型）ときは、1 ファイル 1 型の規約（§3.3 原則 1）に沿って `git rm` で消す。ガードテストを新設するときも 1 ファイル 1 型。
- §4（最終引き継ぎ台帳）: 「`Mapping.cs` 参照分（101 箇所）」→ 上記の置き換え、「Pre-Host（11 箇所）」→ 12。`Program.rootHub` は「App 内 12 行、19 ファイル」→ 現行 25 件。新規に追加する行:
  - 生存する `Global` の実体（D 分類 144 名）: Phase7 でサービス側へ移管して消す。
  - `BackingStore` の旧保存系メソッド 10 件（要判断）。
  - DI インターフェースの 0 件メンバー 52 件（個別判断）。
  - `DS4WinWPF.AppHost.Dispose()` に呼び出し元が無いこと。
  - K7-1: `fallbackProfileRepository` は Step10-4-2b で遅延生成に変更済み（K7-1 の記述の更新）。
- §5 ロールバック: 「PR-2 の削除コミットを revert」→ Wave 単位でコミットを分け、Wave ごとに revert できるようにする。
- §6 チェックリスト: 「101 件」「11 件」の置き換え、テスト数（595）、`ScpUtil.cs` → `Global.cs`、Wave ごとのラチェット基準値の更新と新規ガードテストの成功を追加。

### 0A.6 削除の進め方（選択肢と推奨）

#### 決定 1: 削除の範囲

- 案 A（推奨）: `Global` の不要 256 件＋`DS4Windows.AppHost`（互換）の未使用 5 件＋`AppDataPath` のセッター。要判断 2 件は、`getDefaultX360ControlBinding` を削除、`DS4_STICK_AXIS_MIDPOINT` を温存。`BackingStore`・DI インターフェースは触らない。
- 案 B: `Global` の不要 256 件だけ（互換ラッパーとセッターは別 Step）。
- 案 C: 案 A に加え、`BackingStore` のメソッド 10 件と DI インターフェースの 0 件メンバーも削除（§2.4 の個別調査が必要で、旧保存形式の判断を含むため非推奨）。

#### 決定 2: 段階（1 回の範囲）

- 案 A（推奨）: 5 つの Wave（各 1 コミット。Wave ごとにユーザーがビルド・テストを実行し、結果を待ってから次へ）。
  - Wave 1（孤立コード 25 件＋イベント引数 3 型）: 宣言 25 件、非空行 約110。ラチェット `Global.cs` の `Global` 47→45。テストの書き換え: 等価性テスト 2 ファイルの数行、`TestSpecialActionUtils`。
  - Wave 2（ProfileSettings 系の互換アクセサ）: 宣言 150 件、非空行 359。`Global` 45→38。テストの書き換え: `ProfileSettingsServiceTests` ほか。
  - Wave 3（`*Instance` のフォールバック群）: 宣言 19 件、非空行 139。`Global` 38→26、`AppHost` 11→5、`rootHub` 9→6。テストの書き換え: 8 ファイル。**実機で起動ログを確認する**。
  - Wave 4（`BackingStore` 直転送ラッパー）: 宣言 62 件＋`getDefaultX360ControlBinding`、非空行 224。`AppHost` 5→4。テストの書き換え: 契約テストほか約 6 ファイル。
  - Wave 5（互換 `AppHost` の未使用 5 件、`AppDataPath` のセッター、ガードテスト、完了報告書）。互換ラッパーの `GetService<T>` は `Mapping.cs:86` が使うので残す。
  - 最終のラチェット基準値: `Global.cs` = (rootHub 6, AppHost 4, Global 26)。全域では `Global.*` 780→759、`AppHost` 48→41、`Program.rootHub` 25→22。
- 案 B: 1 回で全削除（コミット 1 つ）。差分が約 1,100 行と大きく、失敗したときの原因の切り分けが難しい。非推奨。
- 案 C: メンバー単位（約 250 回）。安全だが、ビルド・テストの回数が過大。非推奨。

#### 決定 3: ガードテストの追加

- 案 A（推奨）: 新規テスト `Step12DeadShimRemovalGuardTests`（1 ファイル 1 型）。(1) 削除した名前が `Global.cs` に再び現れないこと、(2) 防壁にあたる名前（`BackingStore` の主要メンバー、Pre-Host の `Global` メンバー、定数、ViGEm の 4 つ）が残っていること。ラチェットとは別に、Wave ごとに名簿を追記する。
- 案 B: ラチェットの基準値更新のみ（件数が増えなければ通るため、削除した名前の復活は検出できない）。

#### 決定 4: テストの扱い

- 案 A（推奨）: 旧シムを検証していたテストは、(a) 生存するプロパティ・サービスへ書き換えられるものは書き換え、(b) 「旧シムと新サービスの一致」そのものが目的のテストは削除する。削除したテストの一覧は、完了報告書に記録する。
- 案 B: テストをすべて残し、旧シムを `[Obsolete]` で温存（削除の目的に反するため非推奨）。

#### 決定 5: 契約側・BackingStore の扱い

- 推奨: `BackingStore` のメソッド 10 件（旧保存系を含む）と DI インターフェースの 0 件メンバー 52 件は、Step12 の範囲外とし、Phase7 の台帳へ登録（§4 の表へ追記）。`DS4WinWPF.AppHost.Dispose()` も同様。

#### 決定 6: 実機確認

- 推奨: Wave 3 の後に、起動して接続・プロファイル切替までを確認する（起動ログの `Host initialized with runtime parser` を含む）。ほかの Wave は、ビルド・テストの成功を条件とする（削除対象は呼び出し元 0 件で、実行経路に載っていない）。確認不能の項目は、チェックリスト §11 の台帳に追記する。

### 0A.7 承認をお願いしたい事項

- 決定 1〜6 の推奨案（A、A、A、A、Phase7 へ登録、Wave 3 後の実機確認）でよいか。
- 要判断 2 件（`DS4_STICK_AXIS_MIDPOINT` は温存、`getDefaultX360ControlBinding` は削除）でよいか。
- §0A.5 の改訂案を、承認後に §0〜§6 へ反映してよいか（承認までは §0A のみを追記し、本文は変更していない）。

### 0A.8 付録: 削除可メンバーの名簿（Wave 別。括弧はテストの参照件数）

- **Wave 1（孤立コード）**
  - メンバー: `m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`、`CheckForDevice`、`ControllerStatusChange`、`ControllerStatusChanged`、`BatteryStatusChange`、`OnBatteryStatusChange`、`ControllerRemoved`、`OnControllerRemoved`、`DeviceStatusChange`、`OnDeviceStatusChanged`、`getUseExclusiveMode`、`DS4Mapping`、`getQuickCharge`、`GetMinToTaskbar`、`DownloadLang`、`getFlashWhenLate`（テスト1）、`getFlashWhenLateAt`（テスト2）、`IsUsingUDPServerSmoothing`（テスト2）、`DebouncingMsHasChanged`、`NormalizeActionName`（テスト3）
  - 同時に `git rm`: `DS4Control/BatteryReportArgs.cs`、`DS4Control/ControllerRemovedArgs.cs`、`DS4Control/DeviceStatusChangeEventArgs.cs`
- **Wave 2（ProfileSettings 系の互換アクセサ）**
  - メンバー: `tempprofilename`（テスト1）、`touchpadActive`（テスト3）、`RightStickDriftXAxis`、`RightStickDriftYAxis`、`LeftStickDriftXAxis`、`LeftStickDriftYAxis`、`InverseRumbleMotors`、`ButtonMouseInfos`、`RumbleBoost`、`getRumbleBoost`（テスト1）、`setRumbleAutostopTime`、`getRumbleAutostopTime`、`EnableTouchToggle`、`getEnableTouchToggle`（テスト1）、`IdleDisconnectTimeout`、`EnableOutputDataToDS4`、`getEnableOutputDataToDS4`、`TouchSensitivity`、`LightbarSettingsInfo`、`DinputOnly`、`getDInputOnly`、`StartTouchpadOff`、`TouchOutMode`、`SATriggers`、`getSATriggers`、`SATriggerCond`、`getSATriggerCond`、`SetSaTriggerCond`、`GyroOutputMode`、`GetGyroOutMode`、`SAMousestickTriggers`、`GetSAMouseStickTriggers`、`SAMouseStickTriggerCond`、`GetSAMouseStickTriggerCond`、`SetSaMouseStickTriggerCond`、`GyroMouseStickTriggerTurns`、`GetGyroMouseStickTriggerTurns`、`GyroMouseStickHorizontalAxis`、`getGyroMouseStickHorizontalAxis`、`GyroMouseStickInf`、`GetGyroMouseStickInfo`、`GyroSwipeInf`、`GetGyroSwipeInfo`、`GyroMouseStickToggle`、`SetGyroMouseStickToggle`、`SASteeringWheelEmulationAxis`、`SASteeringWheelEmulationRange`、`GetSASteeringWheelEmulationRange`、`TouchDisInvertTriggers`、`GyroSensitivity`、`getGyroSensitivity`、`GyroSensVerticalScale`、`getGyroSensVerticalScale`、`GyroInvert`、`getGyroInvert`、`GyroTriggerTurns`、`getGyroTriggerTurns`、`GyroMouseHorizontalAxis`、`getGyroMouseHorizontalAxis`、`GyroMouseDeadZone`、`GetGyroMouseDeadZone`、`SetGyroMouseDeadZone`、`GyroMouseToggle`、`SetGyroMouseToggle`、`SetGyroControlsToggle`、`GyroMouseInfo`、`GyroControlsInf`、`GetGyroControlsInfo`、`WheelSmoothInfo`、`SAWheelFuzzValues`、`getLowColor`、`getChargingColor`、`getCustomColor`、`getUseCustomLed`、`getFlashColor`、`TapSensitivity`、`DoubleTap`、`getDoubleTap`、`ScrollSensitivity`、`getScrollSensitivity`、`LowerRCOn`、`TouchClickPassthru`、`TouchpadButtonMode`、`TouchpadJitterCompensation`、`TouchpadInvert`、`SXDeadzone`、`getSXDeadzone`、`SZDeadzone`、`getSZDeadzone`、`LSModInfo`、`RSModInfo`、`SXMaxzone`、`getSXMaxzone`、`SZMaxzone`、`getSZMaxzone`、`LSRotation`、`RSRotation`、`SXSens`、`getSXSens`、`SZSens`、`getSZSens`、`LSSens`、`RSSens`、`BTPollRate`、`getBTPollRate`、`DualSenseRumbleEmulationMode`、`UseGenericRumbleStrRescaleForDualSenses`、`DualSenseHapticPowerLevel`、`SquStickInfo`、`LSAntiSnapbackInfo`、`RSAntiSnapbackInfo`、`LSOutputSettings`、`RSOutputSettings`、`setLsOutCurveMode`、`getLsOutCurveMode`、`lsOutBezierCurveObj`、`setRsOutCurveMode`、`getRsOutCurveMode`、`rsOutBezierCurveObj`、`L2ModInfo`（テスト1）、`R2ModInfo`（テスト1）、`L2Sens`（テスト1）、`R2Sens`（テスト1）、`L2OutputSettings`（テスト1）、`R2OutputSettings`（テスト1）、`l2OutBezierCurveObj`（テスト1）、`r2OutBezierCurveObj`（テスト1）、`OutputVirtualTriggerButton`（テスト1）、`OutputDS4TriggerMode`（テスト1）、`setL2OutCurveMode`（テスト1）、`getL2OutCurveMode`（テスト1）、`setR2OutCurveMode`（テスト1）、`getR2OutCurveMode`（テスト1）、`setSXOutCurveMode`、`getSXOutCurveMode`、`sxOutBezierCurveObj`、`setSZOutCurveMode`、`getSZOutCurveMode`、`szOutBezierCurveObj`、`TrackballMode`、`getTrackballMode`、`TrackballFriction`、`getTrackballFriction`、`TouchMouseStickInf`、`TouchRelMouse`、`LaunchProgram`、`getDS4CSettings`、`GetDS4CSetting`
- **Wave 3（`*Instance` フォールバック群）**
  - メンバー: `profileRepository`、`fallbackProfileRepository`、`ProfileRepositoryInstance`（テスト6）、`specialActionRepository`、`fallbackSpecialActionRepository`、`SpecialActionRepositoryInstance`（テスト3）、`deviceStateService`、`fallbackDeviceStateService`、`DeviceStateServiceInstance`（テスト3）、`outputSlotService`、`fallbackOutputSlotService`、`CreateFallbackOutputSlotService`、`OutputSlotServiceInstance`（テスト6）、`pathService`、`fallbackPathService`、`PathServiceInstance`（テスト3）、`environmentService`、`fallbackEnvironmentService`、`EnvironmentServiceInstance`（テスト4）
- **Wave 4（`BackingStore` 直転送ラッパー）**
  - メンバー: `getTouchSensitivity`、`TouchActive`、`GetTouchActive`（テスト1）、`IsUsingTouchpadForControls`（テスト1）、`IsUsingSAForControls`（テスト1）、`getTouchDisInvertTriggers`、`getTapSensitivity`、`getTouchpadJitterCompensation`、`getTouchpadInvert`、`GetL2ModInfo`、`getL2Deadzone`、`GetR2ModInfo`、`getR2Deadzone`、`getLSDeadzone`、`getRSDeadzone`、`getLSAntiDeadzone`、`getRSAntiDeadzone`、`GetLSDeadInfo`、`GetRSDeadInfo`、`SXAntiDeadzone`、`getSXAntiDeadzone`、`SZAntiDeadzone`、`getSZAntiDeadzone`、`getLSMaxzone`、`getRSMaxzone`、`getL2AntiDeadzone`、`getR2AntiDeadzone`、`getL2Maxzone`、`getR2Maxzone`、`getLSRotation`、`getRSRotation`、`getL2Sens`、`getR2Sens`、`getLSSens`、`getRSSens`、`GetSquareStickInfo`、`GetLSAntiSnapbackInfo`、`GetRSAntiSnapbackInfo`、`GetTouchMouseStickInfo`、`GetOutputDS4TriggerMode`（テスト1）、`getProfileActionCount`（テスト1）、`CalculateProfileActionCount`、`UpdateDS4CSetting`、`UpdateDS4Extra`、`GetDS4Action`、`GetDS4KeyType`、`GetDS4Extra`、`GetDS4STrigger`、`GetControlSettingsGroup`（テスト1）、`HasCustomActions`、`HasCustomExtras`、`containsCustomAction`（テスト1）、`containsCustomExtras`（テスト1）、`GetActions`（テスト8）、`GetProfileActionIndexOf`（テスト2）、`CalculateProfileActionDicts`、`Save`（テスト1）、`SaveProfile`、`SaveAsProfile`、`getDefaultX360ControlBinding`
- **Wave 5**
  - `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`）の `Host`、`CreateHost`、`GetService(Type)`、`Dispose`、`_loggedResolvedTypes`。
  - `IPathService.AppDataPath`／`PathService.AppDataPath` のセッター。

### 0A.9 Step12-0 の前提検証の記録

- Step11 の判定: GO（`Phase6-Step11-Completion-Report.md` §5、2026-10-17）。
- HEAD: `e0bc8258`、作業ツリーはクリーン（走査の開始時点）。ベースラインのテスト結果は、Step11 の記録（`DS4WindowsTests` 582、`StandaloneTests` 13、全件成功、ビルド警告 0）を採用する。Step12-0 では `dotnet` を実行していない。
- 本節の追記と、走査スクリプト（`scripts/audit/Step12/`）の追加以外に、コードの変更は無い。

### 0A.10 承認された決定（2026-10-17）

- 決定 1＝案 A、決定 2＝案 A（5 Wave）、決定 3＝案 A（`Step12DeadShimRemovalGuardTests`）、決定 4＝案 A、決定 5＝Phase7 へ登録、決定 6＝Wave 3 の後に実機確認。
- 要判断: `DS4_STICK_AXIS_MIDPOINT` は温存、`getDefaultX360ControlBinding` は削除（Wave 4）。
- §0A.5 の改訂案を §0〜§6 へ反映済み（2026-10-17）。Wave 1 に着手。

---

### 0A.11 Wave 1 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 削除（`Global.cs`）: 24 件 — `m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`、`DS4Mapping`、`DownloadLang`、`CheckForDevice`、イベント 4 種とその発火メソッド 4 件（`ControllerStatusChange`／`ControllerStatusChanged`、`BatteryStatusChange`／`OnBatteryStatusChange`、`ControllerRemoved`／`OnControllerRemoved`、`DeviceStatusChange`／`OnDeviceStatusChanged`）、`getUseExclusiveMode`、`getQuickCharge`、`GetMinToTaskbar`、`getFlashWhenLate`、`getFlashWhenLateAt`、`IsUsingUDPServerSmoothing`、`NormalizeActionName`（付随する孤立コメント 1 行も削除）。
- `git rm`: `DS4Control/BatteryReportArgs.cs`、`ControllerRemovedArgs.cs`、`DeviceStatusChangeEventArgs.cs`。
- **Wave 1 から外した 1 件（§0A.6 の予定からの変更）: `DebouncingMsHasChanged`（`Global.DebouncingMsChanged` の発火メソッド）。**
  - 理由: `DS4Device.SetupDebouncer`（`DS4Device.cs:1442`）が `Global.DebouncingMsChanged` を購読しており、削除するとこの購読が永久に発火しない状態を固定してしまうため。
  - 経緯（`git log -S`）: 発火元は上流 `2b357940`（2024-08-26）で追加され、プロファイル編集画面の保存処理が呼んでいた。2026-09-14 の `9da60b85`（ProfileEditor 保存処理の統合）で呼び出しが `Global.ApplyProfile(...)`（ホットリロード）に置き換わり、現在の呼び出し元は 0 件。
  - 懸念: 接続中のコントローラーでデバウンス時間（`DebouncingMs`）を変更して保存したとき、`Debouncer` の持続時間が即時に更新されない可能性がある（再接続で反映）。`ApplyProfile` 側でデバウンサーが更新されるかは未確認。**回帰かどうかの確認が必要**（Phase6 より前の変更）。修正の前にユーザーの承認を得る。確認不能なら §11 の台帳に追記する。
  - 扱い: 温存（Wave 4 の後に、確認の結果を見て削除または修正を判断する）。`DebouncingMsHasChanged` を含む形で名簿 §0A.8 の Wave 1 は 25 件→24 件。
- テストの書き換え: `ControlServicePr4ShimEquivalenceTests`（旧 getter との比較 1 行を削除）、`ControlServicePr5ShimEquivalenceTests`（`IsUsingUDPServerSmoothing()` → `UseUDPSeverSmoothing`、`getFlashWhenLateAt()` → `FlashWhenLateAt`）、`StandaloneTests/TestSpecialActionUtils`（`Util` と `Global` の一致テストを、`Util` の期待値の固定テスト `Util_NormalizeActionName_FixedExpectations` に置換）。削除したテストケースは無し（観点は維持）。
- ラチェット: `Global.cs` = (rootHub 9, AppHost 11, Global **45**)。全域は `Global.*` 780→778。`Global.cs` の件数は走査で確認済み（45）。
- 新規ガード: `DS4WindowsTests/Step12DeadShimRemovalGuardTests.cs`（削除済み名の再宣言の禁止、削除済みファイルの不在、防壁の 18 名の存在）。
- ユーザーに依頼: ビルド（警告 0）と `dotnet test`（`DS4WindowsTests` 582＋新規 3 件、`StandaloneTests` 13）。結果を確認後、Wave 2 へ。

---

## 0. 背景と方針（2026-10-17 改訂。承認済みの決定は §0A.10）

### 0.1 Step2〜Step11 の完了実績と残存コードの状況
- Phase6 の Step2〜Step10・Step10b・Step11 で、アプリ全域の `Global` 直接参照をコンストラクタ経由の Pure DI サービス呼び出しへ置き換えた。全域の残存は、ラチェット基準値で `Global.*` 780、`AppHost` 48、`Program.rootHub` 25（分類は `Phase6-Step11-Global-Residual-Ledger.md`）。
- Step11 の総合検証は判定 GO（2026-10-17）。自動テストは `DS4WindowsTests` 582 件＋`StandaloneTests` 13 件が成功、ビルド警告 0。実機は DualShock 4 v.2（Bluetooth）で検証した（Vader 4 Pro は削除済み）。確認不能の項目は `Phase6-Step11-RealDevice-Verification-Checklist.md` §11。
- 静的クラス `Global`（`DS4Windows/DS4Control/Global.cs`、3,472 行。旧 `ScpUtil.cs` は Step10b W5 で `Global.cs`／`BackingStore.cs`／`Changelog.cs` に分割済み）に、呼び出し元が 0 件になった旧ラッパー・旧プロパティが残っている。

### 0.2 方針: 安全防壁付きの物理削除
- Phase6 の成果として、呼び出し元 0 件の旧ラッパーを物理削除し、`Global.cs` をスリム化する。ただし §1 の 5 つの保護防壁に属するものは削除しない。
- 削除は §0A.2 の台帳（削除可 256 件）に基づき、Wave 単位（§3）で行う。

---

## 1. 5大保護防壁（物理削除の絶対禁止リスト）

1. **防壁1: `BackingStore`（`DS4Control/BackingStore.cs`）の全フィールド・配列・プロパティ**: 各 DI サービスが内部で直接参照する正本（SSOT）。Phase7 まで温存。メソッドは要判断（推奨は温存して Phase7 へ。§0A.3、決定5）。
2. **防壁2: Phase7 持ち越し（分類 G）**: `Mapping.cs`（`Global.*` 53 件［うち定数 50］、`AppHost` 6、`rootHub` 1）、その静的 `profileSettings` 束縛（方針 P2、TODO 付き）と温存 3 件、`SyntheticDispatcher`（`outputKBMHandler`／`outputKBMMapping`）、第 1 層（`DS4Device`／`DS4Devices`／`DS4SixAxis`）、`DS4LightBar`。Phase7 の `Mapping` インスタンス化まで温存。
3. **防壁3: Pre-Host ブートストラップ**: `App.xaml.cs` の DI コンテナ構築前の 12 箇所（Step7-0 の再集計）、共用ヘルパー `ApplyLanguageSetting` の 2 件、`AttemptSave` の `appdatapath = null`（TODO 付き）。静的呼び出しのまま温存。
4. **防壁4: 真の定数と純粋計算**: `MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_INDEX`、`ASSEMBLY_RESOURCE_PREFIX`、`RESOURCES_PREFIX` ほかの定数、`Clamp`、`getTransitionedColor` ほか。`DS4_STICK_AXIS_MIDPOINT` は呼び出し元 0 だが、定数のため温存（決定）。
5. **防壁5: ViGEm クライアント依存**: `RefreshViGEmBusInfo`、`IsRunningSupportedViGEmBus`、`vigembusVersion`、`vigemInstalled`。仮想バス刷新まで温存。

---

## 2. 物理削除の対象

- 対象の正本は §0A.2（分類）と §0A.8（Wave 別の名簿）。2026-09-18 版にあった「代表例」の一覧は、現行コードと食い違っていたため廃止した（実際に 0 件だったのは 7 件、存在しないものが 5 件、残りは薄いシムの委譲先として生存）。
- **Step7 からの登録（有効）**: `IPathService.AppDataPath` と `PathService.AppDataPath` のセッター（`{ get; set; }` → `{ get; }`）。理由: private フィールド `_customAppDataPath` に保存するだけで `Global.appdatapath` を変えない孤立した実装（§2.4 で「使うべきでないもの」）。本体の呼び出し元は 0 件。テストは 1 件（`Phase6Step10GroupOneContractTests.cs:100`）で、同時に削除する。`PathService(string appDataPath)` と `_customAppDataPath` は残す。Wave 5 で実施。
- **削除対象外（有効）**: `Global.ResetConnectionFlags()`、`Global.LoadActions()`、`Global.CreateStdActions()` は、DI サービスの委譲先として使われ続ける。
- **削除対象外（今回決定）**: `BackingStore` のメソッド 10 件、DI インターフェースの 0 件メンバー 52 件、`DS4WinWPF.AppHost.Dispose()` は Phase7 の台帳へ登録（決定5）。

---

## 3. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行し、結果を待って次へ進む）

- **Step12-0**: 完了（§0A、§0A.9）。
- **Wave 1**: 孤立コード 24 件＋イベント引数 3 型（`git rm`）。実施済み（§0A.11。`DebouncingMsHasChanged` は回帰確認のため温存）。テストの書き換えを伴う。ラチェット `Global.cs` の `Global` 47→45。
- **Wave 2**: ProfileSettings 系の互換アクセサ 150 件。`Global` 45→38。
- **Wave 3**: `*Instance` のフォールバック群 19 件。`Global` 38→26、`AppHost` 11→5、`rootHub` 9→6。**この Wave の後に実機で起動・接続・プロファイル切替を確認**（起動ログ `Host initialized with runtime parser`）。
- **Wave 4**: `BackingStore` 直転送ラッパー 62 件＋`getDefaultX360ControlBinding`。`AppHost` 5→4。
- **Wave 5**: 互換 `AppHost`（`DI/Compat/AppHost.cs`）の未使用 5 件、`AppDataPath` のセッター、ガードテストの最終確認、完了報告書（`Phase6-Step12-Completion-Report.md`、`Phase6-Completion-Report.md`）。
- **各 Wave の共通作業**:
  - 対象の削除とテストの書き換え（旧シム専用の等価性テストは削除、書き換え可能なものは生存プロパティ・サービスへ）。削除したテストは一覧にして完了報告書へ記録する。
  - `Step10FourGlobalRatchetGuardTests` の `Global.cs` の基準値を更新する。
  - `Step12DeadShimRemovalGuardTests`（1 ファイル 1 型）に、削除した名前を追記する（再混入の禁止）。防壁の名前の存在確認も含める。
  - 検証: ユーザーが `dotnet build` と `dotnet test` を実行（警告 0、全件成功）。
- **最終のラチェット基準値**: `Global.cs` = (rootHub 6, AppHost 4, Global 26)。全域では `Global.*` 780→759、`AppHost` 48→41、`Program.rootHub` 25→22。

---

## 4. Phase7 への最終引き継ぎ台帳

Step12 完了後に残るもの（Phase7 で解体）:

| 残存コンポーネント | 残存理由 | Phase7 での扱い |
|---|---|---|
| `BackingStore` の全フィールド・配列・プロパティ | DI サービスの正本データ | ドメインエンティティへ移譲して消去 |
| `BackingStore` の旧保存系メソッド 10 件（`SaveOld`、`PostProcessLoad`、`UsedSavedProfileString`、`RebuildProfileActionDicts`、`GetDS4Action` ほか） | 3.9.9 互換の保存・読込の設計判断（`576426b8`）に絡む。要判断 | 保存方式の方針決定とあわせて個別に判断 |
| `Global` の生存する実体（分類 D の 144 名） | サービス実装が `Global` の実体へ委譲する薄いシム（§2.1） | 実体をサービス側へ移管して消す |
| 分類 G（`Mapping` の静的 `profileSettings` 束縛 P2、温存 3 件、`SyntheticDispatcher`、第 1 層） | ホットパス。`Mapping` の instance 化が前提 | `Mapping` の instance 化で解消 |
| Pre-Host 12 箇所 ＋ 共用ヘルパー 2 件 ＋ `AttemptSave` の `appdatapath = null` | DI コンテナ構築前の起動 | 起動シーケンスの近代化で整理 |
| `Global.outDevTypeTemp`／`activeOutDevType`（実行時状態の静的配列） | `OutputSlotService` が裏づけとして返す（Step5 の案X） | 所有を `OutputSlotService` へ移す（案Y。`Phase6-Step5-Plan.md` §9） |
| K7-1: DI ホストの暗黙構築 | `Global` の静的初期化がホストを作りうる。`fallbackProfileRepository` は Step10-4-2b で遅延生成に変更済み。起動引数は `IStartupArguments` で回避済み | `Global` の静的初期化の整理とあわせて解消 |
| `Program.rootHub`（Step12 後 22 件）と `App.xaml.cs` の Service Locator | 共有の入口。`App` が所有者 | 各利用側をコンストラクタ注入へ切り替え、すべてなくなった時点で削除 |
| `DS4WinWPF.AppHost.Dispose()` | 呼び出し元が互換ラッパーだけ。アプリ終了時のホスト破棄が元から無い | 起動・終了シーケンスの整理で扱う |
| DI インターフェースの 0 件メンバー 52 件 | §2.4 の個別の経緯調査が必要 | 個別に判断 |
| K2: `GetProfileActionIndexOf` の未検出時の戻り値 | `ProfileActionProvider` 側に残る | 別途判断 |
| 真の定数（`const`、`DS4_STICK_AXIS_MIDPOINT` を含む） | 共通定数値 | ドメインごとの定数クラスへ再配置 |

---

## 5. ロールバック方針

1. Wave ごとにコミットを分け、問題が出た Wave のコミットだけを `git revert` する（直前の Wave の状態へ復帰）。
2. 原因の単離: スタックトレースやビルドエラーから、動的参照されていたメンバーを特定し、保護リストに追加したうえで §0A.8 の名簿を更新する。

---

## 6. 完了判定チェックリスト（Phase6 全体の完了ゲート）

- [ ] 5 つの保護防壁（`BackingStore` の正本、Phase7 持ち越し、Pre-Host 12 箇所、定数・純粋計算、ViGEm 依存）が保護されている。`Step12DeadShimRemovalGuardTests` の存在確認が成功している。
- [ ] 削除した全メンバーが、削除前の走査で呼び出し元 0 件だった（§0A.8 の名簿と一致）。
- [ ] `Global.cs` から旧ラッパーが削除され、ラチェットの基準値が `Global.cs` = (6, 4, 26)、全域 759／41／22 になっている。
- [ ] `dotnet build` が警告 0・エラー 0。
- [ ] `DS4WindowsTests` と `StandaloneTests` の全件が成功している（テスト数の増減は完了報告書に記録）。
- [ ] Wave 3 の後の実機確認（起動・接続・プロファイル切替）が完了している。
- [ ] §4 の引き継ぎ台帳が完全に記録されている。
- [ ] `Phase6-Step12-Completion-Report.md` と `Phase6-Completion-Report.md` が作成されている。
