using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tạo và lưu đệm (cache) các Sprite bo góc 9-slice, hình tròn, mũi tên cong và bộ Icon Vector sắc nét
/// phục vụ giao diện "Friendly Puzzle" mà không cần phụ thuộc vào gói tài nguyên bên ngoài hay lỗi font Unicode.
/// </summary>
public static class RubikUISpriteFactory
{
    public enum IconType
    {
        Home,
        Camera,
        Lightbulb,
        Settings,
        Grid2D,
        Shuffle,
        Play,
        Undo,
        Redo,
        SparkleBrain,
        Book,
        Reset,
        Paste,
        Copy,
        Trash,
        Clock,
        CubeReset,
        ZoomIn,
        ZoomOut,
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ChevronLeft,
        ChevronRight,
        BackArrow,
        Check
    }

    private static readonly Dictionary<string, Sprite> roundedCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<IconType, Sprite> iconCache = new Dictionary<IconType, Sprite>();
    private static Sprite circleSprite;
    private static Sprite rubikLogoSprite;
    private static Sprite curvedRotationArrowSprite;
    private static Sprite orbitArcSprite;

    /// <summary>
    /// Tạo 9-slice Sprite hình chữ nhật bo góc mượt mà với bán kính tuỳ chỉnh.
    /// </summary>
    public static Sprite GetRoundedSprite(int radius = 16, int borderThickness = 0)
    {
        radius = Mathf.Clamp(radius, 2, 40);
        string key = $"{radius}_{borderThickness}";
        if (roundedCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int size = Mathf.Max(64, radius * 3);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = $"RoundedRect_{key}"
        };

        Color[] pixels = new Color[size * size];
        float r = radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float dx = 0f;
                if (px < r) dx = r - px;
                else if (px > size - r) dx = px - (size - r);

                float dy = 0f;
                if (py < r) dy = r - py;
                else if (py > size - r) dy = py - (size - r);

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(r - dist + 0.5f);

                if (borderThickness > 0 && alpha > 0f)
                {
                    float innerR = Mathf.Max(1f, r - borderThickness);
                    float idx = 0f;
                    if (px < r) idx = r - px;
                    else if (px > size - r) idx = px - (size - r);
                    else if (px < borderThickness) idx = borderThickness - px;
                    else if (px > size - borderThickness) idx = px - (size - borderThickness);

                    float idy = 0f;
                    if (py < r) idy = r - py;
                    else if (py > size - r) idy = py - (size - r);
                    else if (py < borderThickness) idy = borderThickness - py;
                    else if (py > size - borderThickness) idy = py - (size - borderThickness);

                    float edgeDistX = Mathf.Min(px, size - px);
                    float edgeDistY = Mathf.Min(py, size - py);
                    float minEdge = Mathf.Min(edgeDistX, edgeDistY);
                    if (dx > 0f || dy > 0f)
                        minEdge = r - dist;

                    float borderAlpha = Mathf.Clamp01(borderThickness - minEdge + 0.5f);
                    alpha *= borderAlpha;
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        roundedCache[key] = sprite;
        return sprite;
    }

    /// <summary>
    /// Tạo Sprite hình tròn khử răng cưa.
    /// </summary>
    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int size = 96;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "CircleSprite"
        };

        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float a = Mathf.Clamp01(radius - d + 0.6f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    /// <summary>
    /// Tạo Logo khối Rubik 3D Isometric nhiều màu sắc giống ảnh tham chiếu số 1.
    /// </summary>
    public static Sprite GetRubikLogoSprite()
    {
        if (rubikLogoSprite != null) return rubikLogoSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "RubikLogo3D"
        };

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Vector2 center = new Vector2(64f, 64f);
        float R = 52f;
        float cos30 = 0.8660254f;
        float sin30 = 0.5f;

        Vector2 pCenter = center;
        Vector2 pTop = center + new Vector2(0f, R);
        Vector2 pBottom = center + new Vector2(0f, -R);
        Vector2 pTopRight = center + new Vector2(R * cos30, R * sin30);
        Vector2 pBottomRight = center + new Vector2(R * cos30, -R * sin30);
        Vector2 pTopLeft = center + new Vector2(-R * cos30, R * sin30);
        Vector2 pBottomLeft = center + new Vector2(-R * cos30, -R * sin30);

        Color colTop = new Color32(0xFA, 0xCC, 0x15, 255);   // Vàng tươi
        Color colLeft = new Color32(0x22, 0xC5, 0x5E, 255);  // Xanh lá
        Color colRight = new Color32(0xEF, 0x44, 0x44, 255); // Đỏ cam
        Color colAltTop = new Color32(0xFB, 0x92, 0x3C, 255);
        Color colAltLeft = new Color32(0x3B, 0x82, 0xF6, 255);
        Color colDark = new Color32(0x1E, 0x29, 0x3B, 255);

        // Vẽ 3 mặt hình thoi của khối lập phương Isometric
        FillQuadWithGrid(pixels, size, pCenter, pTopLeft, pTop, pTopRight, colTop, colAltTop, colDark);
        FillQuadWithGrid(pixels, size, pBottom, pBottomLeft, pTopLeft, pCenter, colLeft, colAltLeft, colDark);
        FillQuadWithGrid(pixels, size, pBottom, pCenter, pTopRight, pBottomRight, colRight, colTop, colDark);

        tex.SetPixels(pixels);
        tex.Apply();
        rubikLogoSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return rubikLogoSprite;
    }

