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

### Editor Play Mode 绕过策略

手游项目常依赖 Firebase / Ads SDK / GDPR 同意弹窗等网络服务，导致 Editor Play Mode 无法正常进入。本 Skill 提供完整的绕过方案：

- `#if UNITY_EDITOR` 跳过 Firebase RemoteConfig 网络请求
- 跳过 GDPR/CTU 同意弹窗
- 跳过 Ads SDK 初始化等待
- 处理 ObscuredTypes（CodeStage AntiCheat Toolkit）加密字段
- 强制满足教程触发条件（StageClearCount、OwnGold、BossClear 等）
- 自动截图协程 + 卡住检测 + 异步 UI 等待

### 双模式 Excel 输出

- **完整版**（含属性）：每个事件下列出所有属性行（事件名 + 属性名 + 类型 + 说明）
- **精简版**（仅事件）：每个事件一行，不含属性

Excel 生成支持两种模式：
- **模板免依赖**（默认）：从零生成完整 .xlsx，无需上传引力引擎模板
- **模板依赖**（可选）：复制引力引擎模板并替换内部 XML

## 📁 文件结构

```
extract-tutorial-tracking/
├── SKILL.md                                    # 主工作流指引（AI 读取的核心指令）
├── references/
│   ├── tutorial-system-patterns.md             # 6 种教程系统模式 + 识别流程图 + ObscuredTypes + 条件类型表
│   ├── gravity-engine-excel-format.md          # 引力引擎 Excel 格式规范 + 双模式生成 + XLSX 内部 XML 结构
│   └── editor-playmode-bypass.md               # Play Mode 阻塞点绕过 + ObscuredTypes 写入 + 强制满足条件
└── scripts/
    ├── GenerateTrackingExcel.cs                # C# Editor 脚本（Excel 生成引擎，支持模板免依赖）
    └── TutorialScreenshotCapture.cs            # C# Editor 脚本（Play Mode 自动截图 + 条件强制 + 卡住检测）
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

### 输出位置

所有产出统一在项目根目录的 `Tracking/` 文件夹：

```
Tracking/
├── Tutorial_Tracking_Plan.md              # 埋点方案文档
├── tutorial_tracking_full.xlsx             # 引力引擎 Excel（完整版）
├── tutorial_tracking_events_only.xlsx     # 引力引擎 Excel（精简版）
├── GenerateTrackingExcel.cs               # Excel 生成脚本
├── TutorialScreenshotCapture.cs            # 截图脚本
└── screenshots/                           # P0 截图
```

## 🔧 技术细节

### Excel 生成方案（模板免依赖）

当不需要引力引擎模板时，Skill 使用 C# Editor 脚本从零构建 xlsx：

1. 生成 `[Content_Types].xml`、`_rels/.rels`、`xl/workbook.xml`、`xl/styles.xml`
2. 构建 `xl/sharedStrings.xml`（去重字符串表）
3. 构建 `xl/worksheets/sheet1.xml`（单元格数据 + 合并单元格定义）
4. 用 `ZipArchive(Create mode)` 写入所有 XML 部件
5. 使用 `UTF8Encoding(false)` 写入（无 BOM）

### 截图方案（Play Mode）

通过 Editor 脚本在 Play Mode 下自动截图：

1. `#if UNITY_EDITOR` 绕过 Firebase/Ads/GDPR 等网络依赖
2. 强制满足教程条件（StageClearCount=999, OwnGold=9999999 等）
3. 处理 ObscuredTypes 加密字段（修改 hiddenValue/fakeValue/inited）
4. 轮询 CurTID 变化，自动截图 + OnClick 推进
5. 卡住检测（3 次相同 TID 自动跳过）

### ObscuredTypes 处理

CodeStage AntiCheat Toolkit 是手游标配反作弊库：

- **读取**：`.asset` 文件（YAML）中 `fakeValue` 字段即为实际值
- **写入**：修改 ObscuredInt 内部字段 `hiddenValue`、`fakeValue`、`inited`
- **限制**：`execute_csharp_script`（Roslyn）无法访问 Assembly-CSharp 类型，必须用 Editor 脚本

### 常见坑点

- `Assets/Editor/` 可能有 asmdef 导致程序集隔离 → 放在无 asmdef 的文件夹
- iOS Xcode 脚本在 Windows 上编译报错 → 用 `#if UNITY_IOS` 包裹
- `execute_csharp_script`（Roslyn）无法访问 Assembly-CSharp → 用 Editor 脚本 + `[MenuItem]`
- Firebase RemoteConfig 在 Editor 中无法连接 → `#if UNITY_EDITOR` 跳过
- ObscuredTypes 不能直接 `SetValue(int)` → 修改内部字段

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
- 引力引擎 Excel 模板（可选，模板免依赖模式不需要）

## 📄 License

MIT
