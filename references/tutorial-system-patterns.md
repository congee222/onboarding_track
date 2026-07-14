# Common Unity Tutorial System Patterns

When discovering a tutorial system, match against these patterns to quickly identify the architecture.

## Pattern 1: Data-Table-Driven (CSV/JSON → ScriptableObject)

Most common in Korean/Chinese mobile games.

**Indicators**:
- A CSV/JSON file in `CsvTable/`, `DataTable/`, or `Resources/` with "tutorial" in the name
- An auto-generated C# class matching the table columns
- A ScriptableObject `.asset` file holding runtime data
- A central manager with `InitXxx()` that loads the table

**Structure**:
```
CsvTable/Normal/tutorialScriptTable.csv  →  ScriptableObject asset  →  TutorialMgr reads at runtime
```

**How to extract**:
- Read the CSV header row for column→field mapping
- Each row = one tutorial step with: ID, type, trigger, target, gesture, wait condition
- Group by ID to identify tutorial phases
- Look for `NoticeXxxEvent` / `AdvanceStep` methods in the manager
- **Read the `.asset` file** (YAML) — ObscuredTypes have `fakeValue` showing actual values
- **Check `IsLobby` property** — determines if tutorial triggers in lobby or battle
- **Check `ConditionType` / `ConditionValue`** — determines when tutorial activates

**Examples**: `tutorialScriptTable.csv` with columns like `TutID, type, StartType, Finger, Target, WaitType`

### ObscuredTypes in Data Tables

If the project uses CodeStage AntiCheat Toolkit, the `.asset` file will have fields like:
```yaml
_ID:
  currentCryptoKey: 1805780620
  hiddenValue: 1805780325
  fakeValue: 1001       # ← This is the actual value
  fakeValueActive: 1
```

Read `fakeValue` to get actual values without runtime decryption.

---

## Pattern 2: Enum/State-Machine-Driven

Common in indie and mid-size projects.

**Indicators**:
- An enum like `TutorialStep`, `TutorialState`, `GuideStep`, `TutorialPhase`
- A manager class with a `switch` statement on the current step
- No external data file; steps are hardcoded
- Methods like `NextStep()`, `GoToStep()`, `CompleteStep()`

**How to extract**:
- Read the enum for all possible steps
- Read the switch statement to understand each step's behavior
- Look for UI element references (buttons, panels) activated per step
- Search for analytics/logging calls already present

---

## Pattern 3: Unity Timeline / Cinemachine-Driven

Common in 3D games and story-heavy tutorials.

**Indicators**:
- `.playable` or `.timeline` assets in a Tutorial folder
- `PlayableDirector` component on a GameObject
- `SignalReceiver` components for timeline-triggered events

**How to extract**:
- Read timeline tracks: each track = one aspect (animation, UI, audio, signal)
- SignalEmitter markers indicate interactive moments
- Look for `SignalReceiver` on UI objects for callback methods

---

## Pattern 4: Event/String-ID-Driven

Common in projects using Unity's official Tutorial Framework or custom event systems.

**Indicators**:
- String-based event IDs like `"tutorial.step.1"`, `"guide.tap.button"`
- An event bus / message system

**How to extract**:
- Search for tutorial-related event ID strings
- Trace event publishers and subscribers
- Map event IDs to player actions

---

## Pattern 5: Visual Scripting (Bolt/Unity Graph)

**Indicators**:
- `.asset` files with ScriptGraph or StateGraph type
- `ScriptMachine` or `StateMachine` components on GameObjects

**How to extract**:
- Inspect GameObjects with ScriptMachine components
- State nodes = tutorial steps; transition arrows = advancement conditions

---

## Pattern 6: Prefab-Sequence-Driven

**Indicators**:
- Numbered prefabs: `TutorialStep_01.prefab`, `TutorialStep_02.prefab`
- A manager that instantiates prefabs in sequence

**How to extract**:
- List all tutorial step prefabs (glob `*Tutorial*Step*.prefab`)
- Read each prefab's root component
- Look for completion callbacks

---

## Quick Identification Flowchart

```
Does the project have a CSV/JSON table with "tutorial" in the name?
  YES → Pattern 1 (Data-Table-Driven)
  NO ↓

Does the project have a TutorialMgr with enum-based steps and a switch statement?
  YES → Pattern 2 (Enum/State-Machine)
  NO ↓

Does the project have .playable/.timeline files in a Tutorial folder?
  YES → Pattern 3 (Timeline-Driven)
  NO ↓

Does the project use string-based event IDs for tutorial flow?
  YES → Pattern 4 (Event/String-ID)
  NO ↓

Does the project use Unity Visual Scripting graphs?
  YES → Pattern 5 (Visual Scripting)
  NO ↓

Does the project have numbered tutorial prefabs?
  YES → Pattern 6 (Prefab-Sequence)
  NO → Custom: read the code manually
```

## Common UI Element Patterns for Screenshots

| Element | Common Names | Purpose |
|---------|-------------|---------|
| Finger/Hand guide | `tutorial_hand`, `finger`, `pointer`, `gesture`, `_imgTouch_01/02/03` | Animated finger showing tap/hold/swipe |
| Dim/Overlay | `dim`, `mask`, `darken`, `highlight` | Darkens screen except target |
| Dialog box | `textBox`, `dialog`, `speech`, `bubble`, `tutorial_popup` | Character speech or instruction text |
| Arrow | `arrow`, `indicator`, `guide_arrow` | Points to target |
| Skip button | `skip`, `bt_skip`, `skipButton` | Skip tutorial option |
| Highlight mask | `spotlight`, `cutout`, `focus` | Hole-in-dark overlay on target |
| Move rect | `_moveRect` | Follows target UI element position |

## Tutorial Condition Types Reference

Common condition types found in Data-Table-Driven systems:

| Condition | Meaning | Force-Satisfy Method |
|-----------|---------|---------------------|
| `NONE` | Always true | N/A |
| `NEXT` | Chain step (triggered by previous) | N/A |
| `ENERGY` | Energy value >= N | `PlayData.CurValue = 999` |
| `OWN_GOLD` | Gold >= N | `PlayManager.OwnGoldValue = 9999999f` |
| `BARRACK` | Barrack number >= N | `PlayData.LastBarrackNumber = 999` |
| `BOX_COUNT` | Box count >= N | Add dummy boxes |
| `BOSS_CLEAR` | Boss difficulty >= N | `BossStageInfo.RealDiffculty = 999` |
| `STAGE_CLEAR` | Stage clear count >= N | `StageInfo.RealStageClearCount = 999` |

See `editor-playmode-bypass.md` for ObscuredTypes handling when force-setting these values.
