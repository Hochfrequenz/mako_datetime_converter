using System;
using FluentAssertions;
using NUnit.Framework;
using MaKoDateTimeConverter;

namespace MaKoDateTimeConverterTests
{
    /// <summary>
    ///  Tests <see cref="DateTimeConversionConfiguration" />
    /// </summary>
    public class DateTimeConversionConfigurationTests
    {
        [Test]
        [TestCase("{\"source\":{\"isGas\":false, \"isEndDate\": false}, \"target\":{\"isGas\":true, \"isGasTagAware\": true, \"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}}")]
        public void Test_Deserialization(string dateTimeConversionConfigJson)
        {
            var dateTimeConversionConfig = System.Text.Json.JsonSerializer.Deserialize<DateTimeConversionConfiguration>(dateTimeConversionConfigJson);
            dateTimeConversionConfig.Should().NotBeNull(because: "the deserialization has to work");
            var newJsonString = System.Text.Json.JsonSerializer.Serialize(dateTimeConversionConfig);
            newJsonString.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        [TestCase("{\"source\":{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\":\"INCLUSIVE\"}, \"target\":{\"isGas\":false,\"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}}", true)]
        [TestCase("{\"source\":{\"isGas\":false, \"isEndDate\": false}, \"target\":{\"isGas\":true, \"isGasTagAware\": true, \"isEndDate\": false}}", false)] // conversion between strom and gas
        [TestCase("{\"source\":{\"isGas\":false, \"isEndDate\": false}, \"target\":{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}}", false)] // conversion between end and non-end date
        public void Test_Validation(string dateTimeConfigJson, bool isValid)
        {
            var dateTimeConfig = System.Text.Json.JsonSerializer.Deserialize<DateTimeConversionConfiguration>(dateTimeConfigJson);
            dateTimeConfig.Should().NotBeNull(because: "the deserialization has to work")
                .And.Subject.As<DateTimeConversionConfiguration>().IsValid.Should().Be(isValid);

            if (!isValid)
            {
                var arbitraryDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var convertAction = () => MaKoDateTimeConverter.MaKoDateTimeConverter.Convert(arbitraryDate, dateTimeConfig);
                convertAction.Should().Throw<ArgumentException>();
            }
        }
    }
}
