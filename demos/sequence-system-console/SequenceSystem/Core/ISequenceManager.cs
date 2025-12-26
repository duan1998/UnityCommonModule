namespace SequenceSystem.Core;

/// <summary>
/// 序列管理器接口
/// 用于依赖注入，便于单元测试和 mock
/// </summary>
public interface ISequenceManager
{
    /// <summary>
    /// 全局主流程管理器
    /// </summary>
    IMainSequenceManager Main { get; }
    
    /// <summary>
    /// 获取或创建命名流程
    /// </summary>
    SequencePlayer GetOrCreate(string name);
    
    /// <summary>
    /// 创建一个新的独立流程
    /// </summary>
    SequencePlayer CreateLocal(IEnumerable<ISequenceStep> steps, ISequenceObserver? observer = null);
    
    /// <summary>
    /// 移除命名流程
    /// </summary>
    void Remove(string name);
    
    /// <summary>
    /// 检查命名流程是否存在
    /// </summary>
    bool Has(string name);
    
    /// <summary>
    /// 获取所有活跃的流程名称
    /// </summary>
    IEnumerable<string> GetActiveNames();
    
    /// <summary>
    /// 每帧调用
    /// </summary>
    void Tick(float dt);
    
    /// <summary>
    /// 取消所有流程
    /// </summary>
    void CancelAll();
    
    /// <summary>
    /// 暂停所有流程
    /// </summary>
    void PauseAll();
    
    /// <summary>
    /// 恢复所有流程
    /// </summary>
    void ResumeAll();
    
    /// <summary>
    /// 中断当前流程
    /// </summary>
    void Interrupt(ISequenceStep urgentStep);
    
    /// <summary>
    /// 中断当前流程
    /// </summary>
    void Interrupt(IEnumerable<ISequenceStep> urgentSteps);
}

/// <summary>
/// 主序列管理器接口
/// </summary>
public interface IMainSequenceManager
{
    /// <summary>
    /// 是否正在播放
    /// </summary>
    bool IsPlaying { get; }
    
    /// <summary>
    /// 是否有待执行的步骤
    /// </summary>
    bool HasPending { get; }
    
    /// <summary>
    /// 是否被中断
    /// </summary>
    bool IsInterrupted { get; }
    
    /// <summary>
    /// 添加步骤到当前上下文
    /// </summary>
    void Add(ISequenceStep step);
    
    /// <summary>
    /// 添加步骤到主序列末尾
    /// </summary>
    void AddToMain(ISequenceStep step);
    
    /// <summary>
    /// 添加多个步骤
    /// </summary>
    void AddRange(IEnumerable<ISequenceStep> steps);
    
    /// <summary>
    /// 检查并启动执行
    /// </summary>
    void Check();
    
    /// <summary>
    /// 每帧调用
    /// </summary>
    void Tick(float dt);
    
    /// <summary>
    /// 取消
    /// </summary>
    void Cancel();
    
    /// <summary>
    /// 暂停
    /// </summary>
    void Pause();
    
    /// <summary>
    /// 恢复
    /// </summary>
    void Resume();
    
    /// <summary>
    /// 中断
    /// </summary>
    void Interrupt(ISequenceStep urgentStep);
    
    /// <summary>
    /// 中断
    /// </summary>
    void Interrupt(IEnumerable<ISequenceStep> urgentSteps);
}

/// <summary>
/// 服务定位器（简单的 DI 容器）
/// 用于在不支持完整 DI 框架的环境中提供依赖注入能力
/// </summary>
public static class SequenceServices
{
    private static ISequenceManager? _manager;
    private static readonly object _lock = new();

    /// <summary>
    /// 获取当前的序列管理器
    /// 如果没有注册，返回默认的单例实现
    /// </summary>
    public static ISequenceManager Manager
    {
        get
        {
            if (_manager == null)
            {
                lock (_lock)
                {
                    _manager ??= SequenceManager.Instance;
                }
            }
            return _manager;
        }
    }

    /// <summary>
    /// 注册自定义的序列管理器（用于测试或自定义实现）
    /// </summary>
    public static void Register(ISequenceManager manager)
    {
        lock (_lock)
        {
            _manager = manager;
        }
    }

    /// <summary>
    /// 重置为默认实现
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _manager = null;
        }
    }
}

/// <summary>
/// Mock 序列管理器（用于单元测试）
/// </summary>
public class MockSequenceManager : ISequenceManager
{
    public IMainSequenceManager Main => MockMain;
    public MockMainSequenceManager MockMain { get; } = new();
    
    public List<string> CreatedSequences { get; } = new();
    public List<ISequenceStep> InterruptedSteps { get; } = new();
    public int TickCount { get; private set; }
    public bool IsCancelled { get; private set; }
    public bool IsPaused { get; private set; }

    public SequencePlayer GetOrCreate(string name)
    {
        CreatedSequences.Add(name);
        return new SequencePlayer(new List<ISequenceStep>());
    }

    public SequencePlayer CreateLocal(IEnumerable<ISequenceStep> steps, ISequenceObserver? observer = null)
    {
        return new SequencePlayer(steps, observer);
    }

    public void Remove(string name) => CreatedSequences.Remove(name);
    public bool Has(string name) => CreatedSequences.Contains(name);
    public IEnumerable<string> GetActiveNames() => CreatedSequences;

    public void Tick(float dt)
    {
        TickCount++;
        MockMain.Tick(dt);
    }

    public void CancelAll()
    {
        IsCancelled = true;
        MockMain.Cancel();
    }

    public void PauseAll()
    {
        IsPaused = true;
        MockMain.Pause();
    }

    public void ResumeAll()
    {
        IsPaused = false;
        MockMain.Resume();
    }

    public void Interrupt(ISequenceStep urgentStep)
    {
        InterruptedSteps.Add(urgentStep);
        MockMain.Interrupt(urgentStep);
    }

    public void Interrupt(IEnumerable<ISequenceStep> urgentSteps)
    {
        InterruptedSteps.AddRange(urgentSteps);
        MockMain.Interrupt(urgentSteps);
    }
}

/// <summary>
/// Mock 主序列管理器（用于单元测试）
/// </summary>
public class MockMainSequenceManager : IMainSequenceManager
{
    public bool IsPlaying { get; private set; }
    public bool HasPending => AddedSteps.Count > 0;
    public bool IsInterrupted { get; private set; }
    
    public List<ISequenceStep> AddedSteps { get; } = new();
    public List<ISequenceStep> InterruptedSteps { get; } = new();
    public int TickCount { get; private set; }
    public bool IsCancelled { get; private set; }
    public bool IsPaused { get; private set; }

    public void Add(ISequenceStep step) => AddedSteps.Add(step);
    public void AddToMain(ISequenceStep step) => AddedSteps.Add(step);
    public void AddRange(IEnumerable<ISequenceStep> steps) => AddedSteps.AddRange(steps);

    public void Check()
    {
        if (AddedSteps.Count > 0)
            IsPlaying = true;
    }

    public void Tick(float dt)
    {
        TickCount++;
    }

    public void Cancel()
    {
        IsCancelled = true;
        IsPlaying = false;
    }

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    public void Interrupt(ISequenceStep urgentStep)
    {
        InterruptedSteps.Add(urgentStep);
        IsInterrupted = true;
    }

    public void Interrupt(IEnumerable<ISequenceStep> urgentSteps)
    {
        InterruptedSteps.AddRange(urgentSteps);
        IsInterrupted = true;
    }
}
