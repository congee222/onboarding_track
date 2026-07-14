---
name: extract-tutorial-tracking
description: Analyze Unity project tutorial/guide systems to extract interactive touchpoints, generate analytics tracking documentation (Markdown), and produce Excel templates in Gravity Engine (引力引擎) batch event format. Use when the user asks to extract tutorial events, create tracking plans, generate埋点文档, or produce analytics Excel files for a Unity game's onboarding/tutorial system.
---

# Extract Tutorial Tracking Points

Extract interactive touchpoints from a Unity game's tutorial/guide system and generate analytics tracking documentation + Excel templates.

## Output Location

All outputs go to a single `Tracking/` folder in the project root:
```
Tracking/
├── Tutorial_Tracking_Plan.md              # 埋点方案文档
├── tutorial_tracking_full.xlsx             # 引力引擎 Excel（事件+属性）
├── tutorial_tracking_events_only.xlsx     # 引力引擎 Excel（仅事件）
├── GenerateTrackingExcel.cs               # Excel 生成脚本
├── TutorialScreenshotCapture.cs            # 截图脚本
└── screenshots/                           # P0 截图
    ├── p01_tutorial_1001_skill_use.png
    └── ...
```

## Workflow

### 1. Discover Tutorial System

Search the codebase for tutorial-related scripts and data:

- **Scripts**: Search for files containing `Tutorial`, `Guide`, `Teach`, `Learn` (case-insensitive) in `Assets/`
- **Data**: Search for CSV/JSON/XML files like `tutorialScriptTable`, `GuideQuestTable`
- **Enums**: Search for enums defining steps, targets, finger types, wait conditions
- **Key class**: Find the central manager (e.g., `TutorialMgr`) and read it fully

Then identify which pattern the project uses. See `references/tutorial-system-patterns.md` for 6 common patterns and a quick identification flowchart.

For any pattern, focus on:
- Enum definitions (`TutTargetType`, `TutFingerType`, `TutWaitType`, `TutStartType`, etc.)
- Data table structure (CSV columns → ScriptableObject fields)
- How interactive points are defined (finger + target + wait condition per row)
- How events trigger (`NoticeTutorialStartEvent`, `NoticeTutorialWaitEvent`, `NextStep()`, signal callbacks, etc.)
- Search for common UI element names: `finger`, `hand`, `dim`, `textBox`, `skip`, `highlight`, `arrow`
- **Existing analytics calls**: Search for `FirebaseAnalytics.LogEvent`, `MyFirebase.Log`, or similar — these reveal already-tracked events

### 2. Extract Interactive Points

Read the tutorial data table (e.g., `tutorialScriptTable.csv` or `.asset`) and map:

- Each row = one interactive step
- Group by tutorial ID (e.g., 100/200/300 for main, 1/201-205/301-309 for sub)
- For each step, identify: target UI element, finger gesture, wait condition, trigger timing
- **Read the `IsLobby` property** — tutorials may only trigger in lobby vs. battle scenes
- **Read condition types** — `STAGE_CLEAR`, `OWN_GOLD`, `BOX_COUNT`, `BOSS_CLEAR`, `BARRACK`, `ENERGY` etc. determine when tutorials activate

**P0 priority** (core conversion funnel): Select the minimal set of events that represent the critical path from game start → tutorial complete. Typically 10-12 points covering: tutorial start → each core gameplay action → tutorial complete.

### 3. Generate Tracking Documentation (Markdown)

Create `Tracking/Tutorial_Tracking_Plan.md` with:
- Event summary table (序号, 中文名, 英文名, 所属阶段, 截图文件)
- Per-event detail sections with: trigger timing, event properties (中文名, 英文名, 类型)
- Common properties table (user_id, device_type, etc.)
- Enum reference tables
- Conversion funnel diagram (ASCII art)
- Key churn monitoring points

### 4. Capture Screenshots (Unity Play Mode)

**Prerequisite**: Read `references/editor-playmode-bypass.md` first — many mobile games have Firebase, Ads SDKs, GDPR consent, and other network dependencies that block Editor Play Mode.

#### 4a. Bypass Network Dependencies (if needed)

If Play Mode gets stuck on loading/network init, add `#if UNITY_EDITOR` guards to skip:
- `FirebaseApp.CheckAndFixDependenciesAsync()` → skip, use test data
- `Core.ConnectNetwork()` → skip, call `done?.Invoke()` directly
- `GDPR/CTU consent dialog` → skip in `EndLoad`
- `AdsCtr.IsInit` wait loop → skip in `EndLoad`
- `LocalPushCtr.RequestNotificationPermission()` → skip
- Intro cutscenes → skip directly to `LoadScene(Stage)`

Also: `PlayerPrefs.DeleteKey("GameDataKey")` to reset save data for fresh tutorial.

#### 4b. Create Screenshot Script

Create an Editor script with `[MenuItem]` methods at a folder **without asmdef** (e.g., `Assets/2.Scripts/Editor/`). The script should:

