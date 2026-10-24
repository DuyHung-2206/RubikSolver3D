using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiệu ứng làm nổi bật tầng xoay (Layer Highlight) trên khối Rubik 3D hiện có
/// và hiển thị mũi tên cong chỉ hướng xoay (Rotation Arrow) cho giao diện Hướng dẫn giải (Ảnh tham chiếu số 3).
/// Sử dụng MaterialPropertyBlock để không làm thay đổi trạng thái logic hay phá vỡ Material gốc.
/// </summary>
public class RubikGuideHighlighter : MonoBehaviour
{
    [Header("Tham chiếu")]
    public RubikGenerator generator;
    public RectTransform arrowIndicatorUI;
    public Image arrowImage;

    [Header("Mũi tên chỉ dẫn 3D (Phương án A & B Procedural)")]
    public Rubik3DArrowIndicator arrow3D;

    private string activeMove = string.Empty;
    private bool isHighlighting = false;
    private MaterialPropertyBlock propBlock;
    private readonly List<Transform> activeLayerCubies = new List<Transform>();

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (generator == null)
            generator = FindAnyObjectByType<RubikGenerator>();
    }

    private void Update()
    {
        if (!isHighlighting || string.IsNullOrEmpty(activeMove))
        {
            if (arrowIndicatorUI != null && arrowIndicatorUI.gameObject.activeSelf)
                arrowIndicatorUI.gameObject.SetActive(false);
            return;
        }

        UpdateArrowTransformAndPulse();
    }

    /// <summary>
    /// Làm nổi bật lớp 9 viên Cubie sẽ xoay của nước đi <paramref name="move"/> và hiển thị mũi tên hướng xoay.
    /// Không làm thay đổi trạng thái logic RubikState.
    /// </summary>
    public void HighlightMove(string move)
    {
        if (generator == null)
            generator = FindAnyObjectByType<RubikGenerator>();

        if (string.IsNullOrWhiteSpace(move) || generator == null || generator.allCubies == null || generator.allCubies.Count == 0)
        {
            ClearHighlight();
            return;
        }

        activeMove = move.Trim().ToUpperInvariant();
        isHighlighting = true;
        activeLayerCubies.Clear();

        char face = activeMove[0];
        float spacing = generator.spacing;
        float targetCoord = GetLayerCoordinate(face) * spacing;

        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null) continue;

            Vector3 p = cubie.localPosition;
            float coord = GetAxisValue(p, face);
            bool inActiveLayer = Mathf.Abs(coord - targetCoord) < spacing * 0.28f;

            if (inActiveLayer)
                activeLayerCubies.Add(cubie);

            ApplyCubieVisualState(cubie, inActiveLayer, face);
        }

        EnsureArrow3D();
        if (arrow3D != null)
        {
            arrow3D.ShowMoveArrow(activeMove, generator);
        }

        if (arrowIndicatorUI != null)
        {
            // Nếu đã có Mũi tên 3D World Space sống động, ẩn mũi tên 2D phẳng
            bool show2DFallback = (arrow3D == null);
            arrowIndicatorUI.gameObject.SetActive(show2DFallback);
            if (show2DFallback)
                UpdateArrowTransformAndPulse();
        }
    }

    /// <summary>
    /// Khôi phục toàn bộ 27 viên Cubie về màu sắc chuẩn 100% và ẩn mũi tên chỉ hướng.
    /// </summary>
    public void ClearHighlight()
    {
        isHighlighting = false;
        activeMove = string.Empty;
        activeLayerCubies.Clear();

        if (arrowIndicatorUI != null)
            arrowIndicatorUI.gameObject.SetActive(false);

        if (arrow3D != null)
            arrow3D.Hide();

        if (generator == null)
            generator = FindAnyObjectByType<RubikGenerator>();

        if (generator == null || generator.allCubies == null) return;

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null) continue;
            Renderer[] renderers = cubie.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(null);
            }
        }
    }

    private void ApplyCubieVisualState(Transform cubie, bool isActiveLayer, char face)
    {
        Renderer[] renderers = cubie.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;

            if (isActiveLayer)
            {
                if (r.transform == cubie)
                {
                    // Viền lõi của tầng đang được chọn có ánh đỏ/xanh nổi bật nhẹ như ảnh tham chiếu 3
                    propBlock.Clear();
                    Color highlightCore = new Color(0.28f, 0.14f, 0.18f, 1f);
                    propBlock.SetColor("_BaseColor", highlightCore);
                    propBlock.SetColor("_Color", highlightCore);
                    r.SetPropertyBlock(propBlock);
                }
                else
                {
                    // Giữ nguyên 100% màu sắc gốc chuẩn xác của các nhãn dán (Sticker) trên tầng xoay
                    r.SetPropertyBlock(null);
                }
            }
            else
            {
                // Làm dịu nhẹ (dim) 18 viên không xoay xuống 42% độ sáng để làm nổi bật tầng xoay
                // nhưng vẫn giữ nguyên sắc độ màu của từng sticker
                Color baseCol = r.sharedMaterial.HasProperty("_BaseColor")
                    ? r.sharedMaterial.GetColor("_BaseColor")
                    : r.sharedMaterial.color;

                Color dimmed = new Color(baseCol.r * 0.42f, baseCol.g * 0.42f, baseCol.b * 0.42f, baseCol.a);
                propBlock.Clear();
                propBlock.SetColor("_BaseColor", dimmed);
                propBlock.SetColor("_Color", dimmed);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void EnsureArrow3D()
    {
        if (arrow3D != null) return;

        arrow3D = GetComponentInChildren<Rubik3DArrowIndicator>(true);
        if (arrow3D == null && generator != null)
            arrow3D = generator.GetComponentInChildren<Rubik3DArrowIndicator>(true);

        if (arrow3D == null)
        {
            GameObject arrowObj = new GameObject("Rubik3DArrowIndicator");
            Transform parent = (generator != null) ? generator.transform : transform;
            arrowObj.transform.SetParent(parent, false);
            arrow3D = arrowObj.AddComponent<Rubik3DArrowIndicator>();
        }
    }

    private void UpdateArrowTransformAndPulse()
    {
        if (arrowIndicatorUI == null || string.IsNullOrEmpty(activeMove)) return;

        Camera cam = Camera.main;
        if (cam == null || generator == null) return;

        char face = activeMove[0];
        bool isPrime = activeMove.Contains("'");

        // Tính vị trí 3D cạnh tầng xoay để đặt mũi tên hướng dẫn
        Vector3 worldAnchor = generator.transform.position + GetArrowWorldOffset(face);
        Vector3 screenPos = cam.WorldToScreenPoint(worldAnchor);

        Canvas canvas = arrowIndicatorUI.GetComponentInParent<Canvas>();
        if (canvas != null && screenPos.z > 0f)
        {
            RectTransform canvasRT = canvas.GetComponent<RectTransform>();
            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, uiCam, out Vector2 localPt))
            {
                // Nhịp nhún nhẹ tạo hiệu ứng động trực quan
                float bob = Mathf.Sin(Time.unscaledTime * 4.5f) * 5f;
                arrowIndicatorUI.anchoredPosition = localPt + new Vector2(0f, bob);
            }
        }

        // Hướng lật và góc nghiêng của mũi tên theo chiều xoay của mặt
        GetArrowOrientation(face, isPrime, out Vector3 scale, out float zAngle);
        float pulseScale = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f;
        arrowIndicatorUI.localScale = scale * pulseScale;
        arrowIndicatorUI.localRotation = Quaternion.Euler(0f, 0f, zAngle);
    }

    private Vector3 GetArrowWorldOffset(char face)
    {
        float d = (generator != null ? generator.spacing : 1.05f) * 1.65f;
        switch (face)
        {
            case 'R': return new Vector3(d * 0.95f, d * 0.72f, 0.25f);
            case 'L': return new Vector3(-d * 0.95f, d * 0.72f, -0.25f);
            case 'U': return new Vector3(d * 0.75f, d * 1.05f, 0f);
            case 'D': return new Vector3(d * 0.75f, -d * 0.95f, 0f);
            case 'F': return new Vector3(-d * 0.75f, d * 0.72f, -d * 0.85f);
            case 'B': return new Vector3(d * 0.85f, d * 0.72f, d * 0.85f);
            default:  return new Vector3(d * 0.85f, d * 0.72f, 0f);
        }
    }

    private void GetArrowOrientation(char face, bool isPrime, out Vector3 scale, out float zAngle)
    {
        scale = Vector3.one;
        zAngle = 0f;

        switch (face)
        {
            case 'R':
                // R thuận: đẩy từ trước-dưới lên trên-sau -> mũi tên cong bên phải
                scale = isPrime ? new Vector3(1f, -1f, 1f) : new Vector3(1f, 1f, 1f);
                zAngle = isPrime ? 15f : -10f;
                break;
            case 'L':
                scale = isPrime ? new Vector3(-1f, 1f, 1f) : new Vector3(-1f, -1f, 1f);
                zAngle = 10f;
                break;
            case 'U':
                scale = isPrime ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, 1f, 1f);
                zAngle = isPrime ? -35f : 35f;
                break;
            case 'D':
                scale = isPrime ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
                zAngle = isPrime ? 35f : -35f;
                break;
            case 'F':
                scale = isPrime ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, 1f, 1f);
                zAngle = 0f;
                break;
            case 'B':
                scale = isPrime ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
                zAngle = 0f;
                break;
            default:
                scale = isPrime ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                zAngle = 0f;
                break;
        }
    }

    private float GetLayerCoordinate(char face)
    {
        switch (face)
        {
            case 'R': return 1f;
            case 'L': return -1f;
            case 'U': return 1f;
            case 'D': return -1f;
            case 'F': return -1f;
            case 'B': return 1f;
            default:  return 0f;
        }
    }

    private float GetAxisValue(Vector3 localPos, char face)
    {
        if (face == 'R' || face == 'L' || face == 'M') return localPos.x;
        if (face == 'U' || face == 'D' || face == 'E') return localPos.y;
        return localPos.z;
    }

    #region CONTEXT MENU KIỂM THỬ
    [ContextMenu("▶ TEST HIGHLIGHT: Nước R")]
    public void TestHighlightR() => HighlightMove("R");

    [ContextMenu("▶ TEST HIGHLIGHT: Nước U'")]
    public void TestHighlightUPrime() => HighlightMove("U'");

    [ContextMenu("▶ TEST HIGHLIGHT: Nước F2")]
    public void TestHighlightF2() => HighlightMove("F2");

    [ContextMenu("▶ XÓA HIGHLIGHT & ẨN MŨI TÊN")]
    public void TestClearHighlight() => ClearHighlight();
    #endregion

    private void OnDisable()
    {
        ClearHighlight();
    }
}
