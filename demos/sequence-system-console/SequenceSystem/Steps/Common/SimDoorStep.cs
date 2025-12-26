using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public class SimDoor
{
    public string Name {get;}
    public bool IsOpen { get; set; }

    public SimDoor(string name, bool isOpen = false)
    {
        Name = name;
        IsOpen = isOpen;
    }
}

public sealed class SimDoorStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }

    private readonly SimDoor _door;
    private readonly bool _open; // true=开门，false=关门
    private readonly float _duration;
    private float _remain;

    public SimDoorStep(string name, SimDoor door, bool open, float duration = 0.5f)
    {
        Name = name;
        _door = door;
        _open = open;
        _duration = duration;
    }

    public void Enter()
    {
        IsDone = false;
        _remain = _duration;
        var action = _open ? "开门" : "关门";
        Console.WriteLine($"[Door] {_door.Name} 开始 {action}...");
    }

    public void Tick(float dt)
    {
        if (IsDone) return;
        _remain -= dt;
        if (_remain <= 0f)
        {
            _door.IsOpen = _open;
            var state = _open ? "已打开" : "已关闭";
            Console.WriteLine($"[Door] {_door.Name} {state}");
            IsDone = true;
        }
    }

    public void Cancel()
    {
        Console.WriteLine($"[Door] {_door.Name} 操作被取消");
        IsDone = true;
    }
}
