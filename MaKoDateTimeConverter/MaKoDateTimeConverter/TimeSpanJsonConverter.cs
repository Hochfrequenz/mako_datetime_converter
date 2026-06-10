using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace MaKoDateTimeConverter;

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
        return XmlConvert.ToTimeSpan(reader.GetString()!);
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
