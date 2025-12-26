using SequenceSystem.Core;

namespace SequenceSystem.Steps.Common;

public sealed class SimAnimStep:ISequenceStep
{
    public string Name { get; }
    public bool IsDone { get; private set; }
    
    private readonly string _actor;
    private readonly string _animName;
    private readonly float _duration;
    private float _remain;

    public SimAnimStep(string Name, string actor, string animName, float duration = 1f)
    {
        this.Name = Name;
        this._actor = actor;
        this._animName = animName;
        this._duration = duration;
    }

    public void Enter()
    {
        IsDone = false;
        _remain = _duration;
        Console.WriteLine($"[Anim] {_actor} 开始播放动画 [{_animName}]");
    }

    public void Tick(float dt)
    {
        if (IsDone) return;
        
        _remain -= dt;
        if (_remain <= 0f)
        {
            Console.WriteLine($"[Anim] {_actor} 动画 [{_animName}] 播放完成");
            IsDone = true;
        }
    }

    public void Cancel()
    {
        Console.WriteLine($"[Anim] {_actor} 动画 [{_animName}] 被取消");
        IsDone = true;
    }
}
