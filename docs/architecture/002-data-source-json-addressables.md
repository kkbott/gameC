# ADR-002 · 玩法数据以 JSON 为源，ScriptableObject 仅作运行时包装

| 字段 | 值 |
| --- | --- |
| 状态 | **已采纳** |
| 日期 | 2026-11-17 |
| 决策者 | 创意总监 + agent |
| 影响系统 | F1 字表、M4 平衡工具、F4 存档 |

## 上下文

字表（60 → 300–500 个条目）是全游戏最重要的数据资产，会被反复调整。同时有三条需求：

1. **M4 平衡工具必须脱离 Unity 运行**（ADR-001 的直接推论）。
2. **数据要能被人审阅和 diff**：300 个字的规则表，靠眼睛过一遍是最后的质量关口。
3. **构建期要能自动校验**：重复部件对、死部件、枚举越界、阶分布。

Unity 的 `.asset`（ScriptableObject）文件由引擎序列化，Unity 之外读不了，且 YAML 结构不适合人工阅读大量条目。

## 决策

- **源文件是 JSON**：`Data/char-table.json` + `Data/component-registry.json`，进 Git。
- **运行时不直接读 JSON**：Editor 导入时构建 `CharTableSO`（ScriptableObject），内部是只读字典（`(glyphId, glyphId) → CharDef`）。
- **禁止在 SO 里手改数值**——会被下次导入覆盖；改数只改 JSON。
- **校验脚本是构建步骤**：JSON 非法则构建失败，不进入运行时。
- 字形等资源走 **Addressables**，与数据分离加载。

## 后果

**正面**

- M4 可直接 `dotnet run` 读 JSON 跑平衡。
- 字表可以 code review、可以写脚本校验（A1–A8 那套验收标准能真的跑）。
- 数据与引擎解耦，将来换引擎不必重写字表。

**代价**

- 多一层导入管线（JSON → SO），需要写导入器。
- 双向编辑容易让人困惑（"我改了 SO 为什么不生效"），需要文档与命名约束。

## 替代方案与否决理由

| 方案 | 否决理由 |
| --- | --- |
| 只用 ScriptableObject | M4 无法离线读；300 条数据无法 diff / code review |
| 运行时直接读 JSON | 每次启动解析开销与 GC；且失去 Unity 的资源引用能力 |
| SQLite / 二进制格式 | 单人项目，工具链成本远超收益 |

## 验证方式

- F1 验收 A1–A8：构建期脚本对 JSON 做 schema 与一致性校验。
- M4 验收 A1：控制台项目只读 JSON、不引用 Unity，编译并运行成功。

## 相关文档

- `design/gdd/character-table.md`（F1）§6.1
- `design/gdd/balance-harness.md`（M4）
