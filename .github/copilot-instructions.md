# エージェント向け作業ルール (copilot-instructions)

作成日: 2026-03-01
最終更新日: 2026-11-10（Phase16 完了: 3.4 R1・R2 の第 1 層・第 3 層の構成［Input/Parsers、IHidTransport、IInputReportParser、IOutputDeviceCartridge、OutContTypeInfo］は、すでに記載の規則どおり。ガードの追加はテスト側。同日: 3.1 にコンストラクターの引数の努力目標を追記［Phase16-Step0］。2026-11-09（3.1 に設定の領域の窓口［ForSlot］を追記［Phase15-6Z］。2026-11-08（2.3 に利用者向けログの書き方［AppLogger.LogUser とリソースのキー］、4 章のテストの命名に Phase 名のテストの改名とテストの状態の初期化を追記［Phase15-0］。2026-11-05（3.1 に RemovedApiRuleTests への置き換え、3.3 原則1 の引き継ぎの注記、4 章に Benchmark の印と「OS の状態に触れない」を追記［Phase14d］。同日: 3.1 に DI の登録の層ごとの分割と後付けの配線の置き場所、3.3 に原則5［コメントには意図を書く。R-15］、3.4 R1・R2 に `Core/Model`、4 章にテストの命名［R-16］と Slow の印を追記［Phase14c-Step7］。同日: 3.4 R1 のルート直下の記述を更新、3.1 に違反 0 件を追記［Phase14b-2］。同日: 3.1 に層の依存の検査［LayerDependencyRuleTests］、3.4 R1 に Input・UI の Abstractions、R2 に Core・第1層が使う契約と共有の値の型の置き場所を追記［Phase14b］。2026-11-04: 3.3 原則3 を達成済みに、3.4 を層別フォルダの規則 R1〜R7 へ全面改定［Phase14］。2026-11-03: 2.3 ログ出力の維持に、Phase13b での方針に沿った変更の例外を追記［Phase12-Step1 の承認済み］。2026-10-27: 調査指示の追記を反映。2026-10-22: 3.2 の巨大ファイルの行数を Phase9 の責務分割［9-B・9-C・9-E・MapCustom の M1〜M3］の後に更新。2026-10-21: 3.2 の Mapping.cs の行数を Phase8-Fix-MacroRepeatUnify の削除後に更新。2026-10-20: 3.2 の Mapping.cs の行数を Phase8-Step8 の削減後に更新。2026-10-18: 2.1 のシム維持の原則を過渡期の記述に改め、3.1 に暗黙のホスト構築の禁止とラチェットの一致検査を追記［Phase7-Step10e の承認済み］。2026-10-15: 3.3 原則1の過渡期ルールを終了し、既存ファイルを含む厳格適用へ更新［Phase6-Step10b の完了］。2026-10-14: 2.2 のカスケードループ防止ガードの記述を現行の機構へ更新［Phase6-Step9b の決定6］。2026-09-24: 2.4 未参照コードの扱いを追記。2026-09-20: 3.3 原則1に過渡期ルールを追記、3.4 ファイル配置ルールを追記。2026-09-03: 3.3 ファイル構成・クラス・名前空間の原則と過渡期ルールを追記）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
モデル図（2026-11-10 Phase17-0 で整理）：`docs-forDIMG\Model-Diagram\01-Structure.md`（現状の構造。実在する型の表は `ModelDiagramGuardTests` が検査）,`docs-forDIMG\Model-Diagram\02-Extension-Guide.md`（機種・出力の種類を足す手順）,`docs-forDIMG\Model-Diagram\03-Lifecycle-and-DI.md`（登録と寿命）,`docs-forDIMG\Model-Diagram\Ideas.md`（未実装の構想）← 全体の構造に関わる部分の意思決定を行う場合は必ず参照し、実装作業の中で変更があった場合はその内容を反映すること。旧 4 文書は `docs-forDIMG\Model-Diagram\Archive` に保管（読むだけ）

このリポジトリで作業を行うすべての AI エージェント（Copilot, Cursor, 外部エージェント等）は、以下のルールを厳格に順守すること。

---

## 1. アーキテクチャ原則

