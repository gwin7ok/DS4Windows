using System;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Utilities
{
    /// <summary>
    /// バージョン文字列の数値化。
    /// スキップしたバージョンの比較（<c>BackingStore.PostProcessLoad</c>、<c>AppSettingsXmlSerializer.Load</c>、<c>AppSettingsService</c>）が使う。
    /// </summary>
    public static class VersionUtil
    {
        public static ulong CompileVersionNumberFromString(string versionStr)
        {
            ulong result = 0;
            try
            {
                Version tmpVersion = new Version(versionStr);
                result = CompileVersionNumber(tmpVersion.Major, tmpVersion.Minor,
                    tmpVersion.Build, tmpVersion.Revision);
            }
            catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[VersionUtil:22] ignored {caught.GetType().Name}: {caught.Message}"); }

            return result;
        }

        public static ulong CompileVersionNumber(int majorPart, int minorPart,
            int buildPart, int privatePart)
        {
            ulong result = (ulong)majorPart << 48 | (ulong)minorPart << 32 |
                (ulong)buildPart << 16 | (ushort)privatePart;
            return result;
        }
    }
}
