using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý bố cục Responsive tự động thích ứng giữa màn hình Desktop/PC (1920x1080, 1366x768)
/// và màn hình dọc Điện thoại (390x844) mà không tạo/hủy đối tượng mỗi frame.
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

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private bool isPortraitMode = false;
    public bool IsPortraitMode => isPortraitMode;

    private void Start()
    {
        ApplyResponsiveLayout(force: true);
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyResponsiveLayout(force: false);
        }
    }

    public void ApplyResponsiveLayout(bool force = false)
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);

        if (!force && w == lastScreenWidth && h == lastScreenHeight)
            return;

        lastScreenWidth = w;
        lastScreenHeight = h;

        float aspect = (float)w / h;
        isPortraitMode = aspect < 1.12f;

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
        }

        // Căn tâm khối Rubik 3D vào giữa vùng xem
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
            canvasScaler.referenceResolution = new Vector2(900, 1950);
            canvasScaler.matchWidthOrHeight = 0.45f;
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
            mobileBottomNav.anchoredPosition = new Vector2(0f, 0f);
            mobileBottomNav.sizeDelta = new Vector2(0f, 112f);
        }

        // 2. Tiêu đề "Rubik 3D" ở đỉnh màn hình dọc
        if (centerHeader != null)
        {
            centerHeader.anchorMin = new Vector2(0.5f, 1f);
            centerHeader.anchorMax = new Vector2(0.5f, 1f);
            centerHeader.pivot = new Vector2(0.5f, 1f);
            centerHeader.anchoredPosition = new Vector2(0f, -28f);
        }

        if (topRightSettingsBtn != null)
        {
            topRightSettingsBtn.anchorMin = new Vector2(1f, 1f);
            topRightSettingsBtn.anchorMax = new Vector2(1f, 1f);
            topRightSettingsBtn.pivot = new Vector2(1f, 1f);
            topRightSettingsBtn.anchoredPosition = new Vector2(-24f, -24f);
        }

        // 3. Vòng cung xoay dưới khối Rubik ở nửa trên màn hình
        if (orbitArcRect != null)
        {
            orbitArcRect.anchorMin = new Vector2(0.5f, 1f);
            orbitArcRect.anchorMax = new Vector2(0.5f, 1f);
            orbitArcRect.pivot = new Vector2(0.5f, 0.5f);
            orbitArcRect.anchoredPosition = new Vector2(0f, -640f);
            orbitArcRect.sizeDelta = new Vector2(440f, 130f);
        }

        // 4. Nút Camera ở cạnh phải vùng 3D
        if (cameraControlsColumn != null)
        {
            cameraControlsColumn.anchorMin = new Vector2(1f, 1f);
            cameraControlsColumn.anchorMax = new Vector2(1f, 1f);
            cameraControlsColumn.pivot = new Vector2(1f, 0.5f);
            cameraControlsColumn.anchoredPosition = new Vector2(-28f, -430f);
        }

        // 5. Panel Điều khiển xoay + Tốc độ xếp gọn phía trên Bảng điều khiển
        if (rightControlPanel != null)
        {
            rightControlPanel.anchorMin = new Vector2(0.5f, 0f);
            rightControlPanel.anchorMax = new Vector2(0.5f, 0f);
            rightControlPanel.pivot = new Vector2(0.5f, 0f);
            rightControlPanel.anchoredPosition = new Vector2(0f, 276f);
            rightControlPanel.sizeDelta = new Vector2(850f, 600f);
            rightControlPanel.localScale = new Vector3(0.95f, 0.95f, 1f);
        }

        // 6. Bảng điều khiển & Chạy thử nằm ngay trên thanh điều hướng dưới
        if (bottomControlPanel != null)
        {
            bottomControlPanel.anchorMin = new Vector2(0.5f, 0f);
            bottomControlPanel.anchorMax = new Vector2(0.5f, 0f);
            bottomControlPanel.pivot = new Vector2(0.5f, 0f);
            bottomControlPanel.anchoredPosition = new Vector2(0f, 126f);
            bottomControlPanel.sizeDelta = new Vector2(850f, 138f);
        }

        if (algorithmDialogCard != null)
        {
            algorithmDialogCard.sizeDelta = new Vector2(820f, 520f);
        }

        if (solverGuideContainer != null)
        {
            solverGuideContainer.anchorMin = new Vector2(0.5f, 0.5f);
            solverGuideContainer.anchorMax = new Vector2(0.5f, 0.5f);
            solverGuideContainer.pivot = new Vector2(0.5f, 0.5f);
            solverGuideContainer.anchoredPosition = new Vector2(0f, 40f);
            solverGuideContainer.sizeDelta = new Vector2(840f, 1450f);
        }

        // Đẩy khối Rubik 3D lên nửa trên màn hình dọc để không bị các bảng điều khiển che khuất
        if (cameraOrbit != null)
        {
            cameraOrbit.targetWorldOffset = new Vector3(0f, -1.35f, 0f);
        }
    }
}
