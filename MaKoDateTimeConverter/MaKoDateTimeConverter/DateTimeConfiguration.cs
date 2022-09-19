namespace MaKoDateTimeConverter;

/// <summary>
/// Describes how a datetime is meant by a system.
/// Two of these configurations allow to convert a datetime smoothly.
/// </summary>
public record DateTimeConfiguration
{
    /// <summary>
    /// true if the the datetime describes an "end date", e.g. a contract end date
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isEndDate")]
    public bool IsEndDate { get; set; }

    /// <summary>
    /// <see cref="EndDateTimeKind"/>
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("endDateTimeKind")]
    public EndDateTimeKind? EndDateTimeKind { get; set; }

    /// <summary>
    /// true iff the datetime describes a datetime in Sparte Gas
    /// </summary>
    /// <remarks>
    /// Please note that this is independent from the information whether the datetime is actually <see cref="IsGasTagAware"/>!
    /// There are systems that discriminate Gas and non-Gas (this is what this flag is for) but are still unaware of the German Gas-Tag. 
    /// </remarks>
    [System.Text.Json.Serialization.JsonPropertyName("isGas")]
    public bool IsGas { get; set; }

    /// <summary>
    /// true iff <see cref="IsGas"/> is true and the date time is aware of the German "Gas-Tag" (meaning that start dates are 6:00 German local time and end dates are 06:00 German local time (if the end date is meant exclusive))
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isGasTagAware")]
    public bool? IsGasTagAware { get; set; }

    /// <summary>
    /// true iff the configuration is valid (self-consistent).
    /// </summary>
    /// <remarks>This basically checks if the nullable properties are not null under certain requirements</remarks>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsValid => ((IsEndDate == false && !EndDateTimeKind.HasValue) || (IsEndDate && EndDateTimeKind.HasValue))
                           && ((IsGas == false && !IsGasTagAware.HasValue) || (IsGas && IsGasTagAware.HasValue));
}
