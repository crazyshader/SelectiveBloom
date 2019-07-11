using UnityEngine;
using UnityEditor;

public class SyncSceneCamera
{
    private const float _persSize = 0.01f; 
    private const float _orthoScaleBias = 2.0f; 
    private const string _menuPathFromMainCamera = "Tools/SyncSceneCamera/FromMainCamera";
    private const string _menuPathToMainCamera = "Tools/SyncSceneCamera/ToMainCamera";
    private static bool _isRuntimeInitialized = false;


    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        Debug.Log("InitializeOnLoad");
        Menu.SetChecked(_menuPathFromMainCamera, EditorPrefs.GetBool(_menuPathFromMainCamera));
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        EditorApplication.update += OnUpdate;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInitializeOnLoad()
    {
        Debug.Log("RuntimeInitializeOnLoad");
        _isRuntimeInitialized = false;
    }

    //将场景视图相机同步到游戏视图相机
    [MenuItem(_menuPathFromMainCamera)]
    private static void SyncFromMainCamera()
    {
        ChangeSyncFromMainCamera();
        if (Menu.GetChecked(_menuPathToMainCamera) == true)
            ChangeSyncToMainCamera();
    }

    private static void ChangeSyncFromMainCamera()
    {
        if (EditorApplication.isPlaying == true)
            _isRuntimeInitialized = true;

        var isSync = (Menu.GetChecked(_menuPathFromMainCamera) == true) ? false : true;
        Menu.SetChecked(_menuPathFromMainCamera, isSync);
        EditorPrefs.SetBool(_menuPathFromMainCamera, isSync);

        if (isSync == false)
        {
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.orthographic = false;
            sceneView.LookAtDirect(Camera.main.transform.position, Camera.main.transform.rotation, _persSize);
        }
    }

    //将游戏视图相机同步到场景视图相机
    [MenuItem(_menuPathToMainCamera)]
    private static void SyncToMainCamera()
    {
        ChangeSyncToMainCamera();
        if (Menu.GetChecked(_menuPathFromMainCamera) == true)
            ChangeSyncFromMainCamera();
    }

    private static void ChangeSyncToMainCamera()
    {
        var isSync = (Menu.GetChecked(_menuPathToMainCamera) == true) ? false : true;
        Menu.SetChecked(_menuPathToMainCamera, isSync);
        EditorPrefs.SetBool(_menuPathToMainCamera, isSync);
    }

    private static void OnUpdate()
    {
        var isSyncFromMain = Menu.GetChecked(_menuPathFromMainCamera);
        if (EditorApplication.isPlaying == true && isSyncFromMain == true)
        {
            if (_isRuntimeInitialized == false)
            {
                EditorApplication.ExecuteMenuItem(_menuPathFromMainCamera);
                Debug.Log("ExecuteMenuItem");
            }
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        var isSyncFromMain = Menu.GetChecked(_menuPathFromMainCamera);
        var isSyncToMain = Menu.GetChecked(_menuPathToMainCamera);

        if (isSyncFromMain == true)
        {
            sceneView.in2DMode = false;
            sceneView.orthographic = Camera.main.orthographic;
            sceneView.camera.fieldOfView = Camera.main.fieldOfView;
            var size = (sceneView.orthographic == true) ? Camera.main.orthographicSize * _orthoScaleBias : _persSize;
            sceneView.LookAtDirect(Camera.main.transform.position, Camera.main.transform.rotation, size);
        }
        else if (isSyncToMain == true)
        {
            Camera.main.transform.position = sceneView.camera.transform.position;
            Camera.main.transform.rotation = sceneView.camera.transform.rotation;
        }
    }
}