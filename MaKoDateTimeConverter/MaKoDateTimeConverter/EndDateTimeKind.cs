using System.Text.Json.Serialization;

namespace MaKoDateTimeConverter;

/// <summary>
/// Describes how an end datetime shall be understood
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EndDateTimeKind
{
    /// <summary>
    /// The end date shall be understood as inclusive end date
    /// </summary>
    /// <example>"2022-10-31" for end of October</example>
    Inclusive,

    /// <summary>
    /// The end date shall be understood as exclusive end date
    /// </summary>
    /// <example>"2022-11-01" for end of October</example>
    Exclusive,
}
