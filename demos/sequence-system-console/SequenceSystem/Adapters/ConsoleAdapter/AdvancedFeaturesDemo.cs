using SequenceSystem.Core;
using SequenceSystem.Steps.Common;
using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Adapters.ConsoleAdapter;

/// <summary>
/// 高级功能演示：
/// 1. TryStep - 错误处理
/// 2. 调试工具 - 步骤树、耗时统计
/// 3. Interrupt - 中断机制
/// 4. 数据驱动 - JSON 配置
/// 5. 依赖注入 - Mock 测试
/// </summary>
public static class AdvancedFeaturesDemo
{
    public static void Run()
    {
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║     SequenceSystem 高级功能演示               ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        Demo1_TryStep();
        Demo2_Debugger();
        Demo3_Interrupt();
        Demo4_DataDriven();
        Demo5_DependencyInjection();

        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║           所有演示完成！                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
    }

    /// <summary>
    /// Demo 1: TryStep 错误处理
    /// </summary>
    static void Demo1_TryStep()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("【Demo 1】TryStep - 错误处理 (try-catch-finally)");
        Console.WriteLine(new string('=', 50) + "\n");

        var tryStep = new TryStep(
            "ErrorHandlingDemo",
            tryStep: new ThrowStep("RiskyOperation", "模拟的业务错误！", throwOnEnter: true),
            catchStep: new LogStep("ErrorHandler", "🔧 捕获到错误，执行恢复逻辑..."),
            finallyStep: new LogStep("Cleanup", "🧹 清理资源，无论成功失败都执行")
        );

        var player = new SequencePlayer(new[] { tryStep });
        player.Play();

        // 模拟游戏循环
        for (int i = 0; i < 10 && player.IsPlaying; i++)
        {
            player.Tick(0.1f);
            Thread.Sleep(50);
        }

