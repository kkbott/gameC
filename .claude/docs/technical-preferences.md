# 技术偏好 — 田字格

> 引擎：**Godot 4.7.2 stable（.NET / mono）**。最后更新：2026-11-17（Unity → Godot 重新立项）

## 引擎与语言

- **引擎**: Godot 4.7.2 stable，**.NET / mono 版**
- **语言**: C# 12 / .NET 8；规则层与表现层同一语言
- **渲染**: Godot Forward+ + CanvasItem 2D；水墨用 `canvas_item` shader
- **输入**: Godot `InputMap` 动作 + `InputEvent`（键鼠 + 手柄）
- **数据**: `Client/Data/*.json` 为唯一源；Sim 用 `System.Text.Json`，View 用 `FileAccess` 读取后交给 Sim
- **UI**: Godot `Control` 体系；CJK 字体用 OFL 授权的思源系，按需生成字形

## 工程结构

| 路径 | 程序集 / 项目 | 规则 |
| --- | --- | --- |
| `Sim/` | `TianziGe.Sim.csproj`（纯 .NET 类库） | **零 Godot 引用**；只用 BCL |
| `Client/` | Godot 工程 + `TianziGe.View.csproj` | Godot API、场景、输入、UI；只读 Sim 状态 |
| `Tests/` | NUnit | 只测试 Sim；可 `dotnet test` |
| `Balance/` | 控制台 | 只引用 Sim；不启动 Godot |
| `Tools/validate-chartable/` | 控制台 | 只引用 Sim；T-007 落地 |

## 命名约定（C#）

| 类别 | 约定 | 示例 |
| --- | --- | --- |
| 类 / 结构体 / 枚举 | `PascalCase` | `FarmState`, `Plot` |
| 公共字段 / 属性 | `PascalCase` | `Money`, `RngState` |
| 私有字段 | `_camelCase` | `_state` |
| 方法 | `PascalCase` | `Validate()`, `Apply()` |
| 常量 | `UPPER_SNAKE_CASE` 或 `PascalCase`（C# 惯用） | `CurrentSaveVersion` |
| 文件 | 与类同名 `PascalCase` | `FarmState.cs` |
| Godot 节点脚本 | 与节点 / 场景职责同名 | `PlotView.cs`, `HudRoot.cs` |
| 场景 / 资源 | `PascalCase` | `Main.tscn`, `PlotView.tscn` |

**命名空间分层是硬要求**：`TianziGe.Sim`（纯 C#，禁止 `using Godot`）与 `TianziGe.View`（Godot 相关）不得互相反向依赖。

## 性能预算

- **目标 60fps / 16.6ms 帧预算**，中低端笔记本可跑（独显非必需）。
- 峰值帧时间不超过 16.6ms；允许建塔 / 祟夜爆兵等瞬间的短时抖动，但不允许持续性掉帧。
- CJK 字形：禁止整包嵌入字体，按需生成字形；具体上限由 T-002 实测决定。

## 测试

- **框架**: NUnit + `dotnet test`（纯 C# 模拟层）。
- **Godot 冒烟**：`godot --headless --path Client -- --boot-probe` 验证 View 程序集能加载 Sim。
- 模拟层必须能在**不启动渲染**的情况下跑完一整季——这是 §5.4 第 1 条的自动化验收方式。
- 平衡验证走"脚本批量跑季"，不靠手动试玩。

## 禁止模式

- ❌ 游戏状态放进 Godot `Node` 的字段或 autoload
- ❌ 单例承载玩法状态（`GameState.Instance`、autoload 全局状态）
- ❌ 规则层写死"当前玩家"
- ❌ `_Process` / `_PhysicsProcess` 的帧时间参与规则判定
- ❌ `GD.Randi` / `RandomNumberGenerator` / `System.Random` / 时间戳作为规则随机源
- ❌ 用 Godot `Resource`（`.tres` / `.res`）充当玩法数据的唯一源
- ❌ 跳过 `TianziGe.Sim` 直接在 View 层写规则

## 允许的库与依赖

- System.Text.Json（存档与字表）
- Godot 内置 API（场景、输入、UI、`FileAccess`）
- GodotSteam / Steamworks.NET（**版本兼容性待 T-004 验证**）

## 待配置项

- [ ] 画质档位与 `Engine.MaxFps` / V-Sync 策略
- [ ] 字体资产与子集策略（T-002）
- [ ] Godot 导出模板与 Windows x64 构建流水线（T-004 后）
