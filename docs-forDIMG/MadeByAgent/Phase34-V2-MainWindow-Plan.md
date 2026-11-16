# Phase34 V2: MainWindow の業務の参照の解消（計画書）

作成日: 2026-11-16 / 対象: `DS4Windows/UI/MainWindow.xaml.cs` / 規則: V2

## 1. 現状（基準の 6 件）
IAppSettingsService、IEnvironmentService、IPathService、IServiceLifecycle、IRuntimeFeatureSwitch、IControllerSlotView。画面の各所から約 130 か所で使っている。

## 2. 方針（決定事項）
サービスごとの転送ではなく、画面が使う用途ごとに、UI 側の窓口（`UI/Services`）へ分ける。

1. `MainWindowSettings`: 画面が読み書きする設定だけを持つ。列幅は、画面の列と同じ順の配列で受け渡し（ログ・コントローラー一覧・出力スロット・自動プロファイル）、設定の項目名との対応を 1 か所にまとめる。Steam フォルダーの有効判定（`CustomSteamFolderIfValid`）、機器オプションの ViewModel 生成もここ。
2. `MainWindowEnvironment`: 保存先のパス（`AppDataPath` ほか）と実行環境（管理者権限、スロット数の上限、バージョン、クラス GUID、HidHide・FakerInput の更新）。`ProfileList` の生成もここ。
3. `MainWindowServiceControl`: サービスの開始・停止・ホットプラグ・状態・イベントと、OSC／UDP／モーション／HidHide／タッチパッドスライドの切り替え。委譲のみ。
4. `IControllerSlotView`: `CompositeDeviceModel.SubscribeSyncChange` へ移す。
5. コンストラクターの引数は 14 → 12（`ConstructorArityBaseline` から外れる）。挙動・ログ文言・イベントの購読順は変えない。

## 3. 手順
窓口 3 つを追加 → 画面の呼び出しを置き換え → 登録（`ServiceRegistration.Ui.cs`）→ 基準 6 件を削除 → ソース文字列を検査するテストの更新 → ビルド 0/0・全テスト → 計画書 §9.3・台帳 → 統合。

## 4. 実機確認（先延ばし。台帳へ記載）
起動・終了、サービスの開始・停止、ホットプラグ、ウィンドウの位置・サイズの保存と復元、列幅の保存と復元（ログ・コントローラー・出力スロット・自動プロファイル）、Steam フォルダーの検出、機器の登録オプション画面、OSC／UDP の切り替え、USB/Bluetooth の同期変化時のトレイ更新。
