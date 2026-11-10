# 構造の見直し（2026-11-10）: 引数の数ではなく「単純で美しいか」で見る

作成日: 2026-11-10
目的: コンストラクターの引数の数（Phase19〜21 の基準）ではなく、全体の構造が単純で美しいかを基準に、変えるべき箇所を洗い出す。引数が多い型は、構造の歪みの症状であって原因ではない。ここでは原因を挙げる。
測定の出典: 2026-11-10 時点のソース（本体 約 52,000 行）。「要確認」は、着手前の調査で事実を確かめる項目。

## 1. 結論（優先度順）

| 順 | 問題 | 症状 | 変える方向 | 規模・リスク |
|---|---|---|---|---|
| A | **設定が「項目ごとの配列」** | `IProfileSettingsService` 158 メンバー、`IAppSettingsService` 68、`BackingStore` 約 1,500 行・公開 292。`X[slot]` の配列が項目の数だけ並ぶ。`Mapping`（引数 15）などがこの巨大な窓口を受ける | スロットごとの **設定のオブジェクト**（領域＝スティック・トリガー・ジャイロ・タッチパッド・出力・ライトバー…を持つ）に作り変える。窓口は `Slot(i)` が返すオブジェクトにする。`ForSlot` の領域の窓口（Phase15）を土台に、配列の窓口を撤去する | 最大。保存の黄金テストがあるので安全網あり。領域ごとに段階的に |
| B | **画面のロジックが code-behind** | `.xaml.cs` 合計 約 10,500 行。`MainWindow` 1,858、`ProfileEditor` 1,802、`BindingWindow` 961、`RecordBox` 899、`AssignmentEditor` 698。ViewModel 層（約 14,600 行）と二重 | 画面ごとに、ロジックを ViewModel へ移し、code-behind は描画とイベントの橋渡しだけにする。引数が多い窓（`MainWindow` 15）は、これで自然に細る | 大。UI の実機確認が要る。画面ごとに独立して進められる |
| C | **`App.xaml.cs` が 1,323 行** | 起動の手順、単一起動、CLI 引数の処理（`CheckOptions` 約 140 行）、IPC のメモリマップトファイル、作業スレッド、終了処理が `App` に同居 | 起動の手順を `Composition/Startup` の部品（起動の流れ、単一起動、IPC の窓口、CLI の処理）へ。`App` は WPF の入口だけにする | 中。順序依存が強い（`StartupOrderGuardTests` あり） |
| D | **`Processing` の直下にファイルが散らばる** | 約 30 ファイルが直下。`Mouse.cs`（1,808 行。実体はタッチパッド→マウスの動作）、`MouseCursor`、`MouseWheel`、`Click`、`FakeTrackball`、`KBMGyroMouse`、`GamepadGyroCamera`、`Macro*`（6 ファイル）、`Synthetic*` など。名前が実体を表さない（`Mouse` は入力機器ではなくタッチパッドの処理） | 機能ごとのフォルダ（`Processing/Touchpad`、`Processing/Macro`、`Processing/Gyro`、`Processing/Synthetic` など）に分け、`Mouse` を実体に合う名前へ。名前空間は規則 R6 に従い、移動（R7）と書き換えを別コミットに | 小〜中。機械的で、安全 |
| E | **実行時の管理が複数の型に分散** | `ControlService`、`ServiceLifecycle`、`DeviceConnector`、`DeviceDisconnectHandler`、`ControllerSlotManager`、`IDeviceSlotState` など。`ControlService` は 12 引数のうち 2 つを「生成の副作用の順序のためだけ」に受ける（Phase20 の 20-7）。名前も役割も重なって見える | 「接続したコントローラーの一生」（接続→設定の適用→読み取り→切断）を表す 1 つの流れに整理し、順序の依存を型で明示する。要確認: 責務の重なりの実態 | 中〜大。実機確認が要る |
| F | **DS4 の名前が全機種の共通語になっている** | `DS4State`、`DS4Controls`、`IDs4DeviceRegistry`、`ds4Timestamp`。DualSense・Switch Pro・（将来の）Xbox も `DS4State` で表す | 共通の概念は機種に依らない名前へ（例: `ControllerState`、`ControllerControls`）。DS4 固有のもの（ライトバー、タッチパッドなど）だけ DS4 の名前を残す | 大きな機械的な置換（数千か所）。テストで守れる。**Xbox を足す前にやるのが望ましい** |
| G | 工場・窓口の太り | `IViewModelFactory` 31 メンバー、`ShellViewModelFactory` 14 引数、`SpecialActionOutputFactory` 14 引数 | B（画面の ViewModel 化）の後に、作る物のグループごとに分ける。単独では急がない | B に従属 |

## 2. 引数の数の基準との関係

`ConstructorArityBaseline` に凍結した 6 型は、上の原因の症状である。基準を満たすためだけに部品を足さず、原因の側で解く。

| 凍結した型 | 原因 |
|---|---|
| `Mapping` 15 | A（巨大な設定の窓口） |
| `MainWindow` 15 | B（画面のロジックが code-behind） |
| `InputReportProcessor` 13、`DeviceConnector` 13 | E（実行時の管理の分散） |
| `ShellViewModelFactory` 14、`SpecialActionOutputFactory` 14 | G（太い工場） |

## 3. 進め方の案（Xbox 対応は最後）

1. **Phase23: D（フォルダと名前の整理）＋ F（DS4 の名前の一般化）**。機械的で安全。以降の作業を読みやすくし、Xbox の追加の前提を整える。
2. **Phase24: C（`App` の分解）**。順序依存を型で明示する。
3. **Phase25: A（設定のオブジェクト化）**。領域ごとに段階的に。保存の黄金テストを毎段階で通す。
4. **Phase26: B（code-behind → ViewModel）**。画面ごと。`MainWindow` の残りと `ProfileEditor` から。
5. **Phase27: E（実行時の管理の整理）**。A・C の後に調べ直す（それで形が変わるため）。
6. **最後: G の整理と、USB の Xbox 360 入力（現 `Phase22-Plan.md`）**。

各 Phase の前に、事実の確認（要確認の項目）と個別計画書の作成・承認を行う（規則 4 章）。

## 4. 更新履歴

- 2026-11-10: 作成。コードは未着手。
