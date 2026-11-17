# 技术验证记录（Tech Validation）

> **状态**: T-002 通过；T-003 通过（PCK 级）；T-004 有条件通过（编辑器内，导出构建待模板）
> **引擎**: Godot 4.7.2 stable（.NET / mono，`4.7.2.stable.mono.official.ed1daf0bf`）
> **平台**: macOS 27.2 arm64（开发机）
> **规则**: 只记录**实际跑过的命令与输出**；没跑过的写"未验证"，不要写"应该可用"。
> **最后更新**: 2026-11-17

## 总览

| 任务 | 验证项 | 结论 | 日期 |
| --- | --- | --- | --- |
| T-001 | Godot 工程 / Sim-View 分层 / `dotnet test` / headless 启动 | **通过**（详见 `production/tasks/T-001.md`） | 2026-11-17 |
| T-002 | CJK 动态字形 / 竖排 / 图集 | **通过（有条件）**：动态字形与原生竖排可用；50 字无缺字，图集 ≤ 4 页 | 2026-11-17 |
| T-003 | JSON → Sim → Godot 导出 | **通过（PCK 级）**：开发运行与导出 PCK 内均读到 50 / 26；完整平台导出待模板 | 2026-11-17 |
| T-004 | Steam 初始化 / 成就上报 | **有条件通过**：编辑器内 flat API 初始化 + 成就 false→true；导出构建待模板与产品 AppID | 2026-11-17 |

---

## T-002 · CJK 字体与竖排

### 结论

**有条件可用。** Godot 4.7.2 .NET 的动态字体（`FontFile`，`fixed_size=0`）能正常栅格化 50 个目标字；
TextServer 原生 `ORIENTATION_VERTICAL` 能直接竖排，标点（，。）会移到右上角；**不需要自定义逐字排布**。
结论的保留项：竖排长文/标点细则（A5）仍需人工排版验收；500 字时的图集外推未实测。

### 字体与授权

- 采用 **Noto Serif SC**（Google 对 Adobe **思源宋体 Source Han Serif SC** 的同一 OFL 字体构建）。
- 源文件：`https://github.com/notofonts/noto-cjk/raw/main/Serif/SubsetOTF/SC/NotoSerifSC-Regular.otf`
  - 源文件 SHA256：`e8f396decc1f0963a016a989c3d8852e863d1350996f573860a80767c83a1cd3`
  - 源文件 11,625,800 bytes；下游只入库**子集**，不入库 11 MB 源文件。
- 子集：`Client/assets/fonts/NotoSerifSC-Regular-subset.otf`（105,080 bytes，191 字）。
  - 字符集 = `char-table.json` 的 50 字 + 数字/拉丁/CJK 标点/菜单与验证图说明文字。
  - 保留全部 OpenType layout features，包括竖排所需的 `vert` / `vrt2`（已用 fontTools 核对）。
  - 生成脚本：`Tools/fonts/subset-font.py`（可复现；见脚本头注释）。
- 授权：`Client/assets/fonts/LICENSE.txt`（SIL OFL 1.1 原文）。
- 导入设置：`allow_system_fallback=false`，避免系统字体掩盖缺字；动态字体，MSDF 关闭。

### 50 字实测数据

```
chars = 秋果杏昌江香村汪苗李柏炎灿旺里胜林杜杆双权功仙囚加星明相休安旱泊沐沁汁汗泪汝男好如仔字思委芯早季对朋
has_char_missing=0
```

| 字号 | 图集页数 | 图集显存（RGBA） |
| --- | --- | --- |
| 16 px | 1 | 256 KiB |
| 32 px | 2 | 512 KiB |
| 120 px | 1 | 4,194,304 B（4 MiB） |
| 64 px（竖排） | 1 | 1 MiB |
| **合计** | **5（按字号缓存合计；单字号最大 2 页）** | **5.75 MiB** |

- 单字号最大 2 页 ≤ A4 的 4 页上限；总显存 5.75 MiB < 64 MiB。
- 50 字在 16 / 32 / 120 px 下全部有字形、无系统 fallback 方块。
- 120 px 动态栅格化直接按目标字号生成，无放大模糊。

