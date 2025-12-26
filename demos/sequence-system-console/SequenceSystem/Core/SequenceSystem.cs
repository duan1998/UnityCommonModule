using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Core;

#region Facade Extension Methods

/// <summary>
/// Facade 扩展方法 - 让业务开发者可以用简单的方式添加步骤
/// 业务代码不需要知道 ISequenceStep 的存在
/// </summary>
public static class SequenceFacade
{
    #region MainSequenceManager Extensions

    /// <summary>
    /// 添加一个同步动作（最简单的用法）
    /// </summary>
    /// <example>
    /// SequenceManager.Instance.Main.Run(() => Debug.Log("Hello"));
    /// </example>
    public static void Run(this MainSequenceManager main, Action action)
    {
        main.Add(new ActionStep(action));
    }

    /// <summary>
    /// 添加一个同步动作（带名称）
    /// </summary>
    public static void Run(this MainSequenceManager main, string name, Action action)
    {
        main.Add(new ActionStep(name, action));
    }

    /// <summary>
    /// 添加延迟
    /// </summary>
    /// <example>
    /// SequenceManager.Instance.Main.Delay(1.5f);
    /// </example>
    public static void Delay(this MainSequenceManager main, float seconds)
    {
        main.Add(new DelayStep(seconds));
    }

    /// <summary>
    /// 添加条件步骤
    /// </summary>
    /// <example>
    /// SequenceManager.Instance.Main.If(() => hp > 0, () => Debug.Log("Alive"));
    /// </example>
    public static void If(this MainSequenceManager main, Func<bool> condition, Action ifTrue, Action? ifFalse = null)
    {
        var ifTrueStep = new ActionStep("IfTrue", ifTrue);
        var ifFalseStep = ifFalse != null ? new ActionStep("IfFalse", ifFalse) : null;
        main.Add(new ConditionStep("If", condition, ifTrueStep, ifFalseStep));
    }

    /// <summary>
    /// 添加等待条件步骤
    /// </summary>
    /// <example>
    /// SequenceManager.Instance.Main.WaitUntil(() => isLoaded);
    /// </example>
    public static void WaitUntil(this MainSequenceManager main, Func<bool> condition)
    {
        main.Add(new AsyncActionStep("WaitUntil", () => { }, condition));
    }

    /// <summary>
    /// 添加一组顺序执行的动作
    /// </summary>
    public static void Sequence(this MainSequenceManager main, params Action[] actions)
    {
        var steps = actions.Select((a, i) => (ISequenceStep)new ActionStep($"Seq[{i}]", a)).ToArray();
        main.Add(new SequenceStep("Sequence", steps));
    }

    /// <summary>
    /// 添加一组并行执行的动作（等待全部完成）
    /// </summary>
    public static void Parallel(this MainSequenceManager main, params Action[] actions)
    {
        var steps = actions.Select((a, i) => (ISequenceStep)new ActionStep($"Par[{i}]", a)).ToArray();
        main.Add(new ParallelStep("Parallel", ParallelWaitMode.WaitAll, steps));
    }

    #endregion

    #region SequencePlayer Extensions

    /// <summary>
    /// 快速创建并启动一个本地流程
    /// </summary>
    /// <example>
    /// SequenceManager.Instance.RunLocal(
    ///     () => Debug.Log("Step 1"),
    ///     () => Debug.Log("Step 2")
    /// );
    /// </example>
    public static SequencePlayer RunLocal(this SequenceManager mgr, params Action[] actions)
    {
        var steps = actions.Select((a, i) => (ISequenceStep)new ActionStep($"Local[{i}]", a)).ToList();
        var player = mgr.CreateLocal(steps);
        player.Play();
        return player;
    }

    #endregion
}

#endregion

/// <summary>
/// 序列系统：统一管理层
/// - Main: 全局主流程（跨模块协调，支持动态嵌套子步骤）
/// - Pool: 命名流程池（局部/并行流程，独立执行）
/// </summary>
public sealed class SequenceManager : ISequenceManager
{
    public static SequenceManager Instance { get; } = new();

    /// <summary>
    /// 全局主流程管理器
    /// 用于跨模块协调的流程（登录弹脸、领奖展示、新手引导等）
    /// 支持动态嵌套子步骤
    /// </summary>
    public MainSequenceManager Main { get; }
    
    IMainSequenceManager ISequenceManager.Main => Main;

