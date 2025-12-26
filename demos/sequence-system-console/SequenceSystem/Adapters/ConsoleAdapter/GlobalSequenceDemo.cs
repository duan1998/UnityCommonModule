namespace SequenceSystem.Adapters.ConsoleAdapter;

using SequenceSystem.Core;
using SequenceSystem.Steps.Common;
using SequenceSystem.Steps.Flow;
using System.Diagnostics;


public static class GlobalSequenceDemo
{
    public static void Run()
    {
        Console.WriteLine("========== GlobalSequenceManager Demo ==========\n");

        var gsm = GlobalSequenceManager.Instance;

        // 模拟：添加引导流程
        gsm.Add(new LogStep("引导A", "开始引导A"));
        gsm.Add(new DynamicQueueStep("引导B", new ISequenceStep[]
        {
            new LogStep("引导B-1", "引导B 第一步"),
            // 这一步会触发演出
            new ActionStep("引导B-触发演出", () =>
            {
                Console.WriteLine("[Game] 检测到需要播放演出！");
                // 演出被解构成一组 step，插入到当前上下文（引导B 内部）
                gsm.Add(new LogStep("演出-镜头移动", "镜头移动中..."));
                gsm.Add(new SimAnimStep("演出-角色动画", "角色A", "Walk", 0.5f));
                gsm.Add(new DynamicQueueStep("演出-对白序列", new ISequenceStep[]
                {
                    new LogStep("对白1", "NPC: 你好，冒险者！"),
                    // 对白中触发选项
                    new ActionStep("对白-触发选项", () =>
                    {
                        Console.WriteLine("[Game] 玩家选择了选项 A，触发支线对话！");
                        gsm.Add(new LogStep("支线对白1", "NPC: 你选了 A？"));
                        gsm.Add(new LogStep("支线对白2", "NPC: 好的，我明白了。"));
                    }),
                    new LogStep("对白2", "NPC: 再见！"),
                }));
                gsm.Add(new LogStep("演出-结束", "演出结束"));
            }),
            new LogStep("引导B-2", "引导B 第二步"),
        }));
        gsm.Add(new LogStep("引导C", "开始引导C"));

        // 检查并启动
        gsm.Check();

        // 主循环
        var sw = Stopwatch.StartNew();
        double last = 0;

        while (gsm.IsPlaying || gsm.HasPending)
        {
            double now = sw.Elapsed.TotalSeconds;
            float dt = (float)(now - last);
            last = now;

            gsm.Tick(dt);
            Thread.Sleep(16);
        }

        Console.WriteLine("\n========== Demo 结束 ==========");
    }
}
