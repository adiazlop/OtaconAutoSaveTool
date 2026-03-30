using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// I'm having random crashes with Unity while working on UVC, so I decided to implement a tool that handles autosaving.
// It loads an Otacon image from MGS2 to indicate that the process was completed correctly.

[InitializeOnLoad]
public class OtaconAutosaveTool : EditorWindow
{
    // Default variables
    private static float intervalMinutes = 5f;
    private static bool enabled = true;
    private static double nextSaveTime;
    
    // Image Texture
    private static Texture2D saveIcon;

    [MenuItem("Tools/Otacon Autosave Config")]
    public static void ShowWindow()
    {
        if (saveIcon == null) LoadIcon();
        GetWindow<OtaconAutosaveTool>("Otacon Autosave");
    }

    private static void LoadIcon()
    {
        saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/OtaconAutosaveTool/Image/Otacon.png");
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update -= Update; 
        EditorApplication.update += Update;
    }

    private void OnEnable()
    {
        if (saveIcon == null) LoadIcon();
        // Repaint the window frequently so the countdown looks smooth
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        // Stop repainting when window is closed to save resources
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        if (saveIcon != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(saveIcon, GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.Label("Autosave Configuration", EditorStyles.boldLabel);
        
        enabled = EditorGUILayout.Toggle("Enabled", enabled);

        // START CHANGE CHECK: Detect if the user modifies the interval
        EditorGUI.BeginChangeCheck();
        
        intervalMinutes = EditorGUILayout.FloatField("Interval (Minutes)", intervalMinutes);

        if (intervalMinutes < 0.5f) intervalMinutes = 0.5f;

        // IF CHANGED: Immediately recalculate the next save time
        if (EditorGUI.EndChangeCheck())
        {
            ResetTimer();
        }

        EditorGUILayout.Space();
        
        if (nextSaveTime <= 0) ResetTimer();

        double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
        
        // Safety: If the remaining time is negative (e.g. after a sleep/wake cycle), force 0
        int displaySeconds = (timeRemaining > 0) ? (int)timeRemaining : 0;
        GUILayout.Label($"Next save in: {displaySeconds} seconds");

        if (GUILayout.Button("Save Now"))
        {
            SaveProject();
        }
    }

    private static void Update()
    {
        if (!enabled || EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        if (nextSaveTime <= 0) ResetTimer();

        if (EditorApplication.timeSinceStartup > nextSaveTime)
        {
            // CRITICAL: Move timer forward first
            ResetTimer();
            SaveProject();
        }
    }

    private static void SaveProject()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        
        if (saveIcon == null) LoadIcon();

        if (HasOpenInstances<OtaconAutosaveTool>())
        {
            OtaconAutosaveTool window = GetWindow<OtaconAutosaveTool>("Otacon Autosave");
            window.ShowNotification(new GUIContent(" Project Saved", saveIcon), 2.0f);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(" Project Saved", saveIcon), 2.0f);
        }

        Debug.Log($"<color=green>Project saved automatically at {System.DateTime.Now:HH:mm:ss}</color>");
    }

    private static void ResetTimer()
    {
        // Re-calculates based on the current intervalMinutes value
        nextSaveTime = EditorApplication.timeSinceStartup + (intervalMinutes * 60);
    }
}