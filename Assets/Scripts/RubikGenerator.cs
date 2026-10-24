using System.Collections.Generic;
using UnityEngine;

public class RubikGenerator : MonoBehaviour
{
    [Header("Cài đặt kích thước & khoảng cách")]
    public float spacing = 1.05f;
    public float stickerOffset = 0.505f;

    [Header("Materials màu sắc (Để trống sẽ tự tạo màu)")]
    public Material matWhite;  // Up (+Y)
    public Material matYellow; // Down (-Y)
    public Material matGreen;  // Front (-Z)
    public Material matBlue;   // Back (+Z)
    public Material matRed;    // Right (+X)
    public Material matOrange; // Left (-X)
    public Material matBlack;  // Lõi đen

    [HideInInspector]
    public List<Transform> allCubies = new List<Transform>();

    public static RubikGenerator Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[RubikGenerator] Phát hiện trùng lặp RubikGenerator trên '{gameObject.name}'. Đang tự hủy instance này để tránh lỗi đè khối.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        InitializeRubik();
    }

    // Tự động khôi phục khối nếu người dùng sửa code trong lúc đang bật Play
    void OnEnable()
    {
        if (Application.isPlaying && allCubies.Count == 0)
        {
            InitializeRubik();
        }
    }

    public void InitializeRubik()
    {
        // Đảm bảo RubikManager nằm chuẩn ở tâm (0,0,0)
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Xóa các component MeshRenderer/Filter/Collider nếu RubikManager vô tình được tạo từ một 3D Cube
        MeshRenderer selfRenderer = GetComponent<MeshRenderer>();
        if (selfRenderer != null) DestroyImmediate(selfRenderer);

        MeshFilter selfFilter = GetComponent<MeshFilter>();
        if (selfFilter != null) DestroyImmediate(selfFilter);

        Collider selfCollider = GetComponent<Collider>();
        if (selfCollider != null) DestroyImmediate(selfCollider);

        InitDefaultMaterials();
        GenerateRubik();
    }

    void InitDefaultMaterials()
    {
        // Đồng bộ 100% mã màu với bảng màu 2D (RubikNetColorPicker.DisplayColors)
        matWhite = CreateStickerMaterial(new Color(0.96f, 0.96f, 0.96f));  // Trắng
        matYellow = CreateStickerMaterial(new Color(1.00f, 0.84f, 0.00f)); // Vàng
        matGreen = CreateStickerMaterial(new Color(0.00f, 0.65f, 0.28f));  // Xanh lá
        matBlue = CreateStickerMaterial(new Color(0.00f, 0.35f, 0.85f));   // Xanh dương
        matRed = CreateStickerMaterial(new Color(0.85f, 0.12f, 0.15f));    // Đỏ
        matOrange = CreateStickerMaterial(new Color(1.00f, 0.40f, 0.00f)); // Cam
        matBlack = CreateColorMaterial(new Color(0.10f, 0.10f, 0.12f), 0.3f); // Lõi đen
    }

    Material CreateStickerMaterial(Color color)
    {
        // Dùng Unlit cho nhãn dán 3D để màu sắc luôn tươi sáng, đồng nhất tuyệt đối với bảng màu 2D ở mọi góc nhìn
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        return mat;
    }

    Material CreateColorMaterial(Color color, float smoothness = 0.5f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    public void GenerateRubik()
    {
        allCubies.Clear();

        // Xóa sạch các đối tượng con cũ bên trong RubikManager
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Tạo 27 khối nhỏ
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 position = new Vector3(x, y, z) * spacing;
                    GameObject cubie = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubie.name = $"Cubie_{x + 1}_{y + 1}_{z + 1}";
                    cubie.transform.SetParent(this.transform, false);
                    cubie.transform.localPosition = position;
                    cubie.transform.localRotation = Quaternion.identity;
                    cubie.transform.localScale = Vector3.one;

                    cubie.GetComponent<Renderer>().material = matBlack;

                    CreateStickers(cubie, x, y, z);
                    allCubies.Add(cubie.transform);
                }
            }
        }
    }

    void CreateStickers(GameObject parentCubie, int x, int y, int z)
    {
        if (y == 1)
            AttachSticker(parentCubie, Vector3.up * stickerOffset,
                Quaternion.Euler(90, 0, 0), matWhite, "Sticker_Up");

        if (y == -1)
            AttachSticker(parentCubie, Vector3.down * stickerOffset,
                Quaternion.Euler(-90, 0, 0), matYellow, "Sticker_Down");

        // Trong hệ tọa độ Left-Handed của Unity:
        // Nhìn từ phía trước vào khối Rubik (Up = +Y, Right = +X, Left = -X):
        // Mặt Trước (Front - Xanh lá) nằm ở -Z (Vector3.back), Mặt Sau (Back - Xanh dương) nằm ở +Z (Vector3.forward).
        if (z == -1)
            AttachSticker(parentCubie, Vector3.back * stickerOffset,
                Quaternion.identity, matGreen, "Sticker_Front");

        if (z == 1)
            AttachSticker(parentCubie, Vector3.forward * stickerOffset,
                Quaternion.Euler(0, 180, 0), matBlue, "Sticker_Back");

        if (x == 1)
            AttachSticker(parentCubie, Vector3.right * stickerOffset,
                Quaternion.Euler(0, -90, 0), matRed, "Sticker_Right");

        if (x == -1)
            AttachSticker(parentCubie, Vector3.left * stickerOffset,
                Quaternion.Euler(0, 90, 0), matOrange, "Sticker_Left");
    }

    void AttachSticker(
        GameObject parent,
        Vector3 localPos,
        Quaternion localRot,
        Material mat,
        string stickerName)
    {
        GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sticker.name = stickerName;

        sticker.transform.SetParent(parent.transform, false);
        sticker.transform.localPosition = localPos;
        sticker.transform.localRotation = localRot;

        // Kích thước nhãn dán bo viền đẹp và không bị lẹm thân đen
        sticker.transform.localScale = new Vector3(0.90f, 0.90f, 0.008f);

        Collider col = sticker.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = sticker.GetComponent<Renderer>();
        renderer.material = mat;
    }
}