### 竖排实测

```
输入：秋，江。明早（5 字，64 px）
Font.GetStringSize(..., ORIENTATION_VERTICAL) = (106, 384)
Font.GetStringSize(..., ORIENTATION_HORIZONTAL) = (384, 93)
vertical_layout=vertical-shaper
```

- 竖排高度 384 / 5 ≈ 76.8 px，字号 64 px → 行距约 1.2 倍，满足 R4 的 ≥ 1.15。
- 标点由 TextServer 自动处理：逗号/句号位于右上；截图见下。
- 结论：**原生竖排可用**；P3 农书可直接用 `TextLine` / `Font.DrawString(..., Orientation.Vertical)`，不需要手写逐字排版。

### 截图证据

- [`docs/screenshots/font-validation/font-probe-16-32-120.png`](screenshots/font-validation/font-probe-16-32-120.png)：16 / 32 / 120 px 全部 50 字 + 竖排 64 px。
- [`docs/screenshots/font-validation/font-probe-16px-3x.png`](screenshots/font-validation/font-probe-16px-3x.png)：16 px 行 ×3 放大，供人工读字。
- [`docs/screenshots/font-validation/font-probe-120px.png`](screenshots/font-validation/font-probe-120px.png)：120 px 大字区域。
- [`docs/screenshots/font-validation/font-probe-vertical-2x.png`](screenshots/font-validation/font-probe-vertical-2x.png)：竖排 5 字 ×2 放大（标点位置可辨）。

### 可执行验证

```text
$ ls -R Client/assets/fonts/ | grep -iE "\.(ttf|otf)$"
NotoSerifSC-Regular-subset.otf

$ grep -ril "SIL OPEN FONT LICENSE" Client/assets/fonts/
Client/assets/fonts/LICENSE.txt

$ godot --headless --path Client -- --font-probe
FONT_PROBE font=res://assets/fonts/NotoSerifSC-Regular-subset.otf chars=50
FONT_PROBE font_name=Noto Serif SC style=Regular fixed_size=0
FONT_PROBE has_char_missing=0
FONT_PROBE headless=1 screenshot=skipped（无头模式不截图；截图请用窗口模式）
FONT_PROBE vertical_text_size=(106, 384) horizontal_text_size=(384, 93)
FONT_PROBE vertical_layout=vertical-shaper
FONT_PROBE atlas size=(16, 0) pages=1 bytes=262144
FONT_PROBE atlas size=(32, 0) pages=2 bytes=524288
FONT_PROBE atlas size=(120, 0) pages=1 bytes=4194304
FONT_PROBE atlas size=(64, 0) pages=1 bytes=1048576
FONT_PROBE atlas_total pages=5 bytes=6029312 mib=5.75
exit=0
```

窗口模式截图命令（本机 GUI 会话）：

```bash
~/.local/godot/4.7.2/Godot_mono.app/Contents/MacOS/Godot \
  --path Client --audio-driver Dummy -- --font-probe
```

### 未验证 / 限制

- **标点竖排细则**（A5：句号右上、数字旋转、英文整词旋转）只验证了逗号/句号的位置，未做完整规则集；需要 P3 农书时人工验收。
- **500 字**时的图集页数未实测；50 字实测 5.75 MiB，按同比例外推仍远低于 64 MiB，但这不是实测结论。
- **A2（构建产物无系统字体）**需要在完整导出产物上检查；本轮只做到 PCK 级（见 T-003），完整导出待模板。

---

## T-003 · 数据管线

### 结论

**PCK 级通过。** `CharTableLoader`（Sim，纯 .NET）能解析真实 JSON；View 的 `DataProbe` 用
`FileAccess` 从 `res://Data` 读取并在开发运行与**导出的 PCK** 中输出 `chars=50, components=26`。
完整 macOS/Windows 可执行产物因导出模板下载受限未生成，见"未验证 / 限制"。

### 实现

- `Sim/Data/CharTableLoader.cs`：`LoadChars` / `LoadComponents` / `Load`，只解析 T-003 所需字段；
  缺字段时抛带 JSON 路径的 `InvalidDataException`。完整 schema 校验仍归 T-007。
