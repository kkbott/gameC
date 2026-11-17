# F3 · 游戏状态层（Farm State）

> **状态**: 设计中 · v0.1
> **作者**: 创意总监 + agent
> **最后更新**: 2026-11-17
> **实现支柱**: P4「一季不回头」的载体；同时也是 §5.4 第 1、3、4 条的实现物
> **系统层级**: 基础层 / MVP / 设计顺序第 4
> **上游**: F1 字表（只读）、F2 确定性步进

---

## 概述

F3 是**整个游戏的唯一可变真相**：一个不继承 Godot `Node`、不引用 Godot 的纯 C# 状态容器。田块、字、祟气、村民、字谱、铜钱、RNG 状态、季相——全部住在这里。

它同时是**存档内容**和（将来的）**联机房间状态**：序列化它就是存档，克隆它就是房间。所有玩法子系统都只是在它身上工作的纯函数模块，谁都不许持有自己的状态副本。

一句话：**F3 定义"这世界现在是什么样"，其余系统定义"它怎么变"。**

---

## 玩家幻想

玩家永远不会看到 F3。但它决定了两件玩家能感觉到的事：

1. **世界记得住**：你上次漏掉的那一夜、你烧过的那块田、你教会的那个字，都在。
2. **世界不会骗你**：重开一次、换个设备、读档回来，规则给出的答案必须一样。

---

## 详细设计

### 核心规则

**R1 · 唯一真相，无可选副本**

```
整个运行期只存在一个 FarmState 实例，由 GameSession 持有并通过参数传递。
禁止：静态字段、单例、子系统私有副本、"当前玩家"全局变量。
```

**R2 · 纯数据，零引擎依赖**

`TianziGe.Sim` 程序集内：**不出现 `using Godot`，不引用 GodotSharp**。允许的类型仅限于 BCL（`int`、`string`、`List<T>`、`Dictionary<K,V>`、`record`、`enum`）。

**R3 · 无单玩家假设（为联机留口）**

所有涉及"谁"的规则一律显式接收实体 ID：

```csharp
// ✅
void Compose(EntityId actor, int plotIndex, GlyphId a, GlyphId b, FarmState s);

// ❌  规则层禁止出现
var me = FarmState.CurrentPlayer;
```

MVP 只有一个 `EntityId(0)` 的"田主"，但签名已经为 N 个行动者留好。

**R4 · 值类型化 ID**

```csharp
public readonly record struct GlyphId(int Value);
public readonly record struct CharId(string Value);   // 稳定 slug，来自 F1
public readonly record struct EntityId(int Value);
public readonly record struct SeasonId(int Value);    // 0..3
```

用强类型 ID 而非裸 `int`/`string`，防的是"把田块索引当字 ID 传"这类低级但致命的错误。

**R5 · 字段只能经命令改变**

见 F2 R1。F3 自身不提供任何 `SetXxx` 公开方法。

### 状态结构（Schema v1）

```csharp
public sealed class FarmState
{
    public int SaveVersion;                 // 从 1 开始，不可省
    public ulong RngState;                  // F2 唯一随机源状态

    public int SeasonIndex;                 // 0..3
    public int RoundIndex;                  // 本季第几回合
    public int TotalSteps;                  // 已执行的逻辑步数

    public int Money;                       // 铜钱
    public int QiOfSui;                     // 祟气

    public Plot[] Plots;                    // 固定容量 12，未开垦的 status = Locked
    public HandState Hand;                  // 手牌（规则归 C2）
    public VillageState Village;            // 村民（规则归 G2，MVP 为常量桩）
    public CodexState Codex;                // 字谱（规则归 G1）
    public SeasonState Season;              // 季相与夜次
}

public struct Plot
{
    public PlotStatus Status;               // Locked / Empty / Char / Sui
    public CharId Char;                     // Status==Char 时有效
    public int AppliedRound;                // 该字生效的回合
    public int RemainingRounds;             // duration=rounds:N 的剩余
    public int Soil;                        // 地力 0..100
}

public sealed class VillageState
{
    public int Count = 5;                   // MVP：初值 5，只减不增，下限 2（见 §6.3）
    public List<Villager> Members;          // G2 落地前为空
}

public sealed class CodexState
{
    public HashSet<(GlyphId, GlyphId)> Learned;   // 以部件对为键（C3 硬契约）
}
```

