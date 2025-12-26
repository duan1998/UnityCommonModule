using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

/// <summary>
/// 模拟移动：打印开始移动，等待指定秒数后打印到达
/// </summary>
public sealed class SimMoveStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly string _actor;
    private readonly string _target;
    private readonly float _duration;
    private float _remain;

    public SimMoveStep(string name, string actor, string target, float duration = 1f)
    {
        Name = name;
        _actor = actor;
        _target = target;
        _duration = duration;
    }

    public void Enter()
    {
        IsDone = false;
        _remain = _duration;
        Console.WriteLine($"[Move] {_actor} 开始走向 {_target}...");
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        _remain -= dt;
        if (_remain <= 0f)
        {
            Console.WriteLine($"[Move] {_actor} 到达 {_target}");
            IsDone = true;
        }
    }

    public void Cancel()
    {
        Console.WriteLine($"[Move] {_actor} 移动被取消");
        IsDone = true;
    }
}
