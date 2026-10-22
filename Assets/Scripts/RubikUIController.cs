using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RubikUIController : MonoBehaviour
{
    [Header("Tham chiếu Logic")]
    public RubikRotator rotator;
    public RubikStateManager stateManager;
    public RubikScrambler scrambler;
    public SpeedcubingTimer timer;

    [Header("Thanh Đỉnh (Top Bar)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scrambleText;
    public TextMeshProUGUI pbText;

    [Header("Thanh Công Cụ (Bottom Panel)")]
    public Button btnScramble;
    public Button btnStart;
    public Button btnUndo;
    public Button btnRedo;
    public Button btnReset;
    public Slider speedSlider;
    public TextMeshProUGUI speedValueText;
    public Button btnAlgorithm;
    public Button btnColorPicker;
    public Button btnCameraScanner;
    public Button btnSolve;

    [Header("Bảng Chiến Thắng (Victory Overlay)")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryTimeText;
    public TextMeshProUGUI victoryMovesText;
    public Button btnPlayAgain;
   
    private int moveCount = 0;

    private void Awake()
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();
        if (scrambler == null) scrambler = FindAnyObjectByType<RubikScrambler>();
        if (timer == null) timer = FindAnyObjectByType<SpeedcubingTimer>();
    }

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // Gắn sự kiện cho các nút bấm
        if (btnScramble != null) btnScramble.onClick.AddListener(OnScrambleClicked);
        if (btnStart != null) btnStart.onClick.AddListener(OnStartClicked);
        if (btnUndo != null) btnUndo.onClick.AddListener(() => stateManager?.Undo3D());
        if (btnRedo != null) btnRedo.onClick.AddListener(() => stateManager?.Redo3D());
        if (btnReset != null) btnReset.onClick.AddListener(OnResetClicked);
        if (btnPlayAgain != null) btnPlayAgain.onClick.AddListener(OnResetClicked);
        if (btnSolve != null) btnSolve.onClick.AddListener(() => {
            var viewer = FindAnyObjectByType<RubikSolutionViewer>();
            viewer?.StartSolution();
        });

        if (btnAlgorithm != null) btnAlgorithm.onClick.AddListener(() => {
            var algInput = FindAnyObjectByType<RubikAlgorithmInput>(FindObjectsInactive.Include);
            algInput?.TogglePanel();
        });

        if (btnColorPicker != null) btnColorPicker.onClick.AddListener(() => {
            var picker = FindAnyObjectByType<RubikNetColorPicker>(FindObjectsInactive.Include);
            picker?.TogglePanel();
        });

        if (btnCameraScanner != null) btnCameraScanner.onClick.AddListener(() => {
            var scanner = FindAnyObjectByType<RubikCameraScanner>(FindObjectsInactive.Include);
            scanner?.ToggleScanner();
        });
        // Cài đặt thanh trượt tốc độ
        if (speedSlider != null && rotator != null)
        {
            speedSlider.minValue = 0.05f;
            speedSlider.maxValue = 0.50f;
            speedSlider.value = rotator.rotationDuration;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            UpdateSpeedLabel(speedSlider.value);
        }

        UpdatePBDisplay();

        // TỰ ĐỘNG TÔ MÀU FLAT DESIGN HIỆN ĐẠI CHO CÁC NÚT BẤM
        ApplyModernFlatDesign();
    }

    private void OnEnable()
    {
        RubikStateManager.OnRubikSolved += ShowVictory;
        RubikStateManager.OnMoveExecuted += CountPlayerMove;
    }

    private void OnDisable()
    {
        RubikStateManager.OnRubikSolved -= ShowVictory;
        RubikStateManager.OnMoveExecuted -= CountPlayerMove;
    }

    private void Update()
    {
        // 1. Cập nhật thời gian hiển thị
        if (timerText != null && timer != null)
        {
            if (timer.currentState == SpeedcubingTimer.TimerState.Running)
                timerText.text = SpeedcubingTimer.FormatTime(timer.elapsedTime);
        }

        // 2. TỰ ĐỘNG KHÓA / MỞ KHÓA TƯƠNG TÁC KHI ĐANG BẬN XOAY TRỘN
        UpdateInteractableStates();
    }

    private void UpdateInteractableStates()
    {
        bool isBusy = rotator != null && rotator.IsBusy;

        // Khi đang bận xoay (nhất là lúc trộn tốc độ cao): Vô hiệu hóa nút để tránh spam
        if (btnScramble != null) btnScramble.interactable = !isBusy;
        if (btnStart != null) btnStart.interactable = !isBusy && (timer == null || timer.currentState != SpeedcubingTimer.TimerState.Running);
        if (btnUndo != null) btnUndo.interactable = !isBusy;
        if (btnRedo != null) btnRedo.interactable = !isBusy;
        if (btnReset != null) btnReset.interactable = !isBusy;
        if (btnSolve != null) btnSolve.interactable = !isBusy;
        if (btnAlgorithm != null) btnAlgorithm.interactable = !isBusy;
        if (btnColorPicker != null) btnColorPicker.interactable = !isBusy;
        if (btnCameraScanner != null) btnCameraScanner.interactable = !isBusy;
        if (speedSlider != null) speedSlider.interactable = !isBusy;
    }

    private void OnStartClicked()
    {
        if (rotator != null && rotator.IsBusy) return;

        if (timer != null)
        {
            moveCount = 0;
            if (victoryPanel != null) victoryPanel.SetActive(false);
            timer.StartTimer();
        }
    }

    private void OnScrambleClicked()
    {
        if (scrambler == null || (rotator != null && rotator.IsBusy)) return;

        moveCount = 0;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (timer != null) timer.ResetTimer();

        scrambler.PerformScramble();

        if (scrambleText != null)
            scrambleText.text = "<b>Scramble:</b> " + scrambler.lastScrambleString;
    }

    private void OnResetClicked()
    {
        moveCount = 0;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (stateManager != null) stateManager.ResetAll();
        if (timer != null) timer.ResetTimer();
        if (scrambleText != null) scrambleText.text = "Sẵn sàng!";
        if (timerText != null) timerText.text = "00:00.000";
    }

    private void OnSpeedChanged(float val)
    {
        if (rotator != null) rotator.rotationDuration = val;
        UpdateSpeedLabel(val);
    }

    private void UpdateSpeedLabel(float val)
    {
        if (speedValueText != null)
            speedValueText.text = $"Tốc độ: <b>{val:F2}s</b>";
    }

    private void CountPlayerMove(string move)
    {
        // Chỉ đếm số nước đi khi đồng hồ đang thực sự chạy
        if (timer != null && timer.currentState == SpeedcubingTimer.TimerState.Running)
        {
            moveCount++;
        }
    }

    private void UpdatePBDisplay()
    {
        if (pbText != null && timer != null)
        {
            pbText.text = timer.personalBest < float.MaxValue
                ? $" PB: <b>{SpeedcubingTimer.FormatTime(timer.personalBest)}</b>"
                : " PB: --:--.---";
        }
    }

    private void ShowVictory()
    {
        if (victoryPanel == null) return;

        // Chỉ chúc mừng nếu người chơi đã bấm Start và đồng hồ đang chạy
        if (timer == null || timer.currentState != SpeedcubingTimer.TimerState.Running)
            return;

        // Khóa đồng hồ ngay lập tức
        timer.StopTimer();

        victoryPanel.SetActive(true);

        if (victoryTimeText != null)
            victoryTimeText.text = $"Thời gian: <b>{SpeedcubingTimer.FormatTime(timer.elapsedTime)}</b>";

        if (victoryMovesText != null)
            victoryMovesText.text = $"Số nước đi: <b>{moveCount}</b>";

        if (timerText != null)
            timerText.text = SpeedcubingTimer.FormatTime(timer.elapsedTime);

        UpdatePBDisplay();
    }

    /// <summary>
    /// Áp dụng bảng màu Flat Design tối giản, thanh lịch
    /// </summary>
    private void ApplyModernFlatDesign()
    {
        // 1. Nút Trộn (Scramble): Xanh dương hiện đại (#2563EB)
        SetFlatColor(btnScramble, new Color(0.15f, 0.39f, 0.92f));

        // 2. Nút Bắt đầu (Start): Xanh lá Emerald nổi bật (#10B981)
        SetFlatColor(btnStart, new Color(0.06f, 0.72f, 0.51f));

        // 3. Nút Undo & Redo: Xám Slate tối giản (#4B5563)
        SetFlatColor(btnUndo, new Color(0.29f, 0.33f, 0.39f));
        SetFlatColor(btnRedo, new Color(0.29f, 0.33f, 0.39f));

        // 4. Nút Reset: Đỏ Crimson cảnh báo (#EF4444)
        SetFlatColor(btnReset, new Color(0.94f, 0.27f, 0.27f));

        // 5. Nút Chơi Lại trong bảng chiến thắng
        SetFlatColor(btnPlayAgain, new Color(0.06f, 0.72f, 0.51f));

        // 6. Nút Tô màu 2D: Tím Violet hiện đại (#8B5CF6)
        SetFlatColor(btnColorPicker, new Color(0.55f, 0.36f, 0.96f));

        // 7. Nút Quét Camera: Xanh Cyan công nghệ (#06B6D4)
        SetFlatColor(btnCameraScanner, new Color(0.02f, 0.71f, 0.83f));
    }

    private void SetFlatColor(Button btn, Color baseColor)
    {
        if (btn == null) return;

        ColorBlock cb = btn.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = baseColor * 1.15f; // Sáng hơn 15% khi di chuột vào
        cb.pressedColor = baseColor * 0.85f;     // Tối hơn 15% khi nhấn
        cb.selectedColor = baseColor;
        cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Xám mờ khi bị khóa
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f; // Hiệu ứng chuyển màu mượt mà
        btn.colors = cb;
    }
}