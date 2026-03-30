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

    [MenuItem("Tools/Autosave Config")]
    public static void ShowWindow()
    {
        // Try to load icon when opening the window if it doesn't exist yet
        if (saveIcon == null) LoadIcon();
        GetWindow<OtaconAutosaveTool>("Autosave");
    }

    // Load default icon
    private static void LoadIcon()
    {
        // Ensure this path exactly matches your project structure
        saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/OtaconAutosaveTool/Image/Otacon.png");
    }

    // ERROR FIX: Use InitializeOnLoadMethod instead of a static constructor 
    // to safely access EditorApplication.timeSinceStartup.
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // Runs safely once Unity has loaded the asset database
        EditorApplication.update -= Update; // Prevent duplicate registrations
        EditorApplication.update += Update;
    }

    private void OnEnable()
    {
        // Load the icon when the window is enabled
        if (saveIcon == null) LoadIcon();
    }

    private void OnGUI()
    {
        // Show icon on config screen
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
        intervalMinutes = EditorGUILayout.FloatField("Interval (Minutes)", intervalMinutes);

        // Clamping minimum interval to avoid infinite save loops
        if (intervalMinutes < 0.5f) intervalMinutes = 0.5f;

        EditorGUILayout.Space();
        
        // Lazy initialization of the timer
        if (nextSaveTime <= 0) ResetTimer();

        double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
        GUILayout.Label($"Next save in: {(timeRemaining > 0 ? (int)timeRemaining : 0)} seconds");

        if (GUILayout.Button("Save Now"))
        {
            SaveProject();
        }
    }

    private static void Update()
    {
        // PLAY MODE PROTECTION: Do not save while playing or paused to avoid data corruption
        if (!enabled || EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        // Initialize timer if it hasn't started
        if (nextSaveTime <= 0) ResetTimer();

        if (EditorApplication.timeSinceStartup > nextSaveTime)
        {
            // CRITICAL: We move the timer forward BEFORE saving to prevent a crash if the save takes too long
            ResetTimer();
            SaveProject();
        }
    }

    private static void SaveProject()
    {
        // Execute the save process
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        
        // Ensure the icon is loaded for the notification
        if (saveIcon == null) LoadIcon();

        // Check if the window is open to show notification there, otherwise use the Scene View
        if (HasOpenInstances<OtaconAutosaveTool>())
        {
            OtaconAutosaveTool window = GetWindow<OtaconAutosaveTool>("Autosave");
            window.ShowNotification(new GUIContent(" Project Saved", saveIcon), 2.0f);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            // Show popup in the Scene View if the window is closed
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(" Project Saved", saveIcon), 2.0f);
        }

        Debug.Log($"<color=green>Project saved automatically at {System.DateTime.Now:HH:mm:ss}</color>");
    }

    private static void ResetTimer()
    {
        // Reset the target time for the next save based on current startup time
        nextSaveTime = EditorApplication.timeSinceStartup + (intervalMinutes * 60);
    }
}