- 本リポジトリの移行目標は `docs-forDIMG/DI-App-Wide-Migration-Plan.md` に定義された**全体4層モデル**に従う。
  - **第1層**: コントローラー固有・入力読み取り層 (`InputDeviceCore`, `InputDevices/*`)
  - **第2層**: 入力処理・マッピング層 (`Mapping`, `SyntheticDispatcher`)
  - **第3層**: 仮想出力層 (`DS4OutDevice`, `Xbox360OutDevice`, `OutputSlotManager`)
  - **第4層**: アプリケーション・UI・サービス層 (4-a UI, 4-b Composition Root, 4-c サービス群)
- 下位層から上位層への直接参照は禁止。依存方向は常に「上位 → 下位」またはインターフェース経由とする。
- **依存の向き（2026-10-19 明確化。依存図 `docs-forDIMG/Model-Diagram/01`・`02` と一致）**: 第2層が核であり、依存は **第4層 → 第2層 → { 第1層、第3層、横断基盤・永続化層 }** の向きに限る。層の番号の大小で向きを決めない（第2層が第3層を使うのは「下から上」ではなく、正しい向き）。第1層・第3層・横断基盤から第2層（および第4層）への参照は禁止で、第2層が第1層・第3層を使うときはインターフェース（ポート）経由とする。第4層の UI は第2層だけに依存し、第1層・第3層を直接参照しない。Composition Root（4-b）は、組み立てのために全層の具象を知ってよい。「アプリ内部の状態遷移」（例: プロファイルの切替 `IProfileSwitcher`）は第2層に置き、第3層（外部への副作用）に置かない。

---

## 2. 品質・安全基準

### 2.1 フォールバック実装・シム維持の原則
- 古い静的経路（`Global.xxx`, `Program.rootHub.xxx` 等）は、新しい DI サービスへの移行が完了し動作確認が取れるまで即座に削除しない。
- 既存の外部呼び出し元との互換性を保つため、古い静的メソッドは新しい DI サービスを呼び出す「薄いシム（委譲ラッパー）」として温存する。
- **適用状況（2026-10-18 更新）**: 上記は移行の過渡期の原則である。Phase7 で `Global`・`Program.rootHub` は削除済みで（残るのはログ文言の文字列 2 件のみ）、静的なシムも `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` を最後に残っていない。今後は、新しい静的な窓口やシムを新設しない（必要な値はコンストラクターで受ける）。残る静的な状態は `Phase7-Residual-Ledger.md` §17.3 の目録に従う（起動前に必要なもの以外は Phase8）。

### 2.2 現在の機能の完全維持 (No Feature Drop)
- DI移行作業そのものによる機能削減、設定値欠落、通知抑制不全を絶対に発生させない。
- カスケードループ防止ガード（プロファイル適用直後の押しっぱなしによる連鎖発火を防ぐ Fresh-press ガード［`ActionInstanceState.RequiresFreshPressAfterReset`。トリガーが `FreshPressReleaseHoldMs`＝80ms 連続して未成立になるまで新規成立とみなさない］と、実行中の再実行防止［`IsExecuting`］。旧 250ms デバウンスは 2026-09-16 に後者へ置き換え済み）、一時プロファイル復帰スタック、排他制御など、安全弁となるロジックは 100% 維持する。
- **例外（機能廃止）**: ユーザーが明示的に承認した機能廃止（別途Issue化されたもの）はこの原則の対象外とする。
  - エージェントが自らの判断のみで機能を削除することは禁止する。
  - ただし、実装作業中に使用頻度が極めて低いと思われる機能を発見し、それを削除することで構造の単純化などの明確なメリットが見込める場合、削除を**ユーザーに提案する**ことは可とする。実施は必ずユーザーの明示的な承認を得てから行う。

### 2.3 ログ出力の厳格な維持
- 通常の Phase では、既存の `AppLogger.LogToGui`、`AppLogger.LogTrace`、`AppLogger.LogDebug` 等のログ出力とログレベル（文言を含む）を完全に維持する。
- **例外（2026-11-03 追記。Phase12-Step1）**: ログ整理の Phase（Phase13b＝L2）に限り、`docs-forDIMG/MadeByAgent/Log-Policy.md` に沿った変更を許可する。変更は「レベルのみ／文言／削除」に分けて記録し、利用者に見える GUI・トレイのログの変更は一覧で承認を得る（Phase8-Step8-Q7 の前例）。
- 新規に書くコードは、最初から `Log-Policy.md` に沿って書く。利用者向けの文言（画面・トレイに出るもの）は、文字列を直接渡さず、`AppLogger.LogUser(level, "Log_…", 引数…)`（画面は表示言語、ファイルは英語）と `Strings.resx`／`Strings.ja.resx` のキーで書く（2026-11-08 追記。Phase15-0）。開発者向けの診断文（例外の全文など）は英語のままでよい。
- DI 境界を通過する重要操作には、標準化ログ接頭辞 `[DI]` を付与して追跡性を高める（維持）。