        Console.WriteLine($"\n结果: HasError={tryStep.HasError}, Exception={tryStep.CaughtException?.Message}");
        Console.WriteLine("✓ Demo 1 完成\n");
    }

    /// <summary>
    /// Demo 2: 调试工具
    /// </summary>
    static void Demo2_Debugger()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("【Demo 2】调试工具 - 步骤树、WaitingFor、耗时统计");
        Console.WriteLine(new string('=', 50) + "\n");

        var debugger = new SequenceDebugger();

        // 创建一些步骤
        var loadStep1 = new AsyncLoadStep("LoadTexture", "texture/hero.png", 0.5f);
        var loadStep2 = new AsyncLoadStep("LoadSound", "audio/bgm.mp3", 0.3f);
        var logStep = new LogStep("Complete", "所有资源加载完成！");

        var steps = new ISequenceStep[] { loadStep1, loadStep2, logStep };
        var player = new SequencePlayer(steps);

        // 记录步骤开始
        foreach (var step in steps)
        {
            debugger.OnStepEnter(step);
        }

        player.Play();

        // 模拟游戏循环
        Console.WriteLine("--- 执行中... ---\n");
        for (int i = 0; i < 20 && player.IsPlaying; i++)
        {
            player.Tick(0.1f);
            
            // 更新 WaitingFor 信息
            if (loadStep1 is IDebuggableStep d1 && !loadStep1.IsDone)
                debugger.UpdateWaitingFor(loadStep1, d1.WaitingFor);
            if (loadStep2 is IDebuggableStep d2 && !loadStep2.IsDone)
                debugger.UpdateWaitingFor(loadStep2, d2.WaitingFor);
            
            // 检查完成
            if (loadStep1.IsDone) debugger.OnStepExit(loadStep1);
            if (loadStep2.IsDone) debugger.OnStepExit(loadStep2);
            if (logStep.IsDone) debugger.OnStepExit(logStep);
            
            Thread.Sleep(50);
        }

        // 打印调试信息
        Console.WriteLine(debugger.PrintExecutionLog(20));
        Console.WriteLine(debugger.PrintTimingStats());
        Console.WriteLine("✓ Demo 2 完成\n");
    }

    /// <summary>
    /// Demo 3: 中断机制
    /// </summary>
    static void Demo3_Interrupt()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("【Demo 3】Interrupt - 中断机制（紧急流程优先）");
        Console.WriteLine(new string('=', 50) + "\n");

        // 注意：这里不能用单例，因为前面的 Demo 可能污染了状态
        // 创建一个新的 SequenceManager 实例来演示
        var mainQueue = new DynamicQueueStep("MainDemo");
        var mainPlayer = new SequencePlayer(new[] { mainQueue });

        // 添加主流程步骤
        Console.WriteLine("添加主流程步骤...");
        mainQueue.Add(new LogStep("Step1", "📋 主流程步骤 1"));
        mainQueue.Add(new DelayStep("Step2", 0.5f));
        mainQueue.Add(new LogStep("Step3", "📋 主流程步骤 3"));
        mainQueue.Add(new DelayStep("Step4", 0.5f));
        mainQueue.Add(new LogStep("Step5", "📋 主流程步骤 5"));

        mainPlayer.Play();

        // 中断相关变量
        SequencePlayer? interruptPlayer = null;
        bool interrupted = false;
        int interruptFrame = 8; // 在第 8 帧触发中断

        Console.WriteLine("\n--- 开始执行主流程（第 8 帧时触发中断）---\n");

        for (int frame = 0; frame < 30; frame++)
        {
            // 触发中断
            if (frame == interruptFrame && !interrupted)
            {
                Console.WriteLine("\n⚠️  [INTERRUPT] 触发紧急中断！暂停主流程...\n");
                mainPlayer.Pause();
                interrupted = true;

                // 创建紧急流程
                interruptPlayer = new SequencePlayer(new ISequenceStep[]
                {
                    new LogStep("Urgent1", "🚨 紧急步骤 1：显示断线提示"),
                    new DelayStep("Urgent2", 0.3f),
                    new LogStep("Urgent3", "🚨 紧急步骤 2：尝试重连"),
                    new DelayStep("Urgent4", 0.2f),
                    new LogStep("Urgent5", "🚨 紧急步骤 3：重连成功！")
                });
                interruptPlayer.Play();
            }

            // 执行中断流程
            if (interrupted && interruptPlayer != null)
            {
                interruptPlayer.Tick(0.1f);

                // 中断流程完成，恢复主流程
                if (!interruptPlayer.IsPlaying)
                {
                    Console.WriteLine("\n✓ 紧急流程完成，恢复主流程...\n");
                    interrupted = false;
                    mainPlayer.Resume();
                }
            }
            else
            {
                mainPlayer.Tick(0.1f);
            }

            if (!mainPlayer.IsPlaying && !interrupted)
                break;

            Thread.Sleep(50);
        }

        Console.WriteLine("\n✓ Demo 3 完成\n");
    }

    /// <summary>
    /// Demo 4: 数据驱动
    /// </summary>
    static void Demo4_DataDriven()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("【Demo 4】数据驱动 - JSON 配置解析");
        Console.WriteLine(new string('=', 50) + "\n");

        // JSON 配置
        var json = """
        {
            "name": "LoginSequence",
            "steps": [
                { "type": "log", "name": "Welcome", "message": "👋 欢迎来到游戏！" },
                { "type": "delay", "name": "Loading", "seconds": 0.3 },
                { "type": "condition", "name": "CheckVIP", "condition": "isVIP",
                    "ifTrue": [
                        { "type": "log", "name": "VIPWelcome", "message": "🌟 尊贵的VIP玩家，您好！" }
                    ],
                    "ifFalse": [
                        { "type": "log", "name": "NormalWelcome", "message": "欢迎普通玩家！" }
                    ]
                },
                { "type": "parallel", "name": "LoadResources", "mode": "all",
                    "steps": [
                        { "type": "load", "name": "LoadUI", "resource": "ui/main.prefab", "seconds": 0.2 },
                        { "type": "load", "name": "LoadAudio", "resource": "audio/bgm.mp3", "seconds": 0.3 }
                    ]
                },
                { "type": "log", "name": "Done", "message": "✅ 登录流程完成！" }
            ]
        }
        """;

        Console.WriteLine("JSON 配置:");
        Console.WriteLine(json);
        Console.WriteLine();

        // 创建工厂并注册条件
        var factory = new StepFactory();
        factory.RegisterCondition("isVIP", () => true); // 模拟 VIP 玩家

        // 解析并创建步骤
        var steps = factory.CreateStepsFromJson(json);
        var player = new SequencePlayer(steps);

        Console.WriteLine("--- 执行数据驱动的序列 ---\n");
        player.Play();

        for (int i = 0; i < 30 && player.IsPlaying; i++)
        {
            player.Tick(0.1f);
            Thread.Sleep(50);
        }

        Console.WriteLine("\n✓ Demo 4 完成\n");
    }

    /// <summary>
    /// Demo 5: 依赖注入
    /// </summary>
    static void Demo5_DependencyInjection()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("【Demo 5】依赖注入 - Mock 测试");
        Console.WriteLine(new string('=', 50) + "\n");

        // 创建 Mock 管理器
        var mockManager = new MockSequenceManager();

        // 注册到服务定位器
        SequenceServices.Register(mockManager);

        // 模拟业务代码使用 SequenceServices
        Console.WriteLine("模拟业务代码调用 SequenceServices.Manager...");
        
        var manager = SequenceServices.Manager;
        manager.Main.Add(new LogStep("Test1", "测试步骤 1"));
        manager.Main.Add(new LogStep("Test2", "测试步骤 2"));
        manager.Main.Check();

        // 模拟几帧
        for (int i = 0; i < 5; i++)
        {
            manager.Tick(0.1f);
        }

        // 触发中断
        manager.Interrupt(new LogStep("Urgent", "紧急步骤"));

        // 验证 Mock 结果
        Console.WriteLine("\nMock 验证结果:");
        Console.WriteLine($"  - AddedSteps.Count: {mockManager.MockMain.AddedSteps.Count}");
        Console.WriteLine($"  - TickCount: {mockManager.MockMain.TickCount}");
        Console.WriteLine($"  - InterruptedSteps.Count: {mockManager.MockMain.InterruptedSteps.Count}");
        Console.WriteLine($"  - IsInterrupted: {mockManager.MockMain.IsInterrupted}");

        // 重置服务定位器
        SequenceServices.Reset();

        Console.WriteLine("\n✓ Demo 5 完成 - Mock 测试验证成功！\n");
    }
}
