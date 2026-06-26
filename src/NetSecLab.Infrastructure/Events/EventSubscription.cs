namespace NetSecLab.Infrastructure.Events;

internal sealed class EventSubscription : IDisposable
{
    private readonly Action _disposeAction;
    private bool _isDisposed;

    public EventSubscription(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _disposeAction();
        _isDisposed = true;
    }
}
