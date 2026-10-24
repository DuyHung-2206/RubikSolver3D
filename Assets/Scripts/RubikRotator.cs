using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RubikRotator : MonoBehaviour
{
    [Header("Cài đặt hiệu ứng xoay chân thực")]
    [Range(0.05f, 1.5f)]
    [Tooltip("Thời gian hoàn thành 1 lần xoay (giây). 0.18s - 0.25s mang lại cảm giác xoay dứt khoát như ngoài thực tế.")]
    public float rotationDuration = 0.22f;

    [Tooltip("Đường cong chuyển động xoay (Ease In-Out cho chuyển động cơ học mượt mà)")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RubikGenerator generator;
    private RubikStateManager stateManager;
    private bool isRotating;
    public bool IsRotating => isRotating;
    private readonly Queue<string> moveQueue = new Queue<string>();

    /// <summary>
    /// Cho biết khối Rubik có đang thực hiện xoay hoặc còn lệnh trong hàng đợi hay không.
    /// </summary>
    public bool IsBusy => isRotating || (moveQueue != null && moveQueue.Count > 0);

    private void Awake()
    {
        generator = GetComponent<RubikGenerator>();
        stateManager = GetComponent<RubikStateManager>();
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();
    }

    private void Update()
    {
        HandleKeyboardInput();

        if (!isRotating && moveQueue.Count > 0)
        {
            string move = moveQueue.Dequeue();
            StartCoroutine(ExecuteMove(move));
        }
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        // Không nhận phím xoay nếu người dùng đang gõ chữ trong ô nhập công thức (TMP_InputField / InputField)
        if (IsUserTypingInInputField())
            return;

        bool prime =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        string suffix = prime ? "'" : "";

        // Ánh xạ phím bấm theo góc nhìn Camera trước khi cho vào hàng đợi xoay
        if (Keyboard.current.fKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('F') + suffix);
        if (Keyboard.current.bKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('B') + suffix);
        if (Keyboard.current.rKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('R') + suffix);
        if (Keyboard.current.lKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('L') + suffix);
        if (Keyboard.current.uKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('U') + suffix);
        if (Keyboard.current.dKey.wasPressedThisFrame) EnqueueMove(MapInputToPhysicalFace('D') + suffix);

        // Các tầng giữa (Slice moves): M, E, S (nhấn Shift để xoay M', E', S')
        if (Keyboard.current.mKey.wasPressedThisFrame) EnqueueMove("M" + suffix);
        if (Keyboard.current.eKey.wasPressedThisFrame) EnqueueMove("E" + suffix);
        if (Keyboard.current.sKey.wasPressedThisFrame) EnqueueMove("S" + suffix);
    }

    /// <summary>
    /// Kiểm tra xem người dùng có đang đặt con trỏ gõ văn bản trong InputField hay không.
    /// </summary>
    public static bool IsUserTypingInInputField()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        GameObject selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;

        var tmpInput = selected.GetComponent<TMPro.TMP_InputField>();
        if (tmpInput != null && tmpInput.isFocused)
            return true;

        var legacyInput = selected.GetComponent<UnityEngine.UI.InputField>();
        if (legacyInput != null && legacyInput.isFocused)
            return true;

        return false;
    }

    /// <summary>
    /// Được gọi từ 6 nút bấm giao diện (U, D, F, B, R, L) trên Panel Điều khiển xoay bên phải.
    /// Tự động kiểm tra phím Shift (hoặc chế độ xoay ngược trên UI) và ánh xạ theo góc nhìn hiện tại.
    /// </summary>
    public void RotateFaceFromUI(char inputFace, bool uiPrimeToggle = false)
    {
        bool shiftHeld = Keyboard.current != null &&
                         (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        bool prime = shiftHeld || uiPrimeToggle;
        string suffix = prime ? "'" : "";
        char physicalFace = MapInputToPhysicalFace(char.ToUpperInvariant(inputFace));
        EnqueueMove(physicalFace + suffix);
    }

    public void EnqueueMove(string move)
    {
        if (string.IsNullOrEmpty(move))
            return;

        move = move.Trim().ToUpperInvariant();

        char face = move[0];
        if ("UDRLFB MES".IndexOf(face) < 0)
            return;

        if (move.Length > 1 && move[1] != '\'' && move[1] != '2')
            return;

        moveQueue.Enqueue(move);
    }

    private IEnumerator ExecuteMove(string move)
    {
        isRotating = true;

        if (generator == null)
            generator = GetComponent<RubikGenerator>();

        if (generator == null)
        {
            Debug.LogError("Không tìm thấy RubikGenerator trên RubikManager.");
            isRotating = false;
            yield break;
        }

        if (generator.allCubies == null || generator.allCubies.Count != 27)
        {
            Debug.LogError("Rubik cần có đủ 27 khối nhỏ trước khi xoay.");
            isRotating = false;
            yield break;
        }

        char face = move[0];
        bool prime = move.Length > 1 && move[1] == '\'';
        bool isDouble = move.Length > 1 && move[1] == '2';

        Vector3 axis;
        float layerCoordinate;
        float angle;

        // Chiều xoay theo ký hiệu Rubik chuẩn (Clockwise theo góc nhìn trực diện từng mặt)
        // Hệ tọa độ Left-Handed của Unity (U=+Y, D=-Y, F=-Z, B=+Z, R=+X, L=-X)
        switch (face)
        {
            case 'R':
                axis = Vector3.right;
                layerCoordinate = 1f;
                angle = 90f;
                break;

            case 'L':
                axis = Vector3.right;
                layerCoordinate = -1f;
                angle = -90f;
                break;

            case 'M': // Nằm giữa R và L (Tọa độ X = 0, quay theo hướng của L)
                axis = Vector3.right;
                layerCoordinate = 0f;
                angle = -90f;
                break;

            case 'U':
                axis = Vector3.up;
                layerCoordinate = 1f;
                angle = 90f;
                break;

            case 'D':
                axis = Vector3.up;
                layerCoordinate = -1f;
                angle = -90f;
                break;

            case 'E': // Nằm giữa U và D (Tọa độ Y = 0, quay theo hướng của D)
                axis = Vector3.up;
                layerCoordinate = 0f;
                angle = -90f;
                break;

            case 'F':
                axis = Vector3.forward;
                layerCoordinate = -1f;
                angle = -90f;
                break;

            case 'B':
                axis = Vector3.forward;
                layerCoordinate = 1f;
                angle = 90f;
                break;

            case 'S': // Nằm giữa F và B (Tọa độ Z = 0, quay theo hướng của F)
                axis = Vector3.forward;
                layerCoordinate = 0f;
                angle = -90f;
                break;

            default:
                isRotating = false;
                yield break;
        }

        if (prime)
            angle = -angle;

        if (isDouble)
            angle *= 2f;

        float spacing = generator.spacing;
        float targetCoordinate = layerCoordinate * spacing;

        List<Transform> selected = new List<Transform>();

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null)
                continue;

            Vector3 p = cubie.localPosition;
            float coordinate;

            if (face == 'R' || face == 'L' || face == 'M')
                coordinate = p.x;
            else if (face == 'U' || face == 'D' || face == 'E')
                coordinate = p.y;
            else if (face == 'F' || face == 'B' || face == 'S')
                coordinate = p.z;
            else
                continue;

            if (Mathf.Abs(coordinate - targetCoordinate) < spacing * 0.25f)
                selected.Add(cubie);
        }

        if (selected.Count != 9)
        {
            Debug.LogError(
                $"Lỗi chọn lớp {face}: tìm thấy {selected.Count} khối, cần 9."
            );
            isRotating = false;
            yield break;
        }

        // Pivot đặt tại tâm Rubik
        GameObject pivotObject = new GameObject("TemporaryRotationPivot");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(generator.transform, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;

        // Giữ nguyên world transform khi đưa các khối vào pivot
        foreach (Transform cubie in selected)
            cubie.SetParent(pivot, true);

        Quaternion startRotation = pivot.localRotation;
        Quaternion endRotation =
            Quaternion.AngleAxis(angle, axis) * startRotation;

        float duration = Mathf.Max(0.05f, rotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Sử dụng AnimationCurve hoặc Cubic Ease In-Out cho chuyển động cơ học mượt và đầm tay
            float curveT = (rotationCurve != null && rotationCurve.length > 0)
                ? rotationCurve.Evaluate(t)
                : (t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f);

            pivot.localRotation =
                Quaternion.Slerp(startRotation, endRotation, curveT);

            yield return null;
        }

        pivot.localRotation = endRotation;

        // Đưa khối về RubikManager và căn lại vị trí, hướng xoay
        foreach (Transform cubie in selected)
        {
            cubie.SetParent(generator.transform, true);

            Vector3 p = cubie.localPosition;
            p.x = Mathf.Round(p.x / spacing) * spacing;
            p.y = Mathf.Round(p.y / spacing) * spacing;
            p.z = Mathf.Round(p.z / spacing) * spacing;
            cubie.localPosition = p;

            cubie.localRotation = SnapRotation(cubie.localRotation);
        }

        Destroy(pivotObject);

        // CẬP NHẬT TRẠNG THÁI LOGIC NGAY SAU KHI HOẠT ẢNH 3D KẾT THÚC
        if (stateManager != null)
        {
            stateManager.OnMoveCompleted(move);
        }

        isRotating = false;
    }

    private Quaternion SnapRotation(Quaternion rotation)
    {
        Vector3 forward = RoundDirection(rotation * Vector3.forward);
        Vector3 up = RoundDirection(rotation * Vector3.up);

        if (forward.sqrMagnitude < 0.5f || up.sqrMagnitude < 0.5f)
            return rotation;

        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.1f)
            return rotation;

        return Quaternion.LookRotation(forward, up);
    }

    private Vector3 RoundDirection(Vector3 direction)
    {
        return new Vector3(
            Mathf.Round(direction.x),
            Mathf.Round(direction.y),
            Mathf.Round(direction.z)
        );
    }

    /// <summary>
    /// Chuyển đổi ký hiệu phím (U, D, F, B, R, L theo góc nhìn màn hình)
    /// sang mặt vật lý thực tế của khối Rubik.
    /// </summary>
    private char MapInputToPhysicalFace(char inputFace)
    {
        Camera cam = Camera.main;
        if (cam == null) return inputFace;

        // Bỏ qua U và D nếu bạn chỉ muốn xoay 4 hướng ngang
        if (inputFace == 'U' || inputFace == 'D')
            return inputFace;

        // Vector hướng nhìn của Camera chiếu phẳng lên mặt đất (mặt phẳng XZ)
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        // 4 hướng mặt ngang cố định của khối Rubik (U=+Y, D=-Y, F=-Z, B=+Z, R=+X, L=-X)
        var directions = new Dictionary<char, Vector3>()
        {
            { 'F', Vector3.back },    // -Z: Xanh lá (Front)
            { 'B', Vector3.forward }, // +Z: Xanh dương (Back)
            { 'R', Vector3.right },   // +X: Đỏ (Right)
            { 'L', Vector3.left }     // -X: Cam (Left)
        };

        // Tìm mặt nào đang hướng thẳng về phía Camera nhất (chính là F trên màn hình)
        char currentFront = 'F';
        float maxDot = -999f;
        foreach (var pair in directions)
        {
            // Vector từ tâm Rubik hướng ra mặt đối đầu với hướng camera (-camForward)
            float dot = Vector3.Dot(pair.Value, -camForward);
            if (dot > maxDot)
            {
                maxDot = dot;
                currentFront = pair.Key;
            }
        }

        // Tìm mặt bên Phải của góc nhìn camera (R trên màn hình)
        char currentRight = 'R';
        maxDot = -999f;
        foreach (var pair in directions)
        {
            float dot = Vector3.Dot(pair.Value, camRight);
            if (dot > maxDot)
            {
                maxDot = dot;
                currentRight = pair.Key;
            }
        }

        // Suy ra mặt Trái và mặt Sau
        char currentBack = GetOppositeFace(currentFront);
        char currentLeft = GetOppositeFace(currentRight);

        // Ánh xạ phím người chơi bấm sang mặt vật lý thực tế
        switch (inputFace)
        {
            case 'F': return currentFront;
            case 'B': return currentBack;
            case 'R': return currentRight;
            case 'L': return currentLeft;
            default: return inputFace;
        }
    }

    private char GetOppositeFace(char face)
    {
        switch (face)
        {
            case 'F': return 'B';
            case 'B': return 'F';
            case 'R': return 'L';
            case 'L': return 'R';
            case 'U': return 'D';
            case 'D': return 'U';
            default: return face;
        }
    }
    /// <summary>
    /// Xóa sạch các lệnh xoay còn tồn đọng trong hàng đợi khi bấm Reset.
    /// </summary>
    public void ClearQueue()
    {
        moveQueue.Clear();
    }
    /// <summary>
    /// Xoay tức thì một nước đi trong 1 frame (0 giây), không chờ hoạt ảnh.
    /// </summary>
    public void ExecuteMoveInstant(string move)
    {
        if (string.IsNullOrEmpty(move)) return;
        move = move.Trim().ToUpperInvariant();

        char face = move[0];
        if ("UDRLFB MES".IndexOf(face) < 0) return;

        bool prime = move.Length > 1 && move[1] == '\'';
        bool doubleTurn = move.EndsWith("2");

        Vector3 axis;
        float layerCoordinate;
        float angle;

        switch (face)
        {
            case 'R': axis = Vector3.right; layerCoordinate = 1f; angle = 90f; break;
            case 'L': axis = Vector3.right; layerCoordinate = -1f; angle = -90f; break;
            case 'U': axis = Vector3.up; layerCoordinate = 1f; angle = 90f; break;
            case 'D': axis = Vector3.up; layerCoordinate = -1f; angle = -90f; break;
            case 'F': axis = Vector3.forward; layerCoordinate = -1f; angle = -90f; break;
            case 'B': axis = Vector3.forward; layerCoordinate = 1f; angle = 90f; break;
            case 'M': axis = Vector3.right; layerCoordinate = 0f; angle = -90f; break;
            case 'E': axis = Vector3.up; layerCoordinate = 0f; angle = -90f; break;
            case 'S': axis = Vector3.forward; layerCoordinate = 0f; angle = -90f; break;
            default: return;
        }

        if (doubleTurn) angle *= 2f;
        else if (prime) angle = -angle;

        if (generator == null) generator = GetComponent<RubikGenerator>();
        if (generator == null) generator = FindAnyObjectByType<RubikGenerator>();
        if (generator == null || generator.allCubies == null || generator.allCubies.Count != 27) return;

        float spacing = generator.spacing;
        float targetCoordinate = layerCoordinate * spacing;

        List<Transform> selected = new List<Transform>();
        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null) continue;
            Vector3 p = cubie.localPosition;
            float coordinate = (face == 'R' || face == 'L' || face == 'M') ? p.x :
                               (face == 'U' || face == 'D' || face == 'E') ? p.y : p.z;

            if (Mathf.Abs(coordinate - targetCoordinate) < spacing * 0.25f)
                selected.Add(cubie);
        }

        if (selected.Count != 9) return;

        GameObject pivotObject = new GameObject("InstantPivot");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(generator.transform, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;

        foreach (Transform cubie in selected)
            cubie.SetParent(pivot, true);

        pivot.localRotation = Quaternion.AngleAxis(angle, axis);

        foreach (Transform cubie in selected)
        {
            cubie.SetParent(generator.transform, true);
            Vector3 p = cubie.localPosition;
            p.x = Mathf.Round(p.x / spacing) * spacing;
            p.y = Mathf.Round(p.y / spacing) * spacing;
            p.z = Mathf.Round(p.z / spacing) * spacing;
            cubie.localPosition = p;
            cubie.localRotation = SnapRotation(cubie.localRotation);
        }

        Destroy(pivotObject);

        // Đồng bộ logic sang StateManager
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();

        if (stateManager != null)
        {
            stateManager.OnMoveCompleted(move);
        }
    }
}
