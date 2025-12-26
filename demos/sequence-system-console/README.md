# SequenceSystem Demo（序列/演出系统原型）

本目录是一个 Console 可运行 Demo，用于沉淀序列/演出系统的核心抽象与落地方式（先把"序列这件事"做对，再 Unity 化）。

## 设计理念

> **框架的复杂性不应该暴露给业务开发者**

本系统采用**分层设计**：
- **底层**：完整的 Step 体系，支持复杂流程编排（给框架开发者）
- **上层**：简洁的 Facade API，只需写 lambda（给业务开发者）

```csharp
// 业务开发者只需要这样写：
SequenceManager.Instance.Main.Run(() => ShowReward());
SequenceManager.Instance.Main.Delay(1.5f);
SequenceManager.Instance.Main.If(() => hp > 0, () => Attack());

// 或者用链式 Builder：
SimpleSequence.Create()
    .Do(() => Console.WriteLine("Hello"))
    .Delay(1.0f)
    .Do(() => Console.WriteLine("World"))
    .Run();
```

## 设计目标

- **动态嵌套子步骤**：任何 Step 执行时都可以动态产生子步骤，无需预先定义完整流程
- **无限递归嵌套**：A → B → BB → BBB...，父步骤自动等待所有子步骤完成
- **自动上下文管理**：不需要手动指定插入位置，GSM 自动管理上下文栈
- **Main + Pool 架构**：全局主流程（跨模块协调）+ 命名流程池（独立并行）
- **可观测性**：支持 Observer 模式，可监听步骤生命周期事件
- **丰富的流程控制**：条件分支、并行执行、超时保护、重复执行、跳过等
- **Facade API**：业务开发者无需了解 Step 概念，直接写 lambda

## Demo 位置与运行

- **项目目录**：`SequenceSystem/`
- **运行**：

```bash
cd demos/sequence-system-console/SequenceSystem
dotnet run
```

## 核心架构

```
┌─────────────────────────────────────────────────────────────┐
│                     SequenceSystem                          │
│                    （统一管理层）                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────┐   ┌─────────────────────────┐ │
│  │    MainSequenceManager   │   │    Pool (命名流程池)     │ │
│  │    （全局主流程）          │   │                         │ │
│  │                         │   │  skill-fireball         │ │
│  │  ┌───────────────────┐  │   │  ui-reward-anim         │ │
│  │  │ DynamicQueueStep  │  │   │  ...                    │ │
│  │  │  (MainSequence)   │  │   │                         │ │
│  │  └───────────────────┘  │   │  每个是独立的            │ │
│  │          │              │   │  SequencePlayer         │ │
│  │          ▼              │   │                         │ │
│  │  ┌───────────────────┐  │   └─────────────────────────┘ │
│  │  │DynamicParentStep  │  │                               │
│  │  │ (自动包装每个step) │  │                               │
│  │  │  ├── inner step   │  │                               │
│  │  │  └── children[]   │  │                               │
│  │  └───────────────────┘  │                               │
│  └─────────────────────────┘                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 核心概念

### Step（步骤）

最小执行单元，实现 `ISequenceStep` 接口：

```csharp
public interface ISequenceStep
{
    string Name { get; }
    void Enter();         // 开始执行
    void Tick(float dt);  // 每帧更新
    bool IsDone { get; }  // 是否完成
    void Cancel();        // 取消执行
}
```

### SequencePlayer（播放器）

驱动一组 Step 的顺序执行，支持暂停、取消、跳过等控制。

### SequenceSystem（统一管理层）

- **Main**：全局主流程，支持动态嵌套子步骤
- **Pool**：命名流程池，用于独立/并行的局部流程

## 动态嵌套子步骤（核心特性）

```csharp
var ss = SequenceSystem.Instance;

// 添加一个步骤
ss.Main.Add(new ActionStep("A", () => {
    Console.WriteLine("A 执行中...");
    
    // 在 A 执行时动态产生子步骤
    ss.Main.Add(new LogStep("B", "B执行"));
    ss.Main.Add(new LogStep("C", "C执行"));
}));

