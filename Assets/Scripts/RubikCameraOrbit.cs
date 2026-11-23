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
    private bool isTouchEmptySpaceOrbiting = false;
    private Vector2 lastTouchPos;
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
            cam.backgroundColor = RubikUITheme.AppBackground; // Xanh xám rất nhạt chuẩn ảnh tham chiếu #EEF3F9
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
            isTouchEmptySpaceOrbiting = false;
            return;
        }

        bool pointerOverUI = IsPointerOverUI();

        // 1. XỬ LÝ MÀN HÌNH CẢM ỨNG (MOBILE TOUCHSCREEN)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            int activeTouchCount = 0;
            for (int i = 0; i < Touchscreen.current.touches.Count; i++)
            {
                if (Touchscreen.current.touches[i].press.isPressed)
                    activeTouchCount++;
            }

            if (activeTouchCount >= 2)
            {
                // 2 NGÓN TAY: Thu phóng Pinch-to-zoom và Xoay góc nhìn Camera
                isTouchEmptySpaceOrbiting = false;
                var t0 = Touchscreen.current.touches[0];
                var t1 = Touchscreen.current.touches[1];

                if (!pointerOverUI && t0.press.isPressed && t1.press.isPressed)
                {
                    StopOrbitTourIfRunning();
                    Vector2 p0 = t0.position.ReadValue();
                    Vector2 p1 = t1.position.ReadValue();
                    Vector2 d0 = t0.delta.ReadValue();
                    Vector2 d1 = t1.delta.ReadValue();

                    float currDist = Vector2.Distance(p0, p1);
                    float prevDist = Vector2.Distance(p0 - d0, p1 - d1);

                    if (Mathf.Abs(currDist - prevDist) > 0.5f)
                    {
                        float pinchDelta = (currDist - prevDist) * 0.012f;
                        distance = Mathf.Clamp(distance - pinchDelta, minDistance, maxDistance);
                    }

                    Vector2 avgDelta = (d0 + d1) * 0.5f;
                    if (avgDelta.sqrMagnitude > 0.001f)
                    {
                        x += avgDelta.x * xSpeed * 0.018f;
                        y -= avgDelta.y * ySpeed * 0.018f;
                        y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
                    }
                }
            }
            else if (activeTouchCount == 1)
            {
                // 1 NGÓN TAY: Xoay góc nhìn Camera khi chạm và kéo trên vùng trống ngoài khối Rubik
                var t = Touchscreen.current.primaryTouch;
                Vector2 touchPos = t.position.ReadValue();

                if (t.press.wasPressedThisFrame && !pointerOverUI)
                {
                    Camera cam = GetComponent<Camera>();
                    if (cam == null) cam = Camera.main;
                    bool hitCube = false;
                    if (cam != null)
                    {
                        Ray ray = cam.ScreenPointToRay(touchPos);
                        hitCube = Physics.Raycast(ray, 100f);
                    }

                    if (!hitCube)
                    {
                        isTouchEmptySpaceOrbiting = true;
                        lastTouchPos = touchPos;
                    }
                }

                if (isTouchEmptySpaceOrbiting && t.press.isPressed)
                {
                    Vector2 posDelta = touchPos - lastTouchPos;
                    lastTouchPos = touchPos;

                    if (posDelta.sqrMagnitude < 0.0001f)
                        posDelta = t.delta.ReadValue();

                    if (posDelta.sqrMagnitude > 0.0001f)
                    {
                        StopOrbitTourIfRunning();
                        x += posDelta.x * xSpeed * 0.022f;
                        y -= posDelta.y * ySpeed * 0.022f;
                        y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
                    }
                }
                else if (!t.press.isPressed)
                {
                    isTouchEmptySpaceOrbiting = false;
                }
            }
            else
            {
                isTouchEmptySpaceOrbiting = false;
            }
        }

        // 2. XỬ LÝ CHUỘT / DESKTOP (WINDOWS PC)
        if (Mouse.current != null)
        {
            Vector2 currentPos = Mouse.current.position.ReadValue();
            bool rightOrMiddleHeld = Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;

            // A. GIỮ CHUỘT PHẢI (HOẶC CHUỘT GIỮA): Xoay góc nhìn Rubik 3D ở bất kỳ đâu trên màn hình
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

            // B. GIỮ CHUỘT TRÁI TRÊN VÙNG TRỐNG NGOÀI KHỐI RUBIK: Xoay góc nhìn Camera tiện lợi
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

            // C. THU PHÓNG BẰNG CON LĂN CHUỘT
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
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        if (Touchscreen.current != null)
        {
            for (int i = 0; i < Touchscreen.current.touches.Count; i++)
            {
                var touch = Touchscreen.current.touches[i];
                if (touch.press.isPressed)
                {
                    int touchId = touch.touchId.ReadValue();
                    if (EventSystem.current.IsPointerOverGameObject(touchId))
                        return true;
                }
            }
        }
        return false;
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
