# Phase7 残存台帳: Service Locator と静的アクセス

作成日: 2026-10-17（Phase7-Step1 W4）  
基準: 現行コードの走査（コメントを除く）。`Phase6-Step11-Global-Residual-Ledger.md` の続き。  
ガード: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`Program.rootHub`）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）。減る変更だけ許可。基準値を更新して固定する。

## 1. 集計（2026-10-17、Phase7-Step1 W3 まで）

- `Global.*`: 733（Phase6 完了時 747。W1 の `BackingStore` 孤立メソッド削除で 14 減）。
- `AppHost.GetService|Services`: 40。
- `Program.rootHub`: 22。
- `ServiceProviderHolder.Provider|GetRequiredService`: 23（今回、追跡を開始）。

## 2. Service Locator の分類

### 2.1 AppHost.GetService|Services（40）
- `MainWindow.xaml.cs` 17: コンストラクターで 14 件のサービスを解決。Composition Root 隣接の画面。→ Phase7-Step5
- `Mapping.cs` 6: 静的束縛（`profileSettings`、`profileApplication`、`VirtualKBM`）と 3 件の遅延解決。→ Phase7-Step7〜9（`Mapping` の instance 化）
- `App.xaml.cs` 4（Composition Root。`CreateHost` 自身を除く解決）、`ProfileEditor.xaml.cs` 4、`Global.cs` 4（`*Instance` のフォールバックと `Load`）。→ Phase7-Step4／5
- `DI/AppHost.cs` 2、`DI/Compat/AppHost.cs` 1（`Mapping.cs:86` 用の互換ラッパー）、`BackingStore.cs` 1、`ControlService.cs` 1。→ Step4／Step9／Step3

### 2.2 Program.rootHub（22）
- `App.xaml.cs` 12（代入、`LoggerHolder` の生成、起動時の呼び出し、終了処理）、`Global.cs` 6、`ServiceRegistration.cs` 1、`Mapping.cs` 1、`ProfileEditor.xaml.cs` 1、`DS4SixAxis.cs` 1。→ Phase7-Step5

### 2.3 ServiceProviderHolder（23。新規に追跡）
- `Actions/ActionManager.cs` 13、`ActionFactory.cs` 1、`DefaultKeyButtonActionControllerFactory.cs` 2、`KeyActionAdapter.cs` 1: **Phase8 の持ち越し**（Actions サブシステムは Phase8 で統合ディスパッチへ作り直す）。計 17。
- `Mapping.cs` 5、`App.xaml.cs` 1: → Phase7-Step5（`App` の `CleanShutdown`）と Step7〜9（`Mapping`）。計 6。
- 設定側の `SetProvider`（`DI/AppHost.cs` 3）は Composition Root の正規の使い方で、数えない。

## 3. 参照
- 調査: `Phase7-0-Survey.md` §1.5（F-A）。計画: `Phase7-Step1-Plan.md` W4。
