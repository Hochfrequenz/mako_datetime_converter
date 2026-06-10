using System;
using AwesomeAssertions;
using MaKoDateTimeConverter;
using NUnit.Framework;

namespace MaKoDateTimeConverterTests
{
    /// <summary>
    ///  Tests <see cref="DateTimeConfiguration" />
    /// </summary>
    public class DateTimeConfigurationTests
    {
        [Test]
        [TestCase("{\"isGas\":false, \"isEndDate\": false}", true)] // simple, strom start
        [TestCase("{\"isGas\":false, \"isEndDate\": true}", false)] // no enddatetimekind given
        [TestCase(
            "{\"isGas\":false, \"isEndDate\": false, \"endDateTimeKind\": \"INCLUSIVE\"}",
            false
        )] // enddatetimekind given but is no enddate
        [TestCase(
            "{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\"}",
            true
        )]
        [TestCase("{\"isGas\":true, \"isEndDate\": false}", false)] // is gas but no awareness given
        [TestCase("{\"isGas\":false, \"isGasTagAware\": true, \"isEndDate\": false}", false)] // gastagawareness given but is no gas
        [TestCase(
            "{\"isGas\":true, \"isGasTagAware\": true, \"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}",
            true
        )]
        // New cases for Resolution validation:
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\"}", false)] // inclusive, no resolution
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"PT1S\"}", true)] // inclusive with 1s
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\", \"resolution\": \"PT1S\"}", false)] // exclusive must not have resolution
        [TestCase("{\"isGas\":false, \"isEndDate\": false, \"resolution\": \"PT1S\"}", false)] // non-end-date must not have resolution
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"-PT1S\"}", false)] // negative resolution
        public void Test_Validation(string dateTimeConfigJson, bool isValid)
        {
            var dateTimeConfig = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(
                dateTimeConfigJson
            );
            dateTimeConfig
                .Should()
                .NotBeNull(because: "the deserialization has to work")
                .And.Subject.As<DateTimeConfiguration>()
                .IsValid.Should()
                .Be(isValid);
        }

        [Test]
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"PT1S\"}", "00:00:01")]
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"P1D\"}", "1.00:00:00")]
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\"}", null)]
        public void Test_Resolution_Deserialization(string json, string? expectedResolutionString)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(json);
            config.Should().NotBeNull();
            if (expectedResolutionString is null)
                config!.Resolution.Should().BeNull();
            else
                config!.Resolution.Should().Be(TimeSpan.Parse(expectedResolutionString));
        }

        [Test]
        [TestCase("PT1S")]
        [TestCase("P1D")]
        [TestCase("PT0.001S")]
        public void Test_Resolution_Serialization_RoundTrip(string iso8601Duration)
        {
            var expected = System.Xml.XmlConvert.ToTimeSpan(iso8601Duration);
            var config = new DateTimeConfiguration
            {
                IsEndDate = true,
                EndDateTimeKind = EndDateTimeKind.Inclusive,
                IsGas = false,
                Resolution = expected,
            };
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            json.Should().Contain("\"resolution\":"); // key is present in JSON
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(json);
            deserialized!.Resolution.Should().Be(expected); // round-trip equality is what matters
        }
    }
}
