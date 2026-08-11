using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.Services;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private readonly string _format;

    public DateOnlyJsonConverter() : this("yyyy-MM-dd") { }

    public DateOnlyJsonConverter(string format)
    {
        _format = format;
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (s is null)
                throw new JsonException("Invalid DateOnly value.");

            if (DateOnly.TryParseExact(s, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;
        }

        throw new JsonException($"Unable to convert value to DateOnly.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
    }
}
