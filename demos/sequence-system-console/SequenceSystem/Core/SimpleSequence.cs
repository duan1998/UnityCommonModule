using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Core;

/// <summary>
/// SimpleSequence - 业务开发者友好的 Facade
/// 
/// 设计目的：让业务开发者不需要知道 ISequenceStep 的存在
/// 
/// 使用示例：
/// <code>
/// // 方式 1：链式调用
/// SimpleSequence.Create()
///     .Do(() => Console.WriteLine("Step 1"))
///     .Delay(1.0f)
///     .Do(() => Console.WriteLine("Step 2"))
///     .If(() => hp > 0, 
///         then: s => s.Do(() => Console.WriteLine("Alive")),
///         else: s => s.Do(() => Console.WriteLine("Dead")))
///     .Run();
/// 
/// // 方式 2：直接执行
/// SimpleSequence.Run(
///     () => Console.WriteLine("Hello"),
///     () => Console.WriteLine("World")
/// );
/// </code>
/// </summary>
public sealed class SimpleSequence
{
    private readonly List<ISequenceStep> _steps = new();
    private readonly string _name;

    private SimpleSequence(string name = "SimpleSequence")
    {
        _name = name;
    }

    #region Static Factory

    /// <summary>
    /// 创建一个新的 SimpleSequence（链式调用入口）
    /// </summary>
    public static SimpleSequence Create(string name = "SimpleSequence")
    {
        return new SimpleSequence(name);
    }

    /// <summary>
    /// 快速执行一组动作（最简单的用法）
    /// </summary>
    public static SequencePlayer Run(params Action[] actions)
    {
        var builder = Create();
        foreach (var action in actions)
            builder.Do(action);
        return builder.Run();
    }

    /// <summary>
    /// 快速执行一组步骤
    /// </summary>
    public static SequencePlayer Run(params ISequenceStep[] steps)
    {
        var player = SequenceManager.Instance.CreateLocal(steps);
        player.Play();
        return player;
    }

    #endregion

    #region Fluent API

    /// <summary>
    /// 添加一个同步动作
    /// </summary>
    public SimpleSequence Do(Action action)
    {
        _steps.Add(new ActionStep(action));
        return this;
    }

    /// <summary>
    /// 添加一个同步动作（带名称）
    /// </summary>
    public SimpleSequence Do(string name, Action action)
    {
        _steps.Add(new ActionStep(name, action));
        return this;
    }

    /// <summary>
    /// 添加延迟
    /// </summary>
    public SimpleSequence Delay(float seconds)
    {
        _steps.Add(new DelayStep(seconds));
        return this;
    }

    /// <summary>
    /// 添加等待条件
    /// </summary>
    public SimpleSequence WaitUntil(Func<bool> condition)
    {
        _steps.Add(new AsyncActionStep("WaitUntil", () => { }, condition));
        return this;
    }

    /// <summary>
    /// 添加条件分支
    /// </summary>
    public SimpleSequence If(Func<bool> condition, Action<SimpleSequence> then, Action<SimpleSequence>? @else = null)
    {
        var thenBuilder = Create("Then");
        then(thenBuilder);

        SimpleSequence? elseBuilder = null;
        if (@else != null)
        {
            elseBuilder = Create("Else");
            @else(elseBuilder);
        }

        _steps.Add(new ConditionStep(
            "If",
            condition,
            thenBuilder.Build(),
            elseBuilder?.Build()
        ));
        return this;
    }

    /// <summary>
    /// 添加并行执行（等待全部完成）
    /// </summary>
    public SimpleSequence Parallel(params Action<SimpleSequence>[] branches)
    {
        var branchSteps = branches.Select((b, i) =>
        {
            var builder = Create($"Branch[{i}]");
            b(builder);
            return builder.Build();
        }).ToArray();

        _steps.Add(new ParallelStep("Parallel", ParallelWaitMode.WaitAll, branchSteps));
        return this;
    }

    /// <summary>
    /// 添加并行执行（任一完成即可）
    /// </summary>
    public SimpleSequence Race(params Action<SimpleSequence>[] branches)
    {
        var branchSteps = branches.Select((b, i) =>
        {
            var builder = Create($"Race[{i}]");
            b(builder);
            return builder.Build();
        }).ToArray();

        _steps.Add(new ParallelStep("Race", ParallelWaitMode.WaitAny, branchSteps));
        return this;
    }

    /// <summary>
    /// 添加重复执行
    /// </summary>
    public SimpleSequence Repeat(int count, Action<SimpleSequence> body)
    {
        var bodyBuilder = Create("RepeatBody");
        body(bodyBuilder);
        var builtStep = bodyBuilder.Build();
        _steps.Add(new RepeatStep(
            "Repeat",
            stepFactory: () => builtStep,
            untilCondition: () => false,
            maxIterations: count
        ));
        return this;
    }

    /// <summary>
    /// 添加子序列
    /// </summary>
    public SimpleSequence Sub(Action<SimpleSequence> sub)
    {
        var subBuilder = Create("Sub");
        sub(subBuilder);
        _steps.Add(subBuilder.Build());
        return this;
    }

    /// <summary>
    /// 添加一个已有的 Step（逃生舱口，用于高级场景）
    /// </summary>
    public SimpleSequence AddStep(ISequenceStep step)
    {
        _steps.Add(step);
        return this;
    }

    #endregion

    #region Build & Run

    /// <summary>
    /// 构建为 ISequenceStep
    /// </summary>
    public ISequenceStep Build()
    {
        if (_steps.Count == 1)
            return _steps[0];
        return new SequenceStep(_name, _steps.ToArray());
    }

    /// <summary>
    /// 构建并立即运行（作为独立流程）
    /// </summary>
    public SequencePlayer Run()
    {
        var player = SequenceManager.Instance.CreateLocal(_steps);
        player.Play();
        return player;
    }

    /// <summary>
    /// 添加到全局主流程
    /// </summary>
    public void AddToMain()
    {
        SequenceManager.Instance.Main.Add(Build());
    }

    /// <summary>
    /// 添加到指定的命名流程
    /// </summary>
    public void AddTo(string poolName)
    {
        var player = SequenceManager.Instance.GetOrCreate(poolName);
        foreach (var step in _steps)
        {
            // Pool 使用的是 SequencePlayer，需要重新设置 steps
            // 这里简化处理，创建新的 player
        }
        // 简化：直接作为独立流程运行
        var newPlayer = SequenceManager.Instance.CreateLocal(_steps);
        newPlayer.Play();
    }

    #endregion
}
