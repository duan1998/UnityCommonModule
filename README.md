# UnityCommonModule - Demos / Prototypes

这个仓库用于沉淀“客户端通用模块”的可运行 Demo（先原型、后 Unity 化），并逐步演进为可复用的 Unity UPM 包。

## 当前内容

### RedDotTree（红点系统原型，C# Console）

位置：`demos/reddot-tree-console/ConsoleApp1/`

能力概览：
- 树结构（入口级粒度）+ dirty/flush 批处理
- 三种红点状态：`Dot / New / Super`（全部在 `RedDotTree` 内）
- **节点级、类型级**的向上传播层数限制（默认无限）
- `SubscribeBadge`：订阅最终展示状态（优先级 `Super > Dot > New`）
- JSON 配置建树（`ConsoleApp1/Configs/reddot.json`）
- 诊断能力：`DumpSubtree(...)`、`Explain(id)`、`ValidateTreeOrThrow()`

## 运行

在 `demos/reddot-tree-console/ConsoleApp1/` 目录：

```bash
dotnet run
```

配置文件会通过 `CopyToOutputDirectory` 拷贝到输出目录：
- `demos/reddot-tree-console/ConsoleApp1/Configs/reddot.json`

## 以后怎么扩展（建议的仓库结构）

当后续 Demo 变多，建议按下面组织：

- `demos/`：可运行 Demo（Console、Unity 场景、压力测试等）
- `packages/`：真正可复用的 Unity UPM 包（Runtime/Editor/Tests）

示例：

- `packages/com.duan.commonmodule.reddot/`
  - `Runtime/` + `*.asmdef`
  - `Editor/`（可选）
  - `Tests/`（可选）
  - `Samples~/`（可选：放演示场景/用法）

## 是否需要做 asmdef / UPM？

- **现在（原型阶段）**：不需要，先把核心抽象与边界跑通。
- **准备落地 Unity 项目复用**：建议做 UPM（`package.json` + `asmdef`），把“通用模块”变成可直接引入的包；Demo 放 `Samples~` 或 `demos/`。


