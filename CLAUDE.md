# 田字格 · Tianzi Ge

汉字合字驱动的江南水乡经营游戏。Steam / PC / 单机，中文优先。
概念：`design/gdd/game-concept.md` ｜ 引擎参考：`docs/engine-reference/godot/VERSION.md`

## 技术栈

- **引擎**: Godot **4.7.2 stable**（**.NET / mono 版**，固定此补丁号；升级只在里程碑边界评估）
- **语言**: C# 12 / .NET 8（Sim 与 View 同一语言；不引入 GDScript 作为规则层）
- **工程结构**: `Client/` 是 Godot 工程（View 层）；`Sim/` 是**纯 .NET 类库**（规则层）；`Tests/` 是 NUnit；`Balance/` 是 M4 命令行工具
- **渲染**: Godot Forward+ 渲染器 + CanvasItem 2D；水墨效果用 `canvas_item` shader（不是 Unity URP）
- **UI**: Godot Control 体系（`Control` / `Label` / `RichTextLabel` / 自绘）；CJK 字形按需生成（T-002 验证）
- **输入**: Godot `InputMap` + `InputEvent`（键鼠 + 手柄；不用 Unity Input System）
- **玩法数据**: `Client/Data/*.json` 是唯一源；Sim 用 `System.Text.Json` 解析，View 用 `FileAccess` 读取
- **测试**: NUnit + `dotnet test`（Sim 层）；Godot `--headless` 冒烟（View 层）
- **上架**: Steam（GodotSteam / Steamworks.NET 的兼容性待 T-004 验证）

## 引擎版本参考

@docs/engine-reference/godot/VERSION.md

## 架构约束（不可协商 · 源自 game-concept.md §5.4）

1. **模拟与表现分离** —— 全部游戏状态住在一个纯 C# 数据层（`TianziGe.Sim`）里，不引用任何 Godot / 渲染 / UI / 引擎节点。验收方式：不启动渲染也能跑完一整季。
2. **确定性 tick + 命令式输入** —— 一次农事轮由固定逻辑步推进；玩家操作表达为命令对象，不直接改状态。
3. **存档 = 状态快照** —— 存档序列化整个状态层，不含引擎对象引用。
4. **禁止全局单例与隐式单玩家假设** —— `player` / `village` 一律作为实体参数传递，规则层禁止写死"当前玩家"。

> 违反以上任一条的代码视为架构债，需在代码评审中拦下。这四条同时降低单人版风险：让平衡可自动化验证、让 bug 可复现、让"字阵胜负"能被脚本批量回归。

## 产品边界（用来砍需求的依据）

见 `design/gdd/game-concept.md` §4.2 反支柱与 §9 否决项。核心四条：

- 不做实时联机（**架构留口，功能延后**）
- 不做 3D 江南（走 2D / 2.5D 水墨）
- 不做战斗系统（冲突只通过田地状态与字阵表达）
- 不做字典 / 教学游戏（只用手写白名单内的常用字）
- 不做开放世界、不做历史剧、不做配音

## 当前阶段

概念 → 预制作。**Godot 4.7.2 工程已建**（`Client/` + `Sim/` + `Tests/` + `Balance/`），下一步按 `production/sprints/sprint-01.md` 做「合字好不好玩」的可行性原型。
**可行性原型不通过就停，不投美术。**

## 常用命令

```bash
dotnet test                                   # Sim 层单测（不启动 Godot）
dotnet run --project Balance -- --runs 20     # M4 平衡工具骨架
dotnet build Sim/TianziGe.Sim.csproj          # 只编译规则层

# View / 工程冒烟（需要 Godot 4.7.2 .NET 版在 PATH；见 docs/engine-reference/godot/VERSION.md）
dotnet build Client/TianziGe.View.csproj       # 先构建 View 程序集
godot --headless --path Client -- --boot-probe
```