- `Client/src/View/DataProbe.cs`：`FileAccess.GetFileAsString` + `CharTableLoader`；
  `--data-probe` 打印条目数并以退出码 0/1 表示结果。
- `Tests/DataPipelineTests.cs`：`Category=DataPipeline`，覆盖真实数据、缺字段、非法 JSON。
- `Client/export_presets.cfg`：macOS / Windows 预设；`include_filter="Data/*.json"` 保证非资源 JSON 进包。
- `Client/TianziGe.View.sln`：Godot 的 C# 导出插件需要工程根有 solution 才能 publish 程序集
  （T-001 只建了 csproj，本轮补上）。

### 可执行验证

```text
$ dotnet test --filter "Category=DataPipeline"
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4

$ godot --headless --path Client -- --data-probe
TianziGe data probe OK: chars=50, components=26
exit=0

$ godot --headless --path Client --export-pack "macOS" build/TianziGe-macos.pck
[ DONE ] dotnet_publish_project        （osx-x64 + osx-arm64）
[ 15% ] savepack | 正在保存文件：res://Data/char-table.json
[ 29% ] savepack | 正在保存文件：res://Data/component-registry.json
[ DONE ] savepack
导出产物：Client/build/TianziGe-macos.pck（108 KiB）

$ godot --headless --path Client --main-pack build/TianziGe-macos.pck -- --data-probe
TianziGe data probe OK: chars=50, components=26
```

> 说明：`--export-pack` 只导出资源包，不打包 .NET 程序集；上面的 PCK 运行用编辑器二进制
> 并同时指定 `--path Client`，让程序集从本机工程加载、资源从 PCK 读取。这验证了
> **JSON 确实在导出包内且运行时可达**，但不是独立可执行产物。

### 导出配置与进包规则（供后续资源复用）

- **资源过滤**：`export_filter="all_resources"`；非资源文件（JSON）用 `include_filter="Data/*.json"` 显式包含。
- **字体**：作为 Godot 资源自动进包；`.import` / `.godot` 生成物不进包。
- **原生库**：原生 Steam 库不能从 PCK 里 `dlopen`，必须放在可执行文件同级（见 T-004）。
- **产物**：`Client/build/` 已加入 `.gitignore`，不入库；只记录配置与校验结果。
- **.NET**：`Client/TianziGe.View.sln` 入库，Godot 导出插件据此 publish 程序集。

### 未验证 / 限制

- **完整 macOS / Windows 可执行导出未完成**：本机没有 4.7.2 mono 导出模板；
  `Godot_v4.7.2-stable_mono_export_templates.tpz`（1.2 GB）从官方对象存储下载，当前环境
  连接被限速/反复停滞，已下载约 956 MB 但剩余部分无法在合理时间内完成。没有伪造导出结果。
- 因此"导出产物体积增量"与"独立 .app / .exe 内读 JSON"两项未测；PCK 内的 50 / 26 已实测。
- Windows x64 产物未做；本轮平台为 macOS。

---

## T-004 · Steam 集成

### 结论

**有条件通过（编辑器内），导出构建受阻。** 两条任务书路线都不适合当前工程：

1. **GodotSteam 4.24**：官方 release 只有"内置 GodotSteam 的整包编辑器 / 导出模板"
   （`macos-g472-...-editor.tar.xz` 等），没有可给现有 .NET 工程使用的 GDExtension 插件；
   这些整包编辑器也未标注 mono/.NET 支持。放弃。
2. **Steamworks.NET 2025.165.0 / 2024.8.0**：托管程序集在本机 Godot 4.7.2 .NET 8 下加载失败，
   `COR_E_FILELOAD 0x80131621`；在纯 .NET 8 控制台、官方 NuGet、Standalone、以及用 Unity
   包源码自行重编译的版本上都复现。放弃托管封装。

**最终选型：直接 P/Invoke Valve `steam_api` 的 flat C API**（与 Steamworks.NET 使用同一原生库）。
已在本机 Steam 客户端登录状态下、用公共测试 AppID 完成：初始化 → 枚举成就 → 选未点亮成就 →
`SetAchievement` → `StoreStats` → `GetAchievement` 由 `false` 变 `true`。

