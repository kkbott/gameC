# 技术偏好 — 田字格

> 引擎：Unity 6000.6.x（Unity 6.3 LTS）。最后更新：2026-11

## 引擎与语言

- **引擎**: Unity 6000.6.x（Unity 6.3 LTS）
- **语言**: C#（主要）；不引入额外脚本语言
- **渲染**: URP + 2D Renderer
- **输入**: Input System（InputAction 资产）
- **数据**: ScriptableObject + Addressables（字表 / 字谱 / 数值）

## 命名约定（Unity C#）

| 类别 | 约定 | 示例 |
| --- | --- | --- |
| 类 / 结构体 / 枚举 | `PascalCase` | `FarmState`, `Plot` |
| 公共字段 / 属性 | `PascalCase` | `MoveSpeed` |
| 私有字段 | `_camelCase` | `_moveSpeed` |
| 方法 | `PascalCase` | `TakeDamage()` |
| 常量 | `UPPER_SNAKE_CASE` | `MAX_PLOTS` |
| 文件 | 与类同名 `PascalCase` | `FarmState.cs` |
| 纯数据层命名空间 | `TianziGe.Sim` | 与 `TianziGe.View` 严格分离 |

**命名空间分层是硬要求**：`TianziGe.Sim`（纯 C#，禁止 `using UnityEngine`）与 `TianziGe.View`（引擎相关）不得互相反向依赖。

## 性能预算

- **目标 60fps / 16.6ms 帧预算**，中低端笔记本可跑（独显非必需）。
- 峰值帧时间不超过 16.6ms；允许建塔 / 祟夜爆兵等瞬间的短时抖动，但不允许持续性掉帧。
- CJK 动态字体图集：禁止整包嵌入字体，按需生成字符图集。

## 测试

- **框架**: Unity Test Framework（NUnit）。
- **EditMode 测试覆盖纯 C# 模拟层**：字表校验、祟气累积、祟夜判定、存档往返（save → load → 状态相等）。
- 模拟层必须能在**不启动渲染**的情况下跑完一整季——这是 §5.4 第 1 条的自动化验收方式。
- 平衡验证走"脚本批量跑季"，不靠手动试玩。

## 禁止模式

- ❌ 游戏状态放进 `MonoBehaviour` 字段
- ❌ 单例承载玩法状态（`GameManager.Instance`）
- ❌ 规则层写死"当前玩家"
- ❌ `Resources.Load` 加载玩法数据
- ❌ `Time.deltaTime` 参与规则判定
- ❌ `JsonUtility` 存储字典 / 多态
- ❌ 跳过 `TianziGe.Sim` 直接在 View 层写规则

## 允许的库与依赖

- System.Text.Json 或 Newtonsoft.Json（存档）
- Addressables（资源与数据加载）
- Input System
- Steamworks C# 绑定（**版本兼容性待验证**，见 `docs/engine-reference/unity/VERSION.md` 人工核对清单）

## 待配置项

- [ ] 画质档位与 `Application.targetFrameRate` 策略
- [ ] 字体资产与子集策略（按 300–500 字白名单生成子集）
- [ ] 构建流水线（本地构建 vs CI）
