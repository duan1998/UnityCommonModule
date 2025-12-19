namespace ConsoleApp1;

public sealed class SubscriptionOwner:IDisposable
{
    private readonly List<IDisposable> _tokens = new();

    public void Add(IDisposable token) => _tokens.Add(token);

    public void Dispose()
    {
        for (var i = 0; i < _tokens.Count; i++)
            _tokens[i].Dispose();
        
        _tokens.Clear();
    }
}