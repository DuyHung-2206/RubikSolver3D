#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RubikCameraScannerEditorBuilder
{
    [MenuItem("Rubik/Thiết kế giao diện Quét Camera (CameraScannerPanel)")]
    public static void BuildCameraScannerUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas (RubikCanvas) trong Scene!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup CameraScanner UI Friendly Puzzle");

        RubikUIController uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        RubikFriendlyPuzzleUIBuilder.BuildCameraScannerModal(canvas.transform, uiCtrl);

        if (uiCtrl != null)
        {
            uiCtrl.BindAllUIEvents();
            if (uiCtrl.cameraScanner != null)
            {
                uiCtrl.cameraScanner.BindScannerUIAndEvents();
                EditorUtility.SetDirty(uiCtrl.cameraScanner);
            }
            EditorUtility.SetDirty(uiCtrl);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#16A34A><b>[RubikSolver3D] Đã nâng cấp giao diện CameraScannerPanel theo phong cách Friendly Puzzle!</b></color>");
    }
}
#endif
