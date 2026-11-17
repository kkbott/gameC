# 技术验证记录（Tech Validation）

> **状态**: T-002 通过；T-003 通过（macOS + Windows 导出）；T-004 通过（技术链路；导出构建 + 公共测试 AppID；产品 AppID 待补）
> **引擎**: Godot 4.7.2 stable（.NET / mono，`4.7.2.stable.mono.official.ed1daf0bf`）
> **平台**: macOS 27.2 arm64（开发机）
> **规则**: 只记录**实际跑过的命令与输出**；没跑过的写"未验证"，不要写"应该可用"。
> **最后更新**: 2026-11-18

## 总览

| 任务 | 验证项 | 结论 | 日期 |
| --- | --- | --- | --- |
| T-001 | Godot 工程 / Sim-View 分层 / `dotnet test` / headless 启动 | **通过**（详见 `production/tasks/T-001.md`） | 2026-11-17 |
| T-002 | CJK 动态字形 / 竖排 / 图集 | **通过（有条件）**：动态字形与原生竖排可用；50 字无缺字，图集 ≤ 4 页 | 2026-11-17 |
| T-003 | JSON → Sim → Godot 导出 | **通过**：开发运行、macOS `.app`、Windows PCK 内均读到 50 / 26 | 2026-11-18 |
| T-004 | Steam 初始化 / 成就上报 | **通过（技术链路，公共测试 AppID）**：编辑器与 macOS 导出构建均完成成就 false→true；产品 AppID 待补 | 2026-11-18 |

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
- **A2（构建产物无系统字体）**：完整 macOS / Windows 导出已生成；`find Client/build -iname "*.ttf" -o -iname "*.otf"` 无输出，字体只以子集资源在 PCK 内。

---

## T-003 · 数据管线

### 结论

**通过。** `CharTableLoader`（Sim，纯 .NET）能解析真实 JSON；View 的 `DataProbe` 用
`FileAccess` 从 `res://Data` 读取。开发运行、导出的 macOS `.app`、以及 Windows x64 导出包内
运行时均输出 `chars=50, components=26`。Windows `.exe` 未在 macOS 上执行，Windows 侧验证到
导出包内运行时读取（用编辑器二进制 + `--main-pack`）。

### 实现

- `Sim/Data/CharTableLoader.cs`：`LoadChars` / `LoadComponents` / `Load`，只解析 T-003 所需字段；
  缺字段时抛带 JSON 路径的 `InvalidDataException`。完整 schema 校验仍归 T-007。
- `Client/src/View/DataProbe.cs`：`FileAccess.GetFileAsString` + `CharTableLoader`；
  `--data-probe` 打印条目数并以退出码 0/1 表示结果。
- `Tests/DataPipelineTests.cs`：`Category=DataPipeline`，覆盖真实数据、缺字段、非法 JSON。
- `Client/export_presets.cfg`：macOS / Windows 预设；`include_filter="Data/*.json"` 保证非资源
  JSON 进包，`exclude_filter="build/*"` 防止导出产物被递归打进包。
- `Client/TianziGe.View.sln`：Godot 的 C# 导出插件需要工程根有 solution 才能 publish 程序集
  （T-001 只建了 csproj，本轮补上）。

### 可执行验证

```text
$ dotnet test --filter "Category=DataPipeline"
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4

$ godot --headless --path Client -- --data-probe
TianziGe data probe OK: chars=50, components=26
exit=0

$ godot --headless --path Client --export-release "macOS" \
    "$PWD/Client/build/macos/TianziGe.app"
[ DONE ] dotnet_publish_project        （osx-x64 + osx-arm64）
[ 13% ] savepack | 正在保存文件：res://Data/char-table.json
[ 26% ] savepack | 正在保存文件：res://Data/component-registry.json
[ DONE ] savepack
[ DONE ] export

$ Client/build/macos/TianziGe.app/Contents/MacOS/TianziGe --headless -- --data-probe
TianziGe data probe OK: chars=50, components=26
exit=0

$ godot --headless --path Client --export-release "Windows Desktop" \
    "$PWD/Client/build/windows/TianziGe.exe"
[ DONE ] dotnet_publish_project        （win-x64）
[ 74% ] savepack | 正在保存文件：res://Data/char-table.json
[ 86% ] savepack | 正在保存文件：res://Data/component-registry.json
[ DONE ] savepack
[ DONE ] export

$ godot --headless --path Client --main-pack "$PWD/Client/build/windows/TianziGe.pck" -- --data-probe
TianziGe data probe OK: chars=50, components=26
```

### 导出结果

| 项 | 实测值 |
| --- | --- |
| 开发运行读取 | `chars=50, components=26` |
| macOS `.app` | 320 MiB；`Contents/Resources/TianziGe.pck` 108 KiB；导出构建运行读到 50 / 26 |
| Windows x64 | `TianziGe.exe` 104 MiB + `TianziGe.pck` 108 KiB + `data_TianziGe.View_windows_x86_64/`；PCK 内读到 50 / 26 |
| 导出体积增量 | PCK 108 KiB（字体 105 KiB + JSON 约 3 KiB） |

### 导出配置与进包规则（供后续资源复用）

- **资源过滤**：`export_filter="all_resources"`；非资源文件（JSON）用 `include_filter="Data/*.json"` 显式包含；
  `exclude_filter="build/*"` 排除导出目录本身（否则产物会被递归打进 PCK）。
