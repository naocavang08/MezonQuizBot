using System;
using System.Threading.Tasks;
using Xunit;
using Mezon_sdk.Managers;

namespace xUTest.Tests
{
    public class EventManagerTests
    {
        [Fact]
        public async Task EmitAsync_InvokesRegisteredHandler()
        {
            // Arrange
            var eventManager = new EventManager();
            bool handlerInvoked = false;
            
            Action<string> handler = (msg) => { handlerInvoked = true; };
            eventManager.On("test_event", handler);

            // Act
            await eventManager.EmitAsync("test_event", "hello");

            // Allow user handlers (fire-and-forget) to execute
            await Task.Delay(100);

            // Assert
            Assert.True(handlerInvoked);
            Assert.True(eventManager.HasListeners("test_event"));
        }

        [Fact]
        public async Task Off_RemovesHandler()
        {
            // Arrange
            var eventManager = new EventManager();
            bool handlerInvoked = false;
            
            Action<string> handler = (msg) => { handlerInvoked = true; };
            eventManager.On("test_event", handler);
            
            // Act
            eventManager.Off("test_event", handler);
            await eventManager.EmitAsync("test_event", "hello");
            await Task.Delay(100);

            // Assert
            Assert.False(handlerInvoked);
            Assert.False(eventManager.HasListeners("test_event"));
        }

        [Fact]
        public async Task EmitAsync_InvokesDefaultHandlerSynchronously()
        {
            // Arrange
            var eventManager = new EventManager();
            bool defaultHandlerInvoked = false;
            
            Action<string> handler = (msg) => { defaultHandlerInvoked = true; };
            // isDefault = true
            eventManager.On("test_event", handler, true);

            // Act
            await eventManager.EmitAsync("test_event", "hello");

            // Assert
            Assert.True(defaultHandlerInvoked);
        }
    }
}
