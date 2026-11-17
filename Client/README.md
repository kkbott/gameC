# Client —— Godot 工程（View 层）

**Godot 4.7.2（.NET / mono）** 工程放在这个目录里：

```
gameC/
├── design/          # 设计文档（仓库根）
├── docs/            # 引擎参考
├── Sim/             # 纯 C# 规则层（零 Godot 引用）
├── Tests/           # NUnit
├── Balance/         # M4 平衡工具
├── CLAUDE.md
└── Client/          # ← Godot 工程根（View）
    ├── project.godot
    ├── TianziGe.View.csproj
    ├── Data/              # 字表 / 部件表 JSON（唯一源；在 res:// 内才能导出）
    ├── scenes/
    └── src/View/
```

## 为什么工程不放在仓库根

设计文档与工具是这个仓库的一等公民，而 Godot 会生成 `.godot/`（导入缓存 / C# 构建产物）。目录分层让"文档 vs 代码 vs 引擎缓存"的边界一眼可见；`.gitignore` 只忽略生成物，不忽略工程文件本身。

## 打开与验证

需要 **Godot 4.7.2 .NET 版**（标准版不能编译 C#）与 **.NET SDK 8.0**。

```bash
# 用编辑器打开 Client/（首次会导入资源并生成 .godot/）
godot --path Client --editor

# 构建 C# 程序集（也可用 godot --headless --path Client --build-solutions --quit）
dotnet build Client/TianziGe.View.csproj

# 无头启动冒烟（Bootstrap 打印 Sim 状态后退出）
godot --headless --path Client -- --boot-probe
```

## 分层约定

- `Client/` 放 View：场景、节点脚本、输入映射、UI、shader；`Client/Data/` 放**唯一数据源** JSON（放进 Godot 工程才能保证导出后可读，见 ADR-002）。
- 规则、状态、数据解析全部在 `Sim/`；`Client/TianziGe.View.csproj` 通过 `ProjectReference` 单向引用 `Sim/TianziGe.Sim.csproj`。
- **禁止**在 `Client/` 里直接改 `FarmState` 字段；所有状态变化必须走 `ICommand`。
- **禁止**把 `using Godot` 写进 `Sim/`（`Tests/ArchitectureTests.cs` 会检查）。

## 生成物（不要提交）

`.godot/`、`bin/`、`obj/`、`build/`、`export_credentials.cfg` 已由根目录 `.gitignore` 覆盖。`project.godot`、`TianziGe.View.csproj`、`TianziGe.View.sln`（若编辑器生成）与场景 / 脚本必须入库。
