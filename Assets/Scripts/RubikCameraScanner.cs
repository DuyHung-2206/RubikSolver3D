using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Quản lý quét màu khối Rubik thực tế qua Webcam hoặc Camera điện thoại (WebCamTexture).
/// Tích hợp nhận diện khung ngắm 3x3, tự động cân bằng ánh sáng (Auto-Calibrate),
/// và đồng bộ màu thực tế lên 27 Cubie của RubikGenerator (Custom Material Sync).
/// </summary>
public class RubikCameraScanner : MonoBehaviour
{
    [Header("Tham chiếu Logic")]
    public RubikGenerator generator;
    public RubikStateManager stateManager;

    [Header("Giao diện Quét Camera (UI Panel)")]
    public GameObject scannerPanel;
    public RawImage cameraFeedImage;
    public RectTransform reticleContainer; // Khung ngắm 3x3 overlay trên Camera
    public TextMeshProUGUI faceTitleText;
    public TextMeshProUGUI faceGuideText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI cameraInfoText;

    [Header("Xem trước & Chỉnh sửa màu (Preview Grid 3x3)")]
    public Transform previewGridContainer; // 9 ô xem trước của mặt đang quét
    public Transform faceBadgesContainer;  // 6 huy hiệu tiến độ U, R, F, D, L, B

    [Header("Các nút điều khiển")]
    public Button btnCaptureFace;
    public Button btnRescanFace;
    public Button btnNextFace;
    public Button btnPrevFace;
    public Button btnSwitchCamera;
    public Button btnApplyToCube;
    public Button btnClose;

    [Header("Cài đặt nâng cao")]
    public Toggle toggleAutoCalibrate;
    public Toggle toggleCustomMaterialSync;

    [Header("Cự ly quét & Kích thước khung ngắm")]
    public Slider sliderDistance;
    public TextMeshProUGUI distanceLabelText;
    [Range(36f, 88f)]
    public float reticleSpread = 56f;
    public float reticleBoxSize = 46f;
    public Vector2 reticleOffset = new Vector2(0f, -25f); // Hạ thấp xuống vị trí tay cầm tự nhiên

    // Nút điều chỉnh vị trí khung ngắm (Nudge)
    public Button btnNudgeUp;
    public Button btnNudgeDown;
    public Button btnNudgeLeft;
    public Button btnNudgeRight;
    public Button btnResetReticlePos;

    [Header("Hướng & Lật Camera")]
    [Tooltip("Mặc định bật Lật gương (Mirror) để chuyển động tay tự nhiên, không bị đảo chiều")]
    public bool flipHorizontal = true;
    [Tooltip("Lật dọc khung hình nếu camera bị lộn ngược đầu")]
    public bool flipVertical = false;
    public Button btnFlipHorizontal;
    public Button btnFlipVertical;

    // Quản lý luồng Camera
    private WebCamTexture webCamTexture;
    private int currentDeviceIndex = 0;
    private bool isSimulatingCamera = false;
    public bool IsSimulatingCamera => isSimulatingCamera;
    private Texture2D mockCameraTexture;

    // Lộ trình quét 6 mặt tối ưu: F -> R -> B -> L -> U -> D
    // Người chơi cầm Rubik xoay vòng tròn sang trái (F -> R -> B -> L), sau đó ngửa lên/xuống (U, D).
    private static readonly char[] FaceOrder = { 'F', 'R', 'B', 'L', 'U', 'D' };
    private static readonly char[] DefaultCenterColors = { 'G', 'R', 'B', 'O', 'W', 'Y' };

    private static readonly string[] FaceNamesVi = {
        "MẶT F (TRƯỚC)",
        "MẶT R (PHẢI)",
        "MẶT B (SAU)",
        "MẶT L (TRÁI)",
        "MẶT U (TRÊN)",
        "MẶT D (DƯỚI)"
    };

    private static readonly string[] FaceGuidesVi = {
        "1. Hướng mặt bất kỳ vào camera làm mặt TRƯỚC (F). Nhấn phím ENTER để chụp.",
        "2. Xoay khối Rubik sang TRÁI 90° để quét mặt PHẢI (R). Nhấn ENTER.",
        "3. Xoay tiếp khối Rubik sang TRÁI 90° để quét mặt SAU (B). Nhấn ENTER.",
        "4. Xoay tiếp khối Rubik sang TRÁI 90° để quét mặt TRÁI (L). Nhấn ENTER.",
        "5. Quay về mặt F, nghiêng khối XUỐNG 90° để quét mặt TRÊN (U). Nhấn ENTER.",
        "6. Quay về mặt F, nghiêng khối LÊN 90° để quét mặt DƯỚI (D). Nhấn ENTER."
    };

    private int currentFaceIndex = 0;

    // Dữ liệu 54 ô quét được: FaceCode -> char[9]
    private readonly Dictionary<char, char[]> scannedFaces = new Dictionary<char, char[]>();
    private readonly HashSet<char> completedFaces = new HashSet<char>();

    // Bảng màu RGB trung bình thực tế được lấy mẫu từ 6 tâm mặt (Auto-Calibrate)
    private readonly Dictionary<char, Color> calibratedColors = new Dictionary<char, Color>();

    // Bộ đệm tái sử dụng chống Garbage Collection cho hàm lấy mẫu Trimmed Mean
    private Color[] kernelColorBuffer = new Color[256];
    private float[] kernelLuminanceBuffer = new float[256];
    private int[] kernelIndexBuffer = new int[256];

    // Bảng màu chuẩn WCA mặc định để nhận diện (Đồng bộ 100% với RubikNetColorPicker.DisplayColors và RubikGenerator)
    private static readonly Dictionary<char, Color> WcaReferenceColors = new Dictionary<char, Color>()
    {
        { 'W', new Color(0.96f, 0.96f, 0.96f) }, // Trắng
        { 'Y', new Color(1.00f, 0.84f, 0.00f) }, // Vàng
        { 'G', new Color(0.00f, 0.65f, 0.28f) }, // Xanh lá
        { 'B', new Color(0.00f, 0.35f, 0.85f) }, // Xanh dương
        { 'R', new Color(0.85f, 0.12f, 0.15f) }, // Đỏ
        { 'O', new Color(1.00f, 0.40f, 0.00f) }, // Cam
        { '?', new Color(0.28f, 0.32f, 0.40f) }  // Mẫu chưa rõ / Cảnh báo
    };

    // UI elements lưu trữ nội bộ
    private Image[] previewCellImages = new Image[9];
    private Image[] reticleCellBorders = new Image[9];
    private RectTransform[] reticleTransforms = new RectTransform[9];
    private TextMeshProUGUI[] reticleLabels = new TextMeshProUGUI[9];
    private Image[] faceBadgeImages = new Image[6];

    // Màu đang được nhận diện thời gian thực của 9 ô
    private readonly char[] liveDetectedColors = new char[9];
    private readonly Color[] liveSampledColors = new Color[9];

    private void Awake()
    {
        AutoBindReferences();

        if (generator == null) generator = FindAnyObjectByType<RubikGenerator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();

        InitDefaultData();

        if (scannerPanel != null)
            scannerPanel.SetActive(false);
    }

    private bool eventsBound = false;

    private void Start()
    {
        AutoBindReferences();

        if (scannerPanel != null)
            scannerPanel.SetActive(false);

        BindScannerUIAndEvents();
    }

