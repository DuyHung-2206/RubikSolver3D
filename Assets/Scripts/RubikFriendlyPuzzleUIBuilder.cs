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
    private const string UIVersionTag = "FriendlyPuzzleUI_v2";

    public static void EnsureUI(RubikUIController uiCtrl, bool forceRebuild = false)
    {
        if (uiCtrl == null)
            uiCtrl = Object.FindAnyObjectByType<RubikUIController>();
        if (uiCtrl == null) return;

        uiCtrl.FindCoreReferences();

        Canvas canvas = FindOrCreateMainCanvas();
        EnsureEventSystem();

        if (uiCtrl.cameraOrbit != null)
            uiCtrl.cameraOrbit.ApplyFriendlyLightBackground();
        else if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color32(0xEE, 0xF3, 0xF9, 255);
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
        // Ẩn các phần tử cũ không còn phù hợp (như TopBar đen cũ) nhưng giữ nguyên CameraScannerPanel và NetColorPickerPanel
        Transform oldTopBar = canvas.transform.Find("TopBar");
        if (oldTopBar != null) oldTopBar.gameObject.SetActive(false);

        // Xóa khung UI cũ của builder nếu rebuild
        string[] managedRootNames = {
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
            "GuideRotationArrow",
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

        // 8. Nút "⚙ Cài đặt" nhỏ ở góc trên bên phải
        RectTransform topRightSettingsRT = BuildTopRightSettingsButton(canvas.transform, uiCtrl);

        // 9. Hộp thoại NHẬP CÔNG THỨC RUBIK (Ảnh tham chiếu 2)
        RectTransform algCardRT = BuildAlgorithmInputModal(canvas.transform, uiCtrl);

        // 10. Giao diện HƯỚNG DẪN GIẢI từng bước + Mũi tên 3D (Ảnh tham chiếu 3)
        RectTransform solverGuideRT = BuildSolutionGuideModal(canvas.transform, uiCtrl);

        // 11. Hộp thoại Xác nhận Đặt lại (Reset Confirmation)
        BuildConfirmResetModal(canvas.transform, uiCtrl);

        // 12. Hộp thoại Cài đặt (Settings Modal)
        BuildSettingsModal(canvas.transform, uiCtrl);

        // 13. Bảng Chiến thắng (Victory Overlay)
        BuildVictoryModal(canvas.transform, uiCtrl);

        // Đưa NetColorPickerPanel và CameraScannerPanel lên trên cùng để hiển thị dạng Modal khi mở
        Transform netPanel = canvas.transform.Find("NetColorPickerPanel");
        if (netPanel != null) netPanel.SetAsLastSibling();

        Transform camScannerPanel = canvas.transform.Find("CameraScannerPanel");
        if (camScannerPanel != null) camScannerPanel.SetAsLastSibling();

        // Đảm bảo ConfirmReset, Settings, AlgorithmInput ở lớp trên cùng
        if (uiCtrl.algorithmInput != null && uiCtrl.algorithmInput.inputPanel != null)
            uiCtrl.algorithmInput.inputPanel.transform.SetAsLastSibling();
        if (uiCtrl.settingsPanel != null)
            uiCtrl.settingsPanel.transform.SetAsLastSibling();
        if (uiCtrl.confirmResetPanel != null)
            uiCtrl.confirmResetPanel.transform.SetAsLastSibling();
        if (uiCtrl.victoryPanel != null)
            uiCtrl.victoryPanel.transform.SetAsLastSibling();

        // 14. Gắn kết bộ điều khiển Responsive tự động cho PC & Điện thoại
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

        TextMeshProUGUI title = CreateTMP("TitleRubik3D", headerGroup.transform, "Rubik 3D", 28, FontStyles.Bold, new Color32(0x1E, 0x29, 0x3B, 255));
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(360, 40);
        titleRT.anchoredPosition = Vector2.zero;

        // Thanh thông tin thời gian & kỷ lục nhỏ gọn bên dưới tiêu đề
        GameObject timerPill = CreateRoundedPanel("TimerPill", headerGroup.transform, new Color(1f, 1f, 1f, 0.85f), 14, addShadow: false);
        RectTransform pillRT = timerPill.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(0.5f, 1f);
        pillRT.anchorMax = new Vector2(0.5f, 1f);
        pillRT.pivot = new Vector2(0.5f, 1f);
        pillRT.sizeDelta = new Vector2(290, 32);
        pillRT.anchoredPosition = new Vector2(0, -44);

        TextMeshProUGUI timerTMP = CreateTMP("TimerText", timerPill.transform, "00:00.000", 15, FontStyles.Bold, new Color32(0x47, 0x55, 0x69, 255));
        RectTransform timerRT = timerTMP.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0f, 0f);
        timerRT.anchorMax = new Vector2(0.5f, 1f);
        timerRT.offsetMin = new Vector2(14, 0);
        timerRT.offsetMax = new Vector2(-4, 0);

        TextMeshProUGUI pbTMP = CreateTMP("PBText", timerPill.transform, "PB: --:--.---", 13, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255));
        RectTransform pbRT = pbTMP.GetComponent<RectTransform>();
        pbRT.anchorMin = new Vector2(0.5f, 0f);
        pbRT.anchorMax = new Vector2(1f, 1f);
        pbRT.offsetMin = new Vector2(4, 0);
        pbRT.offsetMax = new Vector2(-12, 0);

        TextMeshProUGUI scrambleTMP = CreateTMP("ScrambleText", headerGroup.transform, "", 12, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255));
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
        GameObject sidebar = CreateRoundedPanel("SidebarPanel", canvasTr, Color.white, 22, addShadow: true);
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

        TextMeshProUGUI appTitle = CreateTMP("AppTitle", brandGroup.transform, "RubikSolver", 24, FontStyles.Bold, new Color32(0x1E, 0x29, 0x3B, 255), TextAlignmentOptions.Left);
        RectTransform appTitleRT = appTitle.GetComponent<RectTransform>();
        appTitleRT.anchorMin = new Vector2(0f, 0.5f);
        appTitleRT.anchorMax = new Vector2(1f, 0.5f);
        appTitleRT.pivot = new Vector2(0f, 0f);
        appTitleRT.offsetMin = new Vector2(86f, 2f);
        appTitleRT.offsetMax = new Vector2(-12f, 32f);

        TextMeshProUGUI appSub = CreateTMP("AppSubtitle", brandGroup.transform, "Quét • Giải • Hướng dẫn", 13.5f, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255), TextAlignmentOptions.Left);
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
        GameObject exploreCard = CreateRoundedPanel("ExploreCard", sidebar.transform, new Color32(0xE0, 0xF2, 0xFE, 255), 16, addShadow: false);
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

        TextMeshProUGUI expTitle = CreateTMP("ExploreTitle", exploreCard.transform, "Khám phá\nRubik 3D", 14.5f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
        RectTransform expTitleRT = expTitle.GetComponent<RectTransform>();
        expTitleRT.anchorMin = new Vector2(0f, 0.5f);
        expTitleRT.anchorMax = new Vector2(1f, 1f);
        expTitleRT.offsetMin = new Vector2(72f, 2f);
        expTitleRT.offsetMax = new Vector2(-46f, -10f);

        TextMeshProUGUI expDesc = CreateTMP("ExploreDesc", exploreCard.transform, "Xoay, quan sát, tìm hiểu mọi góc độ.", 11.5f, FontStyles.Normal, new Color32(0x47, 0x55, 0x69, 255), TextAlignmentOptions.Left);
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
        expBtnBg.color = new Color32(0x3B, 0x82, 0xF6, 255);

        Button expBtn = expBtnObj.AddComponent<Button>();
        CreateIconChild(expBtnObj.transform, RubikUISpriteFactory.IconType.ArrowRight, Color.white, new Vector2(18f, 18f));
        uiCtrl.btnExplore3D = expBtn;

        return rt;
    }

    private static Button CreateSidebarNavButton(Transform parent, string name, string labelText, RubikUISpriteFactory.IconType iconType)
    {
        GameObject btnObj = CreateRoundedPanel(name, parent, new Color(1f, 1f, 1f, 0f), 14, addShadow: false);
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
        iconImg.color = new Color32(0x64, 0x74, 0x8B, 255);
        iconImg.raycastTarget = false;

        TextMeshProUGUI tmp = CreateTMP("Label", btnObj.transform, labelText, 16.5f, FontStyles.Normal, new Color32(0x33, 0x41, 0x55, 255), TextAlignmentOptions.Left);
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
        GameObject mobileNav = CreateRoundedPanel("MobileBottomNav", canvasTr, Color.white, 18, addShadow: true);
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
        GameObject obj = CreateRoundedPanel(name, parent, new Color(1f, 1f, 1f, 0f), 14, addShadow: false);
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
        iconImg.color = new Color32(0x64, 0x74, 0x8B, 255);
        iconImg.raycastTarget = false;

        TextMeshProUGUI tmp = CreateTMP("Label", obj.transform, label, 15f, FontStyles.Normal, new Color32(0x33, 0x41, 0x55, 255), TextAlignmentOptions.Center);
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
        sh.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.12f);
        sh.effectDistance = new Vector2(0f, -2f);

        Button btn = obj.AddComponent<Button>();
        CreateIconChild(obj.transform, icon, new Color32(0x33, 0x41, 0x55, 255), new Vector2(24f, 24f));
        return btn;
    }

    // =========================================================================
    // 6. BẢNG ĐIỀU KHIỂN PHÍA DƯỚI ("BẢNG ĐIỀU KHIỂN & CHẠY THỬ")
    // =========================================================================
    private static RectTransform BuildBottomControlPanel(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject panel = CreateRoundedPanel("BottomPanel", canvasTr, Color.white, 20, addShadow: true);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(850f, 138f);
        rt.anchoredPosition = new Vector2(-42f, 24f);

        uiCtrl.bottomControlCard = panel;

        TextMeshProUGUI header = CreateTMP("SectionHeader", panel.transform, "BẢNG ĐIỀU KHIỂN & CHẠY THỬ", 14.5f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
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
            new Color32(0x3B, 0x82, 0xF6, 255), Color.white,
            RubikUISpriteFactory.IconType.Shuffle, hasOutline: false);

        uiCtrl.btnStart = CreateBottomActionItem(
            row.transform, "BtnStart", "Bắt đầu",
            new Color32(0x22, 0xC5, 0x5E, 255), Color.white,
            RubikUISpriteFactory.IconType.Play, hasOutline: false);

        uiCtrl.btnUndo = CreateBottomActionItem(
            row.transform, "BtnUndo", "Hoàn tác\n(Undo)",
            new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x47, 0x55, 0x69, 255),
            RubikUISpriteFactory.IconType.Undo, hasOutline: false);

        uiCtrl.btnRedo = CreateBottomActionItem(
            row.transform, "BtnRedo", "Làm lại\n(Redo)",
            new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x47, 0x55, 0x69, 255),
            RubikUISpriteFactory.IconType.Redo, hasOutline: false);

        uiCtrl.btnSolve = CreateBottomActionItem(
            row.transform, "BtnSolve", "Giải tự động",
            new Color32(0xDD, 0xD6, 0xFE, 255), new Color32(0x7C, 0x3A, 0xED, 255),
            RubikUISpriteFactory.IconType.SparkleBrain, hasOutline: false);

        uiCtrl.btnAlgorithm = CreateBottomActionItem(
            row.transform, "BtnAlgorithm", "Xem công thức",
            new Color32(0xEF, 0xF6, 0xFF, 255), new Color32(0x25, 0x63, 0xEB, 255),
            RubikUISpriteFactory.IconType.Book, hasOutline: true);

        uiCtrl.btnReset = CreateBottomActionItem(
            row.transform, "BtnReset", "Đặt lại (Reset)",
            new Color32(0xFE, 0xE2, 0xE2, 255), new Color32(0xEF, 0x44, 0x44, 255),
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
        GameObject btnBox = CreateRoundedPanel(name, itemContainer.transform, boxColor, 14, addShadow: false);
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
            outImg.sprite = RubikUISpriteFactory.GetRoundedSprite(14, 2);
            outImg.type = Image.Type.Sliced;
            outImg.color = new Color32(0x93, 0xC5, 0xFD, 255);
            outImg.raycastTarget = false;
        }

        Button btn = btnBox.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, boxColor);

        CreateIconChild(btnBox.transform, iconType, iconColor, new Vector2(26f, 26f));

        // Nhãn tiếng Việt bên dưới
        TextMeshProUGUI lbl = CreateTMP("Label", itemContainer.transform, labelText, 12.5f, FontStyles.Normal, new Color32(0x1E, 0x29, 0x3B, 255), TextAlignmentOptions.Top);
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
        GameObject rotCard = CreateRoundedPanel("RotationControlCard", rightRoot.transform, Color.white, 22, addShadow: true);
        RectTransform rotRT = rotCard.GetComponent<RectTransform>();
        rotRT.anchorMin = new Vector2(0f, 1f);
        rotRT.anchorMax = new Vector2(1f, 1f);
        rotRT.pivot = new Vector2(0.5f, 1f);
        rotRT.sizeDelta = new Vector2(0f, 488f);
        rotRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI rotTitle = CreateTMP("Title", rotCard.transform, "Điều khiển xoay", 21f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
        RectTransform rotTitleRT = rotTitle.GetComponent<RectTransform>();
        rotTitleRT.anchorMin = new Vector2(0f, 1f);
        rotTitleRT.anchorMax = new Vector2(1f, 1f);
        rotTitleRT.pivot = new Vector2(0.5f, 1f);
        rotTitleRT.offsetMin = new Vector2(22f, -44f);
        rotTitleRT.offsetMax = new Vector2(-22f, -14f);

        TextMeshProUGUI rotSub = CreateTMP("Subtitle", rotCard.transform, "Nhấn phím hoặc bấm nút để xoay mặt", 13f, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255), TextAlignmentOptions.Left);
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
            new Color32(0xFA, 0xCC, 0x15, 255), new Color32(0x0F, 0x17, 0x2A, 255),
            RubikUISpriteFactory.IconType.ArrowUp, out uiCtrl.labelFaceU);

        uiCtrl.btnFaceD = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceD", "D", "Mặt dưới",
            new Color32(0x6E, 0xE7, 0xB7, 255), new Color32(0x0F, 0x17, 0x2A, 255),
            RubikUISpriteFactory.IconType.ArrowDown, out uiCtrl.labelFaceD);

        uiCtrl.btnFaceF = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceF", "F", "Mặt trước",
            new Color32(0x3B, 0x82, 0xF6, 255), Color.white,
            RubikUISpriteFactory.IconType.ArrowRight, out uiCtrl.labelFaceF);

        // Hàng 2: B (Tím), R (Đỏ), L (Cam)
        uiCtrl.btnFaceB = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceB", "B", "Mặt sau",
            new Color32(0x7C, 0x3A, 0xED, 255), Color.white,
            RubikUISpriteFactory.IconType.ArrowLeft, out uiCtrl.labelFaceB);

        uiCtrl.btnFaceR = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceR", "R", "Mặt phải",
            new Color32(0xEF, 0x44, 0x44, 255), Color.white,
            RubikUISpriteFactory.IconType.ArrowRight, out uiCtrl.labelFaceR);

        uiCtrl.btnFaceL = CreateFaceRotateCard(
            gridObj.transform, "BtnFaceL", "L", "Mặt trái",
            new Color32(0xF9, 0x73, 0x16, 255), Color.white,
            RubikUISpriteFactory.IconType.ArrowLeft, out uiCtrl.labelFaceL);

        // Hộp Shift "Xoay ngược chiều" ở dưới thẻ Điều khiển xoay
        GameObject shiftBox = CreateRoundedPanel("BtnShiftToggle", rotCard.transform, new Color32(0xF8, 0xFA, 0xFC, 255), 14, addShadow: false);
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
        sbImg.sprite = RubikUISpriteFactory.GetRoundedSprite(14, 2);
        sbImg.type = Image.Type.Sliced;
        sbImg.color = new Color32(0xE2, 0xE8, 0xF0, 255);
        sbImg.raycastTarget = false;

        uiCtrl.btnShiftToggle = shiftBox.AddComponent<Button>();

        GameObject keycap = CreateRoundedPanel("ShiftKeycap", shiftBox.transform, new Color32(0xE2, 0xE8, 0xF0, 255), 10, addShadow: false);
        RectTransform keyRT = keycap.GetComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 0.5f);
        keyRT.anchorMax = new Vector2(0f, 0.5f);
        keyRT.pivot = new Vector2(0f, 0.5f);
        keyRT.sizeDelta = new Vector2(60f, 38f);
        keyRT.anchoredPosition = new Vector2(14f, 0f);

        uiCtrl.shiftBadgeBg = keycap.GetComponent<Image>();
        uiCtrl.shiftBadgeText = CreateTMP("KeyText", keycap.transform, "Shift", 14f, FontStyles.Bold, new Color32(0x47, 0x55, 0x69, 255));
        StretchFull(uiCtrl.shiftBadgeText.GetComponent<RectTransform>());

        TextMeshProUGUI shiftTitle = CreateTMP("ShiftTitle", shiftBox.transform, "Xoay ngược chiều", 15f, FontStyles.Bold, new Color32(0x1E, 0x29, 0x3B, 255), TextAlignmentOptions.Left);
        RectTransform stRT = shiftTitle.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0.5f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.offsetMin = new Vector2(88f, 0f);
        stRT.offsetMax = new Vector2(-12f, -10f);

        TextMeshProUGUI shiftSub = CreateTMP("ShiftSub", shiftBox.transform, "Giữ Shift + phím tương ứng", 12.5f, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255), TextAlignmentOptions.Left);
        RectTransform ssRT = shiftSub.GetComponent<RectTransform>();
        ssRT.anchorMin = new Vector2(0f, 0f);
        ssRT.anchorMax = new Vector2(1f, 0.5f);
        ssRT.offsetMin = new Vector2(88f, 10f);
        ssRT.offsetMax = new Vector2(-12f, 0f);

        // --- THẺ 2: Tốc độ xoay ---
        GameObject speedCard = CreateRoundedPanel("SpeedControlCard", rightRoot.transform, Color.white, 20, addShadow: true);
        RectTransform speedRT = speedCard.GetComponent<RectTransform>();
        speedRT.anchorMin = new Vector2(0f, 0f);
        speedRT.anchorMax = new Vector2(1f, 0f);
        speedRT.pivot = new Vector2(0.5f, 0f);
        speedRT.sizeDelta = new Vector2(0f, 114f);
        speedRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI speedHeader = CreateTMP("SpeedTitle", speedCard.transform, "Tốc độ:", 18.5f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
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
        clockImg.color = new Color32(0x47, 0x55, 0x69, 255);

        // Giá trị thời gian "0.22s"
        TextMeshProUGUI speedValTMP = CreateTMP("SpeedValueText", speedCard.transform, "0.22s", 18f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
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

        GameObject btnCard = CreateRoundedPanel(name, cell.transform, cardColor, 16, addShadow: true);
        RectTransform cardRT = btnCard.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0f, 1f);
        cardRT.anchorMax = new Vector2(1f, 1f);
        cardRT.pivot = new Vector2(0.5f, 1f);
        cardRT.sizeDelta = new Vector2(0f, 92f);
        cardRT.anchoredPosition = Vector2.zero;

        Button btn = btnCard.AddComponent<Button>();
        ApplySubtleButtonTransition(btn, cardColor);

        letterLabel = CreateTMP("FaceLetter", btnCard.transform, faceLetter, 25f, FontStyles.Bold, contentColor, TextAlignmentOptions.Center);
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

        TextMeshProUGUI cap = CreateTMP("Caption", cell.transform, captionVi, 13.5f, FontStyles.Normal, new Color32(0x33, 0x41, 0x55, 255), TextAlignmentOptions.Center);
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
        GameObject btnObj = CreateRoundedPanel("BtnTopRightSettings", canvasTr, Color.white, 16, addShadow: true);
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
        iconImg.color = new Color32(0x33, 0x41, 0x55, 255);
        iconImg.raycastTarget = false;

        TextMeshProUGUI lbl = CreateTMP("Label", btnObj.transform, "Cài đặt", 14.5f, FontStyles.Bold, new Color32(0x1E, 0x29, 0x3B, 255), TextAlignmentOptions.Left);
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
        backdropImg.color = new Color(0.06f, 0.09f, 0.16f, 0.35f);

        GameObject card = CreateRoundedPanel("DialogCard", backdrop.transform, Color.white, 24, addShadow: true);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(540f, 430f);
        cardRT.anchoredPosition = Vector2.zero;

        // Tiêu đề lớn: NHẬP CÔNG THỨC RUBIK
        TextMeshProUGUI title = CreateTMP("TitleText", card.transform, "NHẬP CÔNG THỨC RUBIK", 26f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.offsetMin = new Vector2(32f, -66f);
        titleRT.offsetMax = new Vector2(-32f, -24f);

        // Ô nhập công thức bo góc
        GameObject inputBox = CreateRoundedPanel("FormulaInputField", card.transform, Color.white, 16, addShadow: false);
        RectTransform inputRT = inputBox.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0f, 1f);
        inputRT.anchorMax = new Vector2(1f, 1f);
        inputRT.pivot = new Vector2(0.5f, 1f);
        inputRT.sizeDelta = new Vector2(-64f, 64f);
        inputRT.anchoredPosition = new Vector2(0f, -82f);

        GameObject inputBorder = CreateUIObject("Border", inputBox.transform);
        StretchFull(inputBorder.GetComponent<RectTransform>());
        Image ibImg = inputBorder.AddComponent<Image>();
        ibImg.sprite = RubikUISpriteFactory.GetRoundedSprite(16, 2);
        ibImg.type = Image.Type.Sliced;
        ibImg.color = new Color32(0x94, 0xA3, 0xB8, 255);
        ibImg.raycastTarget = false;

        GameObject textArea = CreateUIObject("Text Area", inputBox.transform);
        RectTransform taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(18f, 8f);
        taRT.offsetMax = new Vector2(-18f, -8f);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMP("Placeholder", textArea.transform, "Ví dụ: R U R' U' F' U2...", 20f, FontStyles.Normal, new Color32(0x64, 0x74, 0x8B, 255), TextAlignmentOptions.Left);
        StretchFull(placeholder.GetComponent<RectTransform>());

        TextMeshProUGUI inputText = CreateTMP("Text", textArea.transform, "", 20f, FontStyles.Normal, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
        StretchFull(inputText.GetComponent<RectTransform>());

        TMP_InputField tmpInput = inputBox.AddComponent<TMP_InputField>();
        tmpInput.textViewport = taRT;
        tmpInput.textComponent = inputText;
        tmpInput.placeholder = placeholder;
        tmpInput.fontAsset = inputText.font;
        tmpInput.pointSize = 20;

        // Dòng thông báo lỗi / trạng thái kiểm tra cú pháp
        TextMeshProUGUI statusTMP = CreateTMP("StatusText", card.transform, "", 13.5f, FontStyles.Normal, new Color32(0xDC, 0x26, 0x26, 255), TextAlignmentOptions.Left);
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

        Button btnPaste = CreateIconLabelPillButton(row1.transform, "BtnPaste", "Dán", RubikUISpriteFactory.IconType.Paste, new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x0F, 0x17, 0x2A, 255));
        Button btnCopy = CreateIconLabelPillButton(row1.transform, "BtnCopy", "Sao chép", RubikUISpriteFactory.IconType.Copy, new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x0F, 0x17, 0x2A, 255));
        Button btnClear = CreateIconLabelPillButton(row1.transform, "BtnClear", "Xóa", RubikUISpriteFactory.IconType.Trash, new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x0F, 0x17, 0x2A, 255));

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

        Button btnPlayAnimate = CreateSimplePillButton(row2.transform, "BtnPlayAnimate", "Chạy mượt", new Color32(0xDB, 0xEA, 0xFE, 255), new Color32(0x1D, 0x4E, 0xD8, 255), 21f, hasBorder: false);
        Button btnApplyInstant = CreateSimplePillButton(row2.transform, "BtnApplyInstant", "Áp dụng ngay", new Color32(0x25, 0x63, 0xEB, 255), Color.white, 21f, hasBorder: false);

        // Hàng 3: Đóng
        Button btnClose = CreateSimplePillButton(card.transform, "BtnClose", "Đóng", new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x0F, 0x17, 0x2A, 255), 21f, hasBorder: true);
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
        GameObject obj = CreateRoundedPanel(name, parent, bgCol, 14, addShadow: false);
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
        GameObject obj = CreateRoundedPanel(name, parent, bgCol, 16, addShadow: false);
        if (hasBorder)
        {
            GameObject b = CreateUIObject("Border", obj.transform);
            StretchFull(b.GetComponent<RectTransform>());
            Image bImg = b.AddComponent<Image>();
            bImg.sprite = RubikUISpriteFactory.GetRoundedSprite(16, 2);
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color32(0x94, 0xA3, 0xB8, 255);
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
        arrowImg.color = new Color32(0x1D, 0x6F, 0xF2, 255);
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
        GameObject topCard = CreateRoundedPanel("GuideTopCard", solverRoot.transform, Color.white, 22, addShadow: true);
        RectTransform topRT = topCard.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.sizeDelta = new Vector2(0f, 205f);
        topRT.anchoredPosition = Vector2.zero;

        // Nút Quay lại "←"
        GameObject backBtnObj = CreateRoundedPanel("BtnClose", topCard.transform, new Color(1f, 1f, 1f, 0f), 12, addShadow: false);
        RectTransform backRT = backBtnObj.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0f, 1f);
        backRT.anchorMax = new Vector2(0f, 1f);
        backRT.pivot = new Vector2(0f, 1f);
        backRT.sizeDelta = new Vector2(46f, 46f);
        backRT.anchoredPosition = new Vector2(18f, -12f);
        Button btnCloseSolver = backBtnObj.AddComponent<Button>();
        CreateIconChild(backBtnObj.transform, RubikUISpriteFactory.IconType.BackArrow, new Color32(0x0F, 0x17, 0x2A, 255), new Vector2(26f, 26f));

        // Tiêu đề "Hướng dẫn giải"
        TextMeshProUGUI guideTitle = CreateTMP("GuideHeaderTitle", topCard.transform, "Hướng dẫn giải", 22f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Center);
        RectTransform gtRT = guideTitle.GetComponent<RectTransform>();
        gtRT.anchorMin = new Vector2(0f, 1f);
        gtRT.anchorMax = new Vector2(1f, 1f);
        gtRT.pivot = new Vector2(0.5f, 1f);
        gtRT.sizeDelta = new Vector2(0f, 46f);
        gtRT.anchoredPosition = new Vector2(0f, -12f);

        // Bộ đếm bước "Bước 3/7"
        TextMeshProUGUI stepInfoTMP = CreateTMP("StepInfoText", topCard.transform, "Bước 1/7", 16.5f, FontStyles.Normal, new Color32(0x47, 0x55, 0x69, 255), TextAlignmentOptions.Right);
        RectTransform siRT = stepInfoTMP.GetComponent<RectTransform>();
        siRT.anchorMin = new Vector2(1f, 1f);
        siRT.anchorMax = new Vector2(1f, 1f);
        siRT.pivot = new Vector2(1f, 1f);
        siRT.sizeDelta = new Vector2(220f, 46f);
        siRT.anchoredPosition = new Vector2(-24f, -12f);

        // Thẻ nền xanh nhạt chứa Ký hiệu lớn (R), Tiêu đề mặt, Mô tả chiều xoay
        GameObject blueCard = CreateRoundedPanel("InstructionBanner", topCard.transform, new Color32(0xEA, 0xF2, 0xFF, 255), 18, addShadow: false);
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
        badgeImg.color = new Color32(0x1D, 0x6F, 0xF2, 255);

        TextMeshProUGUI badgeText = CreateTMP("MoveBadgeText", badgeCircle.transform, "R", 26f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchFull(badgeText.GetComponent<RectTransform>());

        TextMeshProUGUI moveTitleTMP = CreateTMP("MoveTitleText", blueCard.transform, "Xoay mặt phải (R)", 21f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
        RectTransform mtRT = moveTitleTMP.GetComponent<RectTransform>();
        mtRT.anchorMin = new Vector2(0f, 0.5f);
        mtRT.anchorMax = new Vector2(1f, 1f);
        mtRT.offsetMin = new Vector2(106f, 2f);
        mtRT.offsetMax = new Vector2(-18f, -14f);

        TextMeshProUGUI moveDescTMP = CreateTMP("NextMoveText", blueCard.transform, "Xoay 90° theo chiều kim đồng hồ", 15.5f, FontStyles.Normal, new Color32(0x33, 0x41, 0x55, 255), TextAlignmentOptions.Left);
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
        botRT.sizeDelta = new Vector2(0f, 200f);
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

        Button btnPrev = CreateSimplePillButton(navRow.transform, "BtnPrev", "<    Trước", new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x0F, 0x17, 0x2A, 255), 19f, hasBorder: false);
        btnPrev.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button btnNext = CreateSimplePillButton(navRow.transform, "BtnNext", "Tiếp theo    >", new Color32(0x1D, 0x6F, 0xF2, 255), Color.white, 19f, hasBorder: false);
        btnNext.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.25f;

        Button btnAuto = CreateSimplePillButton(navRow.transform, "BtnAuto", "⚡ Tự động", new Color32(0xDD, 0xD6, 0xFE, 255), new Color32(0x6D, 0x28, 0xD9, 255), 16.5f, hasBorder: false);
        btnAuto.gameObject.AddComponent<LayoutElement>().preferredWidth = 135f;

        // Khung trắng chứa chuỗi Chip nước đi (R, U, R', U'...) + chấm tròn bên dưới
        GameObject chipsCard = CreateRoundedPanel("ChipsCard", bottomArea.transform, Color.white, 20, addShadow: true);
        RectTransform ccRT = chipsCard.GetComponent<RectTransform>();
        ccRT.anchorMin = new Vector2(0f, 0f);
        ccRT.anchorMax = new Vector2(1f, 0f);
        ccRT.pivot = new Vector2(0.5f, 0f);
        ccRT.sizeDelta = new Vector2(0f, 118f);
        ccRT.anchoredPosition = Vector2.zero;

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
    // 11. HỘP THOẠI XÁC NHẬN ĐẶT LẠI (RESET CONFIRMATION)
    // =========================================================================
    private static void BuildConfirmResetModal(Transform canvasTr, RubikUIController uiCtrl)
    {
        GameObject backdrop = CreateUIObject("ConfirmResetModal", canvasTr);
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image bg = backdrop.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.09f, 0.16f, 0.38f);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, 22, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(440f, 230f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "Xác nhận đặt lại khối Rubik?", 21f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Center);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(-40f, 40f);
        tRT.anchoredPosition = new Vector2(0f, -24f);

        TextMeshProUGUI desc = CreateTMP("Desc", card.transform, "Tiến trình xoay hoặc thời gian hiện tại sẽ được đưa về trạng thái đã giải ban đầu.", 15f, FontStyles.Normal, new Color32(0x47, 0x55, 0x69, 255), TextAlignmentOptions.Center);
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

        uiCtrl.btnConfirmResetCancel = CreateSimplePillButton(row.transform, "BtnCancel", "Hủy", new Color32(0xE2, 0xE8, 0xF0, 255), new Color32(0x1E, 0x29, 0x3B, 255), 17f, hasBorder: false);
        uiCtrl.btnConfirmResetYes = CreateSimplePillButton(row.transform, "BtnConfirm", "Đặt lại ngay", new Color32(0xEF, 0x44, 0x44, 255), Color.white, 17f, hasBorder: false);
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
        bg.color = new Color(0.06f, 0.09f, 0.16f, 0.38f);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, 24, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(520f, 410f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "CÀI ĐẶT ỨNG DỤNG", 24f, FontStyles.Bold, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Left);
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

        uiCtrl.btnResetPB = CreateSimplePillButton(bottomRow.transform, "BtnResetPB", "Xóa kỷ lục (PB)", new Color32(0xFE, 0xE2, 0xE2, 255), new Color32(0xDC, 0x26, 0x26, 255), 16.5f, hasBorder: false);
        uiCtrl.btnCloseSettings = CreateSimplePillButton(bottomRow.transform, "BtnCloseSettings", "Đóng", new Color32(0x25, 0x63, 0xEB, 255), Color.white, 17.5f, hasBorder: false);
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

        TextMeshProUGUI lbl = CreateTMP("Label", row.transform, label, 15.5f, FontStyles.Normal, new Color32(0x33, 0x41, 0x55, 255), TextAlignmentOptions.Left);
        RectTransform lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f);
        lRT.anchorMax = new Vector2(0.75f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.sizeDelta = new Vector2(0f, 26f);
        lRT.anchoredPosition = Vector2.zero;

        valueText = CreateTMP("Val", row.transform, $"{defaultVal:F2}", 15.5f, FontStyles.Bold, new Color32(0x25, 0x63, 0xEB, 255), TextAlignmentOptions.Right);
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
        bg.color = new Color(0.06f, 0.09f, 0.16f, 0.42f);

        GameObject card = CreateRoundedPanel("Card", backdrop.transform, Color.white, 24, addShadow: true);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 290f);

        TextMeshProUGUI title = CreateTMP("Title", card.transform, "🎉 HOÀN THÀNH XUẤT SẮC!", 24f, FontStyles.Bold, new Color32(0x16, 0xA3, 0x4A, 255), TextAlignmentOptions.Center);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(-32f, 44f);
        tRT.anchoredPosition = new Vector2(0f, -26f);

        uiCtrl.victoryTimeText = CreateTMP("VictoryTimeText", card.transform, "Thời gian: <b>00:00.000</b>", 18.5f, FontStyles.Normal, new Color32(0x0F, 0x17, 0x2A, 255), TextAlignmentOptions.Center);
        RectTransform vtRT = uiCtrl.victoryTimeText.GetComponent<RectTransform>();
        vtRT.sizeDelta = new Vector2(380f, 36f);
        vtRT.anchoredPosition = new Vector2(0f, 24f);

        uiCtrl.victoryMovesText = CreateTMP("VictoryMovesText", card.transform, "Số nước đi: <b>0</b>", 17.5f, FontStyles.Normal, new Color32(0x47, 0x55, 0x69, 255), TextAlignmentOptions.Center);
        RectTransform vmRT = uiCtrl.victoryMovesText.GetComponent<RectTransform>();
        vmRT.sizeDelta = new Vector2(380f, 34f);
        vmRT.anchoredPosition = new Vector2(0f, -16f);

        uiCtrl.btnPlayAgain = CreateSimplePillButton(card.transform, "BtnPlayAgain", "Chơi ván mới", new Color32(0x22, 0xC5, 0x5E, 255), Color.white, 19f, hasBorder: false);
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
            sh.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.09f);
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
        FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Slider CreateModernSlider(string name, Transform parent, float min, float max, float val)
    {
        GameObject sliderObj = CreateUIObject(name, parent);
        Slider slider = sliderObj.AddComponent<Slider>();

        // Track nền xám nhạt
        GameObject bgObj = CreateRoundedPanel("Background", sliderObj.transform, new Color32(0xE2, 0xE8, 0xF0, 255), 6, addShadow: false);
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

        GameObject fill = CreateRoundedPanel("Fill", fillArea.transform, new Color32(0x3B, 0x82, 0xF6, 255), 6, addShadow: false);
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
        hShadow.effectColor = new Color(0.06f, 0.09f, 0.16f, 0.22f);
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
