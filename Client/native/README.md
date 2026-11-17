# Client/native · Steam 原生库

T-004 的 Steam 集成**不走托管封装**，直接 P/Invoke Valve `steam_api` 的 flat C API。
原因见 `docs/tech-validation.md` 的 T-004 段：Steamworks.NET 的托管程序集在
Godot 4.7.2 / .NET 8 下加载失败（`COR_E_FILELOAD 0x80131621`，用官方 NuGet、
Standalone 与自行重编译三个版本均可复现），而同一原生库的 flat API 可正常初始化并上报成就。

| 文件 | 来源 | 说明 |
| --- | --- | --- |
| `macos/libsteam_api.dylib` | `Steamworks.NET-Standalone_2025.165.0.zip` → `OSX-Linux-x64/steam_api.bundle/Contents/MacOS/` | Steamworks SDK 1.65 原生库，macOS universal（x86_64 + arm64） |
| `LICENSE-Steamworks.NET.txt` | 同上 | Steamworks.NET 封装的 MIT 许可（保留来源说明） |
| `README.md` | 本仓库 | 放置规则与选型结论 |

Windows 导出时需要把 Steamworks SDK 1.65 的 `steam_api64.dll` 放到导出目录；
本次只在 macOS 实测。

## 放置规则

- **编辑器 / 开发运行**：代码里的 `DllImportResolver` 指向 `Client/native/macos/libsteam_api.dylib`；把 `SteamAppId` 设为测试 AppID（不写进仓库）。
- **macOS 导出**：把 `libsteam_api.dylib` 复制进 `TianziGe.app/Contents/MacOS/`，与可执行文件同级。
- **Windows 导出**：`steam_api64.dll` 与 `TianziGe.exe` 同级。
- 本机 AppID 文件**不入库、不随包发布**；正式包由 Steam 客户端启动时注入 AppID。

## 符号版本

flat API 的访问器带 SDK 版本后缀（当前 `SteamUserStats_v013` / `SteamUtils_v011` / `SteamUser_v023`），
升级原生库时用 `nm -gU Client/native/macos/libsteam_api.dylib` 核对导出符号，并同步
`Client/src/View/SteamTest/SteamProbe.cs` 的 P/Invoke 声明。

原生库版权归 Valve Corporation，按 Steamworks SDK 条款随游戏分发；上游 Steamworks.NET 封装为 MIT。
