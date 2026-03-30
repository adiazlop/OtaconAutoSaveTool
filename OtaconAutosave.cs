using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

//I'm having random crashes with Unity on working on UVC, so I decided to implement a tool for Unity that handles autosaving for me
//It will load an Otacon image from MGS2 to show that the process was made correctly

[InitializeOnLoad]
public class OtaconAutosaveTool : EditorWindow
{
    // Default variables
    private static float intervalMinutes = 5f;
    private static bool enabled = true;
    
    // Image Texture
    private static Texture2D saveIcon;
    private static double nextSaveTime;

    [MenuItem("Tools/Autosave Config")]
    public static void ShowWindow()
    {
        // On windows load, try to load icon if does not exists
        if (saveIcon == null) LoadIcon();
        GetWindow<OtaconAutosaveTool>("Autosave");
    }

    // Load default icon
    private static void LoadIcon()
    {
        // If you want to customiza the app, you can change the route here and put another image
        saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/OtaconAutosaveTool/Image/Otacon.png");
    }

    static OtaconAutosaveTool()
    {
        EditorApplication.update += Update;
        ResetTimer();
    }

    void OnGUI()
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

        GUILayout.Label("Configuración de Auto-Guardado", EditorStyles.boldLabel);
        
        enabled = EditorGUILayout.Toggle("Activado", enabled);
        intervalMinutes = EditorGUILayout.FloatField("Intervalo (Minutos)", intervalMinutes);

        if (intervalMinutes < 0.5f) intervalMinutes = 0.5f;

        EditorGUILayout.Space();
        double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
        GUILayout.Label($"Próximo guardado en: {(timeRemaining > 0 ? (int)timeRemaining : 0)} segundos");

        if (GUILayout.Button("Guardar ahora"))
        {
            SaveProject();
        }
    }

    private static void Update()
    {
        if (!enabled || EditorApplication.isPlaying) return;

        if (EditorApplication.timeSinceStartup > nextSaveTime)
        {
            SaveProject();
        }
    }

    private static void SaveProject()
    {
        // Save 
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        
        // Visual notification
        if (saveIcon == null) LoadIcon(); // Makes sure we have the icon

        // Looks for the open window, if not looks for the visual notification
        EditorWindow windowToNotify = GetWindow<OtaconAutosaveTool>("Autosave", false);
        if (windowToNotify == null)
        {
            // Shows notification if windows is closed
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(" Proyecto Guardado", saveIcon), 2.0f);
        }
        else
        {
            // If it's open shows in window
            windowToNotify.ShowNotification(new GUIContent(" Proyecto Guardado", saveIcon), 2.0f);
        }

        Debug.Log($"<color=green>Proyecto guardado automáticamente a las {System.DateTime.Now:HH:mm:ss}</color>");
        ResetTimer();
    }

    private static void ResetTimer()
    {
        //Reset timer when the time interval is finished
        nextSaveTime = EditorApplication.timeSinceStartup + (intervalMinutes * 60);
    }
}
