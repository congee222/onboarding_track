# 🎮 extract-tutorial-tracking

> Codely CLI Skill — 从 Unity 游戏项目中自动提取新手教程交互点位，生成埋点文档和引力引擎 Excel。

## 📋 这是什么

做游戏数据分析的同学都知道，埋点文档的梳理是一个**重复且耗时**的过程：读代码搞清楚教程系统怎么运作 → 从数据表里逐行解析交互步骤 → 判断哪些是核心转化节点 → 写埋点文档给研发和数据团队对齐 → 按平台格式整理 Excel。

这个 Skill 把整个流程**固化为可复用的工作流**，AI 会自动完成从代码分析到文档输出的全链路工作。

## ✨ 功能特性

### 五步自动化工作流

| 步骤 | 功能 | 说明 |
|:---:|---|---|
| 1 | **发现教程系统** | 搜索代码、数据表、枚举定义，自动识别教程架构模式 |
| 2 | **提取交互点位** | 解析教程数据表，筛选 P0 核心转化漏斗（10-12个关键节点） |
| 3 | **生成埋点文档** | 输出 Markdown 文档，含事件总览表 + 逐事件属性定义 + 枚举参考 |
| 4 | **运行时截图** | 通过 Editor 脚本 + 反射控制教程状态，逐个截取 Game View 截图 |
| 5 | **生成 Excel** | 输出引力引擎批量添加事件模板格式的 .xlsx 文件 |

### 支持的 6 种 Unity 教程系统架构

| 架构模式 | 适用场景 | 识别方式 |
|---|---|---|
| **Data-Table-Driven** | 韩系/中式手游 | 有 CSV/JSON 教程数据表 → ScriptableObject |
| **Enum/State-Machine** | 独立/中型项目 | 有 `TutorialStep` 枚举 + `switch` 语句 |
| **Timeline-Driven** | 3D/剧情向游戏 | 有 `.playable`/`.timeline` 文件 |
| **Event/String-ID** | 事件总线架构 | 用字符串事件 ID 驱动教程流程 |
| **Visual Scripting** | Unity 可视化编程 | 有 ScriptGraph/StateGraph 资产 |
| **Prefab-Sequence** | 休闲/超休闲游戏 | 有编号的步骤 Prefab 序列 |

内置**快速识别流程图**，按顺序回答 6 个 yes/no 问题即可匹配到对应模式。

### 双模式 Excel 输出

- **完整版**（含属性）：每个事件下列出所有属性行（事件名 + 属性名 + 类型 + 说明）
- **精简版**（仅事件）：每个事件一行，不含属性

Excel 格式严格遵循引力引擎批量添加事件模板规范：
- A-E 列（事件信息）支持纵向合并
- F-I 列（属性行）禁止合并
- 事件名/属性名遵循命名规则（英文、字母开头、≤50字符）
- 属性类型支持：文本、整数、浮点数、布尔值、日期、时间、列表

## 📁 文件结构

```
extract-tutorial-tracking/
├── SKILL.md                                    # 主工作流指引（AI 读取的核心指令）
├── references/
│   ├── tutorial-system-patterns.md             # 6 种教程系统模式 + 识别流程图 + UI 元素命名参考
│   └── gravity-engine-excel-format.md          # 引力引擎 Excel 格式详细规范 + XLSX 内部 XML 结构
└── scripts/
    └── GenerateTrackingExcel.cs                # C# Editor 脚本模板（通用 Excel 生成引擎）
```

## 📦 安装

### 方式 1：直接复制（推荐）

```bash
git clone https://github.com/congee222/onboarding_track.git
cp -r onboarding_track /your-project/.codely-cli/skills/extract-tutorial-tracking
```

### 方式 2：用 Codely CLI 安装

```bash
codely skills install extract-tutorial-tracking.skill --scope workspace --consent
```

### 方式 3：Git Submodule（团队共享）

