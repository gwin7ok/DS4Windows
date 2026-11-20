#nullable enable

using DS4Windows.Output.Abstractions;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;

namespace DS4Windows.Output.Services
{
    public class OutputSlotStore : IOutputSlotStore
    {
        private readonly IPathService _pathService;

        public OutputSlotStore(IPathService pathService)
        {
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;
        }

        public bool Load(OutputSlotManager slotManager)
        {
            if (slotManager == null) return false;
            try
            {
                bool result = OutputSlotPersist.ReadConfig(slotManager, _pathService.AppDataPath);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Load: result={result}");
                return result;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogWarn($"[DI] OutputSlotStore.Load failed: {ex}");
                return false;
            }
        }

        public bool Save(OutputSlotManager slotManager)
        {
            if (slotManager == null) return false;
            try
            {
                bool result = OutputSlotPersist.WriteConfig(slotManager, _pathService.AppDataPath);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotStore.Save: result={result}");
                return result;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogWarn($"[DI] OutputSlotStore.Save failed: {ex}");
                return false;
            }
        }
    }
}
