# 门检查报告 · Godot 重新立项

| 字段 | 值 |
| --- | --- |
| **日期** | 2026-11-17 |
| **检查者** | agent（Godot 重新立项工单） |
| **项目** | 田字格 · Tianzi Ge |
| **裁决** | **通过（含关注项）** |
| **前序报告** | `2026-11-17-tech-setup-to-preproduction.md`（Unity 选型，已被本次重新立项取代，仅作历史） |

---

## 1. 必需产物: 5/5 存在

| | 产物 | 状态 | 说明 |
| --- | --- | --- | --- |
| [x] | 引擎已选择 | ✅ | `CLAUDE.md` = **Godot 4.7.2 stable（.NET / mono）**；固定补丁号；无 `[CHOOSE]` 残留 |
| [x] | 技术偏好已配置 | ✅ | `.claude/docs/technical-preferences.md`：C# 命名、Sim/View 分层、60fps 预算、Godot 禁止模式 |
| [x] | 架构决策记录（ADR） | ✅ | ADR-001 / 002 / 004 已采纳；ADR-003 标记废止；索引齐全 |
| [x] | 引擎参考文档 | ✅ | `docs/engine-reference/godot/VERSION.md`：版本、校验和、已验证 / 未验证清单、agent 规则 |
| [x] | Godot 工程与分层 | ✅ | `Client/`（Godot + View csproj）+ `Sim/`（纯 .NET）+ `Tests/` + `Balance/` |

## 2. 质量检查: 2/2 通过

| | 检查项 | 状态 | 证据 |
| --- | --- | --- | --- |
| [x] | 架构决策涵盖核心系统 | ✅ | ADR-001 状态与确定性；ADR-002 数据；ADR-004 引擎 / 渲染 / 输入 |
| [x] | 技术偏好含命名约定与性能预算 | ✅ | 命名表 + 60fps / 16.6ms + CJK 字形约束 |

## 3. 上游阶段完整性（系统设计 → 技术设置）

| 检查项 | 状态 |
| --- | --- |
| 系统索引含 MVP 枚举 | ✅ 29 个系统，MVP 17 个 |
| 至少 1 份 GDD | ✅ 17 份系统 GDD（含概念与索引为 19 份文档） |
| GDD 具备 8 个必需章节 | ✅ 全部按 8 章节结构编写 |
| 依赖关系已映射 | ✅ 分层图 + 瓶颈系统 + 3 个环 |
| MVP 优先级已定义 | ✅ 四层优先级 + 推荐设计顺序 |
| MVP 全部系统设计完成 | ✅ 17/17 已批准 |

## 4. 可执行证据（T-001）

```text
$ dotnet test
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12

$ dotnet run --project Balance -- --runs 20
Balance skeleton OK: runs=20, seed_base=0, final_hash=0x1A271B7B4135C131

$ dotnet build Client/TianziGe.View.csproj
Build succeeded. 0 Warning(s), 0 Error(s)

$ godot --headless --path Client --build-solutions --quit
Godot Engine v4.7.2.stable.mono.official.ed1daf0bf
[ DONE ] dotnet_build_project

$ godot --headless --path Client -- --boot-probe
Godot Engine v4.7.2.stable.mono.official.ed1daf0bf
TianziGe boot OK: sim=TianziGe.Sim money=3
```

## 5. 阻塞项

**无。**

## 6. 关注项（不阻塞推进，但必须在预生产期解决）

| # | 关注项 | 风险 | 要求 |
| --- | --- | --- | --- |
| 1 | **CJK 动态字形 / 竖排在 Godot 4.7.2 下未验证** | **最高**：字体方案错了，整个 UI 层（P2/P3/P4）要返工 | **第 1 周技术验证（T-002）** |
| 2 | **Godot 导出产物内 JSON 是否可达未验证** | 高：打包后字表读不到 | 第 1 周（T-003） |
| 3 | **Steam 绑定方案未验证**（GodotSteam / Steamworks.NET） | 上架死线 | 第一个月跑通成就 + 云存档（T-004） |
| 4 | 悬置项 Q5：开垦回本过快（0.3 回合） | 中：经济张力消失 | 可行性原型前调数 |
| 5 | 悬置项 Q8：一局 28 回合 / 6–8 小时体量 | 中：单人产能可能不足 | 原型后复核，必要时缩到 3 季 |
| 6 | 悬置项 Q10：C7 缺"只读 D/E 估算接口" | 低：P2 可能写出第二套公式 | 写代码前补契约 |
| 7 | 没有 CI；当前门禁靠本地命令 | 中：回归不会被自动拦截 | 原型稳定后加最小 CI（`dotnet test` 优先） |
| 8 | Windows x64 导出未验证 | 中：发布平台 | 原型后做一次导出冒烟 |

## 7. 建议的下一步

1. **T-002**：CJK 动态字形 + 竖排（最高风险，先出结论）
2. **T-003**：JSON → Sim → Godot 导出（与 T-002 并行，互不阻塞）
3. **T-004**：Steam 初始化 + 成就（需要人类提供 AppID / 测试账号）
4. T-005 起按 `production/sprints/sprint-01.md` 推进；不要跳过「合字好不好玩」的生死门

> **不要跳过 T-002 去做美术。** 字体方案没定，UI 层的返工成本最高。
