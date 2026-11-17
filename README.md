# 田字格 · Tianzi Ge

汉字合字驱动的江南水乡经营游戏。Steam / PC / 单机，中文优先。

> **一句话**：字生田，田生字——用合字给土地下指令，白天写字养地，夜里用你写下的字守住家园。

## 目录导航

| 路径 | 内容 |
| --- | --- |
| [`design/gdd/game-concept.md`](design/gdd/game-concept.md) | 游戏概念文档（当前阶段的主文档） |
| [`docs/engine-reference/godot/`](docs/engine-reference/godot/) | Godot 4.7.2 引擎版本参考与已验证项 |
| [`CLAUDE.md`](CLAUDE.md) | 技术栈、架构约束、产品边界（agent 与协作者都从这里起步） |
| [`.claude/docs/technical-preferences.md`](.claude/docs/technical-preferences.md) | 命名约定、性能预算、禁止模式 |
| [`Client/`](Client/README.md) | **Godot 工程**（View 层） |
| `Sim/` | 纯 C# 规则层（零 Godot 引用） |
| `Tests/` | Sim 层 NUnit 测试 |
| `Balance/` | M4 平衡回归工具 |
| `Sim/` | 纯 C# 规则层（不引用 Godot） |
| `Tests/` | Sim 层 NUnit 测试 |
| `Balance/` | M4 平衡回归工具 |
| `Client/Data/` | 字表 / 部件表 JSON（唯一数据源） |

## 当前阶段

**概念 → 预制作**。核心循环已定、支柱已定、否决项已定；**Godot 4.7.2（.NET）工程与 Sim/View 分层已落地**。

下一步顺序：

1. **第 1 周技术验证**：CJK 字体（T-002，最高风险）、数据管线（T-003）、Steam 绑定（T-004）
2. **第 1 个月：可行性原型** —— 3 块田 + 60 字 + 1 季 + 2 场祟夜，只验「合字好不好玩」
3. 原型通过再进纵向切片

> ⚠️ **可行性原型不通过就停，不投美术。** 这是全项目唯一的生死门。

## 不可协商的架构约束

见 [`CLAUDE.md`](CLAUDE.md) 与概念文档 §5.4。四条：模拟与表现分离 / 确定性 tick + 命令式输入 / 存档即状态快照 / 无单例与无单玩家硬编码。

这四条既为将来加联机留口，也让单人版的平衡可自动化验证。

## 仓库状态

- Git 仓库已初始化，远端 `kkbott/gameC`
- 引擎：**Godot 4.7.2 stable（.NET / mono）**，工程在 `Client/`
- 规则层：`Sim/TianziGe.Sim.csproj`，可用 `dotnet test` 脱离引擎验证
- 引擎切换记录：`docs/architecture/004-godot-engine-stack.md`

## 常用命令

```bash
dotnet test
dotnet run --project Balance -- --runs 20
godot --headless --path Client -- --boot-probe
```
