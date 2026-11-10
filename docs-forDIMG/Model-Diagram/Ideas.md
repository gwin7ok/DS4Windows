# 構想（実装されていないもの）

最終更新: 2026-11-10（Phase17-0）。
旧モデル図（`Archive/`）に目標像として書かれていたが、**現在のコードに存在しないもの**をここに分ける。実装した時点で `01-Structure.md` へ移す。ここの名前は `ModelDiagramGuardTests` の対象ではない。

| 構想 | 旧図での名前 | 現状 |
|---|---|---|
| 入力ループの統括 | `InputLoopCoordinator`、`IInputLoopCoordinator` | 読み取りループは各 `DS4Device` 系が持つ。**Phase17-3 の `ControllerSession` が近い構想** |
| マッピングの文脈オブジェクトとプロセッサー列 | `MappingPipelineContext`、`IInputMappingPipeline`、`StickProcessor` ほか | `Mapping` が段（`CurveAndDeadzoneStage`、`MouseConversionStage` ほか）を呼ぶ形に落ち着いた。残りの分割は Phase17-6 |
| マウスエンジン | `IMouseEngine` | `MouseConversionStage` と `Mouse` などに分かれている |
| ホットプラグ監視 | `IDeviceHotplugMonitor` | `DS4Devices` と `HidDevices` が列挙する |
| 解析器の工場 | `IInputReportParserFactory` | Phase17-1 で、記述子が解析器を渡す形にする |
| Vader 4 Pro の解析 | `Vader4ProReportParser` | 対応を Phase6-Step10 で削除済み。復活の予定なし |
| 親子 ViewModel の調停 | Mediator パターン | 用途別の 3 つのファクトリーに分けた段階。残りは Phase17-6 |

旧図の詳細は `Archive/` を参照する（更新しない）。
