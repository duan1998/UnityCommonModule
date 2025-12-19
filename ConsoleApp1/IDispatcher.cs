namespace ConsoleApp1;

public interface IDispatcher
{
    void Post(Action action);
}

public sealed class ImmediateDispatcher : IDispatcher
{
    public void Post(Action action) => action();
}

public sealed class MainThreadDispatcher : IDispatcher
{
    private readonly Queue<Action> _queue = new();
    private readonly object _lock = new();
    
    public void Post(Action action)
    {
        lock (_lock)
        {
            _queue.Enqueue(action);
        }
    }

    public void Pump(int maxActions = 1000)
    {
        for (int i = 0; i < maxActions; i++)
        {
            Action? a;
            lock (_lock)
            {
                if (_queue.Count == 0)
                    return;
                a = _queue.Dequeue();
            }

            a();
        }
    }
}