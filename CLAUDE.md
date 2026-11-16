# 田字格 · Tianzi Ge

汉字合字驱动的江南水乡经营游戏。Steam / PC / 单机，中文优先。
概念：`design/gdd/game-concept.md` ｜ 引擎参考：`docs/engine-reference/unity/VERSION.md`

## 技术栈

- **引擎**: Unity 6000.6.x（Unity 6.3 LTS）
- **语言**: C#（主要）
- **构建系统**: Unity 构建管道，目标 Windows x64（首要），macOS / Linux 视时间
- **资源管道**: Unity 资源导入管道 + Addressables（玩法数据：字表 / 字谱）
- **渲染**: URP + 2D Renderer（2D / 2.5D 水墨）
- **UI**: UGUI（运行时）+ UI Toolkit（编辑器工具）；CJK 使用动态字体图集
- **输入**: Input System（PlayerInput + Input Actions），原型阶段即支持手柄
- **测试**: Unity Test Framework（NUnit），EditMode 覆盖纯 C# 模拟层
- **上架**: Steamworks C# 绑定（版本待验证，见 `docs/engine-reference/unity/VERSION.md`）

## 引擎版本参考

@docs/engine-reference/unity/VERSION.md

## 架构约束（不可协商 · 源自 game-concept.md §5.4）

1. **模拟与表现分离** —— 全部游戏状态住在一个纯数据层里，不引用任何渲染 / UI / 引擎节点。验收方式：不启动渲染也能跑完一整季。
2. **确定性 tick + 命令式输入** —— 一次农事轮由固定步长推进；玩家操作表达为命令对象，不直接改状态。
3. **存档 = 状态快照** —— 存档序列化整个状态层，不含引擎对象引用。
4. **禁止全局单例与隐式单玩家假设** —— `player` / `village` 一律作为实体参数传递，规则层禁止写死"当前玩家"。

> 违反以上任一条的代码视为架构债，需在代码评审中拦下。这四条同时降低单人版风险：让平衡可自动化验证、让 bug 可复现、让"字阵胜负"能被脚本批量回归。

## 产品边界（用来砍需求的依据）

见 `design/gdd/game-concept.md` §4.2 反支柱与 §9 否决项。核心四条：

- 不做实时联机（**架构留口，功能延后**）
- 不做 3D 江南（走 2D / 2.5D 水墨）
- 不做战斗系统（冲突只通过田地状态与字阵表达）
- 不做字典 / 教学游戏（只用手写白名单内的常用字）
- 不做开放世界、不做历史剧、不做配音

## 当前阶段

概念 → 预制作。下一步：`/map-systems` 拆系统，然后第 1 个月做「合字好不好玩」的可行性原型。
**可行性原型不通过就停，不投美术。**