1. **Force-satisfy tutorial conditions**: Set `StageClearCount`, `OwnGoldValue`, `BossDifficulty`, `LastBarrackNumber` etc. to high values so all tutorials activate. See `references/editor-playmode-bypass.md` for ObscuredTypes handling.
2. **Auto-capture coroutine**: For each tutorial step:
   - Wait for `CurTID > 0` (tutorial activated by `ManagerLateUpdate`)
   - `ScreenCapture.CaptureScreenshot()` 
   - Call `UITutorial.OnClick()` via reflection to advance
   - Wait for `CurTID` to change (poll every 0.5s, timeout 10s)
   - Stuck detection: if same TID appears 3 times, force-clear
3. **Handle chain tutorials**: Some tutorials have `Next_ID` chains — after `OnClick`, the next step activates automatically. Wait for the new `CurTID`.
4. **Handle scene transitions**: Some tutorials require returning to lobby (`LoadScene(Stage, true, null)`) or entering battle (click Start button) to trigger.

#### 4c. Fallback: Edit Mode Prefab Rendering

If Play Mode is completely blocked (rare), render tutorial prefab in Edit Mode:
- Load `TutorialTable.asset` via `AssetDatabase.LoadAssetAtPath`
- Load `Popup_Tutorial.prefab`
- Create `ScreenSpaceCamera` Canvas + dedicated Camera
- Instantiate prefab, call `UITutorial.RefreshUI()` with dummy target
- Capture via `RenderTexture` → `Texture2D.ReadPixels` → `EncodeToPNG`
- **Limitation**: Only shows tutorial UI (finger + highlight), no game background

#### 4d. Manual Fallback

If auto-capture fails on complex flows:
1. Enter Play Mode
2. Use `Tools/Tracking/Advance Tutorial Step` menu to advance each step
3. Use `unity_screenshot.capture_game_view` after each advance
4. Name screenshots to match P0 events: `p01_tutorial_1001_skill_use.png`

**Common pitfalls** (see `references/editor-playmode-bypass.md` for details):
- `Assets/Editor/` may have asmdef isolation; use a folder without asmdef
- `execute_csharp_script` (Roslyn) cannot access `Assembly-CSharp` types; use Editor scripts with `[MenuItem]`
- iOS-only Editor scripts cause compilation errors on Windows; wrap in `#if UNITY_IOS`
- ObscuredTypes (AntiCheat) cannot be set via `FieldInfo.SetValue(int)` — must modify internal fields (`hiddenValue`, `fakeValue`, `inited`)
- `ScreenCapture.CaptureScreenshot` writes to project root (relative path), not to absolute path
- Tutorial `ManagerLateUpdate` only fires when `CanvasType == Main` UI is active

### 5. Generate Excel (Gravity Engine format)

Use the C# Editor script template at `scripts/GenerateTrackingExcel.cs`.

**Two modes**:
- **Template-based** (original): Copy 引力引擎 template `.xlsx`, replace internal XML via `ZipArchive(Update)`
- **Template-free** (improved): Generate complete `.xlsx` from scratch by writing all XML parts (`[Content_Types].xml`, `_rels/.rels`, `xl/workbook.xml`, `xl/styles.xml`, `xl/sharedStrings.xml`, `xl/worksheets/sheet1.xml`) into a new `ZipArchive(Create)`. No template file needed — works for any user without uploading a template.

**Excel format** (引力引擎批量添加事件模板):
- **Columns A-E** (merged per event): 事件名 | 事件显示名 | 是否接收 | 触发时机 | 事件说明
- **Columns F-I** (one row per property): 属性名 | 属性显示名 | 属性类型 | 属性说明
- **Property types**: 文本, 整数, 浮点数, 布尔值, 日期, 时间, 列表
- A-E merged for multi-property events; F-I must NOT have merged cells
- Event names: English, letters/digits/underscore only, start with letter, ≤50 chars
- Property names: same rules, cannot start with `$`

See `references/gravity-engine-excel-format.md` for detailed column specs and XLSX internal XML structure.

## Key Files

- `scripts/GenerateTrackingExcel.cs` — C# Editor script template (supports both template-based and template-free modes)
- `references/gravity-engine-excel-format.md` — Excel template format reference + XLSX internal XML structure
- `references/tutorial-system-patterns.md` — 6 common Unity tutorial system patterns + identification flowchart + UI element naming reference
- `references/editor-playmode-bypass.md` — Common Play Mode blocking points + bypass strategies + ObscuredTypes handling + force-satisfy conditions

## Notes

- When switching to a new Unity project, run through the identification flowchart in `references/tutorial-system-patterns.md` first
- Read `references/editor-playmode-bypass.md` before attempting Play Mode screenshots — most mobile games have network dependencies that block Editor
- The Excel generation script supports template-free mode — other users don't need to upload a 引力引擎 template
- `execute_csharp_script` (Roslyn) cannot access `Assembly-CSharp` types; always use Editor scripts with `[MenuItem]` for runtime operations
- ObscuredTypes (CodeStage AntiCheat Toolkit) are common in mobile games — see bypass reference for handling
- All outputs go to `Tracking/` folder — keep everything in one place
