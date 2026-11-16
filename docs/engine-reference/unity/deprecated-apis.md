# Unity 弃用 API 对照表

> 使用方式：写代码前查这张表。**置信度「中」和「待核对」的条目，必须先验证再用。**
> 最后验证：2026-11

## 已确认（置信度：高）

| 不要使用 | 改用 | 说明 |
| --- | --- | --- |
| `FindObjectOfType<T>()` | `FindAnyObjectByType<T>()` 或 `FindFirstObjectByType<T>()` | 2023.1 起弃用；语义区分"任意一个"与"第一个" |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` | 同上批次 |
| `WWW` | `UnityWebRequest` | 极早期弃用 |
| `Application.LoadLevel` / `LoadLevelAsync` | `SceneManager.LoadScene` / `LoadSceneAsync` | 需引用 `UnityEngine.SceneManagement` |
| 运行时 `OnGUI()` / IMGUI 面板 | UGUI 或 UI Toolkit | 运行时用 IMGUI 性能与可访问性都差 |
| `Input.GetAxis` / `Input.GetKey`（旧 Input Manager） | Input System 的 `InputAction` | 新项目应直接上 Input System；旧 Manager 仅为兼容 |

## 中置信度（本项目相关，使用前请验证）

| 不要使用 | 改用 | 备注 |
| --- | --- | --- |
| `Resources.Load` 大量资源 | Addressables | 本项目的**字表/字谱数据**建议走 ScriptableObject + Addressables，而不是 Resources 目录 |
| `JsonUtility` 做复杂存档 | System.Text.Json 或自定义序列化 | `JsonUtility` 不支持字典、多态、顶层数组；本项目 §5.4 要求"存档 = 状态快照"，字典与多态几乎必然出现 |
| 单例 `Instance` 承载游戏状态 | 显式传入的纯 C# 状态对象 | 直接违反 game-concept.md §5.4 第 4 条 |

## 待核对（不在训练数据内，必须查官方文档）

| 项目 | 待核对内容 |
| --- | --- |
| URP 2D 相关类 | 6000.6 下 `Renderer2D`、2D Shader Graph 子图 API 是否有更名 |
| Addressables | 2.x 的 API 与 1.x 差异（`AsyncOperationHandle` 系列） |
| TextMeshPro | CJK 动态字体图集：6000.6 是否已并入 UI Toolkit 的 Text 组件 |
| Unity Multiplayer / Netcode | 若将来加联机（概念文档 §5.4 留口），Unity 6 的 Multiplayer Services 与 Netcode for GameObjects 版本 |
| Steamworks C# 绑定 | 与 6000.6 / 本项目 .NET profile 的兼容性 |
