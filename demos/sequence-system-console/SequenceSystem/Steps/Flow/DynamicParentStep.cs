using SequenceSystem.Core;

namespace SequenceSystem.Steps.Flow;

/// <summary>
/// 动态父步骤包装器：
/// - 包装任何 ISequenceStep，让它能够在执行时产生子步骤
/// - 当被包装的 step 执行时，如果调用 gsm.Add()，新步骤会成为它的子步骤
/// - IsDone = 内部step完成 AND 所有子步骤完成
/// </summary>
public sealed class DynamicParentStep : ISequenceStep, IStepContainer
{
    private readonly ISequenceStep _inner;
    private readonly Queue<ISequenceStep> _children = new();
    private ISequenceStep? _currentChild;
    private bool _innerDone;
    private bool _exited;

    public string Name => _inner.Name;
    
    /// <summary>
    /// 完成条件：内部step完成 AND 没有待执行的子步骤 AND 当前子步骤完成
    /// </summary>
    public bool IsDone => _innerDone 
                          && _children.Count == 0 
                          && (_currentChild == null || _currentChild.IsDone);

    public bool HasPending => _children.Count > 0 || (_currentChild != null && !_currentChild.IsDone);

    public DynamicParentStep(ISequenceStep inner)
    {
        _inner = inner;
    }

    public void Enter()
    {
        _innerDone = false;
        _currentChild = null;
        _exited = false;
        _children.Clear();

        // 把自己设为当前上下文，这样 _inner.Enter() 里的 gsm.Add() 会进入 _children
        SequenceManager.Instance.Main.PushContext(this);
        
        Console.WriteLine($"[DynamicParent] Enter '{Name}'");
        _inner.Enter();
        _innerDone = _inner.IsDone;
        
        // 如果内部 step 是同步完成的，且没有产生子步骤，直接退出上下文
        if (_innerDone && _children.Count == 0)
        {
            OnExit();
        }
    }

    public void Tick(float dt)
    {
        if (IsDone) return;

        // 1. 如果内部 step 还没完成，先驱动它
        if (!_innerDone)
        {
            _inner.Tick(dt);
            _innerDone = _inner.IsDone;
        }

        // 2. 内部完成后，依次执行子步骤
        if (_innerDone)
        {
            // 如果当前没有子步骤在执行，取下一个
            if (_currentChild == null || _currentChild.IsDone)
            {
                if (_children.Count > 0)
                {
                    _currentChild = _children.Dequeue();
                    Console.WriteLine($"[DynamicParent] '{Name}' starting child '{_currentChild.Name}'");
                    _currentChild.Enter();
                }
            }

            // 驱动当前子步骤
            if (_currentChild != null && !_currentChild.IsDone)
            {
                _currentChild.Tick(dt);
            }

            // 检查是否全部完成
            if (_currentChild == null || _currentChild.IsDone)
            {
                if (_children.Count == 0)
                {
                    OnExit();
                }
            }
        }
    }

    public void Cancel()
    {
        Console.WriteLine($"[DynamicParent] Cancel '{Name}'");
        
        // 取消内部 step
        if (!_innerDone)
        {
            _inner.Cancel();
        }
        
        // 取消当前子步骤
        _currentChild?.Cancel();
        
        // 取消所有待执行的子步骤
        foreach (var child in _children)
        {
            child.Cancel();
        }
        _children.Clear();
        
        OnExit();
    }

    /// <summary>
    /// IStepContainer 实现：添加子步骤
    /// 注意：添加的步骤会被自动包装成 DynamicParentStep（在 SequenceSystem.Main.Add 里处理）
    /// </summary>
    public void Add(ISequenceStep step)
    {
        Console.WriteLine($"[DynamicParent] '{Name}' received child '{step.Name}'");
        _children.Enqueue(step);
    }

    private void OnExit()
    {
        if (_exited) return;
        _exited = true;
        
        Console.WriteLine($"[DynamicParent] Exit '{Name}'");
        SequenceManager.Instance.Main.PopContext(this);
    }
}
