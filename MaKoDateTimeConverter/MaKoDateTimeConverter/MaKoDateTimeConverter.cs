using System;

namespace MaKoDateTimeConverter;

/// <summary>
/// a converter that takes a datetime and converts it to another datetime, according to its configuration
/// </summary>
public static class MaKoDateTimeConverter
{
    /// <summary>
    /// the Berlin time zone as serialized string
    /// </summary>
    /// <remarks>Created using <see cref="TimeZoneInfo.ToSerializedString"/></remarks>
    private const string BerlinTimeSerialized = "Central Europe Standard Time;60;(UTC+01:00) Belgrad, Bratislava (Pressburg), Budapest, Ljubljana, Prag;Mitteleuropäische Zeit ;Mitteleuropäische Sommerzeit ;[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];";

    /// <summary>
    /// the timezone in Germany
    /// </summary>
    private static readonly TimeZoneInfo BerlinTime;

    /// <summary>
    /// a static constructor to initialize <see cref="BerlinTime"/>
    /// </summary>
    static MaKoDateTimeConverter()
    {
        BerlinTime = TimeZoneInfo.FromSerializedString(BerlinTimeSerialized);
    }

    /// <summary>
    /// check if this is the begin of a German stromtag
    /// </summary>
    /// <param name="dt"></param>
    /// <returns>returns true iff the given datetime is midnight in Germany</returns>
    public static bool IsGermanMidnight(this DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }

        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        return germanTime.Hour == 0 && germanTime.Minute == 0 && germanTime.Second == 0 && germanTime.Millisecond == 0;
    }

    /// <summary>
    /// <inheritdoc cref="IsGerman6Am(System.DateTime)"/>
    /// </summary>
    public static bool? IsGermanMidnight(this DateTime? dt) => dt?.IsGermanMidnight();

    /// <summary>
    /// <inheritdoc cref="IsGermanMidnight(System.DateTime)"/>
    /// </summary>
    public static bool IsGermanMidnight(this DateTimeOffset dto) => dto.UtcDateTime.IsGermanMidnight();

    /// <summary>
    /// <inheritdoc cref="IsGerman6Am(System.DateTime)"/>
    /// </summary>
    public static bool? IsGermanMidnight(this DateTimeOffset? dto) => dto?.IsGermanMidnight();

    /// <summary>
    /// Converts a German 6AM to German midnight of the same German day
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the input is not utc or not 6am</exception>
    public static DateTime Convert6AamToMidnight(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        if (!IsGerman6Am(dt))
        {
            throw new ArgumentException("You must only use German 6am as input", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        // the offset between strom and gastag is _not_ always 6hours. On the days of the DST/non-DST transition it may be 7 or 5 hours
        var germanMidnightOfSameDay = new DateTime(germanTime.Year, germanTime.Month, germanTime.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(germanMidnightOfSameDay, BerlinTime);
    }


    /// <summary>
    /// Converts a German midnight to 6Am of the same German day
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the input is not utc or not midnight</exception>
    public static DateTime ConvertMidnightTo6Am(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        if (!IsGermanMidnight(dt))
        {
            throw new ArgumentException("You must only use German midnight as input", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        // the offset between strom and gastag is _not_ always 6hours. On the days of the DST/non-DST transition it may be 7 or 5 hours
        var german6AmOfSameDay = new DateTime(germanTime.Year, germanTime.Month, germanTime.Day, 6, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(german6AmOfSameDay, BerlinTime);
    }

    /// <summary>
    /// Adds 1 German day
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the input is not utc</exception>
    public static DateTime AddGermanDay(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        var nextDay = germanTime + TimeSpan.FromDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(nextDay, BerlinTime);
    }

    /// <summary>
    /// Subtracts 1 German day
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the input is not utc</exception>
    public static DateTime SubtractGermanDay(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        var nextDay = germanTime - TimeSpan.FromDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(nextDay, BerlinTime);
    }

    /// <summary>
    /// remove all hours, minutes, seconds, milliseconds (in german local time) from the given <paramref name="dt"/>.
    /// This is similar to a "round down" or "floor" in German local time.
    /// </summary>
    public static DateTime StripTime(this DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        var result = new DateTime(germanTime.Year, germanTime.Month, germanTime.Day, 0, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(result, BerlinTime);
    }

    /// <summary>
    /// <inheritdoc cref="StripTime(System.DateTime)"/>
    /// </summary>
    public static DateTimeOffset StripTime(this DateTimeOffset dto) => new(dto.UtcDateTime.StripTime());

    /// <summary>
    /// check if this is the begin of a German Gastag
    /// </summary>
    /// <param name="dt"></param>
    /// <returns>returns true iff the given datetime is 06:00am in Germany</returns>
    public static bool IsGerman6Am(this DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"The {nameof(dt.Kind)} has to be {DateTimeKind.Utc} but was {dt.Kind}", nameof(dt));
        }
        var germanTime = TimeZoneInfo.ConvertTimeFromUtc(dt, BerlinTime);
        return germanTime.Hour == 6 && germanTime.Minute == 0 && germanTime.Second == 0 && germanTime.Millisecond == 0;
    }

    /// <summary>
    /// <inheritdoc cref="IsGerman6Am(System.DateTime)"/>
    /// </summary>
    public static bool? IsGerman6Am(this DateTime? dt) => dt?.IsGerman6Am();

    /// <summary>
    /// <inheritdoc cref="IsGerman6Am(System.DateTime)"/>
    /// </summary>
    public static bool IsGerman6Am(this DateTimeOffset dto) => dto.UtcDateTime.IsGerman6Am();

    /// <summary>
    /// <inheritdoc cref="IsGerman6Am(System.DateTimeOffset)"/>
    /// </summary>
    public static bool? IsGerman6Am(this DateTimeOffset? dto) => dto?.IsGerman6Am();

    /// <summary>
    /// converts the given <paramref name="sourceDateTime"/> to a target by applying all transformations which are derived from the given <paramref name="conversionConfiguration"/> where <paramref name="sourceDateTime"/> is described by <see cref="DateTimeConversionConfiguration.Source"/>
    /// </summary>
    /// <param name="sourceDateTime">the source date time (input value of the conversion)</param>
    /// <param name="conversionConfiguration">describes the steps necessary to convert the given <paramref name="sourceDateTime"/></param>
    /// <returns></returns>
    public static DateTime Convert(this DateTime sourceDateTime, DateTimeConversionConfiguration conversionConfiguration)
    {
        if (!conversionConfiguration.IsValid)
        {
            throw new ArgumentException("The configuration is invalid", nameof(conversionConfiguration));
        }

        if (sourceDateTime.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException($"The kind of the provided datetime must not be unspecified but was {sourceDateTime.Kind}", nameof(sourceDateTime));
        }
        DateTimeOffset result = sourceDateTime; // this is an implicit conversion to a Utc DateTime
        if (conversionConfiguration.Source.StripTime)
        {
            result = result.StripTime();
        }

        if (conversionConfiguration.Source == conversionConfiguration.Target)
        {
            // both are the same, no conversion needed
            return result.UtcDateTime;
        }

        if (conversionConfiguration.Source.IsGas) // this implies that the target is also gas, because otherwise the configuration would be invalid
        {
            // handle gas stuff here
            if (conversionConfiguration.Source.IsGasTagAware!.Value && !conversionConfiguration.Target.IsGasTagAware!.Value)
            {
                // convert from gas-tag to non-gas-tag
                if (IsGerman6Am(result))
                {
                    result = Convert6AamToMidnight(result.UtcDateTime);
                }
            }
            if (!conversionConfiguration.Source.IsGasTagAware!.Value && conversionConfiguration.Target.IsGasTagAware!.Value)
            {
                // convert from non-gastag to gas-tag
                if (IsGermanMidnight(result))
                {
                    result = ConvertMidnightTo6Am(result.UtcDateTime);
                }
            }
        }
        else
        {
            // handle strom-only stuff here
        }

        if (conversionConfiguration.Source.IsEndDate && conversionConfiguration.Target.IsEndDate &&
            conversionConfiguration.Source.EndDateTimeKind!.Value != conversionConfiguration.Target.EndDateTimeKind!.Value)
        {
            if (conversionConfiguration.Source.EndDateTimeKind == EndDateTimeKind.Inclusive) // implicit: target is exclusive
            {
                // convert from inclusive to exclusive
                result = AddGermanDay(result.UtcDateTime);
            }

            if (conversionConfiguration.Source.EndDateTimeKind == EndDateTimeKind.Exclusive) // implicit: target is inclusive
            {
                // convert from exclusive to inclusive
                result = SubtractGermanDay(result.UtcDateTime);
            }
        }

        if (conversionConfiguration.Target.StripTime)
        {
            result = result.StripTime();
        }

        return result.UtcDateTime;
    }

    /// <summary>
    /// <inheritdoc cref="System.Convert"/>
    /// </summary>
    /// <param name="sourceDateTime"></param>
    /// <param name="conversionConfiguration"></param>
    /// <returns></returns>
    public static DateTimeOffset Convert(this DateTimeOffset sourceDateTime, DateTimeConversionConfiguration conversionConfiguration) =>
        sourceDateTime.UtcDateTime.Convert(conversionConfiguration);
}
