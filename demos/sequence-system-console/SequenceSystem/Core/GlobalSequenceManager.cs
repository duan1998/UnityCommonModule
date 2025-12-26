using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Core;

/// <summary>
/// 全局序列管理器： 统一入口 + 上下文管理
/// </summary>
public sealed class GlobalSequenceManager
{
    public static GlobalSequenceManager Instance { get; } = new ();

    private readonly DynamicQueueStep _mainSequence;
    private readonly SequencePlayer _player;
    private readonly Stack<IStepContainer> _contextStack = new();

    public bool IsPlaying => _player.IsPlaying;
    public bool HasPending => _mainSequence.HasPending;

    private GlobalSequenceManager()
    {
        _mainSequence = new DynamicQueueStep("MainSequence");
        _player = new SequencePlayer(new[] { _mainSequence });
    }

    /// <summary>
    /// 添加步骤到当前上下文（如果有）或主序列*（如果没有）
    /// </summary>
    /// <param name="step"></param>
    public void Add(ISequenceStep step)
    {
        if (_contextStack.Count > 0)
        {
            _contextStack.Peek().Add(step);
            Console.WriteLine($"[GSM] Add '{step.Name}' to context '{((ISequenceStep)_contextStack.Peek()).Name}'");
        }
        else
        {
            _mainSequence.Add(step);
            Console.WriteLine($"[GSM Add '{step.Name}' to MainSequence']");
        }
    }

    /// <summary>
    /// 添加多个步骤
    /// </summary>
    /// <param name="steps"></param>
    public void AddRange(IEnumerable<ISequenceStep> steps)
    {
        foreach (var step in steps)
            Add(step);
    }

    /// <summary>
    /// 压入上下文（由容器型 Step 在 Enter 时调用）
    /// </summary>
    public void PushContext(IStepContainer container)
    {
        _contextStack.Push(container);
        Console.WriteLine($"[GSM] PushContext '{((ISequenceStep)container).Name}', depth={_contextStack.Count}");
    }

    /// <summary>
    /// 弹出上下文（由容器型 Step 在完成/取消时调用）
    /// </summary>
    public void PopContext(IStepContainer container)
    {
        if (_contextStack.Count > 0 && _contextStack.Peek() == container)
        {
            _contextStack.Pop();
            Console.WriteLine($"[GSM] PopContext '{((ISequenceStep)container).Name}', depth={_contextStack.Count}");
        }
    }
    
    /// <summary>
    /// 检查并启动执行（如果有待执行的步骤且当前未执行）
    /// </summary>
    public void Check()
    {
        if (!_player.IsPlaying && _mainSequence.HasPending)
        {
            Console.WriteLine("[GSM] Check: starting player");
            _player.Play();
        }
    }
    
    /// <summary>
    /// 每帧调用
    /// </summary>
    public void Tick(float dt)
    {
        _player.Tick(dt);

        // 如果执行完了但还有待执行的步骤，重新启动
        if (!_player.IsPlaying && _mainSequence.HasPending)
        {
            _player.Play();
        }
    }
    
    /// <summary>
    /// 取消当前执行
    /// </summary>
    public void Cancel()
    {
        _player.Cancel();
        _contextStack.Clear();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause() => _player.Pause();

    /// <summary>
    /// 继续
    /// </summary>
    public void Resume() => _player.Resume();
}