    /// <summary>
    /// 命名流程池
    /// 用于独立/并行的局部流程（技能演出、UI动画等）
    /// </summary>
    private readonly Dictionary<string, SequencePlayer> _pool = new();

    private SequenceManager()
    {
        Main = new MainSequenceManager();
    }

    #region Pool API

    /// <summary>
    /// 获取或创建命名流程
    /// </summary>
    public SequencePlayer GetOrCreate(string name)
    {
        if (!_pool.ContainsKey(name))
        {
            Console.WriteLine($"[SequenceSystem] Create pool sequence '{name}'");
            _pool[name] = new SequencePlayer(new List<ISequenceStep>());
        }
        return _pool[name];
    }

    /// <summary>
    /// 创建一个新的独立流程（不进入池子，用完即弃）
    /// </summary>
    public SequencePlayer CreateLocal(IEnumerable<ISequenceStep> steps, ISequenceObserver? observer = null)
    {
        return new SequencePlayer(steps, observer);
    }

    /// <summary>
    /// 移除命名流程
    /// </summary>
    public void Remove(string name)
    {
        if (_pool.TryGetValue(name, out var player))
        {
            player.Cancel();
            _pool.Remove(name);
            Console.WriteLine($"[SequenceSystem] Remove pool sequence '{name}'");
        }
    }

    /// <summary>
    /// 检查命名流程是否存在
    /// </summary>
    public bool Has(string name) => _pool.ContainsKey(name);

    /// <summary>
    /// 获取所有活跃的流程名称
    /// </summary>
    public IEnumerable<string> GetActiveNames() => _pool.Keys;

    #endregion

    #region Lifecycle

    /// <summary>
    /// 每帧调用：驱动所有流程
    /// </summary>
    public void Tick(float dt)
    {
        // 驱动主流程
        Main.Tick(dt);

        // 驱动池中的所有流程
        foreach (var player in _pool.Values)
        {
            player.Tick(dt);
        }

        // 清理已完成的流程
        var toRemove = _pool
            .Where(kv => !kv.Value.IsPlaying)
            .Select(kv => kv.Key)
            .ToList();
        
        foreach (var name in toRemove)
        {
            _pool.Remove(name);
            Console.WriteLine($"[SequenceSystem] Auto-remove completed sequence '{name}'");
        }
    }

    /// <summary>
    /// 取消所有流程
    /// </summary>
    public void CancelAll()
    {
        Main.Cancel();
        foreach (var player in _pool.Values)
        {
            player.Cancel();
        }
        _pool.Clear();
    }

    /// <summary>
    /// 暂停所有流程
    /// </summary>
    public void PauseAll()
    {
        Main.Pause();
        foreach (var player in _pool.Values)
        {
            player.Pause();
        }
    }

    /// <summary>
    /// 恢复所有流程
    /// </summary>
    public void ResumeAll()
    {
        Main.Resume();
        foreach (var player in _pool.Values)
        {
            player.Resume();
        }
    }

    #endregion

    #region Interrupt

    /// <summary>
    /// 中断当前流程，立即执行紧急流程
    /// 紧急流程完成后自动恢复
    /// </summary>
    public void Interrupt(ISequenceStep urgentStep)
    {
        Main.Interrupt(urgentStep);
    }

    /// <summary>
    /// 中断当前流程，执行一组紧急步骤
    /// </summary>
    public void Interrupt(IEnumerable<ISequenceStep> urgentSteps)
    {
        Main.Interrupt(urgentSteps);
    }

    #endregion

    #region Debug

    /// <summary>
    /// 打印当前状态（调试用）
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine("=== SequenceSystem Status ===");
        Console.WriteLine($"Main: IsPlaying={Main.IsPlaying}, HasPending={Main.HasPending}");
        Console.WriteLine($"Pool: {_pool.Count} active sequences");
        foreach (var kv in _pool)
        {
            Console.WriteLine($"  - {kv.Key}: IsPlaying={kv.Value.IsPlaying}");
        }
        Console.WriteLine("=============================");
    }

    #endregion
}

/// <summary>
/// 主序列管理器：支持动态嵌套子步骤
/// </summary>
public sealed class MainSequenceManager : IMainSequenceManager
{
    private readonly DynamicQueueStep _mainSequence;
    private readonly SequencePlayer _player;
    private readonly Stack<IStepContainer> _contextStack = new();
    
    // 中断相关
    private SequencePlayer? _interruptPlayer;
    private bool _isInterrupted;