    private static void FillQuadWithGrid(Color[] pixels, int size, Vector2 p00, Vector2 p01, Vector2 p11, Vector2 p10, Color mainCol, Color accentCol, Color lineCol)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                if (TryGetBilinearUV(pt, p00, p10, p11, p01, out float u, out float v))
                {
                    // Kẻ lưới 3x3
                    float gu = (u * 3f) % 1f;
                    float gv = (v * 3f) % 1f;
                    int cellU = Mathf.Clamp((int)(u * 3f), 0, 2);
                    int cellV = Mathf.Clamp((int)(v * 3f), 0, 2);

                    bool isBorder = u < 0.045f || u > 0.955f || v < 0.045f || v > 0.955f ||
                                    gu < 0.08f || gu > 0.92f || gv < 0.08f || gv > 0.92f;

                    Color c = ((cellU + cellV) == 1) ? accentCol : mainCol;
                    pixels[y * size + x] = isBorder ? lineCol : c;
                }
            }
        }
    }

    private static bool TryGetBilinearUV(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float u, out float v)
    {
        u = 0f;
        v = 0f;
        Vector2 ab = b - a;
        Vector2 ad = d - a;
        Vector2 ap = p - a;
        float det = ab.x * ad.y - ab.y * ad.x;
        if (Mathf.Abs(det) < 0.001f) return false;

        u = (ap.x * ad.y - ap.y * ad.x) / det;
        v = (ab.x * ap.y - ab.y * ap.x) / det;
        return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
    }

    /// <summary>
    /// Tạo Sprite mũi tên cong chỉ hướng xoay 3D giống ảnh tham chiếu số 3.
    /// </summary>
    public static Sprite GetCurvedRotationArrowSprite()
    {
        if (curvedRotationArrowSprite != null) return curvedRotationArrowSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "CurvedRotationArrow"
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(58f, 62f);
        float radius = 38f;
        float thickness = 6.5f;

        // Cung tròn từ góc 145 độ xuống -25 độ (theo chiều kim đồng hồ)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Vector2 diff = p - center;
                float dist = diff.magnitude;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                float alpha = 0f;
                if (angle >= -15f && angle <= 145f)
                {
                    float ringDist = Mathf.Abs(dist - radius);
                    // Vuốt thon phần đuôi tại 145 độ
                    float t = Mathf.InverseLerp(145f, 20f, angle);
                    float curThick = Mathf.Lerp(2.0f, thickness, t);
                    alpha = Mathf.Clamp01(curThick - ringDist + 0.6f);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        // Đầu mũi tên tam giác tại góc -18 độ
        float headAngle = -15f * Mathf.Deg2Rad;
        Vector2 arcEnd = center + new Vector2(Mathf.Cos(headAngle), Mathf.Sin(headAngle)) * radius;
        Vector2 tangent = new Vector2(Mathf.Sin(headAngle), -Mathf.Cos(headAngle)); // Hướng xuôi kim đồng hồ
        Vector2 normal = new Vector2(Mathf.Cos(headAngle), Mathf.Sin(headAngle));

        Vector2 tip = arcEnd + tangent * 16f;
        Vector2 leftWing = arcEnd - tangent * 4f + normal * 14f;
        Vector2 rightWing = arcEnd - tangent * 4f - normal * 14f;

        DrawFilledTriangle(pixels, size, tip, leftWing, rightWing, Color.white);

        tex.SetPixels(pixels);
        tex.Apply();
        curvedRotationArrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return curvedRotationArrowSprite;
    }

    /// <summary>
    /// Tạo vòng cung xoay trang trí phía dưới khối Rubik 3D giống ảnh tham chiếu số 1.
    /// </summary>
    public static Sprite GetOrbitArcSprite()
    {
        if (orbitArcSprite != null) return orbitArcSprite;

        int w = 512;
        int h = 160;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "OrbitArcSprite"
        };

        Color[] pixels = new Color[w * h];
        Vector2 center = new Vector2(w * 0.5f, h * 0.62f);
        float rx = 220f;
        float ry = 58f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float angle = Mathf.Atan2(p.y / ry, p.x / rx) * Mathf.Rad2Deg;

                // Chỉ vẽ nửa dưới từ -168 độ đến -12 độ
                float alpha = 0f;
                if (angle >= -168f && angle <= -12f)
                {
                    float norm = Mathf.Sqrt((p.x * p.x) / (rx * rx) + (p.y * p.y) / (ry * ry));
                    float distPx = Mathf.Abs(norm - 1f) * 95f;
                    alpha = Mathf.Clamp01(1.8f - distPx);

                    // Làm mờ nhẹ ở chính giữa đáy để nổi bật 2 cánh cung 2 bên như ảnh 1
                    float midFade = Mathf.Clamp01(Mathf.Abs(angle + 90f) / 35f);
                    alpha *= Mathf.Lerp(0.25f, 0.85f, midFade);
                }

                pixels[y * w + x] = new Color(0.58f, 0.64f, 0.72f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        orbitArcSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return orbitArcSprite;
    }

    /// <summary>
    /// Trả về Sprite icon vector sắc nét theo loại IconType.
    /// </summary>
    public static Sprite GetIcon(IconType type)
    {
        if (iconCache.TryGetValue(type, out Sprite cached) && cached != null)
            return cached;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = $"Icon_{type}"
        };

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        DrawIconShape(pixels, size, type);

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        iconCache[type] = sp;
        return sp;
    }

    private static void DrawIconShape(Color[] p, int s, IconType type)
    {
        Color w = Color.white;
        switch (type)
        {
            case IconType.Home:
                // Mái nhà tam giác + thân nhà
                DrawFilledTriangle(p, s, new Vector2(32, 54), new Vector2(10, 34), new Vector2(54, 34), w);
                DrawFilledRect(p, s, 16, 12, 32, 23, w);
                DrawFilledRect(p, s, 27, 12, 10, 13, Color.clear); // Cửa
                break;

            case IconType.Camera:
                // Thân máy ảnh + ống kính tròn
                DrawRoundedBox(p, s, 10, 14, 44, 30, 6f, w);
                DrawRoundedBox(p, s, 22, 42, 20, 8, 3f, w);
                DrawCircle(p, s, new Vector2(32, 29), 10f, Color.clear);
                DrawCircle(p, s, new Vector2(32, 29), 6.5f, w);
                break;

            case IconType.Lightbulb:
                // Bóng đèn hướng dẫn
                DrawCircle(p, s, new Vector2(32, 38), 14f, w);
                DrawFilledRect(p, s, 25, 16, 14, 12, w);
                DrawFilledRect(p, s, 27, 11, 10, 3, w);
                break;

            case IconType.Settings:
                // Bánh răng cài đặt
                DrawCircle(p, s, new Vector2(32, 32), 14f, w);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    Vector2 c = new Vector2(32 + Mathf.Cos(a) * 16f, 32 + Mathf.Sin(a) * 16f);
                    DrawCircle(p, s, c, 4.5f, w);
                }
                DrawCircle(p, s, new Vector2(32, 32), 6.5f, Color.clear);
                break;

            case IconType.Grid2D:
                // Lưới 3x3 Tô màu 2D
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        DrawRoundedBox(p, s, 11 + c * 15, 11 + r * 15, 12, 12, 2.5f, w);
                    }
                }
                break;

            case IconType.Shuffle:
                // Hai mũi tên đan chéo (Trộn khối)
                DrawLine(p, s, new Vector2(12, 20), new Vector2(46, 44), 3.2f, w);
                DrawLine(p, s, new Vector2(12, 44), new Vector2(46, 20), 3.2f, w);
                DrawFilledTriangle(p, s, new Vector2(54, 46), new Vector2(43, 49), new Vector2(47, 37), w);
                DrawFilledTriangle(p, s, new Vector2(54, 18), new Vector2(47, 27), new Vector2(43, 15), w);
                break;

            case IconType.Play:
                // Tam giác Play (Bắt đầu)
                DrawFilledTriangle(p, s, new Vector2(50, 32), new Vector2(18, 50), new Vector2(18, 14), w);
                break;

            case IconType.Undo:
                // Mũi tên vòng sang trái (Hoàn tác)
                DrawArc(p, s, new Vector2(34, 28), 14f, 3.5f, -30f, 180f, w);
                DrawFilledTriangle(p, s, new Vector2(14, 30), new Vector2(25, 38), new Vector2(25, 22), w);
                break;

            case IconType.Redo:
                // Mũi tên vòng sang phải (Làm lại)
                DrawArc(p, s, new Vector2(30, 28), 14f, 3.5f, 0f, 210f, w);
                DrawFilledTriangle(p, s, new Vector2(50, 30), new Vector2(39, 38), new Vector2(39, 22), w);
                break;

            case IconType.SparkleBrain:
                // Ngôi sao AI / Giải tự động
                DrawSparkle(p, s, new Vector2(30, 30), 17f, w);
                DrawSparkle(p, s, new Vector2(47, 46), 8f, w);
                DrawSparkle(p, s, new Vector2(16, 46), 6f, w);
                break;

            case IconType.Book:
                // Cuốn sách mở (Xem công thức)
                DrawRoundedBox(p, s, 10, 14, 21, 36, 3f, w);
                DrawRoundedBox(p, s, 33, 14, 21, 36, 3f, w);
                DrawRoundedBox(p, s, 14, 18, 13, 28, 1f, Color.clear);
                DrawRoundedBox(p, s, 37, 18, 13, 28, 1f, Color.clear);
                break;

            case IconType.Reset:
                // Vòng tròn mũi tên Đặt lại
                DrawArc(p, s, new Vector2(32, 32), 16f, 3.6f, -50f, 240f, w);
                DrawFilledTriangle(p, s, new Vector2(48, 42), new Vector2(37, 45), new Vector2(45, 31), w);
                break;

            case IconType.Paste:
                // Bảng ghi nhớ (Dán)
                DrawRoundedBox(p, s, 14, 10, 36, 42, 4f, w);
                DrawRoundedBox(p, s, 18, 14, 28, 32, 2f, Color.clear);
                DrawRoundedBox(p, s, 23, 46, 18, 8, 3f, w);
                break;

            case IconType.Copy:
                // Hai trang giấy chồng nhau (Sao chép)
                DrawRoundedBox(p, s, 12, 18, 28, 34, 4f, w);
                DrawRoundedBox(p, s, 16, 22, 20, 26, 2f, Color.clear);
                DrawRoundedBox(p, s, 24, 10, 28, 34, 4f, w);
                DrawRoundedBox(p, s, 28, 14, 20, 26, 2f, Color.clear);
                break;

            case IconType.Trash:
                // Thùng rác (Xóa)
                DrawRoundedBox(p, s, 16, 10, 32, 34, 4f, w);
                DrawRoundedBox(p, s, 20, 14, 24, 26, 2f, Color.clear);
                DrawFilledRect(p, s, 12, 46, 40, 4, w);
                DrawFilledRect(p, s, 25, 50, 14, 4, w);
                DrawFilledRect(p, s, 26, 18, 3, 18, w);
                DrawFilledRect(p, s, 35, 18, 3, 18, w);
                break;

            case IconType.Clock:
                // Đồng hồ tốc độ
                DrawCircle(p, s, new Vector2(32, 32), 20f, w);
                DrawCircle(p, s, new Vector2(32, 32), 16f, Color.clear);
                DrawLine(p, s, new Vector2(32, 32), new Vector2(32, 43), 2.8f, w);
                DrawLine(p, s, new Vector2(32, 32), new Vector2(41, 27), 2.8f, w);
                break;

            case IconType.CubeReset:
                // Khối hộp + vòng xoay camera
                DrawArc(p, s, new Vector2(32, 32), 21f, 2.8f, -150f, 150f, w);
                DrawRoundedBox(p, s, 21, 21, 22, 22, 4f, w);
                DrawRoundedBox(p, s, 25, 25, 14, 14, 2f, Color.clear);
                break;

            case IconType.ZoomIn:
                // Kính lúp dấu +
                DrawCircle(p, s, new Vector2(28, 36), 15f, w);
                DrawCircle(p, s, new Vector2(28, 36), 11.5f, Color.clear);
                DrawLine(p, s, new Vector2(39, 25), new Vector2(51, 13), 4.0f, w);
                DrawFilledRect(p, s, 22, 34, 12, 4, w);
                DrawFilledRect(p, s, 26, 30, 4, 12, w);
                break;

            case IconType.ZoomOut:
                // Kính lúp dấu -
                DrawCircle(p, s, new Vector2(28, 36), 15f, w);
                DrawCircle(p, s, new Vector2(28, 36), 11.5f, Color.clear);
                DrawLine(p, s, new Vector2(39, 25), new Vector2(51, 13), 4.0f, w);
                DrawFilledRect(p, s, 22, 34, 12, 4, w);
                break;

            case IconType.ArrowUp:
                DrawLine(p, s, new Vector2(32, 14), new Vector2(32, 46), 3.6f, w);
                DrawLine(p, s, new Vector2(20, 36), new Vector2(32, 48), 3.6f, w);
                DrawLine(p, s, new Vector2(44, 36), new Vector2(32, 48), 3.6f, w);
                break;

            case IconType.ArrowDown:
                DrawLine(p, s, new Vector2(32, 18), new Vector2(32, 50), 3.6f, w);
                DrawLine(p, s, new Vector2(20, 28), new Vector2(32, 16), 3.6f, w);
                DrawLine(p, s, new Vector2(44, 28), new Vector2(32, 16), 3.6f, w);
                break;

            case IconType.ArrowLeft:
            case IconType.BackArrow:
                DrawLine(p, s, new Vector2(16, 32), new Vector2(50, 32), 3.8f, w);
                DrawLine(p, s, new Vector2(28, 44), new Vector2(15, 32), 3.8f, w);
                DrawLine(p, s, new Vector2(28, 20), new Vector2(15, 32), 3.8f, w);
                break;

            case IconType.ArrowRight:
                DrawLine(p, s, new Vector2(14, 32), new Vector2(48, 32), 3.8f, w);
                DrawLine(p, s, new Vector2(36, 44), new Vector2(49, 32), 3.8f, w);
                DrawLine(p, s, new Vector2(36, 20), new Vector2(49, 32), 3.8f, w);
                break;

            case IconType.ChevronLeft:
                DrawLine(p, s, new Vector2(38, 48), new Vector2(22, 32), 4.0f, w);
                DrawLine(p, s, new Vector2(22, 32), new Vector2(38, 16), 4.0f, w);
                break;

            case IconType.ChevronRight:
                DrawLine(p, s, new Vector2(26, 48), new Vector2(42, 32), 4.0f, w);
                DrawLine(p, s, new Vector2(42, 32), new Vector2(26, 16), 4.0f, w);
                break;

            case IconType.Check:
                DrawLine(p, s, new Vector2(14, 32), new Vector2(27, 20), 4.2f, w);
                DrawLine(p, s, new Vector2(27, 20), new Vector2(50, 44), 4.2f, w);
                break;
        }
    }

    private static void DrawFilledRect(Color[] p, int s, int rx, int ry, int rw, int rh, Color col)
    {
        for (int y = Mathf.Max(0, ry); y < Mathf.Min(s, ry + rh); y++)
            for (int x = Mathf.Max(0, rx); x < Mathf.Min(s, rx + rw); x++)
                p[y * s + x] = col;
    }

    private static void DrawRoundedBox(Color[] p, int s, int rx, int ry, int rw, int rh, float radius, Color col)
    {
        for (int y = Mathf.Max(0, ry); y < Mathf.Min(s, ry + rh); y++)
        {
            for (int x = Mathf.Max(0, rx); x < Mathf.Min(s, rx + rw); x++)
            {
                float lx = x - rx + 0.5f;
                float ly = y - ry + 0.5f;
                float dx = 0f;
                if (lx < radius) dx = radius - lx;
                else if (lx > rw - radius) dx = lx - (rw - radius);

                float dy = 0f;
                if (ly < radius) dy = radius - ly;
                else if (ly > rh - radius) dy = ly - (rh - radius);

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius + 0.5f)
                    p[y * s + x] = col;
            }
        }
    }

    private static void DrawCircle(Color[] p, int s, Vector2 center, float radius, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (d <= radius + 0.4f)
                    p[y * s + x] = col;
            }
        }
    }

    private static void DrawArc(Color[] p, int s, Vector2 center, float radius, float thickness, float startDeg, float endDeg, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Vector2 diff = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = diff.magnitude;
                if (Mathf.Abs(dist - radius) <= thickness * 0.5f + 0.4f)
                {
                    float deg = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                    if (deg < startDeg) deg += 360f;
                    if (deg >= startDeg && deg <= endDeg)
                        p[y * s + x] = col;
                }
            }
        }
    }

    private static void DrawSparkle(Color[] p, int s, Vector2 center, float r, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - center.x) / r;
                float dy = Mathf.Abs(y + 0.5f - center.y) / r;
                if (Mathf.Pow(dx, 0.55f) + Mathf.Pow(dy, 0.55f) <= 1.0f)
                    p[y * s + x] = col;
            }
        }
    }

    private static void DrawLine(Color[] p, int s, Vector2 a, Vector2 b, float halfWidth, Color col)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                float t = lenSq > 0.001f ? Mathf.Clamp01(Vector2.Dot(pt - a, ab) / lenSq) : 0f;
                Vector2 proj = a + ab * t;
                float d = Vector2.Distance(pt, proj);
                if (d <= halfWidth)
                    p[y * s + x] = col;
            }
        }
    }

    private static void DrawFilledTriangle(Color[] p, int s, Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Sign(pt, a, b);
                float d2 = Sign(pt, b, c);
                float d3 = Sign(pt, c, a);
                bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                if (!(hasNeg && hasPos))
                    p[y * s + x] = col;
            }
        }
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
