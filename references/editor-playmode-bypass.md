# Editor Play Mode Bypass Strategies

Many mobile games have network dependencies (Firebase, Ads SDKs, GDPR consent, etc.) that block Unity Editor Play Mode. This document covers common blocking points and how to bypass them for tutorial screenshot capture.

## Quick Diagnosis

Enter Play Mode and check the Game View:
- **Stuck on loading screen** → Firebase RemoteConfig or network init blocking
- **GDPR consent dialog** → Need to click "Continue/Confirm"
- **Intro cutscene** → Need to click "Skip"
- **Black screen** → Asset loading blocked by network dependency

## Common Blocking Points

### 1. Firebase RemoteConfig

**Symptom**: Game stuck on loading, `UpdateRemoteConfig` never completes.

**Fix**: Add `#if UNITY_EDITOR` guard in `MyFirebase.UpdateRemoteConfig()`:
```csharp
public async Task UpdateRemoteConfig(Action done)
{
    IsConnectRemoteConfig = false;
    if (!Utility.IsConnectNetwork(false))
    {
        // ...
    }
    else
    {
#if UNITY_EDITOR
        // Skip Firebase network calls in Editor
        IsConnectRemoteConfig = true;
        done?.Invoke();
        return;
#else
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        // ... original Firebase code ...
#endif
    }
}
```

**Note**: `TestUpdate()` may provide mock data — but it may run before `UserDataManager` is initialized, causing NRE. Safer to just skip and let the game use defaults.

### 2. Core.ConnectNetwork()

**Symptom**: Game stuck after "Check Load_01" log, never proceeds to "Check Load_02".

**Fix**: Add `#if UNITY_EDITOR` guard to skip all network operations:
```csharp
public async void ConnectNetwork(bool isOpenUI, bool isVersion, Action done)
{
#if UNITY_EDITOR
    done?.Invoke();
    return;
#endif
    // ... original network code ...
}
```

### 3. GDPR / CTU Consent Dialog

**Symptom**: Consent popup appears, blocking game flow. Game stuck after "Check Load_02".

**Fix**: Either auto-click the confirm button, or skip the entire `EndLoad` flow:
```csharp
public override IEnumerator EndLoad(eSceneType _prevSceneType)
{
#if UNITY_EDITOR
    UserDataManager.Instance.OnNetwork();
    SceneLoadManager.Instance.LoadScene(eSceneType.Stage, false);
    yield break;
#endif
    // ... original EndLoad code (Ads init, Invite, Intro) ...
}
```

**Alternative**: Auto-click via `execute_csharp_script`:
```csharp
var buttons = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
foreach (var btn in buttons)
{
    if (btn.gameObject.name == "BtnOk" && btn.transform.parent?.parent?.name.Contains("CTU") == true)
    {
        btn.onClick.Invoke();
        break;
    }
}
```

### 4. LocalPushCtr Null Reference

**Symptom**: `EntrySceneLoader.Load()` crashes because `LocalPushCtr` is null (not yet initialized).

**Fix**: Skip push notification permission request:
```csharp
Core.Instance.ConnectNetwork(false, true, () => { isStop = false; });
#if !UNITY_EDITOR
yield return UserDataManager.Instance.LocalPushCtr.RequestNotificationPermission();
#endif
while (isStop)
    yield return null;
```

### 5. Intro Cutscenes

**Symptom**: Story cutscene plays, blocking access to game. "Skip" button needs clicking.

**Fix**: Auto-click via `execute_csharp_script`:
```csharp
var buttons = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
foreach (var btn in buttons)
{
    var name = btn.gameObject.name;
    if (name.Contains("跳过") || name.Contains("Skip") || name == "BG")
    {
        btn.onClick.Invoke();
        break;
    }
}
```

### 6. Ads SDK Initialization

**Symptom**: `EndLoad` waits for `AdsCtr.IsInit` which never completes in Editor.

**Fix**: Skip the wait loop in `#if UNITY_EDITOR` (see GDPR fix above — skip entire `EndLoad`).

## ObscuredTypes Handling (CodeStage AntiCheat Toolkit)

