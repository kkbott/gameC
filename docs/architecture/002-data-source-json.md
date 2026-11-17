# ADR-002 · 玩法数据以 JSON 为源，运行时由 Sim 直接解析

| 字段 | 值 |
| --- | --- |
| 状态 | **已采纳**（2026-11-17 引擎切换后修订实现层） |
| 日期 | 2026-11-17 |
| 决策者 | 创意总监 + agent |
| 影响系统 | F1 字表、M4 平衡工具、F4 存档 |

## 上下文

字表（60 → 300–500 个条目）是全游戏最重要的数据资产，会被反复调整。同时有三条需求：

1. **M4 平衡工具必须脱离引擎运行**（ADR-001 的直接推论）。
2. **数据要能被人审阅和 diff**：300 个字的规则表，靠眼睛过一遍是最后的质量关口。
3. **构建期要能自动校验**：重复部件对、死部件、枚举越界、阶分布。

引擎序列化资源（Unity 的 `.asset` / ScriptableObject，Godot 的 `.tres` / `.res`）由引擎读写：M4 工具不启动引擎时读不了，且资源格式不适合人工审阅 300–500 条数据。

## 决策

- **源文件是 JSON**：`Client/Data/char-table.json` + `Client/Data/component-registry.json`，进 Git。
- **Sim 直接解析 JSON**：`TianziGe.Sim` 用 `System.Text.Json` 把 JSON 读成只读字典（`(glyphId, glyphId) → CharDef`），不经过任何引擎资源导入步骤。
- **View 只负责提供字节 / 文本**：Godot 侧用 `FileAccess.GetFileAsString("res://Data/...")` 读取后交给 Sim；Sim 不接触 `res://`、`FileAccess` 或任何 Godot 类型。
- **禁止在引擎资源里手改数值**：`.tres` / `.res` 不是数据源；改数只改 JSON。
- **校验脚本是构建步骤**：JSON 非法则构建失败，不进入运行时（T-007 落地 `Tools/validate-chartable`）。
- 字形等表现资源走 Godot `Resource` / `ResourceLoader`，与玩法数据分离；不引入 Unity Addressables。

## 后果

**正面**

- M4 可直接 `dotnet run` 读 JSON 跑平衡。
- 字表可以 code review、可以写脚本校验（A1–A8 那套验收标准能真的跑）。
- 数据与引擎解耦，将来换引擎不必重写字表。

**代价**

- JSON → 运行时对象需要一次解析；数据量大时启动开销需要测（300–500 条量级可控）。
- 没有引擎导入器的资源引用校验，需要靠 `Tools/validate-chartable` 与测试补。
- 导出时必须确认 `res://Data/*.json` 被包含进包（T-003 验证）。

## 替代方案与否决理由

| 方案 | 否决理由 |
| --- | --- |
| 只用引擎资源（SO / Resource） | M4 无法离线读；300 条数据无法 diff / code review |
| 引擎导入器 + 资源包装 | 多一层易错管线；Godot 的 C# 资源导入需要额外工程代码，收益低 |
| SQLite / 二进制格式 | 单人项目，工具链成本远超收益 |

## 验证方式

- F1 验收 A1–A8：构建期脚本对 JSON 做 schema 与一致性校验（`Tools/validate-chartable`，T-007）。
- M4 验收 A1：控制台项目只读 JSON、不引用引擎，编译并运行成功。
- T-003：验证 Godot 导出产物内包含 JSON，且 View 能通过 `FileAccess` 读取。

## 相关文档

- `design/gdd/character-table.md`（F1）§6.1
- `design/gdd/balance-harness.md`（M4）

## 修订记录

| 日期 | 变更 |
| --- | --- |
| 2026-11-17 | 引擎切换为 Godot 4.7.2（ADR-004）。保留"JSON 为源、引擎资源不作源"的决策；删除 ScriptableObject + Addressables 导入层，改为 Sim 用 `System.Text.Json` 直接解析、View 用 `FileAccess` 提供文本。 |
