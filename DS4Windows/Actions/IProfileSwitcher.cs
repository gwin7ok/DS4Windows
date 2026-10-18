using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    public interface IProfileSwitcher
    {
        void SwitchProfile(int deviceIndex, SpecialAction action);
        void RestoreProfile(int deviceIndex);
        // Phase8-Step1 W5: ApplyManualProfile は削除した（追加の 2026-09-02 から本番の呼び出しが 0 件。UI は IProfileApplicationService.ApplyProfile を直接使う正規の経路で、重複する並行の入口だった。Phase8-Step1-Orphan-Decisions.md §2 E）。

        // TODO(Phase8-Step6): 切断時の後始末（Phase5-Step3 の「切断時スタック残留防止」。§5.6 ガードレール6）のために配備された契約だが、
        // 本番の呼び出しが 0 件のまま（物理切断の経路 ControlService.On_DS4Removal から呼ばれていない。呼ぶのはテストのみ）。
        // 本来使うべき契約。ただし OlderProfilePath は再接続時の復元（ControlService の接続処理）にも使われるため、
        // 何を残し何を消すかを Step6（コントローラー接続時の紐付け）で調べてから、切断の経路へ接続する。
        void ClearState(int deviceIndex);
    }
}
