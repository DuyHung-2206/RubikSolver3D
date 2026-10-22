using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RubikSolutionViewer : MonoBehaviour
{
    [Header("Tham chiếu")]
    public RubikRotator rotator;
    public RubikSolver solver;
    public RubikStateManager stateManager;

    [Header("UI Bảng điều khiển từng bước")]
    public GameObject solverPanel;
    public TextMeshProUGUI stepInfoText;
    public TextMeshProUGUI nextMoveText;
    [Tooltip("Text hiển thị toàn bộ chuỗi công thức giải")]
    public TextMeshProUGUI fullFormulaText;
    public Button btnPrevStep;
    public Button btnNextStep;
    public Button btnAutoSolve;
    public Button btnCloseSolver;

    [Header("Dữ liệu bước giải")]
    public List<string> currentSolution = new List<string>();
    public int currentStepIndex = 0;
    public bool isAutoSolving = false;

    private void Awake()
    {
        if (rotator == null) rotator = FindAnyObjectByType<RubikRotator>();
        if (solver == null) solver = FindAnyObjectByType<RubikSolver>();
        if (stateManager == null) stateManager = FindAnyObjectByType<RubikStateManager>();
    }

    private void Start()
    {
        if (solverPanel != null) solverPanel.SetActive(false);

        if (btnNextStep != null) btnNextStep.onClick.AddListener(StepNext);
        if (btnPrevStep != null) btnPrevStep.onClick.AddListener(StepPrevious);
        if (btnAutoSolve != null) btnAutoSolve.onClick.AddListener(ToggleAutoSolve);
        if (btnCloseSolver != null) btnCloseSolver.onClick.AddListener(CloseViewer);
    }

    // --- HÀM UPDATE QUAN TRỌNG: Tự động mở khóa nút ngay khi xoay xong ---
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
        int total = currentSolution.Count;

        // Tự động mở khóa lại nút Lùi và Tiếp theo khi khối Rubik xoay xong
        if (btnPrevStep != null) btnPrevStep.interactable = !isBusy && currentStepIndex > 0 && !isAutoSolving;
        if (btnNextStep != null) btnNextStep.interactable = !isBusy && currentStepIndex < total && !isAutoSolving;
    }

    public void StartSolution()
    {
        if (rotator != null && rotator.IsBusy) return;

        if (solver == null)
            solver = FindAnyObjectByType<RubikSolver>();

        currentSolution = solver.SolveCurrentState();

        if (currentSolution == null || currentSolution.Count == 0)
        {
            Debug.Log("[SolutionViewer] Không cần giải (Rubik đã giải xong hoặc chưa trộn).");
            return;
        }

        currentStepIndex = 0;
        isAutoSolving = false;
        if (stateManager != null) stateManager.isExecutingUndoRedo = false;

        if (solverPanel != null) solverPanel.SetActive(true);
        UpdateDisplay();
    }

    public void StepNext()
    {
        if (rotator == null || rotator.IsBusy || currentStepIndex >= currentSolution.Count) return;

        string move = currentSolution[currentStepIndex];
        currentStepIndex++;

        StartCoroutine(ExecuteSingleStep(move));
    }

    public void StepPrevious()
    {
        if (rotator == null || rotator.IsBusy || currentStepIndex <= 0) return;

        currentStepIndex--;
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
        else
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

            string move = currentSolution[currentStepIndex];
            currentStepIndex++;
            rotator.EnqueueMove(move);

            while (rotator.IsBusy) yield return null;

            UpdateDisplay();
            yield return new WaitForSeconds(0.05f);
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
        if (solverPanel != null) solverPanel.SetActive(false);
    }

    private void UpdateDisplay()
    {
        if (solverPanel == null || !solverPanel.activeSelf) return;

        int total = currentSolution.Count;

        // 1. Dòng tiến độ
        if (stepInfoText != null)
            stepInfoText.text = $"Tiến độ: <b>{currentStepIndex} / {total}</b> bước";

        // 2. Nước đi tiếp theo
        if (nextMoveText != null)
        {
            if (currentStepIndex < total)
            {
                string next = currentSolution[currentStepIndex];
                nextMoveText.text = $"Nước kế tiếp: <color=#FBBF24><b>[ {next} ]</b></color>";
            }
            else
            {
                nextMoveText.text = "<color=#10B981><b>🎉 ĐÃ HOÀN TẤT GIẢI!</b></color>";
            }
        }

        // 3. TOÀN BỘ CÔNG THỨC GIẢI (Có tô màu nước đang đi)
        if (fullFormulaText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < total; i++)
            {
                if (i == currentStepIndex)
                    sb.Append($"<color=#FBBF24><b>[{currentSolution[i]}]</b></color> "); // Nước hiện tại (Màu Vàng)
                else if (i < currentStepIndex)
                    sb.Append($"<color=#9CA3AF><s>{currentSolution[i]}</s></color> ");   // Nước đã đi (Gạch ngang mờ)
                else
                    sb.Append($"<color=#FFFFFF>{currentSolution[i]}</color> ");          // Nước sắp tới (Trắng)
            }
            fullFormulaText.text = "<b>Công thức toàn bộ:</b>\n" + sb.ToString();
        }

        if (btnAutoSolve != null)
        {
            var textTMP = btnAutoSolve.GetComponentInChildren<TextMeshProUGUI>();
            if (textTMP != null) textTMP.text = isAutoSolving ? "Tạm dừng" : "⚡ Tự động giải";
        }
    }
}