ss.Main.Add(new LogStep("X", "X执行"));

// 执行顺序：A → B → C → X
// X 必须等 A 完全完成（包括 B、C）后才执行
```

**递归嵌套**：

```
A
├── A 自身逻辑
└── 子步骤
    ├── B
    │   ├── B 自身逻辑
    │   └── 子步骤
    │       ├── BB1
    │       └── BB2
    ├── C
    └── D
```

## 内置 Step 类型

### 流程控制 Step（`Steps/Flow/`）

| Step | 说明 |
|------|------|
| `SequenceStep` | 串行执行多个子步骤 |
| `ParallelStep` | 并行执行，支持 WaitAll/WaitAny |
| `ConditionStep` | 条件分支（if-else） |
| `SwitchStep` | 多路分支（switch-case） |
| `RepeatStep` | 循环执行直到条件满足 |
| `TimeoutStep` | 超时保护包装器 |
| `DelayStep` | 纯延时 |
| `ActionStep` | 执行一个 Action |
| `DynamicParentStep` | 自动包装器，支持动态子步骤 |
| `DynamicQueueStep` | 动态队列容器 |
| `SkippableStep` | 可跳过包装器 |
| `SkipAwareStep` | 响应全局跳过标志 |

### 通用 Step（`Steps/Common/`）

| Step | 说明 |
|------|------|
| `LogStep` | 打印日志 |
| `WaitSecondsStep` | 等待指定秒数 |
| `WaitSignalStep` | 等待外部信号 |
| `FakeAsyncStep` | 模拟异步操作（演示用） |
| `SimMoveStep` | 模拟移动（演示用） |
| `SimAnimStep` | 模拟动画（演示用） |
| `SimDoorStep` | 模拟开门（演示用） |

## 使用场景

### ✅ 适合用 SequenceSystem

| 场景 | 说明 |
|------|------|
| 剧情演出 | 线性、不可打断、按顺序播放 |
| 新手引导 | 一步一步、等待玩家操作 |
| 登录流程 | 检查 → 下载 → 登录 → 进入 |
| 场景切换 | 淡出 → 加载 → 初始化 → 淡入 |
| 技能表现 | 起手 → 施法 → 命中 → 收尾 |
| 奖励展示 | 角色获得 → 皮肤获得 → 奖励列表 |

### ❌ 不适合用 SequenceSystem

| 场景 | 应该用 |
|------|--------|
| 游戏主循环 | Update + 事件 |
| UI 管理 | 栈 + 状态机 |
| 战斗逻辑 | 状态机 / ECS |
| AI 决策 | 行为树 / 状态机 |
| 网络通信 | 异步回调 / 协程 |

## Main vs Pool

| 维度 | Main（全局主流程） | Pool（命名流程池） |
|------|------------------|------------------|
| 用途 | 跨模块协调的流程 | 独立/并行的局部流程 |
| 嵌套 | 支持动态嵌套子步骤 | 不支持 |
| 示例 | 登录弹脸、领奖展示 | 技能演出、UI动画 |
| 生命周期 | 全局单例 | 按需创建/销毁 |

## 文件结构

```
SequenceSystem/
├── Core/
│   ├── ISequenceStep.cs        # 基础接口
│   ├── IStepContainer.cs       # 容器接口
│   ├── ISequenceObserver.cs    # 观察者接口
│   ├── ISequenceManager.cs     # DI 接口 + Mock
│   ├── IDebuggableStep.cs      # 调试接口
│   ├── SequencePlayer.cs       # 播放器
│   ├── SequenceSystem.cs       # 统一管理层 + Facade
│   ├── SequenceDebugger.cs     # 调试工具
│   ├── StepFactory.cs          # 数据驱动工厂
│   └── SimpleSequence.cs       # 链式 Builder
├── Steps/
│   ├── Flow/                   # 流程控制 Step
│   │   ├── ActionStep.cs       # 同步/异步动作
│   │   ├── ConditionStep.cs
│   │   ├── DelayStep.cs
│   │   ├── DynamicParentStep.cs
│   │   ├── DynamicQueueStep.cs
│   │   ├── ParallelStep.cs
│   │   ├── RepeatStep.cs
│   │   ├── SequenceStep.cs
│   │   ├── SkipAwareStep.cs
│   │   ├── SkippableStep.cs
│   │   ├── SwitchStep.cs
│   │   ├── TimeoutStep.cs
│   │   └── TryStep.cs          # 错误处理
│   └── Common/                 # 通用 Step
│       └── ...
├── Adapters/
│   └── ConsoleAdapter/         # Console 适配层
│       ├── AdvancedFeaturesDemo.cs  # 高级功能演示
│       ├── FacadeDemo.cs            # Facade API 演示
│       ├── SequenceSystemDemo.cs    # 基础功能演示
│       └── ...
├── Program.cs
└── SequenceSystem.csproj
```

## Facade API（业务开发者推荐）

### 扩展方法

```csharp
var main = SequenceManager.Instance.Main;

