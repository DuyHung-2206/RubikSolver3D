using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RubikCameraOrbit : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    public Transform target;

    [Header("Tốc độ & Độ nhạy")]
    public float xSpeed = 15.0f;
    public float ySpeed = 15.0f;
    public float zoomSpeed = 0.65f;

    [Header("Giới hạn góc xoay")]
    public float yMinLimit = -80f;
    public float yMaxLimit = 80f;

    [Header("Giới hạn Zoom")]
    public float minDistance = 4.5f;
    public float maxDistance = 14.0f;

    [Header("Căn chỉnh khung nhìn 3D")]
    [Tooltip("Độ lệch tâm mục tiêu để khối Rubik luôn nằm chính giữa vùng trống giữa các Panel UI")]
    public Vector3 targetWorldOffset = new Vector3(0.15f, -0.15f, 0f);

    private float distance = 7.2f;
    // Góc mặc định (-35, 22) nhìn trực diện vào 3 mặt chuẩn: Trên (U), Trước-Trái (F), Trước-Phải (R)
    private const float DefaultX = -35.0f;
    private const float DefaultY = 22.0f;
    private const float DefaultDistance = 7.2f;

    private float x = DefaultX;
    private float y = DefaultY;
    private Coroutine orbitTourCoroutine;

    // Theo dõi tọa độ chuột trực tiếp mỗi frame để đảm bảo xoay mượt 100% trên mọi chuột & touchpad Windows 11
    private bool isRightOrMiddleOrbiting = false;
    private bool isLeftEmptySpaceOrbiting = false;
    private Vector2 lastPointerPos;
    private RubikUIController cachedUIController;

    private void Awake()
    {
        InitializeOrbitState();
    }

    private void OnEnable()
    {
        InitializeOrbitState();
    }

    private void Start()
    {
        InitializeOrbitState();
    }

    private void InitializeOrbitState()
    {
        EnsureTarget();

        if (minDistance < 4.2f) minDistance = 4.5f;
        if (maxDistance < 10.0f) maxDistance = 14.0f;
        if (xSpeed <= 1f) xSpeed = 15.0f;
        if (ySpeed <= 1f) ySpeed = 15.0f;

        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        ApplyFriendlyLightBackground();
        UpdateCameraTransform();
    }

    private void EnsureTarget()
    {
        if (target != null) return;

        GameObject obj = GameObject.Find("RubikManager");
        if (obj != null)
        {
            target = obj.transform;
            return;
        }

        RubikGenerator gen = FindAnyObjectByType<RubikGenerator>();
        if (gen != null)
        {
            target = gen.transform;
        }
    }

    public void ApplyFriendlyLightBackground()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xEE, 0xF3, 0xF9, 255); // Xanh xám rất nhạt chuẩn ảnh tham chiếu #EEF3F9
        }
    }

    private bool IsAnyBlockingModalOpen()
    {
        if (cachedUIController == null)
            cachedUIController = FindAnyObjectByType<RubikUIController>();

        if (cachedUIController != null)
        {
            if (cachedUIController.cameraScanner != null && cachedUIController.cameraScanner.scannerPanel != null && cachedUIController.cameraScanner.scannerPanel.activeInHierarchy)
                return true;
            if (cachedUIController.colorPicker != null && cachedUIController.colorPicker.netPanel != null && cachedUIController.colorPicker.netPanel.activeInHierarchy)
                return true;
            if (cachedUIController.algorithmInput != null && cachedUIController.algorithmInput.inputPanel != null && cachedUIController.algorithmInput.inputPanel.activeInHierarchy)
                return true;
            if (cachedUIController.settingsPanel != null && cachedUIController.settingsPanel.activeInHierarchy)
                return true;
            if (cachedUIController.confirmResetPanel != null && cachedUIController.confirmResetPanel.activeInHierarchy)
                return true;
            if (cachedUIController.victoryPanel != null && cachedUIController.victoryPanel.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void Update()
    {
        EnsureTarget();
        if (target == null) return;

        if (IsAnyBlockingModalOpen())
        {
            isRightOrMiddleOrbiting = false;
            isLeftEmptySpaceOrbiting = false;
            return;
        }

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Mouse.current != null)
        {
            Vector2 currentPos = Mouse.current.position.ReadValue();
            bool rightOrMiddleHeld = Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;

            // 1. GIỮ CHUỘT PHẢI (HOẶC CHUỘT GIỮA): Xoay góc nhìn Rubik 3D ở bất kỳ đâu trên màn hình
            if (rightOrMiddleHeld)
            {
                if (!isRightOrMiddleOrbiting)
                {
                    isRightOrMiddleOrbiting = true;
                    lastPointerPos = currentPos;
                }
                else
                {
                    Vector2 posDelta = currentPos - lastPointerPos;
                    lastPointerPos = currentPos;

                    if (posDelta.sqrMagnitude < 0.0001f)
                        posDelta = Mouse.current.delta.ReadValue();

                    if (posDelta.sqrMagnitude > 0.0001f)
                    {
                        StopOrbitTourIfRunning();
                        x += posDelta.x * xSpeed * 0.022f;
                        y -= posDelta.y * ySpeed * 0.022f;
                        y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
                    }
                }
            }
            else
            {
                isRightOrMiddleOrbiting = false;
            }

            // 2. GIỮ CHUỘT TRÁI TRÊN VÙNG TRỐNG NGOÀI KHỐI RUBIK: Cũng hỗ trợ xoay góc nhìn Camera tiện lợi
            if (!rightOrMiddleHeld)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && !pointerOverUI)
                {
                    Camera cam = GetComponent<Camera>();
                    if (cam == null) cam = Camera.main;
                    bool hitCube = false;
                    if (cam != null)
                    {
                        Ray ray = cam.ScreenPointToRay(currentPos);
                        hitCube = Physics.Raycast(ray, 100f);
                    }

                    if (!hitCube)
                    {
                        isLeftEmptySpaceOrbiting = true;
                        lastPointerPos = currentPos;
                    }
                }

                if (isLeftEmptySpaceOrbiting && Mouse.current.leftButton.isPressed)
                {
                    Vector2 posDelta = currentPos - lastPointerPos;
                    lastPointerPos = currentPos;

                    if (posDelta.sqrMagnitude < 0.0001f)
                        posDelta = Mouse.current.delta.ReadValue();

                    if (posDelta.sqrMagnitude > 0.0001f)
                    {
                        StopOrbitTourIfRunning();
                        x += posDelta.x * xSpeed * 0.022f;
                        y -= posDelta.y * ySpeed * 0.022f;
                        y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
                    }
                }
                else if (!Mouse.current.leftButton.isPressed)
                {
                    isLeftEmptySpaceOrbiting = false;
                }
            }

            // 3. THU PHÓNG BẰNG CON LĂN CHUỘT (Chuẩn hóa bước cuộn để không bị phóng đại đột ngột)
            if (!pointerOverUI)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    StopOrbitTourIfRunning();
                    float step = Mathf.Sign(scroll) * 0.55f;
                    distance = Mathf.Clamp(distance - step, minDistance, maxDistance);
                }
            }
        }

        // 4. Hỗ trợ xoay camera bằng 2 ngón tay trên màn hình cảm ứng điện thoại
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2 && !pointerOverUI)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];
            if (t0.press.isPressed && t1.press.isPressed)
            {
                StopOrbitTourIfRunning();
                Vector2 avgDelta = (t0.delta.ReadValue() + t1.delta.ReadValue()) * 0.5f;
                x += avgDelta.x * xSpeed * 0.018f;
                y -= avgDelta.y * ySpeed * 0.018f;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
            }
        }
    }

    private void LateUpdate()
    {
        EnsureTarget();
        if (target == null) return;
        UpdateCameraTransform();
    }

    private void UpdateCameraTransform()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(y, x, 0f);
        Vector3 focusCenter = target.position + rotation * targetWorldOffset;
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + focusCenter;

        transform.rotation = rotation;
        transform.position = position;
    }

    /// <summary>
    /// Đưa Camera về góc nhìn Isometric mặc định chuẩn.
    /// </summary>
    public void ResetCameraView()
    {
        StopOrbitTourIfRunning();
        orbitTourCoroutine = StartCoroutine(SmoothTransitionTo(DefaultX, DefaultY, DefaultDistance, 0.35f));
    }

    /// <summary>
    /// Phóng to (tiến gần khối Rubik).
    /// </summary>
    public void ZoomIn()
    {
        StopOrbitTourIfRunning();
        distance = Mathf.Clamp(distance - 0.8f, minDistance, maxDistance);
        UpdateCameraTransform();
    }

    /// <summary>
    /// Thu nhỏ (lùi xa khối Rubik).
    /// </summary>
    public void ZoomOut()
    {
        StopOrbitTourIfRunning();
        distance = Mathf.Clamp(distance + 0.8f, minDistance, maxDistance);
        UpdateCameraTransform();
    }

    /// <summary>
    /// Chế độ "Khám phá Rubik 3D": Xoay vòng 360 độ mượt mà quanh khối Rubik để quan sát mọi góc độ.
    /// </summary>
    public void StartExploreOrbitTour()
    {
        StopOrbitTourIfRunning();
        orbitTourCoroutine = StartCoroutine(ExploreOrbitTourCoroutine());
    }

    private void StopOrbitTourIfRunning()
    {
        if (orbitTourCoroutine != null)
        {
            StopCoroutine(orbitTourCoroutine);
            orbitTourCoroutine = null;
        }
    }

    private IEnumerator SmoothTransitionTo(float targetX, float targetY, float targetDist, float duration)
    {
        float startX = x;
        float startY = y;
        float startDist = distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            x = Mathf.LerpAngle(startX, targetX, t);
            y = Mathf.Lerp(startY, targetY, t);
            distance = Mathf.Lerp(startDist, targetDist, t);
            UpdateCameraTransform();
            yield return null;
        }

        x = targetX;
        y = targetY;
        distance = targetDist;
        UpdateCameraTransform();
        orbitTourCoroutine = null;
    }

    private IEnumerator ExploreOrbitTourCoroutine()
    {
        float startX = x;
        float startY = y;
        float startDist = distance;
        float duration = 2.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            x = startX + 360f * t;
            y = Mathf.Lerp(startY, DefaultY, Mathf.Sin(t * Mathf.PI));
            distance = Mathf.Lerp(startDist, DefaultDistance, t);
            UpdateCameraTransform();
            yield return null;
        }

        x = DefaultX;
        y = DefaultY;
        distance = DefaultDistance;
        UpdateCameraTransform();
        orbitTourCoroutine = null;
    }
}
