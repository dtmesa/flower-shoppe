using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PlumeriaStore.Api.Common.Data;

/// <summary>
/// SQLite has no timezone-aware date type, so EF Core reads DateTime columns back as Kind=Unspecified.
/// Entity configurations apply this to any UTC timestamp column so JSON serialization always emits a
/// trailing "Z" instead of silently becoming a false "local" time on the way back out.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        toDb => toDb.Kind == DateTimeKind.Utc ? toDb : toDb.ToUniversalTime(),
        fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
    {
    }
}
