using UniversityLostFound.Application.Common;

namespace UniversityLostFound.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
