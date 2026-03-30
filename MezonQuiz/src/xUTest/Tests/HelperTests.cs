using System.Text.Json;
using Mezon_sdk.Constants;
using Mezon_sdk.Utils;

namespace xUTest.Tests
{
    public class HelperTests
    {
        [Theory]
        [InlineData((int)ChannelType.ChannelTypeDm, (int)ChannelStreamMode.StreamModeDm)]
        [InlineData((int)ChannelType.ChannelTypeGroup, (int)ChannelStreamMode.StreamModeGroup)]
        [InlineData((int)ChannelType.ChannelTypeChannel, (int)ChannelStreamMode.StreamModeChannel)]
        [InlineData((int)ChannelType.ChannelTypeThread, (int)ChannelStreamMode.StreamModeThread)]
        [InlineData(999, 0)]
        public void ConvertChannelTypeToChannelMode_ReturnsExpectedValue(int channelType, int expectedMode)
        {
            var mode = Helper.ConvertChannelTypeToChannelMode(channelType);

            Assert.Equal(expectedMode, mode);
        }

        [Theory]
        [InlineData("12345", true)]
        [InlineData(12345, true)]
        [InlineData(12345L, true)]
        [InlineData(12.5, true)]
        [InlineData("abc", false)]
        [InlineData("12a", false)]
        public void IsValidUserId_ReturnsExpectedResult(object userId, bool expected)
        {
            var result = Helper.IsValidUserId(userId);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("https://socket.mezon.ai", "socket.mezon.ai", "443", true)]
        [InlineData("http://localhost:8080", "localhost", "8080", false)]
        [InlineData("http://example.com/path", "example.com", "80", false)]
        public void ParseUrlToHostAndSsl_ReturnsHostPortAndSsl(string url, string expectedHost, string expectedPort, bool expectedUseSsl)
        {
            var result = Helper.ParseUrlToHostAndSsl(url);

            Assert.Equal(expectedHost, result.Host);
            Assert.Equal(expectedPort, result.Port);
            Assert.Equal(expectedUseSsl, result.UseSSL);
        }

        [Fact]
        public void GenerateSnowflakeId_ReturnsIncreasingPositiveIds()
        {
            var first = Helper.GenerateSnowflakeId();
            var second = Helper.GenerateSnowflakeId();

            Assert.True(first > 0);
            Assert.True(second > 0);
            Assert.True(second > first);
        }

        [Theory]
        [InlineData(12, 12)]
        [InlineData("34", 34)]
        [InlineData(null, null)]
        [InlineData("not-a-number", null)]
        public void ToInt_HandlesPrimitiveInputs(object? value, int? expected)
        {
            var result = Helper.ToInt(value);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToInt_HandlesJsonElementNumberAndString()
        {
            using var numberDoc = JsonDocument.Parse("123");
            using var stringDoc = JsonDocument.Parse("\"456\"");

            Assert.Equal(123, Helper.ToInt(numberDoc.RootElement));
            Assert.Equal(456, Helper.ToInt(stringDoc.RootElement));
        }
    }
}
