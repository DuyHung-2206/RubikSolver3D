using UnityEngine;

/// <summary>
/// Hệ thống thiết kế (Design Tokens) tập trung cho toàn bộ giao diện RubikSolver3D.
/// Giữ nguyên tông màu "Friendly Puzzle": nền xanh-xám sáng + thẻ trắng + điểm nhấn xanh dương (bảng màu Slate/Blue).
/// Mọi script UI cần dùng màu / bo góc / bóng đổ ở đây thay vì tự gõ mã hex rời rạc.
/// </summary>
public static class RubikUITheme
{
    // ───────────────────────── NỀN ─────────────────────────
    public static readonly Color32 AppBackground = new Color32(0xEE, 0xF3, 0xF9, 255); // Nền toàn màn hình
    public static readonly Color32 Surface       = new Color32(0xFF, 0xFF, 0xFF, 255); // Thẻ / Panel
    public static readonly Color32 SurfaceSoft   = new Color32(0xF8, 0xFA, 0xFC, 255); // Thẻ phụ
    public static readonly Color32 SurfaceMuted  = new Color32(0xF1, 0xF5, 0xF9, 255); // Ô nhập / vùng chìm

    // ───────────────────────── SLATE (chữ, viền, nền trung tính) ─────────────────────────
    public static readonly Color32 Slate50  = new Color32(0xF8, 0xFA, 0xFC, 255);
    public static readonly Color32 Slate100 = new Color32(0xF1, 0xF5, 0xF9, 255);
    public static readonly Color32 Slate200 = new Color32(0xE2, 0xE8, 0xF0, 255); // Viền / nút phụ
    public static readonly Color32 Slate300 = new Color32(0xCB, 0xD5, 0xE1, 255);
    public static readonly Color32 Slate400 = new Color32(0x94, 0xA3, 0xB8, 255);
    public static readonly Color32 Slate500 = new Color32(0x64, 0x74, 0x8B, 255); // Chữ phụ / icon
    public static readonly Color32 Slate600 = new Color32(0x47, 0x55, 0x69, 255);
    public static readonly Color32 Slate700 = new Color32(0x33, 0x41, 0x55, 255); // Chữ thân
    public static readonly Color32 Slate800 = new Color32(0x1E, 0x29, 0x3B, 255); // Tiêu đề
    public static readonly Color32 Slate900 = new Color32(0x0F, 0x17, 0x2A, 255); // Tiêu đề đậm / Ink

    // ───────────────────────── XANH DƯƠNG (màu thương hiệu) ─────────────────────────
    public static readonly Color32 Blue50  = new Color32(0xEF, 0xF6, 0xFF, 255);
    public static readonly Color32 Blue100 = new Color32(0xDB, 0xEA, 0xFE, 255);
    public static readonly Color32 Blue300 = new Color32(0x93, 0xC5, 0xFD, 255);
    public static readonly Color32 Blue400 = new Color32(0x60, 0xA5, 0xFA, 255);
    public static readonly Color32 Blue500 = new Color32(0x3B, 0x82, 0xF6, 255); // Primary
    public static readonly Color32 Blue600 = new Color32(0x25, 0x63, 0xEB, 255); // Primary đậm
    public static readonly Color32 Blue700 = new Color32(0x1D, 0x4E, 0xD8, 255);
    public static readonly Color32 BrandBlue = new Color32(0x1D, 0x6F, 0xF2, 255);
    public static readonly Color32 Sky100  = new Color32(0xE0, 0xF2, 0xFE, 255);
    public static readonly Color32 Sky600  = new Color32(0x02, 0x84, 0xC7, 255);

    // ───────────────────────── NGỮ NGHĨA (trạng thái) ─────────────────────────
    public static readonly Color32 Success      = new Color32(0x16, 0xA3, 0x4A, 255);
    public static readonly Color32 SuccessLight = new Color32(0x22, 0xC5, 0x5E, 255);
    public static readonly Color32 Warning      = new Color32(0xD9, 0x77, 0x06, 255);
    public static readonly Color32 Danger       = new Color32(0xDC, 0x26, 0x26, 255);
    public static readonly Color32 DangerLight  = new Color32(0xEF, 0x44, 0x44, 255);
    public static readonly Color32 DangerBg     = new Color32(0xFE, 0xE2, 0xE2, 255);
    public static readonly Color32 Violet       = new Color32(0x7C, 0x3A, 0xED, 255);
    public static readonly Color32 VioletBg     = new Color32(0xDD, 0xD6, 0xFE, 255);
    public static readonly Color32 Yellow       = new Color32(0xFA, 0xCC, 0x15, 255);

    // ───────────────────────── BO GÓC (Radius scale) ─────────────────────────
    public const int RadiusXs = 8;   // Chip nhỏ, ô quét
    public const int RadiusSm = 12;  // Nút phụ, ô nhập
    public const int RadiusMd = 16;  // Nút / thẻ nhỏ
    public const int RadiusLg = 20;  // Thẻ lớn, panel dưới
    public const int RadiusXl = 24;  // Sidebar, hộp thoại

    // ───────────────────────── BÓNG ĐỔ & LỚP PHỦ ─────────────────────────
    public const float ShadowCard    = 0.09f; // Thẻ lớn
    public const float ShadowButton  = 0.12f; // Nút tròn / nút nổi
    public const float ShadowHandle  = 0.22f; // Tay nắm slider
    public const float ScrimLight    = 0.38f; // Lớp phủ modal nhẹ
    public const float ScrimStrong   = 0.88f; // Lớp phủ modal đậm (camera / lưới màu)

    /// <summary>Màu mực (Slate900) với độ trong suốt tùy chọn – dùng cho bóng đổ, viền mờ, lớp phủ.</summary>
    public static Color Ink(float alpha)
    {
        return new Color(0.06f, 0.09f, 0.16f, alpha);
    }
}
