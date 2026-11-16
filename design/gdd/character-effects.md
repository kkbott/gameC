# C4 · 字效系统（Character Effects）

> **状态**: 设计中 · v0.1
> **作者**: 创意总监 + agent
> **最后更新**: 2026-11-17
> **实现支柱**: P1 字即是手、P2 认得就是得到
> **系统层级**: 核心层 / MVP / 设计顺序第 6
> **上游**: [F1 字表](character-table.md) v0.2、[C1 田块](plots-and-slots.md) v0.1、[C3 合字校验](compose-validation.md) v0.1

---

## 概述

C4 把「一个字」变成「土地上的变化」。F1 声明了 13 个效果模板（`template`），C4 负责**实现这 13 个模板的行为**，并规定它们怎么结算、怎么叠加、什么时候失效。

它是一层**纯派发与结算逻辑**：不判断组合对不对（C3 管），不产生铜钱（C5 管），不汇总防力（C7 管）。**任何"某个字特殊处理"的代码都是缺陷**——字只有 `template`，行为只由模板决定。

---

## 玩家幻想

> **「我写的字，地照做了。」**

玩家写的不是 buff，是**指令**。所以反馈必须**具体、可见、当步发生**：

- 写 `秋`，全场立刻黄了——不是"产量 +40%"，是**画面瞬间成熟**。
- 写 `休`，那块田真的荒一季——玩家会肉疼，这正是 P4 要的。
- 写 `林`，祟夜第一波撞在树上——玩家会记得"林救了我一次"。

抽象数值属于战斗游戏，**这片地上的一切都必须有形**。

---

## 详细设计

### 核心规则

**R1 · 按 template 派发，禁止按字特判**

```csharp
// ✅
switch (def.Template) { case Template.AllRipen: ... }

// ❌ 禁止
if (def.Char == "秋") { ... }
```

字表新增一个字，只要复用已有 template，**C4 零改动**。这是"内容靠规则组合"能否成立的技术前提。

**R2 · 三种结算时机**

| 时机 | 触发 | 模板 |
| --- | --- | --- |
| 即时（合字当步） | C3 成功分支 | `single_ripen` `all_ripen` `clear_sui` `villager_favor` `spirit_affinity` |
| 回合结算 | 每回合末，F2 R5 第 2 步 | `adjacent_boost` `sustained_yield` `fallow_restore` `soil_guard` |
| 防御期读取 | 祟夜结算时被 C7 读取 | `ward_wave` `reveal_stealth` `will_boost` `hand_growth` |

**R3 · 13 个模板的实现契约**

| 模板 | 结算 | 效果 | 写入状态 |
| --- | --- | --- | --- |
| `single_ripen` | 即时 | 该田产出 ×3，田转 `Empty` | `Money += 计算值`（交 C5） |
| `all_ripen` | 即时 | 所有 `Char` 田各产出 ×2，所有田转 `Empty`；受 `perSeasonLimit` | 批量产出 |
| `adjacent_boost` | 回合 | 每个相邻 `Char` 田产出 +50%（可叠，总上限 ×2.5） | 记入本回合产出修正 |
| `sustained_yield` | 回合 | 每回合固定产出，持续 N 回合 | `RemainingRounds--` |
| `clear_sui` | 即时 | 所有 `Sui` 田转 `Empty` | 清 `suiCharCount` |
| `fallow_restore` | 回合 | 本回合不产，`Soil` 回满 100 | `Soil = 100`，田保持 `Char` |
| `soil_guard` | 回合 | 本季内该田 `Soil` 不被祟夜削减 | 标记位（推导） |
| `ward_wave` | 祟夜 | 抵掉祟夜第一波（按波次序号） | 该夜间消耗 |
| `reveal_stealth` | 祟夜 | 隐身祟不再获得闪避，正常计入防力 | 该夜间生效 |
| `will_boost` | 祟夜 | 该田战意 +2 | **推导，不存字段**（见 R5） |
| `villager_favor` | 即时 | 村民好感 +1（G2 落地前为无效空操作） | 交 G2 |
| `hand_growth` | 即时 | 下一回合手牌上限 +1（一回合） | 交 C2 |
| `spirit_affinity` | 即时 | 字灵亲密度 +1（G3 落地前为无效空操作） | 交 G3 |

**R4 · 产出换算的边界（与 C5 切分）**

C4 **不产生铜钱**，只产生产出修正：

```
C4 输出：YieldModifier[] { plotIndex, multiplier }
C5 输入：baseYield × YieldModifier × soilFactor → Money
```

这样"字的强力程度"（C4）和"经济曲线"（C5）可以**独立调参**，不必改同一个公式。

