using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pafiso.Enumerables;

public class PagedListJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(PagedList<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(PagedListJsonConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class PagedListJsonConverter<T> : JsonConverter<PagedList<T>> {
    public override PagedList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new JsonException("Expected StartObject token.");
        }

        IList<T> entries = [];
        var totalEntries = 0;
        int? pageNumber = null;
        int? pageSize = null;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName) {
                throw new JsonException("Expected PropertyName token.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName) {
                case var name when string.Equals(name, nameof(PagedList<T>.Entries), StringComparison.OrdinalIgnoreCase):
                    entries = JsonSerializer.Deserialize<IList<T>>(ref reader, options) ?? [];
                    break;
                case var name when string.Equals(name, nameof(PagedList<T>.TotalEntries), StringComparison.OrdinalIgnoreCase):
                    totalEntries = reader.GetInt32();
                    break;
                case var name when string.Equals(name, nameof(PagedList<T>.PageNumber), StringComparison.OrdinalIgnoreCase):
                    pageNumber = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                    break;
                case var name when string.Equals(name, nameof(PagedList<T>.PageSize), StringComparison.OrdinalIgnoreCase):
                    pageSize = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new PagedList<T>(entries, totalEntries, pageNumber, pageSize);
    }

    public override void Write(Utf8JsonWriter writer, PagedList<T> value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        var namingPolicy = options.PropertyNamingPolicy;

        writer.WritePropertyName(namingPolicy?.ConvertName(nameof(PagedList<T>.Entries)) ?? nameof(PagedList<T>.Entries));
        JsonSerializer.Serialize(writer, value.Entries, options);

        writer.WriteNumber(namingPolicy?.ConvertName(nameof(PagedList<T>.TotalEntries)) ?? nameof(PagedList<T>.TotalEntries), value.TotalEntries);

        var pageNumberName = namingPolicy?.ConvertName(nameof(PagedList<T>.PageNumber)) ?? nameof(PagedList<T>.PageNumber);
        if (value.PageNumber.HasValue)
            writer.WriteNumber(pageNumberName, value.PageNumber.Value);
        else
            writer.WriteNull(pageNumberName);

        var pageSizeName = namingPolicy?.ConvertName(nameof(PagedList<T>.PageSize)) ?? nameof(PagedList<T>.PageSize);
        if (value.PageSize.HasValue)
            writer.WriteNumber(pageSizeName, value.PageSize.Value);
        else
            writer.WriteNull(pageSizeName);

        writer.WriteEndObject();
    }
}
