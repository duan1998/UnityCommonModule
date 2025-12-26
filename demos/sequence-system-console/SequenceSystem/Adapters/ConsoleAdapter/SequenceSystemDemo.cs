using SequenceSystem.Core;
using SequenceSystem.Steps.Common;
using SequenceSystem.Steps.Flow;

namespace SequenceSystem.Adapters.ConsoleAdapter;

/// <summary>
/// SequenceSystem 完整演示：
/// 1. Main 流程的动态嵌套子步骤
/// 2. Pool 流程的独立/并行执行
/// </summary>
public static class SequenceSystemDemo
{
    public static void Run()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  SequenceSystem Demo");
        Console.WriteLine("  - Main: 全局主流程（动态嵌套）");
        Console.WriteLine("  - Pool: 命名流程池（独立并行）");
        Console.WriteLine("========================================\n");

        var ss = SequenceManager.Instance;

        // ============================================
        // 场景1：Main 流程 - 模拟登录弹脸序列
        // ============================================
        Console.WriteLine("【场景1】Main 流程 - 登录弹脸序列\n");

        // 添加活动弹脸
        ss.Main.Add(new LogStep("活动弹脸", "显示活动弹窗..."));

        // 添加每月登录领奖（这个 step 执行时会产生子步骤）
        ss.Main.Add(new MonthlyLoginStep());

        // 添加月卡失效弹脸
        ss.Main.Add(new LogStep("月卡弹脸", "显示月卡失效提示..."));

        // 启动 Main 流程
        ss.Main.Check();

        // 模拟游戏循环
        Console.WriteLine("\n--- 开始模拟 Main 流程 ---\n");
        float dt = 0.1f;
        int maxFrames = 100;
        int frame = 0;

        while (ss.Main.IsPlaying && frame < maxFrames)
        {
            ss.Tick(dt);
            frame++;
            Thread.Sleep(50);
        }

        Console.WriteLine("\n--- Main 流程完成 ---\n");

        // ============================================
        // 场景2：Pool 流程 - 模拟技能演出（独立并行）
        // ============================================
        Console.WriteLine("\n【场景2】Pool 流程 - 技能演出（与 Main 并行）\n");

        // 添加新的 Main 流程步骤
        ss.Main.Add(new LogStep("主流程A", "主流程步骤A执行中..."));
        ss.Main.Add(new DelayStep("主流程等待", 0.5f));
        ss.Main.Add(new LogStep("主流程B", "主流程步骤B执行中..."));
        ss.Main.Check();

        // 同时创建一个独立的技能演出流程
        var skillSeq = ss.GetOrCreate("skill-fireball");
        // 注意：Pool 里的 SequencePlayer 需要手动添加步骤
        // 我们用 CreateLocal 来创建一个独立流程
        var skillPlayer = ss.CreateLocal(new ISequenceStep[]
        {
            new LogStep("火球-起手", "🔥 火球术起手动画..."),
            new DelayStep("火球-蓄力", 0.3f),
            new LogStep("火球-释放", "🔥 火球飞出！"),
            new DelayStep("火球-飞行", 0.2f),
            new LogStep("火球-命中", "💥 火球命中目标！"),
        });
        skillPlayer.Play();

        Console.WriteLine("\n--- 开始模拟并行执行 ---\n");
        frame = 0;
        while ((ss.Main.IsPlaying || skillPlayer.IsPlaying) && frame < maxFrames)
        {
            ss.Tick(dt);
            skillPlayer.Tick(dt);
            frame++;
            Thread.Sleep(50);
        }

        Console.WriteLine("\n--- 并行流程完成 ---\n");

        // ============================================
        // 场景3：多层嵌套演示
        // ============================================
        Console.WriteLine("\n【场景3】多层嵌套演示\n");

        ss.Main.Add(new ActionStep("外层A", () =>
        {
            Console.WriteLine("外层A 开始，产生子步骤...");
            
            // 外层A 产生 B
            ss.Main.Add(new ActionStep("中层B", () =>
            {
                Console.WriteLine("  中层B 开始，产生子步骤...");
                
                // 中层B 产生 C 和 D
                ss.Main.Add(new LogStep("内层C", "    内层C 执行"));
                ss.Main.Add(new LogStep("内层D", "    内层D 执行"));
            }));
            
            // 外层A 还产生 E
            ss.Main.Add(new LogStep("中层E", "  中层E 执行"));
        }));

        ss.Main.Add(new LogStep("外层F", "外层F 执行（等A完全完成后）"));

        ss.Main.Check();

        Console.WriteLine("\n--- 开始模拟多层嵌套 ---\n");
        frame = 0;
        while (ss.Main.IsPlaying && frame < maxFrames)
        {
            ss.Tick(dt);
            frame++;
            Thread.Sleep(50);
        }

        Console.WriteLine("\n--- 多层嵌套完成 ---\n");

        // 打印最终状态
        ss.PrintStatus();

        Console.WriteLine("\n========================================");
        Console.WriteLine("  Demo 结束");
        Console.WriteLine("========================================");
    }
}

/// <summary>
/// 模拟"每月登录领奖"步骤
/// 执行时会调用 GameUtil.OnlineLoadGetBonus()，产生子步骤
/// </summary>
public class MonthlyLoginStep : ISequenceStep
{
    public string Name => "每月登录";
    public bool IsDone { get; private set; }

    public void Enter()
    {
        Console.WriteLine("[MonthlyLogin] 显示每月登录界面，玩家点击领取...");
        
        // 模拟玩家点击领取，触发 OnlineLoadGetBonus
        SimulateOnlineLoadGetBonus();
        
        // 自身逻辑完成（但子步骤还没完成）
        IsDone = true;
    }

    public void Tick(float dt) { }
    public void Cancel() { }

    /// <summary>
    /// 模拟 GameUtil.OnlineLoadGetBonus
    /// 这个方法会根据奖励内容动态添加展示步骤
    /// </summary>
    private void SimulateOnlineLoadGetBonus()
    {
        var ss = SequenceManager.Instance;

        Console.WriteLine("[OnlineLoadGetBonus] 处理奖励，添加展示步骤...");

        // 模拟获得了2个角色、1个皮肤
        ss.Main.Add(new LogStep("角色获得1", "🎉 获得角色：战士！"));
        ss.Main.Add(new LogStep("角色获得2", "🎉 获得角色：法师！"));
        ss.Main.Add(new LogStep("皮肤获得", "👗 获得皮肤：战士-黄金铠甲！"));
        ss.Main.Add(new LogStep("奖励展示", "📦 显示完整奖励列表..."));
    }
}
