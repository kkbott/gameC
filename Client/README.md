# Client —— Unity 工程位置

Unity 工程放在**这个目录里**，最终结构：

```
gameC/
├── design/          # 设计文档（仓库根）
├── docs/            # 引擎参考
├── CLAUDE.md
└── Client/          # ← Unity 工程根
    ├── Assets/
    ├── Packages/
    └── ProjectSettings/
```

## 为什么 Unity 工程不放在仓库根

设计文档是这个仓库的一等公民，而 Unity 会往根目录塞 `Library/`、`Temp/`、`*.csproj`。两者混在一起会让 `git status` 长期处于噪声状态。`.gitignore` 已覆盖 Unity 生成物，但目录分层能让"文档 vs 代码"的边界一眼可见。

## 尚未创建

Unity 工程还没建，原因有二：

1. **先验「合字好不好玩」**，再开正式工程。可行性原型可以用最粗糙的方式做（甚至先做纸面/表格推演），不需要完整工程结构。
2. **Workstation 路径问题**：本仓库目前暂存在 agent 会话工作区（路径含 `.dsh/state`），Unity 会在 `ProjectSettings`、`.csproj`、`Library` 里写大量绝对路径。**建 Unity 工程前，先把整个 `gameC/` 搬到正常开发路径**（例如 `~/dev/gameC`）。

## 建工程时的检查项

- [ ] 模板选 **Universal 2D**（URP + 2D Renderer）
- [ ] 输入系统选 **Input System Package**（新版）
- [ ] 项目名 `TianziGe`，命名空间根 `TianziGe`，分层 `TianziGe.Sim` / `TianziGe.View`
- [ ] 先建 `TianziGe.Sim` 程序集（纯 C#，不引用 UnityEngine），再建 `TianziGe.View`
- [ ] 挂上测试：EditMode 测试覆盖模拟层
