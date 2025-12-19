# packages/

这里预留给 Unity UPM 包（真正可复用的通用模块）。

建议每个模块一个包，例如：

- `packages/com.duan.commonmodule.reddot/`
  - `package.json`
  - `Runtime/`（含 `*.asmdef`）
  - `Editor/`（可选）
  - `Tests/`（可选）
  - `Samples~/`（可选：演示场景/用法）

原型与验证阶段的 Console/Unity Demo 建议放在 `demos/`。


