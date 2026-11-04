using System;
using System.Collections.Generic;

namespace DS4Windows
{
    // Represents per-device runtime items that previously lived as globals in Mapping (eg. untriggeraction/untriggerindex, macro queues, lightbar state).
    public class DeviceRuntimeState
    {
        public SpecialAction UntriggerAction = null;
        public int UntriggerIndex = -1;
        // Phase9 9-C（ユーザー判断 Q2・Q3）: 未使用だった MacroControl／MacroCount（デバイスごとの置き場として作られたが未接続）は削除した。
        // デバイスごとのマクロの出力フラグと個数は、MacroRuntimeState（MacroControl[device]／MacroCount[device]）が持つ。

        // Lightbar/fade state mirrors some Mapping globals
        public int FadeTimer = 0;
        public DS4Color LastColor = new DS4Color();
        public bool ForceLight = false;

        public DeviceRuntimeState() { }
    }
}
