# Unity — 版本参考（VERSION）

| 字段 | 值 |
| --- | --- |
| **引擎版本** | Unity 6.3 LTS（编辑器版本线 **6000.6**，安装时取最新 6000.6.x 补丁） |
| **项目固定** | 2026-11 |
| **LLM 知识截止** | 2025-05（本模型对 Unity 的实际可靠覆盖约到 2023.3 / 早期 6000.0） |
| **风险级别** | **高 — 版本明显超出训练数据** |
| **最后验证** | 2026-11（会话内网络检索；官方文档正文无法抓取，仅验证了版本与文档路径存在） |

## 为什么是高风险的

版本号本身已确认存在（[Unity 6.3 LTS 发布公告](https://unity.com/cn/blog/unity-6-3-lts-is-now-available)，[官方 6000.6 文档](https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity63)），但 **6.1 / 6.2 / 6.3 三个版本的具体 API 变更、URP 渲染特性、Addressables 版本、UI Toolkit 运行时成熟度，全部在我的训练数据之外**。

因此本目录的参考文档按以下纪律编写：

- ✅ **已确认条目**：标注置信度，可直接使用。
- ⚠️ **待核对条目**：明确标注，使用前必须查 6000.6 官方文档或 WebSearch 验证。
- ❌ **不写猜测**：宁可留空并标注"未验证"，也不编造 API 行为。

## 对代理（Agent）的强制规则

1. 建议任何 Unity 代码前，先读本文件与 `deprecated-apis.md`。
2. 涉及 6.1+ 新 API 或行为时，**必须** WebSearch 验证，不可凭记忆断言。
3. 不确定的 API，明确说"不确定，需验证"，不要用自信语气包装猜测。
4. 引用官方文档时给链接，让人类可以复核。

## 人工核对清单（值得在第一周做完）

- [ ] [Upgrade to Unity 6.3 LTS](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity63) 通读一遍，把影响本项目的条目补进 `breaking-changes.md`
- [ ] 确认 URP 包版本与 2D Renderer 在 6000.6 下的可用状态
- [ ] 确认 Addressables 当前主版本（训练数据里是 1.x，Unity 6 已是 2.x）
- [ ] 确认 TextMeshPro / UI Toolkit 在 6000.6 下对 CJK 动态字体图集的支持状态（本项目重度依赖中文字排版）
- [ ] 确认 Steamworks.NET 或 Steamworks 官方 C# 绑定对 6000.6 / .NET 版本的兼容性（上架必需）
