namespace Omnius.Lxna.Ui.Desktop.Service.Internal;

public class ActionDebouncer
{
    private readonly Func<Task> _callback;
    private bool _pending = false;
    private bool _running = false;
    private readonly object _lockObject = new();

    public ActionDebouncer(Func<Task> callback)
    {
        _callback = callback;
    }

    public void Signal()
    {
        lock (_lockObject)
        {
            _pending = true;

            if (_running) return;
            _running = true;
            var _ = this.RunAsync();
        }
    }

    private async Task RunAsync()
    {
        await Task.Delay(1).ConfigureAwait(false);

        try
        {
            for (; ; )
            {
                lock (_lockObject)
                {
                    if (!_pending) return;
                    _pending = false;
                }

                await _callback().ConfigureAwait(false);
            }
        }
        finally
        {
            lock (_lockObject)
            {
                _running = false;
            }
        }
    }
}
