using NetSecLab.Core.Interfaces;

namespace NetSecLab.Infrastructure.Events;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _syncRoot = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        Type eventType = typeof(TEvent);

        lock (_syncRoot)
        {
            if (!_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        return new EventSubscription(() => Unsubscribe(handler));
    }

    public void Publish<TEvent>(TEvent eventData)
    {
        List<Delegate> handlersCopy;
        Type eventType = typeof(TEvent);

        lock (_syncRoot)
        {
            if (!_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                return;
            }

            handlersCopy = handlers.ToList();
        }

        foreach (Delegate handler in handlersCopy)
        {
            if (handler is Action<TEvent> typedHandler)
            {
                typedHandler(eventData);
            }
        }
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        Type eventType = typeof(TEvent);

        lock (_syncRoot)
        {
            if (_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                handlers.Remove(handler);
            }
        }
    }
}
