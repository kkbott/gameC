# 任务发布说明

> 给人类（委托人）看的：怎么把活派给 agent（或人），以及怎么判断它真的做完了。

---

## 发布流程（三步）

### 第一步：从 Sprint 里挑一个任务

`production/sprints/sprint-01.md` 里的 T1–T15 已经是拆好的任务，每个都带：
- 引用的 GDD 与**验收标准编号**
- 估时
- 依赖前序任务

**一次只发一个任务。** 并发两个以上任务给 agent，会出现两份互不知情的接口实现。

### 第二步：复制模板，填成任务文件

```bash
cp production/tasks/TEMPLATE.md production/tasks/T-002.md
```

填完贴给 agent，**或者**直接说：

> 读 `AGENTS.md`，然后执行 `production/tasks/T-002.md`。

### 第三步：验收

看三样东西，缺一不可：

| 检查 | 合格 |
| --- | --- |
| 验证命令真的跑了 | 任务文件里有实际输出（不是"应该通过"） |
| 铁律没被破坏 | `TianziGe.Sim` 无 `using Godot` / Godot 程序集引用；无直接写状态 |
| 状态与缺口都记了 | `systems-index.md` 状态更新；`## 遗留` 段非空或明确写"无" |

---

## 写任务时的四条规矩

**1. 用验收编号，不用形容词。**
- ✅ "通过 C3 A1、A2、A4、A5"
- ❌ "实现合字校验系统，做好一点"

**2. 必须写「不做什么」。**
不写禁止项的 agent 一定会顺手做点别的，然后你要花时间删掉。

**3. 说明依赖的契约来源。**
"用 `F1.Lookup(glyphIdA, glyphIdB)`，见 `character-table.md` §交互表" —— 不给出处，agent 就会自己发明一个接口。

**4. 一次一个可验证的产出。**
"建工程 + 实现合字 + 做 UI" 是三件事，拆开。

---

## 任务文件命名

```
production/tasks/T-001.md      ← Sprint 01 的 T1
production/tasks/T-002.md      ← Sprint 01 的 T2
...
```

编号与 `sprint-01.md` 的 T 编号对齐，方便对账。

---

## 当前任务清单

| 任务 | 内容 | 状态 |
| --- | --- | --- |
| [T-001](T-001.md) | 建 Godot 工程 + Sim/View 分层 | **已完成（2026-11-17）** |
| [T-002](T-002.md) | CJK 字体 / 竖排验证（**最高风险**） | 待发布 |
| [T-003](T-003.md) | 数据管线：JSON → Sim → Godot 导出 | 待发布 |
| [T-004](T-004.md) | Steam 集成（GodotSteam / Steamworks.NET） | 待发布 |
| T-005 | `FarmState` + `ICommand` 骨架 | 待创建（T-001 已建最小版） |
| … | 见 `sprint-01.md` | |

> T-002 是本月唯一"错了要返工整个 UI 层"的任务，建议在 T-001 后**立刻**发。
> T-003 / T-004 都需要 T-001 的工程；T-004 还需要人类提供 AppID / 测试账号。
