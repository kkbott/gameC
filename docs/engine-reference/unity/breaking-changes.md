# Unity 版本间重大变更

> 只记录**影响本项目**的变更。最后验证：2026-11
> ⚠️ 6.1 / 6.2 / 6.3 的条目**尚未验证**——请对照 [官方 6.3 升级指南](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity63) 补齐后再依赖本文件。

## 2023.x → 6000.0（Unity 6）（置信度：中高）

| 变更 | 影响 |
| --- | --- |
| `Find*OfType` 系列改名拆分 | 所有按类型查找的代码需改（见 `deprecated-apis.md`） |
| Input System 成为新项目默认路径 | 输入层从一开始就用 Input System + InputAction 资产 |
| URP 成为新项目默认渲染管线 | 本项目用 URP + 2D Renderer；Built-in 不再考虑 |
| Addressables 升到 2.x | 资源加载代码不能照抄 1.x 教程 |
| 包管理：`com.unity.*` 包大量版本跃迁 | 用 Package Manager 锁定版本，不要手改 manifest 猜版本号 |

## 6000.0 → 6000.6（Unity 6.3 LTS）（⚠️ 未验证）

> 官方升级指南存在但正文无法抓取。以下条目**必须在第一周人工核对后填写**，在此之前不要假设任何行为：

- [ ] 是否有 URP / 2D 渲染相关破坏性变更
- [ ] Addressables 是否再次跃迁
- [ ] TextMeshPro / UI Toolkit 文本系统是否合并或改名（对本项目的中文字排版影响最大）
- [ ] 是否有 C# / .NET profile 变化（影响 Newtonsoft/System.Text.Json 的可用性）
- [ ] 是否有编辑器序列化格式变更（影响存档兼容）
- [ ] 是否移除任何 `Obsolete` 已久的 API（本项目要用的老 API 尽早清掉）

## 对本项目的净影响判断

低风险的部分：本项目是 2D / 2.5D 单机、数据驱动、无复杂渲染。

高风险的部分集中在三处，都在第一个月可验证：

1. **中文字体与排版**（TextMeshPro / UI Toolkit 的 CJK 支持）——这是本项目视觉的核心，必须最先验证。
2. **Addressables 2.x 加载字表**。
3. **Steamworks C# 绑定的版本兼容**（上架死线，不能等到第 11 个月）。
