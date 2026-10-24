using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RubikAlgorithmInput : MonoBehaviour
{
    [Header("Tham chiếu Logic")]
    public RubikRotator rotator;
    public RubikStateManager stateManager;

    [Header("UI Panel Nhập Công Thức")]
    public GameObject inputPanel;
    public TMP_InputField formulaInputField;
    public InputField legacyInputField;
    public TextMeshProUGUI statusText;
    public Text legacyStatusText;

    [Header("Nút bấm chức năng")]
    public Button btnPlayAnimate;  // Chế độ 1: Chạy mượt
    public Button btnApplyInstant; // Chế độ 2: Áp dụng ngay
    public Button btnClear;
    public Button btnPaste;
    public Button btnCopy;
    public Button btnClose;

    public string FormulaText
    {
        get
        {
            AutoBindReferences();
            if (formulaInputField != null) return formulaInputField.text;
            if (legacyInputField != null) return legacyInputField.text;
            return string.Empty;
        }
        set
        {
            AutoBindReferences();
            if (formulaInputField != null) formulaInputField.text = value;
            if (legacyInputField != null) legacyInputField.text = value;
        }
    }

    private void Awake()
    {
        AutoBindReferences();

        // Đảm bảo ẩn ngay từ Awake khi scene vừa load
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
    }

    private void Start()
    {
        AutoBindReferences();

        // Đảm bảo lần đầu chạy không bao giờ hiện bảng công thức
        ClosePanel();

        // Đăng ký sự kiện an toàn cho các nút chức năng
        RegisterButtonEvents();
    }

    private void RegisterButtonEvents()
    {
        if (btnPlayAnimate != null)
        {
            btnPlayAnimate.onClick.RemoveListener(OnPlayAnimateClicked);
            btnPlayAnimate.onClick.AddListener(OnPlayAnimateClicked);
        }
        if (btnApplyInstant != null)
        {
            btnApplyInstant.onClick.RemoveListener(OnApplyInstantClicked);
            btnApplyInstant.onClick.AddListener(OnApplyInstantClicked);
        }
        if (btnClear != null)
        {
            btnClear.onClick.RemoveListener(ClearInput);
            btnClear.onClick.AddListener(ClearInput);
        }
        if (btnPaste != null)
        {
            btnPaste.onClick.RemoveListener(PasteFromClipboard);
            btnPaste.onClick.AddListener(PasteFromClipboard);
        }
        if (btnCopy != null)
        {
            btnCopy.onClick.RemoveListener(CopyToClipboard);
            btnCopy.onClick.AddListener(CopyToClipboard);
        }
        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(ClosePanel);
            btnClose.onClick.AddListener(ClosePanel);
        }
    }

    /// <summary>
    /// Tìm chính xác GameObject của Panel chứa giao diện "NHẬP CÔNG THỨC RUBIK".
    /// </summary>
    public GameObject FindActualPanel()
    {
        // 1. Nếu inputPanel đã được gán rõ ràng và không phải là Canvas hay RubikManager
        if (inputPanel != null && inputPanel.name != "RubikCanvas" && inputPanel.name != "RubikManager" && inputPanel.GetComponent<Canvas>() == null)
        {
            return inputPanel;
        }

        // 2. Tìm ngược lên từ ô nhập formulaInputField
        if (formulaInputField != null)
        {
            Transform t = formulaInputField.transform;
            while (t != null && t.parent != null && t.parent.GetComponent<Canvas>() == null)
            {
                string n = t.name.ToLower();
                if (n.Contains("panel") || n.Contains("formula") || n.Contains("popup") || n.Contains("input") || n.Contains("congthuc") || n.Contains("algorithm"))
                {
                    return t.gameObject;
                }
                t = t.parent;
            }
            if (t != null && t.GetComponent<Canvas>() == null)
            {
                return t.gameObject;
            }
        }

        // 3. Tìm theo tiêu đề "NHẬP CÔNG THỨC" trong toàn bộ Scene
        var allTmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var tmp in allTmps)
        {
            if (tmp.text != null && tmp.text.ToUpperInvariant().Contains("NHẬP CÔNG THỨC"))
            {
                Transform p = tmp.transform;
                while (p != null && p.parent != null && p.parent.GetComponent<Canvas>() == null)
                {
                    p = p.parent;
                }
                if (p != null && p.GetComponent<Canvas>() == null)
                {
                    return p.gameObject;
                }
            }
        }

        // 4. Tìm trong các con của chính GameObject này
        Transform childPanel = transform.Find("InputPanel");
        if (childPanel == null) childPanel = transform.Find("Panel");
        if (childPanel == null) childPanel = transform.Find("FormulaPanel");
        if (childPanel != null) return childPanel.gameObject;

        // 5. Nếu chính GameObject này là Panel hợp lệ
        if (name != "RubikCanvas" && name != "RubikManager" && GetComponent<Canvas>() == null)
        {
            return gameObject;
        }

        return null;
    }

    /// <summary>
    /// Tự động tìm kiếm và liên kết các thành phần UI bị thiếu để tránh lỗi NullReferenceException.
    /// </summary>
    public void AutoBindReferences()
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();

        // 1. Tự động xác định GameObject của Panel
        inputPanel = FindActualPanel();

        // 2. Tìm kiếm InputField (Hỗ trợ cả TextMeshPro lẫn UI InputField thông thường)
        if (formulaInputField == null && legacyInputField == null)
        {
            if (inputPanel != null)
            {
                formulaInputField = inputPanel.GetComponentInChildren<TMP_InputField>(true);
                if (formulaInputField == null)
                    legacyInputField = inputPanel.GetComponentInChildren<InputField>(true);
            }

            if (formulaInputField == null)
                formulaInputField = GetComponentInChildren<TMP_InputField>(true);
            if (formulaInputField == null && legacyInputField == null)
                legacyInputField = GetComponentInChildren<InputField>(true);

            if (formulaInputField == null && legacyInputField == null)
            {
                formulaInputField = FindAnyObjectByType<TMP_InputField>(FindObjectsInactive.Include);
                if (formulaInputField == null)
                    legacyInputField = FindAnyObjectByType<InputField>(FindObjectsInactive.Include);
            }
        }

        // 3. Tìm kiếm Status Text
        if (statusText == null && legacyStatusText == null)
        {
            Transform container = inputPanel != null ? inputPanel.transform : (formulaInputField != null ? formulaInputField.transform.parent : transform);
            if (container != null)
            {
                var tmps = container.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.GetComponentInParent<TMP_InputField>() != null) continue;
                    if (t.GetComponentInParent<Button>() != null) continue;
                    if (t.text.Contains("NHẬP CÔNG THỨC")) continue;
                    statusText = t;
                    break;
                }

                if (statusText == null)
                {
                    var txts = container.GetComponentsInChildren<Text>(true);
                    foreach (var t in txts)
                    {
                        if (t.GetComponentInParent<InputField>() != null) continue;
                        if (t.GetComponentInParent<Button>() != null) continue;
                        if (t.text.Contains("NHẬP CÔNG THỨC")) continue;
                        legacyStatusText = t;
                        break;
                    }
                }
            }
        }

        // 4. Tự động nhận diện và gắn sự kiện cho tất cả các nút bấm trên Panel
        Transform buttonContainer = inputPanel != null ? inputPanel.transform : transform;
        Button[] buttons = buttonContainer.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            string bName = b.gameObject.name.ToLower();
            string label = "";
            var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) label = tmp.text.ToLower().Trim();
            else
            {
                var txt = b.GetComponentInChildren<Text>(true);
                if (txt != null) label = txt.text.ToLower().Trim();
            }

            if (btnPlayAnimate == null && (bName.Contains("animate") || bName.Contains("play") || label.Contains("chạy mượt") || label.Contains("mượt")))
                btnPlayAnimate = b;
            else if (btnApplyInstant == null && (bName.Contains("instant") || bName.Contains("apply") || label.Contains("áp dụng") || label.Contains("ngay")))
                btnApplyInstant = b;
            else if (btnPaste == null && (bName.Contains("paste") || label.Contains("dán")))
                btnPaste = b;
            else if (btnClear == null && (bName.Contains("clear") || label.Contains("xóa trắng") || label.Contains("xóa")))
                btnClear = b;
            else if (btnCopy == null && (bName.Contains("copy") || label.Contains("sao chép") || label.Contains("chép")))
                btnCopy = b;
            else if (bName.Contains("close") || bName.Contains("dong") || label == "đóng" || label.Contains("đóng") || label.Contains("thoát"))
            {
                btnClose = b;
                b.onClick.RemoveListener(ClosePanel);
                b.onClick.AddListener(ClosePanel);
            }
        }
    }

    /// <summary>
    /// Bật/Tắt giao diện nhập công thức khi bấm nút "Công thức".
    /// </summary>
    public void TogglePanel()
    {
        AutoBindReferences();

        if (inputPanel == null)
            inputPanel = FindActualPanel();

        if (inputPanel != null)
        {
            bool nextState = !inputPanel.activeSelf;
            inputPanel.SetActive(nextState);

            if (nextState)
            {
                FocusInputField();
                RegisterButtonEvents();
            }
        }
        else
        {
            Debug.LogWarning("[RubikAlgorithmInput] Không tìm thấy inputPanel để bật/tắt!");
        }
    }

    /// <summary>
    /// Đóng hoàn toàn giao diện nhập công thức.
    /// </summary>
    public void ClosePanel()
    {
        if (inputPanel == null)
            inputPanel = FindActualPanel();

        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
            Debug.Log("<color=cyan>[RubikAlgorithmInput] Đã đóng giao diện nhập công thức.</color>");
        }

        // Dự phòng: Nếu có ô nhập, tắt luôn panel cha của nó nếu vẫn còn hiện
        if (formulaInputField != null && formulaInputField.gameObject.activeInHierarchy)
        {
            Transform p = formulaInputField.transform.parent;
            if (p != null && p.GetComponent<Canvas>() == null)
            {
                p.gameObject.SetActive(false);
            }
        }
    }

    private void FocusInputField()
    {
        if (formulaInputField != null)
        {
            formulaInputField.Select();
            formulaInputField.ActivateInputField();
        }
        else if (legacyInputField != null)
        {
            legacyInputField.Select();
            legacyInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// Bộ phân tích cú pháp chuỗi WCA toàn năng (Universal Algorithm Parser)
    /// </summary>
    public static bool TryParseAlgorithm(string input, out List<string> moves, out string errorMessage)
    {
        moves = new List<string>();
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "Vui lòng nhập công thức!";
            return false;
        }

        // 1. Chuẩn hóa toàn bộ Unicode ký tự ẩn, các biến thể dấu nháy đơn, nháy kép, dấu ngoặc
        System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length);
        foreach (char c in input)
        {
            // Bỏ qua zero-width space, BOM, ký tự định dạng ẩn
            if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\uFEFF' || c == '\u00AD')
                continue;

            // Chuyển đổi mọi loại dấu nháy đơn, prime, acute accent thành chuẩn ASCII '\''
            if (IsPrimeChar(c))
            {
                sb.Append('\'');
            }
            // Chuyển đổi dấu nháy kép / double prime thành số 2
            else if (IsDoublePrimeChar(c))
            {
                sb.Append('2');
            }
            // Ký tự số 2 mũ (²)
            else if (c == '²' || c == '\u00B2')
            {
                sb.Append('2');
            }
            // Chuyển các dấu ngoặc nhóm công thức và dấu phân tách thành khoảng trắng: (), [], {}, ,, ;, :, |, /, \, -
            else if ("()[]{},;:|/\\-".IndexOf(c) >= 0)
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }

        string normalized = sb.ToString();

        // 2. Phân tích cú pháp linh hoạt (Tokenizer)
        int i = 0;
        while (i < normalized.Length)
        {
            // Bỏ qua khoảng trắng
            if (char.IsWhiteSpace(normalized[i]))
            {
                i++;
                continue;
            }

            char c = char.ToUpperInvariant(normalized[i]);

            // Nếu gặp mặt Rubik hợp lệ: U, D, R, L, F, B, M, E, S
            if ("UDRLFB MES".IndexOf(c) >= 0)
            {
                char face = c;
                i++;

                // Bỏ qua ký tự 'W' nếu là wide move (ví dụ: Rw -> R, Uw -> U)
                if (i < normalized.Length && (normalized[i] == 'w' || normalized[i] == 'W'))
                {
                    i++;
                }

                // Bỏ qua khoảng trắng nằm giữa mặt và dấu bổ trợ (ví dụ người dùng gõ: R ' hoặc R 2)
                while (i < normalized.Length && char.IsWhiteSpace(normalized[i]))
                {
                    if (i + 1 < normalized.Length && IsModifierChar(normalized[i + 1]))
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                string modifier = "";

                // Kiểm tra modifier liền sau:
                if (i < normalized.Length)
                {
                    char m = normalized[i];

                    // 1. Dấu nghịch đảo: ' hoặc i (inverted) hoặc p (prime)
                    if (m == '\'' || m == 'i' || m == 'I' || m == 'p' || m == 'P')
                    {
                        modifier = "'";
                        i++;

                        // Bỏ qua khoảng trắng nếu có tiếp số 2: R' 2 -> R2
                        while (i < normalized.Length && char.IsWhiteSpace(normalized[i]))
                        {
                            if (i + 1 < normalized.Length && normalized[i + 1] == '2') i++;
                            else break;
                        }

                        if (i < normalized.Length && normalized[i] == '2')
                        {
                            modifier = "2";
                            i++;
                        }
                    }
                    // 2. Quay 180 độ: 2
                    else if (m == '2')
                    {
                        modifier = "2";
                        i++;

                        // Nếu có dấu ' sau số 2 (ví dụ R2' chuẩn WCA vẫn là R2)
                        while (i < normalized.Length && char.IsWhiteSpace(normalized[i]))
                        {
                            if (i + 1 < normalized.Length && (normalized[i + 1] == '\'' || normalized[i + 1] == 'i' || normalized[i + 1] == 'I')) i++;
                            else break;
                        }

                        if (i < normalized.Length && (normalized[i] == '\'' || normalized[i] == 'i' || normalized[i] == 'I' || normalized[i] == 'p' || normalized[i] == 'P'))
                        {
                            i++;
                        }
                    }
                    // 3. Quay 270 độ: 3 (tương đương 1 lần quay ngược ')
                    else if (m == '3')
                    {
                        modifier = "'";
                        i++;
                    }
                }

                moves.Add(face + modifier);
                continue;
            }

            // Bỏ qua các ký tự xoay toàn bộ khối cube rotation nếu có trong công thức (x, y, z)
            if (c == 'X' || c == 'Y' || c == 'Z')
            {
                i++;
                if (i < normalized.Length && (normalized[i] == '\'' || normalized[i] == '2' || normalized[i] == '3'))
                    i++;
                continue;
            }

            // Nếu gặp ký tự không nhận diện được
            errorMessage = $"Ký tự '<b>{normalized[i]}</b>' tại vị trí {i + 1} không hợp lệ! (Chỉ chấp nhận U, D, R, L, F, B, M, E, S và dấu ' hoặc 2)";
            moves.Clear();
            return false;
        }

        if (moves.Count == 0)
        {
            errorMessage = "Không tìm thấy nước đi nào hợp lệ!";
            return false;
        }

        return true;
    }

    private static bool IsModifierChar(char c)
    {
        return c == '\'' || c == '2' || c == '3' ||
               c == 'i' || c == 'I' || c == 'p' || c == 'P' ||
               IsPrimeChar(c) || IsDoublePrimeChar(c);
    }

    private static bool IsPrimeChar(char c)
    {
        return c == '\'' || c == '’' || c == '‘' || c == '′' || c == '‵' ||
               c == '´' || c == '`' || c == 'ʼ' || c == 'ʽ' || c == 'ʻ' ||
               c == 'ˈ' || c == 'ˊ' || c == 'ˋ' || c == '‘' || c == '‛' ||
               c == 'ꞌ' || c == '＇';
    }

    private static bool IsDoublePrimeChar(char c)
    {
        return c == '"' || c == '”' || c == '“' || c == '″' || c == '‶' ||
               c == '˝' || c == '״' || c == '＂';
    }

    // CHẾ ĐỘ 1: Diễn hoạt mượt mà
    public void OnPlayAnimateClicked()
    {
        AutoBindReferences();

        if (rotator == null)
            rotator = FindAnyObjectByType<RubikRotator>();

        if (rotator != null && rotator.IsBusy)
        {
            SetStatus("Khối Rubik đang bận xoay, vui lòng chờ!", new Color32(0xD9, 0x77, 0x06, 255));
            return;
        }

        string rawInput = FormulaText;
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            SetStatus("Vui lòng nhập công thức trước!", new Color32(0xD9, 0x77, 0x06, 255));
            return;
        }

        if (TryParseAlgorithm(rawInput, out List<string> moves, out string error))
        {
            if (rotator == null)
            {
                SetStatus("Không tìm thấy RubikRotator trên Scene!", new Color32(0xDC, 0x26, 0x26, 255));
                return;
            }

            SetStatus($"Đang chạy mượt {moves.Count} nước đi: {string.Join(" ", moves)}", new Color32(0x16, 0xA3, 0x4A, 255));

            foreach (string move in moves)
            {
                rotator.EnqueueMove(move);
            }

            // Đóng hộp thoại để người dùng quan sát trực tiếp hoạt ảnh trên Rubik 3D
            ClosePanel();
        }
        else
        {
            SetStatus("⚠ " + error, new Color32(0xDC, 0x26, 0x26, 255));
        }
    }

    // CHẾ ĐỘ 2: Áp dụng tức thì
    public void OnApplyInstantClicked()
    {
        AutoBindReferences();

        if (rotator == null)
            rotator = FindAnyObjectByType<RubikRotator>();

        if (rotator != null && rotator.IsBusy)
        {
            SetStatus("Khối Rubik đang bận xoay, vui lòng chờ!", new Color32(0xD9, 0x77, 0x06, 255));
            return;
        }

        string rawInput = FormulaText;
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            SetStatus("Vui lòng nhập công thức trước!", new Color32(0xD9, 0x77, 0x06, 255));
            return;
        }

        if (TryParseAlgorithm(rawInput, out List<string> moves, out string error))
        {
            if (rotator == null)
            {
                SetStatus("Không tìm thấy RubikRotator trên Scene!", new Color32(0xDC, 0x26, 0x26, 255));
                return;
            }

            foreach (string move in moves)
            {
                rotator.ExecuteMoveInstant(move);
            }

            SetStatus($"✔ Đã áp dụng tức thì {moves.Count} nước đi!", new Color32(0x16, 0xA3, 0x4A, 255));
        }
        else
        {
            SetStatus("⚠ " + error, new Color32(0xDC, 0x26, 0x26, 255));
        }
    }

    public void ClearInput()
    {
        FormulaText = "";
        SetStatus("Đã xóa nội dung ô nhập.", new Color32(0x64, 0x74, 0x8B, 255));
    }

    public void PasteFromClipboard()
    {
        string clip = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(clip))
        {
            FormulaText = clip;
            SetStatus("✔ Đã dán công thức từ bộ nhớ tạm!", new Color32(0x25, 0x63, 0xEB, 255));
        }
        else
        {
            SetStatus("Bộ nhớ tạm đang trống!", new Color32(0xD9, 0x77, 0x06, 255));
        }
    }

    public void CopyToClipboard()
    {
        string text = FormulaText;
        if (!string.IsNullOrEmpty(text))
        {
            GUIUtility.systemCopyBuffer = text;
            SetStatus("✔ Đã sao chép công thức vào bộ nhớ tạm!", new Color32(0x25, 0x63, 0xEB, 255));
        }
        else
        {
            SetStatus("Ô nhập đang trống, không có gì để sao chép!", new Color32(0xD9, 0x77, 0x06, 255));
        }
    }

    private void SetStatus(string msg, Color col)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = col;
        }
        if (legacyStatusText != null)
        {
            legacyStatusText.text = msg;
            legacyStatusText.color = col;
        }
        if (statusText == null && legacyStatusText == null)
        {
            Debug.Log($"[RubikAlgorithmInput] {msg}");
        }
    }
}
