using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace DevStart.Infrastructure.Helpers
{
    internal static class EfJson
    {
        public static readonly ValueConverter<List<string>, string> StringListConverter =
        new(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
        );

        public static readonly ValueConverter<List<Guid>, string> GuidListConverter =
        new(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new()
        );
    }
}
