using UnityEngine;
using UnityEngine.InputSystem;

public class RubikDragController : MonoBehaviour
{
    [Header("Cài đặt kéo thả")]
    [Tooltip("Khoảng cách tối thiểu chuột phải kéo (pixel) để kích hoạt xoay")]
    public float dragThreshold = 25f;

    private Camera mainCamera;
    private RubikRotator rotator;
    private RubikGenerator generator;

    // Trạng thái khi bắt đầu click
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private Transform clickedCubie;
    private Vector3 hitNormal;

    private void Awake()
    {
        mainCamera = Camera.main;
        rotator = GetComponent<RubikRotator>();
        generator = GetComponent<RubikGenerator>();
    }

    private void Update()
    {
        if (Mouse.current == null || rotator == null) return;
        // KHÓA KÉO THẢ NẾU KHỐI RUBIK ĐANG BẬN XOAY HOẶC ĐANG TRỘN
        if (rotator.IsBusy)
        {
            isDragging = false;
            return;
        }
        // 1. Nhấn chuột trái: Bắn Raycast xác định mặt và viên Rubik
        if (Mouse.current.leftButton.wasPressedThisFrame)
            if (Mouse.current == null || rotator == null) return;

        // Nếu khối đang xoay hoạt ảnh thì không nhận tương tác kéo mới
        if (rotator.IsRotating)
        {
            isDragging = false;
            return;
        }

        // 1. Nhấn chuột trái: Bắn Raycast xác định mặt và viên Rubik
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Chỉ nhận các khối con thuộc RubikManager
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                {
                    clickedCubie = hit.transform;
                    hitNormal = hit.normal;
                    dragStartPos = Mouse.current.position.ReadValue();
                    isDragging = true;
                }
            }
        }

        // 2. Đang giữ chuột: Kiểm tra xem đã kéo đủ khoảng cách threshold chưa
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 dragDelta = currentMousePos - dragStartPos;

            if (dragDelta.magnitude >= dragThreshold)
            {
                ResolveAndExecuteDrag(dragDelta);
                isDragging = false; // Đã kích hoạt 1 lần, kết thúc lượt kéo
            }
        }

        // 3. Nhả chuột: Hủy cờ kéo nếu chưa đủ threshold
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }

    /// <summary>
    /// Tính toán từ vector kéo 2D trên màn hình ra nước đi Rubik 3D tương ứng.
    /// </summary>
    private void ResolveAndExecuteDrag(Vector2 dragDelta)
    {
        if (clickedCubie == null) return;

        // Xác định pháp tuyến chuẩn hóa về hệ trục RubikManager
        Vector3 localNormal = transform.InverseTransformDirection(hitNormal);
        localNormal = SnapVectorToAxis(localNormal);

        // Vị trí của cubie trong không gian local của Rubik (dùng InverseTransformPoint để an toàn tuyệt đối)
        Vector3 cubieLocalPos = transform.InverseTransformPoint(clickedCubie.position) / (generator != null ? generator.spacing : 1.05f);

        // Xác định hai trục tiếp tuyến (Tangent & Bitangent) nằm trên mặt bị click
        Vector3 axis1 = Vector3.zero;
        Vector3 axis2 = Vector3.zero;

        if (Mathf.Abs(localNormal.y) > 0.5f) // Mặt U (+Y) hoặc D (-Y)
        {
            axis1 = Vector3.right;   // Trục X (ngang)
            axis2 = Vector3.forward; // Trục Z (dọc)
        }
        else if (Mathf.Abs(localNormal.z) > 0.5f) // Mặt F (+Z) hoặc B (-Z)
        {
            axis1 = Vector3.right; // Trục X (ngang)
            axis2 = Vector3.up;    // Trục Y (dọc)
        }
        else if (Mathf.Abs(localNormal.x) > 0.5f) // Mặt R (+X) hoặc L (-X)
        {
            axis1 = Vector3.forward; // Trục Z (ngang)
            axis2 = Vector3.up;      // Trục Y (dọc)
        }

        // Chiếu 2 trục 3D này lên không gian màn hình 2D của Camera
        Vector2 screenDir1 = GetScreenDirection(clickedCubie.position, transform.TransformDirection(axis1));
        Vector2 screenDir2 = GetScreenDirection(clickedCubie.position, transform.TransformDirection(axis2));

        // So sánh độ tương đồng góc với vector kéo của chuột bằng tích vô hướng Dot
        float dot1 = Vector2.Dot(dragDelta.normalized, screenDir1);
        float dot2 = Vector2.Dot(dragDelta.normalized, screenDir2);

        Vector3 chosenAxis;
        bool positiveDirection;

        if (Mathf.Abs(dot1) > Mathf.Abs(dot2))
        {
            chosenAxis = axis1;
            positiveDirection = dot1 > 0;
        }
        else
        {
            chosenAxis = axis2;
            positiveDirection = dot2 > 0;
        }

        // Trục xoay thực tế sẽ là tích có hướng của pháp tuyến mặt và hướng kéo
        Vector3 rotationAxis = Vector3.Cross(localNormal, chosenAxis);
        if (!positiveDirection) rotationAxis = -rotationAxis;
        rotationAxis = SnapVectorToAxis(rotationAxis);

        // Chuyển trục xoay và tọa độ của viên rubik thành tên nước đi chuẩn
        string move = ConvertToMoveName(rotationAxis, cubieLocalPos);
        if (!string.IsNullOrEmpty(move))
        {
            Debug.Log($"[Drag Controller] Kéo mặt -> Thực thi nước đi: {move}");
            rotator.EnqueueMove(move);
        }
    }

    private string ConvertToMoveName(Vector3 rotAxis, Vector3 pos)
    {
        // 1. Xoay quanh trục Y (Các tầng ngang: U, E, D)
        if (Mathf.Abs(rotAxis.y) > 0.5f)
        {
            bool isClockwise = rotAxis.y > 0;
            if (pos.y > 0.4f) return isClockwise ? "U" : "U'";
            if (pos.y < -0.4f) return isClockwise ? "D'" : "D";
            // Tầng giữa E (Equator)
            return isClockwise ? "E'" : "E";
        }
        // 2. Xoay quanh trục X (Các tầng dọc theo chiều X: R, M, L)
        else if (Mathf.Abs(rotAxis.x) > 0.5f)
        {
            bool isClockwise = rotAxis.x > 0;
            if (pos.x > 0.4f) return isClockwise ? "R" : "R'";
            if (pos.x < -0.4f) return isClockwise ? "L'" : "L";
            // Tầng giữa M (Middle)
            return isClockwise ? "M'" : "M";
        }
        // 3. Xoay quanh trục Z (Các tầng dọc theo chiều Z: F, S, B)
        else if (Mathf.Abs(rotAxis.z) > 0.5f)
        {
            bool isClockwise = rotAxis.z < 0;
            if (pos.z > 0.4f) return isClockwise ? "F" : "F'";
            if (pos.z < -0.4f) return isClockwise ? "B'" : "B";
            // Tầng giữa S (Standing)
            return isClockwise ? "S" : "S'";
        }

        return string.Empty;
    }

    private Vector2 GetScreenDirection(Vector3 originWorldPos, Vector3 worldDir)
    {
        Vector3 p1 = mainCamera.WorldToScreenPoint(originWorldPos);
        Vector3 p2 = mainCamera.WorldToScreenPoint(originWorldPos + worldDir * 0.5f);
        return ((Vector2)(p2 - p1)).normalized;
    }

    private Vector3 SnapVectorToAxis(Vector3 v)
    {
        float x = Mathf.Abs(v.x);
        float y = Mathf.Abs(v.y);
        float z = Mathf.Abs(v.z);

        if (x > y && x > z) return new Vector3(Mathf.Sign(v.x), 0, 0);
        if (y > x && y > z) return new Vector3(0, Mathf.Sign(v.y), 0);
        return new Vector3(0, 0, Mathf.Sign(v.z));
    }
}