> **`CodexState.Learned` 用部件对而不是 `CharId` 作键**——这是 C3 自审时定下的硬契约：只有这样才能在 `Lookup` 之前判断"免检"，否则 A3 无法实现。

### 状态和转换

**Plot 状态机**

```
Locked --[开垦命令]--> Empty --[合字成功]--> Char
Empty  --[合字失败]--> Sui    --[清除]--> Empty
Char   --[季末洗田 / 自毁]--> Empty
Char   --[祟夜摧毁]--> Sui（地力 −50）
```

**季状态机**

```
SeasonStart --> RoundLoop(1..N) --[祟气满]--> Night --> RoundLoop
RoundLoop --[回合用尽]--> SeasonEnd --[S0..S2]--> SeasonStart
SeasonEnd --[S3 且守住大祟]--> GameWon
SeasonEnd --[漏祟达上限]--> GameLost
```

**顶层流程**（与 P7 分工）

```
Boot --> MainMenu --> NewGame/Continue --> SeasonStart ... --> GameWon/GameLost
```

F3 只持有"一局"的状态；`MainMenu`/`Boot` 属 P7，不进 `FarmState`。

### 与其他系统的交互

| 系统 | 方向 | 接口 | 所有权 |
| --- | --- | --- | --- |
| F1 字表 | 上游（只读） | 读 `CharDef`；**字表不进存档**，存档只存 `CharId` | F1 拥有数据 |
| F2 步进 | 上游 | `Execute(ICommand)` 是唯一写入通道 | F2 拥有通道 |
| C1–C7 | 下游 | 各自实现命令 + 在 `FarmState` 上读字段 | 各系统拥有规则，F3 拥有数据 |
| F4 存档 | 下游 | `Serialize(FarmState) -> json` / `Deserialize` | F4 拥有格式，F3 拥有结构 |
| M4 工具 | 下游 | 直接构造 `FarmState` 并批量跑命令 | — |
| P5/P6/P2 | 下游 | 只读状态 + 订阅 F2 事件，**不得回写** | 表现层 |

---

## 公式

### 4.1 状态哈希（与 F2 共用）

```
hash = FNV1a64(canonicalJson(FarmState))
规范形式 = 字段字典序 + 枚举字符串 + 定点整数 + 集合按稳定键排序
```

### 4.2 存档体积预算

```
12 田块 + 手牌 ≤ 8 + 村民 ≤ 8 + 字谱 ≤ 500 对
预计 JSON 体积 < 120 KB（含命令日志时另计）
```

### 4.3 地力与祟气字段口径

```
Soil:  0..100 整数；祟夜摧毁时 −50，下限 0
QiOfSui: 0..Qmax（F1/概念定 12）；由 C6 按 suiCharCount 等重算，F3 只存值
```

> F3 **不实现**祟气公式——它只提供字段。谁算谁写，见各系统 GDD。

---

## 边缘情况

| # | 情况 | 处理 |
| --- | --- | --- |
| E1 | `SaveVersion` 高于当前程序 | 拒绝加载，提示更新，不尝试"尽力解析" |
| E2 | `SaveVersion` 更低 | 走 F4 的迁移链；迁移失败则拒绝并保留原档 |
| E3 | `CharId` 指向字表已删除的条目 | 加载时该田转为 `Sui` 并记警告（与 F1 E6 一致），不崩溃 |
| E4 | `Plots` 数组长度与当前版本不符（未来扩到 18 块） | 迁移时补 `Locked` 空位；缩容时保留前 N 个并记录被截断的田 |
| E5 | `VillageState.Count` 与 `Members.Count` 不一致 | MVP 阶段只信 `Count`；G2 落地后 `Count` 改为推导属性，禁止双写 |
| E6 | 存档里出现 `float` 字段 | **禁止**：序列化时以定点整数写出；旧档若含浮点则拒绝并记录 |
| E7 | 两个系统同时想写同一字段 | 结构上不可能——只有一个 `Execute` 通道（F2 R1） |

