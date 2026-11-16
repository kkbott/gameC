# ADR-001 · 模拟与表现分离 + 确定性状态管理

| 字段 | 值 |
| --- | --- |
| 状态 | **已采纳** |
| 日期 | 2026-11-17 |
| 决策者 | 创意总监 + agent |
| 影响系统 | F2 步进、F3 状态层、C1–C7、G1–G5、M4 |

## 上下文

这是第一款游戏、单人开发、9–12 个月工期。同时明确了两个长期约束：

1. **将来可能要加合作模式**（概念决议 D6：先单机，架构为联机留口）。
2. **一个人没有测试人手**，平衡只能靠自动化验证（M4）。

Unity 的默认路径是把玩法状态放在 `MonoBehaviour` 上、用 `Update`/`Time.deltaTime` 驱动、用 `UnityEngine.Random` 取随机数。这条路能让原型很快跑起来，但它同时摧毁了上面两个约束：状态与渲染耦合后无法离线测试，浮点与时间驱动的逻辑无法 bit-exact 复现。

## 决策

1. **全部玩法状态住在一个纯 C# 状态层**（`TianziGe.Sim`），不 `using UnityEngine`，不继承 `MonoBehaviour`。
2. **状态只能通过命令改变**：`ICommand.Validate(state) → ICommand.Apply(state)`，单一入口。
3. **回合制步进，不是实时 tick**：逻辑步由玩家命令驱动，动画时长与规则无关。
4. **唯一随机源**：自实现的 seeded PRNG，状态进存档；禁止 `UnityEngine.Random` / `System.Random` / 时间戳。
5. **判定量用整数或定点**，禁止 `float`/`double` 参与规则。
6. **表现层只读状态 + 订阅事件**，不得回写。
7. **无单例、无"当前玩家"**：`EntityId` 显式传参。

## 后果

**正面**

- 平衡工具 M4 可作为独立控制台项目运行，100 局 < 1 秒。
- Bug 可复现：状态 + 种子 + 命令日志 = 完整一局。
- 将来加联机时，同步量是"每回合几条命令"而非"每帧状态"。
- 状态序列化即存档，即房间状态，一份结构三处复用。

**代价**

- 初期开发比"直接写 MonoBehaviour"慢，需要多写命令类型与状态类。
- 渲染层需要自己同步状态到表现（多做一层映射）。
- 团队（将来）要理解这套分层，学习成本上升。

## 替代方案与否决理由

| 方案 | 否决理由 |
| --- | --- |
| 状态放 `MonoBehaviour` | 无法离线测试 → M4 不可行 → 一个人无法验证平衡 |
| 用 `Time.deltaTime` 驱动逻辑 | 跨平台/跨帧率不可复现，回放与联机校验失效 |
| 用 `UnityEngine.Random` | 状态不可存档，回放必然分歧 |
| 实时 tick（30Hz 模拟） | 本作是回合制，实时模拟没有收益，只增加复杂度与不确定性 |

## 验证方式

- 静态检查：`TianziGe.Sim` 内 0 处 `using UnityEngine`、0 处 `MonoBehaviour`、0 处静态可变字段、0 处 `float` 字段。
- 行为检查：不启动渲染跑完整一季；同 `(状态, 种子, 命令序列)` 的 `ReplayHash` 两次一致。

## 相关文档

- `design/gdd/farm-state.md`（F3）
- `design/gdd/deterministic-step.md`（F2）
- `design/gdd/balance-harness.md`（M4）
- `design/gdd/game-concept.md` §5.4
