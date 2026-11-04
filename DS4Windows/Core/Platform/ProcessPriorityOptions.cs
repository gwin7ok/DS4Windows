using System.Collections.Generic;
using System.Diagnostics;

namespace DS4Windows.Services
{
    /// <summary>
    /// プロセス優先度の選択肢の表。設定（<c>IAppSettingsService.ProcessPriority</c>）は、この表の添字で保存されている。
    /// 旧 <c>MainWindow.ProcessPriorityClasses</c>（UI の静的リスト）と <c>ServiceLifecycle</c> の複製を、1 か所へまとめたもの（Phase9 9-F F-5）。
    /// 並び順を変えると、保存済みの設定が別の優先度にずれるため、変えてはならない。
    /// </summary>
    public static class ProcessPriorityOptions
    {
        public static readonly IReadOnlyList<ProcessPriorityClass> Classes = new[]
        {
            ProcessPriorityClass.Normal, ProcessPriorityClass.AboveNormal,
            ProcessPriorityClass.High, ProcessPriorityClass.RealTime
        };

        public static int IndexOf(ProcessPriorityClass priority)
        {
            for (int i = 0; i < Classes.Count; i++)
            {
                if (Classes[i] == priority) return i;
            }
            return -1;
        }
    }
}
