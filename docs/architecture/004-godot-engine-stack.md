# ADR-004 · 引擎切换：Godot 4.7.2（.NET）+ C#

| 字段 | 值 |
| --- | --- |
| 状态 | **已采纳**（取代 ADR-003） |
| 日期 | 2026-11-17 |
| 决策者 | 创意总监 + agent（人类确认"按 Godot 重新立项"） |
| 影响系统 | 全部；重点 P1 / P2 / P3 / P4 / P5 / M2 / M4 |
| 关联 | ADR-001、ADR-002（决策不变，实现层随引擎调整） |

## 上下文

原技术栈选 Unity 6.3 LTS（编辑器 6000.6.x）。实际落地时遇到两个硬阻塞：

1. 本机没有 Unity Editor，Unity Hub 下载编辑器包时中国区 CDN 反复 404；许可证激活也卡住。连续尝试多种下载路径均未解决。
2. Unity 6000.6 明显超出模型训练数据，而项目是 1 人开发，没有额外的技术验证人手。

2026-11-17，人类确认**按 Godot 重新立项**，并同意顺序：先定引擎与技术栈 → 重写文档与验收 → 重排任务 → 建 Godot 工程。

## 决策

| 层 | 选择 | 理由 |
| --- | --- | --- |
| 引擎 | **Godot 4.7.2 stable（.NET / mono 版）** | 开源、单二进制、无许可证服务器；官方下载可校验；2D 管线成熟 |
| 语言 | **C# 12 / .NET 8**（Sim + View 同一语言） | 保留已批准的"纯 C# 规则层 + 可离线测试"架构；`dotnet test` / `dotnet run` 工具链继续可用 |
| 规则层 | `Sim/TianziGe.Sim.csproj`，纯 .NET 类库 | 零 Godot 引用，可脱离引擎跑完整季与 M4 平衡 |
| 表现层 | `Client/` Godot 工程 + `TianziGe.View.csproj`（`Godot.NET.Sdk/4.7.2`） | Godot 只出现在 View 层；规则不依赖引擎 |
| 渲染 | Godot **Forward+** + CanvasItem 2D；水墨用 `canvas_item` shader | 2D / 2.5D 水墨方向不变；不引入 3D 江南 |
| 文字 | Godot 动态字体（OFL 思源系）+ 按需字形；竖排 T-002 验证 | 中文排版仍是最大技术风险，验证项保留 |
| 输入 | Godot `InputMap` + `InputEvent`（键鼠 + 手柄） | 双输入模型不变；不照搬 Unity Input System |
| 数据 | JSON 源，放在 `Client/Data/`；Sim 用 `System.Text.Json` 解析；View 用 `FileAccess` 读取 `res://Data/` | 见修订后的 ADR-002；数据在 Godot 工程内才能被打包 |
| 构建 / 发布 | Godot 导出模板，Windows x64 优先 | Steam 首发；macOS / Linux 视时间 |
| Steam | **T-004 验证** GodotSteam 或 Steamworks.NET | 不在此 ADR 中预选未验证方案 |

**明确否决**：

- Unity 6.3（下载 / 许可阻塞；继续投入的沉没成本不可控）
- Godot + 纯 GDScript 规则层（失去 `dotnet test` 的离线确定性测试、M4 工具与编译期隔离）
- Godot 表现层 + GDScript / C# 混合规则层（跨语言互操作与边界复杂度高于收益）
- 3D 江南、实时 3D 水墨（与概念 §9 否决项冲突）

## 后果

**正面**

- 引擎可自由下载、无许可证服务器；CI / 新机器可复现。
- 保留纯 C# 规则层：平衡工具、回放、静态架构守卫都能继续用 `dotnet` 跑。
- 2D 管线更轻；Godot 的场景 / 节点模型对单人 2D 项目够用。
- 引擎参考可以从"猜 API"变成"本机实测 + 官方文档"。

**代价 / 风险**

- Godot 4.7.2 同样超出训练数据；所有 API 必须实测或查文档。
- 团队（1 人）需要从 Unity 心智模型切换到 Godot（`Node`、场景树、`InputMap`、`FileAccess`、C# 的 `partial` 脚本类）。
- CJK 动态字形与竖排的成熟度未验证，T-002 仍是最高风险。
- Steam 绑定方案未定，T-004 必须在第一个月跑通。
- 旧 Unity 工单停止（工单层面由调度方关闭）；Unity 分支保留作历史，不再维护。以本 ADR 为新的技术基线。

## 验证方式

- T-001 的可执行验收：`dotnet test`、`dotnet build Client/TianziGe.View.csproj`、`godot --headless --path Client -- --boot-probe`（见 `production/tasks/T-001.md` 的实际输出）。
- T-002 ~ T-004 分别验证字体、数据打包、Steam。
- `docs/engine-reference/godot/VERSION.md` 维护版本与已验证项清单。

## 相关文档

- `CLAUDE.md` / `.claude/docs/technical-preferences.md`
- `docs/engine-reference/godot/VERSION.md`
- `production/tasks/T-001.md` ~ `T-004.md`
- `production/sprints/sprint-01.md`
