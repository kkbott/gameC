# ADR-003 · 渲染与输入技术栈：URP 2D + Input System

| 字段 | 值 |
| --- | --- |
| 状态 | **已采纳（含未验证项）** |
| 日期 | 2026-11-17 |
| 决策者 | 创意总监 + agent |
| 影响系统 | P1 拖拽、P4 字体、P5 水墨 VFX、P2 HUD |

## 上下文

- 美术方向是**江南水墨、2D/2.5D**（已明确否决 3D 江南，见概念 §9 技术否决）。
- 视觉主体是**中文排版**：合字大字、农书竖排、HUD 文案。
- 需要**手柄支持**（Steam 上架必需）。
- 引擎已定 Unity 6000.6（Unity 6.3 LTS），该版本**超出 AI 训练数据**，多项行为未验证。

## 决策

| 层 | 选择 | 理由 |
| --- | --- | --- |
| 渲染管线 | **URP + 2D Renderer** | 水墨需要 shader 后处理（墨晕、纸纹、飞白），Built-in 已不是新项目路径 |
| 水墨效果 | **Shader Graph** | 无需手写 HLSL 即可迭代；顶点变形做字形动画，不做逐帧 |
| 文字 | **TextMeshPro + 动态 SDF 图集**（备选 UI Toolkit） | CJK 必须按需生成字形，禁止整包嵌入 |
| 输入 | **Input System（InputAction 资产）** | 需键鼠 + 手柄热插拔；旧 Input Manager 仅为兼容 |
| 资源加载 | **Addressables** | 字表、字形、场景分包（见 ADR-002） |
| 构建目标 | Windows x64 优先 | Steam 首发；macOS/Linux 视时间 |

**明确否决**：实时 3D 水墨、逐帧字形动画、位图字、旧 Input Manager。

## ⚠️ 未验证项（必须在第 1 周人工确认）

本决策依赖 Unity 6000.6 的三个行为，**全部超出 AI 训练数据**：

1. **TMP 的 CJK 动态 SDF 图集**在 6000.6 下是否正常（无缺字、无模糊）。→ 若不可用，UI 层改用 UI Toolkit 文本组件，**整个 P4 方案要重做**。
2. **Addressables 2.x** 的 API 与 1.x 差异（`AsyncOperationHandle` 系列）。
3. **Steamworks C# 绑定**与本项目 .NET profile 的兼容性（上架死线，必须在第一个月跑通成就 + 云存档）。

第 1 条是本项目的**最高技术风险**——字体方案错了，后面所有文字相关的工作都要返工。

## 后果

**正面**

- 水墨效果通过 Shader Graph 迭代，一个人可控。
- 手柄与键鼠共用 InputAction 抽象，P1 的双输入模型有基础。
- 中文动态图集让 300–500 字的包体可控（vs 整包嵌入 >20MB）。

**代价**

- URP 2D + 自定义 shader 有学习曲线。
- TMP 竖排需自写 layout（无原生支持）。
- 三个未验证项意味着**第 1 周可能推翻部分方案**，需要预留返工时间。

## 替代方案与否决理由

| 方案 | 否决理由 |
| --- | --- |
| Built-in 渲染管线 | 已非新项目路径，后处理生态弱 |
| 3D 水墨（真实水体/建筑） | 一个人做不完（概念 §9 技术否决） |
| 旧 Input Manager | 无法干净支持热插拔与多设备 |
| 位图字 / 逐帧字形动画 | 300–500 字的资产量不可控 |

## 验证方式

- P4 验收 A3：6000.6 下 500 字全渲染无缺字（第 1 周技术验证）。
- P1 验收 A6/A7：手柄 ≤3 步布字、热插拔切换提示。
- 人工核对清单见 `docs/engine-reference/unity/VERSION.md`。

## 相关文档

- `design/gdd/drag-input.md`（P1）
- `design/gdd/cjk-typography.md`（P4）
- `design/gdd/hud.md`（P2）
- `docs/engine-reference/unity/VERSION.md`
- `.claude/docs/technical-preferences.md`