```bash
cd /path/to/your-game-project
git submodule add https://github.com/congee222/onboarding_track.git .codely-cli/skills/extract-tutorial-tracking
```

安装后在 Codely CLI 中执行 `/skills reload` 激活。

## 🚀 使用

### 基本用法

在 Codely CLI 中直接对话即可触发：

```
> 帮我提取新手教程的埋点
> 整理一下教程交互点位
> 生成引力引擎格式的埋点 Excel
```

### 需要连接 Unity 的功能

| 功能 | 是否需要 Unity | 说明 |
|---|:---:|---|
| 搜索教程代码 | ❌ | 只需项目文件 |
| 提取交互点位 | ❌ | 只需读取数据表 |
| 生成埋点 MD 文档 | ❌ | 纯文本输出 |
| 运行时截图 | ✅ | 需要 Play Mode + Game View |
| 生成 Excel | ❌ | 不需要 Unity（用 C# Editor 脚本执行） |

### 输出示例

**埋点文档**（Markdown）：
```markdown
| 序号 | 点位中文名 | 英文名 | 所属阶段 | 截图文件 |
|:---:|---|---|---|---|
| 1 | 教程开始 | tutorial_start | 教程100 | p01_tutorial_start.png |
| 2 | 点击垃圾桶 | trash_tap | 教程100 | p02_trash_tap.png |
| ... | ... | ... | ... | ... |
```

**Excel**（引力引擎格式）：
```
事件名              | 事件显示名 | 是否接收 | 触发时机     | 事件说明     | 属性名         | 属性显示名 | 属性类型 | 属性说明
tutorial_start      | 教程开始   | 是      | 游戏启动后…  | 教程100阶段  | tutorial_id   | 教程ID    | 整数    | 100
(合并)              | (合并)    | (合并)  | (合并)      | (合并)      | tutorial_phase| 教程阶段  | 文本    | tutorial_100
```

## 🔧 技术细节

### Excel 生成方案（无 Python 环境）

当项目环境中没有 Python/openpyxl 时，Skill 使用 C# Editor 脚本直接操作 XLSX 内部 XML：

1. 构建 `sharedStrings.xml`（去重字符串表）
2. 构建 `sheet1.xml`（单元格数据 + 合并单元格定义）
3. 复制引力引擎模板 `.xlsx`
4. 用 `ZipArchive(Update mode)` 替换内部 XML 条目
5. 使用 `UTF8Encoding(false)` 写入（无 BOM）

### 截图方案

通过 Editor 脚本 + 反射控制教程状态：
- 重置教程完成状态 → 触发指定教程步骤
- 调用 `SetActiveFinger` / `SetActiveTextBox` / `SetActiveDim` 显示引导 UI
- 截取 Game View（支持竖屏 1080×2340）

### 常见坑点

- `Assets/Editor/` 可能有 asmdef 导致程序集隔离 → 放在 `Assets/1_Scripts/Editor/`
- iOS Xcode 脚本在 Windows 上编译报错 → 用 `#if UNITY_IOS` 包裹
- `execute_csharp_script`（Roslyn）无法访问 Assembly-CSharp → 用 Editor 脚本 + `[MenuItem]`

## 📊 P0 筛选原则

> 从"游戏启动"到"教程完成"的最短路径上，每个玩家必须执行的操作就是一个 P0 点位。

典型的新手教程 P0 漏斗：

```
教程开始 → 招客 → 接单 → 烹饪 → 上菜(厨房) → 切场景 → 上菜(大厅) → 收款 → 擦桌 → 教程完成
```

这 10-12 个点构成完整的**新手转化漏斗**，任何一个环节流失都值得分析。

## 📝 依赖

- [Codely CLI](https://www.tuanjie.com/) (Tuanjie Cowork)
- Unity Editor（截图功能需要，其他功能不需要）
- 引力引擎 Excel 模板（`引力引擎_批量添加事件模板.xlsx`，由使用方提供）

## 📄 License

MIT
