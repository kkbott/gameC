# 田字格 · Tianzi Ge

汉字合字驱动的江南水乡经营游戏。Steam / PC / 单机，中文优先。

> **一句话**：字生田，田生字——用合字给土地下指令，白天写字养地，夜里用你写下的字守住家园。

## 目录导航

| 路径 | 内容 |
| --- | --- |
| [`design/gdd/game-concept.md`](design/gdd/game-concept.md) | 游戏概念文档（当前阶段的主文档） |
| [`docs/engine-reference/unity/`](docs/engine-reference/unity/) | Unity 6000.6 引擎版本参考与弃用 API |
| [`CLAUDE.md`](CLAUDE.md) | 技术栈、架构约束、产品边界（agent 与协作者都从这里起步） |
| [`.claude/docs/technical-preferences.md`](.claude/docs/technical-preferences.md) | 命名约定、性能预算、禁止模式 |
| [`Client/`](Client/README.md) | **Unity 工程将放在这里**（尚未创建） |

## 当前阶段

**概念 → 预制作**。核心循环已定、支柱已定、否决项已定。

下一步顺序：

1. `/map-systems` —— 把概念拆成有依赖关系的系统清单
2. **第 1 个月：可行性原型** —— 3 块田 + 60 字 + 1 季 + 2 场祟夜，只验「合字好不好玩」
3. 原型通过再进纵向切片

> ⚠️ **可行性原型不通过就停，不投美术。** 这是全项目唯一的生死门。

## 不可协商的架构约束

见 [`CLAUDE.md`](CLAUDE.md) 与概念文档 §5.4。四条：模拟与表现分离 / 确定性 tick + 命令式输入 / 存档即状态快照 / 无单例与无单玩家硬编码。

这四条既为将来加联机留口，也让单人版的平衡可自动化验证。

## 仓库状态

- 尚未 `git init`
- 目标远端：`kkbott/gameC`
- 本地暂存位置：agent 会话工作区（**开 Unity 前需搬到正常开发路径**）
