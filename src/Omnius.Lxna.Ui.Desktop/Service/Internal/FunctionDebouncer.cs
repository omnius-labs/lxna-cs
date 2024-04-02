namespace Omnius.Lxna.Ui.Desktop.Service.Internal;

public class FunctionDebouncer<T> where T : notnull
{
    private readonly Func<T, Task> _callback;
    private T? _lastParam;
    private bool _pending = false;
    private bool _running = false;
    private readonly object _lockObject = new();

    public FunctionDebouncer(Func<T, Task> callback)
    {
        _callback = callback;
    }

    public void Call(T param)
    {
        lock (_lockObject)
        {
            _lastParam = param;
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
                T? lastParam;

                lock (_lockObject)
                {
                    if (!_pending) return;
                    lastParam = _lastParam!;
                    _pending = false;
                }

                await _callback(lastParam).ConfigureAwait(false);
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