### 2.4 未参照コード（呼出元0件のクラス・メソッド等）の扱い
呼出元が0件のクラス・メソッド・プロパティ・インターフェースメンバーを見つけた場合、「使われていないから削除」と即断しない。次の手順で判断する。

1. **経緯の調査**: `git log -S`（追加したコミットの特定）と、当時の計画書・完了報告書（`docs-forDIMG/`）を確認し、何のために作られたかを明らかにする。特に、DI 化の過程で「サービス側の契約だけ作り、呼び出し側の切り替えが未実施」になっていないかを確認する。
2. **判断基準**: 本プロジェクトの目的（Pure DI 化と、全体4層モデルの依存方向の整理）に照らして、**本来その API を使うべきか**を判断する。
   - **使うべきもの**（例: 上位層が具体クラスを直接呼んでいる箇所を、抽象経由に置き換えるための契約）: 削除しない。`[Obsolete]` も付けない。
   - **使うべきでないもの**（例: 誤った値を返すだけの孤立実装、正規の経路と重複するだけの並行実装、廃止済み機能の残骸）: 削除してよい。ただし 2.2 の機能廃止の例外に当たる場合は、ユーザーの承認を得る。
3. **使うべきものと判断した場合の対応**:
   - 当該コードに、3.3 原則4 に従う TODO コメントを付ける（作られた経緯、未接続である理由、接続予定の Step、現状の呼び出し側との差分）。
   - 実際に使われる状態にする作業（呼び出し側の切り替え、差分の是正）を、適切な Step の計画書へ追加するか、新しい Step の計画書を作成する。現在の Step の範囲に混ぜない。
4. **判断の記録**: どちらの判断をしたかと根拠を、現在の Step の計画書（決定事項）と `Phase6-Status.md` 等の進捗文書に記録し、ユーザーの承認を得てから実装する。
- 前例: Phase6-Step5 の孤立配列 `_deviceTypes` と `GetOutputDeviceType`／`SetOutputDeviceType` は「使うべきでない」と判断して削除。Phase6-Step6 の `IOutputSlotService.PluginSlot`／`UnplugSlot` は「Phase5-Step12 で UI 接続のために作られたが未接続」と判明したため温存し、接続作業を `Phase6-Step10-Plan.md` §8 に追加した。

---

## 3. 技術標準

### 3.1 DI (Dependency Injection) の実装
- DI コンテナへの登録は `DS4Windows/Composition/ServiceRegistration.cs` に集約する（Phase14 で `DI/` から移動）。
  - **層ごとの分割**（2026-11-05 追記。Phase14c-Step6）: 登録は `partial` で、実装クラスの置き場所（層のフォルダ）ごとのファイルに分ける（`ServiceRegistration.Core.cs`／`.Processing.cs`／`.Output.cs`／`.Ui.cs`）。新しい登録は、実装クラスの層のファイルへ足す。同じサービス型を複数登録する `IInputDeviceModule` は、解決すると登録順の列になるため、並びを変えない。
  - **後付けの配線**（生成のときに、別のオブジェクトへ自分や自分の持ち物を結ぶ処理。例: `BackingStore.AttachProfileIdentity`）は、`ServiceRegistration.Wiring.cs` の `AddWiredServices` だけに置き、冒頭の一覧を更新する。新しい依存は、まずコンストラクター注入で表せないかを考える（後付けの配線は、ブートストラップの `BackingStore` や循環など、コンストラクターで渡せない理由があるときだけ）。
