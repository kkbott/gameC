# AGENTS.md · 田字格（Tianzi Ge）

> 本文件是**任何 AI agent 在本仓库开工前的第一入口**。
> 人类协作者也建议读一遍——它说明了这个项目的约束和边界。
> 若你用 Claude Code，`CLAUDE.md` 是更详细的技术栈版本；两者不冲突，本文件优先。

---

## 0. 一句话

**汉字合字驱动的江南水乡经营游戏。** 玩家把偏旁拖进田里合成汉字，字决定土地产出什么，夜里用白天写下的字守住家园。

---

## 1. 开工前必读（按此顺序，别跳）

| # | 文件 | 为什么读 |
| --- | --- | --- |
| 1 | `AGENTS.md` | 本文件：约束与边界 |
| 2 | `CLAUDE.md` | 技术栈 + **四条不可协商架构约束** + 产品边界 |
| 3 | `design/gdd/systems-index.md` | 29 个系统、依赖分层、**哪些已设计**、设计顺序 |
| 4 | 任务指定的 GDD → 及其上游 GDD 的「交互」章节 | 接口契约全在这里 |
| 5 | `docs/architecture/`（ADR） | 已定的技术决策**及其否决理由** |
| 6 | `docs/engine-reference/godot/VERSION.md` | Godot 4.7.2 版本风险 + 待人工核对清单 |
| 7 | `production/stage.txt` | 当前阶段 |

**读完后，先向委托人复述三件事再动手**：
- 这个任务要交付什么
- 它依赖哪些既有契约（接口名 + 来自哪份文档）
- 它**明确不做什么**

复述不对，就先别写代码。这是最便宜的纠错点。

---

## 2. 铁律（违反任一条 = 工作作废）

1. **`TianziGe.Sim` 严禁 `using Godot`、严禁引用 GodotSharp。** 纯 C#，可脱离 Godot 用 `dotnet build` / `dotnet test` 编译运行。Godot 只存在于 `Client/`（View 层）。
2. **状态只能经 `ICommand` 改变。** 禁止任何子系统直接写 `FarmState` 字段。
3. **判定量用整数或定点，禁止 `float`/`double` 参与规则。** 显示层可用浮点，但不得回写状态。
4. **禁止单例、禁止隐式"当前玩家"。** `EntityId` 一律显式传参；不要用 Godot autoload 承载玩法状态。
5. **随机只能来自 `DeterministicRng`。** 禁止 `GD.Randi` / `RandomNumberGenerator` / `System.Random` / 时间戳 / `Guid`。
6. **不确定的 Godot 4.7.2 API 不许编。** 该版本超出多数模型训练数据；要么用本机 Godot 实测、要么查官方文档并给链接；暂时查不到的，写明"不确定，需验证"，不要用自信语气包装猜测。
7. **不许扩范围。** 任务范围外的东西写成 `TODO` 记进任务文件，不要顺手做。
8. **不许改设计决策。** `design/gdd/` 里的规则属于创意总监；你可提"建议变更"，不可直接改。
9. **不许打包未授权字体。** 只允许 SIL OFL / Apache 2.0 等明确可商用的字体，且 LICENSE 必须入库。
10. **不许提交密钥、Steam AppID、个人凭据。**

---

## 3. 任务的验收方式：机器说话，不是口头保证

每个任务都必须有**可执行的验证**。常用的四种：

| 类型 | 命令 | 对应 |
| --- | --- | --- |
| 纯逻辑 | `dotnet test` | Sim 层全部 |
| 数据 | `dotnet run --project Tools/validate-chartable` | 字表 JSON（工具在 T-007 落地） |
| 平衡 | `dotnet run --project Balance -- --runs 20` | 平衡回归 |
| 表现 / 工程 | `dotnet build Client/TianziGe.View.csproj` + `godot --headless --path Client -- --boot-probe` | View 层与工程可启动 |

**"我做完了"不算验收。** 没有可跑命令的交付物，不算完成。

**总账在 `design/gdd/acceptance-matrix.md`**：五层验收（L1 数据 / L2 单元 / L3 集成 / L4 平衡 / L5 表现）、跨文档引用用限定 ID（`F1-A3` 而非裸 `A3`）、以及**必须人工验收的 6 项**明确列出。开工前先看它，能省掉很多返工。

---

## 4. 完成一个任务时必须做的三件事

1. **更新状态**：`design/gdd/systems-index.md` 里对应系统的状态（未开始 → 审查中 → 已批准）。
2. **记录验证**：在任务文件里贴实际命令输出（截取关键行即可）。
3. **留缺口**：未完成 / 有疑问 / 需要决策的点，写进任务文件的 `## 遗留` 段，**不许静默丢弃**。

---

## 5. 需要人类决策时怎么办

遇到下列情况**停下来提问**，不要自己拍板：

- 要改动 §2 铁律中的任何一条
- 两份 GDD 互相矛盾（记录到 `design/gdd/systems-index.md` §7 悬置项）
- 需要选择 Godot 4.7.2 中行为未验证的 API，且无法在本机实测
- 任务描述与 GDD 验收标准冲突
- 需要新增系统（不在 29 个系统清单内）

---

## 6. 当前状态（2026-11-17）

| 项 | 状态 |
| --- | --- |
| 阶段 | **PreProduction**（`production/stage.txt`） |
| 引擎 | **Godot 4.7.2 stable（.NET / mono）** |
| MVP 系统设计 | **17/17 已批准** |
| 字表 | 50 条 / 26 部件已落地，**未经机器校验** |
| Godot 工程 | **已创建**：`Client/`（View）+ `Sim/`（纯 C#）+ `Tests/` + `Balance/` |
| 当前冲刺 | `production/sprints/sprint-01.md`（4 周可行性原型） |
| 下一个任务 | `production/tasks/T-002.md`（CJK 字体验证，最高风险） |

---

## 7. 这个项目最容易犯的三个错

1. **把状态写进 Godot `Node`。** 快，但会让 M4 平衡工具和将来的联机同时报废。T-001 已经把这道墙立起来，后续任务不许绕过。
2. **给某个字写特判。** `if (def.Char == "秋")` 会让 300 字的内容量变成 300 个分支，一个人维护不动。行为只由 `template` 决定。
3. **先做美术。** 整个项目唯一的生死门是「合字好不好玩」。在 Sprint 01 给出结论前，任何美术投入都是赌。

> 从 Unity 迁移过来的旧习惯（`MonoBehaviour`、`Update`/`Time.deltaTime` 驱动规则、`UnityEngine.Random`）一律按铁律 1/3/5 处理，不要照搬。
