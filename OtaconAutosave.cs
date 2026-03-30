using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// I'm having random crashes with Unity on working on UVC, so I decided to implement a tool for Unity that handles autosaving for me
// It will load an Otacon image from MGS2 to show that the process was made correctly

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
        // Al abrir la ventana, intentamos cargar el icono si no existe
        if (saveIcon == null) LoadIcon();
        GetWindow<OtaconAutosaveTool>("Autosave");
    }

    // Load default icon
    private static void LoadIcon()
    {
        // Asegúrate de que esta ruta coincida exactamente con la de tu proyecto
        saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/OtaconAutosaveTool/Image/Otacon.png");
    }

    // 1. SOLUCIÓN AL ERROR: Usamos InitializeOnLoadMethod en lugar del constructor estático
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // Esto se ejecuta de forma segura una vez que Unity ha cargado la base de datos de assets
        EditorApplication.update -= Update; // Evitamos duplicados
        EditorApplication.update += Update;
        
        // No llamamos a ResetTimer aquí directamente para evitar el error de ScriptableObject al arrancar
    }

    void OnEnable()
    {
        // Cargamos el icono al habilitar la ventana
        if (saveIcon == null) LoadIcon();
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
        
        // Inicialización tardía del timer si es necesario
        if (nextSaveTime <= 0) ResetTimer();

        double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
        GUILayout.Label($"Próximo guardado en: {(timeRemaining > 0 ? (int)timeRemaining : 0)} segundos");

        if (GUILayout.Button("Guardar ahora"))
        {
            SaveProject();
        }
    }

    private static void Update()
    {
        // 2. BLOQUEO EN PLAY MODE: No guardar mientras jugamos
        if (!enabled || EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        // Inicialización del timer por si acaso
        if (nextSaveTime <= 0) ResetTimer();

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
        if (saveIcon == null) LoadIcon();

        // Buscamos si la ventana está abierta para mostrar la notificación allí
        if (HasOpenInstances<OtaconAutosaveTool>())
        {
            OtaconAutosaveTool window = GetWindow<OtaconAutosaveTool>("Autosave");
            window.ShowNotification(new GUIContent(" Proyecto Guardado", saveIcon), 2.0f);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            // Si la ventana está cerrada, mostramos el popup en la vista de Escena
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(" Proyecto Guardado", saveIcon), 2.0f);
        }

        Debug.Log($"<color=green>Proyecto guardado automáticamente a las {System.DateTime.Now:HH:mm:ss}</color>");
        ResetTimer();
    }

    private static void ResetTimer()
    {
        nextSaveTime = EditorApplication.timeSinceStartup + (intervalMinutes * 60);
    }
}