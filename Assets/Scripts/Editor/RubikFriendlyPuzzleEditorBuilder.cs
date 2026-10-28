#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RubikFriendlyPuzzleEditorBuilder
{
    static RubikFriendlyPuzzleEditorBuilder()
    {
        EditorApplication.delayCall += AutoEnsureFriendlyPuzzleUIInEditor;
    }

    private static void AutoEnsureFriendlyPuzzleUIInEditor()
    {
        if (EditorSettings.enterPlayModeOptionsEnabled)
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureMainSceneInBuildSettings();

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
            return;

        RubikUIController uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl == null)
        {
            GameObject rm = GameObject.Find("RubikManager");
            if (rm == null) return;
            uiCtrl = rm.GetComponent<RubikUIController>();
            if (uiCtrl == null)
                uiCtrl = rm.AddComponent<RubikUIController>();
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.transform.Find("FriendlyPuzzleUI_v4") == null)
        {
            RubikFriendlyPuzzleUIBuilder.EnsureUI(uiCtrl, forceRebuild: true);
            EditorUtility.SetDirty(uiCtrl);
            if (uiCtrl.cameraOrbit != null) EditorUtility.SetDirty(uiCtrl.cameraOrbit);
            if (uiCtrl.cameraScanner != null) EditorUtility.SetDirty(uiCtrl.cameraScanner);
            if (uiCtrl.colorPicker != null) EditorUtility.SetDirty(uiCtrl.colorPicker);
            if (uiCtrl.solutionViewer != null) EditorUtility.SetDirty(uiCtrl.solutionViewer);
            if (canvas != null) EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=#2563EB><b>[RubikSolver3D] Đã tự động nâng cấp giao diện Friendly Puzzle v4 (thêm SolverLoadingModal bất đồng bộ) trên Scene!</b></color>");
        }
    }

    [MenuItem("Rubik/Thiết kế Giao diện Friendly Puzzle (Chuẩn 3 Ảnh Tham Chiếu)", priority = 1)]
    public static void BuildFriendlyPuzzleUIMenu()
    {
        EditorSettings.enterPlayModeOptionsEnabled = false;

        RubikUIController uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl == null)
        {
            GameObject rm = GameObject.Find("RubikManager");
            if (rm == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy RubikManager trong Scene hiện tại! Hãy mở Assets/Scenes/MainScene.unity.", "OK");
                return;
            }
            uiCtrl = rm.GetComponent<RubikUIController>();
            if (uiCtrl == null)
                uiCtrl = Undo.AddComponent<RubikUIController>(rm);
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Friendly Puzzle UI v3");

        RubikFriendlyPuzzleUIBuilder.EnsureUI(uiCtrl, forceRebuild: true);
        EnsureMainSceneInBuildSettings();

        EditorUtility.SetDirty(uiCtrl);
        if (uiCtrl.cameraOrbit != null) EditorUtility.SetDirty(uiCtrl.cameraOrbit);
        if (uiCtrl.cameraScanner != null) EditorUtility.SetDirty(uiCtrl.cameraScanner);
        if (uiCtrl.colorPicker != null) EditorUtility.SetDirty(uiCtrl.colorPicker);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#16A34A><b>[RubikSolver3D] Hoàn tất dựng và lưu giao diện Friendly Puzzle v3!</b></color>");
    }

    private static void EnsureMainSceneInBuildSettings()
    {
        const string mainScenePath = "Assets/Scenes/MainScene.unity";
        if (!System.IO.File.Exists(mainScenePath))
            return;

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == mainScenePath)
            {
                scenes[i].enabled = true;
                found = true;
                if (i != 0)
                {
                    var item = scenes[i];
                    scenes.RemoveAt(i);
                    scenes.Insert(0, item);
                }
                break;
            }
        }

        if (!found)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(mainScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