    public bool IsPlaying => _player.IsPlaying || (_interruptPlayer?.IsPlaying ?? false);
    public bool HasPending => _mainSequence.HasPending;
    public bool IsInterrupted => _isInterrupted;

    internal MainSequenceManager()
    {
        _mainSequence = new DynamicQueueStep("MainSequence");
        _player = new SequencePlayer(new[] { _mainSequence });
    }

    /// <summary>
    /// 添加步骤到当前上下文
    /// 步骤会被自动包装成 DynamicParentStep，支持动态产生子步骤
    /// </summary>
    public void Add(ISequenceStep step)
    {
        // 自动包装成 DynamicParentStep
        var wrapped = new DynamicParentStep(step);

        if (_contextStack.Count > 0)
        {
            var context = _contextStack.Peek();
            context.Add(wrapped);
            Console.WriteLine($"[Main] Add '{step.Name}' to context '{((ISequenceStep)context).Name}'");
        }
        else
        {
            _mainSequence.Add(wrapped);
            Console.WriteLine($"[Main] Add '{step.Name}' to MainSequence");
        }
    }

    /// <summary>
    /// 添加步骤到主序列末尾（忽略当前上下文）
    /// 用于需要在主流程末尾追加的场景
    /// </summary>
    public void AddToMain(ISequenceStep step)
    {
        var wrapped = new DynamicParentStep(step);
        _mainSequence.Add(wrapped);
        Console.WriteLine($"[Main] AddToMain '{step.Name}'");
    }

    /// <summary>
    /// 添加多个步骤
    /// </summary>
    public void AddRange(IEnumerable<ISequenceStep> steps)
    {
        foreach (var step in steps)
            Add(step);
    }

    /// <summary>
    /// 压入上下文（由 DynamicParentStep 在 Enter 时调用）
    /// </summary>
    internal void PushContext(IStepContainer container)
    {
        _contextStack.Push(container);
        Console.WriteLine($"[Main] PushContext '{((ISequenceStep)container).Name}', depth={_contextStack.Count}");
    }

    /// <summary>
    /// 弹出上下文（由 DynamicParentStep 在完成/取消时调用）
    /// </summary>
    internal void PopContext(IStepContainer container)
    {
        if (_contextStack.Count > 0 && _contextStack.Peek() == container)
        {
            _contextStack.Pop();
            Console.WriteLine($"[Main] PopContext '{((ISequenceStep)container).Name}', depth={_contextStack.Count}");
        }
    }

    /// <summary>
    /// 检查并启动执行
    /// </summary>
    public void Check()
    {
        if (!_player.IsPlaying && _mainSequence.HasPending && !_isInterrupted)
        {
            Console.WriteLine("[Main] Check: starting player");
            _player.Play();
        }
    }

    /// <summary>
    /// 每帧调用
    /// </summary>
    public void Tick(float dt)
    {
        // 如果有中断流程，优先执行中断
        if (_isInterrupted && _interruptPlayer != null)
        {
            _interruptPlayer.Tick(dt);
            
            // 中断流程完成，恢复主流程
            if (!_interruptPlayer.IsPlaying)
            {
                Console.WriteLine("[Main] Interrupt completed, resuming main flow");
                _isInterrupted = false;
                _interruptPlayer = null;
                _player.Resume();
            }
            return;
        }
        
        _player.Tick(dt);

        // 如果执行完了但还有待执行的步骤，重新启动
        if (!_player.IsPlaying && _mainSequence.HasPending)
        {
            _player.Play();
        }
    }

    /// <summary>
    /// 中断当前流程，立即执行紧急流程
    /// </summary>
    public void Interrupt(ISequenceStep urgentStep)
    {
        Interrupt(new[] { urgentStep });
    }

    /// <summary>
    /// 中断当前流程，执行一组紧急步骤
    /// </summary>
    public void Interrupt(IEnumerable<ISequenceStep> urgentSteps)
    {
        Console.WriteLine("[Main] INTERRUPT: Pausing main flow for urgent steps");
        
        // 暂停当前主流程
        _player.Pause();
        _isInterrupted = true;
        
        // 创建并启动中断流程
        _interruptPlayer = new SequencePlayer(urgentSteps);
        _interruptPlayer.Play();
    }

    /// <summary>
    /// 取消
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
    /// 恢复
    /// </summary>
    public void Resume() => _player.Resume();
}
