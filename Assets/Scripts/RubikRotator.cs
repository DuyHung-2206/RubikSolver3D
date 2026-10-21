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
    private bool isRotating;
    private readonly Queue<string> moveQueue = new Queue<string>();

    private void Awake()
    {
        generator = GetComponent<RubikGenerator>();
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

        bool prime =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        string suffix = prime ? "'" : "";

        if (Keyboard.current.uKey.wasPressedThisFrame) EnqueueMove("U" + suffix);
        if (Keyboard.current.dKey.wasPressedThisFrame) EnqueueMove("D" + suffix);
        if (Keyboard.current.rKey.wasPressedThisFrame) EnqueueMove("R" + suffix);
        if (Keyboard.current.lKey.wasPressedThisFrame) EnqueueMove("L" + suffix);
        if (Keyboard.current.fKey.wasPressedThisFrame) EnqueueMove("F" + suffix);
        if (Keyboard.current.bKey.wasPressedThisFrame) EnqueueMove("B" + suffix);
    }

    public void EnqueueMove(string move)
    {
        if (string.IsNullOrEmpty(move))
            return;

        move = move.Trim().ToUpperInvariant();

        char face = move[0];
        if ("UDRLFB".IndexOf(face) < 0)
            return;

        if (move.Length > 1 && move[1] != '\'')
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

        Vector3 axis;
        float layerCoordinate;
        float angle;

        // Chiều xoay theo ký hiệu Rubik chuẩn (Clockwise theo góc nhìn trực diện từng mặt)
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

            case 'F':
                axis = Vector3.forward;
                layerCoordinate = 1f;
                angle = -90f;
                break;

            case 'B':
                axis = Vector3.forward;
                layerCoordinate = -1f;
                angle = 90f;
                break;

            default:
                isRotating = false;
                yield break;
        }

        if (prime)
            angle = -angle;

        float spacing = generator.spacing;
        float targetCoordinate = layerCoordinate * spacing;

        List<Transform> selected = new List<Transform>();

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null)
                continue;

            Vector3 p = cubie.localPosition;
            float coordinate;

            if (face == 'R' || face == 'L')
                coordinate = p.x;
            else if (face == 'U' || face == 'D')
                coordinate = p.y;
            else
                coordinate = p.z;

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
}