- **設定の領域の窓口**（2026-11-09 追記。Phase15-6Z）: プロファイル編集画面の ViewModel は、`IProfileSettingsService` のスロット番号の配列を直接触らず、`ForSlot(slot)` が返す領域の窓口（`Core/Settings` の `GyroSlotSettings` など）だけを受け取る。新しい設定の項目は、まず該当する窓口へ足す。`SettingsSlotWindowUsageTests` が検査する。
- インターフェースには `I` プレフィックスを付ける。
- **Pure DI（純粋コンストラクタ注入）の堅持**: ViewModel やサービスに `AppHost.Services`（Service Locator）を持ち込ませず、コンストラクタで明示的に依存を受け取る。
- **暗黙のホスト構築の禁止**（2026-10-18 追記）: `AppHost.GetService` は、ホストが未構築（または Dispose 済み）のとき `null` を返し、暗黙にホストを作らない。ホストは `App` の起動（`CreateHost(config, parser)`）だけが作る。テストは `TestHost` 経由で共有のホストを使う。
- **ラチェットの一致検査**（2026-10-18 追記）: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`Program.rootHub`）と `Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）の基準値は、「超えない」だけでなく「実数と一致する」ことを `Phase7Step10eW5RatchetEqualityGuardTests` が検査する。件数を減らしたら、基準値も同時に更新すること。
  - **置き換え**（2026-11-05 追記。Phase14d-Step2）: 上の 3 つのテストは撤去した（`Global`・`Program`・`ServiceProviderHolder` の型はすでになく、基準値はほぼ 0 だった）。代わりに `RemovedApiRuleTests` が、撤去した型・メンバーがないことと、`AppHost` を使うのが `App` と `DS4Windows.Composition` だけであることを、型の検査で確かめる。型やメンバーを撤去したときは、同テストの表へ 1 行足す。基準値を実数と一致させる考え方は、今あるラチェット（`LayerDependencyBaseline`、`Phase10Step4dW0RuntimeKeyRatchetTests`）で引き続き守る。
- **層の依存の検査**（2026-11-05 追記。Phase14b）: `LayerDependencyRuleTests`（ArchUnitNET。メソッド本体を含む実際の型参照）が、1 章の依存の向きを規則 L1〜L7 で検査する。今ある違反は `LayerDependencyBaseline` に凍結し、実数との一致を検査する（多くても少なくても失敗）。新しい違反は許可リストへ足さずに是正する（2026-11-05 Phase14b で、禁止の規則の違反は 0 件になった。許可リストは空のまま保つ）。是正で減ったら同じコミットで許可リストも減らす（環境変数 `DS4W_LAYER_BASELINE_OUT` に書き出し先のパスを入れてテストを実行すると、実測から作り直せる）。

- **第 1 層・第 3 層の差し替え口**（2026-11-10 追記。Phase16）: 入力機種の通信は `IHidTransport`、バイト列の解析は `IInputReportParser`（`Input/Parsers`。状態を持たず、新機種は旧実装との差分テストで一致を確かめる）、仮想パッドの出力の種類は `IOutputDeviceCartridge`（`Output/Devices`）で足す。出力の種類の保存キー・選択肢の番号は `OutContTypeInfo` に集め、種類ごとの `if`／`switch` を増やさない。ライトバーの色の計算は `ILightbarService`（時間は `TimeProvider`）。ViGEm の型は `Output/Backend` の中だけ、`HidDevice` の具象は `DS4Windows.Input.Hid` の中だけ（`LayerDependencyRuleTests` の L9・L10）。
- **入力機種の追加と設定の定義**（2026-11-10 追記。Phase17）: 入力機種の追加は、機種の種類（`InputDeviceType`）に `[InputDeviceFamily]` を付け、カートリッジ＋モジュールを `AddInputDevice<TCartridge, TModule>()` の 1 行で登録する。単純な機種は `IDeviceProtocol` ＋ `ProtocolDevice` で足せる。機種ごとの設定は `ControllerSetting`（`Bool`／`Enum`。`WithUi` で画面の見せ方）の並びで宣言し、保存・読み込み・設定画面・はじめの起動画面はその定義から作る（機種ごとの DTO・Wrapper・画面の `switch` を増やさない）。手順は `docs-forDIMG/Model-Diagram/02-Extension-Guide.md`。
- **コンストラクターの引数の努力目標**（2026-11-10 追記。Phase16-Step0）: 新規・変更するクラスのコンストラクターの引数は 15 以下を努力目標とする。超えるときは、理由をクラスのコメントに書く。値の入れ物・工場・View（Window）も数は同じ基準で測るが、責務の分割ではなく、生成物側の整理に追随して減らす。3 つ以上の bool を引数に持つ型は、呼び出し側で名前付き引数を使う。`ConstructorArityRuleTests` が、15 を超える型を `ConstructorArityBaseline` に凍結し、実数との一致を検査する（環境変数 `DS4W_ARITY_BASELINE_OUT` に書き出し先のパスを入れてテストを実行すると、実測から作り直せる）。減らしたら同じコミットで基準も更新する。引数の数だけを指標にせず、束ねるだけで依存が見えなくなる変更（何でも箱）はしない。

### 3.2 巨大ファイルの編集方針
- `Mapping.cs`（約 1,100 行。Phase8-Step8 で 7,322 行から削減、Phase8-Fix-MacroRepeatUnify で死んだマクロ経路を削除、Phase9 の 9-B・9-C・MapCustom の M1〜M3 で約 4,400 行から分割）、`ControlService.cs`（約 1,075 行。Phase9 の 9-E E-1〜E-5 で約 3,400 行から分割）、`BackingStore.cs`（約 1,500 行）などの巨大ファイルは、全体を再生成せず、対象メソッドのみをピンポイントで置換する（Strangler Fig パターンの徹底）。

### 3.3 ファイル構成・クラス設計・名前空間の3原則と過渡期ルール
近代的な C# 設計として以下の「3大原則」を最終目標とするが、大規模移行の安全性を最優先するため過渡期ルールを設ける。

1. **原則1: 1ファイル ＝ 1型（クラス／インターフェース）**【厳格順守】:
   - 新規作成するファイルには複数の型（クラス／インターフェース／構造体／列挙型／デリゲート）を同居させず、単一責任で分割する。
   - **適用状況（2026-10-15 更新）**: Phase6-Step10b（`docs-forDIMG/MadeByAgent/Phase6-Step10b-Plan.md`）で、既存の複数型ファイル 55 件をすべて分割した（`DS4Windows`・`DS4WindowsTests`・`StandaloneTests` の全 `.cs` が、トップレベル型 1 つ・ファイル名＝型名）。過渡期ルール（既存の複数型ファイルを分割しない）は終了した。
   - 今後は、既存ファイルを含めて厳格に適用する。複数の型を 1 ファイルに置いてはならない（ネスト型は、親型の実装詳細として許可。`partial` による同一型の複数ファイルへの分割は、この原則の違反ではない）。
   - 型を追加・分割するときは、`Step10FourGlobalRatchetGuardTests` の基準値（ファイル別の `Global`／`AppHost`／`Program.rootHub` の件数）も、新しいファイルへ引き継ぐこと（合計を増やさない）。（2026-11-05 Phase14d-Step2: 同テストは `RemovedApiRuleTests` に置き換えたため、引き継ぎは不要になった。ファイル別の基準値を持つ `Phase10Step4dW0RuntimeKeyRatchetTests` は、引き続き引き継ぐ）
2. **原則2: ファイル名 ＝ クラス名（完全一致）**【厳格順守】:
   - `ProfileRepository.cs` には `ProfileRepository` のみを定義する。
3. **原則3: 名前空間 ＝ フォルダパス**【Phase14 で達成。厳格順守】:
   - 名前空間は `DS4Windows.<フォルダパス（ドット区切り）>` とする（例: `Processing/Services/` → `DS4Windows.Processing.Services`、ルート直下 → `DS4Windows`）。`Phase14NamespaceFolderGuardTests` が全 `.cs` と XAML の `x:Class` を検査する。
   - ファイルを移動するときは、同じ Step で名前空間を新しいフォルダへ合わせる（移動と書き換えは別コミット。3.4 R7）。
   - 例外は、第三者由来の名前空間 `Sensorit.Base`（`Core/Utilities/LowpassFilter.cs`、`OneEuroFilter.cs`）のみ。例外を増やさない。
   - `using` は、そのファイルで使う名前空間だけを明示する（`global using` は使わない）。
4. **原則4: 過渡期の段階で実際には使わないが削除せず意図的に残すクラスやメソッドについて**
   - 誤って使用されないよう、その理由や残してある経緯や最終的な対応予定フェーズ を明記した TODO/技術的負債コメントををコード部分にコメントとして残すこと
   - 既存コメントの消失防止： PowerShell 更新スクリプト作成時は、対象ファイル内の既存コメント（特に設計意図や TODO 等）を誤って削ったり上書き消滅させたりしないよう、厳密に置換・追記範囲を限定すること
5. **原則5: コメントには意図（なぜそうなっているか）を書く**（2026-11-05 追記。Phase14c-Step7、レビュー R-15）
   - 新しく書くコメントは、今のコードの意図・理由・制約（循環、スレッド、呼ばれる時点など）を書く。Phase 名・Step 名と旧実装の説明（「旧: …」「…を削除した」）は、コミットメッセージと計画書へ書き、コードには書き足さない（経緯は git の履歴と `docs-forDIMG/` にある）。
   - 既存の経緯コメントは、そのファイルを触る Step の計画書で範囲を決めたときに限り縮めてよい。設計意図・理由・原則4 の TODO は残し、縮めたことはコミットメッセージに書く（ついでの一括削除はしない。原則4 の消失防止を優先する）。

### 3.4 ファイル配置ルール（フォルダ構成の基準。Phase14 で改定）
`.cs` ファイルを新規作成・移動するときは、次の基準でフォルダを決める（経緯と全ファイルの移行は `docs-forDIMG/MadeByAgent/Phase14-Plan.md`、層とフォルダの対応は `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md` の末尾を参照）。

| ID | ルール |
|---|---|
| R1 | 最上位のフォルダは層に対応する: `Input/`（第1層。`Abstractions`、`Devices`、`Hid`）、`Processing/`（第2層。`Abstractions`、`Actions`、`Services`）、`Output/`（第3層。`Abstractions`、`Backend`［仮想パッドのドライバー ViGEm の型を参照してよいのはここだけ。`VirtualPadBackendBoundaryTests` が検査。2026-11-10 Phase16-Step4a］、`Devices`、`Kbm`、`Services`）、`UI/`（4-a。`Abstractions`、`ViewModels`、`Controls`［再利用する部品。例: `ControllerVisualizerControl`］、`Converters`、`Themes`、`Services`）、`Composition/`（4-b。`AppHost`、`ServiceRegistration`、`Startup`）、`Core/`（横断基盤・永続化。`Abstractions`、`Model`、`Logging`、`Utilities`、`Platform`、`Settings`、`Persistence`、`Profiles`、`Updater`）。ルート直下は `App.xaml(.cs)` などアプリの起点だけ（旧 `StartupMethods.cs` は Phase14b で `Core/Platform/StartupEntryService` へ移した。UI からルートの `App` を参照しない。`App` が持つ機能を UI が使うときは、`UI/Abstractions` の契約と `Composition` のアダプターを通す） |
| R2 | インターフェースは、実装と同じ層の `Abstractions/` に置く。第2層が第1層・第3層・UI の実装を使うポートの契約は、使う側の `Processing/Abstractions/` に置く（依存の向きを保つ。1 章）。同様に、`Core`・第1層が使い、上の層が実装する契約は、使う側の `Core/Abstractions/`・`Input/Abstractions/` に置く（2026-11-05 Phase14b。例: `IProfileLoadTarget`、`IDeviceLayerSettings`）。層をまたいで共有する値の型（設定値・イベント引数など）は `Core/` に置く。どの層も使うコントローラーの値の型（列挙型・色・名前の表。例: `DS4Controls`、`DS4Color`、`X360Controls`）と、層をまたいで共有する状態の持ち主（例: `LightbarOverrides`）は `Core/Model` に置く（2026-11-05 Phase14c）。入力 1 フレームの状態の型（`DS4State`、`DS4StateExposed`、`SixAxis`）も `Core/Model` に置く（2026-11-10 Phase16-Step3。UI・第3層が第1層の名前空間を参照しないため） |
| R3 | `ServiceRegistration`（DI 登録）と `AppHost` は `Composition/` に置く。`Composition/` は組み立て専用で、業務ロジックを置かない |
| R4 | 第2層の Actions サブシステム（`Action*`、`*ActionController`）は `Processing/Actions/` に置く |
| R5 | 設定値・プロファイルの型とその永続化（XML、DTO、プロファイルの節）は `Core/Settings`・`Core/Persistence` に置く。層をまたいで共有する基盤は `Core/` に置き、`Core/` から第2層以上を参照しない |
| R6 | 名前空間は 3.3 の原則3 に従い、`DS4Windows.<フォルダ>` とする。フォルダを移したら名前空間も合わせる |
| R7 | ファイルの移動は `git mv` のみで行い、同じコミットでファイルの内容を変更しない（移動を `git diff -M100%` で検証できるようにする）。名前空間の書き換えは別コミットにする |

---

## 4. 開発プロセス

- **マイクロステップの原則**:
  - 作業は必ず `PhaseX-StepY-Plan.md` のように個別計画書を作成し、承認を得てからマイクロタスク単位で進める。
  - 各 Step ごとに必ず単体テストを作成・実行し、`dotnet test` で常時グリーン（合格）を確認しながら進める。
  - **テストの命名**（2026-11-05 追記。Phase14c-Step7、レビュー R-16）: 新しいテストのファイル名・クラス名は、守る機能や振る舞いの名前にする（例: `BackingStoreActionsLoadedHandlerTests`）。Phase 名・Step 名は付けず、経緯は XML コメントに書く。層・名前空間・型の参照の検査は、ソースを文字列で読まず、`LayerDependencyRuleTests`（ArchUnitNET）などの型の検査で書く。既存の Phase 名のテスト 303 件は Phase15-0 で機能名へ改名した（旧名 → 新名の対応表は `docs-forDIMG/MadeByAgent/Phase15-0-Test-Rename-Map.md`）。テストの実行順はクラス名で決まるため、前のテストが残す状態に依存するテストは、改名や追加で順序が変わると落ちる。テストは自分で状態を初期化し、終わりに元へ戻す。
  - **遅いテストの印**（2026-11-05 追記。Phase14c-0）: 1 件 0.3 秒以上かかるテストには `[Trait("Category", "Slow")]` を付ける。Step の途中の確認は `--filter "Category!=Slow"`、Step の終わりの確認（Debug ALL）は全件で行う。
  - **性能の計測の印**（2026-11-05 追記。Phase14d-Step4）: 時間を出力するだけで Assert を持たない性能の計測には `[Trait("Category", "Benchmark")]` を付け、通常の実行から外す（Debug ALL の「Execute Test (Actions)」は `--filter "Category!=Benchmark"`。途中の確認は `--filter "Category!=Slow&Category!=Benchmark"`）。性能を比べるときだけ `--filter "Category=Benchmark"` で流す。Assert で守る性能の検査（割り当てが 0 など）は Benchmark にしない。
  - **OS の状態に触れない**（2026-11-05 追記。Phase14d-Step4）: 単体テストで部品を直接作るときは、タスク スケジューラー・スタートアップ フォルダー・レジストリなど利用者の環境に触れる実物を渡さず、偽物を渡す（例: `FakeStartupEntryService`）。実物の振る舞いを確かめるテストは、そのことをクラスの説明に書く。
- **アーキテクチャ・ガードレールの順守**:
  - `Phase5-Plan.md` §5 に規定された 6 大ガードレール（同一XML排他ロック、適用時Halt停止、スレッド直列化、On-Demandパス評価、ドライバ破棄順序、切断時クリーンアップ）を確実に設計に組み込む。
・慣れない機能はゼロから書かず、まずGitHubでオープンソースプロジェクトを検索してアプローチを比較してから手を動かす
---

## 5. 外部エージェントで移行作業を行う際の行動指示

1. **前提の確認**: 作業開始前に必ず本指示書、全体計画書、および該当 Step の個別計画書を通読すること。
2. **生成されたファイル内容の提供方法(更新スクリプトの場合)**:
   - リポジトリのルートから各場所へ成果物（`.md` やスクリプト）を保存するスクリプトを PowerShell 形式で提示する。
   - ローカルリポジトリのルートは `"G:\Cursor_Folder\DS4Windows-Vader4Pro"` とする。
   - 具体的な出力ルールは `.github/PowerShell-script-generation-rules-for-deliverables.md` を厳格に参照・順守すること。
3. **生成されたファイル内容の提供方法(ダウンロードボタンの場合)**
   - ダウンロードボタンのすぐ上に、[更新or新規]のプリフィックス付きのファイル命名規則に従ったファイル名を記載する
   -ファイル名1→ダウンロードボタン1→ファイル名2→ダウンロードボタン2、、、という順番に並べるのが理想
