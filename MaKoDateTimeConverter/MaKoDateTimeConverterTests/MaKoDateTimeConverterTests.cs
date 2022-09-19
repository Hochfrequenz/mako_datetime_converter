using System;
using FluentAssertions;
using NUnit.Framework;
using MaKoDateTimeConverter;

namespace MaKoDateTimeConverterTests;

/// <summary>
///  Tests <see cref="MaKoDateTimeConverter" />
/// </summary>
public class MaKoDateTimeConverterTests
{
    [Test]
    [TestCase("2022-12-31T23:00:00Z", true)]
    [TestCase("2022-06-15T22:00:00Z", true)]
    [TestCase("2022-12-15T23:00:00Z", true)]
    [TestCase("2022-12-31T22:00:00Z", false)]
    [TestCase("2022-06-15T23:00:00Z", false)]
    public void Test_Is_German_Midnight(string dateTimeString, bool isGermanMidnight)
    {
        var dto = DateTimeOffset.Parse(dateTimeString);
        var dt = dto.UtcDateTime;
        dt.IsGermanMidnight().Should().Be(isGermanMidnight);
        ((DateTime?)dt).IsGermanMidnight().Should().Be(isGermanMidnight);
        dto.IsGermanMidnight().Should().Be(isGermanMidnight);
        ((DateTimeOffset?)dto).IsGermanMidnight().Should().Be(isGermanMidnight);
    }

