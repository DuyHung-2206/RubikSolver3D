#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RubikNetColorPickerEditorBuilder
{
    [MenuItem("Rubik/Thiết kế giao diện Tô Màu 2D (NetColorPickerPanel)")]
    public static void BuildNetColorPickerUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas (RubikCanvas) trong Scene!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup NetColorPicker UI Friendly Puzzle");

        RubikUIController uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        RubikFriendlyPuzzleUIBuilder.BuildNetColorPickerModal(canvas.transform, uiCtrl);

        if (uiCtrl != null)
        {
            uiCtrl.BindAllUIEvents();
            if (uiCtrl.colorPicker != null)
            {
                uiCtrl.colorPicker.BindNetPickerUIAndEvents();
                EditorUtility.SetDirty(uiCtrl.colorPicker);
            }
            EditorUtility.SetDirty(uiCtrl);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#16A34A><b>[RubikSolver3D] Đã nâng cấp giao diện NetColorPickerPanel theo phong cách Friendly Puzzle!</b></color>");
    }
}
#endif
