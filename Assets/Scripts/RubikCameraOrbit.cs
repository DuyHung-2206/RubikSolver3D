using UnityEngine;
using UnityEngine.InputSystem; // Thư viện Input mới của Unity 6

public class RubikCameraOrbit : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    public Transform target;

    [Header("Tốc độ & Độ nhạy")]
    public float xSpeed = 15.0f;
    public float ySpeed = 15.0f;
    public float zoomSpeed = 0.01f;

    [Header("Giới hạn góc xoay")]
    public float yMinLimit = -80f;
    public float yMaxLimit = 80f;

    [Header("Giới hạn Zoom")]
    public float minDistance = 4.0f;
    public float maxDistance = 15.0f;

    private float distance = 7.0f;
    // Góc mặc định (-35, 20) nhìn trực diện vào 3 mặt chuẩn: Trên (Trắng - U), Trước-Trái (Xanh lá - F), Trước-Phải (Đỏ - R)
    private float x = -35.0f;
    private float y = 20.0f;

    void Start()
    {
        if (target == null)
        {
            GameObject obj = GameObject.Find("RubikManager");
            if (obj != null) target = obj.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null || Mouse.current == null) return;

        // CHỈ XOAY CAMERA KHI GIỮ CHUỘT PHẢI (Để dành chuột trái cho việc kéo Rubik)
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            x += delta.x * xSpeed * 0.02f;
            y -= delta.y * ySpeed * 0.02f;
            y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
        }

        // Thu phóng bằng con lăn chuột
        float scroll = Mouse.current.scroll.ReadValue().y;
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        // Cập nhật vị trí và góc nhìn camera
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}
