# XboxProbe（調査用プログラム。ビルド対象外）

作成日: 2026-11-10
目的: HidHide で隠した USB の Xbox コントローラーを、許可リストに登録したアプリから読めるかを確かめる（`../Xbox-Input-Research.md` §7）。XInput の 4 スロットと、HID の機器（VID_045E／PID_028E）の生のレポートを、指定した秒数だけ読んで表示する。
使い方: `Program.cs.txt` と `XboxProbe.csproj.txt` を、それぞれ `Program.cs`・`XboxProbe.csproj` に名前を戻し、`dotnet build -c Release -p:Platform=x64` でビルドする。Phase22 の 22-0（レポートのデコード）で、操作ごとのレポートを記録するのに再利用する。
