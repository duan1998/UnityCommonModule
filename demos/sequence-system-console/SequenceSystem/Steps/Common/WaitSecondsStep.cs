using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public sealed class WaitSecondsStep  : ISequenceStep
{
    public string Name { get; }
    private float _remain;
    public bool IsDone => _remain <= 0f;

    public WaitSecondsStep(string name, float seconds)
    {
        Name = name;
        _remain = seconds;
    }

    public void Enter()
    {
        Console.WriteLine($"[Step] {Name}:wait {_remain:0.$###}s");
    }

    public void Tick(float dt)
    {
        _remain -= dt;
    }

    public void Cancel()
    {
        _remain = 0f;
    }
}
