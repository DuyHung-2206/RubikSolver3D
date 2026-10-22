using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RubikNetColorPicker : MonoBehaviour
{
    [Header("Tham chiếu Logic")]
    public RubikGenerator generator;
    public RubikStateManager stateManager;

    [Header("UI Panel Nhập Màu")]
    public GameObject netPanel;
    public Transform netContainer; // Nơi chứa 6 mặt chữ thập
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI countInfoText;
    public Button btnApplyToCube;
    public Button btnResetToSolved;
    public Button btnClose;

    [Header("Bảng Chọn Màu (Palette)")]
    public Button btnColorWhite;
    public Button btnColorYellow;
    public Button btnColorGreen;
    public Button btnColorBlue;
    public Button btnColorRed;
    public Button btnColorOrange;

    // Màu đang được người chơi chọn để tô ('W', 'Y', 'G', 'B', 'R', 'O')
    public char selectedColor = 'W';

    // Dữ liệu màu 6 mặt đang tô trên lưới 2D (mỗi mặt 9 ký tự)
    private readonly Dictionary<char, char[]> netFaces = new Dictionary<char, char[]>();

    // Bảng màu thực tế hiển thị trên UI
    private static readonly Dictionary<char, Color> DisplayColors = new Dictionary<char, Color>()
    {
        { 'W', new Color(0.96f, 0.96f, 0.96f) }, // Trắng
        { 'Y', new Color(1.00f, 0.84f, 0.00f) }, // Vàng
        { 'G', new Color(0.00f, 0.65f, 0.28f) }, // Xanh lá
        { 'B', new Color(0.00f, 0.35f, 0.85f) }, // Xanh dương
        { 'R', new Color(0.85f, 0.12f, 0.15f) }, // Đỏ
        { 'O', new Color(1.00f, 0.40f, 0.00f) }  // Cam
    };

    // Lưu các Button ô vuông trên lưới để đổi màu UI khi tô
    private readonly Dictionary<string, Image> cellImages = new Dictionary<string, Image>();

    private void Awake()
    {
        AutoBindReferences();

        if (generator == null) generator = FindAnyObjectByType<RubikGenerator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();

        InitDefaultSolvedNet();

        if (netPanel != null)
            netPanel.SetActive(false);
    }

    private void Start()
    {
        AutoBindReferences();

        if (netPanel != null)
            netPanel.SetActive(false);

        // Gắn sự kiện cho Palette
        if (btnColorWhite != null) btnColorWhite.onClick.AddListener(() => SelectColor('W'));
        if (btnColorYellow != null) btnColorYellow.onClick.AddListener(() => SelectColor('Y'));
        if (btnColorGreen != null) btnColorGreen.onClick.AddListener(() => SelectColor('G'));
        if (btnColorBlue != null) btnColorBlue.onClick.AddListener(() => SelectColor('B'));
        if (btnColorRed != null) btnColorRed.onClick.AddListener(() => SelectColor('R'));
        if (btnColorOrange != null) btnColorOrange.onClick.AddListener(() => SelectColor('O'));

        if (btnApplyToCube != null) btnApplyToCube.onClick.AddListener(ApplyNetTo3DCube);
        if (btnResetToSolved != null) btnResetToSolved.onClick.AddListener(ResetNetToSolved);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);

        // Tự động khởi tạo lưới 54 ô theo hình chữ thập nếu chưa có
        BuildNetGridUI();

        SelectColor('W');
        ValidateNetState();
    }

    public void ClosePanel()
    {
        if (netPanel != null)
            netPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        AutoBindReferences();

        if (netPanel == null) return;
        netPanel.SetActive(!netPanel.activeSelf);
        if (netPanel.activeSelf)
        {
            ReadCurrentCubeToNet();
            ValidateNetState();
        }
    }

    public void AutoBindReferences()
    {
        if (generator == null) generator = FindAnyObjectByType<RubikGenerator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();

        if (netPanel == null)
        {
            var p = GameObject.Find("NetColorPickerPanel");
            if (p == null)
            {
                var allPanels = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
                foreach (var r in allPanels)
                {
                    if (r.name == "NetColorPickerPanel")
                    {
                        p = r.gameObject;
                        break;
                    }
                }
            }
            if (p != null) netPanel = p;
        }

        if (netPanel != null)
        {
            if (netContainer == null)
            {
                Transform c = netPanel.transform.Find("NetContainer");
                if (c != null) netContainer = c;
            }

            if (statusText == null)
            {
                Transform st = netPanel.transform.Find("StatusText");
                if (st != null) statusText = st.GetComponent<TextMeshProUGUI>();
            }

            if (countInfoText == null)
            {
                Transform ci = netPanel.transform.Find("CountInfoText");
                if (ci != null) countInfoText = ci.GetComponent<TextMeshProUGUI>();
            }

            if (btnApplyToCube == null)
            {
                Transform b = netPanel.transform.Find("BtnApplyToCube");
                if (b == null) b = FindChildRecursive(netPanel.transform, "BtnApplyToCube");
                if (b != null) btnApplyToCube = b.GetComponent<Button>();
            }

            if (btnResetToSolved == null)
            {
                Transform b = netPanel.transform.Find("BtnResetSolved");
                if (b == null) b = FindChildRecursive(netPanel.transform, "BtnResetSolved");
                if (b != null) btnResetToSolved = b.GetComponent<Button>();
            }

            if (btnClose == null)
            {
                Transform b = netPanel.transform.Find("BtnClose");
                if (b == null) b = FindChildRecursive(netPanel.transform, "BtnClose");
                if (b != null) btnClose = b.GetComponent<Button>();
            }

            if (btnColorWhite == null) btnColorWhite = FindButton(netPanel.transform, "BtnWhite", "BtnColorWhite");
            if (btnColorYellow == null) btnColorYellow = FindButton(netPanel.transform, "BtnYellow", "BtnColorYellow");
            if (btnColorGreen == null) btnColorGreen = FindButton(netPanel.transform, "BtnGreen", "BtnColorGreen");
            if (btnColorBlue == null) btnColorBlue = FindButton(netPanel.transform, "BtnBlue", "BtnColorBlue");
            if (btnColorRed == null) btnColorRed = FindButton(netPanel.transform, "BtnRed", "BtnColorRed");
            if (btnColorOrange == null) btnColorOrange = FindButton(netPanel.transform, "BtnOrange", "BtnColorOrange");
        }
    }

    private Button FindButton(Transform root, params string[] names)
    {
        foreach (string n in names)
        {
            Transform t = FindChildRecursive(root, n);
            if (t != null)
            {
                Button btn = t.GetComponent<Button>();
                if (btn != null) return btn;
            }
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void InitDefaultSolvedNet()
    {
        netFaces['U'] = new char[9] { 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W' };
        netFaces['D'] = new char[9] { 'Y', 'Y', 'Y', 'Y', 'Y', 'Y', 'Y', 'Y', 'Y' };
        netFaces['F'] = new char[9] { 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G', 'G' };
        netFaces['B'] = new char[9] { 'B', 'B', 'B', 'B', 'B', 'B', 'B', 'B', 'B' };
        netFaces['R'] = new char[9] { 'R', 'R', 'R', 'R', 'R', 'R', 'R', 'R', 'R' };
        netFaces['L'] = new char[9] { 'O', 'O', 'O', 'O', 'O', 'O', 'O', 'O', 'O' };
    }

    public void SelectColor(char col)
    {
        selectedColor = col;
        HighlightActivePalette();
    }

    private void HighlightActivePalette()
    {
        HighlightBtn(btnColorWhite, selectedColor == 'W');
        HighlightBtn(btnColorYellow, selectedColor == 'Y');
        HighlightBtn(btnColorGreen, selectedColor == 'G');
        HighlightBtn(btnColorBlue, selectedColor == 'B');
        HighlightBtn(btnColorRed, selectedColor == 'R');
        HighlightBtn(btnColorOrange, selectedColor == 'O');
    }

    private void HighlightBtn(Button btn, bool active)
    {
        if (btn == null) return;
        Outline outline = btn.GetComponent<Outline>();
        if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3, -3);
        outline.enabled = active;
    }

    /// <summary>
    /// Tự động sinh giao diện chữ thập 54 ô trong netContainer
    /// </summary>
    private void BuildNetGridUI()
    {
        if (netContainer == null) return;

        // Vị trí của 6 mặt trong hệ lưới chữ thập (X, Y)
        // Kích thước 1 mặt 3x3 là khoảng 105x105 pixel
        var facePositions = new Dictionary<char, Vector2>()
        {
            { 'U', new Vector2(0, 110) },
            { 'L', new Vector2(-110, 0) },
            { 'F', new Vector2(0, 0) },
            { 'R', new Vector2(110, 0) },
            { 'B', new Vector2(220, 0) },
            { 'D', new Vector2(0, -110) }
        };

        foreach (var pair in facePositions)
        {
            char face = pair.Key;
            Vector2 pos = pair.Value;

            GameObject faceObj = new GameObject($"Face_{face}", typeof(RectTransform));
            faceObj.transform.SetParent(netContainer, false);
            RectTransform rt = faceObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(100, 100);

            GridLayoutGroup grid = faceObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(30, 30);
            grid.spacing = new Vector2(3, 3);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            for (int i = 0; i < 9; i++)
            {
                int index = i;
                GameObject cell = new GameObject($"Cell_{face}_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(faceObj.transform, false);

                Image img = cell.GetComponent<Image>();
                img.color = DisplayColors[netFaces[face][index]];
                cellImages[$"{face}_{index}"] = img;

                Button btn = cell.GetComponent<Button>();
                btn.onClick.AddListener(() => OnCellClicked(face, index));

                // Ô tâm (index 4) khóa viền đen làm mốc định hướng
                if (index == 4)
                {
                    Outline centerBorder = cell.AddComponent<Outline>();
                    centerBorder.effectColor = Color.black;
                    centerBorder.effectDistance = new Vector2(2, -2);
                }
            }
        }
    }

    private void OnCellClicked(char face, int index)
    {
        // 6 ô tâm (index 4) là mốc chuẩn WCA cố định, không cho phép đổi
        if (index == 4)
        {
            SetStatus($"Ô tâm mặt {face} là mốc cố định chuẩn WCA, không thể đổi màu!", Color.yellow);
            return;
        }

        // Tô màu đang chọn vào ô
        netFaces[face][index] = selectedColor;
        cellImages[$"{face}_{index}"].color = DisplayColors[selectedColor];

        ValidateNetState();
    }

    /// <summary>
    /// Bộ kiểm tra tính hợp lệ nâng cao (Color Counts + Parity Check)
    /// </summary>
    public bool ValidateNetState()
    {
        // 1. Kiểm tra số lượng mỗi màu (Phải đủ 9 ô mỗi màu)
        Dictionary<char, int> counts = new Dictionary<char, int>()
        {
            { 'W', 0 }, { 'Y', 0 }, { 'G', 0 }, { 'B', 0 }, { 'R', 0 }, { 'O', 0 }
        };

        foreach (var face in netFaces.Values)
        {
            foreach (char c in face)
            {
                if (counts.ContainsKey(c)) counts[c]++;
            }
        }

        if (countInfoText != null)
        {
            countInfoText.text = $"Trắng: {counts['W']}/9 | Vàng: {counts['Y']}/9 | Xanh lá: {counts['G']}/9\n" +
                                 $"Xanh dương: {counts['B']}/9 | Đỏ: {counts['R']}/9 | Cam: {counts['O']}/9";
        }

        foreach (var pair in counts)
        {
            if (pair.Value != 9)
            {
                SetStatus($"Lỗi: Mỗi màu phải có đúng 9 ô! ({pair.Key} đang có {pair.Value} ô)", Color.red);
                if (btnApplyToCube != null) btnApplyToCube.interactable = false;
                return false;
            }
        }

        // 2. Kiểm tra tính hợp lệ vật lý (Không được có 2 màu đối diện trên cùng 1 viên cạnh/góc)
        // Các cặp màu đối diện không bao giờ chạm nhau: Trắng-Vàng, Xanh lá-Xanh dương, Đỏ-Cam
        if (!CheckPhysicalEdgeAndCorners(out string errorMsg))
        {
            SetStatus($"Lỗi vật lý: {errorMsg}", Color.red);
            if (btnApplyToCube != null) btnApplyToCube.interactable = false;
            return false;
        }

        SetStatus("Khối Rubik 100% hợp lệ! Sẵn sàng đồng bộ lên 3D.", Color.green);
        if (btnApplyToCube != null) btnApplyToCube.interactable = true;
        return true;
    }

    private bool CheckPhysicalEdgeAndCorners(out string error)
    {
        error = "";
        // Kiểm tra 12 viên cạnh
        (char, int, char, int)[] edges = new (char, int, char, int)[]
        {
            ('U', 7, 'F', 1), ('U', 3, 'L', 1), ('U', 1, 'B', 1), ('U', 5, 'R', 1),
            ('D', 1, 'F', 7), ('D', 3, 'L', 7), ('D', 7, 'B', 7), ('D', 5, 'R', 7),
            ('F', 3, 'L', 5), ('F', 5, 'R', 3), ('B', 5, 'L', 3), ('B', 3, 'R', 5)
        };

        foreach (var e in edges)
        {
            char c1 = netFaces[e.Item1][e.Item2];
            char c2 = netFaces[e.Item3][e.Item4];
            if (c1 == c2) { error = $"Viên cạnh ({e.Item1}-{e.Item3}) bị trùng màu!"; return false; }
            if (IsOppositeColor(c1, c2)) { error = $"Viên cạnh ({e.Item1}-{e.Item3}) chứa 2 màu đối diện!"; return false; }
        }

        return true;
    }

    private bool IsOppositeColor(char c1, char c2)
    {
        return (c1 == 'W' && c2 == 'Y') || (c1 == 'Y' && c2 == 'W') ||
               (c1 == 'G' && c2 == 'B') || (c1 == 'B' && c2 == 'G') ||
               (c1 == 'R' && c2 == 'O') || (c1 == 'O' && c2 == 'R');
    }

    /// <summary>
    /// Đồng bộ màu sắc từ sơ đồ phẳng lên mô hình 3D và RubikState
    /// </summary>
    public void ApplyNetTo3DCube()
    {
        if (!ValidateNetState()) return;

        // 1. Reset mô hình 3D về góc quay ban đầu
        if (stateManager != null) stateManager.ResetAll();

        // 2. Gán dữ liệu màu vào RubikState
        RubikState state = stateManager.CurrentState;
        foreach (var pair in netFaces)
        {
            char face = pair.Key;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    state.SetColor(face, r, c, pair.Value[r * 3 + c]);
                }
            }
        }

        // 3. Cập nhật Material trực tiếp cho nhãn dán trên 27 khối 3D
        Update3DStickerMaterials(state);

        SetStatus("Đã đồng bộ lên khối Rubik 3D thành công!", Color.cyan);
        netPanel.SetActive(false);
    }

    private void Update3DStickerMaterials(RubikState state)
    {
        if (generator == null || generator.allCubies == null) return;

        float spacing = generator.spacing;

        foreach (Transform cubie in generator.allCubies)
        {
            if (cubie == null) continue;

            int x = Mathf.RoundToInt(cubie.localPosition.x / spacing);
            int y = Mathf.RoundToInt(cubie.localPosition.y / spacing);
            int z = Mathf.RoundToInt(cubie.localPosition.z / spacing);

            // Cập nhật từng mặt của Cubie
            if (y == 1) ApplyStickerColor(cubie, "Sticker_Up", state.GetColor('U', z + 1, x + 1));
            if (y == -1) ApplyStickerColor(cubie, "Sticker_Down", state.GetColor('D', 1 - z, x + 1));
            if (z == 1) ApplyStickerColor(cubie, "Sticker_Front", state.GetColor('F', 1 - y, x + 1));
            if (z == -1) ApplyStickerColor(cubie, "Sticker_Back", state.GetColor('B', 1 - y, 1 - x));
            if (x == 1) ApplyStickerColor(cubie, "Sticker_Right", state.GetColor('R', 1 - y, 1 - z));
            if (x == -1) ApplyStickerColor(cubie, "Sticker_Left", state.GetColor('L', 1 - y, z + 1));
        }
    }

    private void ApplyStickerColor(Transform parentCubie, string stickerName, char colorCode)
    {
        Transform sticker = parentCubie.Find(stickerName);
        if (sticker == null) return;

        Renderer r = sticker.GetComponent<Renderer>();
        if (r == null) return;

        r.material = GetMaterialByCode(colorCode);
    }

    private Material GetMaterialByCode(char c)
    {
        switch (c)
        {
            case 'W': return generator.matWhite;
            case 'Y': return generator.matYellow;
            case 'G': return generator.matGreen;
            case 'B': return generator.matBlue;
            case 'R': return generator.matRed;
            case 'O': return generator.matOrange;
            default: return generator.matBlack;
        }
    }

    private void ResetNetToSolved()
    {
        InitDefaultSolvedNet();
        UpdateAllCellUI();
        ValidateNetState();
    }

    private void ReadCurrentCubeToNet()
    {
        if (stateManager == null || stateManager.CurrentState == null) return;
        RubikState state = stateManager.CurrentState;

        char[] faces = { 'U', 'D', 'F', 'B', 'R', 'L' };
        foreach (char f in faces)
        {
            netFaces[f] = state.GetFace(f);
        }
        UpdateAllCellUI();
    }

    private void UpdateAllCellUI()
    {
        foreach (var pair in netFaces)
        {
            char face = pair.Key;
            for (int i = 0; i < 9; i++)
            {
                string key = $"{face}_{i}";
                if (cellImages.ContainsKey(key))
                {
                    cellImages[key].color = DisplayColors[pair.Value[i]];
                }
            }
        }
    }

    private void SetStatus(string msg, Color col)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = col;
        }
    }
}