using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public static class RubikNetColorPickerEditorBuilder
{
    [MenuItem("Rubik/Thiết kế giao diện Tô Màu 2D (NetColorPickerPanel)")]
    public static void BuildNetColorPickerUI()
    {
        // 1. Tìm Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas (RubikCanvas) trong Scene!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup NetColorPicker UI");
        int undoGroup = Undo.GetCurrentGroup();

        // 2. Tìm hoặc tạo NetColorPickerPanel
        Transform panelTr = canvas.transform.Find("NetColorPickerPanel");
        GameObject panelObj;

        if (panelTr == null)
        {
            panelObj = new GameObject("NetColorPickerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelObj, "Create NetColorPickerPanel");
            panelObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            panelObj = panelTr.gameObject;
            Undo.RecordObject(panelObj, "Modify NetColorPickerPanel");
        }

        // Cấu hình NetColorPickerPanel
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(700, 520);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color32(0x11, 0x18, 0x27, 250); // Xám đen đậm #111827

        // 3. Tiêu đề TitleText
        TextMeshProUGUI titleTMP = GetOrCreateTMP(panelObj.transform, "TitleText", new Vector2(0, 230), new Vector2(650, 40));
        titleTMP.text = "TÔ MÀU KHỐI RUBIK 2D";
        titleTMP.fontSize = 24;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color32(0xFB, 0xBF, 0x24, 255); // Vàng kim #FBBF24
        titleTMP.alignment = TextAlignmentOptions.Center;

        // 4. NetContainer (chứa lưới 54 ô vuông 2D)
        GameObject netContainerObj = GetOrCreateEmptyChild(panelObj.transform, "NetContainer", new Vector2(-50, 20), new Vector2(350, 350));

        // 5. Palette Container (Hàng chọn 6 màu)
        GameObject paletteObj = GetOrCreateEmptyChild(panelObj.transform, "PaletteContainer", new Vector2(235, 20), new Vector2(130, 280));
        VerticalLayoutGroup vlg = paletteObj.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = paletteObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        TextMeshProUGUI paletteLabel = GetOrCreateTMP(paletteObj.transform, "PaletteLabel", Vector2.zero, new Vector2(120, 25));
        paletteLabel.text = "<b>Bảng màu:</b>";
        paletteLabel.fontSize = 15;
        paletteLabel.color = Color.white;
        paletteLabel.alignment = TextAlignmentOptions.Center;

        Button btnWhite = GetOrCreateColorButton(paletteObj.transform, "BtnWhite", "Trắng", new Color(0.96f, 0.96f, 0.96f));
        Button btnYellow = GetOrCreateColorButton(paletteObj.transform, "BtnYellow", "Vàng", new Color(1.00f, 0.84f, 0.00f));
        Button btnGreen = GetOrCreateColorButton(paletteObj.transform, "BtnGreen", "Xanh lá", new Color(0.00f, 0.65f, 0.28f));
        Button btnBlue = GetOrCreateColorButton(paletteObj.transform, "BtnBlue", "Xanh dương", new Color(0.00f, 0.35f, 0.85f));
        Button btnRed = GetOrCreateColorButton(paletteObj.transform, "BtnRed", "Đỏ", new Color(0.85f, 0.12f, 0.15f));
        Button btnOrange = GetOrCreateColorButton(paletteObj.transform, "BtnOrange", "Cam", new Color(1.00f, 0.40f, 0.00f));

        // 6. Dòng đếm số lượng CountInfoText
        TextMeshProUGUI countInfoTMP = GetOrCreateTMP(panelObj.transform, "CountInfoText", new Vector2(0, -170), new Vector2(650, 30));
        countInfoTMP.fontSize = 15;
        countInfoTMP.color = new Color32(0x9C, 0xA3, 0xAF, 255); // Trắng xám
        countInfoTMP.alignment = TextAlignmentOptions.Center;
        countInfoTMP.text = "Trắng: 9/9 | Vàng: 9/9 | Xanh lá: 9/9 | Xanh dương: 9/9 | Đỏ: 9/9 | Cam: 9/9";

        // 7. Dòng thông báo StatusText
        TextMeshProUGUI statusTMP = GetOrCreateTMP(panelObj.transform, "StatusText", new Vector2(0, -200), new Vector2(650, 30));
        statusTMP.fontSize = 17;
        statusTMP.color = new Color32(0x10, 0xB9, 0x81, 255); // Xanh lá
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.text = "Khối Rubik 100% hợp lệ! Sẵn sàng đồng bộ lên 3D.";

        // 8. Hàng nút hành động dưới đáy
        GameObject actionsObj = GetOrCreateEmptyChild(panelObj.transform, "ActionsContainer", new Vector2(0, -238), new Vector2(650, 45));
        HorizontalLayoutGroup hlg = actionsObj.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = actionsObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        Button btnApply = GetOrCreateActionButton(actionsObj.transform, "BtnApplyToCube", "✔ Đồng bộ lên 3D", new Color(0.06f, 0.72f, 0.51f), 190);
        Button btnReset = GetOrCreateActionButton(actionsObj.transform, "BtnResetSolved", "Khôi phục Solved", new Color(0.29f, 0.33f, 0.39f), 160);
        Button btnClose = GetOrCreateActionButton(actionsObj.transform, "BtnClose", "✖ Đóng", new Color(0.94f, 0.27f, 0.27f), 100);

        // 9. Nút mở Tô Màu 2D trên BottomPanel
        Button btnColorPicker = SetupBottomPanelButton(canvas.transform);

        // 10. Gắn Component trên RubikManager
        GameObject rubikManager = GameObject.Find("RubikManager");
        if (rubikManager != null)
        {
            RubikNetColorPicker picker = rubikManager.GetComponent<RubikNetColorPicker>();
            if (picker == null)
            {
                picker = Undo.AddComponent<RubikNetColorPicker>(rubikManager);
            }

            Undo.RecordObject(picker, "Link RubikNetColorPicker References");
            picker.netPanel = panelObj;
            picker.netContainer = netContainerObj.transform;
            picker.statusText = statusTMP;
            picker.countInfoText = countInfoTMP;
            picker.btnApplyToCube = btnApply;
            picker.btnResetToSolved = btnReset;
            picker.btnClose = btnClose;

            picker.btnColorWhite = btnWhite;
            picker.btnColorYellow = btnYellow;
            picker.btnColorGreen = btnGreen;
            picker.btnColorBlue = btnBlue;
            picker.btnColorRed = btnRed;
            picker.btnColorOrange = btnOrange;

            EditorUtility.SetDirty(picker);
        }

        // 11. Gắn vào RubikUIController
        RubikUIController uiCtrl = canvas.GetComponent<RubikUIController>();
        if (uiCtrl == null) uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl != null && btnColorPicker != null)
        {
            Undo.RecordObject(uiCtrl, "Link BtnColorPicker to UI Controller");
            uiCtrl.btnColorPicker = btnColorPicker;
            EditorUtility.SetDirty(uiCtrl);
        }

        // Mặc định ẩn Panel khi khởi chạy
        panelObj.SetActive(false);
        EditorUtility.SetDirty(panelObj);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green><b>🎉 THÀNH CÔNG: Đã tạo giao diện NetColorPickerPanel và liên kết Inspector 100%!</b></color>");
        EditorUtility.DisplayDialog("Thành công", "Đã thiết kế xong NetColorPickerPanel, nút 'Tô màu 2D' và liên kết toàn bộ Inspector thành công!", "Tuyệt vời");
    }

    private static Button SetupBottomPanelButton(Transform canvasTr)
    {
        Transform bottomPanel = canvasTr.Find("BottomPanel");
        if (bottomPanel == null)
        {
            var all = canvasTr.GetComponentsInChildren<RectTransform>(true);
            foreach (var r in all)
            {
                if (r.name == "BottomPanel") { bottomPanel = r; break; }
            }
        }

        if (bottomPanel == null) return null;

        Transform existingBtn = bottomPanel.Find("BtnColorPicker");
        GameObject btnObj;

        if (existingBtn != null)
        {
            btnObj = existingBtn.gameObject;
        }
        else
        {
            Transform template = bottomPanel.Find("BtnAlgorithm");
            if (template == null) template = bottomPanel.Find("BtnSolve");
            if (template == null) template = bottomPanel.Find("BtnReset");

            if (template != null)
            {
                btnObj = Object.Instantiate(template.gameObject, bottomPanel, false);
                btnObj.name = "BtnColorPicker";
                Undo.RegisterCreatedObjectUndo(btnObj, "Create BtnColorPicker");
            }
            else
            {
                btnObj = new GameObject("BtnColorPicker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(btnObj, "Create BtnColorPicker");
                btnObj.transform.SetParent(bottomPanel, false);
            }
        }

        TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = "Tô màu 2D";
        }

        Image img = btnObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.55f, 0.36f, 0.96f); // Tím #8B5CF6
        }

        return btnObj.GetComponent<Button>();
    }

    private static TextMeshProUGUI GetOrCreateTMP(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            obj.transform.SetParent(parent, false);
        }
        else
        {
            obj = tr.gameObject;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return obj.GetComponent<TextMeshProUGUI>();
    }

    private static GameObject GetOrCreateEmptyChild(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            obj.transform.SetParent(parent, false);
        }
        else
        {
            obj = tr.gameObject;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return obj;
    }

    private static Button GetOrCreateColorButton(Transform parent, string name, string label, Color color)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            obj.transform.SetParent(parent, false);
        }
        else
        {
            obj = tr.gameObject;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110, 36);

        Image img = obj.GetComponent<Image>();
        img.color = color;

        Button btn = obj.GetComponent<Button>();

        TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            tmp = textObj.GetComponent<TextMeshProUGUI>();
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        tmp.text = $"<b>{label}</b>";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = (color == Color.white || color.r > 0.8f && color.g > 0.8f) ? Color.black : Color.white;

        return btn;
    }

    private static Button GetOrCreateActionButton(Transform parent, string name, string label, Color color, float width)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            obj.transform.SetParent(parent, false);
        }
        else
        {
            obj = tr.gameObject;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 40);

        Image img = obj.GetComponent<Image>();
        img.color = color;

        Button btn = obj.GetComponent<Button>();

        TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            tmp = textObj.GetComponent<TextMeshProUGUI>();
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        tmp.text = $"<b>{label}</b>";
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }
}