**R5 · `will_boost` 不存状态**

战意加成是**纯推导**：只要田里是这个字、模板是 `will_boost`，就 +2。写入 `FarmState` 会让存档多一个可能与字脱同步的字段（F2 确定性风险）。

```
plotWillBonus(p) = (p.Status == Char && Template(p.Char) == WillBoost) ? +2 : 0
```

**R6 · 叠加规则**

| 组合 | 规则 |
| --- | --- |
| 同模板多块田 | 数值可叠，但受各自上限约束（`adjacent_boost` 总上限 ×2.5） |
| 不同模板 | 可共存，按 R2 的时机分别结算 |
| 同名不同模板 | 允许存在（F1 保证同部件对只有一条，所以实际不会出现） |
| `all_ripen` 与 `single_ripen` 同回合触发 | 按 F2 R5 顺序，`all_ripen` 先（影响面大），避免同田被算两次 |

**R7 · 效果不残留**

田被祟夜摧毁 → 该田所有持续效果**立即终止**，不迁移、不补偿（与 C3 E5 一致）。

### 状态和转换

C4 自身无状态。它读 `FarmState` 并在结算 Step 内产生 `EffectDelta`。状态由 F3 持有，经由 F2 命令应用。

**一次合字后的时序**

```
C3 成功分支
  → C4.OnComposed(plot, charDef)
      instantaneous 模板 → 当步结算，产出 delta 交 C5
      持续模板        → 写入 RemainingRounds / 标记
  → P5 播合流动画（Step 结束后）
```

### 与其他系统的交互

| 系统 | 方向 | 接口 | 所有权 |
| --- | --- | --- | --- |
| F1 字表 | 上游 | 读 `template` / `category` / `tier` / `duration` / `perSeasonLimit` | F1 拥有数据 |
| C1 田块 | 上游 | 读 `neighbors(i)` / `RemainingRounds` / `PlotStatus` | C1 拥有空间与寿命 |
| C3 校验 | 上游 | `OnComposed(plot, def)` 被调用 | C3 触发，C4 实现 |
| C5 经济 | 下游 | 交出 `YieldModifier[]`；`Money` 由 C5 写 | **C4 不碰 Money** |
| C6 祟气 | 下游 | `clear_sui` 改变 `suiCharCount` | C6 重算祟气 |
| C7 祟夜 | 下游 | 读 `ward_wave` / `reveal_stealth` / `plotWillBonus` | C7 拥有防力汇总 |
| C2 偏旁池 | 下游 | `hand_growth` 修改下一回合手牌上限 | C2 拥有手牌 |
| G2 / G3 | 下游 | `villager_favor` / `spirit_affinity` | MVP 阶段为空操作 |
| P5 / P6 | 下游 | 抛 `effect_applied` 事件 | — |

---

## 公式

### 4.1 该田本回合产出修正

```
yieldMul_eff(c)     = F1 §4.2 的产出系数
adjBonus(p)         = min( 1 + 0.5 × 相邻 adjacent_boost 田数, 2.5 )
yieldModifier(p)    = yieldMul_eff(c) × adjBonus(p) × seasonAffinityMul(p)
```

### 4.2 战意修正（给 C7）

```
plotWillBonus(p) = will_boost ? 2 : 0
```

### 4.3 即时产出的数量

```
single_ripen:  immediateGold = 3 × baseGold(soilLevel)
all_ripen:     immediateGold = 2 × baseGold  (每块 Char 田)
sustained_yield: perRoundGold = 1 × baseGold
baseGold(soil) = C5 定义（本系统只给倍数）
```

---

## 边缘情况

| # | 情况 | 处理 |
| --- | --- | --- |
| E1 | `all_ripen` 与某田 `adjacent_boost` 同回合 | 先 `all_ripen`（R6），已被清空的田不再参与相邻加成 |
| E2 | `fallow_restore` 的田被祟夜摧毁 | 地力先回满再被削 50（F2 R5：产出结算第 2 步，摧毁第 5 步） |
| E3 | `soil_guard` 与 `fallow_restore` 同一田 | 不可能（一块田只有一个字）。若数据出错则按 `soil_guard` 优先 |
| E4 | `hand_growth` 与 C2 手牌上限同时变化 | 加成只在"下一回合"有效，用回合序号标记，不累加 |
| E5 | G2/G3 未落地时触发 `villager_favor` | **空操作但记日志**，不报错。这是 MVP 合法状态 |
| E6 | `ward_wave` 遇到祟夜无波次（空夜） | 消耗无效，不保留到下一夜（防囤积） |
| E7 | 同一模板字写多块田的数值叠加 | 除 `adjacent_boost` 有上限外，其余按数量线性叠——但受 F1 `repeatPenalty` 与 `perSeasonLimit` 抑制 |
| E8 | 效果触发时目标田已不在 `Char` 状态 | 跳过该目标，不抛异常（Step 内状态可能已被前序效果改变） |

