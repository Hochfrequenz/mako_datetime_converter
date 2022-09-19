using FluentAssertions;
using NUnit.Framework;
using MaKoDateTimeConverter;

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
        [TestCase("{\"isGas\":false, \"isEndDate\": false, \"endDateTimeKind\": \"INCLUSIVE\"}", false)] // enddatetimekind given but is no enddate
        [TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\"}", true)]
        [TestCase("{\"isGas\":true, \"isEndDate\": false}", false)] // is gas but no awareness given
        [TestCase("{\"isGas\":false, \"isGasTagAware\": true, \"isEndDate\": false}", false)]// gastagawareness given but is no gas
        [TestCase("{\"isGas\":true, \"isGasTagAware\": true, \"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}", true)]
        public void Test_Validation(string dateTimeConfigJson, bool isValid)
        {
            var dateTimeConfig = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(dateTimeConfigJson);
            dateTimeConfig.Should().NotBeNull(because: "the deserialization has to work")
                .And.Subject.As<DateTimeConfiguration>().IsValid.Should().Be(isValid);
        }
    }
}