- 测试 AppID：**480（Spacewar，公共测试应用）**；仅用于技术链路验证，**不是产品 AppID**。
  产品 AppID 未提供，不能据此声称产品级通过。
- 测试账号：本机已登录的 Steam 账号；账号名/SteamID 不写入仓库（日志仅在内部记录）。
- 原生库：Steamworks SDK 1.65 的 `libsteam_api.dylib`（universal），放 `Client/native/macos/`。
- 代码：`Client/src/View/SteamTest/SteamProbe.cs`（`--steam-probe`），无第三方托管依赖。

### 编辑器内实测输出

```text
STEAM_PROBE steam_running=True
[S_API] SteamAPI_Init(): Loaded '.../steamclient.dylib' OK.
Setting breakpad minidump AppID = 480
STEAM_PROBE app_id=480 steam_id=<本机账号> user_stats_ptr=...
STEAM_PROBE achievements=5
STEAM_PROBE achievement[0]=ACH_TRAVEL_FAR_ACCUM
STEAM_PROBE achievement[1]=ACH_TRAVEL_FAR_SINGLE
STEAM_PROBE achievement[2]=ACH_WIN_100_GAMES
STEAM_PROBE achievement[3]=ACH_WIN_ONE_GAME
STEAM_PROBE achievement[4]=NEW_ACHIEVEMENT_0_4
STEAM_PROBE achievement=ACH_TRAVEL_FAR_SINGLE get_before_ok=True set_ok=True store_ok=True get_after_ok=True achieved_before=False achieved_after=True
```

> 运行方式（AppID 只从环境注入，不写仓库）：
> `SteamAppId=<test-appid> Godot --headless --path Client -- --steam-probe`
> 编辑器运行时通过 `NativeLibrary.SetDllImportResolver` 指向 `Client/native/macos/libsteam_api.dylib`。

### 原生库放置与导出规则

| 场景 | 放置 | 规则 |
| --- | --- | --- |
| 编辑器 / 开发运行 | `Client/native/macos/libsteam_api.dylib` | 代码里的 resolver 指向该路径；`Client/native/.gdignore` 阻止 Godot 导入 |
| macOS 导出 | `TianziGe.app/Contents/MacOS/libsteam_api.dylib` | 与可执行文件同级；PCK 内放原生库无法 `dlopen` |
| Windows 导出 | `steam_api64.dll` 与 `TianziGe.exe` 同级 | 本次未实测；SDK 1.65 文件在 Steamworks.NET Standalone 包内 |
| AppID | 运行环境 `SteamAppId` / 本机 AppID 文件 | **不入库、不随包发布**；正式包由 Steam 客户端启动时注入 |

### 导出构建未完成

- 需要 4.7.2 mono 导出模板；与环境里 T-003 相同的模板下载限制，未生成独立 macOS `.app`。
- 因此"导出构建中成就上报"未验证。已如实记录为**阻塞**，没有用编辑器结果替代。

### 遗留 / 需要人

- **产品 Steam AppID**：需要人类提供（或确认先用 480 继续做技术验证）；AppID 不得入库。
- **导出模板**：需要可用的 4.7.2 mono 模板（或允许跳过导出，等网络恢复补做）。
- flat API 的访问器带 SDK 版本后缀（`SteamUserStats_v013` / `SteamUtils_v011` / `SteamUser_v023`）；
  升级原生库时必须用 `nm -gU` 核对导出符号并同步声明。
- 未做 Steam 云存档、成就表、排行榜。

---

## 仓库凭据静态检查

```bash
grep -rniE "steam_appid|480|appid.*[0-9]{4,}" \
  --exclude-dir=.git --exclude-dir=.godot .
```

原始输出只命中三类非凭据文本：T-004 验收命令自身、`docs/tech-validation.md` 的说明、
上游 `README-Steamworks.NET.md` 与原生库二进制里的字符串。**没有任何产品 AppID / Web API Key /
账号口令入库**；测试用 AppID 只通过运行环境变量注入。