Many mobile games use [ObscuredTypes](https://codestage.net/uas_files/actk/) to prevent memory hacking. These types cannot be set via simple `FieldInfo.SetValue(int)`.

### Problem

```csharp
// This throws: Cannot convert Int32 to ObscuredInt
field.SetValue(obj, 999);
```

### Solution: Modify Internal Fields

ObscuredInt has these fields:
- `currentCryptoKey` (int) — encryption key
- `hiddenValue` (int) — encrypted value
- `fakeValue` (int) — visible value in Editor (what gets returned)
- `inited` (bool) — whether initialized
- `fakeValueActive` (bool) — whether to use fakeValue

**In Editor scripts** (Assembly-CSharp-Editor, can access Assembly-CSharp):
```csharp
// Direct strong-typed assignment — works because Editor assembly references Assembly-CSharp
si.RealStageClearCount = 999;  // ObscuredInt has implicit operator from int

// Or via reflection on an existing ObscuredInt instance:
var obscured = field.GetValue(parent);
var ot = obscured.GetType();
ot.GetField("hiddenValue", BindingFlags.Public | BindingFlags.Instance).SetValue(obscured, value);
ot.GetField("fakeValue", BindingFlags.Public | BindingFlags.Instance).SetValue(obscured, value);
ot.GetField("inited", BindingFlags.Public | BindingFlags.Instance).SetValue(obscured, true);
```

**In Roslyn scripts** (`execute_csharp_script`): Cannot access `Assembly-CSharp` types. Must use Editor scripts with `[MenuItem]` instead.

### Common ObscuredTypes

| Type | Fields | Notes |
|------|--------|-------|
| `ObscuredInt` | hiddenValue, fakeValue, inited | Most common for game state |
| `ObscuredShort` | Same structure | Used for Group_ID etc. |
| `ObscuredString` | hiddenChars, cryptoKey | Used for target paths |
| `ObscuredBool` | hiddenValue, fakeValue | Used for flags |

**Tip**: In the `.asset` file (YAML), ObscuredTypes have a `fakeValue` field that shows the actual value — useful for reading data without runtime decryption.

## Force-Satisfy Tutorial Conditions

Tutorial systems typically check conditions before activating steps. To capture screenshots of all steps, force-satisfy conditions:

### Common Conditions

| Condition Type | What to Set | Where |
|---------------|------------|-------|
| `STAGE_CLEAR >= N` | `UserData.StageInfo.RealStageClearCount = 999` | UserDataManager → UserData → StageInfo |
| `OWN_GOLD >= N` | `PlayManager.OwnGoldValue = 9999999f` | PlayManager (property, not ObscuredType) |
| `BOX_COUNT >= N` | Add dummy boxes to PlayerInfo | PlayManager → PlayerInfo → BoxInfos |
| `BOSS_CLEAR >= N` | `UserData.BossStageInfo.RealDiffculty = 999` | UserDataManager → UserData → BossStageInfo |
| `BARRACK >= N` | `PlayManager.PlayData.LastBarrackNumber = 999` | PlayManager → PlayData |
| `ENERGY >= N` | `PlayManager.PlayData.CurValue = 999` | PlayManager → PlayData |

### Re-trigger Tutorials After Forcing Conditions

After setting conditions, call `TutorialManager.StartScene(true, eSceneType.Stage)` to reload the tutorial list with new conditions.

### Reset Save Data

To start fresh (new player):
```csharp
PlayerPrefs.DeleteKey("GameDataKey");  // Find the key in PlayerPrefsExt or save system
PlayerPrefs.Save();
```

## Save Data Key Discovery

Search for the save data key:
```csharp
// Common patterns:
PlayerPrefsExt.GameData  // → "ZvsZ_GameData" etc.
PlayerPrefs.GetString("SaveKey")
ES3.Load("SaveKey")
```

## Auto-Capture Coroutine Architecture

The screenshot script should use a coroutine (via `EditorCoroutineHelper`) that:

1. **Force conditions** at the start of each iteration
2. **Check TutorialManager.Instance** — if null, call `StartScene` to reload
3. **Check CurTID** — if 0, wait for `ManagerLateUpdate` to activate a tutorial
4. **If CurTID > 0**: Screenshot → `OnClick()` → Wait for CurTID change
5. **Stuck detection**: If same TID appears 3 times, force-clear tutorial list
6. **Scene transitions**: Some tutorials require entering battle (click Start button) or returning to lobby (`LoadScene`)

### Key Timing

- Wait 1-2s after `ScreenCapture.CaptureScreenshot` for file write
- Wait 0.5s intervals when polling for `CurTID` change (timeout 10s)
- Wait 3s after `StartScene` reload for tutorials to activate
- Wait 2s after `LoadScene` for scene transition

## Scene-Aware Tutorial Activation

Tutorials often have `IsLobby` flag — they only activate in specific scenes:
- `IsLobby = true` → Only in lobby/preparation screen
- `IsLobby = false` → Only in battle

**To capture lobby tutorials**: Ensure game is in Stage scene (lobby), not in battle.
**To capture battle tutorials**: Click "Start" button to enter battle, then wait for tutorial to trigger.
