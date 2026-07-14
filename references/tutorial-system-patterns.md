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

**Examples**: `tutorialScriptTable.csv` with columns like `TutID, type, StartType, Finger, Target, WaitType`

---

## Pattern 2: Enum/State-Machine-Driven

Common in indie and mid-size projects.

**Indicators**:
- An enum like `TutorialStep`, `TutorialState`, `GuideStep`, `TutorialPhase`
- A manager class with a `switch` statement on the current step
- No external data file; steps are hardcoded
- Methods like `NextStep()`, `GoToStep()`, `CompleteStep()`

**Structure**:
```
TutorialManager.cs:
  enum TutorialStep { Intro, MoveCamera, TapButton, Complete }
  TutorialStep currentStep;
  void NextStep() { currentStep++; switch(currentStep) { ... } }
```

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
- Cinemachine virtual cameras named like `Cam_Tutorial_01`

**Structure**:
```
Timeline/Tutorial_01.playable  →  PlayableDirector plays  →  Signals trigger UI popups
```

**How to extract**:
- Read timeline tracks: each track = one aspect (animation, UI, audio, signal)
- SignalEmitter markers indicate interactive moments
- Look for `SignalReceiver` on UI objects for callback methods
- Check for `PlayableDirector.stopped` callback for timeline completion

---

## Pattern 4: Event/String-ID-Driven

Common in projects using Unity's official Tutorial Framework or custom event systems.

**Indicators**:
- String-based event IDs like `"tutorial.step.1"`, `"guide.tap.button"`
- An event bus / message system (`EventBus`, `EventManager`, `UnityEvent`)
- No central manager; tutorial logic scattered across UI controllers
- Possibly uses Unity's `UnityEngine.Tutorials` namespace

**Structure**:
```
TutorialController listens to EventBus:
  EventBus.On("tutorial.start") → show popup
  EventBus.On("button.tapped") → advance to next
```

**How to extract**:
- Search for tutorial-related event ID strings
- Trace event publishers (who fires "tutorial.xxx") and subscribers (who listens)
- Map event IDs to player actions
- Look for a tutorial flow config file (JSON/XML) listing event sequences

---

## Pattern 5: Visual Scripting (Bolt/Unity Graph)

Common in projects using Unity Visual Scripting.

**Indicators**:
- `.asset` files with ScriptGraph or StateGraph type
- `ScriptMachine` or `StateMachine` components on GameObjects
- Variables named `tutorialStep`, `currentGuide`, `isTutorialActive`

**Structure**:
```
Assets/VisualScripting/TutorialFlow.asset  →  StateMachine on TutorialManager GO
```

**How to extract**:
- Use `unity_editor` to inspect GameObjects with `ScriptMachine`/`StateMachine` components
- Read the graph assets via `UnityGraphs` API or manual XML inspection
- State nodes = tutorial steps; transition arrows = advancement conditions
- Trigger nodes (`OnButtonClicked`, `CustomEvent`) = interactive moments

---

## Pattern 6: Prefab-Sequence-Driven

Common in casual/hyper-casual games.

**Indicators**:
- Numbered prefabs: `TutorialStep_01.prefab`, `TutorialStep_02.prefab`
- A manager that instantiates prefabs in sequence
- Each prefab contains its own UI + logic (finger, highlight, dialog)
- Manager destroys current prefab and instantiates next on completion

**Structure**:
```
Prefabs/Tutorial/Step_01.prefab  →  Instantiate →  StepController.OnComplete →  Destroy →  Step_02
```

**How to extract**:
- List all tutorial step prefabs (glob `*Tutorial*Step*.prefab`)
- Read each prefab's root component (usually a `TutorialStepBase` or similar)
- Look for completion callbacks (`OnComplete`, `Advance`, `Next`)
- Check for a config file listing prefab order

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

Regardless of system pattern, tutorial UI typically uses these elements:

| Element | Common Names | Purpose |
|---------|-------------|---------|
| Finger/Hand guide | `tutorial_hand`, `finger`, `pointer`, `gesture` | Animated finger showing tap/hold/swipe |
| Dim/Overlay | `dim`, `mask`, `darken`, `highlight` | Darkens screen except target |
| Dialog box | `textBox`, `dialog`, `speech`, `bubble`, `tutorial_popup` | Character speech or instruction text |
| Arrow | `arrow`, `indicator`, `guide_arrow` | Points to target |
| Skip button | `skip`, `bt_skip`, `skipButton` | Skip tutorial option |
| Highlight mask | `spotlight`, `cutout`, `focus` | Hole-in-dark overlay on target |

Search for these names in the scene hierarchy to locate tutorial UI components for screenshot staging.
