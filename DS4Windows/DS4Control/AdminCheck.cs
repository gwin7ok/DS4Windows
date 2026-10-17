using System;
using System.IO;
using System.Security.Principal;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10d W2（Q5＝A）: 旧 <c>Global.AdminNeeded／IsAdministrator</c>。状態を持たない Windows の判定（内容は移動のみ）。
    /// </summary>
    public static class AdminCheck
    {
        /// <summary>
        /// Check if Admin Rights are needed to write in Appliplation Directory
        /// </summary>
        /// <returns></returns>
        public static bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(PathService.ExecutableDirectoryState + "\\test.txt", "test");
                File.Delete(PathService.ExecutableDirectoryState + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