// 同步动作
main.Run(() => DoSomething());
main.Run("Named", () => DoSomething());

// 延迟
main.Delay(1.5f);

// 条件
main.If(() => hp > 0, () => Attack(), () => Retreat());

// 等待条件
main.WaitUntil(() => isLoaded);

// 并行
main.Parallel(() => LoadA(), () => LoadB(), () => LoadC());
```

### 链式 Builder

```csharp
SimpleSequence.Create("MyFlow")
    .Do(() => Console.WriteLine("开始"))
    .Delay(1.0f)
    .If(() => hp > 50,
        then: s => s.Do(() => Console.WriteLine("血量充足")),
        @else: s => s.Do(() => Console.WriteLine("血量不足")))
    .Parallel(
        s => s.Do(() => LoadModel()),
        s => s.Do(() => LoadTexture())
    )
    .Repeat(3, s => s.Do(() => Console.WriteLine("重复3次")))
    .Run();
```

## 高级功能

### 1. 错误处理（TryStep）

```csharp
new TryStep(
    tryStep: new ActionStep(() => RiskyOperation()),
    catchStep: new ActionStep(() => HandleError()),
    finallyStep: new ActionStep(() => Cleanup())
);
```

### 2. 调试工具

```csharp
var debugger = new SequenceDebugger();
debugger.RecordStepEnter(step);
debugger.RecordStepExit(step);
debugger.PrintExecutionLog();
debugger.PrintTimingStats();
debugger.PrintStepTree(rootStep);
```

### 3. 中断机制

```csharp
// 紧急流程打断当前流程
SequenceManager.Instance.Interrupt(new ActionStep(() => {
    ShowUrgentMessage("服务器即将维护！");
}));
// 紧急流程完成后自动恢复主流程
```

### 4. 数据驱动

```csharp
var json = @"{
    ""name"": ""LoginFlow"",
    ""steps"": [
        { ""type"": ""log"", ""message"": ""开始登录"" },
        { ""type"": ""delay"", ""duration"": 1.0 },
        { ""type"": ""log"", ""message"": ""登录完成"" }
    ]
}";
var step = StepFactory.CreateFromJson(json);
```

### 5. 依赖注入

```csharp
// 注册
SequenceServices.Register(mockManager);

// 获取
var manager = SequenceServices.Get();

// 测试
var mock = new MockSequenceManager();
SequenceServices.Register(mock);
```

## 扩展方向

- **Unity 集成**：MonoBehaviour 适配、协程支持
- **可视化编辑器**：节点式流程编辑
- **性能优化**：对象池、无 GC 执行

## 相关知识点

- 命令模式（Command Pattern）
- 组合模式（Composite Pattern）
- 观察者模式（Observer Pattern）
- 状态机（State Machine）
- 协程/异步（Coroutine/Async）