    [Test]
    public void Test_Is_German_Midnight_ArgumentException()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => localDt.IsGermanMidnight();
        checkAction.Should().Throw<ArgumentException>();
    }


    [Test]
    [TestCase("2022-12-31T23:00:00Z", false)]
    [TestCase("2022-06-15T22:00:00Z", false)]
    [TestCase("2022-01-01T05:00:00Z", true)]
    [TestCase("2022-12-31T05:00:00Z", true)]
    [TestCase("2022-06-15T04:00:00Z", true)]
    public void Test_Is_German_6am(string dateTimeString, bool isGerman6Am)
    {
        var dto = DateTimeOffset.Parse(dateTimeString);
        var dt = dto.UtcDateTime;
        dt.IsGerman6Am().Should().Be(isGerman6Am);
        ((DateTime?)dt).IsGerman6Am().Should().Be(isGerman6Am);
        dto.IsGerman6Am().Should().Be(isGerman6Am);
        ((DateTimeOffset?)dto).IsGerman6Am().Should().Be(isGerman6Am);
    }

    [Test]
    public void Test_Is_German_6Am_ArgumentException()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => localDt.IsGerman6Am();
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Test_Is_German_Midnight_Conversion_ArgumentException_Utc()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.Convert6AamToMidnight(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Test_Is_German_Midnight_Conversion_ArgumentException_6Am()
    {
        var localDt = new DateTime(2022, 1, 1, 3, 0, 0, DateTimeKind.Utc);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.Convert6AamToMidnight(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    [TestCase("2022-12-31T05:00:00Z", "2022-12-30T23:00:00Z")]
    [TestCase("2023-01-01T05:00:00Z", "2022-12-31T23:00:00Z")]
    [TestCase("2023-06-01T04:00:00Z", "2023-05-31T22:00:00Z")]
    [TestCase("2023-06-02T04:00:00Z", "2023-06-01T22:00:00Z")]
    [TestCase("2023-03-26T04:00:00Z", "2023-03-25T23:00:00Z")] // umstellung auf sommerzeit
    [TestCase("2023-10-29T05:00:00Z", "2023-10-28T22:00:00Z")] // umstellung auf winterzeit
    public void Test_German_6am_To_Midnight(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        MaKoDateTimeConverter.MaKoDateTimeConverter.Convert6AamToMidnight(dt).Should().Be(expected);
    }

    [Test]
    public void Test_Is_German_6Am_Conversion_ArgumentException_Utc()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.ConvertMidnightTo6Am(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Test_Is_German_6Am_Conversion_ArgumentException_6Am()
    {
        var localDt = new DateTime(2022, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.ConvertMidnightTo6Am(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    [TestCase("2022-12-31T23:00:00Z", "2023-01-01T05:00:00Z")]
    [TestCase("2022-12-30T23:00:00Z", "2022-12-31T05:00:00Z")]
    [TestCase("2023-06-30T22:00:00Z", "2023-07-01T04:00:00Z")]
    [TestCase("2023-05-30T22:00:00Z", "2023-05-31T04:00:00Z")]
    [TestCase("2023-03-25T23:00:00Z", "2023-03-26T04:00:00Z")] // sommer- auf winterzeit
    [TestCase("2023-10-28T22:00:00Z", "2023-10-29T05:00:00Z")] // winter- auf sommerzeit
    public void Test_German_Midnight_To_6Am(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        MaKoDateTimeConverter.MaKoDateTimeConverter.ConvertMidnightTo6Am(dt).Should().Be(expected);
    }

    [Test]
    [TestCase("2022-12-31T23:00:00Z", "2022-12-31T23:00:00Z")]
    [TestCase("2022-12-31T23:00:01Z", "2022-12-31T23:00:00Z")]
    [TestCase("2022-12-31T23:34:01Z", "2022-12-31T23:00:00Z")]
    [TestCase("2023-01-01T12:00:00Z", "2022-12-31T23:00:00Z")]
    public void Test_StripTime(string dateTimeString, string expectedResultString)
    {
        var dto = DateTimeOffset.Parse(dateTimeString);
        var dt = dto.UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString);
        dt.StripTime().Should().Be(expected.UtcDateTime);
        dto.StripTime().Should().Be(expected);
    }

    [Test]
    public void Test_StripTime_ArgumentException()
    {
        var localDt = new DateTime(2022, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var checkAction = () => localDt.StripTime();
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Test_AddGermanDay_ArgumentException_Utc()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.AddGermanDay(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Test_SubtractGermanDay_ArgumentException_Utc()
    {
        var localDt = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.SubtractGermanDay(localDt);
        checkAction.Should().Throw<ArgumentException>();
    }

    [TestCase("2023-06-01T04:00:00Z", "2023-05-31T22:00:00Z")]
    [TestCase("2023-12-01T05:00:00Z", "2023-11-30T23:00:00Z")]
    public void Test_GasTagAware_To_NonGasTagAware(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = false,
                IsGas = true,
                IsGasTagAware = true
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = false,
                IsGas = true,
                IsGasTagAware = false
            }
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [TestCase("2023-05-31T22:00:00Z", "2023-06-01T04:00:00Z")]
    [TestCase("2023-11-30T23:00:00Z", "2023-12-01T05:00:00Z")]
    public void Test_NonTagAware_To_GasTagAware(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = false,
                IsGas = true,
                IsGasTagAware = false
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = false,
                IsGas = true,
                IsGasTagAware = true
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [TestCase("2023-05-30T22:00:00Z", "2023-05-31T22:00:00Z")]
    [TestCase("2023-05-31T22:00:00Z", "2023-06-01T22:00:00Z")]
    [TestCase("2023-12-31T23:00:00Z", "2024-01-01T23:00:00Z")]
    [TestCase("2023-12-01T23:00:00Z", "2023-12-02T23:00:00Z")]
    public void Test_Strom_InclusiveEnd_To_ExclusiveEnd(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = false,
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Exclusive,
                IsGas = false,
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [TestCase("2023-05-30T04:00:00Z", "2023-05-31T04:00:00Z")]
    [TestCase("2023-12-30T05:00:00Z", "2023-12-31T05:00:00Z")]
    [TestCase("2023-12-01T05:00:00Z", "2023-12-02T05:00:00Z")]
    public void Test_Gas_InclusiveEnd_To_ExclusiveEnd(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = true,
                IsGasTagAware = true
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Exclusive,
                IsGas = true,
                IsGasTagAware = true
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [TestCase("2023-05-30T22:00:00Z", "2023-06-01T04:00:00Z")]
    [TestCase("2023-03-25T23:00:00Z", "2023-03-27T04:00:00Z")]
    [TestCase("2022-10-29T22:00:00Z", "2022-10-31T05:00:00Z")]
    public void Test_Gas_InclusiveEnd_To_ExclusiveEnd_And_Make_GasTagAware(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = true,
                IsGasTagAware = false
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Exclusive,
                IsGas = true,
                IsGasTagAware = true
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [TestCase("2022-01-01T00:00:00Z", "2022-01-01T00:00:00Z")]
    public void Test_Identity(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = false
            },
            Target = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = false,
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);

        var invertedConfig = conversion.GetInverted();
        expected.Convert(invertedConfig).Should().Be(dt);
    }

    [Test]
    [TestCase("2022-01-01T00:00:00Z", "2021-12-31T23:00:00Z")]
    [TestCase("2022-01-01T12:45:56Z", "2021-12-31T23:00:00Z")]
    public void Test_Stripping_Time_Source(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration
            {
                StripTime = true // <-- this is what is actually tested here
            },
            Target = new DateTimeConfiguration(),
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);
    }

    [Test]
    [TestCase("2022-01-01T00:00:00Z", "2021-12-31T23:00:00Z")]
    [TestCase("2022-01-01T12:45:56Z", "2021-12-31T23:00:00Z")]
    public void Test_Stripping_Time_Target(string dateTimeString, string expectedResultString)
    {
        var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
        var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
        var conversion = new DateTimeConversionConfiguration
        {
            Source = new DateTimeConfiguration(),
            Target = new DateTimeConfiguration
            {
                StripTime = true // <-- this is what is actually tested here
            },
        };
        var actual = dt.Convert(conversion);
        actual.Should().Be(expected);
    }
}
