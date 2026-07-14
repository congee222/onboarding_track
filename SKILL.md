---
name: extract-tutorial-tracking
description: Analyze Unity project tutorial/guide systems to extract interactive touchpoints, generate analytics tracking documentation (Markdown), and produce Excel templates in Gravity Engine (引力引擎) batch event format. Use when the user asks to extract tutorial events, create tracking plans, generate埋点文档, or produce analytics Excel files for a Unity game's onboarding/tutorial system.
---

# Extract Tutorial Tracking Points

Extract interactive touchpoints from a Unity game's tutorial/guide system and generate analytics tracking documentation + Excel templates.

## Workflow

### 1. Discover Tutorial System

Search the codebase for tutorial-related scripts and data:

- **Scripts**: Search for files containing `Tutorial`, `Guide`, `Teach`, `Learn` (case-insensitive) in `Assets/`
- **Data**: Search for CSV/JSON/XML files like `tutorialScriptTable`, `GuideQuestTable`
- **Enums**: Search for enums defining steps, targets, finger types, wait conditions
- **Key class**: Find the central manager (e.g., `TutorialMgr`) and read it fully

Then identify which pattern the project uses. See `references/tutorial-system-patterns.md` for 6 common patterns and a quick identification flowchart:

| Pattern | Quick Check |
|---------|-------------|
| **Data-Table-Driven** (CSV/JSON → ScriptableObject) | Has a CSV/JSON table with "tutorial" in name? |
| **Enum/State-Machine** | Has a `TutorialStep` enum + `switch` in manager? |
| **Timeline-Driven** | Has `.playable`/`.timeline` files in Tutorial folder? |
| **Event/String-ID** | Uses string event IDs like `"tutorial.step.1"`? |
| **Visual Scripting** | Has ScriptGraph/StateGraph assets + ScriptMachine? |
| **Prefab-Sequence** | Has numbered prefabs `TutorialStep_01.prefab`? |

Match the pattern, then follow the extraction approach described in the reference file for that pattern.

For any pattern, focus on:
- Enum definitions (`TutTargetType`, `TutFingerType`, `TutWaitType`, `TutStartType`, etc.)
- Data table structure (CSV columns → ScriptableObject fields)
- How interactive points are defined (finger + target + wait condition per row)
- How events trigger (`NoticeTutorialStartEvent`, `NoticeTutorialWaitEvent`, `NextStep()`, signal callbacks, etc.)
- Search for common UI element names: `finger`, `hand`, `dim`, `textBox`, `skip`, `highlight`, `arrow`

### 2. Extract Interactive Points

Read the tutorial data table (e.g., `tutorialScriptTable.csv`) and map:

- Each row = one interactive step
- Group by tutorial ID (e.g., 100/200/300 for main, 1/201-205/301-309 for sub)
- For each step, identify: target UI element, finger gesture, wait condition, trigger timing

**P0 priority** (core conversion funnel): Select the minimal set of events that represent the critical path from game start → tutorial complete. Typically 10-12 points covering: tutorial start → each core gameplay action → tutorial complete.

### 3. Generate Tracking Documentation (Markdown)

Create an MD file with:
- Event summary table (序号, 中文名, 英文名, 所属阶段, 截图文件)
- Per-event detail sections with: trigger timing, event properties (中文名, 英文名, 类型)
- Common properties table (user_id, device_type, etc.)
- Enum reference tables

### 4. Capture Screenshots (Unity)

If Unity is available:

1. Create an Editor script at `Assets/1_Scripts/Editor/` (NOT `Assets/Editor/` which has asmdef isolation) with `[MenuItem]` methods
2. Access the tutorial manager. Approach depends on pattern:
   - **Data-Table-Driven / Enum-Driven**: Use reflection to call private methods (`SetActiveDim`, `SetActiveTextBox`, `SetActiveFinger`)
   - **Timeline-Driven**: Set `PlayableDirector.time` to jump to specific frames; enable/disable UI overlays
   - **Event/String-ID**: Fire the event that triggers each tutorial step
   - **Prefab-Sequence**: Instantiate the step prefab directly
   - **Visual Scripting**: Set the state machine variable to each step value
3. For each P0 point: reset tutorial state → trigger the step → capture Game View screenshot
4. Set Game View to portrait resolution (e.g., 1080×2340) before capturing
5. Search for common UI element names in scene hierarchy: `finger`, `hand`, `dim`, `textBox`, `skip`, `highlight`, `arrow` — see `references/tutorial-system-patterns.md` for full list

**Common pitfalls**:
- `Assets/Editor/` may have an asmdef isolating it from Assembly-CSharp; use `Assets/1_Scripts/Editor/` or any folder without asmdef
- `execute_csharp_script` (Roslyn) cannot access `Assembly-CSharp` types; use Editor scripts with `[MenuItem]`
- iOS-only Editor scripts may cause compilation errors on Windows; wrap in `#if UNITY_IOS` / `#endif`
- If `ZipFile`/`ZipArchive` is unavailable in Roslyn, use it from an Editor script instead

### 5. Generate Excel (Gravity Engine format)

Use the C# Editor script template at `scripts/GenerateTrackingExcel.cs` as a starting point.

**Excel format** (引力引擎批量添加事件模板):
- **Columns A-E** (merged per event): 事件名 | 事件显示名 | 是否接收 | 触发时机 | 事件说明
- **Columns F-I** (one row per property): 属性名 | 属性显示名 | 属性类型 | 属性说明
- **Property types**: 文本, 整数, 浮点数, 布尔值, 日期, 时间, 列表
- A-E merged for multi-property events; F-I must NOT have merged cells
- Event names: English, letters/digits/underscore only, start with letter, ≤50 chars
- Property names: same rules, cannot start with `$`

**Implementation approach** (when Python/openpyxl is unavailable):
1. Create C# Editor script that builds sharedStrings.xml + sheet1.xml
2. Copy the template .xlsx, open with `ZipArchive(FileStream, Update)`, replace entries
3. Use `System.Security.SecurityElement.Escape()` for XML escaping
4. Write with `UTF8Encoding(false)` (no BOM) for XML content

See `references/gravity-engine-excel-format.md` for detailed column specs and rules.

## Key Files

- `scripts/GenerateTrackingExcel.cs` — C# Editor script template for Excel generation
- `references/gravity-engine-excel-format.md` — Excel template format reference
- `references/tutorial-system-patterns.md` — 6 common Unity tutorial system patterns + identification flowchart + UI element naming reference

## Notes

- When switching to a new Unity project, run through the identification flowchart in `references/tutorial-system-patterns.md` first to determine the tutorial system pattern, then follow the matching extraction approach
- If the project has iOS-only Editor scripts causing compilation errors on Windows, wrap them in `#if UNITY_IOS` / `#endif` to unblock Assembly-CSharp-Editor compilation
- The `Assets/Editor/` directory may have an asmdef that isolates it from Assembly-CSharp; place scripts in a folder without asmdef (e.g., `Assets/1_Scripts/Editor/`)
- `execute_csharp_script` (Roslyn) cannot access `Assembly-CSharp` types directly; use Editor scripts with `[MenuItem]` instead
- The Excel generation script template (`scripts/GenerateTrackingExcel.cs`) is fully reusable — only the `events` data list and two path constants need to be changed per project
