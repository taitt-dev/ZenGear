using ZenGear.Application.Common.Interfaces;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// DateTime service implementation.
/// Provides current UTC time - abstracted for testing.
/// </summary>
public class DateTimeService : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
