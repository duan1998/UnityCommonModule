using System.Data;
using System.Security.Cryptography;
using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

/// <summary>
/// 动态队列 Step： 可以在运行时追加子步骤
/// </summary>
public sealed class DynamicQueueStep:ISequenceStep, IStepContainer
{
    public string Name { get; }
    public bool IsDone { get; private set; }
    public bool HasPending => _queue.Count > 0 || (_current != null && !_current.IsDone);

    private readonly Queue<ISequenceStep> _queue = new ();
    private ISequenceStep? _current;

    public DynamicQueueStep(string name, IEnumerable<ISequenceStep>? initialSteps = null)
    {
        Name = name;
        if (initialSteps != null)
        {
            foreach (var step in initialSteps)
                _queue.Enqueue(step);
        }
    }

    public void Add(ISequenceStep step)
    {
        _queue.Enqueue(step);
    }

    public void AddRange(IEnumerable<ISequenceStep> steps)
    {
        foreach (var step in steps)
            _queue.Enqueue(step);
    }

    public void Enter()
    {
        IsDone = false;
        _current = null;
        
        // 注册为当前上下文（仅当作为主序列容器使用时）
        // 注意：DynamicQueueStep 作为 MainSequence 时会被 MainSequenceManager 直接管理
        // 这里的 PushContext 主要用于兼容性

        TryAdvance();
    }

    public void Tick(float dt)
    {
        if (IsDone)
            return;
        
        if(_current != null && !_current.IsDone)
            _current.Tick(dt);

        if (_current == null || _current.IsDone)
            TryAdvance();
    }

    public void TryAdvance()
    {
        if (_queue.Count == 0)
        {
            OnExit();
            IsDone = true;
            return;
        }
        
        _current = _queue.Dequeue();
        _current.Enter();
    }

    public void Cancel()
    {
        if (IsDone) return;
        
        _current?.Cancel();
        _queue.Clear();
        OnExit();
        IsDone = true;
    }

    private void OnExit()
    {
        // 注意：DynamicQueueStep 作为 MainSequence 时不需要 PopContext
        // 上下文管理由 DynamicParentStep 负责
    }
}
