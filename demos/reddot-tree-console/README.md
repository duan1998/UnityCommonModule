# RedDotTree Demo（入口级红点系统原型）

本目录是一个 **Console 可运行 Demo**，用于沉淀红点系统的核心抽象与落地方式（先把“红点这件事”做对，再 Unity 化）。

## 设计目标（你应该得到什么）

- **入口级粒度**：红点系统统一治理“入口/模块/页面节点”，而不是每个列表项实例都建节点。
- **三种红点状态**：`Dot / New / Super`（全部放进 `RedDotTree` 内）
- **节点级/类型级传播上限**：每个节点对每种类型可以配置“向上影响多少层”（默认无限）。
- **dirty + flush**：业务变化高频，计算/通知在帧末批处理。
- **只通知变化**：只有最终展示状态变化才会触发订阅回调（避免 UI 刷新风暴）。
- **可诊断**：能回答“为什么亮/从哪来的/传播到哪层了”。

## Demo 位置与运行

- 项目目录：`ConsoleApp1/`
- 运行：

```bash
cd demos/reddot-tree-console/ConsoleApp1
dotnet run
```

配置文件：`ConsoleApp1/Configs/reddot.json`（通过 `CopyToOutputDirectory` 自动拷贝到输出目录）

## 红点语义（核心约定）

- `BadgeType.Dot`：普通红点/数量（例如未读数、可领取数）
- `BadgeType.New`：新获得/新内容（例如新角色、新道具）
- `BadgeType.Super`：强提醒（例如必须处理/强引导/关键事件）

最终展示是一个 `BadgeState`（**优先级固定**）：

1. `SuperDot`（count = Super value）
2. `Dot`（count = Dot value）
3. `New`（count = New value）
4. `None`

> 你可以把这理解为“红点系统内部的展示规则”，它是红点产品语义，不是 UI 细节。

## 配置（JSON）

文件：`ConsoleApp1/Configs/reddot.json`

### 结构

- `defaultPropagate`：全局默认的传播上限（`-1` 表示无限；`0` 表示只影响自己；`>0` 表示向上 N 层）
- `nodes[]`：入口级节点树
  - `id`：节点唯一 id（建议稳定、可读，别用显示名）
  - `parentId`：父节点 id（根节点为 null）
  - `aggregator`：**Dot** 的聚合器（`AnyBool | Sum | Max`）
  - `propagate`：可选，节点级覆盖 `defaultPropagate`（对 Dot/New/Super 分别设置）

示例（节选）：

```json
{
  "defaultPropagate": { "dot": -1, "new": -1, "super": -1 },
  "nodes": [
    { "id": "Root", "parentId": null, "aggregator": "AnyBool" },
    { "id": "Mail", "parentId": "Root", "aggregator": "Sum" },
    {
      "id": "Mail_Friend",
      "parentId": "Mail",
      "aggregator": "Sum",
      "propagate": { "dot": 1, "new": 0, "super": 2 }
    }
  ]
}
```

## 接入指南（项目里怎么用）

### 1) 初始化建树（一次性）

- 解析 JSON -> `RedDotConfig`
- `tree.RegisterFromConfig(config)`：注册节点 + 校验 + Build

### 2) 业务写入（推荐：批量写入）

业务变化不要直接触发计算，而是写入 pending：

- `tree.SetStateDeferred(entryId, BadgeType.Dot, value)`
- `tree.SetStateDeferred(entryId, BadgeType.New, value)`
- `tree.SetStateDeferred(entryId, BadgeType.Super, value)`

### 3) 帧末批处理（Flush）

在一帧末尾（Unity 常用 `LateUpdate`）：

- `tree.ApplyPendingStatesAndFlush()`

它会：
- 按类型的传播上限标脏祖先
- 自底向上重算
- 只通知变化的入口（BadgeState 变化）
- 返回 `changedIds`（可用于 UI 局部刷新/统计）

### 4) UI 订阅（入口级）

UI 一般只订阅入口节点：

- `tree.SubscribeBadge(entryId, (BadgeState s) => { ... }, fireImmediately: true)`

> 入口级粒度意味着：列表项/实例级【新】通常由业务 map 判定渲染；入口级只写入聚合结果（例如 `Inventory.NewCount`）再由树向上传播。

## 关于列表项/实例级【新】

典型实践：

- 业务层维护一个 `HashSet<itemId>` 或 `Dictionary<itemId,bool>`（例如 `isNewMap`）
- 列表项 UI 直接查 `isNewMap[itemId]` 决定是否显示“新”
- 入口级 `NewCount = isNewMap.Count` 写入 `tree.SetStateDeferred("Inventory", BadgeType.New, NewCount)`

这样既满足“新需要向上传播”，又避免为每个实例建节点导致爆炸。

## 诊断与排错（上线必备）

- `tree.ValidateConfigOrThrow(config)`：配置级校验（id/hops 等）
- `tree.ValidateTreeOrThrow()`：树级校验（缺父/多根/循环/断链）
- `tree.DumpSubtree("Root")`：打印子树（含三类 value 与 hops）
- `tree.Explain(entryId)`：解释某个入口为什么亮（self/maxChild/remaining hops 等）

## 常见坑（强烈建议你能讲清）

- **忘记批处理**：业务一变就立刻重算 -> 同一帧重复计算导致卡顿
- **订阅泄漏**：UI 订阅不退订 -> 重复回调 + 内存泄漏
- **传播范围不受控**：所有细粒度变化都传到 Root -> 大厅被红点淹没
- **实例级节点爆炸**：为每个 item 建节点 -> 节点数/订阅数/清理复杂度暴涨

## Unity 接入建议（落地要点）

- `ApplyPendingStatesAndFlush()` 放到统一调度点（建议帧末）
- 订阅回调通过主线程派发（本 demo 用 `IDispatcher` 抽象支持）
- 红点系统与 UI 框架解耦：UI 只消费 `BadgeState`


