using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý bố cục Responsive tự động thích ứng giữa màn hình Desktop/PC (1920x1080, 1366x768)
/// và màn hình dọc Điện thoại (390x844, 1080x1920, 1080x2400) mà không tạo/hủy đối tượng mỗi frame.
/// </summary>
public class RubikResponsiveLayout : MonoBehaviour
{
    [Header("Tham chiếu Canvas & Camera")]
    public CanvasScaler canvasScaler;
    public RubikCameraOrbit cameraOrbit;

    [Header("Các khối giao diện chính")]
    public RectTransform sidebarPanel;
    public RectTransform mobileBottomNav;
    public RectTransform centerHeader;
    public RectTransform orbitArcRect;
    public RectTransform cameraControlsColumn;
    public RectTransform bottomControlPanel;
    public RectTransform rightControlPanel;
    public RectTransform topRightSettingsBtn;

    [Header("Các hộp thoại / Panel phụ")]
    public RectTransform algorithmDialogCard;
    public RectTransform solverGuideContainer;
    public RectTransform cameraScannerCard;
    public RectTransform netColorPickerCard;
    public RectTransform settingsModalCard;
    public RectTransform confirmResetCard;
    public RectTransform victoryCard;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private bool isPortraitMode = false;
    public bool IsPortraitMode => isPortraitMode;