    public void BindScannerUIAndEvents()
    {
        AutoBindReferences();

        if (btnCaptureFace != null)
        {
            btnCaptureFace.onClick.RemoveListener(CaptureCurrentFace);
            btnCaptureFace.onClick.AddListener(CaptureCurrentFace);
        }
        if (btnRescanFace != null)
        {
            btnRescanFace.onClick.RemoveListener(RescanCurrentFace);
            btnRescanFace.onClick.AddListener(RescanCurrentFace);
        }
        if (btnNextFace != null)
        {
            btnNextFace.onClick.RemoveListener(NextFace);
            btnNextFace.onClick.AddListener(NextFace);
        }
        if (btnPrevFace != null)
        {
            btnPrevFace.onClick.RemoveListener(PrevFace);
            btnPrevFace.onClick.AddListener(PrevFace);
        }
        if (btnSwitchCamera != null)
        {
            btnSwitchCamera.onClick.RemoveListener(SwitchCamera);
            btnSwitchCamera.onClick.AddListener(SwitchCamera);
        }
        if (btnApplyToCube != null)
        {
            btnApplyToCube.onClick.RemoveListener(ApplyTo3DCube);
            btnApplyToCube.onClick.AddListener(ApplyTo3DCube);
        }
        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(CloseScanner);
            btnClose.onClick.AddListener(CloseScanner);
        }

        if (sliderDistance != null)
        {
            sliderDistance.minValue = 36f;
            sliderDistance.maxValue = 110f;
            sliderDistance.SetValueWithoutNotify(reticleSpread);
            sliderDistance.onValueChanged.RemoveListener(OnDistanceSliderChanged);
            sliderDistance.onValueChanged.AddListener(OnDistanceSliderChanged);
        }

        if (!eventsBound)
        {
            if (btnNudgeUp != null) btnNudgeUp.onClick.AddListener(() => NudgeReticle(new Vector2(0, 10f)));
            if (btnNudgeDown != null) btnNudgeDown.onClick.AddListener(() => NudgeReticle(new Vector2(0, -10f)));
            if (btnNudgeLeft != null) btnNudgeLeft.onClick.AddListener(() => NudgeReticle(new Vector2(-10f, 0)));
            if (btnNudgeRight != null) btnNudgeRight.onClick.AddListener(() => NudgeReticle(new Vector2(10f, 0)));
            eventsBound = true;
        }

        if (btnResetReticlePos != null)
        {
            btnResetReticlePos.onClick.RemoveListener(ResetReticleOffset);
            btnResetReticlePos.onClick.AddListener(ResetReticleOffset);
        }
        if (btnFlipHorizontal != null)
        {
            btnFlipHorizontal.onClick.RemoveListener(ToggleFlipHorizontal);
            btnFlipHorizontal.onClick.AddListener(ToggleFlipHorizontal);
        }
        if (btnFlipVertical != null)
        {
            btnFlipVertical.onClick.RemoveListener(ToggleFlipVertical);
            btnFlipVertical.onClick.AddListener(ToggleFlipVertical);
        }

