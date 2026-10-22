using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public static class RubikCameraScannerEditorBuilder
{
    [MenuItem("Rubik/Thiết kế giao diện Quét Camera (CameraScannerPanel)")]
    public static void BuildCameraScannerUI()
    {
        // 1. Tìm Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas (RubikCanvas) trong Scene!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup CameraScanner UI");
        int undoGroup = Undo.GetCurrentGroup();

        // 2. Tìm hoặc tạo CameraScannerPanel
        Transform panelTr = canvas.transform.Find("CameraScannerPanel");
        GameObject panelObj;

        if (panelTr == null)
        {
            panelObj = new GameObject("CameraScannerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelObj, "Create CameraScannerPanel");
            panelObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            panelObj = panelTr.gameObject;
            Undo.RecordObject(panelObj, "Modify CameraScannerPanel");
        }

        // Cấu hình Panel (780 x 570)
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(780, 570);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color32(0x0F, 0x17, 0x2A, 252); // Xám đen Slate sâu #0F172A

        // 3. Header Container
        GameObject headerObj = GetOrCreateEmptyChild(panelObj.transform, "Header", new Vector2(0, 255), new Vector2(740, 40));
        TextMeshProUGUI titleTMP = GetOrCreateTMP(headerObj.transform, "TitleText", new Vector2(-20, 0), new Vector2(600, 36));
        titleTMP.text = "📷 QUÉT KHỐI RUBIK QUA CAMERA";
        titleTMP.fontSize = 22;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color32(0x06, 0xB6, 0xD4, 255); // Cyan #06B6D4
        titleTMP.alignment = TextAlignmentOptions.Left;

        Button btnClose = GetOrCreateActionButton(headerObj.transform, "BtnClose", "✖", new Color(0.35f, 0.40f, 0.48f), 38);
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchoredPosition = new Vector2(350, 0);
        closeRT.sizeDelta = new Vector2(36, 36);

        // 4. Khối bên trái: LeftContainer (Camera View)
        GameObject leftContainer = GetOrCreateEmptyChild(panelObj.transform, "LeftContainer", new Vector2(-190, -15), new Vector2(360, 470));

        // Khung Camera Feed
        GameObject cameraFeedObj = GetOrCreateEmptyChild(leftContainer.transform, "CameraFeed", new Vector2(0, 45), new Vector2(330, 330));
        RawImage rawImg = cameraFeedObj.GetComponent<RawImage>();
        if (rawImg == null) rawImg = cameraFeedObj.AddComponent<RawImage>();
        rawImg.color = Color.white;

        // Overlay 3x3 Reticle
        GameObject reticleOverlay = GetOrCreateEmptyChild(cameraFeedObj.transform, "ReticleOverlay", Vector2.zero, new Vector2(330, 330));
        RectTransform reticleRT = reticleOverlay.GetComponent<RectTransform>();
        reticleRT.anchorMin = Vector2.zero;
        reticleRT.anchorMax = Vector2.one;
        reticleRT.sizeDelta = Vector2.zero;

        // Sinh 9 ô ngắm Reticle_0 .. Reticle_8 với cự ly mặc định vừa vặn (56f) để quét từ xa thoải mái
        float defaultSpread = 56f;
        float defaultBoxSize = 46f;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                float xPos = (col - 1) * defaultSpread;
                float yPos = (1 - row) * defaultSpread;

                GameObject box = GetOrCreateEmptyChild(reticleOverlay.transform, $"Reticle_{index}", new Vector2(xPos, yPos), new Vector2(defaultBoxSize, defaultBoxSize));
                Image boxImg = box.GetComponent<Image>();
                if (boxImg == null) boxImg = box.AddComponent<Image>();
                boxImg.color = new Color(1f, 1f, 1f, 0.40f);

                // Tạo nhãn ký tự màu hiển thị thời gian thực (W, Y, G, B, R, O)
                TextMeshProUGUI boxLabel = GetOrCreateTMP(box.transform, "ColorLabel", Vector2.zero, new Vector2(40, 40));
                boxLabel.fontSize = 20;
                boxLabel.fontStyle = FontStyles.Bold;
                boxLabel.alignment = TextAlignmentOptions.Center;
                boxLabel.text = (index == 4) ? "?" : "";
                boxLabel.color = Color.white;

                // Tạo tâm ngắm nhỏ ở giữa
                GameObject dot = GetOrCreateEmptyChild(box.transform, "CenterDot", Vector2.zero, new Vector2(8, 8));
                Image dotImg = dot.GetComponent<Image>();
                if (dotImg == null) dotImg = dot.AddComponent<Image>();
                dotImg.color = (index == 4) ? Color.yellow : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        // Dòng nhãn cự ly ngắm (Xa / Gần)
        TextMeshProUGUI distanceLabelTMP = GetOrCreateTMP(leftContainer.transform, "DistanceLabelText", new Vector2(0, -125), new Vector2(330, 20));
        distanceLabelTMP.text = "Cự ly ngắm: <b>Vừa (Cầm tay 25-40cm)</b>";
        distanceLabelTMP.fontSize = 12;
        distanceLabelTMP.color = new Color32(0x38, 0xBD, 0xF8, 255);
        distanceLabelTMP.alignment = TextAlignmentOptions.Center;

        // Thanh trượt cự ly (SliderDistance)
        Slider sliderDist = GetOrCreateSlider(leftContainer.transform, "SliderDistance", new Vector2(0, -145), new Vector2(250, 16), 36f, 88f, 56f);

        // Hàng nút dịch chuyển vị trí khung ngắm (Nudge buttons: ◄, ▲, ▼, ►, Tâm)
        GameObject nudgeRow = GetOrCreateEmptyChild(leftContainer.transform, "NudgeRow", new Vector2(0, -170), new Vector2(260, 24));
        HorizontalLayoutGroup nudgeHlg = nudgeRow.GetComponent<HorizontalLayoutGroup>();
        if (nudgeHlg == null) nudgeHlg = nudgeRow.AddComponent<HorizontalLayoutGroup>();
        nudgeHlg.spacing = 6;
        nudgeHlg.childAlignment = TextAnchor.MiddleCenter;
        nudgeHlg.childControlWidth = false;
        nudgeHlg.childControlHeight = false;

        Button btnNudgeLeft = GetOrCreateActionButton(nudgeRow.transform, "BtnNudgeLeft", "◄", new Color(0.20f, 0.25f, 0.35f), 32);
        btnNudgeLeft.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 22);

        Button btnNudgeUp = GetOrCreateActionButton(nudgeRow.transform, "BtnNudgeUp", "▲", new Color(0.20f, 0.25f, 0.35f), 32);
        btnNudgeUp.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 22);

        Button btnNudgeDown = GetOrCreateActionButton(nudgeRow.transform, "BtnNudgeDown", "▼", new Color(0.20f, 0.25f, 0.35f), 32);
        btnNudgeDown.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 22);

        Button btnNudgeRight = GetOrCreateActionButton(nudgeRow.transform, "BtnNudgeRight", "►", new Color(0.20f, 0.25f, 0.35f), 32);
        btnNudgeRight.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 22);

        Button btnResetNudge = GetOrCreateActionButton(nudgeRow.transform, "BtnResetReticlePos", "Tâm", new Color(0.25f, 0.30f, 0.40f), 44);
        btnResetNudge.GetComponent<RectTransform>().sizeDelta = new Vector2(44, 22);

        // Hàng nút điều khiển camera (Đổi camera, Lật gương M, Lật dọc V)
        GameObject camControlsRow = GetOrCreateEmptyChild(leftContainer.transform, "CamControlsRow", new Vector2(0, -198), new Vector2(330, 26));
        HorizontalLayoutGroup camHlg = camControlsRow.GetComponent<HorizontalLayoutGroup>();
        if (camHlg == null) camHlg = camControlsRow.AddComponent<HorizontalLayoutGroup>();
        camHlg.spacing = 6;
        camHlg.childAlignment = TextAnchor.MiddleCenter;
        camHlg.childControlWidth = false;
        camHlg.childControlHeight = false;

        Button btnSwitch = GetOrCreateActionButton(camControlsRow.transform, "BtnSwitchCamera", "📷 Đổi Cam", new Color(0.20f, 0.25f, 0.35f), 95);
        btnSwitch.GetComponent<RectTransform>().sizeDelta = new Vector2(95, 26);

        Button btnFlipH = GetOrCreateActionButton(camControlsRow.transform, "BtnFlipHorizontal", "🪞 Gương (M)", new Color(0.12f, 0.45f, 0.35f), 115);
        btnFlipH.GetComponent<RectTransform>().sizeDelta = new Vector2(115, 26);

        Button btnFlipV = GetOrCreateActionButton(camControlsRow.transform, "BtnFlipVertical", "↕ Dọc (V)", new Color(0.20f, 0.25f, 0.35f), 85);
        btnFlipV.GetComponent<RectTransform>().sizeDelta = new Vector2(85, 26);

        // Dòng thông tin Camera
        TextMeshProUGUI cameraInfoTMP = GetOrCreateTMP(leftContainer.transform, "CameraInfoText", new Vector2(0, -222), new Vector2(330, 20));
        cameraInfoTMP.text = "Camera: Tự động nhận diện thiết bị";
        cameraInfoTMP.fontSize = 12;
        cameraInfoTMP.color = new Color32(0x94, 0xA3, 0xB8, 255);
        cameraInfoTMP.alignment = TextAlignmentOptions.Center;

        // 5. Khối bên phải: RightContainer (Hướng dẫn, Xem trước & Nút thao tác)
        GameObject rightContainer = GetOrCreateEmptyChild(panelObj.transform, "RightContainer", new Vector2(190, -15), new Vector2(360, 470));

        // Tiêu đề mặt đang quét
        TextMeshProUGUI faceTitleTMP = GetOrCreateTMP(rightContainer.transform, "FaceTitleText", new Vector2(0, 205), new Vector2(340, 30));
        faceTitleTMP.text = "MẶT 1/6: MẶT F (TRƯỚC)";
        faceTitleTMP.fontSize = 17;
        faceTitleTMP.fontStyle = FontStyles.Bold;
        faceTitleTMP.color = new Color32(0xFB, 0xBF, 0x24, 255); // Vàng kim #FBBF24
        faceTitleTMP.alignment = TextAlignmentOptions.Center;

        // Dòng hướng dẫn cách cầm
        TextMeshProUGUI faceGuideTMP = GetOrCreateTMP(rightContainer.transform, "FaceGuideText", new Vector2(0, 168), new Vector2(340, 38));
        faceGuideTMP.text = "1. Hướng mặt bất kỳ vào camera làm mặt TRƯỚC (F). Nhấn phím ENTER để chụp.";
        faceGuideTMP.fontSize = 13;
        faceGuideTMP.color = new Color32(0xCB, 0xD5, 0xE1, 255);
        faceGuideTMP.alignment = TextAlignmentOptions.Center;

        // Huy hiệu 6 mặt: FaceBadgesContainer (F, R, B, L, U, D)
        GameObject faceBadgesObj = GetOrCreateEmptyChild(rightContainer.transform, "FaceBadgesContainer", new Vector2(0, 126), new Vector2(320, 28));
        HorizontalLayoutGroup badgeHlg = faceBadgesObj.GetComponent<HorizontalLayoutGroup>();
        if (badgeHlg == null) badgeHlg = faceBadgesObj.AddComponent<HorizontalLayoutGroup>();
        badgeHlg.spacing = 8;
        badgeHlg.childAlignment = TextAnchor.MiddleCenter;
        badgeHlg.childControlWidth = false;
        badgeHlg.childControlHeight = false;

        string[] badgeNames = { "F", "R", "B", "L", "U", "D" };
        for (int i = 0; i < 6; i++)
        {
            GameObject badge = GetOrCreateEmptyChild(faceBadgesObj.transform, $"Badge_{badgeNames[i]}", Vector2.zero, new Vector2(42, 26));
            Image bImg = badge.GetComponent<Image>();
            if (bImg == null) bImg = badge.AddComponent<Image>();
            bImg.color = (i == 0) ? new Color(0.96f, 0.65f, 0.14f) : new Color(0.25f, 0.28f, 0.35f);

            TextMeshProUGUI bTxt = GetOrCreateTMP(badge.transform, "Text", Vector2.zero, new Vector2(40, 24));
            bTxt.text = $"<b>{badgeNames[i]}</b>";
            bTxt.fontSize = 13;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = Color.white;
        }

        // Khung xem trước PreviewGridContainer (130 x 130)
        GameObject previewGridObj = GetOrCreateEmptyChild(rightContainer.transform, "PreviewGridContainer", new Vector2(0, 36), new Vector2(130, 130));
        GridLayoutGroup grid = previewGridObj.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = previewGridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(38, 38);
        grid.spacing = new Vector2(4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        // Sinh 9 ô preview Cell_0 .. Cell_8
        for (int i = 0; i < 9; i++)
        {
            GameObject cell = GetOrCreateEmptyChild(previewGridObj.transform, $"Cell_{i}", Vector2.zero, new Vector2(38, 38));
            Image cellImg = cell.GetComponent<Image>();
            if (cellImg == null) cellImg = cell.AddComponent<Image>();
            cellImg.color = (i == 4) ? Color.white : new Color(0.85f, 0.85f, 0.85f);

            Button cellBtn = cell.GetComponent<Button>();
            if (cellBtn == null) cell.AddComponent<Button>();
        }

        // Nút Chụp mặt này (Hỗ trợ phím ENTER)
        Button btnCapture = GetOrCreateActionButton(rightContainer.transform, "BtnCaptureFace", "📷 CHỤP MẶT (ENTER)", new Color(0.06f, 0.72f, 0.51f), 260);
        RectTransform capRT = btnCapture.GetComponent<RectTransform>();
        capRT.anchoredPosition = new Vector2(0, -56);
        capRT.sizeDelta = new Vector2(260, 38);

        // Hàng nút điều hướng (Trước / Quét lại / Tiếp)
        GameObject navRow = GetOrCreateEmptyChild(rightContainer.transform, "NavRow", new Vector2(0, -98), new Vector2(320, 32));
        HorizontalLayoutGroup navHlg = navRow.GetComponent<HorizontalLayoutGroup>();
        if (navHlg == null) navHlg = navRow.AddComponent<HorizontalLayoutGroup>();
        navHlg.spacing = 8;
        navHlg.childAlignment = TextAnchor.MiddleCenter;
        navHlg.childControlWidth = false;
        navHlg.childControlHeight = false;

        Button btnPrev = GetOrCreateActionButton(navRow.transform, "BtnPrevFace", "◀ Trước", new Color(0.25f, 0.30f, 0.38f), 90);
        Button btnRescan = GetOrCreateActionButton(navRow.transform, "BtnRescanFace", "🔄 Quét lại", new Color(0.35f, 0.38f, 0.45f), 110);
        Button btnNext = GetOrCreateActionButton(navRow.transform, "BtnNextFace", "Tiếp ▶", new Color(0.25f, 0.30f, 0.38f), 90);

        // Các Checkbox Tùy chọn
        Toggle toggleCalibrate = GetOrCreateToggle(rightContainer.transform, "ToggleAutoCalibrate", "Tự động cân bằng ánh sáng (Auto-Calibrate)", new Vector2(0, -140), true);
        Toggle toggleCustomMat = GetOrCreateToggle(rightContainer.transform, "ToggleCustomMaterialSync", "Đồng bộ chất liệu màu thực tế (Custom Material)", new Vector2(0, -168), true);

        // Dòng trạng thái StatusText
        TextMeshProUGUI statusTMP = GetOrCreateTMP(rightContainer.transform, "StatusText", new Vector2(0, -198), new Vector2(340, 28));
        statusTMP.fontSize = 13;
        statusTMP.color = new Color32(0x38, 0xBD, 0xF8, 255); // Xanh nhạt
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.text = "Sẵn sàng quét. Hãy căn thẳng khối Rubik!";

        // Nút Đồng bộ lên 3D
        Button btnApply = GetOrCreateActionButton(rightContainer.transform, "BtnApplyToCube", "✔ ĐỒNG BỘ LÊN KHỐI 3D", new Color(0.08f, 0.65f, 0.92f), 290);
        RectTransform applyRT = btnApply.GetComponent<RectTransform>();
        applyRT.anchoredPosition = new Vector2(0, -238);
        applyRT.sizeDelta = new Vector2(290, 42);

        // 6. Tạo nút trên BottomPanel
        Button btnCameraBottom = SetupBottomPanelButton(canvas.transform);

        // 7. Gắn Component RubikCameraScanner vào RubikManager
        GameObject rubikManager = GameObject.Find("RubikManager");
        if (rubikManager != null)
        {
            RubikCameraScanner scanner = rubikManager.GetComponent<RubikCameraScanner>();
            if (scanner == null)
            {
                scanner = Undo.AddComponent<RubikCameraScanner>(rubikManager);
            }

            Undo.RecordObject(scanner, "Link RubikCameraScanner References");
            scanner.scannerPanel = panelObj;
            scanner.cameraFeedImage = rawImg;
            scanner.reticleContainer = reticleRT;
            scanner.faceTitleText = faceTitleTMP;
            scanner.faceGuideText = faceGuideTMP;
            scanner.statusText = statusTMP;
            scanner.cameraInfoText = cameraInfoTMP;
            scanner.previewGridContainer = previewGridObj.transform;
            scanner.faceBadgesContainer = faceBadgesObj.transform;
            scanner.btnCaptureFace = btnCapture;
            scanner.btnRescanFace = btnRescan;
            scanner.btnNextFace = btnNext;
            scanner.btnPrevFace = btnPrev;
            scanner.btnSwitchCamera = btnSwitch;
            scanner.btnFlipHorizontal = btnFlipH;
            scanner.btnFlipVertical = btnFlipV;
            scanner.flipHorizontal = true;
            scanner.flipVertical = false;
            scanner.btnApplyToCube = btnApply;
            scanner.btnClose = btnClose;
            scanner.toggleAutoCalibrate = toggleCalibrate;
            scanner.toggleCustomMaterialSync = toggleCustomMat;
            scanner.sliderDistance = sliderDist;
            scanner.distanceLabelText = distanceLabelTMP;
            scanner.reticleSpread = 56f;
            scanner.reticleBoxSize = 46f;
            scanner.reticleOffset = new Vector2(0f, -25f);
            scanner.btnNudgeUp = btnNudgeUp;
            scanner.btnNudgeDown = btnNudgeDown;
            scanner.btnNudgeLeft = btnNudgeLeft;
            scanner.btnNudgeRight = btnNudgeRight;
            scanner.btnResetReticlePos = btnResetNudge;

            EditorUtility.SetDirty(scanner);
        }

        // 8. Gắn vào RubikUIController
        RubikUIController uiCtrl = canvas.GetComponent<RubikUIController>();
        if (uiCtrl == null) uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl != null && btnCameraBottom != null)
        {
            Undo.RecordObject(uiCtrl, "Link BtnCameraScanner to UI Controller");
            uiCtrl.btnCameraScanner = btnCameraBottom;
            EditorUtility.SetDirty(uiCtrl);
        }

        // Mặc định ẩn Panel khi khởi chạy
        panelObj.SetActive(false);
        EditorUtility.SetDirty(panelObj);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green><b>🎉 THÀNH CÔNG: Đã tạo giao diện CameraScannerPanel và liên kết Inspector 100%!</b></color>");
        EditorUtility.DisplayDialog("Thành công", "Đã thiết kế xong CameraScannerPanel, nút 'Quét Camera' và liên kết toàn bộ Inspector thành công!", "Tuyệt vời");
    }

    private static Button SetupBottomPanelButton(Transform canvasTr)
    {
        Transform bottomPanel = canvasTr.Find("BottomPanel");
        if (bottomPanel == null)
        {
            RectTransform[] all = canvasTr.GetComponentsInChildren<RectTransform>(true);
            foreach (var r in all)
            {
                if (r.name == "BottomPanel") { bottomPanel = r; break; }
            }
        }

        if (bottomPanel == null) return null;

        Transform existingBtn = bottomPanel.Find("BtnCameraScanner");
        GameObject btnObj;

        if (existingBtn != null)
        {
            btnObj = existingBtn.gameObject;
        }
        else
        {
            Transform template = bottomPanel.Find("BtnColorPicker");
            if (template == null) template = bottomPanel.Find("BtnAlgorithm");
            if (template == null) template = bottomPanel.Find("BtnSolve");

            if (template != null)
            {
                btnObj = Object.Instantiate(template.gameObject, bottomPanel, false);
                btnObj.name = "BtnCameraScanner";
                Undo.RegisterCreatedObjectUndo(btnObj, "Create BtnCameraScanner");
            }
            else
            {
                btnObj = new GameObject("BtnCameraScanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(btnObj, "Create BtnCameraScanner");
                btnObj.transform.SetParent(bottomPanel, false);
            }
        }

        // Đảm bảo có LayoutElement IgnoreLayout = false để layout group tự xếp
        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = false;

        TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = "Quét Camera";
        }

        Image img = btnObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.02f, 0.71f, 0.83f); // Cyan #06B6D4
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
        rt.sizeDelta = new Vector2(width, 36);

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
        tmp.color = Color.white;

        return btn;
    }

    private static Toggle GetOrCreateToggle(Transform parent, string name, string label, Vector2 pos, bool defaultOn)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
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
        rt.sizeDelta = new Vector2(330, 24);

        Toggle toggle = obj.GetComponent<Toggle>();
        toggle.isOn = defaultOn;

        // Background
        GameObject bgObj = GetOrCreateEmptyChild(obj.transform, "Background", new Vector2(-150, 0), new Vector2(20, 20));
        Image bgImg = bgObj.GetComponent<Image>();
        if (bgImg == null) bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.25f, 0.35f);

        // Checkmark
        GameObject checkObj = GetOrCreateEmptyChild(bgObj.transform, "Checkmark", Vector2.zero, new Vector2(14, 14));
        Image checkImg = checkObj.GetComponent<Image>();
        if (checkImg == null) checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0.06f, 0.72f, 0.51f);

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;

        // Label
        TextMeshProUGUI labelTMP = GetOrCreateTMP(obj.transform, "Label", new Vector2(20, 0), new Vector2(280, 22));
        labelTMP.text = label;
        labelTMP.fontSize = 12;
        labelTMP.color = new Color(0.85f, 0.90f, 0.95f);
        labelTMP.alignment = TextAlignmentOptions.Left;

        return toggle;
    }

    private static Slider GetOrCreateSlider(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max, float def)
    {
        Transform tr = parent.Find(name);
        GameObject obj;
        if (tr == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(Slider));
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

        Slider slider = obj.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = def;

        // Background
        GameObject bg = GetOrCreateEmptyChild(obj.transform, "Background", Vector2.zero, size);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        if (bgImg == null) bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.25f, 0.35f);

        // Fill Area
        GameObject fillArea = GetOrCreateEmptyChild(obj.transform, "Fill Area", Vector2.zero, size);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.sizeDelta = Vector2.zero;

        GameObject fill = GetOrCreateEmptyChild(fillArea.transform, "Fill", Vector2.zero, size);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        if (fillImg == null) fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.06f, 0.72f, 0.51f);

        slider.fillRect = fillRT;

        return slider;
    }
}