- **字体**：作为 Godot 资源自动进包；`.import` / `.godot` 生成物不进包。
- **原生库**：原生 Steam 库不能从 PCK 里 `dlopen`，必须放在可执行文件同级（见 T-004）。
- **产物**：`Client/build/` 已加入 `.gitignore`，不入库；只记录配置与校验结果。
- **.NET**：`Client/TianziGe.View.sln` 入库，Godot 导出插件据此 publish 程序集。

### macOS 导出的签名限制（重要）

Godot 4.7.2 导出的 ad-hoc 签名在本机 macOS 27.2 上会被 AMFI 拒绝
（`failed parsing DER entitlements`，进程 SIGKILL）。实测的本地验证签名步骤：

```bash
codesign -d --entitlements :- TianziGe.app 2>/dev/null > /tmp/entitlements.plist
codesign --force --deep --sign - --options runtime \
  --entitlements /tmp/entitlements.plist TianziGe.app
```

这只适用于本机验证；正式发布必须用 Developer ID 签名 + notarization，并把
`libsteam_api.dylib` 纳入签名/分发流程。

### 未验证 / 限制

- **Windows `.exe` 未在 Windows 上运行**（开发机为 macOS）；Windows 侧只验证了导出包内 JSON 可达。
- macOS 导出的 ad-hoc 签名需按上面的步骤重签才能在本机启动；正式签名待上架阶段。
- 完整产物体积（320 MiB / 181 MiB）包含自包含 .NET 运行时，属正常但尚未做裁剪。

---

## T-004 · Steam 集成

### 结论

**通过（技术链路；公共测试 AppID）。** 编辑器内与 **macOS 导出构建** 都完成了初始化 + 成就
`false → true`；产品 AppID 尚未提供，因此产品级验证仍待补。两条任务书路线都不适合当前工程：

1. **GodotSteam 4.24**：官方 release 只有"内置 GodotSteam 的整包编辑器 / 导出模板"
   （`macos-g472-...-editor.tar.xz` 等），没有可给现有 .NET 工程使用的 GDExtension 插件；
   这些整包编辑器也未标注 mono/.NET 支持。放弃。
2. **Steamworks.NET 2025.165.0 / 2024.8.0**：托管程序集在本机 Godot 4.7.2 .NET 8 下加载失败，
   `COR_E_FILELOAD 0x80131621`；在纯 .NET 8 控制台、官方 NuGet、Standalone、以及用 Unity
   包源码自行重编译的版本上都复现。放弃托管封装。

**最终选型：直接 P/Invoke Valve `steam_api` 的 flat C API**（与 Steamworks.NET 使用同一原生库）。
已在本机 Steam 客户端登录状态下、用公共测试 AppID 完成：编辑器内与 **导出 macOS 构建内**
各跑通一次 初始化 → 枚举成就 → 选未点亮成就 → `SetAchievement` → `StoreStats` →
`GetAchievement` 由 `false` 变 `true`。

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

### 导出构建实测输出

```text
$ SteamAppId=480 Client/build/macos/TianziGe.app/Contents/MacOS/TianziGe \
    --headless -- --steam-probe
STEAM_PROBE steam_running=True
STEAM_PROBE app_id=480 steam_id=<本机账号> user_stats_ptr=...
STEAM_PROBE achievements=5
STEAM_PROBE achievement[0]=ACH_TRAVEL_FAR_ACCUM
STEAM_PROBE achievement[1]=ACH_TRAVEL_FAR_SINGLE
STEAM_PROBE achievement[2]=ACH_WIN_100_GAMES
STEAM_PROBE achievement[3]=ACH_WIN_ONE_GAME
STEAM_PROBE achievement[4]=NEW_ACHIEVEMENT_0_4
STEAM_PROBE achievement=NEW_ACHIEVEMENT_0_4 get_before_ok=True set_ok=True \
  store_ok=True get_after_ok=True achieved_before=False achieved_after=True
```

> 导出时把 `libsteam_api.dylib` 复制到 `TianziGe.app/Contents/MacOS/` 并重签（见 T-003 的
> macOS 签名步骤）；AppID 只由运行环境注入。

### 原生库放置与导出规则

| 场景 | 放置 | 规则 |
| --- | --- | --- |
| 编辑器 / 开发运行 | `Client/native/macos/libsteam_api.dylib` | 代码里的 resolver 指向该路径；`Client/native/.gdignore` 阻止 Godot 导入 |
| macOS 导出 | `TianziGe.app/Contents/MacOS/libsteam_api.dylib` | 与可执行文件同级；PCK 内放原生库无法 `dlopen` |
| Windows 导出 | `steam_api64.dll` 与 `TianziGe.exe` 同级 | 本次未实测；SDK 1.65 文件在 Steamworks.NET Standalone 包内 |
| AppID | 运行环境 `SteamAppId` / 本机 AppID 文件 | **不入库、不随包发布**；正式包由 Steam 客户端启动时注入 |

### 遗留 / 需要人

- **产品 Steam AppID**：需要人类在上架前提供；本记录只证明公共测试 AppID 下的技术链路。
  AppID 不得入库、不随包发布。
- **Windows 导出构建的 Steam 链未实测**：本次只验证 macOS `.app`；Windows 侧需在 Windows 机上
  把 `steam_api64.dll` 放到 `TianziGe.exe` 同级后重跑 `--steam-probe`。
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
原生库二进制里的字符串。**没有任何产品 AppID / Web API Key /
账号口令入库**；测试用 AppID 只通过运行环境变量注入。
