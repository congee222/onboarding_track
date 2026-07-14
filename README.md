# extract-tutorial-tracking

Codely CLI Skill — 从 Unity 游戏项目中提取新手教程交互点位，生成埋点文档和引力引擎 Excel。

## 安装

```bash
# 方式1：直接复制到项目的 .codely-cli/skills/ 目录
cp -r extract-tutorial-tracking /your-project/.codely-cli/skills/

# 方式2：用 codely CLI 安装打包文件
codely skills install extract-tutorial-tracking.skill --scope workspace --consent
```

安装后在 Codely CLI 中执行 `/skills reload` 激活。

## 功能

1. **发现教程系统** — 自动识别 6 种常见 Unity 教程架构
2. **提取交互点位** — 筛选 P0 核心转化漏斗
3. **生成埋点文档** — Markdown 格式，含事件表 + 属性定义 + 枚举参考
4. **运行时截图** — 通过 Editor 脚本控制教程状态并截图（需连接 Unity）
5. **生成 Excel** — 引力引擎批量添加事件模板格式

## 文件结构

```
extract-tutorial-tracking/
├── SKILL.md                              # 主工作流
├── references/
│   ├── tutorial-system-patterns.md       # 6 种教程系统模式 + 识别流程图
│   └── gravity-engine-excel-format.md    # 引力引擎 Excel 格式规范
└── scripts/
    └── GenerateTrackingExcel.cs          # C# Excel 生成脚本模板
```

## 使用

在 Codely CLI 中说：
- "帮我提取新手教程的埋点"
- "整理一下教程交互点位"
- "生成引力引擎格式的埋点 Excel"
