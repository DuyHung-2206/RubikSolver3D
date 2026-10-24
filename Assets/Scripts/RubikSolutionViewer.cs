using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RubikSolutionViewer : MonoBehaviour
{
    [Header("Tham chiếu Logic")]
    public RubikRotator rotator;
    public RubikSolver solver;
    public RubikStateManager stateManager;
    public RubikGuideHighlighter guideHighlighter;

    [Header("UI Bảng Hướng Dẫn Giải (Ảnh tham chiếu 3)")]
    public GameObject solverPanel;
    public TextMeshProUGUI stepInfoText;     // "Bước 1/7"
    public TextMeshProUGUI moveBadgeText;    // Ký hiệu lớn trong vòng tròn xanh: "R"
    public TextMeshProUGUI moveTitleText;    // "Xoay mặt phải (R)"
    public TextMeshProUGUI nextMoveText;     // Mô tả chi tiết: "Xoay 90° theo chiều kim đồng hồ"
    [Tooltip("Text hiển thị toàn bộ chuỗi công thức giải (dự phòng)")]
    public TextMeshProUGUI fullFormulaText;
    public Transform chipsContainer;         // Thanh chứa các nút Chip nước đi (R, U, R', U'...)
    public Button btnPrevStep;               // "< Trước"
    public Button btnNextStep;               // "Tiếp theo >"
    public Button btnAutoSolve;              // "⚡ Giải tự động"
    public Button btnCloseSolver;            // Nút "←" Quay lại

    [Header("Dữ liệu bước giải")]
    public List<string> currentSolution = new List<string>();
    [Tooltip("Chỉ số bước thực tế đã thực thi trên khối Rubik 3D")]
    public int currentStepIndex = 0;
    [Tooltip("Chỉ số bước đang được xem trước (Preview) khi người dùng nhấn vào từng Chip")]
    public int previewStepIndex = 0;
    public bool isAutoSolving = false;

    // Lưu trữ danh sách các Chip UI tái sử dụng để tối ưu hiệu năng (Zero GC / Destroy mỗi frame)
    private readonly List<ChipItemUI> chipPool = new List<ChipItemUI>();

    private class ChipItemUI
    {
        public GameObject rootObj;
        public Button button;
        public Image pillBg;
        public Image pillBorder;
        public TextMeshProUGUI label;
        public Image dotImage;
    }

    private void Awake()
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (solver == null) solver = FindAnyObjectByType<RubikSolver>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();
        if (guideHighlighter == null) guideHighlighter = GetComponent<RubikGuideHighlighter>();
        if (guideHighlighter == null) guideHighlighter = FindAnyObjectByType<RubikGuideHighlighter>();
    }

    private void Start()
    {
        if (solverPanel != null) solverPanel.SetActive(false);
        BindButtons();
    }

    public void BindButtons()
    {
        if (btnNextStep != null)
        {
            btnNextStep.onClick.RemoveListener(StepNext);
            btnNextStep.onClick.AddListener(StepNext);
        }
        if (btnPrevStep != null)
        {
            btnPrevStep.onClick.RemoveListener(StepPrevious);
            btnPrevStep.onClick.AddListener(StepPrevious);
        }
        if (btnAutoSolve != null)
        {
            btnAutoSolve.onClick.RemoveListener(ToggleAutoSolve);
            btnAutoSolve.onClick.AddListener(ToggleAutoSolve);
        }
        if (btnCloseSolver != null)
        {
            btnCloseSolver.onClick.RemoveListener(CloseViewer);
            btnCloseSolver.onClick.AddListener(CloseViewer);
        }
    }

    private void Update()
    {
        if (solverPanel != null && solverPanel.activeSelf)
        {
            UpdateButtonsState();
        }
    }

    private void UpdateButtonsState()
    {
        bool isBusy = rotator != null && rotator.IsBusy;
        int total = currentSolution != null ? currentSolution.Count : 0;

        if (total == 0)
        {
            if (btnPrevStep != null) btnPrevStep.interactable = false;
            if (btnNextStep != null) btnNextStep.interactable = false;
            if (btnAutoSolve != null) btnAutoSolve.interactable = false;
            return;
        }

        if (btnAutoSolve != null) btnAutoSolve.interactable = !isBusy || isAutoSolving;
        if (btnPrevStep != null) btnPrevStep.interactable = !isBusy && currentStepIndex > 0 && !isAutoSolving;
        if (btnNextStep != null) btnNextStep.interactable = !isBusy && currentStepIndex < total && !isAutoSolving;
    }

    /// <summary>
    /// Mở giao diện Hướng dẫn giải từng bước (Khi nhấn mục "Hướng dẫn giải" ở Sidebar).
    /// </summary>
    public void StartSolution()
    {
        OpenSolutionInternal(autoStart: false);
    }

    /// <summary>
    /// Mở giao diện Hướng dẫn giải và tự động chạy giải tuần tự (Khi nhấn nút "Giải tự động" ở Bảng điều khiển).
    /// </summary>
    public void StartAutoSolveDirect()
    {
        OpenSolutionInternal(autoStart: true);
    }

    private void OpenSolutionInternal(bool autoStart)
    {
        if (rotator != null && rotator.IsBusy) return;

        if (solver == null)
            solver = FindAnyObjectByType<RubikSolver>();
        if (guideHighlighter == null)
        {
            guideHighlighter = GetComponent<RubikGuideHighlighter>();
            if (guideHighlighter == null && rotator != null)
                guideHighlighter = rotator.gameObject.AddComponent<RubikGuideHighlighter>();
        }

        currentSolution = solver != null ? solver.SolveCurrentState() : new List<string>();
        currentStepIndex = 0;
        previewStepIndex = 0;
        isAutoSolving = false;

        if (solverPanel != null) solverPanel.SetActive(true);

        if (currentSolution == null || currentSolution.Count == 0)
        {
            RebuildChipsUI();
            if (guideHighlighter != null) guideHighlighter.ClearHighlight();

            if (stateManager != null && stateManager.IsSolved())
            {
                if (stepInfoText != null) stepInfoText.text = "Hoàn thành";
                if (moveBadgeText != null) moveBadgeText.text = "✓";
                if (moveTitleText != null) moveTitleText.text = "Khối Rubik đã hoàn thành!";
                if (nextMoveText != null) nextMoveText.text = "Hãy nhấn 'Trộn khối' ở Trang chủ để tạo trạng thái mới và xem hướng dẫn từng bước.";
                if (fullFormulaText != null) fullFormulaText.text = "6 mặt đã đồng màu hoàn hảo.";
            }
            else
            {
                string error = (solver != null && !string.IsNullOrEmpty(solver.LastErrorMessage))
                    ? solver.LastErrorMessage
                    : "Không tìm thấy lời giải hợp lệ cho khối Rubik này!";

                if (stepInfoText != null) stepInfoText.text = "Lỗi trạng thái";
                if (moveBadgeText != null) moveBadgeText.text = "!";
                if (moveTitleText != null) moveTitleText.text = "Không thể giải khối Rubik";
                if (nextMoveText != null) nextMoveText.text = error + " (Kiểm tra lại màu đã quét hoặc chỉnh trong bảng Tô màu 2D).";
                if (fullFormulaText != null) fullFormulaText.text = error;
            }
            UpdateButtonsState();
            return;
        }

        if (stateManager != null) stateManager.isExecutingUndoRedo = false;

        RebuildChipsUI();
        UpdateDisplay();

        if (autoStart && currentSolution.Count > 0)
        {
            StartCoroutine(AutoSolveCoroutine());
        }
    }

    /// <summary>
    /// Xem trước (Preview) một bước bất kỳ khi nhấn vào Chip mà KHÔNG làm thay đổi trạng thái logic RubikState.
    /// </summary>
    public void PreviewStep(int index)
    {
        if (currentSolution == null || index < 0 || index >= currentSolution.Count) return;
        if (isAutoSolving) return;

        previewStepIndex = index;
        UpdateDisplay();
    }

    /// <summary>
    /// Thực hiện bước hiện tại lên khối Rubik 3D và tiến sang bước tiếp theo.
    /// </summary>
    public void StepNext()
    {
        if (rotator == null || rotator.IsBusy || currentSolution == null || currentStepIndex >= currentSolution.Count) return;

        string move = currentSolution[currentStepIndex];
        previewStepIndex = currentStepIndex;
        UpdateDisplay();

        currentStepIndex++;
        previewStepIndex = Mathf.Min(currentStepIndex, currentSolution.Count - 1);

        StartCoroutine(ExecuteSingleStep(move));
    }

    /// <summary>
    /// Lùi lại bước trước đó cả trên khối Rubik 3D và ma trận logic.
    /// </summary>
    public void StepPrevious()
    {
        if (rotator == null || rotator.IsBusy || currentSolution == null || currentStepIndex <= 0) return;

        currentStepIndex--;
        previewStepIndex = currentStepIndex;
        string lastMove = currentSolution[currentStepIndex];
        string inverse = RubikStateManager.GetInverseMove(lastMove);

        StartCoroutine(ExecuteSingleStep(inverse));
    }

    private IEnumerator ExecuteSingleStep(string move)
    {
        if (stateManager != null) stateManager.isExecutingUndoRedo = true;
        rotator.EnqueueMove(move);

        while (rotator.IsBusy)
        {
            yield return null;
        }

        if (!isAutoSolving && stateManager != null)
        {
            stateManager.isExecutingUndoRedo = false;
        }

        UpdateDisplay();
    }

    public void ToggleAutoSolve()
    {
        if (isAutoSolving)
        {
            isAutoSolving = false;
            StopAllCoroutines();
            if (stateManager != null) stateManager.isExecutingUndoRedo = false;
            UpdateDisplay();
        }
        else if (currentSolution != null && currentStepIndex < currentSolution.Count)
        {
            StartCoroutine(AutoSolveCoroutine());
        }
    }

    private IEnumerator AutoSolveCoroutine()
    {
        isAutoSolving = true;
        if (stateManager != null) stateManager.isExecutingUndoRedo = true;
        UpdateDisplay();

        while (isAutoSolving && currentStepIndex < currentSolution.Count)
        {
            while (rotator.IsBusy) yield return null;

            previewStepIndex = currentStepIndex;
            UpdateDisplay();

            string move = currentSolution[currentStepIndex];
            currentStepIndex++;
            rotator.EnqueueMove(move);

            while (rotator.IsBusy) yield return null;

            previewStepIndex = Mathf.Min(currentStepIndex, Mathf.Max(0, currentSolution.Count - 1));
            UpdateDisplay();
            yield return new WaitForSeconds(0.08f);
        }

        isAutoSolving = false;
        if (stateManager != null) stateManager.isExecutingUndoRedo = false;
        UpdateDisplay();
    }

    public void CloseViewer()
    {
        isAutoSolving = false;
        if (stateManager != null) stateManager.isExecutingUndoRedo = false;
        StopAllCoroutines();

        if (guideHighlighter != null)
            guideHighlighter.ClearHighlight();

        if (solverPanel != null)
            solverPanel.SetActive(false);

        var uiCtrl = FindAnyObjectByType<RubikUIController>();
        if (uiCtrl != null)
            uiCtrl.OnGuideClosed();
    }

    private void RebuildChipsUI()
    {
        if (chipsContainer == null) return;

        int total = currentSolution != null ? currentSolution.Count : 0;

        while (chipPool.Count < total)
        {
            ChipItemUI item = CreateChipElement(chipsContainer, chipPool.Count);
            chipPool.Add(item);
        }

        for (int i = 0; i < chipPool.Count; i++)
        {
            bool active = i < total;
            if (chipPool[i].rootObj != null)
                chipPool[i].rootObj.SetActive(active);

            if (active)
            {
                int capturedIndex = i;
                chipPool[i].label.text = currentSolution[i];
                chipPool[i].button.onClick.RemoveAllListeners();
                chipPool[i].button.onClick.AddListener(() => PreviewStep(capturedIndex));
            }
        }
    }

    private ChipItemUI CreateChipElement(Transform parent, int idx)
    {
        ChipItemUI item = new ChipItemUI();

        GameObject root = new GameObject($"MoveChip_{idx}", typeof(RectTransform), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(64, 58);

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 64;
        le.preferredHeight = 58;
        le.minWidth = 56;

        // Nút Pill phía trên
        GameObject pillObj = new GameObject("Pill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        pillObj.transform.SetParent(root.transform, false);
        RectTransform pillRT = pillObj.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(0.5f, 1f);
        pillRT.anchorMax = new Vector2(0.5f, 1f);
        pillRT.pivot = new Vector2(0.5f, 1f);
        pillRT.sizeDelta = new Vector2(60, 38);
        pillRT.anchoredPosition = new Vector2(0, -2);

        Image pillBg = pillObj.GetComponent<Image>();
        pillBg.sprite = RubikUISpriteFactory.GetRoundedSprite(14);
        pillBg.type = Image.Type.Sliced;

        // Viền mảnh cho Chip không chọn
        GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderObj.transform.SetParent(pillObj.transform, false);
        RectTransform borderRT = borderObj.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.sizeDelta = Vector2.zero;
        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.sprite = RubikUISpriteFactory.GetRoundedSprite(14, 2);
        borderImg.type = Image.Type.Sliced;
        borderImg.raycastTarget = false;

        // Nhãn ký hiệu nước đi (R, U, R', U'...)
        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(pillObj.transform, false);
        RectTransform lblRT = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = lblObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // Chấm tròn trạng thái phía dưới mỗi Chip (giống ảnh tham chiếu số 3)
        GameObject dotObj = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotObj.transform.SetParent(root.transform, false);
        RectTransform dotRT = dotObj.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0.5f, 0f);
        dotRT.anchorMax = new Vector2(0.5f, 0f);
        dotRT.pivot = new Vector2(0.5f, 0f);
        dotRT.sizeDelta = new Vector2(9, 9);
        dotRT.anchoredPosition = new Vector2(0, 3);

        Image dotImg = dotObj.GetComponent<Image>();
        dotImg.sprite = RubikUISpriteFactory.GetCircleSprite();
        dotImg.raycastTarget = false;

        item.rootObj = root;
        item.button = pillObj.GetComponent<Button>();
        item.pillBg = pillBg;
        item.pillBorder = borderImg;
        item.label = tmp;
        item.dotImage = dotImg;
        return item;
    }

    public void UpdateDisplay()
    {
        if (solverPanel == null || !solverPanel.activeSelf) return;

        int total = currentSolution != null ? currentSolution.Count : 0;
        if (total == 0) return;

        bool isCompleted = currentStepIndex >= total;
        int activeIdx = isCompleted ? (total - 1) : Mathf.Clamp(previewStepIndex, 0, total - 1);
        bool isPreviewingOtherStep = !isCompleted && (previewStepIndex != currentStepIndex);

        // 1. Thanh tiêu đề: "Bước 3/7"
        if (stepInfoText != null)
        {
            if (isCompleted)
                stepInfoText.text = $"Hoàn thành ({total}/{total})";
            else if (isPreviewingOtherStep)
                stepInfoText.text = $"Xem bước {activeIdx + 1}/{total} (Đang ở {currentStepIndex + 1}/{total})";
            else
                stepInfoText.text = $"Bước {currentStepIndex + 1}/{total}";
        }

        // 2. Thẻ hướng dẫn nền xanh nhạt (Ký hiệu lớn, Tiêu đề mặt, Mô tả chiều xoay)
        if (isCompleted)
        {
            if (moveBadgeText != null) moveBadgeText.text = "✓";
            if (moveTitleText != null) moveTitleText.text = "🎉 Đã hoàn tất giải khối Rubik!";
            if (nextMoveText != null) nextMoveText.text = "Toàn bộ 6 mặt đã đồng màu hoàn hảo.";
            if (guideHighlighter != null) guideHighlighter.ClearHighlight();
        }
        else
        {
            string move = currentSolution[activeIdx];
            GetMoveExplanationVi(move, out string badge, out string title, out string desc);

            if (isPreviewingOtherStep)
                desc += "  •  (Chế độ xem trước — nhấn 'Tiếp theo' để thực hiện bước hiện tại)";

            if (moveBadgeText != null) moveBadgeText.text = badge;
            if (moveTitleText != null) moveTitleText.text = title;
            if (nextMoveText != null) nextMoveText.text = desc;

            // Làm nổi bật đúng tầng sẽ xoay trên Rubik 3D và hiển thị mũi tên cong chỉ hướng
            if (guideHighlighter != null)
                guideHighlighter.HighlightMove(move);
        }

        // 3. Cập nhật trạng thái màu sắc của các Chip nước đi
        Color32 activeBlue = new Color32(0x1D, 0x6F, 0xF2, 255);
        Color32 doneBlueLight = new Color32(0xE0, 0xF2, 0xFE, 255);
        Color32 borderGray = new Color32(0xCB, 0xD5, 0xE1, 255);
        Color32 textDark = new Color32(0x1E, 0x29, 0x3B, 255);
        Color32 dotGray = new Color32(0xD1, 0xD5, 0xDB, 255);

        for (int i = 0; i < total && i < chipPool.Count; i++)
        {
            var chip = chipPool[i];
            bool isSelected = !isCompleted && (i == activeIdx);
            bool isDone = i < currentStepIndex;

            if (isSelected)
            {
                chip.pillBg.color = activeBlue;
                chip.pillBorder.color = Color.clear;
                chip.label.color = Color.white;
                chip.dotImage.color = activeBlue;
            }
            else if (isDone)
            {
                chip.pillBg.color = doneBlueLight;
                chip.pillBorder.color = new Color32(0x93, 0xC5, 0xFD, 255);
                chip.label.color = new Color32(0x1D, 0x4E, 0xD8, 255);
                chip.dotImage.color = new Color32(0x60, 0xA5, 0xFA, 255);
            }
            else
            {
                chip.pillBg.color = Color.white;
                chip.pillBorder.color = borderGray;
                chip.label.color = textDark;
                chip.dotImage.color = dotGray;
            }
        }

        // 4. Cập nhật chuỗi văn bản dự phòng (nếu có gán fullFormulaText)
        if (fullFormulaText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < total; i++)
            {
                if (i == activeIdx && !isCompleted)
                    sb.Append($"<color=#2563EB><b>[{currentSolution[i]}]</b></color> ");
                else if (i < currentStepIndex)
                    sb.Append($"<color=#64748B><s>{currentSolution[i]}</s></color> ");
                else
                    sb.Append($"{currentSolution[i]} ");
            }
            fullFormulaText.text = sb.ToString();
        }

        if (btnAutoSolve != null)
        {
            var textTMP = btnAutoSolve.GetComponentInChildren<TextMeshProUGUI>();
            if (textTMP != null) textTMP.text = isAutoSolving ? "Tạm dừng" : "⚡ Tự động";
        }
    }

    /// <summary>
    /// Trả về thông tin giải thích chi tiết bằng tiếng Việt cho từng ký hiệu nước đi chuẩn WCA.
    /// </summary>
    public static void GetMoveExplanationVi(string move, out string badge, out string title, out string description)
    {
        badge = string.IsNullOrEmpty(move) ? "?" : move.Trim();
        char face = badge.Length > 0 ? char.ToUpperInvariant(badge[0]) : 'R';
        bool isPrime = badge.Contains("'");
        bool isDouble = badge.Contains("2");

        string faceName;
        switch (face)
        {
            case 'R': faceName = "Xoay mặt phải"; break;
            case 'L': faceName = "Xoay mặt trái"; break;
            case 'U': faceName = "Xoay mặt trên"; break;
            case 'D': faceName = "Xoay mặt dưới"; break;
            case 'F': faceName = "Xoay mặt trước"; break;
            case 'B': faceName = "Xoay mặt sau"; break;
            case 'M': faceName = "Xoay tầng giữa dọc"; break;
            case 'E': faceName = "Xoay tầng giữa ngang"; break;
            case 'S': faceName = "Xoay tầng giữa trước-sau"; break;
            default:  faceName = "Xoay mặt"; break;
        }

        title = $"{faceName} ({badge})";

        if (isDouble)
            description = "Xoay 180° (2 lần 90°) theo chiều kim đồng hồ";
        else if (isPrime)
            description = "Xoay 90° ngược chiều kim đồng hồ";
        else
            description = "Xoay 90° theo chiều kim đồng hồ";
    }
}
