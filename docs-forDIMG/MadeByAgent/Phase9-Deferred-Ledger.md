# Phase9 先送り台帳

作成日: 2026-10-23
目的: Phase9（責務分割）で「やらない」ではなく「今は行わない」と決めた項目を、再開の条件とともに記録する。

| ID | 内容 | 先送りの理由 | 再開の条件 | 関連 |
|---|---|---|---|---|
| F-1c | 起動の手順（`Application_Startup` が呼ぶ各ステップ）を、`AppStartupSequence` へ移す | `runShutdown`／`skipSave`／`exitApp` の持ち主を決める必要がある。F-1b で手順は短くなり、順序のテスト（`Phase9F1StartupOrderGuardTests`）も付いた | 起動処理を頻繁に変更する必要が出たとき | `Phase9-F-Plan.md` §5 |
| F-2c | IPC コマンドの実行（`MainWindow.Ipc.cs` の 9 個の `Ipc*`）を、`IpcCommandProcessor`（窓口 `IIpcCommandHost`）へ | 窓口のメンバーが多く（サービス状態、デバイス一覧、プロファイル、出力スロット）、効果に比べて変更が大きい。F-2b で `MainWindow.Ipc.cs` は 327 行に収まった | IPC の機能を増やすとき、または UI なしでコマンドを単体テストしたいとき | `Phase9-F-Plan.md` §6 |
| F-4 | `ProfileEditor.xaml.cs`（2,098 行）の分割（`Reload`、SA の一覧の見出し・並べ替え、振動テスト） | Controls／SA の画面の仕様が、今回の UI 変更で大きく変わった。仕様が落ち着いてからのほうが安全 | UI の仕様が固まったとき。9-G を行う場合は同時に判断する | `Phase9-F-Plan.md` F-4 |
| F-5c | `ControlService` の `running`（利用者: `AutoProfileService`／`App`／`MainWindow`／`TrayIconViewModel`）と `suspending`（`MainWindow`）の転送を、`IServiceLifecycle` 経由へ | 4 つの利用者は `Start`／`Stop`／`slotManager` などのためにも `ControlService` を直接持つ。`running` だけを替えると、コンストラクターの引数とテストが増えるだけで効果が小さい | `ControlService` の他のメンバーの切り離しと同時に行う（`ControlService.cs` に TODO を記載済み） | `Phase9-F-Plan.md` F-5 |
