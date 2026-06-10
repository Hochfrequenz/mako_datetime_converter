using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace MaKoDateTimeConverter;

/// <summary>
/// Serializes and deserializes <see cref="Nullable{T}">TimeSpan?</see> as an ISO 8601 duration string
/// (e.g. <c>"PT1S"</c> for 1 second, <c>"P1D"</c> for 1 day, <c>"PT0.001S"</c> for 1 millisecond).
/// Uses <see cref="System.Xml.XmlConvert"/> for formatting and parsing.
/// See https://en.wikipedia.org/wiki/ISO_8601#Durations for the duration format specification.
/// </summary>
internal sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
{
    public override bool HandleNull => true;

    public override TimeSpan? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected a JSON string for a TimeSpan value, got {reader.TokenType}."
            );
        var raw = reader.GetString();
        if (string.IsNullOrEmpty(raw))
            throw new JsonException("Cannot parse an empty or null string as a TimeSpan.");
        try
        {
            return XmlConvert.ToTimeSpan(raw);
        }
        catch (FormatException ex)
        {
            throw new JsonException(
                $"The value '{raw}' is not a valid ISO 8601 duration (e.g. \"PT1S\", \"P1D\", \"PT0.001S\").",
                ex
            );
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan? value,
        JsonSerializerOptions options
    )
    {
        if (!value.HasValue)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(XmlConvert.ToString(value.Value));
    }
}
