using Application.Common.Interfaces;

namespace Infrastructure.Common.Untils;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
