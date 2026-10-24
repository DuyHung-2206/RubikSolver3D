using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class RubikUIController : MonoBehaviour
{
    public enum NavTab
    {
        Home = 0,
        Camera = 1,
        Guide = 2,
        Settings = 3,
        Color2D = 4
    }

    [Header("Tham chiếu Logic")]
    public RubikRotator rotator;
    public RubikStateManager stateManager;
    public RubikScrambler scrambler;
    public SpeedcubingTimer timer;
    public RubikCameraOrbit cameraOrbit;
    public RubikSolutionViewer solutionViewer;
    public RubikAlgorithmInput algorithmInput;
    public RubikNetColorPicker colorPicker;
    public RubikCameraScanner cameraScanner;

    [Header("Sidebar Điều Hướng (Trái & Mobile)")]
    public Button btnNavHome;
    public Button btnNavCamera;
    public Button btnNavGuide;
    public Button btnNavSettings;
    public Button btnNavColor2D;
    public Button btnExplore3D;

    public Button btnMobileNavHome;
    public Button btnMobileNavCamera;
    public Button btnMobileNavGuide;
    public Button btnMobileNavSettings;
    public Button btnMobileNavColor2D;

    [Header("Khu Vực Trung Tâm (Rubik 3D & Camera)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scrambleText;
    public TextMeshProUGUI pbText;
    public Button btnCamReset;
    public Button btnCamZoomIn;
    public Button btnCamZoomOut;
    public Button btnTopRightSettings;

    [Header("Bảng Điều Khiển & Chạy Thử (Bottom Panel)")]
    public GameObject bottomControlCard;
    public Button btnScramble;
    public Button btnStart;
    public Button btnUndo;
    public Button btnRedo;
    public Button btnSolve;
    public Button btnAlgorithm;
    public Button btnReset;
    public Button btnColorPicker;
    public Button btnCameraScanner;

    [Header("Panel Điều Khiển Xoay & Tốc Độ (Right Panel)")]
    public GameObject rightControlContainer;
    public Button btnFaceU;
    public Button btnFaceD;
    public Button btnFaceF;
    public Button btnFaceB;
    public Button btnFaceR;
    public Button btnFaceL;
    public TextMeshProUGUI labelFaceU;
    public TextMeshProUGUI labelFaceD;
    public TextMeshProUGUI labelFaceF;
    public TextMeshProUGUI labelFaceB;
    public TextMeshProUGUI labelFaceR;
    public TextMeshProUGUI labelFaceL;

    public Button btnShiftToggle;
    public Image shiftBadgeBg;
    public TextMeshProUGUI shiftBadgeText;
    public Slider speedSlider;
    public TextMeshProUGUI speedValueText;

    [Header("Hộp Thoại Xác Nhận Đặt Lại (Reset Confirmation)")]
    public GameObject confirmResetPanel;
    public Button btnConfirmResetYes;
    public Button btnConfirmResetCancel;

    [Header("Hộp Thoại Cài Đặt (Settings Panel)")]
    public GameObject settingsPanel;
    public Slider settingsSpeedSlider;
    public TextMeshProUGUI settingsSpeedText;
    public Slider settingsCamSensitivitySlider;
    public TextMeshProUGUI settingsCamSensitivityText;
    public Slider settingsScrambleCountSlider;
    public TextMeshProUGUI settingsScrambleCountText;
    public Button btnResetPB;
    public Button btnCloseSettings;

    [Header("Bảng Chiến Thắng (Victory Overlay)")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryTimeText;
    public TextMeshProUGUI victoryMovesText;
    public Button btnPlayAgain;

    private int moveCount = 0;
    private bool uiPrimeToggle = false;
    private bool lastEffectivePrime = false;
    private NavTab currentTab = NavTab.Home;

    private void Awake()
    {
        FindCoreReferences();
        // Đảm bảo giao diện Friendly Puzzle chuẩn 3 ảnh tham chiếu luôn được khởi tạo và gắn kết đầy đủ
        RubikFriendlyPuzzleUIBuilder.EnsureUI(this);
    }

    public void FindCoreReferences()
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();
        if (scrambler == null) scrambler = FindAnyObjectByType<RubikScrambler>();
        if (timer == null) timer = FindAnyObjectByType<SpeedcubingTimer>();
        if (cameraOrbit == null) cameraOrbit = FindAnyObjectByType<RubikCameraOrbit>();
        if (solutionViewer == null) solutionViewer = FindAnyObjectByType<RubikSolutionViewer>(FindObjectsInactive.Include);
        if (algorithmInput == null) algorithmInput = FindAnyObjectByType<RubikAlgorithmInput>(FindObjectsInactive.Include);
        if (colorPicker == null) colorPicker = FindAnyObjectByType<RubikNetColorPicker>(FindObjectsInactive.Include);
        if (cameraScanner == null) cameraScanner = FindAnyObjectByType<RubikCameraScanner>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        FindCoreReferences();

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (confirmResetPanel != null) confirmResetPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        BindAllUIEvents();

        // Đồng bộ thanh trượt tốc độ với RubikRotator
        if (speedSlider != null && rotator != null)
        {
            speedSlider.minValue = 0.05f;
            speedSlider.maxValue = 0.60f;
            speedSlider.SetValueWithoutNotify(rotator.rotationDuration);
            UpdateSpeedLabel(rotator.rotationDuration);
        }

        SyncSettingsUI();
        UpdatePBDisplay();
        SelectNavTab(NavTab.Home, triggerPanelAction: false);
        UpdatePrimeVisualState(force: true);
    }

    public void BindAllUIEvents()
    {
        // 1. Điều hướng Sidebar & Mobile Nav
        BindButton(btnNavHome, () => SelectNavTab(NavTab.Home));
        BindButton(btnNavCamera, () => SelectNavTab(NavTab.Camera));
        BindButton(btnNavGuide, () => SelectNavTab(NavTab.Guide));
        BindButton(btnNavSettings, () => SelectNavTab(NavTab.Settings));
        BindButton(btnNavColor2D, () => SelectNavTab(NavTab.Color2D));

        BindButton(btnMobileNavHome, () => SelectNavTab(NavTab.Home));
        BindButton(btnMobileNavCamera, () => SelectNavTab(NavTab.Camera));
        BindButton(btnMobileNavGuide, () => SelectNavTab(NavTab.Guide));
        BindButton(btnMobileNavSettings, () => SelectNavTab(NavTab.Settings));
        BindButton(btnMobileNavColor2D, () => SelectNavTab(NavTab.Color2D));

        BindButton(btnExplore3D, OnExplore3DClicked);
        BindButton(btnTopRightSettings, () => SelectNavTab(NavTab.Settings));

        // 2. Điều khiển Camera nổi cạnh khối 3D
        BindButton(btnCamReset, () => cameraOrbit?.ResetCameraView());
        BindButton(btnCamZoomIn, () => cameraOrbit?.ZoomIn());
        BindButton(btnCamZoomOut, () => cameraOrbit?.ZoomOut());

        // 3. Bảng điều khiển & Chạy thử (Bottom Panel)
        BindButton(btnScramble, OnScrambleClicked);
        BindButton(btnStart, OnStartClicked);
        BindButton(btnUndo, () => stateManager?.Undo3D());
        BindButton(btnRedo, () => stateManager?.Redo3D());
        BindButton(btnSolve, OnAutoSolveClicked);
        BindButton(btnAlgorithm, OnOpenAlgorithmClicked);
        BindButton(btnReset, OnResetRequested);
        BindButton(btnPlayAgain, ExecuteImmediateReset);

        if (btnColorPicker != null && btnColorPicker != btnNavColor2D)
            BindButton(btnColorPicker, () => SelectNavTab(NavTab.Color2D));
        if (btnCameraScanner != null && btnCameraScanner != btnNavCamera)
            BindButton(btnCameraScanner, () => SelectNavTab(NavTab.Camera));

        // 4. Panel Điều khiển xoay 6 mặt (U, D, F, B, R, L) & Shift
        BindButton(btnFaceU, () => TriggerFaceRotation('U'));
        BindButton(btnFaceD, () => TriggerFaceRotation('D'));
        BindButton(btnFaceF, () => TriggerFaceRotation('F'));
        BindButton(btnFaceB, () => TriggerFaceRotation('B'));
        BindButton(btnFaceR, () => TriggerFaceRotation('R'));
        BindButton(btnFaceL, () => TriggerFaceRotation('L'));
        BindButton(btnShiftToggle, TogglePrimeMode);

        if (speedSlider != null)
        {
            speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        // 5. Hộp thoại Xác nhận Reset
        BindButton(btnConfirmResetYes, () =>
        {
            if (confirmResetPanel != null) confirmResetPanel.SetActive(false);
            ExecuteImmediateReset();
        });
        BindButton(btnConfirmResetCancel, () =>
        {
            if (confirmResetPanel != null) confirmResetPanel.SetActive(false);
        });

        // 6. Hộp thoại Cài đặt
        BindButton(btnCloseSettings, CloseSettingsPanel);
        BindButton(btnResetPB, () =>
        {
            if (timer != null) timer.ClearPersonalBest();
            UpdatePBDisplay();
        });

        if (settingsSpeedSlider != null)
        {
            settingsSpeedSlider.onValueChanged.RemoveListener(OnSettingsSpeedChanged);
            settingsSpeedSlider.onValueChanged.AddListener(OnSettingsSpeedChanged);
        }
        if (settingsCamSensitivitySlider != null)
        {
            settingsCamSensitivitySlider.onValueChanged.RemoveListener(OnSettingsCamSensitivityChanged);
            settingsCamSensitivitySlider.onValueChanged.AddListener(OnSettingsCamSensitivityChanged);
        }
        if (settingsScrambleCountSlider != null)
        {
            settingsScrambleCountSlider.onValueChanged.RemoveListener(OnSettingsScrambleCountChanged);
            settingsScrambleCountSlider.onValueChanged.AddListener(OnSettingsScrambleCountChanged);
        }
    }

    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
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
        // 1. Cập nhật đồng hồ tốc độ
        if (timerText != null && timer != null)
        {
            if (timer.currentState == SpeedcubingTimer.TimerState.Running)
                timerText.text = SpeedcubingTimer.FormatTime(timer.elapsedTime);
        }

        // 2. Cập nhật trạng thái giữ phím Shift (Xoay ngược chiều)
        UpdatePrimeVisualState(force: false);

        // 3. Tự động đồng bộ trạng thái Sidebar khi người dùng đóng các bảng phụ
        SyncActiveNavTabWithPanels();

        // 4. Tự động khóa/mở khóa nút khi khối Rubik đang bận xoay
        UpdateInteractableStates();
    }

    private void UpdateInteractableStates()
    {
        bool isBusy = rotator != null && rotator.IsBusy;
        bool hasUndo = stateManager != null && stateManager.GetUndoMoveHistory().Count > 0;

        if (btnScramble != null) btnScramble.interactable = !isBusy;
        if (btnStart != null) btnStart.interactable = !isBusy && (timer == null || timer.currentState != SpeedcubingTimer.TimerState.Running);
        if (btnUndo != null) btnUndo.interactable = !isBusy && hasUndo;
        if (btnRedo != null) btnRedo.interactable = !isBusy;
        if (btnReset != null) btnReset.interactable = !isBusy;
        if (btnSolve != null) btnSolve.interactable = !isBusy;
        if (btnAlgorithm != null) btnAlgorithm.interactable = !isBusy;

        if (btnFaceU != null) btnFaceU.interactable = !isBusy;
        if (btnFaceD != null) btnFaceD.interactable = !isBusy;
        if (btnFaceF != null) btnFaceF.interactable = !isBusy;
        if (btnFaceB != null) btnFaceB.interactable = !isBusy;
        if (btnFaceR != null) btnFaceR.interactable = !isBusy;
        if (btnFaceL != null) btnFaceL.interactable = !isBusy;
    }

    private void UpdatePrimeVisualState(bool force)
    {
        bool shiftHeld = Keyboard.current != null &&
                         (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        bool effectivePrime = shiftHeld || uiPrimeToggle;

        if (!force && effectivePrime == lastEffectivePrime)
            return;

        lastEffectivePrime = effectivePrime;

        if (shiftBadgeBg != null)
        {
            shiftBadgeBg.color = effectivePrime
                ? new Color32(0x25, 0x63, 0xEB, 255)
                : new Color32(0xE2, 0xE8, 0xF0, 255);
        }
        if (shiftBadgeText != null)
        {
            shiftBadgeText.color = effectivePrime
                ? Color.white
                : new Color32(0x47, 0x55, 0x69, 255);
        }

        string s = effectivePrime ? "'" : "";
        if (labelFaceU != null) labelFaceU.text = "U" + s;
        if (labelFaceD != null) labelFaceD.text = "D" + s;
        if (labelFaceF != null) labelFaceF.text = "F" + s;
        if (labelFaceB != null) labelFaceB.text = "B" + s;
        if (labelFaceR != null) labelFaceR.text = "R" + s;
        if (labelFaceL != null) labelFaceL.text = "L" + s;
    }

    public void TogglePrimeMode()
    {
        uiPrimeToggle = !uiPrimeToggle;
        UpdatePrimeVisualState(force: true);
    }

    public void TriggerFaceRotation(char face)
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (rotator == null || rotator.IsBusy) return;

        rotator.RotateFaceFromUI(face, uiPrimeToggle);
    }

    /// <summary>
    /// Chuyển đổi giữa 5 mục trên Sidebar (Trang chủ, Quét camera, Hướng dẫn giải, Cài đặt, Tô màu 2D).
    /// </summary>
    public void SelectNavTab(NavTab tab, bool triggerPanelAction = true)
    {
        FindCoreReferences();
        currentTab = tab;
        UpdateNavButtonsVisual(tab);

        if (!triggerPanelAction) return;

        // Đóng các bảng phụ trước khi mở trang mới
        if (tab != NavTab.Camera && cameraScanner != null && cameraScanner.scannerPanel != null && cameraScanner.scannerPanel.activeSelf)
            cameraScanner.CloseScanner();

        if (tab != NavTab.Color2D && colorPicker != null && colorPicker.netPanel != null && colorPicker.netPanel.activeSelf)
            colorPicker.ClosePanel();

        if (tab != NavTab.Guide && solutionViewer != null && solutionViewer.solverPanel != null && solutionViewer.solverPanel.activeSelf)
            solutionViewer.CloseViewer();

        if (tab != NavTab.Settings && settingsPanel != null && settingsPanel.activeSelf)
            settingsPanel.SetActive(false);

        switch (tab)
        {
            case NavTab.Home:
                SetMainWorkspacePanelsVisible(true);
                break;

            case NavTab.Camera:
                SetMainWorkspacePanelsVisible(true);
                if (cameraScanner != null)
                    cameraScanner.OpenScanner();
                break;

            case NavTab.Guide:
                // Ẩn bớt bảng điều khiển phải & dưới để khung Hướng dẫn giải (Ảnh 3) bao quanh khối Rubik 3D thoáng đãng
                SetMainWorkspacePanelsVisible(false);
                if (solutionViewer != null)
                    solutionViewer.StartSolution();
                break;

            case NavTab.Settings:
                SetMainWorkspacePanelsVisible(true);
                SyncSettingsUI();
                if (settingsPanel != null)
                    settingsPanel.SetActive(true);
                break;

            case NavTab.Color2D:
                SetMainWorkspacePanelsVisible(true);
                if (colorPicker != null)
                {
                    if (colorPicker.netPanel != null && !colorPicker.netPanel.activeSelf)
                        colorPicker.TogglePanel();
                }
                break;
        }
    }

    public void OnGuideClosed()
    {
        SetMainWorkspacePanelsVisible(true);
        currentTab = NavTab.Home;
        UpdateNavButtonsVisual(NavTab.Home);
    }

    private void SetMainWorkspacePanelsVisible(bool visible)
    {
        if (bottomControlCard != null)
            bottomControlCard.SetActive(visible);
        if (rightControlContainer != null)
            rightControlContainer.SetActive(visible);
    }

    private void SyncActiveNavTabWithPanels()
    {
        if (currentTab == NavTab.Camera && (cameraScanner == null || cameraScanner.scannerPanel == null || !cameraScanner.scannerPanel.activeSelf))
        {
            currentTab = NavTab.Home;
            UpdateNavButtonsVisual(NavTab.Home);
        }
        else if (currentTab == NavTab.Color2D && (colorPicker == null || colorPicker.netPanel == null || !colorPicker.netPanel.activeSelf))
        {
            currentTab = NavTab.Home;
            UpdateNavButtonsVisual(NavTab.Home);
        }
        else if (currentTab == NavTab.Settings && (settingsPanel == null || !settingsPanel.activeSelf))
        {
            currentTab = NavTab.Home;
            UpdateNavButtonsVisual(NavTab.Home);
        }
    }

    private void UpdateNavButtonsVisual(NavTab selectedTab)
    {
        StyleNavButton(btnNavHome, selectedTab == NavTab.Home);
        StyleNavButton(btnNavCamera, selectedTab == NavTab.Camera);
        StyleNavButton(btnNavGuide, selectedTab == NavTab.Guide);
        StyleNavButton(btnNavSettings, selectedTab == NavTab.Settings);
        StyleNavButton(btnNavColor2D, selectedTab == NavTab.Color2D);

        StyleNavButton(btnMobileNavHome, selectedTab == NavTab.Home);
        StyleNavButton(btnMobileNavCamera, selectedTab == NavTab.Camera);
        StyleNavButton(btnMobileNavGuide, selectedTab == NavTab.Guide);
        StyleNavButton(btnMobileNavSettings, selectedTab == NavTab.Settings);
        StyleNavButton(btnMobileNavColor2D, selectedTab == NavTab.Color2D);
    }

    private void StyleNavButton(Button btn, bool isSelected)
    {
        if (btn == null) return;

        Image bg = btn.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isSelected
                ? new Color32(0x3B, 0x82, 0xF6, 255) // Xanh dương chuẩn ảnh tham chiếu #3B82F6
                : new Color32(0xFF, 0xFF, 0xFF, 0);  // Trong suốt trên nền trắng
        }

        Transform iconTr = btn.transform.Find("Icon");
        if (iconTr != null)
        {
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.color = isSelected
                    ? Color.white
                    : new Color32(0x64, 0x74, 0x8B, 255); // Icon xám #64748B
            }
        }

        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = isSelected
                ? Color.white
                : new Color32(0x33, 0x41, 0x55, 255); // Chữ xám đậm #334155
            label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void OnExplore3DClicked()
    {
        SelectNavTab(NavTab.Home);
        if (cameraOrbit != null)
            cameraOrbit.StartExploreOrbitTour();
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
            scrambleText.text = "Trộn: " + scrambler.lastScrambleString;
    }

    private void OnAutoSolveClicked()
    {
        if (rotator != null && rotator.IsBusy) return;
        FindCoreReferences();

        if (solutionViewer != null)
        {
            currentTab = NavTab.Guide;
            UpdateNavButtonsVisual(NavTab.Guide);
            SetMainWorkspacePanelsVisible(false);
            solutionViewer.StartAutoSolveDirect();
        }
    }

    private void OnOpenAlgorithmClicked()
    {
        if (rotator != null && rotator.IsBusy) return;
        FindCoreReferences();

        if (algorithmInput != null)
        {
            algorithmInput.TogglePanel();
        }
    }

    private void OnResetRequested()
    {
        if (rotator != null && rotator.IsBusy) return;

        bool hasProgress = (stateManager != null && (!stateManager.IsSolved() || stateManager.GetUndoMoveHistory().Count > 0)) ||
                           (timer != null && timer.currentState == SpeedcubingTimer.TimerState.Running);

        if (hasProgress && confirmResetPanel != null)
        {
            confirmResetPanel.SetActive(true);
        }
        else
        {
            ExecuteImmediateReset();
        }
    }

    public void ExecuteImmediateReset()
    {
        moveCount = 0;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (confirmResetPanel != null) confirmResetPanel.SetActive(false);
        if (solutionViewer != null && solutionViewer.solverPanel != null && solutionViewer.solverPanel.activeSelf)
            solutionViewer.CloseViewer();

        if (stateManager != null) stateManager.ResetAll();
        if (timer != null) timer.ResetTimer();
        if (scrambleText != null) scrambleText.text = "";
        if (timerText != null) timerText.text = "00:00.000";
    }

    private void OnSpeedChanged(float val)
    {
        if (rotator != null) rotator.rotationDuration = val;
        UpdateSpeedLabel(val);
        if (settingsSpeedSlider != null)
            settingsSpeedSlider.SetValueWithoutNotify(val);
        if (settingsSpeedText != null)
            settingsSpeedText.text = $"{val:F2}s";
    }

    private void UpdateSpeedLabel(float val)
    {
        if (speedValueText != null)
            speedValueText.text = $"{val:F2}s";
    }

    private void SyncSettingsUI()
    {
        if (rotator != null && settingsSpeedSlider != null)
        {
            settingsSpeedSlider.minValue = 0.05f;
            settingsSpeedSlider.maxValue = 0.60f;
            settingsSpeedSlider.SetValueWithoutNotify(rotator.rotationDuration);
            if (settingsSpeedText != null) settingsSpeedText.text = $"{rotator.rotationDuration:F2}s";
        }

        if (cameraOrbit != null && settingsCamSensitivitySlider != null)
        {
            settingsCamSensitivitySlider.minValue = 5f;
            settingsCamSensitivitySlider.maxValue = 35f;
            settingsCamSensitivitySlider.SetValueWithoutNotify(cameraOrbit.xSpeed);
            if (settingsCamSensitivityText != null) settingsCamSensitivityText.text = $"{cameraOrbit.xSpeed:F0}";
        }

        if (scrambler != null && settingsScrambleCountSlider != null)
        {
            settingsScrambleCountSlider.minValue = 10f;
            settingsScrambleCountSlider.maxValue = 25f;
            settingsScrambleCountSlider.wholeNumbers = true;
            settingsScrambleCountSlider.SetValueWithoutNotify(scrambler.scrambleMoveCount);
            if (settingsScrambleCountText != null) settingsScrambleCountText.text = $"{scrambler.scrambleMoveCount} bước";
        }
    }

    private void OnSettingsSpeedChanged(float val)
    {
        if (rotator != null) rotator.rotationDuration = val;
        if (speedSlider != null) speedSlider.SetValueWithoutNotify(val);
        UpdateSpeedLabel(val);
        if (settingsSpeedText != null) settingsSpeedText.text = $"{val:F2}s";
    }

    private void OnSettingsCamSensitivityChanged(float val)
    {
        if (cameraOrbit != null)
        {
            cameraOrbit.xSpeed = val;
            cameraOrbit.ySpeed = val;
        }
        if (settingsCamSensitivityText != null)
            settingsCamSensitivityText.text = $"{val:F0}";
    }

    private void OnSettingsScrambleCountChanged(float val)
    {
        int count = Mathf.RoundToInt(val);
        if (scrambler != null)
            scrambler.scrambleMoveCount = count;
        if (settingsScrambleCountText != null)
            settingsScrambleCountText.text = $"{count} bước";
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (currentTab == NavTab.Settings)
        {
            currentTab = NavTab.Home;
            UpdateNavButtonsVisual(NavTab.Home);
        }
    }

    private void CountPlayerMove(string move)
    {
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
                ? $"PB: {SpeedcubingTimer.FormatTime(timer.personalBest)}"
                : "PB: --:--.---";
        }
    }

    private void ShowVictory()
    {
        if (victoryPanel == null) return;

        if (timer == null || timer.currentState != SpeedcubingTimer.TimerState.Running)
            return;

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
}
