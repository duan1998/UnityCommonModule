using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public sealed class LogStep : ISequenceStep
{
    public string Name { get; }
    private readonly string _msg;
    public bool IsDone { get; private set; }

    public LogStep(string name, string msg)
    {
        Name = name;
        _msg = msg;
    }

    public void Enter()
    {
        Console.WriteLine($"[Step] {Name}: {_msg}");
        IsDone = true; 
    }

    public void Tick(float dt)
    {
        
    }

    public void Cancel()
    {
        IsDone = true;
    }
}
