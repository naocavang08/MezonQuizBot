using System;
using System.Threading.Tasks;
using Xunit;
using Mezon_sdk.Socket;
using Mezon_sdk.Models;

namespace xUTest.Tests
{
    public class DefaultSocketTests
    {
        [Fact]
        public void GenerateCid_ReturnsSequentialIds()
        {
            // Arrange
            var socket = new DefaultSocket("localhost", "8080");

            // Act
            var cid1 = socket.GenerateCid();
            var cid2 = socket.GenerateCid();

            // Assert
            Assert.NotEqual(cid1, cid2);
            // cid sequence usually increments
            Assert.True(long.Parse(cid1) < long.Parse(cid2));
        }

        [Fact]
        public void DefaultSocket_Initialization_SetsPropertiesCorrectly()
        {
            // Arrange
            string host = "api.mezon.com";
            string port = "443";
            bool useSsl = true;

            // Act
            var socket = new DefaultSocket(host, port, useSsl);

            // Assert
            Assert.Equal(host, socket.Host);
            Assert.Equal(port, socket.Port);
            Assert.True(socket.UseSsl);
            Assert.Equal("wss", socket.WebsocketScheme);
        }
        
        [Fact]
        public void DefaultSocket_Initialization_WithNoSsl_ReturnsWsScheme()
        {
            // Arrange
            var socket = new DefaultSocket("localhost", "8080", false);

            // Assert
            Assert.Equal("ws", socket.WebsocketScheme);
        }
    }
}
