using System.Text.Json.Serialization;

namespace MaKoDateTimeConverter;

/// <summary>
/// A date time conversion configuration describes which steps are necessary to convert a datetime from a source to a target
/// </summary>
public record DateTimeConversionConfiguration
{
    /// <summary>
    /// The configuration of the datetime before the conversion
    /// </summary>
    [JsonPropertyName("source")]
    public DateTimeConfiguration Source { get; set; } = default!;

    /// <summary>
    /// The configuration of the datetime before the conversion
    /// </summary>
    [JsonPropertyName("target")]
    public DateTimeConfiguration Target { get; set; } = default!;

    /// <summary>
    /// true iff the configuration is valid
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Source.IsGas == Target.IsGas // you must not convert to/from different sparten 
                           && Source.IsEndDate == Target.IsEndDate // you must not convert to/from end/start date
                           && Source.IsValid && Target.IsValid;
}