    private void Start()
    {
        EnsureCardReferences();
        ApplyResponsiveLayout(force: true);
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyResponsiveLayout(force: false);
        }
    }

    public void EnsureCardReferences()
    {
        Transform canvasTr = transform;

        if (cameraScannerCard == null)
        {
            Transform tr = canvasTr.Find("CameraScannerPanel/ScannerCard");
            if (tr != null) cameraScannerCard = tr.GetComponent<RectTransform>();
        }
        if (netColorPickerCard == null)
        {
            Transform tr = canvasTr.Find("NetColorPickerPanel/NetCard");
            if (tr != null) netColorPickerCard = tr.GetComponent<RectTransform>();
        }
        if (settingsModalCard == null)
        {
            Transform tr = canvasTr.Find("SettingsModal/Card");
            if (tr != null) settingsModalCard = tr.GetComponent<RectTransform>();
        }
        if (confirmResetCard == null)
        {
            Transform tr = canvasTr.Find("ConfirmResetModal/Card");
            if (tr != null) confirmResetCard = tr.GetComponent<RectTransform>();
        }
        if (victoryCard == null)
        {
            Transform tr = canvasTr.Find("VictoryPanel/Card");
            if (tr != null) victoryCard = tr.GetComponent<RectTransform>();
        }
        if (algorithmDialogCard == null)
        {
            Transform tr = canvasTr.Find("AlgorithmInputPanel/Card");
            if (tr != null) algorithmDialogCard = tr.GetComponent<RectTransform>();
        }
        if (solverGuideContainer == null)
        {
            Transform tr = canvasTr.Find("SolverPanel");
            if (tr != null) solverGuideContainer = tr.GetComponent<RectTransform>();
        }
    }

    public void ApplyResponsiveLayout(bool force = false)
    {
        EnsureCardReferences();

        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);

        if (!force && w == lastScreenWidth && h == lastScreenHeight)
            return;

        lastScreenWidth = w;
        lastScreenHeight = h;

        float aspect = (float)w / h;
        isPortraitMode = aspect < 1.15f;

        if (canvasScaler == null)
            canvasScaler = GetComponent<CanvasScaler>();
        if (cameraOrbit == null)
            cameraOrbit = FindAnyObjectByType<RubikCameraOrbit>();

        if (!isPortraitMode)
        {
            ApplyDesktopLayout(aspect);
        }
        else
        {
            ApplyMobilePortraitLayout();
        }
    }

    private void ApplyDesktopLayout(float aspect)
    {
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = aspect < 1.6f ? 0.35f : 0.5f;
        }

        // 1. Hiện Sidebar bên trái, ẩn thanh điều hướng dưới của Mobile
        if (sidebarPanel != null)
        {
            sidebarPanel.gameObject.SetActive(true);
            sidebarPanel.anchorMin = new Vector2(0f, 0f);
            sidebarPanel.anchorMax = new Vector2(0f, 1f);
            sidebarPanel.pivot = new Vector2(0f, 0.5f);
            sidebarPanel.anchoredPosition = new Vector2(24f, 0f);
            sidebarPanel.sizeDelta = new Vector2(264f, -48f);
        }

        if (mobileBottomNav != null)
            mobileBottomNav.gameObject.SetActive(false);

        // 2. Tiêu đề "Rubik 3D" ở trung tâm phía trên
        if (centerHeader != null)
        {
            centerHeader.anchorMin = new Vector2(0.5f, 1f);
            centerHeader.anchorMax = new Vector2(0.5f, 1f);
            centerHeader.pivot = new Vector2(0.5f, 1f);
            centerHeader.anchoredPosition = new Vector2(-42f, -34f);
            centerHeader.sizeDelta = new Vector2(560f, 95f);
        }

        // 3. Vòng cung xoay dưới chân khối Rubik
        if (orbitArcRect != null)
        {
            orbitArcRect.anchorMin = new Vector2(0.5f, 0.5f);
            orbitArcRect.anchorMax = new Vector2(0.5f, 0.5f);
            orbitArcRect.pivot = new Vector2(0.5f, 0.5f);
            orbitArcRect.anchoredPosition = new Vector2(-42f, -105f);
            orbitArcRect.sizeDelta = new Vector2(490f, 150f);
        }

        // 4. Cụm 3 nút điều khiển Camera nổi bên phải khối Rubik
        if (cameraControlsColumn != null)
        {
            cameraControlsColumn.anchorMin = new Vector2(0.5f, 0.5f);
            cameraControlsColumn.anchorMax = new Vector2(0.5f, 0.5f);
            cameraControlsColumn.pivot = new Vector2(0.5f, 0.5f);
            cameraControlsColumn.anchoredPosition = new Vector2(265f, 110f);
        }

        // 5. Bảng điều khiển phía dưới ("BẢNG ĐIỀU KHIỂN & CHẠY THỬ")
        if (bottomControlPanel != null)
        {
            bottomControlPanel.anchorMin = new Vector2(0.5f, 0f);
            bottomControlPanel.anchorMax = new Vector2(0.5f, 0f);
            bottomControlPanel.pivot = new Vector2(0.5f, 0f);
            bottomControlPanel.anchoredPosition = new Vector2(-42f, 24f);
            bottomControlPanel.sizeDelta = new Vector2(850f, 138f);
        }

        // 6. Panel Điều khiển xoay & Tốc độ bên phải
        if (rightControlPanel != null)
        {
            rightControlPanel.anchorMin = new Vector2(1f, 0.5f);
            rightControlPanel.anchorMax = new Vector2(1f, 0.5f);
            rightControlPanel.pivot = new Vector2(1f, 0.5f);
            rightControlPanel.anchoredPosition = new Vector2(-24f, -12f);
            rightControlPanel.sizeDelta = new Vector2(364f, 620f);
            rightControlPanel.localScale = Vector3.one;

            Transform rotCard = rightControlPanel.Find("RotationControlCard");
            if (rotCard != null)
            {
                RectTransform rotRT = rotCard.GetComponent<RectTransform>();
                if (rotRT != null) rotRT.sizeDelta = new Vector2(0f, 488f);
            }
        }

        if (topRightSettingsBtn != null)
        {
            topRightSettingsBtn.anchorMin = new Vector2(1f, 1f);
            topRightSettingsBtn.anchorMax = new Vector2(1f, 1f);
            topRightSettingsBtn.pivot = new Vector2(1f, 1f);
            topRightSettingsBtn.anchoredPosition = new Vector2(-28f, -24f);
        }

        if (algorithmDialogCard != null)
        {
            algorithmDialogCard.sizeDelta = new Vector2(540f, 430f);
        }

        if (solverGuideContainer != null)
        {
            solverGuideContainer.anchorMin = new Vector2(0.5f, 0.5f);
            solverGuideContainer.anchorMax = new Vector2(0.5f, 0.5f);
            solverGuideContainer.pivot = new Vector2(0.5f, 0.5f);
            solverGuideContainer.anchoredPosition = new Vector2(120f, 0f);
            solverGuideContainer.sizeDelta = new Vector2(680f, 920f);

            Transform topCard = solverGuideContainer.Find("GuideTopCard");
            if (topCard != null)
            {
                RectTransform tcRT = topCard.GetComponent<RectTransform>();
                tcRT.anchorMin = new Vector2(0f, 1f);
                tcRT.anchorMax = new Vector2(1f, 1f);
                tcRT.pivot = new Vector2(0.5f, 1f);
                tcRT.anchoredPosition = Vector2.zero;
                tcRT.sizeDelta = new Vector2(0f, 205f);
            }

            Transform botArea = solverGuideContainer.Find("GuideBottomArea");
            if (botArea != null)
            {
                RectTransform baRT = botArea.GetComponent<RectTransform>();
                baRT.anchorMin = new Vector2(0f, 0f);
                baRT.anchorMax = new Vector2(1f, 0f);
                baRT.pivot = new Vector2(0.5f, 0f);
                baRT.anchoredPosition = Vector2.zero;
                baRT.sizeDelta = new Vector2(0f, 196f);
            }
        }

        if (cameraScannerCard != null)
        {
            cameraScannerCard.sizeDelta = new Vector2(880f, 630f);
            Transform leftTr = cameraScannerCard.Find("LeftContainer");
            if (leftTr != null)
            {
                RectTransform lRT = leftTr.GetComponent<RectTransform>();
                lRT.sizeDelta = new Vector2(400f, 510f);
                lRT.anchoredPosition = new Vector2(-206f, -34f);
            }
            Transform rightTr = cameraScannerCard.Find("RightContainer");
            if (rightTr != null)
            {
                RectTransform rRT = rightTr.GetComponent<RectTransform>();
                rRT.sizeDelta = new Vector2(400f, 510f);
                rRT.anchoredPosition = new Vector2(206f, -34f);
            }
        }

        if (netColorPickerCard != null)
        {
            netColorPickerCard.sizeDelta = new Vector2(840f, 610f);
            Transform leftTr = netColorPickerCard.Find("LeftContainer");
            if (leftTr != null)
            {
                RectTransform lRT = leftTr.GetComponent<RectTransform>();
                lRT.sizeDelta = new Vector2(420f, 480f);
                lRT.anchoredPosition = new Vector2(-190f, -30f);
            }
            Transform rightTr = netColorPickerCard.Find("RightContainer");
            if (rightTr != null)
            {
                RectTransform rRT = rightTr.GetComponent<RectTransform>();
                rRT.sizeDelta = new Vector2(340f, 480f);
                rRT.anchoredPosition = new Vector2(210f, -30f);
            }
        }

        if (settingsModalCard != null) settingsModalCard.sizeDelta = new Vector2(460f, 420f);
        if (confirmResetCard != null) confirmResetCard.sizeDelta = new Vector2(440f, 230f);
        if (victoryCard != null) victoryCard.sizeDelta = new Vector2(460f, 380f);

        // Căn tâm khối Rubik 3D vào giữa vùng xem trên Desktop
        if (cameraOrbit != null)
        {
            cameraOrbit.targetWorldOffset = new Vector3(0.22f, -0.15f, 0f);
        }
    }

    private void ApplyMobilePortraitLayout()
    {
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.matchWidthOrHeight = 0f; // Khớp 100% bề ngang để không bao giờ bị cắt viền trên bất kỳ điện thoại nào
        }

        // 1. Ẩn Sidebar dọc, bật Thanh điều hướng dưới cùng cho điện thoại
        if (sidebarPanel != null)
            sidebarPanel.gameObject.SetActive(false);

        if (mobileBottomNav != null)
        {
            mobileBottomNav.gameObject.SetActive(true);
            mobileBottomNav.anchorMin = new Vector2(0f, 0f);
            mobileBottomNav.anchorMax = new Vector2(1f, 0f);
            mobileBottomNav.pivot = new Vector2(0.5f, 0f);
            mobileBottomNav.anchoredPosition = Vector2.zero;
            mobileBottomNav.sizeDelta = new Vector2(0f, 130f);
        }

        // 2. Tiêu đề "Rubik 3D" ở đỉnh màn hình dọc
        if (centerHeader != null)
        {
            centerHeader.anchorMin = new Vector2(0f, 1f);
            centerHeader.anchorMax = new Vector2(1f, 1f);
            centerHeader.pivot = new Vector2(0.5f, 1f);
            centerHeader.anchoredPosition = new Vector2(0f, -24f);
            centerHeader.sizeDelta = new Vector2(-60f, 110f);
        }

        if (topRightSettingsBtn != null)
        {
            topRightSettingsBtn.anchorMin = new Vector2(1f, 1f);
            topRightSettingsBtn.anchorMax = new Vector2(1f, 1f);
            topRightSettingsBtn.pivot = new Vector2(1f, 1f);
            topRightSettingsBtn.anchoredPosition = new Vector2(-28f, -24f);
        }

        // 3. Vòng cung xoay dưới khối Rubik ở vùng trung tâm
        if (orbitArcRect != null)
        {
            orbitArcRect.anchorMin = new Vector2(0.5f, 0.5f);
            orbitArcRect.anchorMax = new Vector2(0.5f, 0.5f);
            orbitArcRect.pivot = new Vector2(0.5f, 0.5f);
            orbitArcRect.anchoredPosition = new Vector2(0f, -140f);
            orbitArcRect.sizeDelta = new Vector2(520f, 160f);
        }

        // 4. Nút Camera ở cạnh phải vùng 3D, vừa tầm ngón tay cái
        if (cameraControlsColumn != null)
        {
            cameraControlsColumn.anchorMin = new Vector2(1f, 0.5f);
            cameraControlsColumn.anchorMax = new Vector2(1f, 0.5f);
            cameraControlsColumn.pivot = new Vector2(1f, 0.5f);
            cameraControlsColumn.anchoredPosition = new Vector2(-24f, 60f);
        }

        // 5. Bảng điều khiển & Chạy thử nằm ngay trên thanh điều hướng dưới
        if (bottomControlPanel != null)
        {
            bottomControlPanel.anchorMin = new Vector2(0f, 0f);
            bottomControlPanel.anchorMax = new Vector2(1f, 0f);
            bottomControlPanel.pivot = new Vector2(0.5f, 0f);
            bottomControlPanel.anchoredPosition = new Vector2(0f, 136f);
            bottomControlPanel.sizeDelta = new Vector2(-40f, 142f);
        }

        // 6. Panel Điều khiển xoay + Tốc độ xếp gọn gàng phía trên
        if (rightControlPanel != null)
        {
            rightControlPanel.anchorMin = new Vector2(0f, 0f);
            rightControlPanel.anchorMax = new Vector2(1f, 0f);
            rightControlPanel.pivot = new Vector2(0.5f, 0f);
            rightControlPanel.anchoredPosition = new Vector2(0f, 286f);
            rightControlPanel.sizeDelta = new Vector2(-40f, 605f);
            rightControlPanel.localScale = Vector3.one;

            Transform rotCard = rightControlPanel.Find("RotationControlCard");
            if (rotCard != null)
            {
                RectTransform rotRT = rotCard.GetComponent<RectTransform>();
                if (rotRT != null) rotRT.sizeDelta = new Vector2(0f, 492f);
            }

            Transform speedCard = rightControlPanel.Find("SpeedCard");
            if (speedCard != null)
            {
                RectTransform spRT = speedCard.GetComponent<RectTransform>();
                if (spRT != null) spRT.anchoredPosition = new Vector2(0f, -504f);
            }
        }

        // 7. Giao diện Hướng dẫn giải từng bước trên màn hình dọc
        if (solverGuideContainer != null)
        {
            solverGuideContainer.anchorMin = new Vector2(0f, 0f);
            solverGuideContainer.anchorMax = new Vector2(1f, 1f);
            solverGuideContainer.pivot = new Vector2(0.5f, 0.5f);
            solverGuideContainer.anchoredPosition = Vector2.zero;
            solverGuideContainer.sizeDelta = Vector2.zero;

            Transform topCard = solverGuideContainer.Find("GuideTopCard");
            if (topCard != null)
            {
                RectTransform tcRT = topCard.GetComponent<RectTransform>();
                tcRT.anchorMin = new Vector2(0f, 1f);
                tcRT.anchorMax = new Vector2(1f, 1f);
                tcRT.pivot = new Vector2(0.5f, 1f);
                tcRT.anchoredPosition = new Vector2(0f, -20f);
                tcRT.sizeDelta = new Vector2(-40f, 215f);
            }

            Transform botArea = solverGuideContainer.Find("GuideBottomArea");
            if (botArea != null)
            {
                RectTransform baRT = botArea.GetComponent<RectTransform>();
                baRT.anchorMin = new Vector2(0f, 0f);
                baRT.anchorMax = new Vector2(1f, 0f);
                baRT.pivot = new Vector2(0.5f, 0f);
                baRT.anchoredPosition = new Vector2(0f, 136f);
                baRT.sizeDelta = new Vector2(-40f, 196f);
            }
        }

        // 8. Tối ưu kích thước các Hộp thoại Modal trên điện thoại
        if (algorithmDialogCard != null)
        {
            algorithmDialogCard.sizeDelta = new Vector2(980f, 660f);
        }

        if (cameraScannerCard != null)
        {
            cameraScannerCard.sizeDelta = new Vector2(1020f, 1720f);
            Transform leftTr = cameraScannerCard.Find("LeftContainer");
            if (leftTr != null)
            {
                RectTransform lRT = leftTr.GetComponent<RectTransform>();
                lRT.sizeDelta = new Vector2(960f, 760f);
                lRT.anchoredPosition = new Vector2(0f, 380f);

                Transform camFrame = leftTr.Find("CameraFeedFrame");
                if (camFrame != null)
                {
                    camFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(490f, 490f);
                    camFrame.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 95f);
                }
                Transform feed = leftTr.Find("CameraFeed");
                if (feed != null)
                {
                    feed.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 480f);
                    feed.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 95f);
                }
                Transform distLabel = leftTr.Find("DistanceLabelText");
                if (distLabel != null) distLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -165f);
                Transform slider = leftTr.Find("SliderDistance");
                if (slider != null)
                {
                    slider.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 24f);
                    slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -192f);
                }
                Transform nudge = leftTr.Find("NudgeRow");
                if (nudge != null)
                {
                    nudge.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 38f);
                    nudge.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -232f);
                }
                Transform camRow = leftTr.Find("CamControlsRow");
                if (camRow != null)
                {
                    camRow.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 44f);
                    camRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -282f);
                }
                Transform camName = leftTr.Find("CameraNameText");
                if (camName != null) camName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -325f);
            }

            Transform rightTr = cameraScannerCard.Find("RightContainer");
            if (rightTr != null)
            {
                RectTransform rRT = rightTr.GetComponent<RectTransform>();
                rRT.sizeDelta = new Vector2(960f, 780f);
                rRT.anchoredPosition = new Vector2(0f, -420f);

                Transform inst = rightTr.Find("InstructionCard");
                if (inst != null)
                {
                    inst.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 75f);
                    inst.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 320f);
                }
                Transform chips = rightTr.Find("StepChipsRow");
                if (chips != null)
                {
                    chips.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 36f);
                    chips.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 255f);
                }
                Transform preview = rightTr.Find("PreviewFrame");
                if (preview != null)
                {
                    preview.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 230f);
                    preview.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 115f);
                }
                Transform capBtn = rightTr.Find("BtnCaptureFace");
                if (capBtn != null)
                {
                    capBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 54f);
                    capBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -25f);
                }
                Transform navRow = rightTr.Find("NavRow");
                if (navRow != null)
                {
                    navRow.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 44f);
                    navRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
                }
                Transform cb1 = rightTr.Find("ToggleAutoCalibrate");
                if (cb1 != null)
                {
                    cb1.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 26f);
                    cb1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -125f);
                }
                Transform cb2 = rightTr.Find("ToggleCustomMaterialSync");
                if (cb2 != null)
                {
                    cb2.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 26f);
                    cb2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -158f);
                }
                Transform mirrorStatus = rightTr.Find("MirrorStatusText");
                if (mirrorStatus != null) mirrorStatus.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -190f);
                Transform applyBtn = rightTr.Find("BtnApplyTo3D");
                if (applyBtn != null)
                {
                    applyBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 58f);
                    applyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -242f);
                }
            }
        }

        if (netColorPickerCard != null)
        {
            netColorPickerCard.sizeDelta = new Vector2(1020f, 1720f);
            Transform leftTr = netColorPickerCard.Find("LeftContainer");
            if (leftTr != null)
            {
                RectTransform lRT = leftTr.GetComponent<RectTransform>();
                lRT.sizeDelta = new Vector2(960f, 850f);
                lRT.anchoredPosition = new Vector2(0f, 330f);
            }
            Transform rightTr = netColorPickerCard.Find("RightContainer");
            if (rightTr != null)
            {
                RectTransform rRT = rightTr.GetComponent<RectTransform>();
                rRT.sizeDelta = new Vector2(960f, 700f);
                rRT.anchoredPosition = new Vector2(0f, -460f);
            }
        }

        if (settingsModalCard != null) settingsModalCard.sizeDelta = new Vector2(920f, 740f);
        if (confirmResetCard != null) confirmResetCard.sizeDelta = new Vector2(860f, 380f);
        if (victoryCard != null) victoryCard.sizeDelta = new Vector2(860f, 640f);

        // Căn tâm khối Rubik 3D chuẩn xác vào giữa vùng nhìn thoáng trên điện thoại
        if (cameraOrbit != null)
        {
            cameraOrbit.targetWorldOffset = new Vector3(0f, -0.28f, 0f);
        }
    }
}
