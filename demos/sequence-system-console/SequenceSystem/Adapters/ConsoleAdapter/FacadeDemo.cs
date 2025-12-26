using SequenceSystem.Core;

namespace SequenceSystem.Adapters.ConsoleAdapter;

/// <summary>
/// Facade Demo - 展示业务开发者友好的简化 API
/// 
/// 对比：
/// - 旧方式：需要理解 ISequenceStep、ActionStep、DelayStep 等概念
/// - 新方式：只需要写 lambda，框架存在感为零
/// </summary>
public static class FacadeDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Facade Demo: 业务友好的简化 API ===\n");

        // Demo 1: 最简单的用法
        Demo1_SimpleRun();

        // Demo 2: 链式调用
        Demo2_FluentChain();

        // Demo 3: 条件分支
        Demo3_Conditional();

        // Demo 4: 并行执行
        Demo4_Parallel();

        // Demo 5: 扩展方法（直接操作 Main）
        Demo5_ExtensionMethods();

        Console.WriteLine("\n=== Facade Demo 完成 ===");
    }

    /// <summary>
    /// Demo 1: 最简单的用法 - 一行代码
    /// </summary>
    private static void Demo1_SimpleRun()
    {
        Console.WriteLine("--- Demo 1: 最简单的用法 ---");
        Console.WriteLine("代码: SimpleSequence.Run(() => Log(\"Hello\"), () => Log(\"World\"));");
        Console.WriteLine();

        // 业务开发者只需要写这一行！
        var player = SimpleSequence.Run(
            () => Console.WriteLine("  Hello"),
            () => Console.WriteLine("  World")
        );

        // 驱动执行
        DriveToCompletion(player);
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 2: 链式调用
    /// </summary>
    private static void Demo2_FluentChain()
    {
        Console.WriteLine("--- Demo 2: 链式调用 ---");
        Console.WriteLine(@"代码:
SimpleSequence.Create()
    .Do(() => Log(""Step 1""))
    .Delay(0.5f)
    .Do(() => Log(""Step 2""))
    .Run();
");

        var player = SimpleSequence.Create("FluentDemo")
            .Do(() => Console.WriteLine("  Step 1: 开始"))
            .Delay(0.5f)  // 模拟延迟
            .Do(() => Console.WriteLine("  Step 2: 延迟后"))
            .Delay(0.3f)
            .Do(() => Console.WriteLine("  Step 3: 完成"))
            .Run();

        DriveToCompletion(player);
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 3: 条件分支
    /// </summary>
    private static void Demo3_Conditional()
    {
        Console.WriteLine("--- Demo 3: 条件分支 ---");

        int hp = 75;
        Console.WriteLine($"当前 HP = {hp}");
        Console.WriteLine(@"代码:
.If(() => hp > 50,
    then: s => s.Do(() => Log(""血量充足"")),
    else: s => s.Do(() => Log(""血量不足"")))
");

        var player = SimpleSequence.Create()
            .Do(() => Console.WriteLine("  检查血量..."))
            .If(() => hp > 50,
                then: s => s.Do(() => Console.WriteLine("  ✅ 血量充足，继续战斗")),
                @else: s => s.Do(() => Console.WriteLine("  ⚠️ 血量不足，需要治疗")))
            .Do(() => Console.WriteLine("  检查完成"))
            .Run();

        DriveToCompletion(player);
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 4: 并行执行
    /// </summary>
    private static void Demo4_Parallel()
    {
        Console.WriteLine("--- Demo 4: 并行执行 ---");
        Console.WriteLine(@"代码:
.Parallel(
    s => s.Do(() => Log(""加载资源A"")),
    s => s.Do(() => Log(""加载资源B"")),
    s => s.Do(() => Log(""加载资源C""))
)
");

        var player = SimpleSequence.Create()
            .Do(() => Console.WriteLine("  开始并行加载..."))
            .Parallel(
                s => s.Do(() => Console.WriteLine("  [A] 加载模型")),
                s => s.Do(() => Console.WriteLine("  [B] 加载贴图")),
                s => s.Do(() => Console.WriteLine("  [C] 加载音效"))
            )
            .Do(() => Console.WriteLine("  所有资源加载完成！"))
            .Run();

        DriveToCompletion(player);
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 5: 扩展方法 - 直接操作 Main
    /// </summary>
    private static void Demo5_ExtensionMethods()
    {
        Console.WriteLine("--- Demo 5: 扩展方法（操作全局 Main）---");
        Console.WriteLine(@"代码:
SequenceManager.Instance.Main.Run(() => Log(""直接添加到主流程""));
SequenceManager.Instance.Main.Delay(0.3f);
SequenceManager.Instance.Main.If(() => true, () => Log(""条件成立""));
");

        var main = SequenceManager.Instance.Main;

        // 使用扩展方法，业务代码非常简洁
        main.Run(() => Console.WriteLine("  [Main] 步骤 1"));
        main.Delay(0.3f);
        main.Run(() => Console.WriteLine("  [Main] 步骤 2"));
        main.If(() => true, 
            () => Console.WriteLine("  [Main] 条件成立"));

        // 驱动主流程
        Console.WriteLine("  开始执行主流程...");
        for (int i = 0; i < 20; i++)
        {
            SequenceManager.Instance.Tick(0.1f);
            if (!main.IsPlaying && !main.HasPending)
                break;
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 辅助方法：驱动 Player 直到完成
    /// </summary>
    private static void DriveToCompletion(SequencePlayer player, int maxTicks = 100)
    {
        for (int i = 0; i < maxTicks && player.IsPlaying; i++)
        {
            player.Tick(0.1f);
        }
    }
}
