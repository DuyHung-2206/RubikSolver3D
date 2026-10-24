using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hiển thị mũi tên chỉ dẫn xoay 3D (3D Curved Guide Arrow) trực tiếp trong không gian World Space
/// kết hợp cả 2 phương án:
///  - Phương án A: Tự sinh 3D Procedural Mesh (Thân cung tròn Ribbon + Đầu mũi tên tam giác dày dặn).
///  - Phương án B: LineRenderer phát sáng Neon (Glow Spine) với hiệu ứng vệt năng lượng chuyển động.
/// Tự động thích ứng với toàn bộ các nước đi chuẩn WCA (U, D, R, L, F, B, M, E, S, dấu nháy ' và xoay 180° '2').
/// </summary>
public class Rubik3DArrowIndicator : MonoBehaviour
{
    [Header("Cài đặt hình học 3D")]
    [Tooltip("Bán kính cung tròn tính từ tâm mặt xoay (đơn vị Unity)")]
    public float arcRadius = 1.15f;

    [Tooltip("Độ rộng của dải thân mũi tên Ribbon")]
    public float ribbonWidth = 0.22f;

    [Tooltip("Độ dày 3D nhô ra khỏi bề mặt")]
    public float extrusionThickness = 0.04f;

    [Tooltip("Khoảng cách nâng mũi tên lên trên mặt Rubik để tránh Z-fighting")]
    public float surfaceOffset = 0.07f;

    [Header("Màu sắc & Phát sáng")]
    public Color neonColor = new Color(0f, 0.92f, 1f, 0.95f); // Neon Cyan hiện đại
    public Color coreColor = new Color(0.9f, 0.98f, 1f, 1f);  // Lõi phát sáng trắng xanh
    public Color glowColor = new Color(0f, 0.7f, 1f, 0.8f);

    [Header("Hiệu ứng chuyển động (Animation)")]
    [Tooltip("Tốc độ nhịp thở của mũi tên")]
    public float pulseSpeed = 4.2f;

    [Tooltip("Biên độ co giãn nhịp thở")]
    public float pulseAmplitude = 0.05f;

    [Tooltip("Tốc độ vệt sáng năng lượng chạy dọc LineRenderer")]
    public float flowSpeed = 2.5f;

    // Các thành phần Procedural nội bộ
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh proceduralMesh;
    private Material ribbonMaterial;

    private GameObject lineChildObj;
    private LineRenderer spineLine;
    private Material lineMaterial;

    // Trạng thái hiện tại
    private string activeMove = string.Empty;
    private bool isVisible = false;
    private float appearTimer = 0f;
    private const float APPEAR_DURATION = 0.18f;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // 1. Khởi tạo MeshFilter & MeshRenderer cho Thân Ribbon 3D (Phương án A)
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (ribbonMaterial == null)
            ribbonMaterial = CreateNeonMaterial(neonColor, 1.4f);

        meshRenderer.material = ribbonMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // 2. Khởi tạo LineRenderer làm cột sống phát sáng Neon (Phương án B)
        Transform existingChild = transform.Find("SpineGlowLine");
        if (existingChild != null)
        {
            lineChildObj = existingChild.gameObject;
            spineLine = lineChildObj.GetComponent<LineRenderer>();
        }
        else
        {
            lineChildObj = new GameObject("SpineGlowLine");
            lineChildObj.transform.SetParent(transform, false);
            spineLine = lineChildObj.AddComponent<LineRenderer>();
        }

        if (lineMaterial == null)
            lineMaterial = CreateNeonMaterial(coreColor, 2.2f);

        spineLine.material = lineMaterial;
        spineLine.useWorldSpace = false;
        spineLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spineLine.receiveShadows = false;
        spineLine.alignment = LineAlignment.View; // Luôn hướng trực diện về Camera để phát sáng rực rỡ nhất
        spineLine.generateLightingData = false;