---

## 依赖关系

| 方向 | 系统 | 契约 |
| --- | --- | --- |
| 上游 | F1 字表 | 硬依赖（只读）：提供 `CharDef` |
| 上游 | F2 步进 | 硬依赖：唯一写入通道 |
| 下游 | 全部玩法系统 | 硬依赖：都在这份状态上工作 |
| 下游 | F4 存档 | 硬依赖：序列化目标 |
| 下游 | M4 / M2 / P7 | 读状态或驱动流程 |

### 6.3 MVP 村民的处理（v0.1 澄清）

G2 村民系统排在 Alpha，但 C7 祟夜的防力公式需要村民数。**MVP 阶段的规则是：**

- 初值 `Count = 5`（使开局系数 `1 + 0.1×(5−5) = 1.0`）
- **只减不增**：C7 漏祟时 `Count −1`，下限 2（此时系数降到 0.7）
- **无招募/回归机制**——那是 G2 的事，MVP 不实现
- `Members` 为空列表

> **原表述"固定为 5 常量桩"已于 v0.1 修订**：审计 C7 时发现村民必须能下降（否则"漏祟 → 村民离开"这条概念中的核心惩罚无处落地）。改为"只减不增"后，G2 落地时只需补上"增加"的那一半，公式不变。

---

## 调整旋钮

| 旋钮 | 初值 | 说明 |
| --- | --- | --- |
| 田块容量 | 12（初始解锁 6） | 扩到 18 需要迁移（E4） |
| 手牌容量上限 | 8 | C2 规则，但容量存在 F3 |
| 村民数下限 / 上限 | 2 / 8 | G2 规则 |
| `Soil` 上限 | 100 | C5 规则 |
| 存档字段裁剪 | 关闭（保留全部） | 开启可减压体积 |

---

## 视觉/音频需求

无。

## UI需求

无直接 UI。间接要求：**存档失败/迁移失败必须有明确提示**（E1/E2），不能静默丢档。

---

## 验收标准

- [ ] **A1** 静态检查：`TianziGe.Sim` 内 0 处 `using Godot`；0 处 Godot 程序集引用；0 处静态可变字段
- [ ] **A2** 不启动渲染，构造 `FarmState` + 命令序列，跑完整一整季（6–8 回合 + 祟夜）成功
- [ ] **A3** 序列化往返：`Deserialize(Serialize(s))` 后 `hash(s) == hash(s')`
- [ ] **A4** 克隆隔离：`Clone(s)` 后修改克隆，原状态 hash 不变
- [ ] **A5** 无单玩家假设：用两个不同 `EntityId` 跑同一命令序列，结果仅取决于传入的 actor 参数（静态检查 + 运行时用例）
- [ ] **A6** `SaveVersion` 缺失或高于当前版本时拒绝加载且不损坏原档
- [ ] **A7** `CharId` 失效时加载为 `Sui` 且不崩溃（E3）
- [ ] **A8** 存档内无浮点字段（检查序列化输出中不含除定点字符串外的数值）
- [ ] **A9** 存档体积 ≤ 120 KB（12 田 + 500 字谱对）
- [ ] **A10** MVP 村民桩：`Count = 5` 时 C7 防力系数恒为 1.0（与 C7 联合验证）

---

## 开放问题

| # | 问题 | 负责人 | 需解决时间 |
| --- | --- | --- | --- |
| Q1 | 田块容量将来扩到 18 是否值得（迁移成本 vs 内容量） | 创意总监 | 纵向切片后 |
| Q2 | 是否需要多存档槽（`FarmState` 数组 vs 单例） | 创意总监 | 上架前 |
| Q3 | 命令日志是否随存档保存（与 F2 Q1 联动） | 创意总监 | 上架前 |

---

## 变更记录

| 日期 | 变更 | 依据 |
| --- | --- | --- |
| 2026-11-17 | 创建：纯 C# 单一真相、强类型 ID、无单玩家假设、字谱以部件对为键、MVP 村民常量桩 | `game-concept.md` §5.4 / `character-table.md` v0.2 / `compose-validation.md` v0.1 |
