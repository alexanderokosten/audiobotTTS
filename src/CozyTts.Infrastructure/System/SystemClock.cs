using CozyTts.Application.Abstractions.System;

namespace CozyTts.Infrastructure.System;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