        // Đường cong độ rộng: thon nhọn ở đuôi, phình rộng ở đầu mũi tên
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.035f);
        widthCurve.AddKey(0.7f, 0.12f);
        widthCurve.AddKey(1f, 0.15f);
        spineLine.widthCurve = widthCurve;

        // Gradient màu: mờ dần ở đuôi -> phát sáng rực rỡ ở đầu
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(neonColor * 0.7f, 0.0f),
                new GradientColorKey(coreColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.2f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        spineLine.colorGradient = grad;

        gameObject.SetActive(false);
    }

    private Material CreateNeonMaterial(Color color, float intensity)
    {
        // Thử tìm shader Unlit URP hoặc Fallback sang Unlit/Color / Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Procedural3DArrowMaterial";

        Color emissiveColor = color * intensity;
        mat.color = emissiveColor;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", emissiveColor);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", emissiveColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissiveColor);
        }

        // Tắt Backface Culling để nhìn thấy cả 2 mặt ở mọi góc quay Camera
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f); // 0 = Off

        return mat;
    }

    private void Update()
    {
        // Phím tắt kiểm tra nhanh khi đang Play: Nhấn phím 'T' để bật/chuyển đổi mũi tên 3D thử nghiệm
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame &&
            !RubikRotator.IsUserTypingInInputField())
        {
            TestNextMove();
        }

        if (!isVisible || string.IsNullOrEmpty(activeMove))
            return;

        // Hoạt ảnh Pop-in khi xuất hiện
        if (appearTimer < APPEAR_DURATION)
        {
            appearTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(appearTimer / APPEAR_DURATION);
            // Elastic out nhẹ
            float popScale = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            transform.localScale = baseScale * (t * popScale);
        }
        else
        {
            // Nhịp thở liên tục (Pulse)
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            transform.localScale = baseScale * pulse;
        }

        // Chạy vệt sáng năng lượng dọc LineRenderer (UV offset scrolling)
        if (lineMaterial != null)
        {
            float offset = -(Time.unscaledTime * flowSpeed) % 1f;
            lineMaterial.mainTextureOffset = new Vector2(offset, 0f);
        }
    }

    /// <summary>
    /// Hiển thị mũi tên chỉ dẫn 3D cho nước đi <paramref name="move"/>.
    /// </summary>
    public void ShowMoveArrow(string move, RubikGenerator generator)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            Hide();
            return;
        }

        activeMove = move.Trim().ToUpperInvariant();
        char face = activeMove[0];
        bool isPrime = activeMove.Contains("'");
        bool isDouble = activeMove.Contains("2");

        if (generator == null)
            generator = FindAnyObjectByType<RubikGenerator>();

        float spacing = (generator != null) ? generator.spacing : 1.05f;
        Vector3 cubeCenter = (generator != null) ? generator.transform.position : Vector3.zero;

        // 1. Xác định Mặt, Pháp tuyến và Trục cơ sở (u, v) trong không gian phẳng của mặt Rubik
        GetFaceCoordinateFrame(face, out Vector3 faceNormal, out Vector3 uDir, out Vector3 vDir, out float layerCoord);

        // Vị trí tâm của mặt xoay, nâng lên một khoảng surfaceOffset
        Vector3 faceCenter = cubeCenter + faceNormal * (spacing * 1.5f + surfaceOffset);
        if (face == 'M' || face == 'E' || face == 'S')
        {
            // Tầng giữa: đặt mũi tên nhô nhẹ qua cạnh để dễ thấy
            faceCenter = cubeCenter + faceNormal * (spacing * 1.55f + surfaceOffset);
        }

        transform.position = faceCenter;
        transform.rotation = Quaternion.LookRotation(faceNormal, uDir);

        // 2. Tạo hình học Procedural Mesh cung tròn 3D + LineRenderer (Phương án A & B)
        BuildArrowGeometry(isPrime, isDouble, spacing);

        appearTimer = 0f;
        transform.localScale = Vector3.zero;
        baseScale = Vector3.one;
        isVisible = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Ẩn mũi tên 3D.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        activeMove = string.Empty;
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Tính toán hệ trục toạ độ cục bộ của từng mặt xoay.
    /// Theo thiết kế:
    ///  - faceNormal: Hướng vuông góc từ tâm mặt chỉ ra ngoài (trục +Z của mũi tên).
    ///  - uDir: Hướng "Trên" của mặt (trục +Y cục bộ của mũi tên).
    ///  - vDir: Hướng "Phải" của mặt (trục +X cục bộ của mũi tên).
    /// Chiều xoay theo chiều kim đồng hồ (Clockwise) luôn là xoay từ uDir (+Y) sang vDir (+X).
    /// </summary>
    private void GetFaceCoordinateFrame(char face, out Vector3 normal, out Vector3 uDir, out Vector3 vDir, out float layerCoord)
    {
        switch (face)
        {
            case 'R':
                normal = Vector3.right;    // +X
                uDir = Vector3.up;         // +Y
                vDir = Vector3.forward;    // +Z
                layerCoord = 1f;
                break;

            case 'L':
                normal = Vector3.left;     // -X
                uDir = Vector3.up;         // +Y
                vDir = Vector3.back;       // -Z
                layerCoord = -1f;
                break;

            case 'M':
                normal = Vector3.left;     // Tầng giữa trục X
                uDir = Vector3.up;
                vDir = Vector3.back;
                layerCoord = 0f;
                break;

            case 'U':
                normal = Vector3.up;       // +Y
                uDir = Vector3.forward;    // +Z
                vDir = Vector3.right;      // +X
                layerCoord = 1f;
                break;

            case 'D':
                normal = Vector3.down;     // -Y
                uDir = Vector3.back;       // -Z
                vDir = Vector3.right;      // +X
                layerCoord = -1f;
                break;

            case 'E':
                normal = Vector3.down;     // Tầng giữa trục Y
                uDir = Vector3.back;
                vDir = Vector3.right;
                layerCoord = 0f;
                break;

            case 'F':
                normal = Vector3.back;     // -Z (Mặt trước trong Unity)
                uDir = Vector3.up;         // +Y
                vDir = Vector3.right;      // +X
                layerCoord = -1f;
                break;

            case 'B':
                normal = Vector3.forward;  // +Z (Mặt sau trong Unity)
                uDir = Vector3.up;         // +Y
                vDir = Vector3.left;       // -X
                layerCoord = 1f;
                break;

            case 'S':
                normal = Vector3.back;     // Tầng giữa trục Z
                uDir = Vector3.up;
                vDir = Vector3.right;
                layerCoord = 0f;
                break;

            default:
                normal = Vector3.back;
                uDir = Vector3.up;
                vDir = Vector3.right;
                layerCoord = 0f;
                break;
        }
    }

    /// <summary>
    /// Sinh Mesh 3D Procedural (Cung tròn + Đầu chóp tam giác) và nạp toạ độ LineRenderer.
    /// Trong hệ toạ độ cục bộ:
    ///  - Mặt phẳng cung tròn nằm trên XY (Z = 0 hoặc nhô nhẹ Z = extrusionThickness).
    ///  - Góc 0 rad tương ứng với trục +Y (uDir).
    ///  - Góc +π/2 rad tương ứng với trục +X (vDir) - Thuận chiều kim đồng hồ.
    /// </summary>
    private void BuildArrowGeometry(bool isPrime, bool isDouble, float spacing)
    {
        float rMid = arcRadius * (spacing / 1.05f);
        float halfW = (ribbonWidth * (spacing / 1.05f)) * 0.5f;
        float rInner = rMid - halfW;
        float rOuter = rMid + halfW;

        // Góc quét: 90° (π/2) cho nước đơn, 180° (π) cho nước đôi '2'
        float sweepAngle = isDouble ? Mathf.PI : (Mathf.PI * 0.5f);
        int segments = isDouble ? 36 : 22;

        // Điểm đầu và điểm cuối cung tròn
        // Đặt cung đối xứng trên góc nhìn hoặc quét đẹp mắt:
        // Đơn: quét từ -25° đến +65° (hoặc 0° đến 90°)
        float startAngle = isDouble ? -Mathf.PI * 0.5f : -Mathf.PI * 0.15f;
        float endAngle = startAngle + sweepAngle;

        // Nếu xoay ngược chiều kim đồng hồ (Prime): đảo chiều quét
        float dirSign = isPrime ? -1f : 1f;
        if (isPrime)
        {
            // Hoán đổi start và end để đầu mũi tên chỉ theo chiều ngược lại
            float temp = startAngle;
            startAngle = endAngle;
            endAngle = temp;
        }

        // --- 1. SINH DỮ LIỆU PROCEDURAL MESH ---
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // Điểm lưu cho LineRenderer (Phương án B)
        List<Vector3> spinePoints = new List<Vector3>();

        // Độ dài góc dành cho đầu mũi tên (Arrowhead)
        float headAngleLength = 0.28f * (halfW * 2f / rMid);
        float bodyEndAngle = endAngle - (dirSign * headAngleLength);

        // Tạo dải Ribbon
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, bodyEndAngle, t);

            // Vector đơn vị trên mặt phẳng XY cục bộ
            // cos(angle)*Y + sin(angle)*X để 0 rad trùng trục Y
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            Vector3 radialDir = new Vector3(sin, cos, 0f);
            Vector3 vIn = radialDir * rInner;
            Vector3 vOut = radialDir * rOuter;

            // Nâng nhẹ trục Z ra phía ngoài bề mặt (hướng về phía Camera)
            vIn.z = extrusionThickness * 0.5f;
            vOut.z = extrusionThickness * 0.5f;

            vertices.Add(vIn);
            vertices.Add(vOut);

            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);

            uvs.Add(new Vector2(0f, t));
            uvs.Add(new Vector2(1f, t));

            // Lưu điểm sống lưng cho LineRenderer
            Vector3 spinePt = radialDir * rMid;
            spinePt.z = extrusionThickness * 0.8f;
            spinePoints.Add(spinePt);

            if (i < segments)
            {
                int baseIdx = i * 2;
                // Tạo tam giác 2 mặt (Double-sided) để đảm bảo luôn nhìn thấy 100% không bị culling
                triangles.Add(baseIdx);
                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);

                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 3);
                triangles.Add(baseIdx + 2);

                triangles.Add(baseIdx);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 1);

                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 3);
            }
        }

        // Tạo Đầu Mũi Tên Tam Giác (Arrowhead) ở cuối
        float headBaseAngle = bodyEndAngle;
        float headTipAngle = endAngle + (dirSign * headAngleLength * 0.35f);

        float cosBase = Mathf.Cos(headBaseAngle);
        float sinBase = Mathf.Sin(headBaseAngle);
        Vector3 dirBase = new Vector3(sinBase, cosBase, 0f);

        float headSpread = halfW * 1.85f;
        Vector3 headLeft = dirBase * (rMid - headSpread);
        Vector3 headRight = dirBase * (rMid + headSpread);
        headLeft.z = extrusionThickness * 0.5f;
        headRight.z = extrusionThickness * 0.5f;

        float cosTip = Mathf.Cos(headTipAngle);
        float sinTip = Mathf.Sin(headTipAngle);
        Vector3 headTip = new Vector3(sinTip, cosTip, 0f) * rMid;
        headTip.z = extrusionThickness * 0.6f;

        int headBaseIdx = vertices.Count;
        vertices.Add(headLeft);
        vertices.Add(headRight);
        vertices.Add(headTip);

        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);

        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0.5f, 1.2f));

        // Hai mặt tam giác đầu mũi tên
        triangles.Add(headBaseIdx);
        triangles.Add(headBaseIdx + 1);
        triangles.Add(headBaseIdx + 2);

        triangles.Add(headBaseIdx);
        triangles.Add(headBaseIdx + 2);
        triangles.Add(headBaseIdx + 1);

        // Nối điểm cuối cùng của LineRenderer tới mũi tam giác
        spinePoints.Add(headTip);

        // Nạp vào Mesh
        if (proceduralMesh == null)
        {
            proceduralMesh = new Mesh();
            proceduralMesh.name = "ProceduralArrowArcMesh";
        }
        else
        {
            proceduralMesh.Clear();
        }

        proceduralMesh.SetVertices(vertices);
        proceduralMesh.SetNormals(normals);
        proceduralMesh.SetUVs(0, uvs);
        proceduralMesh.SetTriangles(triangles, 0);
        proceduralMesh.RecalculateBounds();

        meshFilter.sharedMesh = proceduralMesh;

        // --- 2. NẠP DỮ LIỆU CHO LINERENDERER (Phương án B) ---
        if (spineLine != null)
        {
            spineLine.positionCount = spinePoints.Count;
            spineLine.SetPositions(spinePoints.ToArray());
        }
    }

    #region CÁC PHƯƠNG THỨC KIỂM THỬ NHANH (CONTEXT MENU & HOTKEY)

    private int testMoveIndex = 0;
    private static readonly string[] TEST_MOVES = new string[] { "R", "U'", "F2", "L", "D'", "B2", "R'", "U" };

    [ContextMenu("▶ TEST: Xoay R (Mặt phải theo chiều kim đồng hồ)")]
    public void TestMoveR() => ShowMoveArrow("R", null);

    [ContextMenu("▶ TEST: Xoay U' (Mặt trên ngược chiều kim đồng hồ)")]
    public void TestMoveUPrime() => ShowMoveArrow("U'", null);

    [ContextMenu("▶ TEST: Xoay F2 (Mặt trước xoay 180 độ)")]
    public void TestMoveF2() => ShowMoveArrow("F2", null);

    [ContextMenu("▶ TEST: Chuyển nước tiếp theo (Cycle Moves)")]
    public void TestNextMove()
    {
        testMoveIndex = (testMoveIndex + 1) % TEST_MOVES.Length;
        string move = TEST_MOVES[testMoveIndex];
        Debug.Log($"[Rubik3DArrowIndicator] Thử nghiệm hiển thị mũi tên 3D cho nước: {move}");
        ShowMoveArrow(move, null);
    }

    [ContextMenu("▶ ẨN MŨI TÊN (Hide)")]
    public void TestHide() => Hide();

    #endregion

    private void OnDestroy()
    {
        if (proceduralMesh != null)
            Destroy(proceduralMesh);

        if (ribbonMaterial != null)
            Destroy(ribbonMaterial);

        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
