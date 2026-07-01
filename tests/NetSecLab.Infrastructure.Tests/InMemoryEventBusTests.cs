using NetSecLab.Infrastructure.Events;

namespace NetSecLab.Infrastructure.Tests;

public class InMemoryEventBusTests
{
    [Fact]
    public void Publish_Should_Call_Subscribed_Handler()
    {
        InMemoryEventBus eventBus = new();
        string? receivedMessage = null;

        eventBus.Subscribe<TestEvent>(ev => receivedMessage = ev.Message);
        eventBus.Publish(new TestEvent("packet-generated"));

        Assert.Equal("packet-generated", receivedMessage);
    }

    [Fact]
    public void Dispose_Subscription_Should_Unsubscribe_Handler()
    {
        InMemoryEventBus eventBus = new();
        int callCount = 0;

        IDisposable subscription = eventBus.Subscribe<TestEvent>(_ => callCount++);
        eventBus.Publish(new TestEvent("before-dispose"));

        subscription.Dispose();
        eventBus.Publish(new TestEvent("after-dispose"));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Publish_Should_Use_Handler_Copy_When_Handler_Unsubscribes_During_Publish()
    {
        InMemoryEventBus eventBus = new();
        int callCount = 0;
        IDisposable? subscription = null;

        subscription = eventBus.Subscribe<TestEvent>(_ =>
        {
            callCount++;
            subscription!.Dispose();
        });

        eventBus.Subscribe<TestEvent>(_ => callCount++);

        eventBus.Publish(new TestEvent("first"));
        eventBus.Publish(new TestEvent("second"));

        Assert.Equal(3, callCount);
    }

    private sealed record TestEvent(string Message);
}
