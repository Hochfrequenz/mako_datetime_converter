﻿using System;
using FluentAssertions;
using MaKoDateTimeConverter;
using NUnit.Framework;

namespace MaKoDateTimeConverterTests;

/// <summary>
/// A minimal working example to demonstrate how to use this library
/// </summary>
public class MinimalWorkingExample
{
    [Test]
    public void Test_Mwe()
    {
        // assume you received an edifact message and were able to parse e.g. a contract end date.
        // this library assumes that you already have a utc-datetime available

        // this is e.g. the end date of a gas supply contract that ends at the end of 2022 
        var myDateTimeFromEdifact = new DateTime(2023, 1, 1, 5, 0, 0, DateTimeKind.Utc); // end of the last Gas-Tag in 2022

        // now we want to hand this date over to a system that doesn't know of the gas quirks and handles end date inclusively
        var config = new DateTimeConversionConfiguration
        {
            // the source describes where our datetime comes from
            Source = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Exclusive,
                IsGas = true,
                IsGasTagAware = true
            },
            // the target describes into which kind of datetime we want to convert it
            Target = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = true,
                IsGasTagAware = false
            },
        };
        var converted = MaKoDateTimeConverter.MaKoDateTimeConverter.Convert(myDateTimeFromEdifact, config);
        // now the converted date is unaware of the Gas-Tag and meant inclusively
        converted.Should().Be(new DateTime(2022, 12, 30, 23, 0, 0, DateTimeKind.Utc));
    }
}
