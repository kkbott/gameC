# Godot — 版本参考（VERSION）

| 字段 | 值 |
| --- | --- |
| **引擎版本** | **Godot 4.7.2 stable（.NET / mono 版）** |
| **项目固定** | 2026-11-17；固定 `4.7.2` 补丁号，不跟随 `4.7.x` 自动升级 |
| **LLM 知识截止** | 2025-05（Godot 4.7 明显超出训练数据） |
| **风险级别** | **高 — 版本超出训练数据；本文件只记录实测或官方文档可核对的内容** |
| **最后验证** | 2026-11-17（本机 macOS 27.2 arm64；Godot 4.7.2 .NET；.NET SDK 8.0.428） |

## 为什么固定 4.7.2

- `4.7.2-stable` 是 2026-08-18 发布的维护版本（GitHub Release `4.7.2-stable`），是当前最新的 4.7 补丁。
- 该项目是 1 人、9–12 个月的 PC 单机项目：**补丁号固定**比"始终最新"更重要；升级只在里程碑边界（原型通过 / 纵向切片结束）评估。
- 不使用 Godot 4.6 / 4.5：没有必须回退的兼容性理由，且 4.7 的 2D / 字体修复对本项目有利。此判断只是工程选择，不是"4.7 一定更稳"的断言。

## 下载与校验（已实测）

- 官方下载入口：<https://godotengine.org/download/archive/4.7.2-stable/>（选择 **.NET** 版）
- 本项目实际下载：`Godot_v4.7.2-stable_mono_macos.universal.zip`（200,756,622 bytes）
- SHA512（来自官方 `SHA512-SUMS.txt`）：

```text
0862c53d7158c7a67f745e2e46f90b68cf5343cbe8b95d6d4333c469e42ca104af9c121d1746a50e5d221a99d09d82ef7016495f8e0d09255842884ed0502795  Godot_v4.7.2-stable_mono_macos.universal.zip
```

> 校验命令：`shasum -a 512 Godot_v4.7.2-stable_mono_macos.universal.zip`。**下载后必须核对**；不核对就等于信任一个未知二进制。本次已核对通过（2026-11-17）。
>
> Godot 引擎本身为 MIT 许可；发布构建需随包附带其 LICENSE 文本（上架阶段处理）。

## 工具链要求

| 工具 | 要求 | 本机实测 |
| --- | --- | --- |
| Godot | 4.7.2 **.NET / mono** 版（标准版不能编译 C#） | 4.7.2.stable.mono（见下方输出） |
| .NET SDK | 8.0（Godot 4.7 .NET 目标框架 `net8.0`） | 8.0.428 |
| 平台 | Windows x64（发布）/ macOS（开发） | macOS 27.2 arm64 |

**注意**：标准版 Godot（非 mono）没有 C# 支持。下载页面必须选 **.NET**。

## 本机验证记录

```text
$ Godot_mono.app/Contents/MacOS/Godot --version
4.7.2.stable.mono.official.ed1daf0bf

$ dotnet --version
8.0.428

$ dotnet test
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12

$ dotnet run --project Balance -- --runs 20
Balance skeleton OK: runs=20, seed_base=0, final_hash=0x1A271B7B4135C131

$ godot --headless --path Client --build-solutions --quit
Godot Engine v4.7.2.stable.mono.official.ed1daf0bf - https://godotengine.org
[ DONE ] first_scan_filesystem
[ DONE ] dotnet_build_project

$ godot --headless --path Client -- --boot-probe
Godot Engine v4.7.2.stable.mono.official.ed1daf0bf - https://godotengine.org
TianziGe boot OK: sim=TianziGe.Sim money=3
```

> 以上是 T-001 的实际输出；若本文件与当前代码不一致，以重新运行的结果为准并更新本节。

## 工程结构（与引擎绑定）

```
Client/                         Godot 工程根（View）
  project.godot
  TianziGe.View.csproj          Sdk="Godot.NET.Sdk/4.7.2"，引用 ../Sim
  Data/                         字表 / 部件表 JSON（唯一源，res://Data）
  scenes/Main.tscn
  src/View/Bootstrap.cs
Sim/                            纯 .NET 类库（规则）
Tests/                          NUnit
Balance/                        M4 控制台
TianziGe.Tools.sln              只包含 Sim/Tests/Balance，不包含 Godot 工程
```

Godot 的 C# 工程由 `Godot.NET.Sdk` 构建；`Client/` 内的生成物 `.godot/`、`bin/`、`obj/` 不入库。

## 已验证 / 未验证

### 已验证（2026-11-17）

- [x] 4.7.2 .NET 编辑器可启动并输出版本号（`4.7.2.stable.mono.official.ed1daf0bf`）
- [x] `Godot.NET.Sdk/4.7.2` 可构建 `Client/TianziGe.View.csproj`（`target net8.0`）
- [x] View 程序集能引用 `TianziGe.Sim`，场景脚本能读取 Sim 类型
- [x] `godot --headless --path Client -- --boot-probe` 能启动项目并执行 C# 脚本
- [x] Sim 的 `dotnet test` 完全不依赖 Godot
- [x] `Client/` 工程可由 `dotnet build` 与 Godot `--build-solutions` 构建（两者均实测通过）
- [x] CJK 动态字形：50 字 @16/32/120px 无缺字；图集合计 5.75 MiB（T-002，2026-11-17）
- [x] 竖排：`TextServer.Orientation.Vertical` 原生可用，标点右上（T-002）
- [x] 数据进包：macOS `.app` 与 Windows PCK 内均读到 `res://Data/*.json`，50 / 26（T-003，2026-11-18）
- [x] Steam flat API：编辑器与 macOS 导出构建内初始化 + 成就 false→true（T-004，公共测试 AppID）

### 未验证（按任务验证，不要凭记忆断言）

| 项 | 为什么重要 | 验证任务 |
| --- | --- | --- |
| Windows 导出构建实机运行 | 发布前提 | T-003 后续（当前只在 macOS 验证 PCK） |
| 产品 Steam AppID / Windows Steam 链 | 上架必需 | T-004 后续 |
| macOS 正式签名 / notarization | 上架必需 | 上架阶段 |
| 2D 水墨 shader 在目标 GPU 上的表现 | 视觉方向 | P5 / 纵向切片 |
| Windows x64 导出模板 | 发布 | 原型后 |

## 对 agent 的强制规则

1. 写任何 Godot API 前，先查官方文档（<https://docs.godotengine.org/en/4.7/>）或在本机用 `--headless` 实测。
2. 涉及 4.7 新增 / 变更的 API，**必须**在 `docs/engine-reference/godot/` 留一条验证记录（命令 + 输出 + 日期）。
3. 不确定的 API，明确说"不确定，需验证"，不要用自信语气包装猜测。
4. 不把 Unity 的 API / 模式照搬进 Godot（`MonoBehaviour`、`Update`、`Time.deltaTime`、`UnityEngine.Random`、`ScriptableObject`、`Addressables`、URP）。
5. `TianziGe.Sim` 内不允许出现 `using Godot`；这条由 `Tests/ArchitectureTests.cs` 持续检查。

## 参考链接

- Godot 4.7 文档：<https://docs.godotengine.org/en/4.7/>
- C# / .NET 支持：<https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/index.html>
- 命令行参数：<https://docs.godotengine.org/en/4.7/tutorials/editor/command_line_tutorial.html>
- 导出：<https://docs.godotengine.org/en/4.7/tutorials/export/index.html>
