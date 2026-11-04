using System;

namespace DS4Windows.Processing.Actions
{
    public interface IProfileSwitcher
    {
        void SwitchProfile(int deviceIndex, SpecialAction action);
        void RestoreProfile(int deviceIndex);
        // Phase11 W6 K-W6-7: 切り替え先が空・削除済み（(none) 表示）なら false。そのとき ProfileSwitchAction は切替も復帰も行わない。既定は true
        bool CanSwitch(SpecialAction action) => true;
        // Phase8-Step1 W5: ApplyManualProfile は削除した（追加の 2026-09-02 から本番の呼び出しが 0 件。UI は IProfileApplicationService.ApplyProfile を直接使う正規の経路で、重複する並行の入口だった。Phase8-Step1-Orphan-Decisions.md §2 E）。

        // Phase8-Step6 W3: ClearState は削除した（承認済み。本番の呼び出しが 0 件のままで、切断の後始末の役割は、エンジンの OnDeviceDisconnected が復帰の合図を消すことで担う。
        // 切断で OlderProfilePath を空にすると、スロットの「通常のプロファイル」が失われる。Phase8-Step6-Plan.md §2.3・Q3、Phase8-Step1-Orphan-Decisions.md §2）。
    }
}
