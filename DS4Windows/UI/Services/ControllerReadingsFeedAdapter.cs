#nullable enable

using System;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary><see cref="IControllerReadingsSource"/> を、画面側の窓口 <see cref="IControllerReadingsFeed"/> として見せる（委譲のみ）。</summary>
    public sealed class ControllerReadingsFeedAdapter : IControllerReadingsFeed
    {
        private readonly IControllerReadingsSource source;

        public ControllerReadingsFeedAdapter(IControllerReadingsSource source)
            => this.source = source ?? throw new ArgumentNullException(nameof(source));

        public bool IsMeasuringProcessingDelay
        {
            get => source.IsMeasuringProcessingDelay;
            set => source.IsMeasuringProcessingDelay = value;
        }

        public bool TryRead(int slot, int profileSlot, ControllerState input, ControllerState mapped, out ControllerReadingInfo info)
            => source.TryRead(slot, profileSlot, input, mapped, out info);

        public ControllerState GetCurrentState(int slot) => source.GetCurrentState(slot);
    }
}
