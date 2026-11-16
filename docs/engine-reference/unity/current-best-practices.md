# Unity 当前最佳实践（针对本项目）

> 这些实践直接服务于 `design/gdd/game-concept.md` §5.4 的四条**不可协商架构约束**。
> 最后验证：2026-11。标注「待验证」的条目使用前请核对 6000.6 文档。

## 1. 模拟层必须是纯 C#

**要求**：田块、字、祟气、村民、季相等全部游戏状态放在**不继承 `MonoBehaviour`、不引用 `UnityEngine` 渲染类型**的普通 C# 类里。

```csharp
// ✅ 正确：纯 C# 状态，可脱离引擎单元测试
public sealed class FarmState
{
    public readonly List<Plot> Plots = new();
    public int QiOfSui;              // 祟气
    public int SeasonIndex;
    public void AdvanceTick(TickContext ctx) { /* 纯逻辑 */ }
}

// ❌ 错误：状态挂在 MonoBehaviour 上，无法离线测试，联机化即重写
public class FarmManager : MonoBehaviour { /* ... */ }
```

**验收方式**：不启动 Unity 渲染，也能跑完一整季农事轮。用 EditMode 测试跑。

## 2. 确定性 tick + 命令式输入

- 农事轮由**固定步长**推进（`FixedUpdate` 或自建 `TickLoop`），不用 `Time.deltaTime` 驱动的浮点累加做规则判定。
- 玩家操作表达为**命令对象**，不是直接改状态：

```csharp
public readonly struct ComposeCharacterCommand
{
    public readonly int PlotIndex;
    public readonly ComponentId Left, Right;
}
```

**为什么**：这一条是日后加联机（§5.4）的唯一低代价路径，同时让"字阵胜负"能被脚本批量回归——第一款游戏最缺的就是回归能力。

## 3. 存档 = 状态快照

- 存档 = 序列化整个 `FarmState`（含村民、字谱、季相），**不含任何引擎对象引用**。
- 用 System.Text.Json 或 Newtonsoft，**不要用 `JsonUtility`**（不支持字典/多态，而字谱几乎必然是字典）。
- 存档版本号从第一天就写进去，`SaveVersion` 字段不能省。

## 4. 数据驱动：字表是资产，不是代码

- 300–500 个字的白名单 → `ScriptableObject` 或 JSON + Addressables。
- 每个字条目至少含：`部件A`、`部件B`、`结果字`、`字义类别`（产出/防御/地力/关系）、`战意值`、`产出系数`。
- **合字校验 = 查表**，不写任何自然语言处理。查不到 → 祟字（失败态）。这是游戏规则，不是技术妥协。

## 5. 中文字排版（本项目最强依赖）⚠️ 待验证

- 300+ 汉字 + 竖排 + 毛笔字体：必须用**动态字体图集**，不能把整个 CJK 字体打进包（体积会爆）。
- 优先验证 TextMeshPro 的 CJK 动态图集在 6000.6 下的表现；若 UI Toolkit 的文本组件已支持，则 UI 层用它。
- 竖排文字（古籍/农书翻页）通常需要自定义排版或 shader，预留时间。

## 6. 2D 水墨管线

- URP + 2D Renderer；笔触效果走 Shader Graph 后处理（墨晕/纸纹/飞白）。
- 四季色盘用后处理或调色板资产统一切换，**不要逐素材改贴图**。
- 动画：字形变形用顶点动画或 shader，不做逐帧（工作量不可控）。

## 7. Steam 上架（死线风险）⚠️ 待验证

- Steamworks C# 绑定（Steamworks.NET 或官方绑定）需在**第一个月**跑通：成就、云存档、Overlay。
- 构建目标：Windows x64（首要）；macOS/Linux 视时间。
- 手柄：Input System 的 PlayerInput + Input Actions，从原型阶段就支持，不要后补。

## 8. 禁止模式

- ❌ 游戏状态放进 `MonoBehaviour` 字段
- ❌ 单例承载状态（`GameManager.Instance` 存玩法数据）
- ❌ 规则层写死"当前玩家"（未来联机必修）
- ❌ `Resources.Load` 加载玩法数据
- ❌ `Time.deltaTime` 参与规则判定
- ❌ 用 `JsonUtility` 存字典/多态
