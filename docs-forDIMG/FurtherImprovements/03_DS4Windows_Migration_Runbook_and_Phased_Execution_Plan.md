# DS4Windows マイグレーション・ランブック & 段階的移行実施計画書
**〜 2,728件のテストを常時グリーンに保つ、壊さない安全なリファクタリング手順書 〜**

- **対象リポジトリ**: `gwin7ok/DS4Windows`
- **文書番号**: 03
- **文書種別**: 実装実行手順書 / マイグレーション・ランブック (Migration Runbook)
- **前提文書**: `01_DS4Windows_Architecture_Analysis_Report.md`, `02_DS4Windows_Refactoring_Design_Proposal.md`

---

## 1. 移行の基本原則（ラチェット維持と安全設計）

本プロジェクトには、Phase 0〜Phase 10 を通じて蓄積された **2,728件の回帰テスト** が存在します。リファクタリングにあたっては以下のルールを厳格に適用します。

1. **常時グリーン原則（Always Green）**:
   各ステップの作業単位（コミット）ごとに `dotnet test` を実行し、全テストの通過を確認する。一度に複数のレイヤーを書き換えてテストが壊れた状態をコミットしない。
2. **テスト先行更新（Test-First Modernization）**:
   プロダクトコードを変更する前に、まずそれを縛っている「脆いテスト（文字列マッチング等）」を「セマンティックなノード検証テスト」へ改修して安全網を張る。
3. **並行稼働と段階的廃止（Strangler Fig Pattern）**:
   既存のクラスをいきなり削除・変更せず、新しい共通コンポーネントを並行して作成し、利用箇所を1画面ずつ移行した後に旧コードを撤廃する。

---

## 2. Phase A 実行手順：テスト堅牢化 & UI見切れバグの根絶

**【目的】**: XAMLの改行や空白でテストが落ちる問題を解消し、トリガー数値の文字切れ（`255` → `25`）を完全に解消する。

### ステップ A-1: XAMLセマンティック検証ヘルパーの導入
* **作業ファイル**: `DS4WindowsTests/TestHelpers/XamlTestHelper.cs`（新規作成）
* **内容**:
  LINQ to XML（`XDocument`）を用いたノード探索ヘルパーをテストプロジェクトに追加する。
```csharp
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DS4WindowsTests.TestHelpers
{
    public static class XamlTestHelper
    {
        private static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2006/xaml";

        public static XDocument Load(string relativePath)
        {
            string fullPath = SourceFileLocator.Find(relativePath);
            return XDocument.Parse(File.ReadAllText(fullPath));
        }

        public static XElement GetElementByName(XDocument doc, string xName)
        {
            return doc.Descendants().FirstOrDefault(e => e.Attribute(XNs + "Name")?.Value == xName);
        }
    }
}
```

### ステップ A-2: 脆いレイアウトテストの改修
* **作業ファイル**: `DS4WindowsTests/Phase10Step3bW0ProfileEditorLayoutTests.cs`
* **作業内容**:
  `TheControlsTab_IsStillA470WideGrid_WithACanvasRowAndAListRow` 内の正規表現アサート（空白や改行を縛っているコード）を、ノード構造チェックに置換する。
* **検証コマンド**:
  ```bash
  dotnet test --filter "FullyQualifiedName~Phase10Step3bW0ProfileEditorLayoutTests"
  ```
  （合格することを確認後、コミット: `test: modernize profile editor layout test to use XDocument`）

### ステップ A-3: `ControllerReadingsControl.xaml` の数値見切れ修正
* **作業ファイル**: `DS4Windows/DS4Forms/ControllerReadingsControl.xaml`
* **作業内容**:
  L2/R2/TX/TY のラベル幅を `Width="38"`、`Padding="1,1"` に改修し、中央揃えを適用。
* **検証**:
  アプリを起動し、Controller Readings タブで L2/R2 を全開にした際に「255」が右端まで完全に表示されることを目視確認。

---

## 3. Phase B 実行手順：コントローラー描画の共通化（UI統合）