        BuildPreviewUI();
        BuildReticleUI();
        BuildFaceBadgesUI();
        ApplyReticleLayout();
        UpdateDistanceLabel();
        UpdateFeedOrientation();
        UpdateFaceBadgesUI();
        UpdateFaceGuideUI();
    }

    private void Update()
    {
        if (scannerPanel == null || !scannerPanel.activeSelf) return;

        bool enterPressed = false;
        bool rightPressed = false;
        bool leftPressed = false;
        bool rPressed = false;
        bool plusPressed = false;
        bool minusPressed = false;
        float scroll = 0f;

        // Đọc tín hiệu Input tương thích hoàn toàn với New Input System của Unity 6
        if (Keyboard.current != null)
        {
            enterPressed = Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.spaceKey.wasPressedThisFrame;

            rightPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame
                        || Keyboard.current.dKey.wasPressedThisFrame;

            leftPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame
                       || Keyboard.current.aKey.wasPressedThisFrame;

            rPressed = Keyboard.current.rKey.wasPressedThisFrame;

            plusPressed = Keyboard.current.equalsKey.wasPressedThisFrame
                       || Keyboard.current.numpadPlusKey.wasPressedThisFrame;

            minusPressed = Keyboard.current.minusKey.wasPressedThisFrame
                        || Keyboard.current.numpadMinusKey.wasPressedThisFrame;
        }

        if (Mouse.current != null)
        {
            scroll = Mouse.current.scroll.ReadValue().y;
        }

        // 1. Phím tắt bàn phím: ENTER hoặc SPACE để chụp mặt tức thì
        if (enterPressed)
        {
            CaptureCurrentFace();
        }
        else if (rightPressed)
        {
            NextFace();
        }
        else if (leftPressed)
        {
            PrevFace();
        }
        else if (rPressed)
        {
            RescanCurrentFace();
        }

        // 2. Lăn chuột hoặc phím +/- để phóng to/thu nhỏ cự ly ngắm (cho phép quét từ xa)
        if (Mathf.Abs(scroll) > 0.01f)
        {
            SetReticleSpread(reticleSpread + Mathf.Sign(scroll) * 4f);
        }
        if (plusPressed)
        {
            SetReticleSpread(reticleSpread + 5f);
        }
        if (minusPressed)
        {
            SetReticleSpread(reticleSpread - 5f);
        }

        // 3. Phím di chuyển khung ngắm: W/S hoặc Shift+Mũi tên lên/xuống để nâng/hạ khung ngắm
        if (Keyboard.current != null)
        {
            float step = 4f;
            if (Keyboard.current.wKey.wasPressedThisFrame || (Keyboard.current.upArrowKey.wasPressedThisFrame && Keyboard.current.shiftKey.isPressed))
                NudgeReticle(new Vector2(0, step));
            if (Keyboard.current.sKey.wasPressedThisFrame || (Keyboard.current.downArrowKey.wasPressedThisFrame && Keyboard.current.shiftKey.isPressed))
                NudgeReticle(new Vector2(0, -step));

            // 4. Phím lật gương / lật camera: M (Lật gương ngang) và V (Lật dọc)
            if (Keyboard.current.mKey.wasPressedThisFrame)
                ToggleFlipHorizontal();
            if (Keyboard.current.vKey.wasPressedThisFrame)
                ToggleFlipVertical();
        }

        // 5. Xử lý lấy mẫu màu thời gian thực từ khung hình
        ProcessLiveColorSampling();
    }

    public void SetReticleSpread(float spread)
    {
        reticleSpread = Mathf.Clamp(spread, 36f, 110f);
        reticleBoxSize = Mathf.Clamp(reticleSpread * 0.82f, 30f, 78f);
        ApplyReticleLayout();

        if (sliderDistance != null && Mathf.Abs(sliderDistance.value - reticleSpread) > 0.5f)
        {
            sliderDistance.value = reticleSpread;
        }

        UpdateDistanceLabel();
    }

    private void OnDistanceSliderChanged(float val)
    {
        SetReticleSpread(val);
    }

    private void UpdateDistanceLabel()
    {
        if (distanceLabelText != null)
        {
            string mode = (reticleSpread <= 48f) ? "Xa (Cầm tay 40-60cm)" : (reticleSpread <= 65f) ? "Vừa (Cầm tay 25-40cm)" : "Gần (Sát camera)";
            distanceLabelText.text = $"Cự ly ngắm: <b>{mode}</b>";
        }
    }

    public void ApplyReticleLayout()
    {
        for (int i = 0; i < 9; i++)
        {
            if (reticleTransforms[i] != null)
            {
                int r = i / 3;
                int c = i % 3;
                float x = (c - 1) * reticleSpread + reticleOffset.x;
                float y = (1 - r) * reticleSpread + reticleOffset.y;
                reticleTransforms[i].anchoredPosition = new Vector2(x, y);
                reticleTransforms[i].sizeDelta = new Vector2(reticleBoxSize, reticleBoxSize);
            }
        }
    }

    public void NudgeReticle(Vector2 delta)
    {
        reticleOffset += delta;
        reticleOffset.x = Mathf.Clamp(reticleOffset.x, -80f, 80f);
        reticleOffset.y = Mathf.Clamp(reticleOffset.y, -110f, 60f);
        ApplyReticleLayout();
    }

    public void ResetReticleOffset()
    {
        reticleOffset = new Vector2(0f, -25f);
        ApplyReticleLayout();
    }

    public void ToggleFlipHorizontal()
    {
        flipHorizontal = !flipHorizontal;
        UpdateFeedOrientation();
        SetStatus(flipHorizontal ? "Đã BẬT chế độ Lật gương (Mirror)" : "Đã TẮT Lật gương (Gốc)", Color.cyan);
    }

    public void ToggleFlipVertical()
    {
        flipVertical = !flipVertical;
        UpdateFeedOrientation();
        SetStatus(flipVertical ? "Đã BẬT Lật dọc (Flip Y)" : "Đã TẮT Lật dọc (Chuẩn)", Color.cyan);
    }

    public void UpdateFeedOrientation()
    {
        if (cameraFeedImage == null) return;

        int videoRotation = (webCamTexture != null) ? webCamTexture.videoRotationAngle : 0;
        cameraFeedImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, -videoRotation);

        bool vMirror = flipVertical;
        if (webCamTexture != null && webCamTexture.videoVerticallyMirrored)
        {
            vMirror = !vMirror;
        }

        float uStart = flipHorizontal ? 1f : 0f;
        float uSize = flipHorizontal ? -1f : 1f;

        float vStart = vMirror ? 1f : 0f;
        float vSize = vMirror ? -1f : 1f;

        cameraFeedImage.uvRect = new Rect(uStart, vStart, uSize, vSize);

        UpdateFlipButtonsUI();
    }

    private void UpdateFlipButtonsUI()
    {
        if (btnFlipHorizontal != null)
        {
            var txt = btnFlipHorizontal.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = flipHorizontal ? "Gương: BẬT" : "Gương: TẮT";
                txt.color = flipHorizontal ? Color.white : RubikUITheme.Slate700;
            }
            var img = btnFlipHorizontal.GetComponent<Image>();
            if (img != null)
            {
                img.color = flipHorizontal ? new Color32(0x10, 0xB9, 0x81, 255) : RubikUITheme.Slate200;
            }
        }

        if (btnFlipVertical != null)
        {
            var txt = btnFlipVertical.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = flipVertical ? "Dọc: LẬT" : "Dọc: CHUẨN";
                txt.color = flipVertical ? Color.white : RubikUITheme.Slate700;
            }
            var img = btnFlipVertical.GetComponent<Image>();
            if (img != null)
            {
                img.color = flipVertical ? new Color32(0xF5, 0x9E, 0x0B, 255) : RubikUITheme.Slate200;
            }
        }
    }

    private void OnDisable()
    {
        StopCamera();
    }

    private void OnDestroy()
    {
        StopCamera();
        if (mockCameraTexture != null)
            Destroy(mockCameraTexture);
    }

    // =========================================================================
    // 1. ĐIỀU KHIỂN BẬT/TẮT VÀ QUẢN LÝ WEBCAM
    // =========================================================================

    public void ToggleScanner()
    {
        AutoBindReferences();

        if (scannerPanel == null) return;

        if (scannerPanel.activeSelf)
        {
            CloseScanner();
        }
        else
        {
            OpenScanner();
        }
    }

    public void OpenScanner()
    {
        BindScannerUIAndEvents();

        if (scannerPanel == null) return;
        scannerPanel.SetActive(true);
        scannerPanel.transform.SetAsLastSibling();

        StartCamera();
        currentFaceIndex = 0;
        UpdateFaceGuideUI();
        UpdatePreviewUIFromStored();
        UpdateFaceBadgesUI();
        SetStatus("Đã kết nối Camera. Hãy căn chỉnh khối Rubik vừa khớp khung ngắm 3x3.", Color.cyan);
    }

    public void CloseScanner()
    {
        StopCamera();
        if (scannerPanel != null)
            scannerPanel.SetActive(false);

        RubikUIController ui = FindAnyObjectByType<RubikUIController>();
        if (ui != null) ui.OnModalClosed();
    }

    private void StartCamera()
    {
        StopCamera();

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        }
#endif

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            isSimulatingCamera = true;
            SetStatus("Không tìm thấy Webcam phần cứng. Đang chuyển sang chế độ Mô phỏng Kiểm thử.", Color.yellow);
            if (cameraInfoText != null) cameraInfoText.text = "Nguồn: Mô phỏng ảo (Virtual Mock)";
            CreateMockCameraFeed();
            return;
        }

        isSimulatingCamera = false;

        // Ưu tiên chọn Rear Camera (Camera sau) trên điện thoại ở lần mở camera đầu tiên
        if (currentDeviceIndex == 0 && devices.Length > 1)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (!devices[i].isFrontFacing)
                {
                    currentDeviceIndex = i;
                    flipHorizontal = false; // Camera sau không dùng lật gương
                    break;
                }
            }
        }

        if (currentDeviceIndex >= devices.Length) currentDeviceIndex = 0;

        string deviceName = devices[currentDeviceIndex].name;
        bool isFront = devices[currentDeviceIndex].isFrontFacing;
        if (cameraInfoText != null)
        {
            cameraInfoText.text = $"Camera: {(isFront ? "Trước" : "Sau")} - {deviceName} ({currentDeviceIndex + 1}/{devices.Length})";
        }

        // Khởi tạo luồng WebCamTexture độ phân giải 640x480 tốc độ 30 FPS
        webCamTexture = new WebCamTexture(deviceName, 640, 480, 30);
        if (cameraFeedImage != null)
        {
            cameraFeedImage.texture = webCamTexture;
            cameraFeedImage.material = null;
        }

        webCamTexture.Play();
        UpdateFeedOrientation();
    }

    private void StopCamera()
    {
        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
                webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }
    }

    public void SwitchCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length <= 1)
        {
            SetStatus("Chỉ có 1 camera hoặc không có webcam phụ.", Color.gray);
            return;
        }

        currentDeviceIndex = (currentDeviceIndex + 1) % devices.Length;
        flipHorizontal = devices[currentDeviceIndex].isFrontFacing;
        StartCamera();
    }

    private void CreateMockCameraFeed()
    {
        if (mockCameraTexture == null)
        {
            mockCameraTexture = new Texture2D(320, 320, TextureFormat.RGBA32, false);
        }

        // Vẽ lưới màu Rubik mô phỏng để test
        Color[] cols = new Color[320 * 320];
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                int r = 2 - (y / (320 / 3));
                int c = x / (320 / 3);
                char face = FaceOrder[currentFaceIndex];
                char colCode = DefaultCenterColors[currentFaceIndex];
                cols[y * 320 + x] = WcaReferenceColors[colCode];
            }
        }
        mockCameraTexture.SetPixels(cols);
        mockCameraTexture.Apply();

        if (cameraFeedImage != null)
            cameraFeedImage.texture = mockCameraTexture;
        UpdateFeedOrientation();
    }

    // =========================================================================
    // 2. THU NHẬN GIÁ TRỊ MÀU THỜI GIAN THỰC & AUTO-CALIBRATE
    // =========================================================================

    private void ProcessLiveColorSampling()
    {
        Texture sourceTex = (webCamTexture != null && webCamTexture.isPlaying)
            ? (Texture)webCamTexture
            : (Texture)mockCameraTexture;

        if (sourceTex == null) return;

        int texWidth = (webCamTexture != null) ? webCamTexture.width : mockCameraTexture.width;
        int texHeight = (webCamTexture != null) ? webCamTexture.height : mockCameraTexture.height;

        if (texWidth <= 16 || texHeight <= 16) return;

        Vector2 feedSize = (cameraFeedImage != null) ? cameraFeedImage.rectTransform.rect.size : new Vector2(330f, 330f);
        if (feedSize.x <= 10f || feedSize.y <= 10f) feedSize = new Vector2(330f, 330f);

        // Lấy mẫu 9 điểm trên lưới 3x3 tương ứng chính xác vị trí của từng khung ngắm trên màn hình
        for (int i = 0; i < 9; i++)
        {
            float screenU, screenV;
            if (reticleTransforms[i] != null)
            {
                Vector2 pos = reticleTransforms[i].anchoredPosition;
                screenU = 0.5f + (pos.x / feedSize.x);
                screenV = 0.5f + (pos.y / feedSize.y);
            }
            else
            {
                int r = i / 3;
                int c = i % 3;
                screenU = 0.5f + ((c - 1) * 88f / feedSize.x);
                screenV = 0.5f + ((1 - r) * 88f / feedSize.y);
            }

            // Ánh xạ tọa độ màn hình sang tọa độ pixel texture tương ứng với uvRect và góc xoay cảm biến camera điện thoại
            float mappedU = screenU;
            float mappedV = screenV;

            int rot = (webCamTexture != null) ? webCamTexture.videoRotationAngle : 0;
            if (rot == 90)
            {
                mappedU = 1.0f - screenV;
                mappedV = screenU;
            }
            else if (rot == 180)
            {
                mappedU = 1.0f - screenU;
                mappedV = 1.0f - screenV;
            }
            else if (rot == 270)
            {
                mappedU = screenV;
                mappedV = 1.0f - screenU;
            }

            float texU = flipHorizontal ? (1.0f - mappedU) : mappedU;

            bool vMirror = flipVertical;
            if (webCamTexture != null && webCamTexture.videoVerticallyMirrored)
            {
                vMirror = !vMirror;
            }
            float texV = vMirror ? (1.0f - mappedV) : mappedV;

            int px = Mathf.Clamp(Mathf.RoundToInt(texU * texWidth), 10, texWidth - 11);
            int py = Mathf.Clamp(Mathf.RoundToInt(texV * texHeight), 10, texHeight - 11);

            // Sử dụng bộ lấy mẫu trung bình loại trừ lóa sáng và viền đen (Trimmed Mean)
            Color avgColor = SamplePixelKernelTrimmed(px, py, 15);
            liveSampledColors[i] = avgColor;

            // Phân loại màu bằng thuật toán Chromaticity + HSV chuẩn xác
            char detectedColor = ClassifyColor(avgColor);
            liveDetectedColors[i] = detectedColor;

            // Cập nhật màu viền reticle trên camera feed
            if (reticleCellBorders[i] != null)
            {
                reticleCellBorders[i].color = WcaReferenceColors[detectedColor];
            }

            // Hiển thị ký tự màu trực tiếp trên ô ngắm
            if (reticleLabels[i] != null)
            {
                reticleLabels[i].text = detectedColor.ToString();
                reticleLabels[i].color = (detectedColor == 'W' || detectedColor == 'Y') ? Color.black : Color.white;
            }
        }

        // Nếu mặt hiện tại chưa được chụp lưu, hiển thị màu nhận diện live lên Preview
        char currentFace = FaceOrder[currentFaceIndex];
        if (!completedFaces.Contains(currentFace))
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int previewIdx = r * 3 + c;
                    int sourceRow = flipVertical ? (2 - r) : r;
                    int sourceCol = flipHorizontal ? (2 - c) : c;
                    int sourceIdx = sourceRow * 3 + sourceCol;

                    if (previewCellImages[previewIdx] != null)
                    {
                        previewCellImages[previewIdx].color = WcaReferenceColors[liveDetectedColors[sourceIdx]];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Lấy mẫu pixel với thuật toán Trimmed Mean tái sử dụng bộ đệm (Zero GC Allocation),
    /// loại bỏ 20% pixel tối nhất do rãnh đen và 15% pixel sáng nhất do lóa đèn trần.
    /// </summary>
    private Color SamplePixelKernelTrimmed(int cx, int cy, int kernelSize)
    {
        int half = kernelSize / 2;
        int totalPixels = kernelSize * kernelSize;

        if (kernelColorBuffer == null || kernelColorBuffer.Length < totalPixels)
        {
            kernelColorBuffer = new Color[totalPixels];
            kernelLuminanceBuffer = new float[totalPixels];
            kernelIndexBuffer = new int[totalPixels];
        }

        int collectedCount = 0;

        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            int startX = Mathf.Clamp(cx - half, 0, Mathf.Max(0, webCamTexture.width - kernelSize));
            int startY = Mathf.Clamp(cy - half, 0, Mathf.Max(0, webCamTexture.height - kernelSize));
            Color[] pixels = webCamTexture.GetPixels(startX, startY, kernelSize, kernelSize);
            collectedCount = Mathf.Min(pixels.Length, totalPixels);
            for (int i = 0; i < collectedCount; i++)
            {
                kernelColorBuffer[i] = pixels[i];
                kernelLuminanceBuffer[i] = 0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b;
                kernelIndexBuffer[i] = i;
            }
        }
        else if (mockCameraTexture != null)
        {
            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= half; x++)
                {
                    if (collectedCount >= totalPixels) break;
                    int px = Mathf.Clamp(cx + x, 0, mockCameraTexture.width - 1);
                    int py = Mathf.Clamp(cy + y, 0, mockCameraTexture.height - 1);
                    Color col = mockCameraTexture.GetPixel(px, py);
                    kernelColorBuffer[collectedCount] = col;
                    kernelLuminanceBuffer[collectedCount] = 0.299f * col.r + 0.587f * col.g + 0.114f * col.b;
                    kernelIndexBuffer[collectedCount] = collectedCount;
                    collectedCount++;
                }
            }
        }

        if (collectedCount == 0) return Color.black;

        // Sắp xếp các chỉ số theo độ sáng in-place (Không phát sinh rác GC)
        System.Array.Sort(kernelLuminanceBuffer, kernelIndexBuffer, 0, collectedCount);

        // Bỏ qua 20% pixel tối nhất (viền đen / rãnh cubie) và 15% pixel sáng nhất (vết phản quang bóng nhẫy / lóa đèn)
        int trimLow = Mathf.RoundToInt(collectedCount * 0.20f);
        int trimHigh = Mathf.RoundToInt(collectedCount * 0.15f);

        float r = 0f, g = 0f, b = 0f;
        int validCount = 0;

        for (int i = trimLow; i < collectedCount - trimHigh; i++)
        {
            int origIdx = kernelIndexBuffer[i];
            r += kernelColorBuffer[origIdx].r;
            g += kernelColorBuffer[origIdx].g;
            b += kernelColorBuffer[origIdx].b;
            validCount++;
        }

        if (validCount == 0) return kernelColorBuffer[kernelIndexBuffer[collectedCount / 2]];

        return new Color(r / validCount, g / validCount, b / validCount);
    }

    /// <summary>
    /// Phân loại màu chuẩn xác cao kết hợp Normalized Chromaticity, không gian HSV,
    /// bảo vệ nghiêm ngặt chống gán nhầm sang White, và nhận diện độ tin cậy để trả về '?' khi không rõ.
    /// </summary>
    private char ClassifyColor(Color c)
    {
        float total = c.r + c.g + c.b;

        // 0. Kiểm tra mẫu quá tối / bóng đen / khe rãnh nhựa:
        // Tuyệt đối không bao giờ mặc định mẫu tối thành White! Trả về '?' để yêu cầu chỉnh góc ngắm/ánh sáng.
        if (total < 0.16f || (c.r < 0.08f && c.g < 0.08f && c.b < 0.08f))
        {
            return '?';
        }

        float nr = c.r / total;
        float ng = c.g / total;
        float nb = c.b / total;

        Color.RGBToHSV(c, out float h, out float s, out float v);
        float hueDeg = h * 360f;

        // Vùng tối mờ / xám mù do thiếu sáng hoặc bóng đổ
        if (v < 0.22f && s < 0.22f)
        {
            return '?';
        }

        // 1. Kiểm tra bộ hiệu chuẩn động (Auto-Calibrate):
        // Chỉ kích hoạt khi đã thu thập đủ trọn vẹn 6 tâm mặt W, Y, G, B, R, O
        bool useCalibrated = (toggleAutoCalibrate == null || toggleAutoCalibrate.isOn) && calibratedColors.Count == 6;
        if (useCalibrated)
        {
            char bestMatch = '?';
            float minDistance = float.MaxValue;

            foreach (var pair in calibratedColors)
            {
                char code = pair.Key;
                Color calCol = pair.Value;

                float calTotal = calCol.r + calCol.g + calCol.b;
                float cnr = (calTotal > 0.01f) ? (calCol.r / calTotal) : 0.33f;
                float cng = (calTotal > 0.01f) ? (calCol.g / calTotal) : 0.33f;
                float cnb = (calTotal > 0.01f) ? (calCol.b / calTotal) : 0.33f;

                Color.RGBToHSV(calCol, out float ch, out float cs, out float cv);
                float chueDeg = ch * 360f;

                float dChrom = Mathf.Sqrt(Mathf.Pow(nr - cnr, 2) + Mathf.Pow(ng - cng, 2) + Mathf.Pow(nb - cnb, 2));
                float dHue = Mathf.Min(Mathf.Abs(hueDeg - chueDeg), 360f - Mathf.Abs(hueDeg - chueDeg)) / 180f;

                float dist = dChrom * 2.0f + dHue;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestMatch = code;
                }
            }

            // Chỉ dùng kết quả hiệu chuẩn nếu khoảng cách đủ gần (ngưỡng tin cậy cao)
            if (minDistance < 0.38f && bestMatch != '?')
            {
                return bestMatch;
            }
            // Nếu không đủ tin cậy, tiếp tục rơi xuống bộ phân loại quang phổ rule-based bên dưới
        }

        // 2. Phân biệt màu Trắng (White):
        // Màu trắng có độ sáng cao (V >= 0.42), độ bão hòa thấp (S <= 0.22), và 3 kênh R,G,B cân bằng.
        // Khóa bảo vệ nghiêm ngặt: Nếu có bất kỳ kênh nào vượt trội, TUYỆT ĐỐI không phải Trắng.
        bool hasColorImbalance = (c.r > c.g * 1.30f) || (c.b > c.g * 1.35f) || (c.g > c.r * 1.30f) || (c.g > c.b * 1.35f);
        if (!hasColorImbalance)
        {
            if (v >= 0.48f && s <= 0.22f)
            {
                return 'W';
            }
            if (v >= 0.40f && s <= 0.15f && Mathf.Abs(c.r - c.g) < 0.10f && Mathf.Abs(c.g - c.b) < 0.10f)
            {
                return 'W';
            }
        }

        // 3. Màu Xanh Dương (Blue):
        // Kênh Blue chiếm ưu thế rõ rệt
        if ((hueDeg >= 165f && hueDeg <= 275f && s >= 0.20f) || (c.b > 0.32f && c.b > c.r * 1.15f && c.b > c.g * 1.10f))
        {
            return 'B';
        }

        // 4. Màu Xanh Lá (Green):
        // Kênh Green chiếm ưu thế rõ rệt
        if ((hueDeg >= 72f && hueDeg < 165f && s >= 0.20f) || (c.g > 0.32f && c.g > c.r * 1.15f && c.g > c.b * 1.12f))
        {
            return 'G';
        }

        // 5. Màu Vàng (Yellow):
        // Cả Red và Green đều cao, Green chiếm tỷ lệ lớn (G/R >= 0.58), Blue thấp (B < G * 0.78), Hue trong [38°..75°]
        float grRatio = (c.r > 0.001f) ? (c.g / c.r) : 0f;
        if (s >= 0.24f && c.b < c.g * 0.78f)
        {
            if (hueDeg >= 38f && hueDeg < 75f && grRatio >= 0.58f)
            {
                return 'Y';
            }
            if (grRatio >= 0.72f && hueDeg >= 30f)
            {
                return 'Y';
            }
        }

        // 6. Phân biệt Hồng / Đỏ San Hô (Stickerless Red) vs Cam (Orange) vs Đỏ Cờ (Classic Red):
        // A) Hồng / Đỏ San Hô (Stickerless Red):
        // Đặc điểm: Blue cao rõ rệt so với Green (B >= G * 0.96 và R > G * 1.20), Hue trong dải tím hồng [305°..360°] hoặc [0°..5°]
        if ((hueDeg >= 305f || hueDeg <= 5f) && (c.b >= c.g * 0.96f) && (c.r > c.g * 1.20f))
        {
            return 'R';
        }
        if (c.b >= c.g * 1.02f && c.r > c.g * 1.20f)
        {
            return 'R'; // Hồng / Đỏ San Hô
        }

        // B) Cam (Orange):
        // Đặc điểm: Green cao hơn Blue rõ rệt (G >= B * 1.05), Red lớn hơn Green, Hue trong [6°..38°]
        if (c.g > c.b * 1.05f && c.r > c.g && (hueDeg >= 6f && hueDeg < 38f))
        {
            return 'O';
        }
        if (c.g > c.b * 1.10f && c.r > c.g * 1.15f)
        {
            return 'O';
        }

        // C) Đỏ Cờ đậm (Classic Red):
        // Red áp đảo cả Green và Blue (R > 1.5G và R > 1.5B), Hue sát 0°
        if (c.r > c.g * 1.50f && c.r > c.b * 1.50f && (hueDeg >= 345f || hueDeg <= 12f))
        {
            return 'R';
        }

        // 7. Dự phòng có căn cứ (Heuristic Fallback cho gam màu ấm):
        if (c.r > c.g && c.r > c.b && s >= 0.30f)
        {
            if (c.g > c.b * 1.08f) return 'O';
            if (c.b > c.g * 0.95f) return 'R';
        }

        // Không chắc chắn: Tuyệt đối không ép thành màu trắng! Trả về '?' để cảnh báo người dùng
        return '?';
    }

    // =========================================================================
    // 3. TIẾN TRÌNH QUÉT TỪNG MẶT & ĐIỀU HƯỚNG
    // =========================================================================

    public void CaptureCurrentFace()
    {
        char currentFace = FaceOrder[currentFaceIndex];
        char[] faceData = new char[9];
        bool hasUnknownCell = false;

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int targetIdx = r * 3 + c;
                int sourceRow = flipVertical ? (2 - r) : r;
                int sourceCol = flipHorizontal ? (2 - c) : c;
                int sourceIdx = sourceRow * 3 + sourceCol;

                char detected = liveDetectedColors[sourceIdx];
                faceData[targetIdx] = detected;
                if (detected == '?') hasUnknownCell = true;
            }
        }

        // Không bắt buộc màu tâm: nhận diện trực tiếp màu thực tế tại ô tâm (ô thứ 4)
        char centerColor = faceData[4];

        // Auto-Calibrate: Chỉ lưu mẫu tâm vào bộ hiệu chuẩn khi màu tâm rõ ràng và hợp lệ
        if (toggleAutoCalibrate == null || toggleAutoCalibrate.isOn)
        {
            if (IsValidWcaColor(centerColor))
            {
                calibratedColors[centerColor] = liveSampledColors[4];
            }
        }

        scannedFaces[currentFace] = faceData;
        completedFaces.Add(currentFace);

        UpdatePreviewUIFromStored();
        UpdateFaceBadgesUI();

        if (hasUnknownCell)
        {
            SetStatus($"Đã lưu mặt {currentFace}, nhưng có ô chưa rõ màu (?). Hãy nhấp vào ô trên lưới xem trước để sửa màu thủ công!", Color.yellow);
        }
        else
        {
            SetStatus($"Đã lưu mặt {currentFace}! ({completedFaces.Count}/6 mặt đã xong)", Color.green);
        }

        // Tự động chuyển sang mặt tiếp theo nếu còn mặt chưa quét
        if (completedFaces.Count < 6)
        {
            NextFace();
        }
        else
        {
            SetStatus("Tuyệt vời! Đã quét đủ 6 mặt. Hãy kiểm tra các ô trên lưới xem trước và bấm 'Đồng bộ lên khối 3D'.", Color.cyan);
        }
    }

    public void RescanCurrentFace()
    {
        char currentFace = FaceOrder[currentFaceIndex];
        completedFaces.Remove(currentFace);
        UpdateFaceBadgesUI();
        SetStatus($"Đang quét lại mặt {currentFace}...", Color.yellow);
    }

    public void NextFace()
    {
        currentFaceIndex = (currentFaceIndex + 1) % 6;
        UpdateFaceGuideUI();
        UpdatePreviewUIFromStored();
        UpdateFaceBadgesUI();
    }

    public void PrevFace()
    {
        currentFaceIndex = (currentFaceIndex + 5) % 6;
        UpdateFaceGuideUI();
        UpdatePreviewUIFromStored();
        UpdateFaceBadgesUI();
    }

    private void UpdateFaceGuideUI()
    {
        if (faceTitleText != null)
            faceTitleText.text = $"MẶT {currentFaceIndex + 1}/6: {FaceNamesVi[currentFaceIndex]}";

        if (faceGuideText != null)
            faceGuideText.text = FaceGuidesVi[currentFaceIndex];
    }

    private void BuildFaceBadgesUI()
    {
        if (faceBadgesContainer == null) return;

        for (int i = 0; i < 6; i++)
        {
            char f = FaceOrder[i];
            Transform badge = faceBadgesContainer.Find($"Badge_{f}");
            if (badge == null && i < faceBadgesContainer.childCount)
                badge = faceBadgesContainer.GetChild(i);

            if (badge != null)
                faceBadgeImages[i] = badge.GetComponent<Image>();
        }
    }

    private void UpdateFaceBadgesUI()
    {
        if (faceBadgeImages[0] == null)
            BuildFaceBadgesUI();

        for (int i = 0; i < 6; i++)
        {
            if (faceBadgeImages[i] == null) continue;
            char f = FaceOrder[i];
            bool done = completedFaces.Contains(f);
            bool isCurrent = (i == currentFaceIndex);

            var badgeTxt = faceBadgeImages[i].GetComponentInChildren<TextMeshProUGUI>(true);

            if (done)
            {
                faceBadgeImages[i].color = RubikUITheme.Success; // Xanh lá đã quét xong
                if (badgeTxt != null) badgeTxt.color = Color.white;
            }
            else if (isCurrent)
            {
                faceBadgeImages[i].color = RubikUITheme.Blue600; // Xanh dương đang quét
                if (badgeTxt != null) badgeTxt.color = Color.white;
            }
            else
            {
                faceBadgeImages[i].color = RubikUITheme.Slate200; // Xám sáng chưa quét
                if (badgeTxt != null) badgeTxt.color = RubikUITheme.Slate600;
            }
        }
    }

    private void UpdatePreviewUIFromStored()
    {
        char currentFace = FaceOrder[currentFaceIndex];
        if (scannedFaces.TryGetValue(currentFace, out char[] colors))
        {
            for (int i = 0; i < 9; i++)
            {
                if (previewCellImages[i] != null)
                {
                    char col = colors[i];
                    previewCellImages[i].color = WcaReferenceColors.ContainsKey(col) ? WcaReferenceColors[col] : Color.gray;
                }
            }
        }
    }

    // =========================================================================
    // 4. KIỂM TRA HỢP LỆ & ĐỒNG BỘ LÊN 27 CUBIE (CUSTOM MATERIAL SYNC)
    // =========================================================================

    public void ApplyTo3DCube()
    {
        if (completedFaces.Count < 6)
        {
            SetStatus($"Chưa quét đủ 6 mặt! Hiện mới xong {completedFaces.Count}/6 mặt.", Color.red);
            return;
        }

        // 1. Kiểm tra không còn ô nào chưa rõ màu ('?')
        foreach (var pair in scannedFaces)
        {
            for (int i = 0; i < 9; i++)
            {
                if (pair.Value[i] == '?')
                {
                    SetStatus($"Mặt {pair.Key} còn ô thứ {i + 1} chưa rõ màu (?). Vui lòng nhấp vào ô đó trên bảng xem trước để chọn màu chuẩn!", Color.red);
                    return;
                }
            }
        }

        // 2. Kiểm tra số lượng mỗi màu phải đúng 9 ô
        Dictionary<char, int> counts = new Dictionary<char, int>()
        {
            { 'W', 0 }, { 'Y', 0 }, { 'G', 0 }, { 'B', 0 }, { 'R', 0 }, { 'O', 0 }
        };

        foreach (var pair in scannedFaces)
        {
            foreach (char color in pair.Value)
            {
                if (counts.ContainsKey(color))
                {
                    counts[color]++;
                }
                else
                {
                    SetStatus($"Phát hiện ô màu không hợp lệ '{color}'. Hãy nhấp vào ô để sửa!", Color.red);
                    return;
                }
            }
        }

        foreach (var pair in counts)
        {
            if (pair.Value != 9)
            {
                SetStatus($"Sai lệch số màu: Màu '{pair.Key}' có {pair.Value}/9 ô (cần đúng 9). Hãy nhấp vào các ô xem trước để chỉnh sửa!", Color.red);
                return;
            }
        }

        // 3. Kiểm tra tính duy nhất của 6 màu tâm (không được trùng tâm giữa các mặt)
        HashSet<char> centerColors = new HashSet<char>();
        foreach (var pair in scannedFaces)
        {
            char center = pair.Value[4];
            if (centerColors.Contains(center))
            {
                SetStatus($"Lỗi cấu trúc: Có 2 mặt bị trùng cùng màu tâm '{center}'. Vui lòng quét lại mặt bị trùng!", Color.red);
                return;
            }
            centerColors.Add(center);
        }

        // 4. Kiểm tra tính hợp lệ vật lý của 12 viên cạnh (Parity Check)
        if (!ValidatePhysicalEdges(out string edgeError))
        {
            SetStatus($"Lỗi vật lý Rubik: {edgeError}. Hãy nhấp vào các ô xem trước để sửa lại!", Color.red);
            return;
        }

        // 5. Reset mô hình 3D về trạng thái gốc
        if (stateManager != null) stateManager.ResetAll();

        // 6. Cập nhật dữ liệu logic RubikState
        RubikState state = stateManager.CurrentState;
        foreach (var pair in scannedFaces)
        {
            char face = pair.Key;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    state.SetColor(face, r, c, pair.Value[r * 3 + c]);
                }
            }
        }

        // 7. Đồng bộ chất liệu màu chuẩn WCA (đồng nhất 100% giữa bảng 2D và mô hình 3D)
        bool useCustomMaterials = (toggleCustomMaterialSync != null && toggleCustomMaterialSync.isOn);
        Update3DStickersWithMaterials(state, useCustomMaterials);

        // Khởi tạo trước bảng tra cứu Kociemba trên luồng phụ để khi nhấn Giải tự động cực mượt
        RubikSolver.WarmupTablesInBackground();

        SetStatus("Đã đồng bộ màu từ Camera lên khối Rubik 3D thành công 100%!", Color.green);
        CloseScanner();
    }

    private bool IsValidWcaColor(char c)
    {
        return c == 'W' || c == 'Y' || c == 'G' || c == 'B' || c == 'R' || c == 'O';
    }

    private bool ValidatePhysicalEdges(out string error)
    {
        error = "";
        // Danh sách 12 viên cạnh chuẩn (Face1, Index1, Face2, Index2)
        (char, int, char, int)[] edges = new (char, int, char, int)[]
        {
            ('U', 7, 'F', 1), ('U', 3, 'L', 1), ('U', 1, 'B', 1), ('U', 5, 'R', 1),
            ('D', 1, 'F', 7), ('D', 3, 'L', 7), ('D', 7, 'B', 7), ('D', 5, 'R', 7),
            ('F', 3, 'L', 5), ('F', 5, 'R', 3), ('B', 5, 'L', 3), ('B', 3, 'R', 5)
        };

        char centerF = scannedFaces.ContainsKey('F') ? scannedFaces['F'][4] : 'G';
        char centerB = scannedFaces.ContainsKey('B') ? scannedFaces['B'][4] : 'B';
        char centerR = scannedFaces.ContainsKey('R') ? scannedFaces['R'][4] : 'R';
        char centerL = scannedFaces.ContainsKey('L') ? scannedFaces['L'][4] : 'O';
        char centerU = scannedFaces.ContainsKey('U') ? scannedFaces['U'][4] : 'W';
        char centerD = scannedFaces.ContainsKey('D') ? scannedFaces['D'][4] : 'Y';

        foreach (var e in edges)
        {
            if (!scannedFaces.ContainsKey(e.Item1) || !scannedFaces.ContainsKey(e.Item3)) continue;

            char c1 = scannedFaces[e.Item1][e.Item2];
            char c2 = scannedFaces[e.Item3][e.Item4];

            if (c1 == c2)
            {
                error = $"Viên cạnh ({e.Item1}-{e.Item3}) có 2 nhãn dán cùng màu '{c1}'";
                return false;
            }

            // Kiểm tra 2 màu trên cùng viên cạnh có thuộc cặp tâm đối diện không
            bool isOpposite = (c1 == centerF && c2 == centerB) || (c1 == centerB && c2 == centerF)
                           || (c1 == centerR && c2 == centerL) || (c1 == centerL && c2 == centerR)
                           || (c1 == centerU && c2 == centerD) || (c1 == centerD && c2 == centerU);

            if (isOpposite)
            {
                error = $"Viên cạnh ({e.Item1}-{e.Item3}) chứa 2 màu tâm đối diện ({c1}-{c2})";
                return false;
            }
        }

        return true;
    }

    private void Update3DStickersWithMaterials(RubikState state, bool useCustomSync)
    {
        if (generator == null || generator.allCubies == null) return;

        float spacing = generator.spacing;

        // Chuẩn bị dictionary chất liệu
        Dictionary<char, Material> activeMats = new Dictionary<char, Material>();
        foreach (char code in DefaultCenterColors)
        {
            Material baseMat = GetBaseMaterial(code);
            if (useCustomSync && calibratedColors.TryGetValue(code, out Color realColor))
            {
                // Nhân bản Material và áp dụng màu chụp thực tế
                Material customMat = new Material(baseMat);
                customMat.color = realColor;
                if (customMat.HasProperty("_BaseColor"))
                    customMat.SetColor("_BaseColor", realColor);
                activeMats[code] = customMat;
            }
            else
            {
                activeMats[code] = baseMat;
            }
        }

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null) continue;

            int x = Mathf.RoundToInt(cubie.localPosition.x / spacing);
            int y = Mathf.RoundToInt(cubie.localPosition.y / spacing);
            int z = Mathf.RoundToInt(cubie.localPosition.z / spacing);

            // Cập nhật từng mặt của Cubie (Chuẩn hệ tọa độ 3D đồng nhất với sơ đồ 2D: U=+Y, D=-Y, F=-Z, B=+Z, R=+X, L=-X)
            if (y == 1) ApplySticker(cubie, "Sticker_Up", activeMats[state.GetColor('U', 1 - z, x + 1)]);
            if (y == -1) ApplySticker(cubie, "Sticker_Down", activeMats[state.GetColor('D', z + 1, x + 1)]);
            if (z == -1) ApplySticker(cubie, "Sticker_Front", activeMats[state.GetColor('F', 1 - y, x + 1)]);
            if (z == 1) ApplySticker(cubie, "Sticker_Back", activeMats[state.GetColor('B', 1 - y, 1 - x)]);
            if (x == 1) ApplySticker(cubie, "Sticker_Right", activeMats[state.GetColor('R', 1 - y, z + 1)]);
            if (x == -1) ApplySticker(cubie, "Sticker_Left", activeMats[state.GetColor('L', 1 - y, 1 - z)]);
        }
    }

    private void ApplySticker(Transform cubie, string stickerName, Material mat)
    {
        Transform s = cubie.Find(stickerName);
        if (s == null) return;
        Renderer r = s.GetComponent<Renderer>();
        if (r != null) r.material = mat;
    }

    private Material GetBaseMaterial(char c)
    {
        if (generator == null) return null;
        switch (c)
        {
            case 'W': return generator.matWhite;
            case 'Y': return generator.matYellow;
            case 'G': return generator.matGreen;
            case 'B': return generator.matBlue;
            case 'R': return generator.matRed;
            case 'O': return generator.matOrange;
            default: return generator.matBlack;
        }
    }

    // =========================================================================
    // 5. XÂY DỰNG GIAO DIỆN UI NỘI BỘ & TỰ ĐỘNG LIÊN KẾT
    // =========================================================================

    private void BuildPreviewUI()
    {
        if (previewGridContainer == null) return;

        // Tìm hoặc tạo 9 ô button xem trước với góc bo mềm mại chuẩn Friendly Puzzle
        for (int i = 0; i < 9; i++)
        {
            Transform cell = previewGridContainer.Find($"Cell_{i}");
            if (cell == null)
            {
                GameObject obj = new GameObject($"Cell_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                obj.transform.SetParent(previewGridContainer, false);
                cell = obj.transform;
            }

            Image img = cell.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusXs);
                img.type = Image.Type.Sliced;
            }
            Outline outline = cell.GetComponent<Outline>();
            if (outline == null) outline = cell.gameObject.AddComponent<Outline>();
            outline.effectColor = RubikUITheme.Ink(0.16f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            previewCellImages[i] = img;
            Button btn = cell.GetComponent<Button>();

            int cellIndex = i;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPreviewCellClicked(cellIndex));
            }
        }
    }

    private void BuildReticleUI()
    {
        if (reticleContainer == null) return;

        for (int i = 0; i < 9; i++)
        {
            Transform box = reticleContainer.Find($"Reticle_{i}");
            if (box != null)
            {
                reticleTransforms[i] = box.GetComponent<RectTransform>();
                reticleCellBorders[i] = box.GetComponent<Image>();
                if (reticleCellBorders[i] != null && reticleCellBorders[i].sprite == null)
                {
                    reticleCellBorders[i].sprite = RubikUISpriteFactory.GetRoundedSprite(RubikUITheme.RadiusSm, 4);
                    reticleCellBorders[i].type = Image.Type.Sliced;
                }
                reticleLabels[i] = box.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    private void OnPreviewCellClicked(int index)
    {
        // Cho phép người dùng nhấp vào từng ô để đổi màu thủ công nếu camera nhận diện sai
        char currentFace = FaceOrder[currentFaceIndex];
        if (!scannedFaces.ContainsKey(currentFace))
        {
            scannedFaces[currentFace] = new char[9];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int previewIdx = r * 3 + c;
                    int sourceRow = flipVertical ? (2 - r) : r;
                    int sourceCol = flipHorizontal ? (2 - c) : c;
                    int sourceIdx = sourceRow * 3 + sourceCol;
                    scannedFaces[currentFace][previewIdx] = liveDetectedColors[sourceIdx];
                }
            }
            completedFaces.Add(currentFace);
        }

        char currentColor = scannedFaces[currentFace][index];
        char nextColor = CycleNextColor(currentColor);
        scannedFaces[currentFace][index] = nextColor;

        if (previewCellImages[index] != null)
            previewCellImages[index].color = WcaReferenceColors.ContainsKey(nextColor) ? WcaReferenceColors[nextColor] : Color.white;

        UpdateFaceBadgesUI();
        SetStatus($"Đã chỉnh ô {index + 1} sang màu '{nextColor}'", Color.cyan);
    }

    private char CycleNextColor(char c)
    {
        char[] seq = { 'W', 'Y', 'G', 'B', 'R', 'O' };
        int idx = Array.IndexOf(seq, c);
        if (idx < 0) return 'W';
        return seq[(idx + 1) % seq.Length];
    }

    private void InitDefaultData()
    {
        scannedFaces.Clear();
        completedFaces.Clear();
        calibratedColors.Clear();

        for (int f = 0; f < 6; f++)
        {
            char faceCode = FaceOrder[f];
            char defaultCol = DefaultCenterColors[f];
            char[] arr = new char[9];
            for (int i = 0; i < 9; i++) arr[i] = defaultCol;
            scannedFaces[faceCode] = arr;
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public void AutoBindReferences()
    {
        if (generator == null) generator = FindAnyObjectByType<RubikGenerator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();

        if (scannerPanel == null)
        {
            var p = GameObject.Find("CameraScannerPanel");
            if (p == null)
            {
                var all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
                foreach (var r in all)
                {
                    if (r.name == "CameraScannerPanel")
                    {
                        p = r.gameObject;
                        break;
                    }
                }
            }
            if (p != null) scannerPanel = p;
        }

        if (scannerPanel != null)
        {
            if (cameraFeedImage == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "CameraFeed");
                if (t != null) cameraFeedImage = t.GetComponent<RawImage>();
            }

            if (reticleContainer == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "ReticleOverlay");
                if (t != null) reticleContainer = t.GetComponent<RectTransform>();
            }

            if (faceTitleText == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "FaceTitleText");
                if (t != null) faceTitleText = t.GetComponent<TextMeshProUGUI>();
            }

            if (faceGuideText == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "FaceGuideText");
                if (t != null) faceGuideText = t.GetComponent<TextMeshProUGUI>();
            }

            if (statusText == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "StatusText");
                if (t != null) statusText = t.GetComponent<TextMeshProUGUI>();
            }

            if (cameraInfoText == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "CameraInfoText");
                if (t != null) cameraInfoText = t.GetComponent<TextMeshProUGUI>();
            }

            if (previewGridContainer == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "PreviewGridContainer");
                if (t != null) previewGridContainer = t;
            }

            if (faceBadgesContainer == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "FaceBadgesContainer");
                if (t != null) faceBadgesContainer = t;
            }

            if (btnCaptureFace == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnCaptureFace");
                if (t != null) btnCaptureFace = t.GetComponent<Button>();
            }

            if (btnRescanFace == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnRescanFace");
                if (t != null) btnRescanFace = t.GetComponent<Button>();
            }

            if (btnNextFace == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnNextFace");
                if (t != null) btnNextFace = t.GetComponent<Button>();
            }

            if (btnPrevFace == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnPrevFace");
                if (t != null) btnPrevFace = t.GetComponent<Button>();
            }

            if (btnSwitchCamera == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnSwitchCamera");
                if (t != null) btnSwitchCamera = t.GetComponent<Button>();
            }

            if (btnApplyToCube == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnApplyToCube");
                if (t != null) btnApplyToCube = t.GetComponent<Button>();
            }

            if (btnClose == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnClose");
                if (t != null) btnClose = t.GetComponent<Button>();
            }

            if (toggleAutoCalibrate == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "ToggleAutoCalibrate");
                if (t != null) toggleAutoCalibrate = t.GetComponent<Toggle>();
            }

            if (toggleCustomMaterialSync == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "ToggleCustomMaterialSync");
                if (t != null) toggleCustomMaterialSync = t.GetComponent<Toggle>();
            }

            if (sliderDistance == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "SliderDistance");
                if (t != null) sliderDistance = t.GetComponent<Slider>();
            }

            if (distanceLabelText == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "DistanceLabelText");
                if (t != null) distanceLabelText = t.GetComponent<TextMeshProUGUI>();
            }

            if (btnNudgeUp == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnNudgeUp");
                if (t != null) btnNudgeUp = t.GetComponent<Button>();
            }

            if (btnNudgeDown == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnNudgeDown");
                if (t != null) btnNudgeDown = t.GetComponent<Button>();
            }

            if (btnNudgeLeft == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnNudgeLeft");
                if (t != null) btnNudgeLeft = t.GetComponent<Button>();
            }

            if (btnNudgeRight == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnNudgeRight");
                if (t != null) btnNudgeRight = t.GetComponent<Button>();
            }

            if (btnResetReticlePos == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnResetReticlePos");
                if (t != null) btnResetReticlePos = t.GetComponent<Button>();
            }

            if (btnFlipHorizontal == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnFlipHorizontal");
                if (t != null) btnFlipHorizontal = t.GetComponent<Button>();
            }

            if (btnFlipVertical == null)
            {
                Transform t = FindDeepChild(scannerPanel.transform, "BtnFlipVertical");
                if (t != null) btnFlipVertical = t.GetComponent<Button>();
            }
        }
    }

    private void SetStatus(string msg, Color col)
    {
        if (statusText != null)
        {
            statusText.text = msg;

            // Chuẩn hóa màu chữ có độ tương phản cao trên nền sáng chuẩn Friendly Puzzle
            if (col == Color.green)
                statusText.color = RubikUITheme.Success; // Xanh lá đậm #16A34A
            else if (col == Color.red)
                statusText.color = RubikUITheme.Danger; // Đỏ đậm #DC2626
            else if (col == Color.yellow)
                statusText.color = RubikUITheme.Warning; // Cam hổ phách #D97706
            else if (col == Color.cyan || col == Color.white)
                statusText.color = RubikUITheme.Sky600; // Xanh dương #0284C7
            else if (col == Color.gray)
                statusText.color = RubikUITheme.Slate500; // Xám Slate #64748B
            else
                statusText.color = col;
        }
    }
}
