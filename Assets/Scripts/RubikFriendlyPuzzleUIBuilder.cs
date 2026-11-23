using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Trình khởi tạo & nâng cấp giao diện "Friendly Puzzle" tự động theo đúng 3 ảnh tham chiếu:
/// 1. Giao diện chính (Sidebar trái, Khu vực Rubik 3D giữa + Bảng điều khiển dưới, Panel Điều khiển xoay & Tốc độ phải).
/// 2. Hộp thoại Nhập Công Thức Rubik.
/// 3. Giao diện Hướng Dẫn Giải từng bước (kèm tô sáng mặt xoay 3D & mũi tên chỉ hướng xoay).
/// </summary>
public static class RubikFriendlyPuzzleUIBuilder
{
    private const string UIVersionTag = "FriendlyPuzzleUI_v6";

    public static void EnsureUI(RubikUIController uiCtrl, bool forceRebuild = false)
    {
        if (uiCtrl == null)
            uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl == null) return;

        uiCtrl.FindCoreReferences();

        Canvas canvas = FindOrCreateMainCanvas();
        EnsureEventSystem();

        if (uiCtrl.cameraOrbit != null)
        {
            if (uiCtrl.cameraOrbit.target == null)
            {
                GameObject rm = GameObject.Find("RubikManager");
                if (rm != null) uiCtrl.cameraOrbit.target = rm.transform;
            }
            uiCtrl.cameraOrbit.ApplyFriendlyLightBackground();
        }
        else if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = RubikUITheme.AppBackground;
        }

        Transform versionMarker = canvas.transform.Find(UIVersionTag);
        if (versionMarker != null && !forceRebuild && uiCtrl.btnFaceU != null && uiCtrl.btnNavHome != null)
        {
            return;
        }

        BuildCompleteFriendlyPuzzleUI(canvas, uiCtrl);
    }

    public static void BuildCompleteFriendlyPuzzleUI(Canvas canvas, RubikUIController uiCtrl)
    {
        // Ẩn TopBar đen cũ nếu còn
        Transform oldTopBar = canvas.transform.Find("TopBar");
        if (oldTopBar != null) oldTopBar.gameObject.SetActive(false);

        // Xóa khung UI cũ của builder nếu rebuild
        string[] managedRootNames = {
            "FriendlyPuzzleUI_v2",
            "FriendlyPuzzleUI_v3",
            "FriendlyPuzzleUI_v4",
            "FriendlyPuzzleUI_v5",
            UIVersionTag,
            "OrbitArcDecoration",
            "CenterHeaderGroup",
            "SidebarPanel",
            "MobileBottomNav",
            "CameraQuickControls",
            "BottomPanel",
            "RightControlPanel",
            "BtnTopRightSettings",
            "AlgorithmInputPanel",
            "SolverPanel",
            "SolverLoadingModal",
            "GuideRotationArrow",
            "CameraScannerPanel",
            "NetColorPickerPanel",
            "ConfirmResetModal",
            "SettingsModal",
            "VictoryPanel"
        };

        foreach (string n in managedRootNames)
        {
            Transform existing = canvas.transform.Find(n);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        // Tạo marker đánh dấu phiên bản UI
        GameObject marker = new GameObject(UIVersionTag, typeof(RectTransform));
        marker.transform.SetParent(canvas.transform, false);

        // 1. Vòng cung xoay dưới chân khối Rubik 3D (như ảnh tham chiếu 1)
        RectTransform orbitArcRT = BuildOrbitArcDecoration(canvas.transform);

        // 2. Tiêu đề "Rubik 3D" & Đồng hồ nhỏ ở trung tâm phía trên
        RectTransform centerHeaderRT = BuildCenterHeader(canvas.transform, uiCtrl);

        // 3. Sidebar bên trái (Logo, 5 mục điều hướng, Thẻ Khám phá Rubik 3D)
        RectTransform sidebarRT = BuildLeftSidebar(canvas.transform, uiCtrl);

        // 4. Thanh điều hướng dưới cùng dành riêng cho màn hình dọc điện thoại (Mobile Responsive)
        RectTransform mobileNavRT = BuildMobileBottomNav(canvas.transform, uiCtrl);

        // 5. Cụm 3 nút điều khiển Camera nổi bên phải khối Rubik 3D (Reset góc nhìn, Zoom +, Zoom -)
        RectTransform camControlsRT = BuildCameraQuickControls(canvas.transform, uiCtrl);

        // 6. Bảng điều khiển phía dưới ("BẢNG ĐIỀU KHIỂN & CHẠY THỬ")
        RectTransform bottomPanelRT = BuildBottomControlPanel(canvas.transform, uiCtrl);

        // 7. Panel bên phải ("Điều khiển xoay" 6 mặt U/D/F/B/R/L + Shift + "Tốc độ")
        RectTransform rightPanelRT = BuildRightControlPanel(canvas.transform, uiCtrl);

        // 8. Nút "Cài đặt" nhỏ ở góc trên bên phải
        RectTransform topRightSettingsRT = BuildTopRightSettingsButton(canvas.transform, uiCtrl);

        // 9. Hộp thoại NHẬP CÔNG THỨC RUBIK (Ảnh tham chiếu 2)
        RectTransform algCardRT = BuildAlgorithmInputModal(canvas.transform, uiCtrl);

        // 10. Giao diện HƯỚNG DẪN GIẢI từng bước + Mũi tên 3D (Ảnh tham chiếu 3)
        RectTransform solverGuideRT = BuildSolutionGuideModal(canvas.transform, uiCtrl);

        // 11. Hộp thoại QUÉT KHỐI RUBIK QUA CAMERA (Friendly Puzzle Style)
        BuildCameraScannerModal(canvas.transform, uiCtrl);

        // 12. Hộp thoại TÔ MÀU KHỐI RUBIK 2D (Friendly Puzzle Style)
        BuildNetColorPickerModal(canvas.transform, uiCtrl);

        // 13. Hộp thoại Xác nhận Đặt lại (Reset Confirmation)
        BuildConfirmResetModal(canvas.transform, uiCtrl);

        // 14. Hộp thoại Cài đặt (Settings Modal)
        BuildSettingsModal(canvas.transform, uiCtrl);

        // 15. Bảng Chiến thắng (Victory Overlay)
        BuildVictoryModal(canvas.transform, uiCtrl);

        // 16. Hộp thoại Loading khi tìm lời giải Kociemba ("Đang tìm hướng giải ngắn nhất...")
        BuildSolverLoadingModal(canvas.transform, uiCtrl);

        // Đảm bảo thứ tự lớp Modal chuẩn xác
        Transform netPanel = canvas.transform.Find("NetColorPickerPanel");
        if (netPanel != null) netPanel.SetAsLastSibling();

        Transform camScannerPanel = canvas.transform.Find("CameraScannerPanel");
        if (camScannerPanel != null) camScannerPanel.SetAsLastSibling();

        if (uiCtrl.algorithmInput != null && uiCtrl.algorithmInput.inputPanel != null)
            uiCtrl.algorithmInput.inputPanel.transform.SetAsLastSibling();
        if (uiCtrl.settingsPanel != null)
            uiCtrl.settingsPanel.transform.SetAsLastSibling();
        if (uiCtrl.confirmResetPanel != null)
            uiCtrl.confirmResetPanel.transform.SetAsLastSibling();
        if (uiCtrl.victoryPanel != null)
            uiCtrl.victoryPanel.transform.SetAsLastSibling();

        Transform loadingModal = canvas.transform.Find("SolverLoadingModal");
        if (loadingModal != null) loadingModal.SetAsLastSibling();

        // 16. Gắn kết bộ điều khiển Responsive tự động cho PC & Điện thoại
        RubikResponsiveLayout responsive = canvas.GetComponent<RubikResponsiveLayout>();
        if (responsive == null) responsive = canvas.gameObject.AddComponent<RubikResponsiveLayout>();
        responsive.canvasScaler = canvas.GetComponent<CanvasScaler>();
        responsive.cameraOrbit = uiCtrl.cameraOrbit;
        responsive.sidebarPanel = sidebarRT;
        responsive.mobileBottomNav = mobileNavRT;
        responsive.centerHeader = centerHeaderRT;
        responsive.orbitArcRect = orbitArcRT;
        responsive.cameraControlsColumn = camControlsRT;
        responsive.bottomControlPanel = bottomPanelRT;
        responsive.rightControlPanel = rightPanelRT;
        responsive.topRightSettingsBtn = topRightSettingsRT;
        responsive.algorithmDialogCard = algCardRT;
        responsive.solverGuideContainer = solverGuideRT;
        responsive.ApplyResponsiveLayout(force: true);

        // Gắn sự kiện cho toàn bộ nút
        uiCtrl.BindAllUIEvents();
        if (uiCtrl.solutionViewer != null)
            uiCtrl.solutionViewer.BindButtons();
        if (uiCtrl.cameraScanner != null)
            uiCtrl.cameraScanner.BindScannerUIAndEvents();
        if (uiCtrl.colorPicker != null)
            uiCtrl.colorPicker.BindNetPickerUIAndEvents();
    }

    private static Canvas FindOrCreateMainCanvas()
    {
        GameObject canvasObj = GameObject.Find("RubikCanvas");
        Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            canvasObj = new GameObject("RubikCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }

    // =========================================================================
    // 1. VÒNG CUNG TRANG TRÍ DƯỚI KHỐI RUBIK 3D
    // =========================================================================
    private static RectTransform BuildOrbitArcDecoration(Transform canvasTr)
    {
        GameObject arcObj = CreateUIObject("OrbitArcDecoration", canvasTr);
        RectTransform rt = arcObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(490, 150);
        rt.anchoredPosition = new Vector2(-42, -105);

        Image img = arcObj.AddComponent<Image>();
        img.sprite = RubikUISpriteFactory.GetOrbitArcSprite();
        img.color = new Color(1f, 1f, 1f, 0.85f);
        img.raycastTarget = false;

        return rt;
    }

    // =========================================================================
    // 2. TIÊU ĐỀ TRUNG TÂM "Rubik 3D" & ĐỒNG HỒ
    // =========================================================================
    private static RectTransform BuildCenterHeader(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject headerGroup = CreateUIObject("CenterHeaderGroup", canvasTr);
        RectTransform rt = headerGroup.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(560, 95);
        rt.anchoredPosition = new Vector2(-42, -34);

        TextMeshProUGUI title = CreateTMP("TitleRubik3D", headerGroup.transform, "Rubik 3D", 28, FontStyles.Bold, RubikUITheme.Slate800);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(360, 40);
        titleRT.anchoredPosition = Vector2.zero;

        // Thanh thông tin thời gian & kỷ lục nhỏ gọn bên dưới tiêu đề
        GameObject timerPill = CreateRoundedPanel("TimerPill", headerGroup.transform, new Color(1f, 1f, 1f, 0.85f), RubikUITheme.RadiusMd, addShadow: false);
        RectTransform pillRT = timerPill.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(0.5f, 1f);
        pillRT.anchorMax = new Vector2(0.5f, 1f);
        pillRT.pivot = new Vector2(0.5f, 1f);
        pillRT.sizeDelta = new Vector2(290, 32);
        pillRT.anchoredPosition = new Vector2(0, -44);

        TextMeshProUGUI timerTMP = CreateTMP("TimerText", timerPill.transform, "00:00.000", 15, FontStyles.Bold, RubikUITheme.Slate600);
        RectTransform timerRT = timerTMP.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0f, 0f);
        timerRT.anchorMax = new Vector2(0.5f, 1f);
        timerRT.offsetMin = new Vector2(14, 0);
        timerRT.offsetMax = new Vector2(-4, 0);

        TextMeshProUGUI pbTMP = CreateTMP("PBText", timerPill.transform, "PB: --:--.---", 13, FontStyles.Normal, RubikUITheme.Slate500);
        RectTransform pbRT = pbTMP.GetComponent<RectTransform>();
        pbRT.anchorMin = new Vector2(0.5f, 0f);
        pbRT.anchorMax = new Vector2(1f, 1f);
        pbRT.offsetMin = new Vector2(4, 0);
        pbRT.offsetMax = new Vector2(-12, 0);

        TextMeshProUGUI scrambleTMP = CreateTMP("ScrambleText", headerGroup.transform, "", 12, FontStyles.Normal, RubikUITheme.Slate500);
        RectTransform scrRT = scrambleTMP.GetComponent<RectTransform>();
        scrRT.anchorMin = new Vector2(0.5f, 1f);
        scrRT.anchorMax = new Vector2(0.5f, 1f);
        scrRT.pivot = new Vector2(0.5f, 1f);
        scrRT.sizeDelta = new Vector2(620, 22);
        scrRT.anchoredPosition = new Vector2(0, -80);

        uiCtrl.timerText = timerTMP;
        uiCtrl.pbText = pbTMP;
        uiCtrl.scrambleText = scrambleTMP;

        if (uiCtrl.timer != null)
        {
            uiCtrl.timer.timerTextTMP = timerTMP;
            uiCtrl.timer.pbTextTMP = pbTMP;
        }

        return rt;
    }

    // =========================================================================
    // 3. SIDEBAR BÊN TRÁI (Ảnh tham chiếu 1)
    // =========================================================================
    private static RectTransform BuildLeftSidebar(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject sidebar = CreateRoundedPanel("SidebarPanel", canvasTr, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rt = sidebar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(24f, 0f);
        rt.sizeDelta = new Vector2(264f, -48f);

        // --- Phần đầu: Logo + Tên ứng dụng + Mô tả ---
        GameObject brandGroup = CreateUIObject("BrandHeader", sidebar.transform);
        RectTransform brandRT = brandGroup.GetComponent<RectTransform>();
        brandRT.anchorMin = new Vector2(0f, 1f);
        brandRT.anchorMax = new Vector2(1f, 1f);
        brandRT.pivot = new Vector2(0.5f, 1f);
        brandRT.sizeDelta = new Vector2(0f, 92f);
        brandRT.anchoredPosition = new Vector2(0f, -12f);

        GameObject logoObj = CreateUIObject("LogoRubik", brandGroup.transform);
        RectTransform logoRT = logoObj.GetComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0f, 0.5f);
        logoRT.anchorMax = new Vector2(0f, 0.5f);
        logoRT.pivot = new Vector2(0f, 0.5f);
        logoRT.sizeDelta = new Vector2(56f, 56f);
        logoRT.anchoredPosition = new Vector2(20f, 0f);
        Image logoImg = logoObj.AddComponent<Image>();
        logoImg.sprite = RubikUISpriteFactory.GetRubikLogoSprite();
        logoImg.preserveAspect = true;

        TextMeshProUGUI appTitle = CreateTMP("AppTitle", brandGroup.transform, "RubikSolver", 28, FontStyles.Bold, RubikUITheme.Slate800, TextAlignmentOptions.Left);
        RectTransform appTitleRT = appTitle.GetComponent<RectTransform>();
        appTitleRT.anchorMin = new Vector2(0f, 0.5f);
        appTitleRT.anchorMax = new Vector2(1f, 0.5f);
        appTitleRT.pivot = new Vector2(0f, 0f);
        appTitleRT.offsetMin = new Vector2(86f, 2f);
        appTitleRT.offsetMax = new Vector2(-12f, 34f);

        TextMeshProUGUI appSub = CreateTMP("AppSubtitle", brandGroup.transform, "Quét • Giải • Hướng dẫn", 13f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left);
        RectTransform appSubRT = appSub.GetComponent<RectTransform>();
        appSubRT.anchorMin = new Vector2(0f, 0.5f);
        appSubRT.anchorMax = new Vector2(1f, 0.5f);
        appSubRT.pivot = new Vector2(0f, 1f);
        appSubRT.offsetMin = new Vector2(86f, -24f);
        appSubRT.offsetMax = new Vector2(-12f, 0f);

        // --- Danh sách 5 mục điều hướng ---
        GameObject navList = CreateUIObject("NavList", sidebar.transform);
        RectTransform navListRT = navList.GetComponent<RectTransform>();
        navListRT.anchorMin = new Vector2(0f, 0f);
        navListRT.anchorMax = new Vector2(1f, 1f);
        navListRT.offsetMin = new Vector2(16f, 145f);
        navListRT.offsetMax = new Vector2(-16f, -118f);

        VerticalLayoutGroup vlg = navList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        uiCtrl.btnNavHome = CreateSidebarNavButton(navList.transform, "BtnNavHome", "Trang chủ", RubikUISpriteFactory.IconType.Home);
        uiCtrl.btnNavCamera = CreateSidebarNavButton(navList.transform, "BtnNavCamera", "Quét camera", RubikUISpriteFactory.IconType.Camera);
        uiCtrl.btnNavGuide = CreateSidebarNavButton(navList.transform, "BtnNavGuide", "Hướng dẫn giải", RubikUISpriteFactory.IconType.Lightbulb);
        uiCtrl.btnNavSettings = CreateSidebarNavButton(navList.transform, "BtnNavSettings", "Cài đặt", RubikUISpriteFactory.IconType.Settings);
        uiCtrl.btnNavColor2D = CreateSidebarNavButton(navList.transform, "BtnNavColor2D", "Tô màu 2D", RubikUISpriteFactory.IconType.Grid2D);

        uiCtrl.btnCameraScanner = uiCtrl.btnNavCamera;
        uiCtrl.btnColorPicker = uiCtrl.btnNavColor2D;

        // --- Thẻ giới thiệu dưới đáy Sidebar: "Khám phá Rubik 3D" ---
        GameObject exploreCard = CreateRoundedPanel("ExploreCard", sidebar.transform, RubikUITheme.Sky100, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform expRT = exploreCard.GetComponent<RectTransform>();
        expRT.anchorMin = new Vector2(0f, 0f);
        expRT.anchorMax = new Vector2(1f, 0f);
        expRT.pivot = new Vector2(0.5f, 0f);
        expRT.sizeDelta = new Vector2(-32f, 106f);
        expRT.anchoredPosition = new Vector2(0f, 18f);

        // Icon khối Rubik bên trái thẻ
        GameObject expIconBg = CreateUIObject("ExploreCubeIcon", exploreCard.transform);
        RectTransform expIconRT = expIconBg.GetComponent<RectTransform>();
        expIconRT.anchorMin = new Vector2(0f, 0.5f);
        expIconRT.anchorMax = new Vector2(0f, 0.5f);
        expIconRT.pivot = new Vector2(0f, 0.5f);
        expIconRT.sizeDelta = new Vector2(52f, 52f);
        expIconRT.anchoredPosition = new Vector2(12f, 0f);
        Image expIconImg = expIconBg.AddComponent<Image>();
        expIconImg.sprite = RubikUISpriteFactory.GetRubikLogoSprite();
        expIconImg.preserveAspect = true;

        TextMeshProUGUI expTitle = CreateTMP("ExploreTitle", exploreCard.transform, "Khám phá\nRubik 3D", 14.5f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left, wordWrapping: true);
        RectTransform expTitleRT = expTitle.GetComponent<RectTransform>();
        expTitleRT.anchorMin = new Vector2(0f, 0.5f);
        expTitleRT.anchorMax = new Vector2(1f, 1f);
        expTitleRT.offsetMin = new Vector2(72f, 2f);
        expTitleRT.offsetMax = new Vector2(-46f, -10f);

        TextMeshProUGUI expDesc = CreateTMP("ExploreDesc", exploreCard.transform, "Xoay, quan sát, tìm hiểu mọi góc độ.", 12.5f, FontStyles.Normal, RubikUITheme.Slate600, TextAlignmentOptions.Left, wordWrapping: true);
        RectTransform expDescRT = expDesc.GetComponent<RectTransform>();
        expDescRT.anchorMin = new Vector2(0f, 0f);
        expDescRT.anchorMax = new Vector2(1f, 0.5f);
        expDescRT.offsetMin = new Vector2(72f, 8f);
        expDescRT.offsetMax = new Vector2(-46f, 2f);

        // Nút mũi tên tròn màu xanh dương bên phải thẻ
        GameObject expBtnObj = CreateUIObject("BtnExplore3D", exploreCard.transform);
        RectTransform expBtnRT = expBtnObj.GetComponent<RectTransform>();
        expBtnRT.anchorMin = new Vector2(1f, 0f);
        expBtnRT.anchorMax = new Vector2(1f, 0f);
        expBtnRT.pivot = new Vector2(1f, 0f);
        expBtnRT.sizeDelta = new Vector2(32f, 32f);
        expBtnRT.anchoredPosition = new Vector2(-10f, 10f);

        Image expBtnBg = expBtnObj.AddComponent<Image>();
        expBtnBg.sprite = RubikUISpriteFactory.GetCircleSprite();
        expBtnBg.color = RubikUITheme.Blue500;

        Button expBtn = expBtnObj.AddComponent<Button>();
        CreateIconChild(expBtnObj.transform, RubikUISpriteFactory.IconType.ArrowRight, Color.white, new Vector2(18f, 18f));
        uiCtrl.btnExplore3D = expBtn;

        return rt;
    }

    private static Button CreateSidebarNavButton(Transform parent, string name, string labelText, RubikUISpriteFactory.IconType iconType)
    {
        GameObject btnObj = CreateRoundedPanel(name, parent, new Color(1f, 1f, 1f, 0f), RubikUITheme.RadiusMd, addShadow: false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject iconObj = CreateUIObject("Icon", btnObj.transform);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(24f, 24f);
        iconRT.anchoredPosition = new Vector2(16f, 0f);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = RubikUISpriteFactory.GetIcon(iconType);
        iconImg.color = RubikUITheme.Slate500;
        iconImg.raycastTarget = false;

        TextMeshProUGUI tmp = CreateTMP("Label", btnObj.transform, labelText, 15.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Left);
        RectTransform lblRT = tmp.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(52f, 0f);
        lblRT.offsetMax = new Vector2(-12f, 0f);

        return btn;
    }

    // =========================================================================
    // 4. THANH ĐIỀU HƯỚNG DƯỚI CHO ĐIỆN THOẠI (MOBILE PORTRAIT)
    // =========================================================================
    private static RectTransform BuildMobileBottomNav(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject mobileNav = CreateRoundedPanel("MobileBottomNav", canvasTr, Color.white, RubikUITheme.RadiusLg, addShadow: true);
        RectTransform rt = mobileNav.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 110f);
        rt.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup hlg = mobileNav.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 10, 10);
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        uiCtrl.btnMobileNavHome = CreateMobileNavButton(mobileNav.transform, "MobileNavHome", "Trang chủ", RubikUISpriteFactory.IconType.Home);
        uiCtrl.btnMobileNavCamera = CreateMobileNavButton(mobileNav.transform, "MobileNavCamera", "Quét", RubikUISpriteFactory.IconType.Camera);
        uiCtrl.btnMobileNavGuide = CreateMobileNavButton(mobileNav.transform, "MobileNavGuide", "Hướng dẫn", RubikUISpriteFactory.IconType.Lightbulb);
        uiCtrl.btnMobileNavSettings = CreateMobileNavButton(mobileNav.transform, "MobileNavSettings", "Cài đặt", RubikUISpriteFactory.IconType.Settings);
        uiCtrl.btnMobileNavColor2D = CreateMobileNavButton(mobileNav.transform, "MobileNavColor2D", "Tô màu 2D", RubikUISpriteFactory.IconType.Grid2D);

        mobileNav.SetActive(false);
        return rt;
    }

    private static Button CreateMobileNavButton(Transform parent, string name, string label, RubikUISpriteFactory.IconType iconType)
    {
        GameObject obj = CreateRoundedPanel(name, parent, new Color(1f, 1f, 1f, 0f), RubikUITheme.RadiusMd, addShadow: false);
        Button btn = obj.AddComponent<Button>();

        GameObject iconObj = CreateUIObject("Icon", obj.transform);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 1f);
        iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.pivot = new Vector2(0.5f, 1f);
        iconRT.sizeDelta = new Vector2(30f, 30f);
        iconRT.anchoredPosition = new Vector2(0f, -12f);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = RubikUISpriteFactory.GetIcon(iconType);
        iconImg.color = RubikUITheme.Slate500;
        iconImg.raycastTarget = false;

        TextMeshProUGUI tmp = CreateTMP("Label", obj.transform, label, 15f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Center);
        RectTransform lblRT = tmp.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(1f, 0f);
        lblRT.pivot = new Vector2(0.5f, 0f);
        lblRT.sizeDelta = new Vector2(0f, 28f);
        lblRT.anchoredPosition = new Vector2(0f, 8f);

        return btn;
    }

    // =========================================================================
    // 5. CỤM 3 NÚT ĐIỀU KHIỂN CAMERA NỔI CẠNH RUBIK 3D
    // =========================================================================
    private static RectTransform BuildCameraQuickControls(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject colObj = CreateUIObject("CameraQuickControls", canvasTr);
        RectTransform rt = colObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(54f, 174f);
        rt.anchoredPosition = new Vector2(265f, 110f);

        VerticalLayoutGroup vlg = colObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        uiCtrl.btnCamReset = CreateCircleIconButton(colObj.transform, "BtnCamReset", RubikUISpriteFactory.IconType.CubeReset);
        uiCtrl.btnCamZoomIn = CreateCircleIconButton(colObj.transform, "BtnCamZoomIn", RubikUISpriteFactory.IconType.ZoomIn);
        uiCtrl.btnCamZoomOut = CreateCircleIconButton(colObj.transform, "BtnCamZoomOut", RubikUISpriteFactory.IconType.ZoomOut);

        return rt;
    }

    private static Button CreateCircleIconButton(Transform parent, string name, RubikUISpriteFactory.IconType icon)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(46f, 46f);

        Image bg = obj.AddComponent<Image>();
        bg.sprite = RubikUISpriteFactory.GetCircleSprite();
        bg.color = Color.white;

        Shadow sh = obj.AddComponent<Shadow>();
        sh.effectColor = RubikUITheme.Ink(RubikUITheme.ShadowButton);
        sh.effectDistance = new Vector2(0f, -2f);

        Button btn = obj.AddComponent<Button>();
        CreateIconChild(obj.transform, icon, RubikUITheme.Slate700, new Vector2(24f, 24f));
        return btn;
    }

    // =========================================================================
    // 6. BẢNG ĐIỀU KHIỂN PHÍA DƯỚI ("BẢNG ĐIỀU KHIỂN & CHẠY THỬ")
    // =========================================================================
    private static RectTransform BuildBottomControlPanel(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject panel = CreateRoundedPanel("BottomPanel", canvasTr, Color.white, RubikUITheme.RadiusLg, addShadow: true);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(850f, 138f);
        rt.anchoredPosition = new Vector2(-42f, 24f);

        uiCtrl.bottomControlCard = panel;

        TextMeshProUGUI header = CreateTMP("SectionHeader", panel.transform, "BẢNG ĐIỀU KHIỂN & CHẠY THỬ", 18f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.offsetMin = new Vector2(24f, -34f);
        headerRT.offsetMax = new Vector2(-24f, -10f);

        GameObject row = CreateUIObject("ButtonsRow", panel.transform);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 0f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.offsetMin = new Vector2(18f, 8f);
        rowRT.offsetMax = new Vector2(-18f, -36f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        uiCtrl.btnScramble = CreateBottomActionItem(
            row.transform, "BtnScramble", "Trộn khối",
            RubikUITheme.Blue500, Color.white,
            RubikUISpriteFactory.IconType.Shuffle, hasOutline: false);

        uiCtrl.btnStart = CreateBottomActionItem(
            row.transform, "BtnStart", "Bắt đầu",
            RubikUITheme.SuccessLight, Color.white,
            RubikUISpriteFactory.IconType.Play, hasOutline: false);

        uiCtrl.btnUndo = CreateBottomActionItem(
            row.transform, "BtnUndo", "Hoàn tác\n(Undo)",
            RubikUITheme.Slate200, RubikUITheme.Slate600,
            RubikUISpriteFactory.IconType.Undo, hasOutline: false);

        uiCtrl.btnRedo = CreateBottomActionItem(
            row.transform, "BtnRedo", "Làm lại\n(Redo)",
            RubikUITheme.Slate200, RubikUITheme.Slate600,
            RubikUISpriteFactory.IconType.Redo, hasOutline: false);

        uiCtrl.btnSolve = CreateBottomActionItem(
            row.transform, "BtnSolve", "Giải tự động",
            RubikUITheme.VioletBg, RubikUITheme.Violet,
            RubikUISpriteFactory.IconType.SparkleBrain, hasOutline: false);

        uiCtrl.btnAlgorithm = CreateBottomActionItem(
            row.transform, "BtnAlgorithm", "Xem công thức",
            RubikUITheme.Blue50, RubikUITheme.Blue600,
            RubikUISpriteFactory.IconType.Book, hasOutline: true);

        uiCtrl.btnReset = CreateBottomActionItem(
            row.transform, "BtnReset", "Đặt lại (Reset)",
            RubikUITheme.DangerBg, RubikUITheme.DangerLight,
            RubikUISpriteFactory.IconType.Reset, hasOutline: false);

        return rt;
    }

    private static Button CreateBottomActionItem(
        Transform parent, string name, string labelText,
        Color boxColor, Color iconColor,
        RubikUISpriteFactory.IconType iconType, bool hasOutline)
    {
        GameObject itemContainer = CreateUIObject(name + "_Container", parent);

        // Nút hộp bo góc phía trên
        GameObject btnBox = CreateRoundedPanel(name, itemContainer.transform, boxColor, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform boxRT = btnBox.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 1f);
        boxRT.anchorMax = new Vector2(0.5f, 1f);
        boxRT.pivot = new Vector2(0.5f, 1f);
        boxRT.sizeDelta = new Vector2(64f, 50f);
        boxRT.anchoredPosition = new Vector2(0f, -2f);

        if (hasOutline)
        {
            GameObject outlineObj = CreateUIObject("Outline", btnBox.transform);
            RectTransform outRT = outlineObj.GetComponent<RectTransform>();
            outRT.anchorMin = Vector2.zero;
            outRT.anchorMax = Vector2.one;
            outRT.sizeDelta = Vector2.zero;
            Image outImg = outlineObj.AddComponent<Image>();
            outImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusMd, 2);
            outImg.type = Image.Type.Sliced;
            outImg.color = RubikUITheme.Blue300;
            outImg.raycastTarget = false;
        }

        Button btn = btnBox.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, boxColor);

        CreateIconChild(btnBox.transform, iconType, iconColor, new Vector2(26f, 26f));

        // Nhãn tiếng Việt bên dưới
        TextMeshProUGUI lbl = CreateTMP("Label", itemContainer.transform, labelText, 12.5f, FontStyles.Normal, RubikUITheme.Slate800, TextAlignmentOptions.Top);
        RectTransform lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(1f, 0f);
        lblRT.pivot = new Vector2(0.5f, 0f);
        lblRT.sizeDelta = new Vector2(0f, 36f);
        lblRT.anchoredPosition = new Vector2(0f, 0f);

        return btn;
    }

    // =========================================================================
    // 7. PANEL BÊN PHẢI: ĐIỀU KHIỂN XOAY (6 MẶT) + TỐC ĐỘ
    // =========================================================================
    private static RectTransform BuildRightControlPanel(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject rightRoot = CreateUIObject("RightControlPanel", canvasTr);
        RectTransform rt = rightRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(364f, 620f);
        rt.anchoredPosition = new Vector2(-24f, -12f);

        uiCtrl.rightControlContainer = rightRoot;

        // --- THẺ 1: Điều khiển xoay ---
        GameObject rotCard = CreateRoundedPanel("RotationControlCard", rightRoot.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rotRT = rotCard.GetComponent<RectTransform>();
        rotRT.anchorMin = new Vector2(0f, 1f);
        rotRT.anchorMax = new Vector2(1f, 1f);
        rotRT.pivot = new Vector2(0.5f, 1f);
        rotRT.sizeDelta = new Vector2(0f, 488f);
        rotRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI rotTitle = CreateTMP("Title", rotCard.transform, "Điều khiển xoay", 21f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform rotTitleRT = rotTitle.GetComponent<RectTransform>();
        rotTitleRT.anchorMin = new Vector2(0f, 1f);
        rotTitleRT.anchorMax = new Vector2(1f, 1f);
        rotTitleRT.pivot = new Vector2(0.5f, 1f);
        rotTitleRT.offsetMin = new Vector2(22f, -44f);
        rotTitleRT.offsetMax = new Vector2(-22f, -14f);

        TextMeshProUGUI rotSub = CreateTMP("Subtitle", rotCard.transform, "Nhấn phím hoặc bấm nút để xoay mặt", 13f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left, wordWrapping: true);
        RectTransform rotSubRT = rotSub.GetComponent<RectTransform>();
        rotSubRT.anchorMin = new Vector2(0f, 1f);
        rotSubRT.anchorMax = new Vector2(1f, 1f);
        rotSubRT.pivot = new Vector2(0.5f, 1f);
        rotSubRT.offsetMin = new Vector2(22f, -68f);
        rotSubRT.offsetMax = new Vector2(-22f, -44f);

        // Lưới 3 cột x 2 hàng cho 6 nút xoay mặt (U, D, F / B, R, L)
        GameObject gridObj = CreateUIObject("FaceButtonsGrid", rotCard.transform);
        RectTransform gridRT = gridObj.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0f, 1f);
        gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.pivot = new Vector2(0.5f, 1f);
        gridRT.sizeDelta = new Vector2(-40f, 290f);
        gridRT.anchoredPosition = new Vector2(0f, -80f);

        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(98f, 130f);
        glg.spacing = new Vector2(14f, 16f);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;
        glg.childAlignment = TextAnchor.UpperCenter;

        // Hàng 1: U (Vàng), D (Xanh lá nhạt), F (Xanh dương)
        uiCtrl.btnFaceU = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceU", "U", "Mặt trên",
            RubikUITheme.Yellow, RubikUITheme.Slate900,
            RubikUISpriteFactory.IconType.ArrowUp, out uiCtrl.labelFaceU);

        uiCtrl.btnFaceD = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceD", "D", "Mặt dưới",
            new Color32(0x6E, 0xE7, 0xB7, 255), RubikUITheme.Slate900,
            RubikUISpriteFactory.IconType.ArrowDown, out uiCtrl.labelFaceD);

        uiCtrl.btnFaceF = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceF", "F", "Mặt trước",
            RubikUITheme.Blue500, Color.white,
            RubikUISpriteFactory.IconType.ArrowRight, out uiCtrl.labelFaceF);

        // Hàng 2: B (Tím), R (Đỏ), L (Cam)
        uiCtrl.btnFaceB = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceB", "B", "Mặt sau",
            RubikUITheme.Violet, Color.white,
            RubikUISpriteFactory.IconType.ArrowLeft, out uiCtrl.labelFaceB);

        uiCtrl.btnFaceR = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceR", "R", "Mặt phải",
            RubikUITheme.DangerLight, Color.white,
            RubikUISpriteFactory.IconType.ArrowRight, out uiCtrl.labelFaceR);

        uiCtrl.btnFaceL = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceL", "L", "Mặt trái",
            new Color32(0xF9, 0x73, 0x16, 255), Color.white,
            RubikUISpriteFactory.IconType.ArrowLeft, out uiCtrl.labelFaceL);

        // Hộp Shift "Xoay ngược chiều" ở dưới thẻ Điều khiển xoay
        GameObject shiftBox = CreateRoundedPanel("BtnShiftToggle", rotCard.transform, RubikUITheme.Slate50, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform shiftRT = shiftBox.GetComponent<RectTransform>();
        shiftRT.anchorMin = new Vector2(0f, 0f);
        shiftRT.anchorMax = new Vector2(1f, 0f);
        shiftRT.pivot = new Vector2(0.5f, 0f);
        shiftRT.sizeDelta = new Vector2(-40f, 70f);
        shiftRT.anchoredPosition = new Vector2(0f, 18f);

        GameObject shiftBorder = CreateUIObject("Border", shiftBox.transform);
        RectTransform sbRT = shiftBorder.GetComponent<RectTransform>();
        sbRT.anchorMin = Vector2.zero;
        sbRT.anchorMax = Vector2.one;
        sbRT.sizeDelta = Vector2.zero;
        Image sbImg = shiftBorder.AddComponent<Image>();
        sbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusMd, 2);
        sbImg.type = Image.Type.Sliced;
        sbImg.color = RubikUITheme.Slate200;
        sbImg.raycastTarget = false;

        uiCtrl.btnShiftToggle = shiftBox.AddComponent<Button>();

        GameObject keycap = CreateRoundedPanel("ShiftKeycap", shiftBox.transform, RubikUITheme.Slate200, RubikUITheme.RadiusSm, addShadow: false);
        RectTransform keyRT = keycap.GetComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 0.5f);
        keyRT.anchorMax = new Vector2(0f, 0.5f);
        keyRT.pivot = new Vector2(0f, 0.5f);
        keyRT.sizeDelta = new Vector2(60f, 38f);
        keyRT.anchoredPosition = new Vector2(14f, 0f);

        uiCtrl.shiftBadgeBg = keycap.GetComponent<Image>();
        uiCtrl.shiftBadgeText = CreateTMP("KeyText", keycap.transform, "Shift", 14f, FontStyles.Bold, RubikUITheme.Slate600);
        StretchFull(uiCtrl.shiftBadgeText.GetComponent<RectTransform>());

        TextMeshProUGUI shiftTitle = CreateTMP("ShiftTitle", shiftBox.transform, "Xoay ngược chiều", 15f, FontStyles.Bold, RubikUITheme.Slate800, TextAlignmentOptions.Left);
        RectTransform stRT = shiftTitle.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0.5f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.offsetMin = new Vector2(88f, 0f);
        stRT.offsetMax = new Vector2(-12f, -10f);

        TextMeshProUGUI shiftSub = CreateTMP("ShiftSub", shiftBox.transform, "Giữ Shift + phím tương ứng", 12.5f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left);
        RectTransform ssRT = shiftSub.GetComponent<RectTransform>();
        ssRT.anchorMin = new Vector2(0f, 0f);
        ssRT.anchorMax = new Vector2(1f, 0.5f);
        ssRT.offsetMin = new Vector2(88f, 10f);
        ssRT.offsetMax = new Vector2(-12f, 0f);

        // --- THẺ 2: Tốc độ xoay ---
        GameObject speedCard = CreateRoundedPanel("SpeedControlCard", rightRoot.transform, Color.white, RubikUITheme.RadiusLg, addShadow: true);
        RectTransform speedRT = speedCard.GetComponent<RectTransform>();
        speedRT.anchorMin = new Vector2(0f, 0f);
        speedRT.anchorMax = new Vector2(1f, 0f);
        speedRT.pivot = new Vector2(0.5f, 0f);
        speedRT.sizeDelta = new Vector2(0f, 114f);
        speedRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI speedHeader = CreateTMP("SpeedTitle", speedCard.transform, "Tốc độ:", 18.5f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform shRT = speedHeader.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 1f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.pivot = new Vector2(0.5f, 1f);
        shRT.offsetMin = new Vector2(22f, -44f);
        shRT.offsetMax = new Vector2(-22f, -14f);

        // Icon đồng hồ
        GameObject clockObj = CreateUIObject("ClockIcon", speedCard.transform);
        RectTransform clockRT = clockObj.GetComponent<RectTransform>();
        clockRT.anchorMin = new Vector2(0f, 0f);
        clockRT.anchorMax = new Vector2(0f, 0f);
        clockRT.pivot = new Vector2(0f, 0.5f);
        clockRT.sizeDelta = new Vector2(24f, 24f);
        clockRT.anchoredPosition = new Vector2(22f, 34f);
        Image clockImg = clockObj.AddComponent<Image>();
        clockImg.sprite = RubikUISpriteFactory.GetIcon(RubikUISpriteFactory.IconType.Clock);
        clockImg.color = RubikUITheme.Slate600;

        // Giá trị thời gian "0.22s"
        TextMeshProUGUI speedValTMP = CreateTMP("SpeedValueText", speedCard.transform, "0.22s", 18f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform svRT = speedValTMP.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0f, 0f);
        svRT.anchorMax = new Vector2(0f, 0f);
        svRT.pivot = new Vector2(0f, 0.5f);
        svRT.sizeDelta = new Vector2(72f, 32f);
        svRT.anchoredPosition = new Vector2(52f, 34f);
        uiCtrl.speedValueText = speedValTMP;

        // Thanh trượt tốc độ màu xanh dương
        Slider slider = CreateModernSlider("SpeedSlider", speedCard.transform, 0.05f, 0.60f, 0.22f);
        RectTransform slRT = slider.GetComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0f, 0f);
        slRT.anchorMax = new Vector2(1f, 0f);
        slRT.pivot = new Vector2(0.5f, 0.5f);
        slRT.offsetMin = new Vector2(128f, 20f);
        slRT.offsetMax = new Vector2(-24f, 48f);
        uiCtrl.speedSlider = slider;

        return rt;
    }

    private static Button CreateFaceRotateCard(
        Transform parent, string name, string faceLetter, string captionVi,
        Color cardColor, Color contentColor,
        RubikUISpriteFactory.IconType arrowIcon, out TextMeshProUGUI letterLabel)
    {
        GameObject cell = CreateUIObject(name + "_Cell", parent);

        GameObject btnCard = CreateRoundedPanel(name, cell.transform, cardColor, RubikUITheme.RadiusMd, addShadow: true);
        RectTransform cardRT = btnCard.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0f, 1f);
        cardRT.anchorMax = new Vector2(1f, 1f);
        cardRT.pivot = new Vector2(0.5f, 1f);
        cardRT.sizeDelta = new Vector2(0f, 92f);
        cardRT.anchoredPosition = Vector2.zero;

        Button btn = btnCard.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, cardColor);

        letterLabel = CreateTMP("FaceLetter", btnCard.transform, faceLetter, 24f, FontStyles.Bold, contentColor, TextAlignmentOptions.Center);
        RectTransform letRT = letterLabel.GetComponent<RectTransform>();
        letRT.anchorMin = new Vector2(0f, 0.45f);
        letRT.anchorMax = new Vector2(1f, 1f);
        letRT.offsetMin = new Vector2(0f, 0f);
        letRT.offsetMax = new Vector2(0f, -6f);

        GameObject arrowObj = CreateUIObject("ArrowIcon", btnCard.transform);
        RectTransform arrRT = arrowObj.GetComponent<RectTransform>();
        arrRT.anchorMin = new Vector2(0.5f, 0f);
        arrRT.anchorMax = new Vector2(0.5f, 0f);
        arrRT.pivot = new Vector2(0.5f, 0f);
        arrRT.sizeDelta = new Vector2(24f, 24f);
        arrRT.anchoredPosition = new Vector2(0f, 14f);

        Image arrImg = arrowObj.AddComponent<Image>();
        arrImg.sprite = RubikUISpriteFactory.GetIcon(arrowIcon);
        arrImg.color = contentColor;
        arrImg.raycastTarget = false;

        TextMeshProUGUI cap = CreateTMP("Caption", cell.transform, captionVi, 13.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Center);
        RectTransform capRT = cap.GetComponent<RectTransform>();
        capRT.anchorMin = new Vector2(0f, 0f);
        capRT.anchorMax = new Vector2(1f, 0f);
        capRT.pivot = new Vector2(0.5f, 0f);
        capRT.sizeDelta = new Vector2(0f, 30f);
        capRT.anchoredPosition = Vector2.zero;

        return btn;
    }

    // =========================================================================
    // 8. NÚT "⚙ Cài đặt" GÓC TRÊN PHẢI
    // =========================================================================
    private static RectTransform BuildTopRightSettingsButton(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject btnObj = CreateRoundedPanel("BtnTopRightSettings", canvasTr, Color.white, RubikUITheme.RadiusMd, addShadow: true);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(118f, 38f);
        rt.anchoredPosition = new Vector2(-28f, -24f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject iconObj = CreateUIObject("Icon", btnObj.transform);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(18f, 18f);
        iconRT.anchoredPosition = new Vector2(14f, 0f);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = RubikUISpriteFactory.GetIcon(RubikUISpriteFactory.IconType.Settings);
        iconImg.color = RubikUITheme.Slate700;
        iconImg.raycastTarget = false;

        TextMeshProUGUI lbl = CreateTMP("Label", btnObj.transform, "Cài đặt", 14.5f, FontStyles.Bold, RubikUITheme.Slate800, TextAlignmentOptions.Left);
        RectTransform lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(40f, 0f);
        lblRT.offsetMax = new Vector2(-10f, 0f);

        uiCtrl.btnTopRightSettings = btn;
        return rt;
    }

    // =========================================================================
    // 9. HỘP THOẠI NHẬP CÔNG THỨC RUBIK (Ảnh tham chiếu 2)
    // =========================================================================
    private static RectTransform BuildAlgorithmInputModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject backdrop = CreateUIObject("AlgorithmInputPanel", canvasTr);
        RectTransform backdropRT = backdrop.GetComponent<RectTransform>();
        StretchFull(backdropRT);
        Image backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = RubikUITheme.Ink(0.35f);

        GameObject card = CreateRoundedPanel("DialogCard", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(540f, 430f);
        cardRT.anchoredPosition = Vector2.zero;

        // Tiêu đề lớn: NHẬP CÔNG THỨC RUBIK
        TextMeshProUGUI title = CreateTMP("TitleText", card.transform, "NHẬP CÔNG THỨC RUBIK", 26f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.offsetMin = new Vector2(32f, -66f);
        titleRT.offsetMax = new Vector2(-32f, -24f);

        // Ô nhập công thức bo góc
        GameObject inputBox = CreateRoundedPanel("FormulaInputField", card.transform, Color.white, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform inputRT = inputBox.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0f, 1f);
        inputRT.anchorMax = new Vector2(1f, 1f);
        inputRT.pivot = new Vector2(0.5f, 1f);
        inputRT.sizeDelta = new Vector2(-64f, 64f);
        inputRT.anchoredPosition = new Vector2(0f, -82f);

        GameObject inputBorder = CreateUIObject("Border", inputBox.transform);
        StretchFull(inputBorder.GetComponent<RectTransform>());
        Image ibImg = inputBorder.AddComponent<Image>();
        ibImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusMd, 2);
        ibImg.type = Image.Type.Sliced;
        ibImg.color = RubikUITheme.Slate400;
        ibImg.raycastTarget = false;

        GameObject textArea = CreateUIObject("Text Area", inputBox.transform);
        RectTransform taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(18f, 8f);
        taRT.offsetMax = new Vector2(-18f, -8f);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMP("Placeholder", textArea.transform, "Ví dụ: R U R' U' F' U2...", 20f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left);
        StretchFull(placeholder.GetComponent<RectTransform>());

        TextMeshProUGUI inputText = CreateTMP("Text", textArea.transform, "", 20f, FontStyles.Normal, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        StretchFull(inputText.GetComponent<RectTransform>());

        TMP_InputField tmpInput = inputBox.AddComponent<TMP_InputField>();
        tmpInput.textViewport = taRT;
        tmpInput.textComponent = inputText;
        tmpInput.placeholder = placeholder;
        tmpInput.fontAsset = inputText.font;
        tmpInput.pointSize = 20;

        // Dòng thông báo lỗi / trạng thái kiểm tra cú pháp
        TextMeshProUGUI statusTMP = CreateTMP("StatusText", card.transform, "", 13.5f, FontStyles.Normal, RubikUITheme.Danger, TextAlignmentOptions.Left);
        RectTransform stRT = statusTMP.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 1f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.pivot = new Vector2(0.5f, 1f);
        stRT.offsetMin = new Vector2(34f, -174f);
        stRT.offsetMax = new Vector2(-34f, -148f);

        // Hàng 1: Dán | Sao chép | Xóa
        GameObject row1 = CreateUIObject("UtilityButtonsRow", card.transform);
        RectTransform r1RT = row1.GetComponent<RectTransform>();
        r1RT.anchorMin = new Vector2(0f, 1f);
        r1RT.anchorMax = new Vector2(1f, 1f);
        r1RT.pivot = new Vector2(0.5f, 1f);
        r1RT.sizeDelta = new Vector2(-64f, 54f);
        r1RT.anchoredPosition = new Vector2(0f, -180f);

        HorizontalLayoutGroup r1Hlg = row1.AddComponent<HorizontalLayoutGroup>();
        r1Hlg.spacing = 14f;
        r1Hlg.childControlWidth = true;
        r1Hlg.childControlHeight = true;
        r1Hlg.childForceExpandWidth = true;
        r1Hlg.childForceExpandHeight = true;

        Button btnPaste = CreateIconLabelPillButton(row1.transform, "BtnPaste", "Dán", RubikUISpriteFactory.IconType.Paste, RubikUITheme.Slate200, RubikUITheme.Slate900);
        Button btnCopy = CreateIconLabelPillButton(row1.transform, "BtnCopy", "Sao chép", RubikUISpriteFactory.IconType.Copy, RubikUITheme.Slate200, RubikUITheme.Slate900);
        Button btnClear = CreateIconLabelPillButton(row1.transform, "BtnClear", "Xóa", RubikUISpriteFactory.IconType.Trash, RubikUITheme.Slate200, RubikUITheme.Slate900);

        // Hàng 2: Chạy mượt | Áp dụng ngay
        GameObject row2 = CreateUIObject("ExecuteButtonsRow", card.transform);
        RectTransform r2RT = row2.GetComponent<RectTransform>();
        r2RT.anchorMin = new Vector2(0f, 1f);
        r2RT.anchorMax = new Vector2(1f, 1f);
        r2RT.pivot = new Vector2(0.5f, 1f);
        r2RT.sizeDelta = new Vector2(-64f, 64f);
        r2RT.anchoredPosition = new Vector2(0f, -254f);

        HorizontalLayoutGroup r2Hlg = row2.AddComponent<HorizontalLayoutGroup>();
        r2Hlg.spacing = 16f;
        r2Hlg.childControlWidth = true;
        r2Hlg.childControlHeight = true;
        r2Hlg.childForceExpandWidth = true;
        r2Hlg.childForceExpandHeight = true;

        Button btnPlayAnimate = CreateSimplePillButton(row2.transform, "BtnPlayAnimate", "Chạy mượt", RubikUITheme.Blue100, RubikUITheme.Blue700, 21f, hasBorder: false);
        Button btnApplyInstant = CreateSimplePillButton(row2.transform, "BtnApplyInstant", "Áp dụng ngay", RubikUITheme.Blue600, Color.white, 21f, hasBorder: false);

        // Hàng 3: Đóng
        Button btnClose = CreateSimplePillButton(card.transform, "BtnClose", "Đóng", RubikUITheme.Slate200, RubikUITheme.Slate900, 21f, hasBorder: true);
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0f, 0f);
        closeRT.anchorMax = new Vector2(1f, 0f);
        closeRT.pivot = new Vector2(0.5f, 0f);
        closeRT.sizeDelta = new Vector2(-64f, 60f);
        closeRT.anchoredPosition = new Vector2(0f, 28f);

        // Gắn kết vào RubikAlgorithmInput
        if (uiCtrl.algorithmInput == null)
        {
            GameObject rm = GameObject.Find("RubikManager");
            if (rm != null)
            {
                uiCtrl.algorithmInput = rm.GetComponent<RubikAlgorithmInput>();
                if (uiCtrl.algorithmInput == null)
                    uiCtrl.algorithmInput = rm.AddComponent<RubikAlgorithmInput>();
            }
        }

        if (uiCtrl.algorithmInput != null)
        {
            uiCtrl.algorithmInput.inputPanel = backdrop;
            uiCtrl.algorithmInput.formulaInputField = tmpInput;
            uiCtrl.algorithmInput.statusText = statusTMP;
            uiCtrl.algorithmInput.btnPaste = btnPaste;
            uiCtrl.algorithmInput.btnCopy = btnCopy;
            uiCtrl.algorithmInput.btnClear = btnClear;
            uiCtrl.algorithmInput.btnPlayAnimate = btnPlayAnimate;
            uiCtrl.algorithmInput.btnApplyInstant = btnApplyInstant;
            uiCtrl.algorithmInput.btnClose = btnClose;
        }

        backdrop.SetActive(false);
        return cardRT;
    }

    private static Button CreateIconLabelPillButton(Transform parent, string name, string text, RubikUISpriteFactory.IconType icon, Color bgCol, Color fgCol)
    {
        GameObject obj = CreateRoundedPanel(name, parent, bgCol, RubikUITheme.RadiusMd, addShadow: false);
        Button btn = obj.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, bgCol);

        HorizontalLayoutGroup hlg = obj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(14, 14, 6, 6);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        GameObject icObj = CreateUIObject("Icon", obj.transform);
        icObj.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 22f);
        Image icImg = icObj.AddComponent<Image>();
        icImg.sprite = RubikUISpriteFactory.GetIcon(icon);
        icImg.color = fgCol;
        icImg.raycastTarget = false;

        TextMeshProUGUI lbl = CreateTMP("Label", obj.transform, text, 18f, FontStyles.Normal, fgCol, TextAlignmentOptions.Center);
        lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(text.Length > 5 ? 92f : 52f, 34f);

        return btn;
    }

    private static Button CreateSimplePillButton(Transform parent, string name, string text, Color bgCol, Color fgCol, float fontSize, bool hasBorder)
    {
        GameObject obj = CreateRoundedPanel(name, parent, bgCol, RubikUITheme.RadiusMd, addShadow: false);
        if (hasBorder)
        {
            GameObject b = CreateUIObject("Border", obj.transform);
            StretchFull(b.GetComponent<RectTransform>());
            Image bImg = b.AddComponent<Image>();
            bImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusMd, 2);
            bImg.type = Image.Type.Sliced;
            bImg.color = RubikUITheme.Slate400;
            bImg.raycastTarget = false;
        }

        Button btn = obj.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, bgCol);

        TextMeshProUGUI lbl = CreateTMP("Label", obj.transform, text, fontSize, FontStyles.Bold, fgCol, TextAlignmentOptions.Center);
        StretchFull(lbl.GetComponent<RectTransform>());
        return btn;
    }

    // =========================================================================
    // 10. GIAO DIỆN HƯỚNG DẪN GIẢI (Ảnh tham chiếu 3)
    // =========================================================================
    private static RectTransform BuildSolutionGuideModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        // Mũi tên cong chỉ hướng xoay 3D trên màn hình
        GameObject arrowObj = CreateUIObject("GuideRotationArrow", canvasTr);
        RectTransform arrowRT = arrowObj.GetComponent<RectTransform>();
        arrowRT.sizeDelta = new Vector2(96f, 96f);
        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.sprite = RubikUISpriteFactory.GetCurvedRotationArrowSprite();
        arrowImg.color = RubikUITheme.BrandBlue;
        arrowImg.raycastTarget = false;
        arrowObj.SetActive(false);

        // Khung chứa Hướng dẫn giải bao quanh vùng Rubik 3D ở giữa
        GameObject solverRoot = CreateUIObject("SolverPanel", canvasTr);
        RectTransform solverRT = solverRoot.GetComponent<RectTransform>();
        solverRT.anchorMin = new Vector2(0.5f, 0.5f);
        solverRT.anchorMax = new Vector2(0.5f, 0.5f);
        solverRT.pivot = new Vector2(0.5f, 0.5f);
        solverRT.sizeDelta = new Vector2(680f, 920f);
        solverRT.anchoredPosition = new Vector2(120f, 0f);

        // --- THẺ TRÊN: Thanh tiêu đề + Thẻ hướng dẫn nền xanh nhạt ---
        GameObject topCard = CreateRoundedPanel("GuideTopCard", solverRoot.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform topRT = topCard.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.sizeDelta = new Vector2(0f, 205f);
        topRT.anchoredPosition = Vector2.zero;

        // Nút Quay lại "←"
        GameObject backBtnObj = CreateRoundedPanel("BtnClose", topCard.transform, new Color(1f, 1f, 1f, 0f), RubikUITheme.RadiusSm, addShadow: false);
        RectTransform backRT = backBtnObj.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0f, 1f);
        backRT.anchorMax = new Vector2(0f, 1f);
        backRT.pivot = new Vector2(0f, 1f);
        backRT.sizeDelta = new Vector2(46f, 46f);
        backRT.anchoredPosition = new Vector2(18f, -12f);
        Button btnCloseSolver = backBtnObj.AddComponent<Button>();
        CreateIconChild(backBtnObj.transform, RubikUISpriteFactory.IconType.BackArrow, RubikUITheme.Slate900, new Vector2(26f, 26f));

        // Tiêu đề "Hướng dẫn giải"
        TextMeshProUGUI guideTitle = CreateTMP("GuideHeaderTitle", topCard.transform, "Hướng dẫn giải", 22f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Center);
        RectTransform gtRT = guideTitle.GetComponent<RectTransform>();
        gtRT.anchorMin = new Vector2(0f, 1f);
        gtRT.anchorMax = new Vector2(1f, 1f);
        gtRT.pivot = new Vector2(0.5f, 1f);
        gtRT.sizeDelta = new Vector2(0f, 46f);
        gtRT.anchoredPosition = new Vector2(0f, -12f);

        // Bộ đếm bước "Bước 3/7"
        TextMeshProUGUI stepInfoTMP = CreateTMP("StepInfoText", topCard.transform, "Bước 1/7", 16.5f, FontStyles.Normal, RubikUITheme.Slate600, TextAlignmentOptions.Right);
        RectTransform siRT = stepInfoTMP.GetComponent<RectTransform>();
        siRT.anchorMin = new Vector2(1f, 1f);
        siRT.anchorMax = new Vector2(1f, 1f);
        siRT.pivot = new Vector2(1f, 1f);
        siRT.sizeDelta = new Vector2(220f, 46f);
        siRT.anchoredPosition = new Vector2(-24f, -12f);

        // Thẻ nền xanh nhạt chứa Ký hiệu lớn (R), Tiêu đề mặt, Mô tả chiều xoay
        GameObject blueCard = CreateRoundedPanel("InstructionBanner", topCard.transform, new Color32(0xEA, 0xF2, 0xFF, 255), RubikUITheme.RadiusLg, addShadow: false);
        RectTransform bcRT = blueCard.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0f, 0f);
        bcRT.anchorMax = new Vector2(1f, 1f);
        bcRT.offsetMin = new Vector2(20f, 18f);
        bcRT.offsetMax = new Vector2(-20f, -66f);

        // Hình tròn xanh dương chứa ký hiệu nước đi (R)
        GameObject badgeCircle = CreateUIObject("MoveBadgeCircle", blueCard.transform);
        RectTransform badgeRT = badgeCircle.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0f, 0.5f);
        badgeRT.anchorMax = new Vector2(0f, 0.5f);
        badgeRT.pivot = new Vector2(0f, 0.5f);
        badgeRT.sizeDelta = new Vector2(68f, 68f);
        badgeRT.anchoredPosition = new Vector2(20f, 0f);
        Image badgeImg = badgeCircle.AddComponent<Image>();
        badgeImg.sprite = RubikUISpriteFactory.GetCircleSprite();
        badgeImg.color = RubikUITheme.BrandBlue;

        TextMeshProUGUI badgeText = CreateTMP("MoveBadgeText", badgeCircle.transform, "R", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchFull(badgeText.GetComponent<RectTransform>());

        TextMeshProUGUI moveTitleTMP = CreateTMP("MoveTitleText", blueCard.transform, "Xoay mặt phải (R)", 21f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform mtRT = moveTitleTMP.GetComponent<RectTransform>();
        mtRT.anchorMin = new Vector2(0f, 0.5f);
        mtRT.anchorMax = new Vector2(1f, 1f);
        mtRT.offsetMin = new Vector2(106f, 2f);
        mtRT.offsetMax = new Vector2(-18f, -14f);

        TextMeshProUGUI moveDescTMP = CreateTMP("NextMoveText", blueCard.transform, "Xoay 90° theo chiều kim đồng hồ", 15.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Left, wordWrapping: true);
        RectTransform mdRT = moveDescTMP.GetComponent<RectTransform>();
        mdRT.anchorMin = new Vector2(0f, 0f);
        mdRT.anchorMax = new Vector2(1f, 0.5f);
        mdRT.offsetMin = new Vector2(106f, 12f);
        mdRT.offsetMax = new Vector2(-18f, 2f);

        // --- THẺ DƯỚI: Hai nút Trước / Tiếp theo + Thanh Chip nước đi ---
        GameObject bottomArea = CreateUIObject("GuideBottomArea", solverRoot.transform);
        RectTransform botRT = bottomArea.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.sizeDelta = new Vector2(0f, 196f);
        botRT.anchoredPosition = Vector2.zero;

        // Hàng nút điều hướng bước: "< Trước" | "Tiếp theo >" | "⚡ Tự động"
        GameObject navRow = CreateUIObject("StepNavButtonsRow", bottomArea.transform);
        RectTransform nrRT = navRow.GetComponent<RectTransform>();
        nrRT.anchorMin = new Vector2(0f, 1f);
        nrRT.anchorMax = new Vector2(1f, 1f);
        nrRT.pivot = new Vector2(0.5f, 1f);
        nrRT.sizeDelta = new Vector2(0f, 64f);
        nrRT.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup nrHlg = navRow.AddComponent<HorizontalLayoutGroup>();
        nrHlg.spacing = 14f;
        nrHlg.childControlWidth = true;
        nrHlg.childControlHeight = true;
        nrHlg.childForceExpandWidth = false;
        nrHlg.childForceExpandHeight = true;

        Button btnPrev = CreateSimplePillButton(navRow.transform, "BtnPrev", "<    Trước", RubikUITheme.Slate200, RubikUITheme.Slate900, 19f, hasBorder: false);
        btnPrev.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button btnNext = CreateSimplePillButton(navRow.transform, "BtnNext", "Tiếp theo    >", RubikUITheme.BrandBlue, Color.white, 19f, hasBorder: false);
        btnNext.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.25f;

        Button btnAuto = CreateSimplePillButton(navRow.transform, "BtnAuto", "Tự động", RubikUITheme.VioletBg, new Color32(0x6D, 0x28, 0xD9, 255), 16.5f, hasBorder: false);
        btnAuto.gameObject.AddComponent<LayoutElement>().preferredWidth = 135f;

        // Khung trắng chứa chuỗi Chip nước đi (R, U, R', U'...) + chấm tròn bên dưới
        GameObject chipsCard = CreateRoundedPanel("ChipsCard", bottomArea.transform, Color.white, RubikUITheme.RadiusLg, addShadow: true);
        RectTransform ccRT = chipsCard.GetComponent<RectTransform>();
        // Neo theo đáy khung & chừa chỗ hàng nút ở trên (64 + khoảng cách 14) để không bao giờ che nút Trước/Tiếp theo/Tự động
        ccRT.anchorMin = new Vector2(0f, 0f);
        ccRT.anchorMax = new Vector2(1f, 1f);
        ccRT.pivot = new Vector2(0.5f, 0f);
        ccRT.offsetMin = Vector2.zero;
        ccRT.offsetMax = new Vector2(0f, -78f);

        GameObject scrollArea = CreateUIObject("ChipsScrollViewport", chipsCard.transform);
        RectTransform saRT = scrollArea.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = new Vector2(18f, 10f);
        saRT.offsetMax = new Vector2(-18f, -10f);
        scrollArea.AddComponent<RectMask2D>();

        GameObject chipsContent = CreateUIObject("ChipsContainer", scrollArea.transform);
        RectTransform contentRT = chipsContent.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0.5f);
        contentRT.anchorMax = new Vector2(1f, 0.5f);
        contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.sizeDelta = new Vector2(0f, 68f);

        HorizontalLayoutGroup chipsHlg = chipsContent.AddComponent<HorizontalLayoutGroup>();
        chipsHlg.spacing = 12f;
        chipsHlg.childAlignment = TextAnchor.MiddleCenter;
        chipsHlg.childControlWidth = false;
        chipsHlg.childControlHeight = false;
        chipsHlg.childForceExpandWidth = false;

        ContentSizeFitter csf = chipsContent.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = chipsCard.AddComponent<ScrollRect>();
        scrollRect.viewport = saRT;
        scrollRect.content = contentRT;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;

        // Gắn kết vào RubikSolutionViewer & RubikGuideHighlighter
        GameObject rm = GameObject.Find("RubikManager");
        if (uiCtrl.solutionViewer == null && rm != null)
        {
            uiCtrl.solutionViewer = rm.GetComponent<RubikSolutionViewer>();
            if (uiCtrl.solutionViewer == null)
                uiCtrl.solutionViewer = rm.AddComponent<RubikSolutionViewer>();
        }

        if (uiCtrl.solutionViewer != null)
        {
            RubikGuideHighlighter highlighter = uiCtrl.solutionViewer.GetComponent<RubikGuideHighlighter>();
            if (highlighter == null)
                highlighter = uiCtrl.solutionViewer.gameObject.AddComponent<RubikGuideHighlighter>();

            highlighter.arrowIndicatorUI = arrowRT;
            highlighter.arrowImage = arrowImg;

            uiCtrl.solutionViewer.guideHighlighter = highlighter;
            uiCtrl.solutionViewer.solverPanel = solverRoot;
            uiCtrl.solutionViewer.stepInfoText = stepInfoTMP;
            uiCtrl.solutionViewer.moveBadgeText = badgeText;
            uiCtrl.solutionViewer.moveTitleText = moveTitleTMP;
            uiCtrl.solutionViewer.nextMoveText = moveDescTMP;
            uiCtrl.solutionViewer.chipsContainer = chipsContent.transform;
            uiCtrl.solutionViewer.btnPrevStep = btnPrev;
            uiCtrl.solutionViewer.btnNextStep = btnNext;
            uiCtrl.solutionViewer.btnAutoSolve = btnAuto;
            uiCtrl.solutionViewer.btnCloseSolver = btnCloseSolver;
        }

        solverRoot.SetActive(false);
        return solverRT;
    }

    // =========================================================================
    // 11. HỘP THOẠI QUÉT KHỐI RUBIK QUA CAMERA (FRIENDLY PUZZLE STYLE)
    // =========================================================================
    public static void BuildCameraScannerModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        Transform existing = canvasTr.Find("CameraScannerPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject backdrop = CreateUIObject("CameraScannerPanel", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(RubikUITheme.ScrimStrong);

        GameObject card = CreateRoundedPanel("ScannerCard", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(880f, 630f);

        // --- HEADER ---
        GameObject headerObj = CreateUIObject("Header", card.transform);
        RectTransform headerRT = headerObj.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(-56f, 60f);
        headerRT.anchoredPosition = new Vector2(0f, -20f);

        GameObject iconBadge = CreateRoundedPanel("HeaderIconBadge", headerObj.transform, RubikUITheme.Blue100, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform ibRT = iconBadge.GetComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0f, 0.5f);
        ibRT.anchorMax = new Vector2(0f, 0.5f);
        ibRT.pivot = new Vector2(0f, 0.5f);
        ibRT.sizeDelta = new Vector2(46f, 46f);
        ibRT.anchoredPosition = Vector2.zero;
        CreateIconChild(iconBadge.transform, RubikUISpriteFactory.IconType.Camera, RubikUITheme.Blue600, new Vector2(24f, 24f));

        TextMeshProUGUI titleTMP = CreateTMP("TitleText", headerObj.transform, "QUÉT KHỐI RUBIK QUA CAMERA", 22f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform tRT = titleTMP.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.5f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(58f, -2f);
        tRT.offsetMax = new Vector2(-56f, 0f);

        TextMeshProUGUI subTMP = CreateTMP("SubtitleText", headerObj.transform, "Căn mặt Rubik vào khung ngắm 3x3 hoặc nhấp vào ô xem trước để chỉnh màu", 13.5f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left);
        RectTransform sRT = subTMP.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 0.5f);
        sRT.offsetMin = new Vector2(58f, 0f);
        sRT.offsetMax = new Vector2(-56f, 2f);

        GameObject closeBtnObj = CreateRoundedPanel("BtnClose", headerObj.transform, RubikUITheme.Slate100, RubikUITheme.RadiusSm, addShadow: false);
        RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 0.5f);
        closeRT.anchorMax = new Vector2(1f, 0.5f);
        closeRT.pivot = new Vector2(1f, 0.5f);
        closeRT.sizeDelta = new Vector2(42f, 42f);
        closeRT.anchoredPosition = Vector2.zero;
        Button btnClose = closeBtnObj.AddComponent<Button>();
        ApplySubtleButtonTransition(btnClose, RubikUITheme.Slate100);
        CreateIconChild(closeBtnObj.transform, RubikUISpriteFactory.IconType.Close, RubikUITheme.Slate600, new Vector2(20f, 20f));

        // --- CỘT TRÁI: KHUNG CAMERA & ĐIỀU CHỈNH KHUNG NGẮM ---
        GameObject leftContainer = CreateRoundedPanel("LeftContainer", card.transform, RubikUITheme.Slate50, RubikUITheme.RadiusLg, addShadow: false);
        RectTransform leftRT = leftContainer.GetComponent<RectTransform>();
        leftRT.sizeDelta = new Vector2(400f, 510f);
        leftRT.anchoredPosition = new Vector2(-206f, -34f);

        GameObject leftBorder = CreateUIObject("Border", leftContainer.transform);
        StretchFull(leftBorder.GetComponent<RectTransform>());
        Image lbImg = leftBorder.AddComponent<Image>();
        lbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusLg, 2);
        lbImg.type = Image.Type.Sliced;
        lbImg.color = RubikUITheme.Slate200;
        lbImg.raycastTarget = false;

        // Khung nền tối ôm sát CameraFeed
        GameObject camFrame = CreateRoundedPanel("CameraFeedFrame", leftContainer.transform, RubikUITheme.Slate900, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform cfRT = camFrame.GetComponent<RectTransform>();
        cfRT.sizeDelta = new Vector2(342f, 342f);
        cfRT.anchoredPosition = new Vector2(0f, 64f);

        GameObject cameraFeedObj = CreateUIObject("CameraFeed", leftContainer.transform);
        RectTransform feedRT = cameraFeedObj.GetComponent<RectTransform>();
        feedRT.sizeDelta = new Vector2(330f, 330f);
        feedRT.anchoredPosition = new Vector2(0f, 64f);
        RawImage rawImg = cameraFeedObj.AddComponent<RawImage>();
        rawImg.color = Color.white;

        GameObject reticleOverlay = CreateUIObject("ReticleOverlay", cameraFeedObj.transform);
        RectTransform reticleRT = reticleOverlay.GetComponent<RectTransform>();
        StretchFull(reticleRT);

        float defaultSpread = 56f;
        float defaultBoxSize = 46f;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                float xPos = (col - 1) * defaultSpread;
                float yPos = (1 - row) * defaultSpread - 25f;

                GameObject box = CreateUIObject($"Reticle_{index}", reticleOverlay.transform);
                RectTransform boxRT = box.GetComponent<RectTransform>();
                boxRT.sizeDelta = new Vector2(defaultBoxSize, defaultBoxSize);
                boxRT.anchoredPosition = new Vector2(xPos, yPos);

                Image boxImg = box.AddComponent<Image>();
                boxImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusSm, 4);
                boxImg.type = Image.Type.Sliced;
                boxImg.color = new Color(1f, 1f, 1f, 0.85f);

                TextMeshProUGUI boxLabel = CreateTMP("ColorLabel", box.transform, (index == 4) ? "?" : "", 18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                StretchFull(boxLabel.GetComponent<RectTransform>());

                GameObject dot = CreateUIObject("CenterDot", box.transform);
                dot.GetComponent<RectTransform>().sizeDelta = new Vector2(7f, 7f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.sprite = RubikUISpriteFactory.GetCircleSprite();
                dotImg.color = (index == 4) ? RubikUITheme.Yellow : new Color(1f, 1f, 1f, 0.65f);
            }
        }

        TextMeshProUGUI distanceLabelTMP = CreateTMP("DistanceLabelText", leftContainer.transform, "Cự ly ngắm: <b>Vừa (Cầm tay 25-40cm)</b>", 13.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Center);
        RectTransform dlRT = distanceLabelTMP.GetComponent<RectTransform>();
        dlRT.sizeDelta = new Vector2(350f, 22f);
        dlRT.anchoredPosition = new Vector2(0f, -124f);

        Slider sliderDist = CreateModernSlider("SliderDistance", leftContainer.transform, 36f, 88f, 56f);
        RectTransform sdRT = sliderDist.GetComponent<RectTransform>();
        sdRT.sizeDelta = new Vector2(310f, 22f);
        sdRT.anchoredPosition = new Vector2(0f, -148f);

        // Hàng nút dịch khung ngắm (Nudge)
        GameObject nudgeRow = CreateUIObject("NudgeRow", leftContainer.transform);
        RectTransform nrRT = nudgeRow.GetComponent<RectTransform>();
        nrRT.sizeDelta = new Vector2(340f, 30f);
        nrRT.anchoredPosition = new Vector2(0f, -180f);

        HorizontalLayoutGroup nudgeHlg = nudgeRow.AddComponent<HorizontalLayoutGroup>();
        nudgeHlg.spacing = 8f;
        nudgeHlg.childAlignment = TextAnchor.MiddleCenter;
        nudgeHlg.childControlWidth = false;
        nudgeHlg.childControlHeight = false;

        Button btnNudgeLeft = CreateSimplePillButton(nudgeRow.transform, "BtnNudgeLeft", "<", RubikUITheme.Slate200, RubikUITheme.Slate800, 14f, hasBorder: false);
        btnNudgeLeft.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 28f);

        Button btnNudgeUp = CreateSimplePillButton(nudgeRow.transform, "BtnNudgeUp", "^", RubikUITheme.Slate200, RubikUITheme.Slate800, 15f, hasBorder: false);
        btnNudgeUp.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 28f);

        Button btnNudgeDown = CreateSimplePillButton(nudgeRow.transform, "BtnNudgeDown", "v", RubikUITheme.Slate200, RubikUITheme.Slate800, 13.5f, hasBorder: false);
        btnNudgeDown.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 28f);

        Button btnNudgeRight = CreateSimplePillButton(nudgeRow.transform, "BtnNudgeRight", ">", RubikUITheme.Slate200, RubikUITheme.Slate800, 14f, hasBorder: false);
        btnNudgeRight.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 28f);

        Button btnResetNudge = CreateSimplePillButton(nudgeRow.transform, "BtnResetReticlePos", "Về tâm", RubikUITheme.Blue100, RubikUITheme.Blue700, 13f, hasBorder: false);
        btnResetNudge.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 28f);

        // Hàng nút điều khiển Camera
        GameObject camControlsRow = CreateUIObject("CamControlsRow", leftContainer.transform);
        RectTransform ccrRT = camControlsRow.GetComponent<RectTransform>();
        ccrRT.sizeDelta = new Vector2(368f, 34f);
        ccrRT.anchoredPosition = new Vector2(0f, -216f);

        HorizontalLayoutGroup camHlg = camControlsRow.AddComponent<HorizontalLayoutGroup>();
        camHlg.spacing = 8f;
        camHlg.childAlignment = TextAnchor.MiddleCenter;
        camHlg.childControlWidth = false;
        camHlg.childControlHeight = false;

        Button btnSwitch = CreateSimplePillButton(camControlsRow.transform, "BtnSwitchCamera", "Đổi Cam", RubikUITheme.Slate200, RubikUITheme.Slate800, 13.5f, hasBorder: false);
        btnSwitch.GetComponent<RectTransform>().sizeDelta = new Vector2(108f, 32f);

        Button btnFlipH = CreateSimplePillButton(camControlsRow.transform, "BtnFlipHorizontal", "Gương: BẬT", new Color32(0x10, 0xB9, 0x81, 255), Color.white, 13.5f, hasBorder: false);
        btnFlipH.GetComponent<RectTransform>().sizeDelta = new Vector2(124f, 32f);

        Button btnFlipV = CreateSimplePillButton(camControlsRow.transform, "BtnFlipVertical", "Dọc: CHUẨN", RubikUITheme.Slate200, RubikUITheme.Slate700, 13.5f, hasBorder: false);
        btnFlipV.GetComponent<RectTransform>().sizeDelta = new Vector2(114f, 32f);

        TextMeshProUGUI cameraInfoTMP = CreateTMP("CameraInfoText", leftContainer.transform, "Camera: Tự động nhận diện thiết bị", 12f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Center);
        RectTransform ciRT = cameraInfoTMP.GetComponent<RectTransform>();
        ciRT.sizeDelta = new Vector2(360f, 18f);
        ciRT.anchoredPosition = new Vector2(0f, -242f);

        // --- CỘT PHẢI: HƯỚNG DẪN MẶT, LƯỚI XEM TRƯỚC 3X3 & ĐỒNG BỘ ---
        GameObject rightContainer = CreateRoundedPanel("RightContainer", card.transform, RubikUITheme.Slate50, RubikUITheme.RadiusLg, addShadow: false);
        RectTransform rightRT = rightContainer.GetComponent<RectTransform>();
        rightRT.sizeDelta = new Vector2(408f, 510f);
        rightRT.anchoredPosition = new Vector2(208f, -34f);

        GameObject rightBorder = CreateUIObject("Border", rightContainer.transform);
        StretchFull(rightBorder.GetComponent<RectTransform>());
        Image rbImg = rightBorder.AddComponent<Image>();
        rbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusLg, 2);
        rbImg.type = Image.Type.Sliced;
        rbImg.color = RubikUITheme.Slate200;
        rbImg.raycastTarget = false;

        // Thẻ hướng dẫn mặt hiện tại nền xanh nhạt
        GameObject guideBanner = CreateRoundedPanel("GuideBanner", rightContainer.transform, RubikUITheme.Blue50, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform gbRT = guideBanner.GetComponent<RectTransform>();
        gbRT.sizeDelta = new Vector2(376f, 76f);
        gbRT.anchoredPosition = new Vector2(0f, 202f);

        TextMeshProUGUI faceTitleTMP = CreateTMP("FaceTitleText", rightContainer.transform, "MẶT 1/6: MẶT F (TRƯỚC)", 17.5f, FontStyles.Bold, RubikUITheme.Blue700, TextAlignmentOptions.Center);
        RectTransform ftRT = faceTitleTMP.GetComponent<RectTransform>();
        ftRT.sizeDelta = new Vector2(356f, 26f);
        ftRT.anchoredPosition = new Vector2(0f, 220f);

        TextMeshProUGUI faceGuideTMP = CreateTMP("FaceGuideText", rightContainer.transform, "1. Hướng mặt bất kỳ vào camera làm mặt TRƯỚC (F). Nhấn phím ENTER để chụp.", 13f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Center, wordWrapping: true);
        RectTransform fgRT = faceGuideTMP.GetComponent<RectTransform>();
        fgRT.sizeDelta = new Vector2(356f, 38f);
        fgRT.anchoredPosition = new Vector2(0f, 186f);

        // Huy hiệu 6 mặt: F, R, B, L, U, D
        GameObject faceBadgesObj = CreateUIObject("FaceBadgesContainer", rightContainer.transform);
        RectTransform fbRT = faceBadgesObj.GetComponent<RectTransform>();
        fbRT.sizeDelta = new Vector2(360f, 30f);
        fbRT.anchoredPosition = new Vector2(0f, 138f);

        HorizontalLayoutGroup badgeHlg = faceBadgesObj.AddComponent<HorizontalLayoutGroup>();
        badgeHlg.spacing = 8f;
        badgeHlg.childAlignment = TextAnchor.MiddleCenter;
        badgeHlg.childControlWidth = false;
        badgeHlg.childControlHeight = false;

        string[] badgeNames = { "F", "R", "B", "L", "U", "D" };
        for (int i = 0; i < 6; i++)
        {
            Color badgeCol = (i == 0) ? (Color)RubikUITheme.Blue600 : (Color)RubikUITheme.Slate200;
            Color txtCol = (i == 0) ? Color.white : (Color)RubikUITheme.Slate600;
            GameObject badge = CreateRoundedPanel($"Badge_{badgeNames[i]}", faceBadgesObj.transform, badgeCol, RubikUITheme.RadiusSm, addShadow: false);
            badge.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 28f);

            TextMeshProUGUI bTxt = CreateTMP("Text", badge.transform, badgeNames[i], 14f, FontStyles.Bold, txtCol, TextAlignmentOptions.Center);
            StretchFull(bTxt.GetComponent<RectTransform>());
        }

        // Khung đế tối chứa 9 ô xem trước (PreviewGridContainer)
        GameObject previewFrame = CreateRoundedPanel("PreviewFrame", rightContainer.transform, RubikUITheme.Slate800, RubikUITheme.RadiusMd, addShadow: false);
        RectTransform pfRT = previewFrame.GetComponent<RectTransform>();
        pfRT.sizeDelta = new Vector2(142f, 142f);
        pfRT.anchoredPosition = new Vector2(0f, 40f);

        GameObject previewGridObj = CreateUIObject("PreviewGridContainer", rightContainer.transform);
        RectTransform pgRT = previewGridObj.GetComponent<RectTransform>();
        pgRT.sizeDelta = new Vector2(132f, 132f);
        pgRT.anchoredPosition = new Vector2(0f, 40f);

        GridLayoutGroup grid = previewGridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(38f, 38f);
        grid.spacing = new Vector2(5f, 5f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
        {
            GameObject cell = CreateRoundedPanel($"Cell_{i}", previewGridObj.transform, (i == 4) ? Color.white : new Color(0.88f, 0.90f, 0.94f), RubikUITheme.RadiusXs, addShadow: false);
            cell.AddComponent<Button>();
        }

        TextMeshProUGUI hintTMP = CreateTMP("PreviewHintText", rightContainer.transform, "Nhấn trực tiếp vào từng ô trên lưới 3x3 để sửa màu thủ công", 12f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Center);
        RectTransform hRT = hintTMP.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(360f, 18f);
        hRT.anchoredPosition = new Vector2(0f, -45f);

        // Nút Chụp mặt này
        Button btnCapture = CreateSimplePillButton(rightContainer.transform, "BtnCaptureFace", "CHỤP MẶT NÀY (ENTER)", RubikUITheme.Success, Color.white, 16f, hasBorder: false);
        RectTransform capRT = btnCapture.GetComponent<RectTransform>();
        capRT.sizeDelta = new Vector2(360f, 42f);
        capRT.anchoredPosition = new Vector2(0f, -78f);

        // Hàng nút điều hướng mặt (Trước / Quét lại / Tiếp)
        GameObject navRow = CreateUIObject("NavRow", rightContainer.transform);
        RectTransform navRowRT = navRow.GetComponent<RectTransform>();
        navRowRT.sizeDelta = new Vector2(360f, 34f);
        navRowRT.anchoredPosition = new Vector2(0f, -122f);

        HorizontalLayoutGroup navHlg = navRow.AddComponent<HorizontalLayoutGroup>();
        navHlg.spacing = 10f;
        navHlg.childAlignment = TextAnchor.MiddleCenter;
        navHlg.childControlWidth = false;
        navHlg.childControlHeight = false;

        Button btnPrev = CreateSimplePillButton(navRow.transform, "BtnPrevFace", "< Trước", RubikUITheme.Slate200, RubikUITheme.Slate800, 14f, hasBorder: false);
        btnPrev.GetComponent<RectTransform>().sizeDelta = new Vector2(106f, 34f);

        Button btnRescan = CreateSimplePillButton(navRow.transform, "BtnRescanFace", "Quét lại", new Color32(0xFE, 0xF3, 0xC7, 255), new Color32(0xB4, 0x53, 0x09, 255), 14f, hasBorder: false);
        btnRescan.GetComponent<RectTransform>().sizeDelta = new Vector2(128f, 34f);

        Button btnNext = CreateSimplePillButton(navRow.transform, "BtnNextFace", "Tiếp >", RubikUITheme.Slate200, RubikUITheme.Slate800, 14f, hasBorder: false);
        btnNext.GetComponent<RectTransform>().sizeDelta = new Vector2(106f, 34f);

        // Hai Toggle cài đặt nâng cao
        Toggle toggleCalibrate = CreateModernToggle(rightContainer.transform, "ToggleAutoCalibrate", "Tự động cân bằng ánh sáng (Auto-Calibrate)", new Vector2(0f, -155f), true);
        Toggle toggleCustomMat = CreateModernToggle(rightContainer.transform, "ToggleCustomMaterialSync", "Đồng bộ chất liệu màu thực tế (Custom Material)", new Vector2(0f, -180f), true);

        // Dòng trạng thái StatusText
        TextMeshProUGUI statusTMP = CreateTMP("StatusText", rightContainer.transform, "Sẵn sàng quét. Hãy căn thẳng khối Rubik!", 13f, FontStyles.Bold, RubikUITheme.Sky600, TextAlignmentOptions.Center);
        RectTransform stRT = statusTMP.GetComponent<RectTransform>();
        stRT.sizeDelta = new Vector2(376f, 22f);
        stRT.anchoredPosition = new Vector2(0f, -203f);

        // Nút Đồng bộ lên khối 3D
        Button btnApply = CreateSimplePillButton(rightContainer.transform, "BtnApplyToCube", "ĐỒNG BỘ LÊN KHỐI 3D", RubikUITheme.Blue600, Color.white, 16f, hasBorder: false);
        RectTransform applyRT = btnApply.GetComponent<RectTransform>();
        applyRT.sizeDelta = new Vector2(360f, 42f);
        applyRT.anchoredPosition = new Vector2(0f, -231f);

        // Gắn tham chiếu vào RubikCameraScanner
        GameObject rm = GameObject.Find("RubikManager");
        RubikCameraScanner scanner = uiCtrl != null ? uiCtrl.cameraScanner : null;
        if (scanner == null && rm != null)
        {
            scanner = rm.GetComponent<RubikCameraScanner>();
            if (scanner == null)
                scanner = rm.AddComponent<RubikCameraScanner>();
        }

        if (scanner != null)
        {
            scanner.scannerPanel = backdrop;
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
            scanner.btnApplyToCube = btnApply;
            scanner.btnClose = btnClose;
            scanner.toggleAutoCalibrate = toggleCalibrate;
            scanner.toggleCustomMaterialSync = toggleCustomMat;
            scanner.sliderDistance = sliderDist;
            scanner.distanceLabelText = distanceLabelTMP;
            scanner.btnNudgeUp = btnNudgeUp;
            scanner.btnNudgeDown = btnNudgeDown;
            scanner.btnNudgeLeft = btnNudgeLeft;
            scanner.btnNudgeRight = btnNudgeRight;
            scanner.btnResetReticlePos = btnResetNudge;

            if (uiCtrl != null)
                uiCtrl.cameraScanner = scanner;
        }

        backdrop.SetActive(false);
    }

    // =========================================================================
    // 12. HỘP THOẠI TÔ MÀU KHỐI RUBIK 2D (FRIENDLY PUZZLE STYLE)
    // =========================================================================
    public static void BuildNetColorPickerModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        Transform existing = canvasTr.Find("NetColorPickerPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject backdrop = CreateUIObject("NetColorPickerPanel", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(RubikUITheme.ScrimStrong);

        GameObject card = CreateRoundedPanel("NetCard", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(840f, 610f);

        // --- HEADER ---
        GameObject headerObj = CreateUIObject("Header", card.transform);
        RectTransform headerRT = headerObj.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(-56f, 60f);
        headerRT.anchoredPosition = new Vector2(0f, -20f);

        GameObject iconBadge = CreateRoundedPanel("HeaderIconBadge", headerObj.transform, new Color32(0xF3, 0xE8, 0xFF, 255), RubikUITheme.RadiusMd, addShadow: false);
        RectTransform ibRT = iconBadge.GetComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0f, 0.5f);
        ibRT.anchorMax = new Vector2(0f, 0.5f);
        ibRT.pivot = new Vector2(0f, 0.5f);
        ibRT.sizeDelta = new Vector2(46f, 46f);
        ibRT.anchoredPosition = Vector2.zero;
        CreateIconChild(iconBadge.transform, RubikUISpriteFactory.IconType.Grid2D, RubikUITheme.Violet, new Vector2(24f, 24f));

        TextMeshProUGUI titleTMP = CreateTMP("TitleText", headerObj.transform, "TÔ MÀU KHỐI RUBIK 2D", 22f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform tRT = titleTMP.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.5f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(58f, -2f);
        tRT.offsetMax = new Vector2(-56f, 0f);

        TextMeshProUGUI subTMP = CreateTMP("SubtitleText", headerObj.transform, "Chọn màu ở bảng bên phải rồi nhấp vào từng ô trên sơ đồ chữ thập để tô màu", 13.5f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Left);
        RectTransform sRT = subTMP.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 0.5f);
        sRT.offsetMin = new Vector2(58f, 0f);
        sRT.offsetMax = new Vector2(-56f, 2f);

        GameObject closeTopObj = CreateRoundedPanel("BtnCloseTop", headerObj.transform, RubikUITheme.Slate100, RubikUITheme.RadiusSm, addShadow: false);
        RectTransform closeTopRT = closeTopObj.GetComponent<RectTransform>();
        closeTopRT.anchorMin = new Vector2(1f, 0.5f);
        closeTopRT.anchorMax = new Vector2(1f, 0.5f);
        closeTopRT.pivot = new Vector2(1f, 0.5f);
        closeTopRT.sizeDelta = new Vector2(42f, 42f);
        closeTopRT.anchoredPosition = Vector2.zero;
        Button btnCloseTop = closeTopObj.AddComponent<Button>();
        ApplySubtleButtonTransition(btnCloseTop, RubikUITheme.Slate100);
        CreateIconChild(closeTopObj.transform, RubikUISpriteFactory.IconType.Close, RubikUITheme.Slate600, new Vector2(20f, 20f));

        // --- KHUNG SƠ ĐỒ CHỮ THẬP 54 Ô BÊN TRÁI ---
        GameObject netBoardCard = CreateRoundedPanel("NetBoardCard", card.transform, RubikUITheme.Slate50, RubikUITheme.RadiusLg, addShadow: false);
        RectTransform nbRT = netBoardCard.GetComponent<RectTransform>();
        nbRT.sizeDelta = new Vector2(540f, 366f);
        nbRT.anchoredPosition = new Vector2(-122f, 16f);

        GameObject nbBorder = CreateUIObject("Border", netBoardCard.transform);
        StretchFull(nbBorder.GetComponent<RectTransform>());
        Image nbbImg = nbBorder.AddComponent<Image>();
        nbbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusLg, 2);
        nbbImg.type = Image.Type.Sliced;
        nbbImg.color = RubikUITheme.Slate200;
        nbbImg.raycastTarget = false;

        GameObject netContainerObj = CreateUIObject("NetContainer", netBoardCard.transform);
        RectTransform ncRT = netContainerObj.GetComponent<RectTransform>();
        ncRT.sizeDelta = new Vector2(460f, 340f);
        ncRT.anchoredPosition = Vector2.zero;

        // --- KHUNG BẢNG CHỌN MÀU BÊN PHẢI ---
        GameObject paletteCard = CreateRoundedPanel("PaletteContainer", card.transform, RubikUITheme.Slate50, RubikUITheme.RadiusLg, addShadow: false);
        RectTransform pcRT = paletteCard.GetComponent<RectTransform>();
        pcRT.sizeDelta = new Vector2(226f, 366f);
        pcRT.anchoredPosition = new Vector2(279f, 16f);

        GameObject pcBorder = CreateUIObject("Border", paletteCard.transform);
        StretchFull(pcBorder.GetComponent<RectTransform>());
        Image pcbImg = pcBorder.AddComponent<Image>();
        pcbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusLg, 2);
        pcbImg.type = Image.Type.Sliced;
        pcbImg.color = RubikUITheme.Slate200;
        pcbImg.raycastTarget = false;

        VerticalLayoutGroup vlg = paletteCard.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 16, 16);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        TextMeshProUGUI paletteLabel = CreateTMP("PaletteLabel", paletteCard.transform, "BẢNG CHỌN MÀU", 14.5f, FontStyles.Bold, RubikUITheme.Slate700, TextAlignmentOptions.Center);
        paletteLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(188f, 24f);

        Button btnWhite = CreatePaletteColorButton(paletteCard.transform, "BtnWhite", "Trắng (W)", new Color(0.96f, 0.96f, 0.96f), RubikUITheme.Slate900);
        Button btnYellow = CreatePaletteColorButton(paletteCard.transform, "BtnYellow", "Vàng (Y)", new Color(1.00f, 0.84f, 0.00f), RubikUITheme.Slate900);
        Button btnGreen = CreatePaletteColorButton(paletteCard.transform, "BtnGreen", "Xanh lá (G)", new Color(0.00f, 0.65f, 0.28f), Color.white);
        Button btnBlue = CreatePaletteColorButton(paletteCard.transform, "BtnBlue", "Xanh dương (B)", new Color(0.00f, 0.35f, 0.85f), Color.white);
        Button btnRed = CreatePaletteColorButton(paletteCard.transform, "BtnRed", "Đỏ (R)", new Color(0.85f, 0.12f, 0.15f), Color.white);
        Button btnOrange = CreatePaletteColorButton(paletteCard.transform, "BtnOrange", "Cam (O)", new Color(1.00f, 0.40f, 0.00f), Color.white);

        // --- THANH ĐẾM MÀU & TRẠNG THÁI ---
        GameObject countPill = CreateRoundedPanel("CountInfoPill", card.transform, RubikUITheme.Slate100, RubikUITheme.RadiusSm, addShadow: false);
        RectTransform cpRT = countPill.GetComponent<RectTransform>();
        cpRT.sizeDelta = new Vector2(784f, 32f);
        cpRT.anchoredPosition = new Vector2(0f, -190f);

        TextMeshProUGUI countInfoTMP = CreateTMP("CountInfoText", countPill.transform,
            "Trắng: <b>9/9</b>   •   Vàng: <b>9/9</b>   •   Xanh lá: <b>9/9</b>   •   Xanh dương: <b>9/9</b>   •   Đỏ: <b>9/9</b>   •   Cam: <b>9/9</b>",
            13.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Center);
        StretchFull(countInfoTMP.GetComponent<RectTransform>());

        TextMeshProUGUI statusTMP = CreateTMP("StatusText", card.transform, "Khối Rubik 100% hợp lệ! Sẵn sàng đồng bộ lên 3D.", 15f, FontStyles.Bold, RubikUITheme.Success, TextAlignmentOptions.Center);
        RectTransform stRT = statusTMP.GetComponent<RectTransform>();
        stRT.sizeDelta = new Vector2(784f, 24f);
        stRT.anchoredPosition = new Vector2(0f, -222f);

        // --- HÀNG NÚT HÀNH ĐỘNG DƯỚI ĐÁY ---
        GameObject actionsObj = CreateUIObject("ActionsContainer", card.transform);
        RectTransform actRT = actionsObj.GetComponent<RectTransform>();
        actRT.sizeDelta = new Vector2(784f, 48f);
        actRT.anchoredPosition = new Vector2(0f, -264f);

        HorizontalLayoutGroup actHlg = actionsObj.AddComponent<HorizontalLayoutGroup>();
        actHlg.spacing = 16f;
        actHlg.childAlignment = TextAnchor.MiddleCenter;
        actHlg.childControlWidth = false;
        actHlg.childControlHeight = false;

        Button btnApply = CreateSimplePillButton(actionsObj.transform, "BtnApplyToCube", "Đồng bộ lên 3D", RubikUITheme.Success, Color.white, 16.5f, hasBorder: false);
        btnApply.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 46f);

        Button btnReset = CreateSimplePillButton(actionsObj.transform, "BtnResetSolved", "Khôi phục chuẩn", RubikUITheme.Slate200, RubikUITheme.Slate800, 16f, hasBorder: false);
        btnReset.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 46f);

        Button btnClose = CreateSimplePillButton(actionsObj.transform, "BtnClose", "Đóng", RubikUITheme.DangerBg, RubikUITheme.Danger, 16f, hasBorder: false);
        btnClose.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 46f);

        // Gắn tham chiếu vào RubikNetColorPicker
        GameObject rm = GameObject.Find("RubikManager");
        RubikNetColorPicker picker = uiCtrl != null ? uiCtrl.colorPicker : null;
        if (picker == null && rm != null)
        {
            picker = rm.GetComponent<RubikNetColorPicker>();
            if (picker == null)
                picker = rm.AddComponent<RubikNetColorPicker>();
        }

        if (picker != null)
        {
            picker.netPanel = backdrop;
            picker.netContainer = netContainerObj.transform;
            picker.statusText = statusTMP;
            picker.countInfoText = countInfoTMP;
            picker.btnApplyToCube = btnApply;
            picker.btnResetToSolved = btnReset;
            picker.btnClose = btnClose;
            picker.btnCloseTop = btnCloseTop;
            picker.btnColorWhite = btnWhite;
            picker.btnColorYellow = btnYellow;
            picker.btnColorGreen = btnGreen;
            picker.btnColorBlue = btnBlue;
            picker.btnColorRed = btnRed;
            picker.btnColorOrange = btnOrange;

            if (uiCtrl != null)
                uiCtrl.colorPicker = picker;
        }

        backdrop.SetActive(false);
    }

    private static Button CreatePaletteColorButton(Transform parent, string name, string label, Color bgCol, Color fgCol)
    {
        GameObject obj = CreateRoundedPanel(name, parent, bgCol, RubikUITheme.RadiusSm, addShadow: false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(188f, 42f);

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = RubikUITheme.Ink(0.14f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = obj.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, bgCol);

        TextMeshProUGUI lbl = CreateTMP("Text", obj.transform, label, 15f, FontStyles.Bold, fgCol, TextAlignmentOptions.Center);
        StretchFull(lbl.GetComponent<RectTransform>());
        return btn;
    }

    private static Toggle CreateModernToggle(Transform parent, string name, string labelText, Vector2 pos, bool defaultOn)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360f, 24f);
        rt.anchoredPosition = pos;

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.isOn = defaultOn;

        GameObject bgObj = CreateRoundedPanel("Background", obj.transform, RubikUITheme.Slate200, 6, addShadow: false);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(0f, 0.5f);
        bgRT.pivot = new Vector2(0f, 0.5f);
        bgRT.sizeDelta = new Vector2(20f, 20f);
        bgRT.anchoredPosition = new Vector2(4f, 0f);

        GameObject checkObj = CreateRoundedPanel("Checkmark", bgObj.transform, RubikUITheme.Blue600, 6, addShadow: false);
        StretchFull(checkObj.GetComponent<RectTransform>());
        CreateIconChild(checkObj.transform, RubikUISpriteFactory.IconType.Check, Color.white, new Vector2(14f, 14f));

        toggle.targetGraphic = bgObj.GetComponent<Image>();
        toggle.graphic = checkObj.GetComponent<Image>();

        TextMeshProUGUI lbl = CreateTMP("Label", obj.transform, labelText, 12.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Left);
        RectTransform lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 0f);
        lRT.anchorMax = new Vector2(1f, 1f);
        lRT.offsetMin = new Vector2(32f, 0f);
        lRT.offsetMax = Vector2.zero;

        return toggle;
    }

    // =========================================================================
    // 11. HỘP THOẠI XÁC NHẬN ĐẶT LẠI (RESET CONFIRMATION)
    // =========================================================================
    private static void BuildConfirmResetModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject backdrop = CreateUIObject("ConfirmResetModal", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(RubikUITheme.ScrimLight);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(440f, 230f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "Xác nhận đặt lại khối Rubik?", 24f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Center);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(-40f, 40f);
        tRT.anchoredPosition = new Vector2(0f, -24f);

        TextMeshProUGUI desc = CreateTMP("Desc", card.transform, "Tiến trình xoay hoặc thời gian hiện tại sẽ được đưa về trạng thái đã giải ban đầu.", 15f, FontStyles.Normal, RubikUITheme.Slate600, TextAlignmentOptions.Center, wordWrapping: true);
        RectTransform dRT = desc.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0f, 0.5f);
        dRT.anchorMax = new Vector2(1f, 0.5f);
        dRT.sizeDelta = new Vector2(-56f, 64f);
        dRT.anchoredPosition = new Vector2(0f, 10f);

        GameObject row = CreateUIObject("Row", card.transform);
        RectTransform rRT = row.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0f, 0f);
        rRT.anchorMax = new Vector2(1f, 0f);
        rRT.pivot = new Vector2(0.5f, 0f);
        rRT.sizeDelta = new Vector2(-48f, 52f);
        rRT.anchoredPosition = new Vector2(0f, 22f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        uiCtrl.btnConfirmResetCancel = CreateSimplePillButton(row.transform, "BtnCancel", "Hủy", RubikUITheme.Slate200, RubikUITheme.Slate800, 17f, hasBorder: false);
        uiCtrl.btnConfirmResetYes = CreateSimplePillButton(row.transform, "BtnConfirm", "Đặt lại ngay", RubikUITheme.DangerLight, Color.white, 17f, hasBorder: false);
        uiCtrl.confirmResetPanel = backdrop;

        backdrop.SetActive(false);
    }

    // =========================================================================
    // 12. HỘP THOẠI CÀI ĐẶT (SETTINGS MODAL)
    // =========================================================================
    private static void BuildSettingsModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject backdrop = CreateUIObject("SettingsModal", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(RubikUITheme.ScrimLight);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(520f, 410f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "CÀI ĐẶT ỨNG DỤNG", 24f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Left);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.offsetMin = new Vector2(28f, -60f);
        tRT.offsetMax = new Vector2(-28f, -20f);

        // Dòng 1: Tốc độ xoay
        CreateSettingsSliderRow(card.transform, "RowSpeed", "Tốc độ hoạt ảnh xoay mặt:", -90f, 0.05f, 0.60f, 0.22f,
            out uiCtrl.settingsSpeedSlider, out uiCtrl.settingsSpeedText);

        // Dòng 2: Độ nhạy Camera
        CreateSettingsSliderRow(card.transform, "RowCam", "Độ nhạy xoay góc nhìn Camera:", -165f, 5f, 35f, 15f,
            out uiCtrl.settingsCamSensitivitySlider, out uiCtrl.settingsCamSensitivityText);

        // Dòng 3: Số bước trộn WCA
        CreateSettingsSliderRow(card.transform, "RowScramble", "Số bước trộn khối chuẩn WCA:", -240f, 10f, 25f, 20f,
            out uiCtrl.settingsScrambleCountSlider, out uiCtrl.settingsScrambleCountText);

        // Nút xóa PB & Đóng
        GameObject bottomRow = CreateUIObject("BottomButtons", card.transform);
        RectTransform brRT = bottomRow.GetComponent<RectTransform>();
        brRT.anchorMin = new Vector2(0f, 0f);
        brRT.anchorMax = new Vector2(1f, 0f);
        brRT.pivot = new Vector2(0.5f, 0f);
        brRT.sizeDelta = new Vector2(-56f, 56f);
        brRT.anchoredPosition = new Vector2(0f, 24f);

        HorizontalLayoutGroup hlg = bottomRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        uiCtrl.btnResetPB = CreateSimplePillButton(bottomRow.transform, "BtnResetPB", "Xóa kỷ lục (PB)", RubikUITheme.DangerBg, RubikUITheme.Danger, 16.5f, hasBorder: false);
        uiCtrl.btnCloseSettings = CreateSimplePillButton(bottomRow.transform, "BtnCloseSettings", "Đóng", RubikUITheme.Blue600, Color.white, 17.5f, hasBorder: false);
        uiCtrl.settingsPanel = backdrop;

        backdrop.SetActive(false);
    }

    private static void CreateSettingsSliderRow(
        Transform parent, string name, string label, float yOffset,
        float minVal, float maxVal, float defaultVal,
        out Slider slider, out TextMeshProUGUI valueText)
    {
        GameObject row = CreateUIObject(name, parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-56f, 64f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        TextMeshProUGUI lbl = CreateTMP("Label", row.transform, label, 15.5f, FontStyles.Normal, RubikUITheme.Slate700, TextAlignmentOptions.Left);
        RectTransform lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f);
        lRT.anchorMax = new Vector2(0.75f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.sizeDelta = new Vector2(0f, 26f);
        lRT.anchoredPosition = Vector2.zero;

        valueText = CreateTMP("Val", row.transform, $"{defaultVal:F2}", 15.5f, FontStyles.Bold, RubikUITheme.Blue600, TextAlignmentOptions.Right);
        RectTransform vRT = valueText.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0.75f, 1f);
        vRT.anchorMax = new Vector2(1f, 1f);
        vRT.pivot = new Vector2(1f, 1f);
        vRT.sizeDelta = new Vector2(0f, 26f);
        vRT.anchoredPosition = Vector2.zero;

        slider = CreateModernSlider("Slider", row.transform, minVal, maxVal, defaultVal);
        RectTransform sRT = slider.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 0f);
        sRT.pivot = new Vector2(0.5f, 0f);
        sRT.sizeDelta = new Vector2(0f, 28f);
        sRT.anchoredPosition = new Vector2(0f, 4f);
    }

    // =========================================================================
    // 13. BẢNG CHIẾN THẮNG (VICTORY OVERLAY)
    // =========================================================================
    private static void BuildVictoryModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject backdrop = CreateUIObject("VictoryPanel", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(RubikUITheme.ScrimStrong);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 290f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "HOÀN THÀNH XUẤT SẮC!", 24f, FontStyles.Bold, RubikUITheme.Success, TextAlignmentOptions.Center);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(-32f, 44f);
        tRT.anchoredPosition = new Vector2(0f, -26f);

        uiCtrl.victoryTimeText = CreateTMP("VictoryTimeText", card.transform, "Thời gian: <b>00:00.000</b>", 18.5f, FontStyles.Normal, RubikUITheme.Slate900, TextAlignmentOptions.Center);
        RectTransform vtRT = uiCtrl.victoryTimeText.GetComponent<RectTransform>();
        vtRT.sizeDelta = new Vector2(380f, 36f);
        vtRT.anchoredPosition = new Vector2(0f, 24f);

        uiCtrl.victoryMovesText = CreateTMP("VictoryMovesText", card.transform, "Số nước đi: <b>0</b>", 17.5f, FontStyles.Normal, RubikUITheme.Slate600, TextAlignmentOptions.Center);
        RectTransform vmRT = uiCtrl.victoryMovesText.GetComponent<RectTransform>();
        vmRT.sizeDelta = new Vector2(380f, 34f);
        vmRT.anchoredPosition = new Vector2(0f, -16f);

        uiCtrl.btnPlayAgain = CreateSimplePillButton(card.transform, "BtnPlayAgain", "Chơi ván mới", RubikUITheme.SuccessLight, Color.white, 19f, hasBorder: false);
        RectTransform paRT = uiCtrl.btnPlayAgain.GetComponent<RectTransform>();
        paRT.anchorMin = new Vector2(0.5f, 0f);
        paRT.anchorMax = new Vector2(0.5f, 0f);
        paRT.pivot = new Vector2(0.5f, 0f);
        paRT.sizeDelta = new Vector2(240f, 54f);
        paRT.anchoredPosition = new Vector2(0f, 26f);

        uiCtrl.victoryPanel = backdrop;
        backdrop.SetActive(false);
    }

    // =========================================================================
    // 14. HỘP THOẠI LOADING TÌM HƯỚNG GIẢI NGẮN NHẤT (SOLVER LOADING MODAL)
    // =========================================================================
    public static void BuildSolverLoadingModal(Transform canvasTr, RubikUIController uiCtrl, RubikSolutionViewer viewerOverride = null)
    {
        Transform existing = canvasTr.Find("SolverLoadingModal");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject backdrop = CreateUIObject("SolverLoadingModal", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = RubikUITheme.Ink(0.40f);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, RubikUITheme.RadiusXl, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 215f);

        // Vòng tròn nền xanh nhạt chứa icon mũi tên xoay tròn (Loading Spinner)
        GameObject spinnerBadge = CreateUIObject("SpinnerBadge", card.transform);
        RectTransform sbRT = spinnerBadge.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.5f, 1f);
        sbRT.anchorMax = new Vector2(0.5f, 1f);
        sbRT.pivot = new Vector2(0.5f, 1f);
        sbRT.sizeDelta = new Vector2(68f, 68f);
        sbRT.anchoredPosition = new Vector2(0f, -24f);
        Image sbImg = spinnerBadge.AddComponent<Image>();
        sbImg.sprite = RubikUISpriteFactory.GetCircleSprite();
        sbImg.color = RubikUITheme.Blue100;
        sbImg.raycastTarget = false;

        GameObject spinnerObj = CreateUIObject("LoadingSpinner", spinnerBadge.transform);
        RectTransform spRT = spinnerObj.GetComponent<RectTransform>();
        spRT.sizeDelta = new Vector2(38f, 38f);
        spRT.anchoredPosition = Vector2.zero;
        Image spImg = spinnerObj.AddComponent<Image>();
        spImg.sprite = RubikUISpriteFactory.GetIcon(RubikUISpriteFactory.IconType.Reset);
        spImg.color = RubikUITheme.Blue600;
        spImg.raycastTarget = false;

        TextMeshProUGUI titleTMP = CreateTMP("LoadingTitleText", card.transform,
            "Đang tìm hướng giải ngắn nhất...",
            20.5f, FontStyles.Bold, RubikUITheme.Slate900, TextAlignmentOptions.Center);
        RectTransform tRT = titleTMP.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0f);
        tRT.pivot = new Vector2(0.5f, 0f);
        tRT.sizeDelta = new Vector2(-40f, 36f);
        tRT.anchoredPosition = new Vector2(0f, 62f);

        TextMeshProUGUI subTMP = CreateTMP("LoadingSubText", card.transform,
            "Đang phân tích 54 ô màu bằng thuật toán Kociemba 2-Phase",
            14f, FontStyles.Normal, RubikUITheme.Slate500, TextAlignmentOptions.Center, wordWrapping: true);
        RectTransform sRT = subTMP.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 0f);
        sRT.pivot = new Vector2(0.5f, 0f);
        sRT.sizeDelta = new Vector2(-48f, 36f);
        sRT.anchoredPosition = new Vector2(0f, 24f);

        RubikSolutionViewer viewer = viewerOverride != null
            ? viewerOverride
            : (uiCtrl != null ? uiCtrl.solutionViewer : Object.FindAnyObjectByType<RubikSolutionViewer>(FindObjectsInactive.Include));

        if (viewer != null)
        {
            viewer.loadingPanel = backdrop;
            viewer.loadingSpinner = spRT;
            viewer.loadingTitleText = titleTMP;
            viewer.loadingSubText = subTMP;
        }

        backdrop.SetActive(false);
    }

    // =========================================================================
    // CÁC HÀM TIỆN ÍCH DỰNG UI CHUẨN FRIENDLY PUZZLE
    // =========================================================================
    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject CreateRoundedPanel(string name, Transform parent, Color color, int radius, bool addShadow)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image img = obj.AddComponent<Image>();
        img.sprite = RubikUISpriteFactory.GetRoundedSprite(radius);
        img.type = Image.Type.Sliced;
        img.color = color;

        if (addShadow)
        {
            Shadow sh = obj.AddComponent<Shadow>();
            sh.effectColor = RubikUITheme.Ink(RubikUITheme.ShadowCard);
            sh.effectDistance = new Vector2(0f, -4f);
        }

        return obj;
    }

    private static void CreateIconChild(Transform parent, RubikUISpriteFactory.IconType icon, Color color, Vector2 size)
    {
        GameObject ic = CreateUIObject("Icon", parent);
        RectTransform rt = ic.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = ic.AddComponent<Image>();
        img.sprite = RubikUISpriteFactory.GetIcon(icon);
        img.color = color;
        img.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateTMP(
        string name, Transform parent, string text, float fontSize,
        FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
        bool wordWrapping = false)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = wordWrapping ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = wordWrapping ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Slider CreateModernSlider(string name, Transform parent, float min, float max, float val)
    {
        GameObject sliderObj = CreateUIObject(name, parent);
        Slider slider = sliderObj.AddComponent<Slider>();

        // Track nền xám nhạt
        GameObject bgObj = CreateRoundedPanel("Background", sliderObj.transform, RubikUITheme.Slate200, 6, addShadow: false);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.sizeDelta = new Vector2(0f, 8f);

        // Fill Area màu xanh dương
        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.5f);
        faRT.anchorMax = new Vector2(1f, 0.5f);
        faRT.sizeDelta = new Vector2(-10f, 8f);

        GameObject fill = CreateRoundedPanel("Fill", fillArea.transform, RubikUITheme.Blue500, 6, addShadow: false);
        RectTransform fRT = fill.GetComponent<RectTransform>();
        fRT.sizeDelta = new Vector2(10f, 8f);

        // Handle tròn trắng có bóng đổ nhẹ
        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f);
        haRT.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform hRT = handle.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(22f, 22f);
        Image hImg = handle.AddComponent<Image>();
        hImg.sprite = RubikUISpriteFactory.GetCircleSprite();
        hImg.color = Color.white;

        Shadow hShadow = handle.AddComponent<Shadow>();
        hShadow.effectColor = RubikUITheme.Ink(RubikUITheme.ShadowHandle);
        hShadow.effectDistance = new Vector2(0f, -2f);

        slider.fillRect = fRT;
        slider.handleRect = hRT;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;

        return slider;
    }

    private static void ApplySubtleButtonTransition(Button btn, Color baseColor)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.95f, 0.97f, 1.0f, 1f);
        cb.pressedColor = new Color(0.88f, 0.90f, 0.94f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.82f, 0.84f, 0.88f, 0.55f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
