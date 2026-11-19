# PhaseRV9 警告台帳（組・フォルダー・件数・待つ Phase・状態）

測定: 2026-11-19、`-p:Nullable=enable` の全体ビルド（`scripts/audit/Measure-Nullable.py`）。着手前の件数。

| 組 | フォルダー | 警告 | 待つ Phase | Step | 状態 |
|---|---|---|---|---|---|
| A | Core/Model・Logging・Utilities・Updater・Profiles | 64 | なし | RV9-1 | 未着手 |
| A | Core/Platform | 69 | なし | RV9-2 | 未着手 |
| A | Input | 84 | なし | RV9-3 | 未着手 |
| A | Output | 98 | なし | RV9-4 | 未着手 |
| A | Processing（Actions 除く） | 156 | なし | RV9-5 | 未着手 |
| A | ルート・DS4WindowsCli | 50 | なし | RV9-6 | 未着手 |
| B | Core/Settings | 110 | RV5・RV6 | RV9-7 | 待ち |
| B | Core/Persistence | 677 | RV5 | RV9-8 | 待ち |
| B | Processing/Actions | 144 | RV6 | RV9-9 | 待ち |
| B | UI/Abstractions・Converters・Services・Controls | 125 | RV3 | RV9-10 | 待ち |
| B | UI/ViewModels | 252 | RV3・RV4 | RV9-11 | 待ち |
| B | UI 直下の画面 | 307 | RV4 | RV9-12 | 待ち |