---

## 依赖关系

| 方向 | 系统 | 契约 |
| --- | --- | --- |
| 上游 | F1 字表 | 硬依赖：13 个 `template` 枚举与 `category` 主导属性 |
| 上游 | C1 田块 | 硬依赖：邻接、寿命、状态 |
| 上游 | C3 校验 | 硬依赖：`OnComposed` 调用时机 |
| 上游 | F2 步进 | 硬依赖：R2 时机表必须挂在 F2 的固定结算顺序上 |
| 下游 | C5 / C6 / C7 / C2 / G2 / G3 | C4 产出修正与事件，不写它们的字段 |

---

## 调整旋钮

| 旋钮 | 初值 | 安全范围 | 太高会怎样 | 太低会怎样 |
| --- | --- | --- | --- | --- |
| `single_ripen` 倍数 | ×3 | ×2–×4 | 单田爆产使布局无意义 | 没人写产出字 |
| `all_ripen` 倍数 | ×2 | ×1.5–×3 | 一季一次的强字变成唯一解 | 强字不值得憋 |
| `adjacent_boost` 单块加成 | +50% | +25%–+75% | 相邻布局压倒一切 | 相邻玩法失去吸引力 |
| 相邻加成总上限 | ×2.5 | ×1.8–×3.0 | 无限放大，经济崩 | 上限形同虚设 |
| `sustained_yield` 持续回合 | 3 | 2–5 | 一次投入长期收益，其他模板变废 | 持续时间太短，不如即时 |
| `will_boost` 加成 | +2 | +1–+3 | 防御字堆叠，祟夜必过 | 加成不值一个格 |

---

## 视觉/音频需求

- 每个模板需要**可辨识的场地反馈**：成熟（金色漫过）、荒（褪色+干裂）、林（树影遮住田）、水（水纹流入）。
- **同一模板在不同字上必须长得一样**——玩家要能"看形知效"，这是 P2 的可读性基础。
- `all_ripen` 必须有全屏量级的动势，它是本作最强的即时爽感，不能做成"数字跳一下"。
- 细则归 P5 / P6。

## UI需求

- 田块上显示当前字所属**模板类别**（产出/地力/防御/关系）的图标，而非只有字。
- 持续效果显示剩余回合数。
- `adjacent_boost` 生效时，相邻田应有连线或流动暗示。

---

## 验收标准

- [ ] **A1** 13 个模板全部有实现分支；`switch` 无 `default` 兜底以外的硬编码字名
- [ ] **A2** 静态检查：C4 内不出现 `Money +=`（经济写入必须经 C5）
- [ ] **A3** 静态检查：C4 内不出现 `def.Char ==`（禁止按字特判）
- [ ] **A4** `will_boost` 不产生 `FarmState` 字段写入（F3 schema 无新增字段）
- [ ] **A5** `adjacent_boost` 叠乘不超过 ×2.5（构造 4 邻田用例）
- [ ] **A6** `all_ripen` 先于 `single_ripen` 结算（构造同回合用例）
- [ ] **A7** 田被摧毁后持续效果立即终止，无残留（构造 rounds 效果中途被毁用例）
- [ ] **A8** G2/G3 未落地时 `villager_favor` / `spirit_affinity` 为空操作且不报错
- [ ] **A9** 新增一个只用已有模板的字，C4 代码零改动即可生效（回归测试：用 mock 字表加一条）
- [ ] **A10** 每个模板至少有 1 个"可看形知效"的表现资源（与 P5 联合验收）

---

## 开放问题

| # | 问题 | 负责人 | 需解决时间 |
| --- | --- | --- | --- |
| Q1 | `sustained_yield` 与 `adjacent_boost` 是否会形成"种一片果林"的最优解？需不需要设计反制 | 创意总监 | 首个试玩后 |
| Q2 | 模板数量 13 是否够撑 300 字？可能最终需要 18–20 个模板 | 创意总监 | 纵向切片前 |
| Q3 | `reveal_stealth` 依赖"祟有隐身属性"，C7 必须支持；若 C7 不做隐身，此模板需替换 | 创意总监 + 设计 | 设计 C7 前 |

---

## 变更记录

| 日期 | 变更 | 依据 |
| --- | --- | --- |
| 2026-11-17 | 创建：13 模板契约、三档时机、不碰 Money、will_boost 推导不存字段、叠加上限 | `character-table.md` v0.2 / `plots-and-slots.md` v0.1 |