**【目的】**: `ProfileEditor` と `BindingWindow` に二重実装されているコントローラー画像＋ボタン配置を単一コントロールへ統合し、座標ズレを根絶する。

```text
[ステップ B-1]                                [ステップ B-2]                             [ステップ B-3]
ControllerLayoutProfile (メタデータ) 作成  ──>  ControllerVisualizerControl 実装  ──>  各画面の旧Canvasを置換
```

### ステップ B-1: レイアウトプロファイル（メタデータ）の定義
* **作業ファイル**:
  * `DS4Windows/DS4Forms/Controls/IControllerLayoutProfile.cs`（新規作成）
  * `DS4Windows/DS4Forms/Controls/Profiles/DualShock4LayoutProfile.cs`（新規作成）
  * `DS4Windows/DS4Forms/Controls/Profiles/Xbox360LayoutProfile.cs`（新規作成）
* **内容**:
  ボタンの中心座標・サイズを「0.0〜1.0」の相対比率で定義。

### ステップ B-2: `ControllerVisualizerControl` の実装
* **作業ファイル**:
  * `DS4Windows/DS4Forms/Controls/ControllerVisualizerControl.xaml`
  * `DS4Windows/DS4Forms/Controls/ControllerVisualizerControl.xaml.cs`
* **内容**:
  画像（Image）のレンダリングサイズに追従して、透過Canvas上にボタンを自動配置するコントロールを作成。

### ステップ B-3: 各画面への適用
1. `BindingWindow.xaml` の `conCanvas` を `ControllerVisualizerControl` に置き換える。
2. `BindingWindow.xaml.cs` の `InitDS4Canvas()` 内の座標指定コード（数十行）を全廃し、`visualizer.SetProfile(new DualShock4LayoutProfile())` に置き換える。
3. `ProfileEditor.xaml` のコントローラー表示エリアも同様に移行。
* **検証**:
  `dotnet test` が全件通過すること、および両画面でボタンホバーがズレなく追従することを確認。

---

## 4. Phase C 実行手順：PureDIの純度向上と `ControlService` のスリム化

**【目的】**: 34個のコンストラクタ引数と、遅延デリゲート（`Func<T>`）を全廃し、テスタブルで保守性の高いPure DI構造へ移行する。

### ステップ C-1: 3大コーディネーターの切り出し
1. **`IInputDeviceCoordinator` / `InputDeviceCoordinator`**:
   `IDs4DeviceRegistry`, `IInputDeviceModuleRegistry`, `IDeviceConnector` 関連の処理を `ControlService` から移動。
2. **`IVirtualOutputCoordinator` / `VirtualOutputCoordinator`**:
   `IVirtualPadService`, `IOutputSlotService`, `IVirtualKBM` 関連の処理を移動。
3. **`IProfileStateCoordinator` / `ProfileStateCoordinator`**:
   `IProfileRepository`, `IProfileSettingsService`, `IAppSettingsService` 関連の処理を移動。

### ステップ C-2: 双方向参照のイベント化
* `DeviceDisconnectHandler` が `ControlService`（`IDeviceSlotHost`）を遅延参照していた箇所を、`InputDeviceCoordinator.DeviceDisconnected` イベントの購読方式に変更。
* `ServiceRegistration.cs` から `Func<IDeviceSlotHost>` 等の登録を1つずつ削除。

### ステップ C-3: `ControlService` のコンストラクタ集約
* `ControlService` の引数を上記の3大コーディネーターとロガーの計4個に集約。
* **検証コマンド**:
  ```bash
  dotnet test --filter "FullyQualifiedName~ControlService"
  ```
  （DI配線テスト群がすべてパスすることを確認）

---

## 5. ロールバック基準と品質ゲート（Quality Gates）

各フェーズの完了判定基準：
1. **ビルド警告ゼロ**: `dotnet build -c Release` で警告が新規発生していないこと。
2. **全テスト合格**: `dotnet test -c Release --no-build` で 2,728 件が 100% 合格すること。
3. **ホットパス配線検証**: 実機ポーリングスレッドにおいてアロケーションが発生していないこと（Phase 4 のベンチマークテストで検証）。