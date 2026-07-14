#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using EnumTable;
using Table;
using Tutorial;
using UIContents.Tutorial;

public class TutorialScreenshotCapture : EditorWindow
{
    [MenuItem("Tools/Tracking/Reset Save Data (New Player)")]
    public static void ResetSaveData()
    {
        PlayerPrefs.DeleteKey("ZvsZ_GameData");
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Save data cleared. Re-enter Play Mode for fresh tutorial.");
    }

    [MenuItem("Tools/Tracking/Capture All Tutorial Screenshots")]
    public static void CaptureAllScreenshots()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Screenshot] Must be in Play Mode!");
            return;
        }
        EditorApplication.delayCall += () => CaptureAllRoutine().RunCoroutine();
    }

    static IEnumerator CaptureAllRoutine()
    {
        string outDir = "Tracking/screenshots";
        Directory.CreateDirectory(outDir);
        Debug.Log("[Screenshot] Starting full capture...");

        yield return new WaitForSeconds(2f);

        var tmType = FindType("TutorialManager");
        var pmType = FindType("PlayManager");
        var udmType = FindType("UserDataManager");

        int captured = 0;
        int lastCapturedTID = -1;
        int stuckCount = 0;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            // Force satisfy all conditions by setting StageClearCount, gold, box count, boss, etc.
            ForceSatisfyConditions(udmType, pmType);

            yield return new WaitForSeconds(1f);

            // Check if TutorialManager exists and has tutorials
            var instanceField = tmType?.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            object tm = instanceField?.GetValue(null);

            if (tm == null)
            {
                // TutorialManager not loaded — try to call StartScene to reload tutorials
                var startSceneMethod = tmType?.GetMethod("StartScene", BindingFlags.Public | BindingFlags.Static);
                if (startSceneMethod != null)
                {
                    var sceneType = FindType("EnumTable.eSceneType");
                    object lobbyVal = Enum.Parse(sceneType, "Stage");
                    try { startSceneMethod.Invoke(null, new object[] { true, lobbyVal }); }
                    catch { }
                }
                yield return new WaitForSeconds(2f);
                tm = instanceField?.GetValue(null);
                if (tm == null)
                {
                    Debug.Log($"[Screenshot] Attempt {attempt}: No TutorialManager. All tutorials may be done.");
                    break;
                }
            }

            // Check CurTID
            var curTIDField = tmType.GetField("CurTID", BindingFlags.Public | BindingFlags.Instance);
            int curTID = curTIDField != null ? (int)curTIDField.GetValue(tm) : 0;

            if (curTID == 0)
            {
                // Tutorial not active yet — wait for ManagerLateUpdate to pick it up
                yield return new WaitForSeconds(2f);
                curTID = curTIDField != null ? (int)curTIDField.GetValue(tm) : 0;
            }

            if (curTID > 0)
            {
                // If same TID as last time, count stuck attempts
                if (curTID == lastCapturedTID)
                {
                    stuckCount++;
                    if (stuckCount >= 3)
                    {
                        Debug.LogWarning($"[Screenshot] Tutorial {curTID} stuck 3x — skipping to next");
                        // Force clear this tutorial from the list
                        var onClearMethod = tmType.GetMethod("OnClearTutorials", BindingFlags.Public | BindingFlags.Instance);
                        if (onClearMethod != null) onClearMethod.Invoke(tm, null);
                        stuckCount = 0;
                        lastCapturedTID = -1;
                        continue;
                    }
                }
                else
                {
                    stuckCount = 0;
                }
                lastCapturedTID = curTID;

                // Tutorial is active — screenshot!
                string filename = $"p{captured + 1:00}_tutorial_{curTID}.png";
                string path = Path.Combine(outDir, filename);
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[Screenshot] Captured: {filename} (CurTID={curTID})");
                captured++;
                yield return new WaitForSeconds(1f);

                // Advance tutorial via UITutorial.OnClick
                var uiTutorialType = FindType("UITutorial");
                if (uiTutorialType != null)
                {
                    var tutorialUI = UnityEngine.Object.FindObjectOfType(uiTutorialType);
                    if (tutorialUI != null)
                    {
                        var onClickMethod = uiTutorialType.GetMethod("OnClick", BindingFlags.Public | BindingFlags.Instance);
                        if (onClickMethod != null)
                        {
                            onClickMethod.Invoke(tutorialUI, null);
                            Debug.Log($"[Screenshot] Advanced tutorial {curTID}");
                        }
                    }
                }

                // Wait for CurTID to change (either to next step or to 0)
                float waitTime = 0f;
                while (waitTime < 10f)
                {
                    yield return new WaitForSeconds(0.5f);
                    waitTime += 0.5f;
                    int newTID = curTIDField != null ? (int)curTIDField.GetValue(tm) : 0;
                    if (newTID != curTID)
                    {
                        Debug.Log($"[Screenshot] CurTID changed: {curTID} -> {newTID} (waited {waitTime}s)");
                        break;
                    }
                }
                if (waitTime >= 10f)
                    Debug.LogWarning($"[Screenshot] CurTID stuck at {curTID} after 10s");

                yield return new WaitForSeconds(1f);
            }
            else
            {
                // CurTID still 0 — condition not met.
                // Force conditions, then call StartScene to reload tutorials
                ForceSatisfyConditions(udmType, pmType);
                yield return new WaitForSeconds(1f);

                // Re-call StartScene to pick up any new tutorials that now meet conditions
                var startSceneMethod = tmType?.GetMethod("StartScene", BindingFlags.Public | BindingFlags.Static);
                if (startSceneMethod != null)
                {
                    var sceneType = FindType("EnumTable.eSceneType");
                    object lobbyVal = Enum.Parse(sceneType, "Stage");
                    try { startSceneMethod.Invoke(null, new object[] { true, lobbyVal }); }
                    catch (Exception ex) { Debug.LogWarning($"[Screenshot] StartScene failed: {ex.Message}"); }
                }
                yield return new WaitForSeconds(3f);

                // Check if TM is still loaded
                tm = instanceField?.GetValue(null);
                if (tm == null)
                {
                    Debug.Log("[Screenshot] TutorialManager destroyed — all tutorials complete!");
                    break;
                }

                // Re-check CurTID
                curTID = curTIDField != null ? (int)curTIDField.GetValue(tm) : 0;
                if (curTID > 0)
                {
                    // Tutorial activated after force — capture it in next iteration
                    Debug.Log($"[Screenshot] Tutorial {curTID} activated after force!");
                    continue;
                }

                // Still 0 — wait more
                yield return new WaitForSeconds(2f);
            }
        }

        Debug.Log($"[Screenshot] Done! {captured} screenshots saved to {outDir}/");
        AssetDatabase.Refresh();
    }

    static void ForceSatisfyConditions(Type udmType, Type pmType)
    {
        // Strong-typed approach — Editor assembly can access Assembly-CSharp

        // Set PlayManager gold
        if (PlayManager.Instance != null)
        {
            PlayManager.Instance.OwnGoldValue = 9999999f;

            // Set LastBarrackNumber
            var pd = PlayManager.Instance.PlayData;
            if (pd != null)
            {
                pd.LastBarrackNumber = 999;
            }
        }

        // Set StageClearCount and BossDifficulty via UserData
        var udm = UserDataManager.Instance;
        if (udm != null && udm.UserData != null)
        {
            // StageInfo
            var si = udm.UserData.StageInfo;
            if (si != null)
            {
                si.RealStageClearCount = 999;
                Debug.Log("[Force] StageClearCount=999");
            }

            // BossStageInfo
            var bi = udm.UserData.BossStageInfo;
            if (bi != null)
            {
                bi.RealDiffculty = 999;
                Debug.Log("[Force] BossDifficulty=999");
            }
        }

        // Force return to lobby if in battle
        if (PlayManager.Instance != null && !PlayManager.Instance.IsLobby)
        {
            SceneLoadManager.Instance.LoadScene(eSceneType.Stage, true, null);
            Debug.Log("[Force] LoadScene(Stage) called — returning to lobby");
        }
    }

    [MenuItem("Tools/Tracking/Advance Tutorial Step")]
    public static void AdvanceStep()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Tutorial] Must be in Play Mode!");
            return;
        }

        var uiTutorialType = FindType("UITutorial");
        if (uiTutorialType == null)
        {
            Debug.LogError("[Tutorial] UITutorial type not found!");
            return;
        }

        var tutorialUI = UnityEngine.Object.FindObjectOfType(uiTutorialType);
        if (tutorialUI == null)
        {
            Debug.LogWarning("[Tutorial] No active UITutorial in scene.");
            return;
        }

        var onClickMethod = uiTutorialType.GetMethod("OnClick", BindingFlags.Public | BindingFlags.Instance);
        if (onClickMethod != null)
        {
            onClickMethod.Invoke(tutorialUI, null);
            Debug.Log("[Tutorial] OnClick called — advanced to next step.");
        }
    }

    [MenuItem("Tools/Tracking/Force Satisfy Conditions")]
    public static void ForceConditionsMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Tutorial] Must be in Play Mode!");
            return;
        }
        var udmType = FindType("UserDataManager");
        var pmType = FindType("PlayManager");
        ForceSatisfyConditions(udmType, pmType);
        Debug.Log("[Tutorial] Conditions forced!");
    }

    [MenuItem("Tools/Tracking/Check Tutorial Status")]
    public static void CheckStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[Tutorial] Not in Play Mode.");
            return;
        }

        var tmType = FindType("TutorialManager");
        if (tmType == null)
        {
            Debug.Log("[Tutorial] TutorialManager type not found.");
            return;
        }

        var isLoadProp = tmType.GetProperty("IsLoad", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        bool isLoad = isLoadProp != null && (bool)isLoadProp.GetValue(null);
        Debug.Log($"[Tutorial] TutorialManager.IsLoad = {isLoad}");

        var instanceField = tmType.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        object tm = instanceField?.GetValue(null);
        Debug.Log($"[Tutorial] TutorialManager.Instance = {(tm != null ? "loaded" : "null")}");

        if (tm != null)
        {
            var curTIDField = tmType.GetField("CurTID", BindingFlags.Public | BindingFlags.Instance);
            int curTID = curTIDField != null ? (int)curTIDField.GetValue(tm) : 0;
            Debug.Log($"[Tutorial] CurTID = {curTID}");

            var datasProp = tmType.GetProperty("Datas");
            var datas = datasProp?.GetValue(tm) as IList;
            Debug.Log($"[Tutorial] Tutorial list count = {(datas != null ? datas.Count : 0)}");
        }

        var udmType = FindType("UserDataManager");
        var udmField = udmType?.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        object udm = udmField?.GetValue(null);
        Debug.Log($"[Tutorial] UserDataManager.Instance = {(udm != null ? "loaded" : "null")}");

        if (udm != null)
        {
            try
            {
                var clearProp = udmType.GetProperty("ClearTutorials");
                var clearList = clearProp?.GetValue(udm) as IList;
                Debug.Log($"[Tutorial] ClearTutorials count = {(clearList != null ? clearList.Count : "null")}");
            }
            catch (Exception e)
            {
                Debug.Log($"[Tutorial] ClearTutorials not accessible: {e.Message}");
            }
        }
    }

    static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.Name == typeName) return t;
            }
        }
        return null;
    }
}

public static class IEnumeratorExtensions
{
    public static void RunCoroutine(this IEnumerator coroutine)
    {
        EditorCoroutineHelper.Start(coroutine);
    }
}

public class EditorCoroutineHelper : MonoBehaviour
{
    static EditorCoroutineHelper instance;
    Queue<IEnumerator> coroutines = new Queue<IEnumerator>();

    public static void Start(IEnumerator coroutine)
    {
        if (instance == null)
        {
            var go = new GameObject("EditorCoroutineHelper");
            go.hideFlags = HideFlags.HideAndDontSave;
            instance = go.AddComponent<EditorCoroutineHelper>();
        }
        instance.coroutines.Enqueue(coroutine);
    }

    void Update()
    {
        if (coroutines.Count > 0)
        {
            var c = coroutines.Peek();
            if (!c.MoveNext())
                coroutines.Dequeue();
        }
    }
}
#endif
